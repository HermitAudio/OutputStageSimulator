using OutputStageSimulator.Core;

namespace OutputStageSimulator.Core.Tests;

[TestFixture]
public class HfeModelTests
{
    // Imax must be <= Iturnover: Imax is the reference point of the log-shaped
    // region below the turnover, where linear derating (dI) takes over.
    private static HfeModel MakeModel() => new()
    {
        Hfemax = 100,
        Imax = 2,
        AFactor = 2,
        DI = 4,
        Iturnover = 3,
    };

    [Test]
    public void Hfe_AtZeroCurrent_ReturnsHfemax()
    {
        var model = MakeModel();
        Assert.That(model.Hfe(0), Is.EqualTo(100));
    }

    [Test]
    public void Hfe_AtImax_ReturnsHfemax()
    {
        // log10(Imax/Imax) = log10(1) = 0, so the a-factor term vanishes.
        var model = MakeModel();
        Assert.That(model.Hfe(model.Imax), Is.EqualTo(100).Within(1e-9));
    }

    [Test]
    public void Hfe_AboveIturnover_DeratesLinearlyWithSlopeDI()
    {
        var model = MakeModel();
        model.RecomputeTurnoverGain();

        var atTurnover = model.HfeAtTurnover;
        var oneAmpAbove = model.Hfe(model.Iturnover + 1);

        Assert.That(atTurnover - oneAmpAbove, Is.EqualTo(model.DI).Within(1e-9));
    }

    [Test]
    public void RecomputeTurnoverGain_MatchesDirectCallAtIturnover()
    {
        var model = MakeModel();
        model.RecomputeTurnoverGain();

        // hfe(Iturnover) always takes the log-shaped branch since Iturnover > Iturnover is false.
        var expected = model.Hfemax / (1 + model.AFactor * Math.Pow(Math.Log10(model.Iturnover / model.Imax), 2));
        Assert.That(model.HfeAtTurnover, Is.EqualTo(expected).Within(1e-9));
    }

    [Test]
    public void Hfe_BelowImax_IsLessThanHfemax()
    {
        var model = MakeModel();
        Assert.That(model.Hfe(model.Imax / 2), Is.LessThan(model.Hfemax));
    }

    // One-sided differences evaluated right at `at`, not straddling it — a
    // central difference here would blend the two branches together right
    // where we're trying to measure whether they actually match.
    private static double BackwardSlope(HfeModel model, double at, double h = 1e-6) =>
        (model.Hfe(at) - model.Hfe(at - h)) / h;

    private static double ForwardSlope(HfeModel model, double at, double h = 1e-6) =>
        (model.Hfe(at + h) - model.Hfe(at)) / h;

    [Test]
    public void Piecewise_HasARealSlopeDiscontinuityAtIturnover()
    {
        // Documents the actual bug the SmoothBlend curve kind fixes: the value
        // is continuous at Iturnover by construction, but the slope jumps.
        var model = MakeModel();
        model.CurveKind = HfeCurveKind.Piecewise;
        model.RecomputeTurnoverGain();

        var slopeBelow = BackwardSlope(model, model.Iturnover);
        var slopeAbove = ForwardSlope(model, model.Iturnover);

        // The linear branch's slope is exactly -DI everywhere above Iturnover.
        Assert.That(slopeAbove, Is.EqualTo(-model.DI).Within(1e-6));
        Assert.That(Math.Abs(slopeAbove - slopeBelow), Is.GreaterThan(0.5),
            "expected a real slope discontinuity at Iturnover");
    }

    [Test]
    public void SmoothBlend_HasNoSlopeDiscontinuityAtIturnover()
    {
        var model = MakeModel();
        model.CurveKind = HfeCurveKind.SmoothBlend;
        model.RecomputeTurnoverGain();

        var slopeBelow = BackwardSlope(model, model.Iturnover);
        var slopeAbove = ForwardSlope(model, model.Iturnover);

        Assert.That(slopeAbove, Is.EqualTo(slopeBelow).Within(1e-3));
    }

    [Test]
    public void SmoothBlend_MatchesPiecewiseClosely_AwayFromIturnover()
    {
        // The two curve kinds should agree closely far from the transition —
        // SmoothBlend should only meaningfully change the curve right around
        // Iturnover, not the calibrated shape everywhere else.
        var piecewise = MakeModel();
        piecewise.CurveKind = HfeCurveKind.Piecewise;
        piecewise.RecomputeTurnoverGain();

        var smooth = MakeModel();
        smooth.CurveKind = HfeCurveKind.SmoothBlend;
        smooth.RecomputeTurnoverGain();

        foreach (var i in new[] { 0.05, 0.2, 1.0, 6.0, 10.0 })
        {
            Assert.That(smooth.Hfe(i), Is.EqualTo(piecewise.Hfe(i)).Within(0.5),
                $"at i={i}, far from Iturnover={piecewise.Iturnover}");
        }
    }
}

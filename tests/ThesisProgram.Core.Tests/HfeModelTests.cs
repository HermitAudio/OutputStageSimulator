using ThesisProgram.Core;

namespace ThesisProgram.Core.Tests;

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
}

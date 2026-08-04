using OutputStageSimulator.Core;

namespace OutputStageSimulator.Core.Tests;

[TestFixture]
public class OutputStagePipelineTests
{
    private static OutputStagePipeline MakePipeline(double offset = 1.0)
    {
        var hfe = new HfeModel
        {
            Hfemax = 100,
            Imax = 10,
            AFactor = 2,
            DI = 5,
            Iturnover = 3,
        };
        hfe.RecomputeTurnoverGain();

        return new OutputStagePipeline(hfe)
        {
            Rg = 100,
            Rl = 8,
            Iq = 0.05,
            Offset = offset,
            AntPar = 1,
        };
    }

    [Test]
    public void BaseCurrent_WithMatchedPair_IsOddFunction()
    {
        var pipeline = MakePipeline(offset: 1.0);

        foreach (var x in new[] { 0.1, 0.5, 1.0, 2.0, 5.0 })
        {
            Assert.That(pipeline.BaseCurrent(-x), Is.EqualTo(-pipeline.BaseCurrent(x)).Within(1e-9),
                $"BaseCurrent should be odd at x={x} for a matched pair (offset=1)");
        }
    }

    [Test]
    public void G_IsZero_WhenXIsTheSolvedRoot()
    {
        var pipeline = MakePipeline();
        const double z = 0.2;
        var x = 1.0;

        TraubSolver.TraubIteration(pipeline.G, 1e-9, z, ref x);

        Assert.That(pipeline.G(x, z), Is.EqualTo(0.0).Within(1e-6));
    }

    [Test]
    public void Analyze_SymmetricPushPull_ProducesOnlyOddHarmonics()
    {
        var pipeline = MakePipeline(offset: 1.0);
        var samples = pipeline.BuildTestTone(20.0);

        var result = pipeline.Analyze(samples);

        foreach (var h in result.Harmonics)
        {
            if (h.HarmonicNumber >= 2 && h.HarmonicNumber % 2 == 0)
            {
                Assert.That(h.Magnitude, Is.LessThan(1e-6),
                    $"harmonic {h.HarmonicNumber} should be ~0 for a symmetric push-pull stage");
            }
        }

        Assert.That(result.Thd, Is.GreaterThan(0).And.LessThan(100));
    }

    [Test]
    public void Analyze_MismatchedPair_ProducesNonZeroEvenHarmonics()
    {
        var pipeline = MakePipeline(offset: 1.3);
        var samples = pipeline.BuildTestTone(20.0);

        var result = pipeline.Analyze(samples);

        var secondHarmonic = result.Harmonics.Single(h => h.HarmonicNumber == 2);
        Assert.That(secondHarmonic.Magnitude, Is.GreaterThan(1e-6),
            "a mismatched pair (offset != 1) should introduce even-order distortion");
    }

    [Test]
    public void BuildTestTone_ProducesOneFullCyclePeakingAtItsSpecifiedAmplitude()
    {
        var pipeline = MakePipeline();
        var samples = pipeline.BuildTestTone(20.0);

        // Peak occurs at i=64 (sin(pi/2)=1) in this program's sample indexing.
        var expectedPeak = samples[64].Re;
        for (var i = 1; i <= FftProcessor.MaxElement; i++)
        {
            Assert.That(samples[i].Re, Is.LessThanOrEqualTo(expectedPeak + 1e-9));
            Assert.That(samples[i].Im, Is.EqualTo(0.0));
        }
    }
}

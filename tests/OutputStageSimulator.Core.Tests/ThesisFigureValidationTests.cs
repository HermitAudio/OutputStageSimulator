using OutputStageSimulator.Core;

namespace OutputStageSimulator.Core.Tests;

/// <summary>
/// Validates the port against measured results transcribed from the thesis
/// itself (see <see cref="ThesisPresets"/> — currently figures 5.14.1,
/// 5.14.4, 5.14.5, 5.14.6, pp. 71, 74, 75, 76, out of 11 total in the
/// thesis): a 14V peak test tone into a 4 ohm load, run through a BD203/204
/// push-pull stage in four bias configurations (current-driven class AB,
/// class B and class A, plus voltage-driven class B). No fitted/calibrated
/// parameters here — Uut=14V and Rl=4 ohm are exactly what the thesis states
/// it used; every other input is transcribed directly from the figure's
/// printout. Matching the thesis's THD and 2nd-5th harmonic levels to the
/// same precision it printed them at (4 and 2 decimal digits respectively)
/// confirms the FFT, Traub solver and hfe/f/g model are all correctly
/// ported — including the class-B cases, whose distortion spectrum has a
/// non-monotonic, "wavy" envelope in the higher-order odd harmonics (a real
/// crossover-distortion effect from Iq=0's hard device handoff at the zero
/// crossing, not an FFT artifact — see SquareTriangleWaveFftTests for the
/// FFT's independent verification against textbook closed-form spectra).
/// </summary>
[TestFixture]
public class ThesisFigureValidationTests
{
    private static IEnumerable<ThesisPreset> PresetsWithExpectedResults() =>
        ThesisPresets.All.Where(p => p.ExpectedThd.HasValue);

    [TestCaseSource(nameof(PresetsWithExpectedResults))]
    public void Analyze_MatchesThesisFigure(ThesisPreset preset)
    {
        var pipeline = preset.CreatePipeline();
        var result = pipeline.Analyze(pipeline.BuildTestTone(preset.PeakOutputVoltage));

        Assert.That(result.Fundamental, Is.EqualTo(preset.ExpectedGrunntone!.Value).Within(0.005));
        Assert.That(result.Thd, Is.EqualTo(preset.ExpectedThd!.Value).Within(0.0001));

        var h2 = result.Harmonics.Single(h => h.HarmonicNumber == 2);
        var h3 = result.Harmonics.Single(h => h.HarmonicNumber == 3);
        var h4 = result.Harmonics.Single(h => h.HarmonicNumber == 4);
        var h5 = result.Harmonics.Single(h => h.HarmonicNumber == 5);

        Assert.That(h2.Db, Is.EqualTo(preset.ExpectedDb2nd!.Value).Within(0.01));
        Assert.That(h3.Db, Is.EqualTo(preset.ExpectedDb3rd!.Value).Within(0.01));
        Assert.That(h4.Db, Is.EqualTo(preset.ExpectedDb4th!.Value).Within(0.01));
        Assert.That(h5.Db, Is.EqualTo(preset.ExpectedDb5th!.Value).Within(0.01));
    }
}

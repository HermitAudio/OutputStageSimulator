using System.Numerics;
using OutputStageSimulator.Core;

namespace OutputStageSimulator.Core.Tests;

/// <summary>
/// Diagnostic-only tests using <see cref="TraubSolver"/>'s trace hooks.
/// Marked [Explicit] so they never run as part of a normal `dotnet test` —
/// run them individually (e.g. `dotnet test --filter Name=...`) when you
/// actually want to look at iteration counts or a non-convergent run.
/// </summary>
[TestFixture]
public class TraubDiagnosticsTests
{
    [Test]
    [Explicit("Diagnostic: reports how many Traub iterations each real thesis preset actually needs.")]
    public void IterationCounts_ForRealThesisPresets_AreReported()
    {
        foreach (var preset in ThesisPresets.All)
        {
            var pipeline = preset.CreatePipeline();
            var samples = pipeline.BuildTestTone(preset.PeakOutputVoltage);

            var iterationsPerSample = new int[FftProcessor.MaxElement + 1];
            TraubSolver.Traub(pipeline.G, 1e-6, samples, trace: (sampleIndex, iteration, _, _) =>
            {
                // Last call per sample has the final iteration count for that sample.
                iterationsPerSample[sampleIndex] = iteration;
            });

            var counts = iterationsPerSample.Skip(1).ToArray();
            TestContext.Out.WriteLine($"{preset.Title}: min={counts.Min()}  max={counts.Max()}  avg={counts.Average():F2}");
        }
    }

    [Test]
    [Explicit("Diagnostic: confirms the mixed absolute/relative convergence test resolves the old Rg=300000 + full-precision-Math.PI oscillation at the sine tone's zero crossing (sample i=128).")]
    public void Rg300000WithFullPrecisionPi_NowConvergesCleanlyAtTheZeroCrossing()
    {
        var hfe = new HfeModel { Hfemax = 7500, Imax = 1.10, AFactor = 0.4, DI = 1000, Iturnover = 2.00 };
        hfe.RecomputeTurnoverGain();
        var pipeline = new OutputStagePipeline(hfe) { Rg = 300000, Rl = 4, Iq = 0.8, Offset = 1.0, AntPar = 1 };

        // Same math as OutputStagePipeline.BuildTestTone, except using full
        // double-precision Math.PI instead of the thesis's truncated
        // FftProcessor.Pi (3.1415927). Sample i=128 is the sine tone's zero
        // crossing (i/128*pi = pi exactly) — with Math.PI, z lands at the
        // machine-epsilon floor there, which used to send the old pure-relative
        // convergence test into a stable, never-converging 4-iteration limit
        // cycle (see git history for the captured trace). The mixed
        // absolute/relative test should resolve it cleanly instead.
        const double peakOutputVoltage = 14.0;
        var iut = peakOutputVoltage / pipeline.Rl;
        var ib = pipeline.AntPar * pipeline.BaseCurrent(iut / pipeline.AntPar);
        var itopp = ib + peakOutputVoltage / pipeline.Rg;

        var samples = new Complex[FftProcessor.MaxElement + 1];
        for (var i = 1; i <= FftProcessor.MaxElement; i++)
        {
            samples[i] = new Complex(itopp * Math.Sin(i / 128.0 * Math.PI), 0.0);
        }

        var zeroCrossingTrace = new List<(int Iteration, double X, double Delta)>();

        void Trace(int sampleIndex, int iteration, double x, double delta)
        {
            if (sampleIndex == 128)
            {
                zeroCrossingTrace.Add((iteration, x, delta));
            }
        }

        Assert.DoesNotThrow(() => TraubSolver.Traub(pipeline.G, 1e-6, samples, trace: Trace));

        TestContext.Out.WriteLine("Converged for all 256 samples (previously hit the iteration cap at i=128).");
        TestContext.Out.WriteLine($"Sample i=128 took {zeroCrossingTrace.Count} iterations:");
        foreach (var (iteration, x, delta) in zeroCrossingTrace)
        {
            TestContext.Out.WriteLine($"  iter={iteration,4}  x={x,18:G10}  delta={delta,14:E4}");
        }
    }
}

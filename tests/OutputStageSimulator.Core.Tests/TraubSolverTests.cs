using OutputStageSimulator.Core;
using System.Numerics;

namespace OutputStageSimulator.Core.Tests;

[TestFixture]
public class TraubSolverTests
{
    [Test]
    public void TraubIteration_SolvesLinearEquation()
    {
        var x = 1.0;
        TraubSolver.TraubIteration((val, z) => val - z, 1e-9, 5.0, ref x);
        Assert.That(x, Is.EqualTo(5.0).Within(1e-6));
    }

    [Test]
    public void TraubIteration_SolvesCubicRoot()
    {
        var x = 1.0;
        TraubSolver.TraubIteration((val, z) => val * val * val - z, 1e-9, 8.0, ref x);
        Assert.That(x, Is.EqualTo(2.0).Within(1e-6));
    }

    [Test]
    public void TraubIteration_ThrowsInsteadOfLoopingForever_WhenNoRootExists()
    {
        // sin(x) never reaches 2, so this has no real root — a genuinely
        // non-convergent case, independent of any precision/stiffness issue.
        var x = 1.0;
        var ex = Assert.Throws<InvalidOperationException>(() =>
            TraubSolver.TraubIteration((val, z) => Math.Sin(val) - z, 1e-9, 2.0, ref x, maxIterations: 20));

        Assert.That(ex!.Message, Does.Contain("did not converge"));
    }

    [Test]
    public void Traub_OnIdentityEquation_RecoversOriginalSamples()
    {
        var samples = new Complex[FftProcessor.MaxElement + 1];
        for (var i = 1; i <= FftProcessor.MaxElement; i++)
        {
            samples[i] = new Complex(i * 0.1, 0.0);
        }

        // g(x, z) = x - z  =>  root is always x = z.
        TraubSolver.Traub((val, z) => val - z, 1e-9, samples);

        for (var i = 1; i <= FftProcessor.MaxElement; i++)
        {
            Assert.That(samples[i].Real, Is.EqualTo(i * 0.1).Within(1e-6), $"sample {i}");
        }
    }
}

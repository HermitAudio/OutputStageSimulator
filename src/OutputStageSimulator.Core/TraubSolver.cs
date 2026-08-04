using System.Numerics;

namespace OutputStageSimulator.Core;

/// <summary>
/// Port of module `Traub_metode` (thesis appendix D.3, p.120-121).
/// A Muller/secant-type root finder used to solve the implicit node
/// equation g(x,z)=0 for x, given z, one sample at a time.
/// </summary>
public static class TraubSolver
{
    public delegate double GFunction(double x, double z);

    /// <summary>Called once per iteration with (iteration number, current x, last step size delta = x - previous x).</summary>
    public delegate void IterationTrace(int iteration, double x, double delta);

    /// <summary>Called once per iteration with (sample index, iteration number, current x, last step size delta).</summary>
    public delegate void SampleIterationTrace(int sampleIndex, int iteration, double x, double delta);

    /// <summary>
    /// Default cap on <see cref="TraubIteration"/>'s convergence loop, as a
    /// backstop in case some future input still doesn't converge under the
    /// mixed absolute/relative test below — normal cases converge in single
    /// digits of iterations, so 1000 is a large margin, not a tuning knob.
    /// </summary>
    public const int MaxIterations = 1000;

    /// <summary>
    /// Default absolute convergence floor (in the same units as x, e.g. volts).
    /// See <see cref="TraubIteration"/> for why this is needed.
    /// </summary>
    public const double DefaultAbsoluteTolerance = 1e-9;

    /// <summary>
    /// Solves g(x, <paramref name="z"/>) = 0 for x, starting from <paramref name="x"/>
    /// as the initial guess, and returns the root in <paramref name="x"/>.
    /// Pascal: traub_iteration(g, error_limit, var input, var for_x) — `input` (here:
    /// z) is never reassigned in the original body, so it is ported as pass-by-value.
    /// </summary>
    /// <remarks>
    /// The original Pascal (and this port, until now) tested convergence with a
    /// purely <em>relative</em> criterion: e = 1 - for_x/now_x. That divides by
    /// now_x, and when the true root is at or near exactly zero — e.g. the
    /// generator-current test tone's zero crossing — both for_x and now_x end up
    /// at the machine-epsilon floor (~1e-15), where that division amplifies
    /// ordinary floating-point noise into an apparently large relative error.
    /// The result was a genuine, stable few-iteration limit cycle that never
    /// satisfied the tolerance, rather than diverging or erroring outright.
    /// <para/>
    /// This is replaced with the standard mixed absolute/relative test used by
    /// most numerical solvers: converged when |Δx| ≤ atol + rtol·|x|. This has
    /// no division at all, so there's nothing for near-zero values to blow up —
    /// the absolute term (<paramref name="absoluteTolerance"/>) takes over
    /// exactly where the pure-relative test broke down, while the relative term
    /// (<paramref name="errorLimit"/>) preserves the original behavior once x is
    /// away from zero.
    /// </remarks>
    /// <param name="trace">
    /// Optional per-iteration diagnostic hook (iteration, x, delta) — e.g. to log
    /// iteration counts or inspect a run that isn't converging. Costs a single
    /// null check per iteration when not supplied; nothing is collected or
    /// logged by default.
    /// </param>
    public static void TraubIteration(
        GFunction g, double errorLimit, double z, ref double x,
        int maxIterations = MaxIterations, double absoluteTolerance = DefaultAbsoluteTolerance,
        IterationTrace? trace = null)
    {
        var forX = x;
        var nowY = g(forX, z);
        var nowX = forX * 1.1;
        double delta;
        double tolerance;
        var iteration = 0;
        do
        {
            iteration++;

            var forY = nowY;
            nowY = g(nowX, z);
            var deriv = (nowY - forY) / (nowX - forX);
            var zett = nowX - (nowY / deriv);
            var fzett = g(zett, z);
            forX = nowX;
            var help = 2 * fzett - nowY;
            nowX = help == 0
                ? zett
                : forX - (forX - zett) * ((fzett - nowY) / help);

            delta = nowX - forX;
            tolerance = absoluteTolerance + errorLimit * Math.Abs(nowX);

            trace?.Invoke(iteration, nowX, delta);

            if (iteration >= maxIterations && Math.Abs(delta) > tolerance)
            {
                throw new InvalidOperationException(
                    $"Traub iteration did not converge within {maxIterations} iterations " +
                    $"(z={z}, x={nowX}, delta={delta:E3}, tolerance={tolerance:E3}).");
            }
        } while (Math.Abs(delta) > tolerance);

        x = nowX;
    }

    /// <summary>
    /// Solves g(x, z)=0 for every sample in <paramref name="main"/>, using each
    /// solution as the initial guess for the next (Pascal: Traub). Overwrites
    /// each element's Real (originally the generator current z) with the
    /// solved output voltage x.
    /// </summary>
    /// <param name="trace">
    /// Optional per-iteration diagnostic hook (sample index, iteration, x, delta).
    /// See <see cref="TraubIteration"/>'s trace parameter — same zero-cost-when-null behavior.
    /// </param>
    public static void Traub(
        GFunction g, double errorLimit, Complex[] main,
        int maxIterations = MaxIterations, double absoluteTolerance = DefaultAbsoluteTolerance,
        SampleIterationTrace? trace = null)
    {
        var output = main[1].Real * 1000;
        for (var i = 1; i <= FftProcessor.MaxElement; i++)
        {
            var z = main[i].Real;
            var sampleIndex = i;
            IterationTrace? iterationTrace = trace is null
                ? null
                : (iteration, x, delta) => trace(sampleIndex, iteration, x, delta);

            try
            {
                TraubIteration(g, errorLimit, z, ref output, maxIterations, absoluteTolerance, iterationTrace);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"{ex.Message} (at sample i={i})", ex);
            }

            main[i] = new Complex(output, main[i].Imaginary);
        }
    }
}

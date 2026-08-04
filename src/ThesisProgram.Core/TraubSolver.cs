namespace ThesisProgram.Core;

/// <summary>
/// Port of module `Traub_metode` (thesis appendix D.3, p.120-121).
/// A Muller/secant-type root finder used to solve the implicit node
/// equation g(x,z)=0 for x, given z, one sample at a time.
/// </summary>
public static class TraubSolver
{
    public delegate double GFunction(double x, double z);

    /// <summary>
    /// Solves g(x, <paramref name="z"/>) = 0 for x, starting from <paramref name="x"/>
    /// as the initial guess, and returns the root in <paramref name="x"/>.
    /// Pascal: traub_iteration(g, error_limit, var input, var for_x) — `input` (here:
    /// z) is never reassigned in the original body, so it is ported as pass-by-value.
    /// </summary>
    public static void TraubIteration(GFunction g, double errorLimit, double z, ref double x)
    {
        var forX = x;
        var nowY = g(forX, z);
        var nowX = forX * 1.1;
        double e;
        do
        {
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

            if (nowX != 0)
            {
                e = 1 - (forX / nowX);
            }
            else
            {
                e = forX == 0 ? 0 : 1;
            }
        } while (Math.Abs(e) >= errorLimit);

        x = nowX;
    }

    /// <summary>
    /// Solves g(x, z)=0 for every sample in <paramref name="main"/>, using each
    /// solution as the initial guess for the next (Pascal: Traub). Overwrites
    /// each element's Re (originally the generator current z) with the solved
    /// output voltage x.
    /// </summary>
    public static void Traub(GFunction g, double errorLimit, Complex[] main)
    {
        var output = main[1].Re * 1000;
        for (var i = 1; i <= FftProcessor.MaxElement; i++)
        {
            var z = main[i].Re;
            TraubIteration(g, errorLimit, z, ref output);
            main[i] = new Complex(output, main[i].Im);
        }
    }
}

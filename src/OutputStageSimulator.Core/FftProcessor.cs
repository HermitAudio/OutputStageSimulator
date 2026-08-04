using System.Numerics;

namespace OutputStageSimulator.Core;

/// <summary>
/// Port of module `fft_subroutine` (thesis appendix D.2, p.116-119).
/// A radix-2 decimation-in-time FFT hard-wired to 8 stages (256 points).
/// Arrays are 1-based (index 0 unused) to mirror the original Pascal `tabell`
/// layout and keep this port mechanically checkable against the thesis listing.
/// </summary>
public static class FftProcessor
{
    public const int MaxNumber = 8; // number of FFT stages, i.e. log2(MaxElement)
    public const int MaxElement = 256; // 2^MaxNumber, fixed transform size

    // The thesis source declares `pi = 3.1415927` (7 significant digits) rather
    // than a full-precision constant; kept verbatim (and exposed, since the main
    // program imports and reuses this same constant) to reproduce exact numerics.
    public const double Pi = 3.1415927;

    /// <summary>Bit-reverses <paramref name="x"/> using <paramref name="m"/> bits (Pascal: IBR).</summary>
    private static int Ibr(int x, int m)
    {
        var ny = 0;
        for (var i = 1; i <= m; i++)
        {
            var old = x;
            x /= 2;
            ny = 2 * (ny - x) + old;
        }

        return ny;
    }

    /// <summary>
    /// In-place FFT of <paramref name="main"/> (1-based, length &gt;= MaxElement+1).
    /// <paramref name="n"/> is always 256 in the original program; the parameter is
    /// kept only because the Pascal source exposed it.
    /// </summary>
    public static void Fft(Complex[] main, int n)
    {
        var powerOfTwo = new int[MaxNumber + 1];
        var bitReversed = new int[MaxElement + 1];
        var partOfAngle = new double[MaxNumber + 1];

        InitArrays(powerOfTwo, bitReversed, partOfAngle);

        for (var h = 1; h <= MaxNumber; h++)
        {
            double e = 0;
            var j = 1;
            var i = j;
            bool ok;
            do
            {
                var k = i + powerOfTwo[MaxNumber - h];
                ok = k > n;
                if (!ok)
                {
                    var twiddle = new Complex(Math.Cos(e), Math.Sin(e));
                    var help = twiddle * main[k];
                    var help2 = main[i];
                    main[i] = help2 + help;
                    main[k] = help2 - help;
                    i++;
                    ok = i > j * powerOfTwo[MaxNumber - h];
                    if (ok)
                    {
                        j += 2;
                        ok = j > powerOfTwo[h];
                        if (!ok)
                        {
                            i += powerOfTwo[MaxNumber - h];
                            var z = bitReversed[i - 1];
                            e = partOfAngle[h] * z;
                        }
                    }
                }
            } while (!ok);
        }

        Unscramble(main, n, powerOfTwo);
    }

    private static void InitArrays(int[] powerOfTwo, int[] bitReversed, double[] partOfAngle)
    {
        powerOfTwo[0] = 1;
        for (var i = 1; i <= MaxNumber; i++)
        {
            powerOfTwo[i] = 2 * powerOfTwo[i - 1];
            partOfAngle[i] = 2 * Pi / powerOfTwo[i];
        }

        for (var i = 0; i <= MaxElement; i++)
        {
            bitReversed[i] = Ibr(i, MaxNumber);
        }
    }

    /// <summary>Restores bit-reversed FFT output to natural order (Pascal: unscramble).</summary>
    private static void Unscramble(Complex[] main, int n, int[] powerOfTwo)
    {
        var i = 1;
        var j = 1;
        bool ok;
        do
        {
            if (!(i >= j))
            {
                var help = main[i];
                main[i] = main[j];
                main[j] = help;
            }

            var k = powerOfTwo[MaxNumber - 1];
            ok = k >= j;
            while (!ok)
            {
                j -= k;
                k /= 2;
                ok = k >= j;
            }

            j += k;
            i++;
            ok = i >= n;
        } while (!ok);
    }
}

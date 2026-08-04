using System.Numerics;

namespace OutputStageSimulator.Core;

/// <summary>
/// One entry of the harmonic table printed by Pascal `utskrift`, extended
/// with phase (not present in the original — the 1985 program only ever
/// printed magnitude/dB). Phase is relative to the same fixed time origin
/// (sample i=1) as every other harmonic, so differences between harmonics
/// are directly meaningful, e.g. to characterize whether a distortion
/// mechanism shifts harmonics in or out of phase with the fundamental.
/// </summary>
public sealed record HarmonicEntry(int HarmonicNumber, double Magnitude, double Db, double PhaseDeg);

/// <summary>Result of running the solve → FFT → normalize → analyze pipeline once.</summary>
public sealed class AnalysisResult
{
    public required IReadOnlyList<HarmonicEntry> Harmonics { get; init; }

    /// <summary>Total harmonic distortion in percent (Pascal: thd).</summary>
    public required double Thd { get; init; }

    /// <summary>Magnitude of the fundamental, i.e. main[2] (Pascal: grunntone).</summary>
    public required double Fundamental { get; init; }

    /// <summary>dB spectrum for bins 1..N/2 relative to the fundamental (Pascal: convert_to_db).</summary>
    public required IReadOnlyList<double> SpectrumDb { get; init; }

    /// <summary>Phase spectrum (degrees) for bins 1..N/2, parallel to <see cref="SpectrumDb"/>.</summary>
    public required IReadOnlyList<double> SpectrumPhaseDeg { get; init; }
}

/// <summary>
/// Port of the nonlinear push-pull output-stage model and analysis pipeline
/// from the thesis main program (p.124-130): functions `f` and `g`, plus the
/// solve/FFT/normalize/report sequence run by the 'R' menu command.
/// </summary>
public sealed class OutputStagePipeline
{
    public HfeModel Hfe { get; }

    /// <summary>Quiescent current per transistor pair (Pascal: Iq).</summary>
    public double Iq { get; set; }

    /// <summary>Mismatch factor between the two devices of a pair (Pascal: offset).</summary>
    public double Offset { get; set; } = 1.0;

    /// <summary>Generator (source) resistance (Pascal: Rg).</summary>
    public double Rg { get; set; }

    /// <summary>Load resistance (Pascal: Rl).</summary>
    public double Rl { get; set; }

    /// <summary>Number of transistors in parallel per side (Pascal: antpar).</summary>
    public int AntPar { get; set; } = 1;

    public OutputStagePipeline(HfeModel hfe) => Hfe = hfe;

    /// <summary>
    /// Base current for a complementary push-pull pair given output current x
    /// (Pascal: function f). Global Iq/offset become instance state here.
    /// </summary>
    public double BaseCurrent(double x)
    {
        var limit = 2 * Iq;
        double i1, i2;
        if (x >= limit)
        {
            i1 = x;
            i2 = 0;
        }
        else if (x <= -limit)
        {
            i2 = Math.Abs(x);
            i1 = 0;
        }
        else
        {
            i1 = x / 2 + Iq;
            i2 = Iq - x / 2;
        }

        var ib1 = i1 / Hfe.Hfe(i1);
        var ib2 = i2 / Hfe.Hfe(i2) * Offset;
        return ib1 - ib2;
    }

    /// <summary>
    /// Sum-function for all voltages at the base node; zero when x is the
    /// output voltage consistent with generator current z (Pascal: function g).
    /// </summary>
    public double G(double x, double z)
    {
        var ibias = AntPar * BaseCurrent(x / Rl / AntPar);
        return (ibias - z) * Rg + x;
    }

    /// <summary>
    /// Builds one full period of the generator-current test tone for a given
    /// peak output voltage (Pascal: the 'I' menu handler). Returns a 1-based,
    /// MaxElement+1-length buffer ready for <see cref="Analyze"/>.
    /// </summary>
    public Complex[] BuildTestTone(double peakOutputVoltage)
    {
        var iut = peakOutputVoltage / Rl;
        var ib = AntPar * BaseCurrent(iut / AntPar);
        var itopp = ib + peakOutputVoltage / Rg;

        var samples = new Complex[FftProcessor.MaxElement + 1];
        for (var i = 1; i <= FftProcessor.MaxElement; i++)
        {
            samples[i] = new Complex(itopp * Math.Sin(i / 128.0 * FftProcessor.Pi), 0.0);
        }

        return samples;
    }

    /// <summary>
    /// Runs Traub's method per sample, FFTs the result, normalizes, and
    /// reports the harmonic table / THD / dB spectrum (Pascal: the 'R' menu
    /// handler, i.e. Traub + fft + normalize + utskrift + convert_to_db).
    /// Mutates <paramref name="samples"/> in place, same as the original.
    /// </summary>
    public AnalysisResult Analyze(Complex[] samples, double errorLimit = 1e-6)
    {
        TraubSolver.Traub(G, errorLimit, samples);
        FftProcessor.Fft(samples, FftProcessor.MaxElement);
        Normalize(samples);

        var fundamental = Complex.Abs(samples[2]);

        var harmonics = new List<HarmonicEntry>(20);
        for (var i = 1; i <= 20; i++)
        {
            var magnitude = Complex.Abs(samples[i]);
            var db = 20 * Math.Log10(magnitude / fundamental);
            var phaseDeg = samples[i].Phase * (180.0 / Math.PI);
            harmonics.Add(new HarmonicEntry(i - 1, magnitude, db, phaseDeg));
        }

        var thdAccumulator = 0.0;
        for (var i = 3; i <= FftProcessor.MaxElement / 2; i++)
        {
            thdAccumulator += Complex.Abs(samples[i]).Sqr();
        }

        var thd = Math.Sqrt(thdAccumulator) / fundamental * 100;

        var spectrumDb = new double[FftProcessor.MaxElement / 2];
        var spectrumPhaseDeg = new double[FftProcessor.MaxElement / 2];
        for (var i = 1; i <= FftProcessor.MaxElement / 2; i++)
        {
            spectrumDb[i - 1] = 20 * Math.Log10(Complex.Abs(samples[i]) / fundamental);
            spectrumPhaseDeg[i - 1] = samples[i].Phase * (180.0 / Math.PI);
        }

        return new AnalysisResult
        {
            Harmonics = harmonics,
            Thd = thd,
            Fundamental = fundamental,
            SpectrumDb = spectrumDb,
            SpectrumPhaseDeg = spectrumPhaseDeg,
        };
    }

    private static void Normalize(Complex[] samples)
    {
        for (var i = 1; i <= FftProcessor.MaxElement; i++)
        {
            samples[i] = new Complex(samples[i].Real / 128, samples[i].Imaginary / 128);
        }
    }
}

using OutputStageSimulator.Core;

namespace OutputStageSimulator.Core.Tests;

[TestFixture]
public class FftProcessorTests
{
    [Test]
    public void Fft_OfConstantSignal_ConcentratesEnergyAtDcBin()
    {
        const double dc = 3.0;
        var samples = new Complex[FftProcessor.MaxElement + 1];
        for (var i = 1; i <= FftProcessor.MaxElement; i++)
        {
            samples[i] = new Complex(dc, 0.0);
        }

        FftProcessor.Fft(samples, FftProcessor.MaxElement);

        // DC bin has no mirror partner, so it scales by N rather than N/2.
        Assert.That(Complex.Mag(samples[1]) / FftProcessor.MaxElement, Is.EqualTo(dc).Within(1e-9));
        for (var i = 2; i <= FftProcessor.MaxElement; i++)
        {
            Assert.That(Complex.Mag(samples[i]), Is.LessThan(1e-9), $"bin {i} should be ~0 for a DC signal");
        }
    }

    [Test]
    public void Fft_OfPureFundamentalSine_ConcentratesEnergyAtBinTwo()
    {
        const double amplitude = 5.0;
        var samples = new Complex[FftProcessor.MaxElement + 1];
        for (var i = 1; i <= FftProcessor.MaxElement; i++)
        {
            samples[i] = new Complex(amplitude * Math.Sin(i / 128.0 * FftProcessor.Pi), 0.0);
        }

        FftProcessor.Fft(samples, FftProcessor.MaxElement);

        // Bin 2 is the fundamental (one cycle per 256-sample block); normalize by N/2.
        var fundamentalMag = Complex.Mag(samples[2]) / 128.0;
        Assert.That(fundamentalMag, Is.EqualTo(amplitude).Within(1e-6));

        // Threshold relaxed vs. exact zero: the source's truncated `pi = 3.1415927`
        // constant means sin(2*pi) isn't exactly 0, leaking a tiny residual into
        // neighboring bins (observed ~1e-6, vs. a raw bin scale of amplitude*N/2=640).
        Assert.That(Complex.Mag(samples[1]), Is.LessThan(1e-3), "DC bin should be ~0 for a pure sine");
        for (var i = 3; i <= FftProcessor.MaxElement / 2; i++)
        {
            Assert.That(Complex.Mag(samples[i]) / 128.0, Is.LessThan(1e-6), $"bin {i} should be ~0 for a pure fundamental sine");
        }
    }

    [Test]
    public void Fft_OfSecondHarmonicSine_ConcentratesEnergyAtBinThree()
    {
        const double amplitude = 2.0;
        var samples = new Complex[FftProcessor.MaxElement + 1];
        for (var i = 1; i <= FftProcessor.MaxElement; i++)
        {
            // Two full cycles per 256-sample block == bin 3 (2nd harmonic).
            samples[i] = new Complex(amplitude * Math.Sin(2 * i / 128.0 * FftProcessor.Pi), 0.0);
        }

        FftProcessor.Fft(samples, FftProcessor.MaxElement);

        var secondHarmonicMag = Complex.Mag(samples[3]) / 128.0;
        Assert.That(secondHarmonicMag, Is.EqualTo(amplitude).Within(1e-6));

        // See note in the fundamental-sine test above re: truncated-pi leakage.
        Assert.That(Complex.Mag(samples[2]), Is.LessThan(1e-3), "fundamental bin should be ~0");
    }
}

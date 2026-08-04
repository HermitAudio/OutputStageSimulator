using OutputStageSimulator.Core;
using System.Numerics;

namespace OutputStageSimulator.Core.Tests;

/// <summary>
/// Verifies the raw FFT (independent of the amplifier/Traub model) against
/// the textbook closed-form spectra of an ideal square wave and an ideal
/// triangle wave. Both are standard, symmetric (half-wave-symmetric)
/// waveforms with well-known harmonic content:
///   - Square wave: only odd harmonics, amplitude ~ 1/n relative to the
///     fundamental (per Wikipedia's "Square wave (waveform)" article).
///   - Triangle wave: only odd harmonics, amplitude ~ 1/n^2 relative to the
///     fundamental (per Wikipedia's "Triangle wave" article).
/// If FftProcessor had a bug that shaped the spectrum's envelope (the kind
/// of artifact suspected from an odd-looking envelope in one of the thesis
/// plots), a clean, closed-form input like this would expose it directly,
/// without any of the amplifier nonlinearity in the way.
/// </summary>
[TestFixture]
public class SquareTriangleWaveFftTests
{
    private const int N = FftProcessor.MaxElement;

    /// <summary>50% duty square wave, one period over N samples, peak amplitude A.</summary>
    private static Complex[] BuildSquareWave(double amplitude)
    {
        var samples = new Complex[N + 1];
        for (var i = 1; i <= N; i++)
        {
            samples[i] = new Complex(i <= N / 2 ? amplitude : -amplitude, 0.0);
        }

        return samples;
    }

    /// <summary>
    /// Symmetric triangle wave, one period over N samples, peak amplitude A,
    /// in the same sine phase convention as the Fourier series quoted in the
    /// test names below: zero at t=0, rising (matches sin(theta), not
    /// cos(theta) — a wave shaped like -cos would still have the right
    /// magnitude spectrum but every harmonic's phase would come out rotated
    /// by a multiple of 90 degrees, which would corrupt the relative-phase
    /// comparisons between harmonics).
    /// </summary>
    private static Complex[] BuildTriangleWave(double amplitude)
    {
        var samples = new Complex[N + 1];
        for (var i = 1; i <= N; i++)
        {
            var t = (i - 1) / (double)N;
            double value;
            if (t < 0.25)
            {
                value = 4 * t;
            }
            else if (t < 0.75)
            {
                value = 2 - 4 * t;
            }
            else
            {
                value = 4 * t - 4;
            }

            samples[i] = new Complex(amplitude * value, 0.0);
        }

        return samples;
    }

    private static double NormalizedMagnitude(Complex[] samples, int bin) => Complex.Abs(samples[bin]) / (N / 2.0);

    private static double PhaseDeg(Complex[] samples, int bin) => samples[bin].Phase * (180.0 / Math.PI);

    /// <summary>Signed phase difference in (-180, 180], independent of absolute phase convention.</summary>
    private static double WrappedPhaseDifferenceDeg(double aDeg, double bDeg)
    {
        var diff = (aDeg - bDeg) % 360.0;
        if (diff > 180.0)
        {
            diff -= 360.0;
        }
        else if (diff <= -180.0)
        {
            diff += 360.0;
        }

        return diff;
    }

    [Test]
    public void Fft_OfSquareWave_HasOnlyOddHarmonicsDecayingAsOneOverN()
    {
        var samples = BuildSquareWave(amplitude: 3.0);
        FftProcessor.Fft(samples, N);

        var fundamental = NormalizedMagnitude(samples, 2);

        for (var n = 3; n <= 21; n += 2)
        {
            var ratio = NormalizedMagnitude(samples, n + 1) / fundamental;
            Assert.That(ratio, Is.EqualTo(1.0 / n).Within(1e-3),
                $"square wave harmonic {n} should have relative amplitude ~1/{n}");
        }

        for (var n = 2; n <= 20; n += 2)
        {
            Assert.That(NormalizedMagnitude(samples, n + 1) / fundamental, Is.LessThan(1e-6),
                $"square wave harmonic {n} (even) should be ~0");
        }
    }

    [Test]
    public void Fft_OfTriangleWave_HasOnlyOddHarmonicsDecayingAsOneOverNSquared()
    {
        var samples = BuildTriangleWave(amplitude: 3.0);
        FftProcessor.Fft(samples, N);

        var fundamental = NormalizedMagnitude(samples, 2);

        for (var n = 3; n <= 21; n += 2)
        {
            var ratio = NormalizedMagnitude(samples, n + 1) / fundamental;
            Assert.That(ratio, Is.EqualTo(1.0 / (n * n)).Within(1e-3),
                $"triangle wave harmonic {n} should have relative amplitude ~1/{n * n}");
        }

        for (var n = 2; n <= 20; n += 2)
        {
            Assert.That(NormalizedMagnitude(samples, n + 1) / fundamental, Is.LessThan(1e-6),
                $"triangle wave harmonic {n} (even) should be ~0");
        }
    }

    /// <summary>
    /// Square wave Fourier series: (4A/pi) * sum_{k odd} sin(k*theta)/k — in
    /// continuous time, every term has the same positive coefficient sign, so
    /// every odd harmonic is exactly in phase with the fundamental. But
    /// <see cref="BuildSquareWave"/>'s transition (+A for the first half of
    /// the block, -A for the second) falls exactly *between* samples 128 and
    /// 129 (and between 256 and the next period's 1), not on one — an
    /// unavoidable consequence of any evenly-split discrete rectangular wave,
    /// since there's no discontinuity-free way to land a sample exactly on a
    /// jump. That is a real half-sample (0.5-sample) shift relative to the
    /// continuous-time formula, and the DFT shift theorem says a shift of d
    /// samples adds a phase of -360*k*d/N degrees to harmonic k; relative to
    /// the fundamental, harmonic n therefore picks up an extra
    /// -(n-1)*180/N degrees on top of the "in phase" baseline. This is
    /// exactly what's asserted below — a clean, exact linear relationship,
    /// which is itself a strong correctness signal (a buggy FFT would not
    /// reproduce the shift theorem's prediction to a fraction of a degree).
    /// </summary>
    [Test]
    public void Fft_OfSquareWave_OddHarmonicPhasesMatchTheHalfSampleShiftTheorem()
    {
        var samples = BuildSquareWave(amplitude: 3.0);
        FftProcessor.Fft(samples, N);

        var fundamentalPhase = PhaseDeg(samples, 2);

        for (var n = 3; n <= 21; n += 2)
        {
            var diff = WrappedPhaseDifferenceDeg(PhaseDeg(samples, n + 1), fundamentalPhase);
            var expected = -(n - 1) * 180.0 / N;
            Assert.That(diff, Is.EqualTo(expected).Within(0.01),
                $"square wave harmonic {n} should be shifted {expected:F4} deg from the fundamental by the half-sample shift theorem");
        }
    }

    /// <summary>
    /// Triangle wave Fourier series: (8A/pi^2) * sum_{k odd} (-1)^((k-1)/2) *
    /// sin(k*theta)/k^2 — the sign flips every other odd harmonic, so harmonics
    /// 1,5,9,... share one phase and 3,7,11,... sit 180 degrees from it.
    /// </summary>
    [Test]
    public void Fft_OfTriangleWave_OddHarmonicsAlternatePhaseEveryOtherOrder()
    {
        var samples = BuildTriangleWave(amplitude: 3.0);
        FftProcessor.Fft(samples, N);

        var fundamentalPhase = PhaseDeg(samples, 2);

        for (var n = 3; n <= 21; n += 2)
        {
            var diff = WrappedPhaseDifferenceDeg(PhaseDeg(samples, n + 1), fundamentalPhase);
            var expected = (n - 1) / 2 % 2 == 0 ? 0.0 : 180.0;
            Assert.That(Math.Abs(diff), Is.EqualTo(expected).Within(1.0),
                $"triangle wave harmonic {n} should be {(expected == 0.0 ? "in phase" : "180 degrees out of phase")} with the fundamental");
        }
    }
}

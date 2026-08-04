namespace OutputStageSimulator.Core;

/// <summary>
/// A transistor type's reference hFE-vs-Ic characteristic, for comparing
/// against the model's calculated curve in the UI. Not used by the analysis
/// pipeline itself — <see cref="HfeModel"/> already carries whatever
/// Hfemax/Imax/etc. the simulation needs; this is purely the "known real
/// device" curve to overlay against it.
/// </summary>
public sealed record TransistorProfile(
    string Name,
    string Description,
    IReadOnlyList<(double Ic, double Hfe)> ReferenceHfeCurve);

/// <summary>
/// Known transistor reference curves. Currently just BD203/204 — add more
/// entries here as additional datasheets are digitized.
/// </summary>
public static class TransistorProfiles
{
    /// <summary>
    /// Digitized by eye from the original Philips "Low-Frequency Power
    /// Transistors" databook (1972/1979), BD201/BD203 datasheet, the
    /// "typical behaviour of D.C. current gain versus collector current"
    /// graph (VCE=2V, Tj=25C). Points are approximate (read off a scanned
    /// log-x/linear-y chart, not extracted from underlying data) — good
    /// enough for a shape comparison, not for precision calibration.
    ///
    /// This is the *single device's* hFE, peaking around 75-80. The
    /// simulation's Hfemax (~7500) represents a compound driver+output
    /// Darlington gain, roughly two orders of magnitude higher — see
    /// <see cref="HfeModel"/>. The two curves are intentionally plotted on
    /// separate axes in the UI rather than scaled to match, since the
    /// driver transistor's own curve (not yet digitized) is what actually
    /// belongs on the "compound" side of a real overlay.
    /// </summary>
    public static readonly TransistorProfile Bd203204 = new(
        Name: "BD203/204",
        Description: "Single-device hFE (VCE=2V, Tj=25C), Philips 1972 databook",
        ReferenceHfeCurve: new (double Ic, double Hfe)[]
        {
            (0.01, 22), (0.015, 27), (0.02, 30), (0.03, 35), (0.05, 40),
            (0.07, 44), (0.10, 47), (0.15, 52), (0.20, 55), (0.30, 60),
            (0.50, 67), (0.70, 72), (1.00, 76), (1.50, 76), (2.00, 73),
            (3.00, 65), (4.00, 55), (5.00, 45), (6.00, 35), (7.00, 27), (8.00, 20),
        });

    public static readonly IReadOnlyList<TransistorProfile> All = new[] { Bd203204 };
}

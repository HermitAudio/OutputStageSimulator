namespace OutputStageSimulator.Core;

/// <summary>
/// Which formula <see cref="HfeModel.Hfe"/> uses to combine the low/mid-current
/// log-shaped rise and the high-current linear derating. Both share the same
/// five calibration parameters (<see cref="HfeModel.Hfemax"/>, <see cref="HfeModel.Imax"/>,
/// <see cref="HfeModel.AFactor"/>, <see cref="HfeModel.DI"/>, <see cref="HfeModel.Iturnover"/>).
/// </summary>
public enum HfeCurveKind
{
    /// <summary>
    /// The thesis's original formula: a hard switch between the two branches
    /// at Iturnover. The value is continuous there by construction, but the
    /// slope isn't — a real kink, which injects extra harmonic content into
    /// anything driven through it (the same reason a triangle wave's slope
    /// discontinuities give it a slower-decaying spectrum than a sine wave).
    /// </summary>
    Piecewise,

    /// <summary>
    /// Same two branch formulas, but smoothly blended across Iturnover with a
    /// tanh weight instead of switched — the same principle SPICE's Gummel-Poon
    /// BJT model uses (a smooth qb "base charge" blend) to avoid a beta-rolloff
    /// kink at the high-current knee, applied directly to this curve instead of
    /// requiring a Vbe-based model. No new parameters: matches the piecewise
    /// curve closely away from Iturnover, and removes the slope discontinuity
    /// right around it.
    /// </summary>
    SmoothBlend,
}

/// <summary>
/// Port of the transistor current-gain (hfe) model from the thesis main
/// program (p.125-126, function `hfe`): flat/log-shaped gain below
/// <see cref="Iturnover"/>, linear derating (<see cref="DI"/>) above it.
/// </summary>
public sealed class HfeModel
{
    public double Hfemax { get; set; }
    public double Imax { get; set; }
    public double AFactor { get; set; }
    public double DI { get; set; }
    public double Iturnover { get; set; }

    /// <summary>Which curve formula <see cref="Hfe"/> uses. Defaults to the thesis's original.</summary>
    public HfeCurveKind CurveKind { get; set; } = HfeCurveKind.Piecewise;

    /// <summary>
    /// How wide the <see cref="HfeCurveKind.SmoothBlend"/> transition region is,
    /// as a fraction of <see cref="Iturnover"/>. Smaller = closer to the sharp
    /// piecewise switch; larger = more gradual.
    /// </summary>
    public double SmoothBlendTransitionFraction { get; set; } = 0.15;

    /// <summary>
    /// Hfe evaluated at <see cref="Iturnover"/>. Pascal computes this once
    /// (`Hfe_at_turnover := hfe(Iturnover);` in `hfe_parm`) rather than on every
    /// call; call <see cref="RecomputeTurnoverGain"/> after changing parameters.
    /// Only used by <see cref="HfeCurveKind.Piecewise"/> — SmoothBlend computes
    /// its own anchor value internally to avoid depending on this being fresh.
    /// </summary>
    public double HfeAtTurnover { get; private set; }

    public void RecomputeTurnoverGain() => HfeAtTurnover = LogShaped(Iturnover);

    public double Hfe(double i) => CurveKind switch
    {
        HfeCurveKind.SmoothBlend => SmoothBlend(i),
        _ => Piecewise(i),
    };

    /// <summary>The low/mid-current branch: a log-shaped peak centered near Imax.</summary>
    private double LogShaped(double i) =>
        Math.Abs(i) > 0 ? Hfemax / (1 + AFactor * Math.Log10(Math.Abs(i) / Imax).Sqr()) : Hfemax;

    /// <summary>The high-current branch: linear derating anchored at the log-shaped value at Iturnover.</summary>
    private double LinearDerated(double i) => LogShaped(Iturnover) - (Math.Abs(i) - Iturnover) * DI;

    private double Piecewise(double i) => i > Iturnover ? HfeAtTurnover - (Math.Abs(i) - Iturnover) * DI : LogShaped(i);

    private double SmoothBlend(double i)
    {
        if (Math.Abs(i) == 0)
        {
            return Hfemax;
        }

        var transitionWidth = Iturnover * SmoothBlendTransitionFraction;
        var weight = 0.5 * (1 + Math.Tanh((i - Iturnover) / transitionWidth));
        return (1 - weight) * LogShaped(i) + weight * LinearDerated(i);
    }
}

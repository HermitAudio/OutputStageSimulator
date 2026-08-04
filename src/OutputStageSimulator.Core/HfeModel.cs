namespace OutputStageSimulator.Core;

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

    /// <summary>
    /// Hfe evaluated at <see cref="Iturnover"/>. Pascal computes this once
    /// (`Hfe_at_turnover := hfe(Iturnover);` in `hfe_parm`) rather than on every
    /// call; call <see cref="RecomputeTurnoverGain"/> after changing parameters.
    /// </summary>
    public double HfeAtTurnover { get; private set; }

    public void RecomputeTurnoverGain() => HfeAtTurnover = Hfe(Iturnover);

    public double Hfe(double i)
    {
        if (i > Iturnover)
        {
            return HfeAtTurnover - (Math.Abs(i) - Iturnover) * DI;
        }

        if (Math.Abs(i) > 0)
        {
            return Hfemax / (1 + AFactor * Math.Log10(i / Imax).Sqr());
        }

        return Hfemax;
    }
}

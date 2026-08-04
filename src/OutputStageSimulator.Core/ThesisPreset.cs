namespace OutputStageSimulator.Core;

/// <summary>
/// One parameter set transcribed from a thesis figure (BD203/204 push-pull
/// stage, plus a peak test-tone voltage), with the figure's reported results
/// where known. Shared by the console harness, the Blazor preset dropdown,
/// and the regression tests, so a new figure only needs to be added once.
/// </summary>
public sealed record ThesisPreset(
    string Title,
    string Description,
    double Rg,
    double Rl,
    double Iq,
    double Offset,
    int AntPar,
    double Hfemax,
    double Imax,
    double AFactor,
    double DI,
    double Iturnover,
    double PeakOutputVoltage,
    double? ExpectedGrunntone = null,
    double? ExpectedThd = null,
    double? ExpectedDb2nd = null,
    double? ExpectedDb3rd = null,
    double? ExpectedDb4th = null,
    double? ExpectedDb5th = null)
{
    public HfeModel CreateHfeModel()
    {
        var model = new HfeModel
        {
            Hfemax = Hfemax,
            Imax = Imax,
            AFactor = AFactor,
            DI = DI,
            Iturnover = Iturnover,
        };
        model.RecomputeTurnoverGain();
        return model;
    }

    public OutputStagePipeline CreatePipeline() => new(CreateHfeModel())
    {
        Rg = Rg,
        Rl = Rl,
        Iq = Iq,
        Offset = Offset,
        AntPar = AntPar,
    };
}

/// <summary>
/// Reference configurations transcribed from the thesis's own figures
/// (BD203/204 push-pull stage). The thesis presents 11 such figures in
/// total; only the ones transcribed so far are listed here.
/// </summary>
public static class ThesisPresets
{
    public const int KnownFigureCount = 11;

    /// <summary>
    /// Thesis-exact parameters (including the original hfe model values) —
    /// used by the regression tests and the console harness to keep checking
    /// against the thesis's own published figures. The console app and web UI
    /// use <see cref="AllUpdated"/> instead.
    /// </summary>
    public static readonly IReadOnlyList<ThesisPreset> All = new[]
    {
        new ThesisPreset(
            Title: "Fig. 5.14.1 - Stromstyrt kl.AB",
            Description: "BD203/204, current-driven, class AB (Iq=0.8A)",
            Rg: 300000.0, Rl: 4.0, Iq: 0.8, Offset: 1.0, AntPar: 1,
            Hfemax: 7500, Imax: 1.10, AFactor: 0.4000, DI: 1000, Iturnover: 2.00,
            PeakOutputVoltage: 14.0,
            ExpectedGrunntone: 14.84, ExpectedThd: 6.7333,
            ExpectedDb2nd: -155.33, ExpectedDb3rd: -23.56, ExpectedDb4th: -159.65, ExpectedDb5th: -39.21),

        new ThesisPreset(
            Title: "Fig. 5.14.4 - Stromstyrt kl.B",
            Description: "BD203/204, current-driven, class B (Iq=0)",
            Rg: 300000.0, Rl: 4.0, Iq: 0.0, Offset: 1.0, AntPar: 1,
            Hfemax: 7500, Imax: 1.10, AFactor: 0.4000, DI: 1000, Iturnover: 2.00,
            PeakOutputVoltage: 14.0,
            ExpectedGrunntone: 14.83, ExpectedThd: 6.5062,
            ExpectedDb2nd: -155.19, ExpectedDb3rd: -23.83, ExpectedDb4th: -159.32, ExpectedDb5th: -41.98),

        new ThesisPreset(
            Title: "Fig. 5.14.5 - Spenningsstyrt kl.B",
            Description: "BD203/204, voltage-driven, class B (Iq=0)",
            Rg: 3000.0, Rl: 4.0, Iq: 0.0, Offset: 1.0, AntPar: 1,
            Hfemax: 7500, Imax: 1.10, AFactor: 0.4000, DI: 1000, Iturnover: 2.00,
            PeakOutputVoltage: 14.0,
            ExpectedGrunntone: 14.11, ExpectedThd: 0.8046,
            ExpectedDb2nd: -154.23, ExpectedDb3rd: -41.94, ExpectedDb4th: -161.57, ExpectedDb5th: -66.26),

        new ThesisPreset(
            Title: "Fig. 5.14.6 - Stromstyrt kl.A",
            Description: "BD203/204, current-driven, class A (Iq=4.0A)",
            Rg: 300000.0, Rl: 4.0, Iq: 4.0, Offset: 1.0, AntPar: 1,
            Hfemax: 7500, Imax: 1.10, AFactor: 0.4000, DI: 1000, Iturnover: 2.00,
            PeakOutputVoltage: 14.0,
            ExpectedGrunntone: 14.37, ExpectedThd: 2.7039,
            ExpectedDb2nd: -154.55, ExpectedDb3rd: -31.37, ExpectedDb4th: -160.77, ExpectedDb5th: -56.99),
    };

    /// <summary>
    /// Same circuit configurations as <see cref="All"/> (Rg/Rl/Iq/Offset/AntPar/
    /// PeakOutputVoltage are still thesis-exact), but with the hfe model
    /// parameters recalibrated against the real BD203/204 datasheet curve
    /// (see the hFE overlay chart) instead of the thesis's own values. This is
    /// what the console app and web UI use going forward; <see cref="All"/>
    /// stays untouched — and without Expected* values here, since this
    /// calibration is deliberately not the thesis's own numbers — so the
    /// regression tests keep checking against the thesis's actual figures.
    /// </summary>
    public static readonly IReadOnlyList<ThesisPreset> AllUpdated =
    [
        new(
            Title: "Fig. 5.14.1 - Stromstyrt kl.AB",
            Description: "BD203/204, current-driven, class AB (Iq=0.8A)",
            Rg: 300000.0, Rl: 4.0, Iq: 0.8, Offset: 1.0, AntPar: 1,
            Hfemax: 7500, Imax: 1.25, AFactor: 0.35000, DI: 950, Iturnover: 2.00,
            PeakOutputVoltage: 14.0),

        new(
            Title: "Fig. 5.14.4 - Stromstyrt kl.B",
            Description: "BD203/204, current-driven, class B (Iq=0)",
            Rg: 300000.0, Rl: 4.0, Iq: 0.0, Offset: 1.0, AntPar: 1,
            Hfemax: 7500, Imax: 1.25, AFactor: 0.35000, DI: 950, Iturnover: 2.00,
            PeakOutputVoltage: 14.0),

        new(
            Title: "Fig. 5.14.5 - Spenningsstyrt kl.B",
            Description: "BD203/204, voltage-driven, class B (Iq=0)",
            Rg: 3000.0, Rl: 4.0, Iq: 0.0, Offset: 1.0, AntPar: 1,
            Hfemax: 7500, Imax: 1.25, AFactor: 0.35000, DI: 950, Iturnover: 2.00,
            PeakOutputVoltage: 14.0),

        new(
            Title: "Fig. 5.14.6 - Stromstyrt kl.A",
            Description: "BD203/204, current-driven, class A (Iq=4.0A)",
            Rg: 300000.0, Rl: 4.0, Iq: 4.0, Offset: 1.0, AntPar: 1,
            Hfemax: 7500, Imax: 1.25, AFactor: 0.35000, DI: 950, Iturnover: 2.00,
            PeakOutputVoltage: 14.0),
    ];
}

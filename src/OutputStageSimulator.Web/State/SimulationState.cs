using OutputStageSimulator.Core;

namespace OutputStageSimulator.Web.State;

/// <summary>
/// Scoped (per-circuit) shared state for the simulation form/results, so
/// the main page and the hFE zoom page stay in sync when navigating
/// between them within the same session.
/// </summary>
public sealed class SimulationState
{
    private readonly TransistorHfeService _hfeService;

    public SimulationState(TransistorHfeService hfeService)
    {
        _hfeService = hfeService;
        LoadPreset(0);
    }

    public int SelectedPresetIndex { get; set; }
    public int SelectedTransistorIndex { get; set; }
    public bool AutoRecalculate { get; set; } = true;

    public string Title { get; set; } = "";
    public double Rg { get; set; }
    public double Rl { get; set; }
    public double Iq { get; set; }
    public double Offset { get; set; }
    public int AntPar { get; set; }
    public double Hfemax { get; set; }
    public double Imax { get; set; }
    public double AFactor { get; set; }
    public double DI { get; set; }
    public double Iturnover { get; set; }
    public double PeakOutputVoltage { get; set; }
    public HfeCurveKind HfeCurveKind { get; set; } = HfeCurveKind.Piecewise;

    public AnalysisResult? Result { get; private set; }
    public string? Error { get; private set; }

    /// <summary>
    /// Applies a thesis preset's parameters (circuit + hfe model) and
    /// recalculates. Uses <see cref="ThesisPresets.AllUpdated"/> — the
    /// circuit values are still thesis-exact, but the hfe model parameters
    /// are calibrated against the real datasheet curve rather than the
    /// thesis's own numbers (see <see cref="ThesisPresets.All"/> for those,
    /// which is what the regression tests check against instead).
    /// </summary>
    public void LoadPreset(int index)
    {
        SelectedPresetIndex = index;
        if (index < 0 || index >= ThesisPresets.AllUpdated.Count)
        {
            Run();
            return;
        }

        var preset = ThesisPresets.AllUpdated[index];
        Title = preset.Title;
        Rg = preset.Rg;
        Rl = preset.Rl;
        Iq = preset.Iq;
        Offset = preset.Offset;
        AntPar = preset.AntPar;
        Hfemax = preset.Hfemax;
        Imax = preset.Imax;
        AFactor = preset.AFactor;
        DI = preset.DI;
        Iturnover = preset.Iturnover;
        PeakOutputVoltage = preset.PeakOutputVoltage;
        Run();
    }

    /// <summary>
    /// Loads the saved/calibrated hFE model for the selected transistor type
    /// (from the JSON store), overriding whatever a thesis preset set — this
    /// intentionally diverges from exact thesis reproduction in favor of the
    /// best-known real-world calibration, so the preset selector is reset to
    /// "Custom".
    /// </summary>
    public void OnTransistorTypeChanged(int index)
    {
        SelectedTransistorIndex = index;
        var name = TransistorProfiles.All[index].Name;
        if (_hfeService.TryGet(name, out var saved))
        {
            SelectedPresetIndex = -1;
            Hfemax = saved.Hfemax;
            Imax = saved.Imax;
            AFactor = saved.AFactor;
            DI = saved.DI;
            Iturnover = saved.Iturnover;
        }

        Run();
    }

    public void SaveCurrentHfeParameters()
    {
        var name = TransistorProfiles.All[SelectedTransistorIndex].Name;
        _hfeService.Save(name, new TransistorHfeParameters(Hfemax, Imax, AFactor, DI, Iturnover));
    }

    public void OnFieldChanged()
    {
        if (AutoRecalculate)
        {
            Run();
        }
    }

    public void Run()
    {
        Error = null;
        try
        {
            var hfe = new HfeModel
            {
                Hfemax = Hfemax,
                Imax = Imax,
                AFactor = AFactor,
                DI = DI,
                Iturnover = Iturnover,
                CurveKind = HfeCurveKind,
            };
            hfe.RecomputeTurnoverGain();

            var pipeline = new OutputStagePipeline(hfe)
            {
                Rg = Rg,
                Rl = Rl,
                Iq = Iq,
                Offset = Offset,
                AntPar = AntPar,
            };

            var samples = pipeline.BuildTestTone(PeakOutputVoltage);
            Result = pipeline.Analyze(samples);
        }
        catch (Exception ex)
        {
            Result = null;
            Error = $"Analysis failed: {ex.Message}";
        }
    }

    public HfeModel CreateHfeModel()
    {
        var model = new HfeModel { Hfemax = Hfemax, Imax = Imax, AFactor = AFactor, DI = DI, Iturnover = Iturnover, CurveKind = HfeCurveKind };
        model.RecomputeTurnoverGain();
        return model;
    }
}

using System.Text.Json;

namespace ThesisProgram.Core;

/// <summary>
/// A saved/calibrated hFE model parameter set for one transistor type,
/// independent of any particular thesis figure's circuit setup (Rg/Rl/Iq).
/// </summary>
public sealed record TransistorHfeParameters(double Hfemax, double Imax, double AFactor, double DI, double Iturnover)
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
}

/// <summary>
/// Loads/saves a dictionary of <see cref="TransistorHfeParameters"/> keyed
/// by transistor name, to/from a JSON file. Used to persist calibration
/// adjustments (made against the real datasheet reference curve) between
/// runs, separately from the hardcoded, thesis-exact <see cref="ThesisPresets"/>.
/// </summary>
public static class TransistorHfeStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public static Dictionary<string, TransistorHfeParameters> Load(string path)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, TransistorHfeParameters>();
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, TransistorHfeParameters>>(json, JsonOptions)
            ?? new Dictionary<string, TransistorHfeParameters>();
    }

    public static void Save(string path, Dictionary<string, TransistorHfeParameters> values)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(values, JsonOptions);
        File.WriteAllText(path, json);
    }
}

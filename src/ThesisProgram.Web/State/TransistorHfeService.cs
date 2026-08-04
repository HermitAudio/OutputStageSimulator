using ThesisProgram.Core;

namespace ThesisProgram.Web.State;

/// <summary>
/// Singleton wrapper around <see cref="TransistorHfeStore"/> bound to this
/// app's actual JSON file on disk (the output-directory copy of
/// Data/transistor-hfe-models.json, not the source-controlled one).
/// </summary>
public sealed class TransistorHfeService
{
    private readonly string _path;
    private readonly object _lock = new();
    private Dictionary<string, TransistorHfeParameters> _values;

    public TransistorHfeService(IWebHostEnvironment env)
    {
        _path = Path.Combine(env.ContentRootPath, "Data", "transistor-hfe-models.json");
        _values = TransistorHfeStore.Load(_path);
    }

    public bool TryGet(string name, out TransistorHfeParameters parameters)
    {
        lock (_lock)
        {
            return _values.TryGetValue(name, out parameters!);
        }
    }

    public void Save(string name, TransistorHfeParameters parameters)
    {
        lock (_lock)
        {
            _values[name] = parameters;
            TransistorHfeStore.Save(_path, _values);
        }
    }
}

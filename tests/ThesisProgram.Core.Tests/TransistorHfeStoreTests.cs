using ThesisProgram.Core;

namespace ThesisProgram.Core.Tests;

[TestFixture]
public class TransistorHfeStoreTests
{
    private string _tempPath = "";

    [SetUp]
    public void SetUp()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"hfe-store-test-{Guid.NewGuid():N}.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_tempPath))
        {
            File.Delete(_tempPath);
        }
    }

    [Test]
    public void Load_OfMissingFile_ReturnsEmptyDictionary()
    {
        var result = TransistorHfeStore.Load(_tempPath);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SaveThenLoad_RoundTripsValuesExactly()
    {
        var values = new Dictionary<string, TransistorHfeParameters>
        {
            ["BD203/204"] = new TransistorHfeParameters(Hfemax: 7500, Imax: 1.25, AFactor: 0.4, DI: 900, Iturnover: 2.0),
        };

        TransistorHfeStore.Save(_tempPath, values);
        var loaded = TransistorHfeStore.Load(_tempPath);

        Assert.That(loaded, Has.Count.EqualTo(1));
        Assert.That(loaded["BD203/204"], Is.EqualTo(values["BD203/204"]));
    }

    [Test]
    public void Save_CreatesMissingDirectory()
    {
        var nestedPath = Path.Combine(Path.GetTempPath(), $"hfe-store-test-dir-{Guid.NewGuid():N}", "models.json");
        try
        {
            TransistorHfeStore.Save(nestedPath, new Dictionary<string, TransistorHfeParameters>
            {
                ["X"] = new TransistorHfeParameters(1, 2, 3, 4, 5),
            });

            Assert.That(File.Exists(nestedPath), Is.True);
        }
        finally
        {
            var dir = Path.GetDirectoryName(nestedPath)!;
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Test]
    public void CreateHfeModel_ProducesModelWithMatchingParameters()
    {
        var parameters = new TransistorHfeParameters(Hfemax: 7500, Imax: 1.25, AFactor: 0.4, DI: 900, Iturnover: 2.0);
        var model = parameters.CreateHfeModel();

        Assert.That(model.Hfemax, Is.EqualTo(parameters.Hfemax));
        Assert.That(model.Imax, Is.EqualTo(parameters.Imax));
        Assert.That(model.AFactor, Is.EqualTo(parameters.AFactor));
        Assert.That(model.DI, Is.EqualTo(parameters.DI));
        Assert.That(model.Iturnover, Is.EqualTo(parameters.Iturnover));

        // RecomputeTurnoverGain should already have been called.
        Assert.That(model.HfeAtTurnover, Is.EqualTo(model.Hfe(parameters.Iturnover)));
    }
}

using OutputStageSimulator.Core;

// Validation harness for the Core library port of the thesis output-stage
// analysis program. Reproduces every transcribed thesis figure (see
// ThesisPresets in Core) and prints computed vs. thesis-reported THD and
// 2nd-5th harmonic levels side by side.

foreach (var preset in ThesisPresets.All)
{
    var pipeline = preset.CreatePipeline();
    var result = pipeline.Analyze(pipeline.BuildTestTone(preset.PeakOutputVoltage));

    var h2 = result.Harmonics.Single(h => h.HarmonicNumber == 2);
    var h3 = result.Harmonics.Single(h => h.HarmonicNumber == 3);
    var h4 = result.Harmonics.Single(h => h.HarmonicNumber == 4);
    var h5 = result.Harmonics.Single(h => h.HarmonicNumber == 5);

    Console.WriteLine($"{preset.Title} - {preset.Description}");
    Console.WriteLine(new string('=', preset.Title.Length + preset.Description.Length + 3));
    Console.WriteLine($"Rg={preset.Rg} ohm  Rl={preset.Rl} ohm  Iq={preset.Iq} A  Uut={preset.PeakOutputVoltage} V (peak)");
    Console.WriteLine();
    Console.WriteLine($"{"",-14}{"computed",14}{"thesis",14}{"phase (deg)",14}");
    Console.WriteLine($"{"Grunntone",-14}{result.Fundamental,14:F4}{preset.ExpectedGrunntone,14:F2}");
    Console.WriteLine($"{"THD %",-14}{result.Thd,14:F4}{preset.ExpectedThd,14:F4}");
    Console.WriteLine($"{"2nd dB",-14}{h2.Db,14:F2}{preset.ExpectedDb2nd,14:F2}{h2.PhaseDeg,14:F1}");
    Console.WriteLine($"{"3rd dB",-14}{h3.Db,14:F2}{preset.ExpectedDb3rd,14:F2}{h3.PhaseDeg,14:F1}");
    Console.WriteLine($"{"4th dB",-14}{h4.Db,14:F2}{preset.ExpectedDb4th,14:F2}{h4.PhaseDeg,14:F1}");
    Console.WriteLine($"{"5th dB",-14}{h5.Db,14:F2}{preset.ExpectedDb5th,14:F2}{h5.PhaseDeg,14:F1}");
    Console.WriteLine("(phase column is new information, not present in the original thesis printout)");
    Console.WriteLine();
}

Console.WriteLine($"{ThesisPresets.All.Count} of {ThesisPresets.KnownFigureCount} thesis figures transcribed so far.");

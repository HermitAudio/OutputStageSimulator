# OutputStageSimulator

A modern port of a 1985 HP Pascal (UCSD p-System) program from Terje
Sandstrom's M.Sc. thesis, which simulated nonlinear distortion in class
A/AB/B push-pull audio output stages. The original source only survives as
photographed thesis printouts; it's been transcribed and ported to C#/.NET,
validated sample-for-sample against the thesis's own published figures, and
wrapped in a small interactive web UI for exploring the model further —
including overlaying its transistor gain model against the real BD203/204
datasheet curve. This section is the map; everything else is short sections
below.

## What this is

- **[`OutputStageSimulator.Core`](src/OutputStageSimulator.Core)** — the
  ported simulation engine: complex-number math, a 256-point FFT, the
  Traub-method nonlinear equation solver, the transistor hFE (current gain)
  model, and the push-pull output-stage pipeline that ties them together
  (solve → FFT → normalize → harmonic/THD report).
- **[`OutputStageSimulator.ConsoleHarness`](src/OutputStageSimulator.ConsoleHarness)** —
  a validation harness that reproduces every transcribed thesis figure and
  prints computed-vs-thesis THD/harmonic levels side by side, for a quick
  correctness check without a browser.
- **[`OutputStageSimulator.Web`](src/OutputStageSimulator.Web)** — an
  interactive Blazor Server app: pick a thesis preset or a transistor type,
  tweak circuit/hFE-model parameters and watch the distortion spectrum
  recalculate live, and compare the model's calculated hFE-vs-Ic curve
  against the real datasheet curve on its own page — with a save button
  that persists calibration adjustments to a JSON file.
- **[`OutputStageSimulator.Core.Tests`](tests/OutputStageSimulator.Core.Tests)** —
  NUnit tests: unit coverage of every ported module, regression tests
  against the thesis's own figures (matched to the same precision the
  thesis printed), and independent FFT verification against the textbook
  closed-form spectra of square and triangle waves.

## Background

The thesis modeled how a transistor's current gain (hFE) falls off at both
low and high collector currents, and how that nonlinearity — combined with
a push-pull output stage's bias point (class A, AB, or B) — shows up as
harmonic distortion. The original program (UCSD Pascal, ~1985) built a test
tone, solved the stage's implicit nonlinear equation per sample via a
Traub-type root finder, ran it through an FFT, and reported THD and
individual harmonic levels — the same pipeline this port runs today, just
in C# rather than Pascal running on 1980s hardware.

Four thesis figures are currently transcribed and used as regression tests
(current-driven class AB/B/A, voltage-driven class B); more will be added
as further pages of the thesis get transcribed.

## Running it

Console harness (reproduces the thesis figures):

```
dotnet run --project src/OutputStageSimulator.ConsoleHarness
```

Web app:

```
dotnet run --project src/OutputStageSimulator.Web
```

then open the URL it prints (default `http://localhost:5241`).

Tests:

```
dotnet test tests/OutputStageSimulator.Core.Tests
```

Needs the .NET 10 SDK (pinned via `global.json`).

## Design

Four projects under `OutputStageSimulator.slnx` — a UI-agnostic core
library, a console validation harness, an NUnit test project, and a Blazor
Server web app — with a shared `ThesisPresets` list (circuit configurations
transcribed from thesis figures) and `TransistorProfiles` (real datasheet
reference curves) in Core, so the harness, the tests, and the web UI's
preset/transistor dropdowns all read from the same source instead of
duplicated numbers.

Transistor hFE-model calibration (Hfe max, Imax, a-factor, dI, Iturnover)
is stored separately from the thesis-exact presets, in
[`src/OutputStageSimulator.Web/Data/transistor-hfe-models.json`](src/OutputStageSimulator.Web/Data/transistor-hfe-models.json) —
editable live from the web app's hFE curve page and saved back to that file,
without touching the values used to reproduce the thesis's own figures.

## CI

GitHub Actions builds and tests on every push and pull request, publishes
results as a Checks summary, and generates a code coverage report — see
[`.github/workflows/ci.yml`](.github/workflows/ci.yml).

---

Part of [Hermit Audio](https://hermitaudio.github.io).

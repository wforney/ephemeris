<!-- Updated: 2026-06-20 -->
---
mode: agent
model: anthropic/claude-sonnet-4-5
tools: [codebase, editFiles, runCommands]
description: Write TUnit tests for Ephemeris calculation methods using Rocks for mocking and Verify for snapshot assertions.
---

You are writing TUnit tests for the Ephemeris .NET 10 library.

## Test project layout

- Test files live in `Ephemeris.Tests/Ephemeris.Tests/`
- Framework: **TUnit** — use `[Test]` attribute (not xUnit, not NUnit)
- Namespace: `namespace Ephemeris.Tests`
- Three libraries available: **TUnit**, **Rocks** (mocking), **Verify.TUnit** (snapshot assertions)

---

## [Rocks](https://github.com/JasonBock/Rocks) — compile-time source-generated mocks

Rocks generates mocks at compile time via Roslyn — no runtime proxies, no `It.IsAny<T>()`.

### Setup
Declare the mock at **assembly level** (once per interface, typically in `GlobalSetup.cs` or a dedicated `Mocks.cs`):

```csharp
[assembly: Rock(typeof(IStateVectorProvider), BuildType.Create)]
[assembly: Rock(typeof(ITimeConverter), BuildType.Create)]
[assembly: Rock(typeof(ISpaceKernelProvider), BuildType.Create)]
```

### Usage pattern
```csharp
[Test]
public async Task GetPosition_ValidKernel_ReturnsCartesianVector()
{
    var expectations = new IStateVectorProviderCreateExpectations();
    expectations.Setups.GetStateVector(
        Arg.Any<string>(), Arg.Any<double>(), Arg.Any<string>(), Arg.Any<string>())
        .ReturnValue(new double[] { 1.0, 2.0, 3.0 });

    var provider = expectations.Instance();
    var result = provider.GetStateVector("Sun", 0.0, "J2000", "Earth");

    expectations.Verify();
    await Assert.That(result).IsEquivalentTo(new double[] { 1.0, 2.0, 3.0 });
}
```

### Argument matchers
| Matcher | Meaning |
|---------|---------|
| `Arg.Any<T>()` | Any value of type T |
| `Arg.Validate<T>(x => x > 0)` | Predicate match |
| Literal value | Exact equality |

### Return values, callbacks, and exceptions
```csharp
// Return a value
expectations.Setups.Method(Arg.Any<int>()).ReturnValue(42);

// Callback to capture arguments or perform side effects
expectations.Setups.Method(Arg.Any<int>()).Callback(a => captured = a);

// Throw an exception on invocation (10.3.0+)
expectations.Setups.Method(Arg.Any<int>()).Throws<InvalidOperationException>();

// Verify expected call count
expectations.Setups.Method(Arg.Any<int>()).ExpectedCallCount(2);
```

### Always call Verify()
Rocks mocks are **strict** — all setups must be called exactly the expected number of times or `Verify()` throws a `VerificationException`. Always call `expectations.Verify()` at the end of the test.

> **Alternative:** [Imposter](https://github.com/themidnightgospel/Imposter) (`[assembly: GenerateImposter(typeof(IFoo))]`, `IFoo.Imposter()`) offers a more fluent chained API and implicit mode. Prefer it when you need `.Returns(1).Then().Returns(2)` chaining or implicit-mode fakes.

---

## Verify.TUnit — snapshot assertions

Use Verify for complex outputs (series of `EphemerisRecord`, plot data, exported CSV/JSON) where a precise numeric assertion would be brittle.

### First run flow
1. Run the test — it **fails** and writes `*.received.txt` next to the test file.
2. Inspect `received` file; if correct, copy/rename to `*.verified.txt`.
3. Subsequent runs diff against the `verified` file.

`*.received.*` files are in `.gitignore`. Commit only `*.verified.*` files.

### Pattern
```csharp
[Test]
public async Task GenerateSunSeries_TwentyFourHours_MatchesSnapshot()
{
    var start = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    var records = EphemerisBatch.GenerateSunSeries(start, 60, 24, -122.4, 37.8);

    await Verify(records);
}
```

### Scrub volatile data before verifying
```csharp
var settings = new VerifySettings();
settings.ScrubMember<EphemerisRecord>(r => r.TimeUtc);  // remove wall-clock timestamps

await Verify(records, settings);
```

---

## Assertion style (non-snapshot tests)

Astronomical calculations must be verified against **external reference values**. Always cite the source:

```csharp
[Test]
public async Task SunRA_J2000_MatchesUsno()
{
    // Reference: USNO Astronomical Almanac 2000, Table C-3
    // Sun RA at J2000.0 ≈ 281.416°
    double T = 0.0;
    var (ra, _) = SunEphemeris.ApparentEquatorialCoordinates(T);
    await Assert.That(ra).IsEqualTo(281.416).Within(0.1);
}
```

Reference sources: JPL Horizons (https://ssd.jpl.nasa.gov/horizons/), USNO Almanac, Meeus "Astronomical Algorithms".

---

## Test naming convention

`<MethodUnderTest>_<Scenario>_<ExpectedBehaviour>`

## Coverage priorities

1. Reference-value assertions at a known epoch (J2000.0 or a dated event)
2. Mocked provider tests (use Rocks for `IStateVectorProvider`, `ITimeConverter`)
3. Snapshot tests for batch output and export round-trips (use Verify)
4. Edge cases: polar observer, body below horizon, midnight sun
5. Round-trip: `EclipticToEquatorial` → `EquatorialToEcliptic`

## After writing tests

1. Run `dotnet test` — first run of Verify tests will fail; approve snapshots.
2. Commit approved `*.verified.*` files with `test(<scope>): <description>`.

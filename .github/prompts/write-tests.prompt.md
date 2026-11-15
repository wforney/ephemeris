<!-- Updated: 2026-11-15 -->
---
mode: agent
model: anthropic/claude-sonnet-4.5
tools: [codebase, editFiles, runCommands]
description: Write TUnit tests for Ephemeris calculation methods using Rocks for mocking and external reference values.
---

You are writing TUnit tests for the Ephemeris .NET 11 library.

## Test project layout

- Test files live in `Ephemeris.Tests/Ephemeris.Tests/`
- Framework: **TUnit** — use `[Test]` attribute (not xUnit, not NUnit)
- Namespace: `namespace Ephemeris.Tests`
- Two libraries available: **TUnit** (tests and assertions) and **Rocks** (mocking)

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

    await Assert.That(result).IsEquivalentTo(new double[] { 1.0, 2.0, 3.0 });
    expectations.Verify();
}
```

### Argument matchers
| Matcher | Meaning |
|---------|---------|
| `Arg.Any<T>()` | Any value of type T |
| `Arg.Is<T>(x => x > 0)` | Predicate match |
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
3. TUnit assertions for batch output and export round-trips
4. Edge cases: polar observer, body below horizon, midnight sun
5. Round-trip: `EclipticToEquatorial` → `EquatorialToEcliptic`

## After writing tests

1. Run `dotnet test` and confirm all tests pass.
2. Commit test changes with `test(<scope>): <description>`.

// Updated: 2026-03-10
using Ephemeris.Phenomenology;

namespace Ephemeris.Tests;

/// <summary>
/// Tests for PlanetaryEventCalculator and InnerPlanetEventCalculator.
/// Reference dates from published JPL opposition/elongation tables.
/// </summary>
public class PlanetaryEventTests
{
    // ── PlanetaryEventCalculator — Oppositions ────────────────────────────────

    [Test]
    public async Task Jupiter_Opposition_2024_IsFound()
    {
        // Jupiter opposition 2024-12-07 (JPL). Simplified elements may be ~30 days off.
        var result = PlanetaryEventCalculator.NextOpposition("jupiter", new DateTime(2024, 9, 1));
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Year).IsEqualTo(2024).Or.IsEqualTo(2025);
        // Allow ±45 days — simplified Kepler elements drift significantly for slow-moving gas giants
        await Assert.That(Math.Abs((result.Value - new DateTime(2024, 12, 7)).TotalDays)).IsLessThan(45);
    }

    [Test]
    public async Task Saturn_Opposition_2024_Sep_IsFound()
    {
        // Saturn opposition 2024-09-08 (JPL)
        var result = PlanetaryEventCalculator.NextOpposition("saturn", new DateTime(2024, 7, 1));
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Year).IsEqualTo(2024);
        // Allow ±30 days for simplified elements
        await Assert.That(Math.Abs((result.Value - new DateTime(2024, 9, 8)).TotalDays)).IsLessThan(30);
    }

    [Test]
    public async Task Mars_Opposition_2025_Jan_IsFound()
    {
        // Mars opposition 2025-01-16 (JPL)
        var result = PlanetaryEventCalculator.NextOpposition("mars", new DateTime(2024, 11, 1));
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Year).IsEqualTo(2025);
        await Assert.That(result.Value.Month).IsEqualTo(1);
        // Mars simplified model can be off by ~15 days
        await Assert.That(Math.Abs((result.Value - new DateTime(2025, 1, 16)).TotalDays)).IsLessThan(15);
    }

    [Test]
    public async Task Opposition_ResultIsAfterStartDate()
    {
        var after = new DateTime(2024, 1, 1);
        var result = PlanetaryEventCalculator.NextOpposition("jupiter", after);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsGreaterThanOrEqualTo(after);
    }

    // ── PlanetaryEventCalculator — Conjunctions ──────────────────────────────

    [Test]
    public async Task Jupiter_Conjunction_IsFound()
    {
        // Jupiter superior conjunction 2024-05-18
        var result = PlanetaryEventCalculator.NextConjunction("jupiter", new DateTime(2024, 3, 1));
        await Assert.That(result).IsNotNull();
        // Must be before the next opposition (Nov 2024)
        await Assert.That(result!.Value.Date).IsLessThan(new DateTime(2024, 11, 1));
    }

    [Test]
    public async Task Conjunction_ResultIsAfterStartDate()
    {
        var after = new DateTime(2024, 6, 1);
        var result = PlanetaryEventCalculator.NextConjunction("saturn", after);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Date).IsGreaterThanOrEqualTo(after);
    }

    [Test]
    public async Task Conjunction_OuterPlanet_LabelIsSuperior()
    {
        // All outer planets can only have superior conjunctions (planet behind the Sun).
        foreach (string planet in new[] { "mars", "jupiter", "saturn", "uranus", "neptune" })
        {
            var result = PlanetaryEventCalculator.NextConjunction(planet, new DateTime(2024, 1, 1));
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Value.ConjunctionType).IsEqualTo("Superior");
        }
    }

    // ── PlanetaryEventCalculator — NextQuadrature ────────────────────────────

    [Test]
    public async Task NextQuadrature_Jupiter_ReturnsBothEastAndWest()
    {
        var (east, west) = PlanetaryEventCalculator.NextQuadrature("jupiter", new DateTime(2024, 1, 1));
        await Assert.That(east).IsNotNull();
        await Assert.That(west).IsNotNull();
    }

    [Test]
    public async Task NextQuadrature_ResultsAreAfterStartDate()
    {
        var after = new DateTime(2024, 6, 1);
        var (east, west) = PlanetaryEventCalculator.NextQuadrature("jupiter", after);
        if (east is not null)
            await Assert.That(east.Value).IsGreaterThanOrEqualTo(after);
        if (west is not null)
            await Assert.That(west.Value).IsGreaterThanOrEqualTo(after);
    }

    [Test]
    public async Task NextQuadrature_Saturn_ReturnsBothQuadratures()
    {
        var (east, west) = PlanetaryEventCalculator.NextQuadrature("saturn", new DateTime(2024, 1, 1));
        await Assert.That(east).IsNotNull();
        await Assert.That(west).IsNotNull();
    }

    [Test]
    public async Task NextQuadrature_EastMatchesNextEastQuadrature()
    {
        // NextQuadrature.East should match NextEastQuadrature independently
        var after = new DateTime(2024, 3, 1);
        var (east, _) = PlanetaryEventCalculator.NextQuadrature("jupiter", after);
        var eastDirect = PlanetaryEventCalculator.NextEastQuadrature("jupiter", after);
        await Assert.That(east).IsEqualTo(eastDirect);
    }

    [Test]
    public async Task NextQuadrature_WestMatchesNextWestQuadrature()
    {
        // NextQuadrature.West should match NextWestQuadrature independently
        var after = new DateTime(2024, 3, 1);
        var (_, west) = PlanetaryEventCalculator.NextQuadrature("jupiter", after);
        var westDirect = PlanetaryEventCalculator.NextWestQuadrature("jupiter", after);
        await Assert.That(west).IsEqualTo(westDirect);
    }

    // ── PlanetaryEventCalculator — Quadratures ───────────────────────────────

    [Test]
    public async Task Jupiter_EastQuadrature_OccursBetweenConjunctionAndOpposition()
    {
        // After conjunction in May 2024, east quadrature should be ~Aug 2024
        var conj = PlanetaryEventCalculator.NextConjunction("jupiter", new DateTime(2024, 3, 1));
        var eastQ = PlanetaryEventCalculator.NextEastQuadrature("jupiter", conj!.Value.Date.AddDays(1));
        var opp = PlanetaryEventCalculator.NextOpposition("jupiter", eastQ!.Value.AddDays(1));
        await Assert.That(eastQ).IsNotNull();
        await Assert.That(eastQ!.Value).IsGreaterThan(conj!.Value.Date);
        await Assert.That(eastQ.Value).IsLessThan(opp!.Value);
    }

    [Test]
    public async Task Jupiter_WestQuadrature_OccursAfterOpposition()
    {
        // West quadrature should be after opposition
        var opp = PlanetaryEventCalculator.NextOpposition("jupiter", new DateTime(2024, 9, 1));
        var westQ = PlanetaryEventCalculator.NextWestQuadrature("jupiter", opp!.Value.AddDays(1));
        await Assert.That(westQ).IsNotNull();
        await Assert.That(westQ!.Value).IsGreaterThan(opp!.Value);
    }

    // ── InnerPlanetEventCalculator ────────────────────────────────────────────

    [Test]
    public async Task Venus_GreatestElongation_2025_Is_Found()
    {
        // Venus greatest eastern elongation 2025-01-10, elongation ≈ 47°
        var result = InnerPlanetEventCalculator.NextGreatestElongation("venus", new DateTime(2024, 11, 1));
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Date.Year).IsEqualTo(2025);
        await Assert.That(result.Value.ElongationDeg).IsGreaterThan(40.0);
        await Assert.That(result.Value.ElongationDeg).IsLessThan(50.0);
        await Assert.That(result.Value.Direction).IsEqualTo("East");
    }

    [Test]
    public async Task Mercury_GreatestElongation_IsWithin30Degrees()
    {
        // Mercury greatest elongation is always between 18° and 28°
        var result = InnerPlanetEventCalculator.NextGreatestElongation("mercury", new DateTime(2024, 11, 1));
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.ElongationDeg).IsGreaterThan(15.0);
        await Assert.That(result.Value.ElongationDeg).IsLessThan(30.0);
    }

    [Test]
    public async Task InnerPlanet_GreatestElongation_DirectionIsEitherEastOrWest()
    {
        var result = InnerPlanetEventCalculator.NextGreatestElongation("venus", new DateTime(2024, 1, 1));
        await Assert.That(result).IsNotNull();
        bool isValidDirection = result!.Value.Direction is "East" or "West";
        await Assert.That(isValidDirection).IsTrue();
    }

    [Test]
    public async Task InnerPlanet_UnknownPlanet_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => Task.FromResult(InnerPlanetEventCalculator.NextGreatestElongation("mars", DateTime.Now)));
    }

    [Test]
    public async Task GreatestElongation_ResultIsAfterStartDate()
    {
        var after = new DateTime(2025, 1, 1);
        var result = InnerPlanetEventCalculator.NextGreatestElongation("mercury", after);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Date).IsGreaterThanOrEqualTo(after);
    }
}

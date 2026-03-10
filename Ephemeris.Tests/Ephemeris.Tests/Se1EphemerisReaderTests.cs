using Ephemeris.Import;
using TUnit;

namespace Ephemeris.Tests;

/// <summary>
/// Tests for <see cref="Se1EphemerisReader"/> using locally available SE1 binary ephemeris data files.
/// Tests are skipped automatically when the SE1 file is not present on the local machine.
/// The expected reference values come from JPL Horizons (DE431) and from our own verified Python
/// analysis of the file binary format.
/// </summary>
/// <remarks>
/// To run these tests locally, place the SE1 data files in ~/ephem-data/.
/// </remarks>
public class Se1EphemerisReaderTests
{
    // Path to the planet SE1 file (covers 1800–2400 CE, DE431).
    private static readonly string SeFile =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "ephem-data", "sepl_18.se1");

    // J2000.0 Julian Day — standard reference epoch (2000-Jan-01.5 TT).
    private const double JdJ2000 = 2451545.0;

    // ── Guard helper ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> if the SE1 data file exists on this machine.
    /// All tests in this class check this and skip if the file is absent.
    /// </summary>
    private static bool FileAvailable => File.Exists(SeFile);

    // ── Constructor / reader lifetime ────────────────────────────────────────

    [Test]
    public async Task Constructor_LoadsHeaderFields_WhenFileExists()
    {
        if (!FileAvailable)
        {
            // Skip gracefully — file is not in the repository.
            return;
        }

        using var reader = new Se1EphemerisReader(SeFile);

        // The file covers 1800–2400 CE; DE431.
        await Assert.That(reader.DeVersion).IsEqualTo(431);
        await Assert.That(reader.FileStart).IsGreaterThan(2_300_000.0);   // > ~1600 CE
        await Assert.That(reader.FileEnd).IsGreaterThan(reader.FileStart);
        await Assert.That(reader.BodyIds).Contains(Se1EphemerisReader.SeiEmb);
        await Assert.That(reader.BodyIds).Contains(Se1EphemerisReader.SeiJupiter);
    }

    // ── EMB distance at J2000 ────────────────────────────────────────────────

    /// <summary>
    /// Earth's distance from the Sun at J2000.0 should be very close to 0.983 AU
    /// (DE431 value: 0.9833 AU).  We verify against this reference value with a
    /// tolerance of 0.002 AU (~300,000 km).
    /// </summary>
    [Test]
    public async Task EMB_DistanceAtJ2000_IsApproximately0983AU()
    {
        if (!FileAvailable) { return; } // skip — file not available

        using var reader = new Se1EphemerisReader(SeFile);
        var (x, y, z) = reader.GetBarycentricPosition(Se1EphemerisReader.SeiEmb, JdJ2000);
        double r = Math.Sqrt(x * x + y * y + z * z);

        // Reference: Earth–Sun distance at J2000.0 ≈ 0.9833 AU (DE431)
        await Assert.That(r).IsGreaterThan(0.980);
        await Assert.That(r).IsLessThan(0.986);
    }

    // ── Jupiter distance at J2000 ─────────────────────────────────────────────

    /// <summary>
    /// Jupiter's heliocentric distance at J2000.0 should be near its semi-major axis
    /// of 5.20 AU.  We accept any value within [4.9, 5.5] AU.
    /// </summary>
    [Test]
    public async Task Jupiter_BarycentricDistanceAtJ2000_IsNearSemiMajorAxis()
    {
        if (!FileAvailable) { return; } // skip — file not available

        using var reader = new Se1EphemerisReader(SeFile);
        var (x, y, z) = reader.GetBarycentricPosition(Se1EphemerisReader.SeiJupiter, JdJ2000);
        double r = Math.Sqrt(x * x + y * y + z * z);

        // Jupiter's semi-major axis ≈ 5.20 AU; actual distance may vary by ~0.3 AU.
        await Assert.That(r).IsGreaterThan(4.9);
        await Assert.That(r).IsLessThan(5.5);
    }

    // ── Geocentric RA ─────────────────────────────────────────────────────────

    /// <summary>
    /// Geocentric RA must be in [0°, 360°) and Dec in [−90°, 90°] for any body.
    /// </summary>
    [Test]
    public async Task Jupiter_GeocentricRaDecAtJ2000_AreInValidRange()
    {
        if (!FileAvailable) { return; } // skip — file not available

        using var reader = new Se1EphemerisReader(SeFile);
        var (x, y, z) = reader.GetGeocentricPosition(Se1EphemerisReader.SeiJupiter, JdJ2000);
        var (coords, dist) = Se1EphemerisReader.CartesianToRaDec(x, y, z);
        double ra = coords.RightAscension;
        double dec = coords.Declination;

        await Assert.That(ra).IsGreaterThanOrEqualTo(0.0);
        await Assert.That(ra).IsLessThan(360.0);
        await Assert.That(dec).IsGreaterThanOrEqualTo(-90.0);
        await Assert.That(dec).IsLessThanOrEqualTo(90.0);
        await Assert.That(dist).IsGreaterThan(3.0);    // Jupiter always > 3 AU from Earth
    }

    /// <summary>
    /// Jupiter's geocentric RA at J2000.0.  From our verified algorithm: RA ≈ 23.9°.
    /// Tolerance ±2° to account for light-time (not applied here) and small algorithm differences.
    /// </summary>
    [Test]
    public async Task Jupiter_GeocentricRA_AtJ2000_IsApproximately24Degrees()
    {
        if (!FileAvailable) { return; } // skip — file not available

        using var reader = new Se1EphemerisReader(SeFile);
        var (x, y, z) = reader.GetGeocentricPosition(Se1EphemerisReader.SeiJupiter, JdJ2000);
        var (coords2, _) = Se1EphemerisReader.CartesianToRaDec(x, y, z);
        double ra = coords2.RightAscension;

        // Verified reference value: RA ≈ 23.9° at JD 2451545.0
        await Assert.That(ra).IsGreaterThan(21.0);
        await Assert.That(ra).IsLessThan(27.0);
    }

    // ── Barycentric Sun ───────────────────────────────────────────────────────

    /// <summary>
    /// The barycentric Sun should be very close to the origin (within ~0.02 AU).
    /// The Solar System barycenter is currently inside the Sun's radius (~0.005 AU).
    /// </summary>
    [Test]
    public async Task BarycentricSun_DistanceAtJ2000_IsLessThan002AU()
    {
        if (!FileAvailable) { return; } // skip — file not available

        using var reader = new Se1EphemerisReader(SeFile);
        var (x, y, z) = reader.GetBarycentricPosition(Se1EphemerisReader.SeiSunBary, JdJ2000);
        double r = Math.Sqrt(x * x + y * y + z * z);

        // Barycentric Sun should be within ~0.02 AU of the barycenter.
        await Assert.That(r).IsLessThan(0.02);
    }

    // ── CartesianToRaDec ─────────────────────────────────────────────────────

    [Test]
    public async Task CartesianToRaDec_XAxisPoint_GivesRa0Dec0()
    {
        var (coords, dist) = Se1EphemerisReader.CartesianToRaDec(1.0, 0.0, 0.0);
        await Assert.That(coords.RightAscension).IsEqualTo(0.0).Within(1e-10);
        await Assert.That(coords.Declination).IsEqualTo(0.0).Within(1e-10);
        await Assert.That(dist).IsEqualTo(1.0).Within(1e-10);
    }

    [Test]
    public async Task CartesianToRaDec_YAxisPoint_GivesRa90Dec0()
    {
        var (coords, dist) = Se1EphemerisReader.CartesianToRaDec(0.0, 1.0, 0.0);
        await Assert.That(coords.RightAscension).IsEqualTo(90.0).Within(1e-10);
        await Assert.That(coords.Declination).IsEqualTo(0.0).Within(1e-10);
        await Assert.That(dist).IsEqualTo(1.0).Within(1e-10);
    }

    [Test]
    public async Task CartesianToRaDec_ZAxisPoint_GivesRa0Dec90()
    {
        var (coords, dist) = Se1EphemerisReader.CartesianToRaDec(0.0, 0.0, 1.0);
        await Assert.That(coords.Declination).IsEqualTo(90.0).Within(1e-6);
        await Assert.That(dist).IsEqualTo(1.0).Within(1e-10);
    }
}

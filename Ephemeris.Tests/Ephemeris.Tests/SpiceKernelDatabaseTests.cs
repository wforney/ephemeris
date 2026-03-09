// Updated: 2026-03-09
using Ephemeris.Import;

namespace Ephemeris.Tests;

/// <summary>Tests for <see cref="SpiceKernelDatabase"/> provider implementations.</summary>
public class SpiceKernelDatabaseTests
{
    private static readonly SpiceKernelDatabase _db = new();

    [Test]
    public async Task ConvertUtcToEphemerisTime_UtcNoonJ2000_ReturnsApprox64Seconds()
    {
        // UTC 2000-Jan-01 12:00:00 is NOT ET=0. J2000.0 is defined in TT, not UTC.
        // ET ≈ 64.184 s because TT = UTC + 32 leap-seconds + 32.184 s
        var j2000Utc = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        double et = _db.ConvertUtcToEphemerisTime(j2000Utc);
        await Assert.That(Math.Abs(et - 64.184)).IsLessThanOrEqualTo(1.0);
    }

    [Test]
    public async Task ConvertUtcToEphemerisTime_OneDayAfterUtcNoonJ2000_Returns86464Seconds()
    {
        // One day after UTC noon J2000: ET ≈ 86400 + 64.184 = 86464.184 s
        var oneDayLater = new DateTime(2000, 1, 2, 12, 0, 0, DateTimeKind.Utc);
        double et = _db.ConvertUtcToEphemerisTime(oneDayLater);
        await Assert.That(Math.Abs(et - 86464.184)).IsLessThanOrEqualTo(1.0);
    }

    [Test]
    public async Task ConvertUtcToEphemerisTime_KnownDate2025_MatchesExpected()
    {
        // 2025-Jan-01 00:00:00 UTC: leap seconds = 37
        // ET ≈ (2025-Jan-01 00:00:00 UTC − 2000-Jan-01 12:00:00 UTC).TotalSeconds + 37 + 32.184
        //    = 9131.5 × 86400 + 69.184 ≈ 788_961_669.184
        var dt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        double et = _db.ConvertUtcToEphemerisTime(dt);
        const double expected = 9131.5 * 86400.0 + 69.184;
        await Assert.That(Math.Abs(et - expected)).IsLessThanOrEqualTo(2.0);
    }

    [Test]
    public async Task LoadKernel_NonExistentFile_ThrowsFileNotFoundException()
    {
        var db2 = new SpiceKernelDatabase();
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await Task.Run(() => db2.LoadKernel("/tmp/nonexistent.bsp")));
    }

    [Test]
    public async Task GetPosition_WithoutLoadedKernel_ThrowsInvalidOperationException()
    {
        var db2 = new SpiceKernelDatabase();
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Task.Run(() => db2.GetPosition("SUN", 0.0, "J2000", "SSB")));
    }
}

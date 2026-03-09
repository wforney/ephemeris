using Ephemeris.Import;

namespace Ephemeris.Tests;

/// <summary>Tests for <see cref="SpiceKernelDatabase"/> provider implementations.</summary>
public class SpiceKernelDatabaseTests
{
    private static readonly SpiceKernelDatabase _db = new();

    [Test]
    public async Task ConvertUtcToEphemerisTime_J2000Epoch_ReturnsZero()
    {
        // J2000.0 = 2000-Jan-1 12:00:00 UTC — ET must be exactly 0.0
        var j2000 = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        double et = _db.ConvertUtcToEphemerisTime(j2000);
        await Assert.That(et).IsEqualTo(0.0);
    }

    [Test]
    public async Task ConvertUtcToEphemerisTime_OneDayAfterJ2000_Returns86400Seconds()
    {
        var oneDayLater = new DateTime(2000, 1, 2, 12, 0, 0, DateTimeKind.Utc);
        double et = _db.ConvertUtcToEphemerisTime(oneDayLater);
        await Assert.That(et).IsEqualTo(86400.0);
    }

    [Test]
    public async Task ConvertUtcToEphemerisTime_KnownDate_MatchesExpected()
    {
        // 2025-Jan-1 00:00:00 UTC is 9131.5 days after J2000.0 (2000-Jan-1 12:00 UTC)
        // ET = 9131.5 * 86400 = 788,961,600.0 seconds
        var dt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        double et = _db.ConvertUtcToEphemerisTime(dt);
        const double expected = 9131.5 * 86400.0;
        await Assert.That(Math.Abs(et - expected)).IsLessThanOrEqualTo(1.0); // within 1 second
    }

    [Test]
    public async Task LoadKernel_NonExistentFile_ThrowsFileNotFoundException()
    {
        var db2 = new SpiceKernelDatabase();
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await Task.Run(() => db2.LoadKernel("/tmp/nonexistent.bsp")));
    }
}

// Updated: 2026-03-09
using Ephemeris.Import;

namespace Ephemeris.Tests;

/// <summary>Tests for <see cref="SpkReader"/> and <see cref="SpkLeapSeconds"/>.</summary>
public class SpkReaderTests
{
    // -----------------------------------------------------------------------
    // SpkLeapSeconds tests
    // -----------------------------------------------------------------------

    [Test]
    public async Task SpkLeapSeconds_Before1972_ReturnsZero()
    {
        var dt = new DateTime(1970, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        int ls = SpkLeapSeconds.GetLeapSeconds(dt);
        await Assert.That(ls).IsEqualTo(0);
    }

    [Test]
    public async Task SpkLeapSeconds_At2000Jan01_Returns32()
    {
        var dt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        int ls = SpkLeapSeconds.GetLeapSeconds(dt);
        await Assert.That(ls).IsEqualTo(32);
    }

    [Test]
    public async Task SpkLeapSeconds_After2017Jan01_Returns37()
    {
        var dt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        int ls = SpkLeapSeconds.GetLeapSeconds(dt);
        await Assert.That(ls).IsEqualTo(37);
    }

    // -----------------------------------------------------------------------
    // Body ID resolution tests
    // -----------------------------------------------------------------------

    [Test]
    public async Task SpkReader_ResolveBodyId_SunReturns10()
    {
        int id = SpkReader.ResolveBodyId("SUN");
        await Assert.That(id).IsEqualTo(10);
    }

    [Test]
    public async Task SpkReader_ResolveBodyId_EarthReturns399()
    {
        int id = SpkReader.ResolveBodyId("EARTH");
        await Assert.That(id).IsEqualTo(399);
    }

    [Test]
    public async Task SpkReader_ResolveBodyId_NumericString_Parses()
    {
        int id = SpkReader.ResolveBodyId("301");
        await Assert.That(id).IsEqualTo(301);
    }

    [Test]
    public async Task SpkReader_ResolveBodyId_CaseInsensitive()
    {
        int id = SpkReader.ResolveBodyId("moon");
        await Assert.That(id).IsEqualTo(301);
    }

    // -----------------------------------------------------------------------
    // Synthetic BSP file tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a minimal valid DAF/SPK Type 2 binary file with a single segment
    /// containing one Chebyshev record with NCOEF=1 (constant position [1000, 2000, 3000] km).
    /// </summary>
    private static string CreateSyntheticBsp()
    {
        string path = Path.Combine(Path.GetTempPath(), $"test_spk_{Guid.NewGuid():N}.bsp");

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var bw = new BinaryWriter(fs);

        // ----- Record 1: File header (1024 bytes) -----
        byte[] rec1 = new byte[1024];

        // LOCIDW: "NAIF/DAF" (8 bytes)
        System.Text.Encoding.ASCII.GetBytes("NAIF/DAF").CopyTo(rec1, 0);

        // ND = 2, NI = 6
        BitConverter.GetBytes(2).CopyTo(rec1, 8);
        BitConverter.GetBytes(6).CopyTo(rec1, 12);

        // LOCIFN: "TEST_KERNEL" padded to 60 bytes
        byte[] locifn = System.Text.Encoding.ASCII.GetBytes("TEST_KERNEL");
        locifn.CopyTo(rec1, 16);
        for (int i = locifn.Length; i < 60; i++)
            rec1[16 + i] = (byte)' ';

        // FWARD = 2 (first summary record is record 2)
        BitConverter.GetBytes(2).CopyTo(rec1, 76);
        // BWARD = 2
        BitConverter.GetBytes(2).CopyTo(rec1, 80);
        // FREE = 394 (first free address after segment data)
        BitConverter.GetBytes(394).CopyTo(rec1, 84);
        // LOCFMT: "LTL-IEEE" (8 bytes)
        System.Text.Encoding.ASCII.GetBytes("LTL-IEEE").CopyTo(rec1, 88);

        bw.Write(rec1);

        // ----- Record 2: Summary record (1024 bytes) -----
        byte[] rec2 = new byte[1024];

        // NEXT = 0.0
        BitConverter.GetBytes(0.0).CopyTo(rec2, 0);
        // PREV = 0.0
        BitConverter.GetBytes(0.0).CopyTo(rec2, 8);
        // NSUM = 1.0
        BitConverter.GetBytes(1.0).CopyTo(rec2, 16);

        // Descriptor (40 bytes) at offset 24
        int dOff = 24;
        BitConverter.GetBytes(-1e10).CopyTo(rec2, dOff);       // start_et
        BitConverter.GetBytes(1e10).CopyTo(rec2, dOff + 8);    // end_et
        BitConverter.GetBytes(10).CopyTo(rec2, dOff + 16);     // target = Sun
        BitConverter.GetBytes(0).CopyTo(rec2, dOff + 20);      // center = SSB
        BitConverter.GetBytes(1).CopyTo(rec2, dOff + 24);      // frame = J2000
        BitConverter.GetBytes(2).CopyTo(rec2, dOff + 28);      // dtype = Type 2
        BitConverter.GetBytes(385).CopyTo(rec2, dOff + 32);    // begin address
        BitConverter.GetBytes(393).CopyTo(rec2, dOff + 36);    // end address

        bw.Write(rec2);

        // ----- Record 3: Name record (1024 bytes) -----
        byte[] rec3 = new byte[1024];
        BitConverter.GetBytes(0.0).CopyTo(rec3, 0);   // NEXT
        BitConverter.GetBytes(0.0).CopyTo(rec3, 8);   // PREV
        BitConverter.GetBytes(1.0).CopyTo(rec3, 16);  // NSUM
        byte[] segName = System.Text.Encoding.ASCII.GetBytes("SUN vs. SSB");
        segName.CopyTo(rec3, 24);
        for (int i = segName.Length; i < 40; i++)
            rec3[24 + i] = (byte)' ';

        bw.Write(rec3);

        // ----- Record 4: Segment data (1024 bytes) -----
        // Addresses 385..393 → 9 doubles at byte offset 3072
        byte[] rec4 = new byte[1024];

        // Chebyshev record (5 doubles: MID, RADIUS, c0_X, c0_Y, c0_Z)
        BitConverter.GetBytes(0.0).CopyTo(rec4, 0);       // [385] MID
        BitConverter.GetBytes(1e10).CopyTo(rec4, 8);       // [386] RADIUS
        BitConverter.GetBytes(1000.0).CopyTo(rec4, 16);    // [387] c0_X
        BitConverter.GetBytes(2000.0).CopyTo(rec4, 24);    // [388] c0_Y
        BitConverter.GetBytes(3000.0).CopyTo(rec4, 32);    // [389] c0_Z

        // Directory (4 doubles: INIT, INTLEN, RSIZE, N)
        BitConverter.GetBytes(-1e10).CopyTo(rec4, 40);     // [390] INIT
        BitConverter.GetBytes(2e10).CopyTo(rec4, 48);      // [391] INTLEN
        BitConverter.GetBytes(5.0).CopyTo(rec4, 56);       // [392] RSIZE
        BitConverter.GetBytes(1.0).CopyTo(rec4, 64);       // [393] N

        bw.Write(rec4);

        return path;
    }

    [Test]
    public async Task SpkReader_SyntheticFile_SegmentIndexed()
    {
        string path = CreateSyntheticBsp();
        try
        {
            using var reader = new SpkReader(path);
            await Assert.That(reader.SegmentCount).IsEqualTo(1);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task SpkReader_SyntheticConstantPosition_ReturnsExpectedKm()
    {
        string path = CreateSyntheticBsp();
        try
        {
            using var reader = new SpkReader(path);
            double[] pos = reader.GetPosition(10, 0.0, 0);

            await Assert.That(Math.Abs(pos[0] - 1000.0)).IsLessThanOrEqualTo(1e-6);
            await Assert.That(Math.Abs(pos[1] - 2000.0)).IsLessThanOrEqualTo(1e-6);
            await Assert.That(Math.Abs(pos[2] - 3000.0)).IsLessThanOrEqualTo(1e-6);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task SpkReader_SyntheticConstantPosition_DifferentEpoch_ReturnsSamePosition()
    {
        string path = CreateSyntheticBsp();
        try
        {
            using var reader = new SpkReader(path);
            // NCOEF=1 means the polynomial is constant, so any ET should give the same result
            double[] pos = reader.GetPosition(10, 1e9, 0);

            await Assert.That(Math.Abs(pos[0] - 1000.0)).IsLessThanOrEqualTo(1e-6);
            await Assert.That(Math.Abs(pos[1] - 2000.0)).IsLessThanOrEqualTo(1e-6);
            await Assert.That(Math.Abs(pos[2] - 3000.0)).IsLessThanOrEqualTo(1e-6);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // -----------------------------------------------------------------------
    // Integration test — skip if no BSP file available
    // -----------------------------------------------------------------------

    [Test]
    public async Task SpkReader_Integration_DE440_SunPositionAtJ2000()
    {
        var bspPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "ephem-data", "de440s.bsp");

        if (!File.Exists(bspPath))
        {
            // Also try de430.bsp
            bspPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "ephem-data", "de430.bsp");
        }

        if (!File.Exists(bspPath))
        {
            await Assert.That(true).IsTrue(); // skip silently
            return;
        }

        using var reader = new SpkReader(bspPath);
        double[] pos = reader.GetPosition(10, 0.0, 0); // Sun relative to SSB at J2000.0

        // Sun distance from SSB at J2000 should be within ~1e6 km
        double dist = Math.Sqrt(pos[0] * pos[0] + pos[1] * pos[1] + pos[2] * pos[2]);
        await Assert.That(dist).IsLessThanOrEqualTo(2e6); // should be small; Sun is near SSB
    }
}

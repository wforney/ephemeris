// Updated: 2026-03-10
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
            return; // skip silently — file not available
        }

        using var reader = new SpkReader(bspPath);
        double[] pos = reader.GetPosition(10, 0.0, 0); // Sun relative to SSB at J2000.0

        // Sun distance from SSB at J2000 should be within ~1e6 km
        double dist = Math.Sqrt(pos[0] * pos[0] + pos[1] * pos[1] + pos[2] * pos[2]);
        await Assert.That(dist).IsLessThanOrEqualTo(2e6); // should be small; Sun is near SSB
    }

    // -----------------------------------------------------------------------
    // Multi-hop BSP graph traversal tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a synthetic three-segment BSP file modelling:
    /// <list type="bullet">
    ///   <item>301 (Moon) relative to 3 (EMB) at constant [100, 200, 300] km</item>
    ///   <item>3 (EMB) relative to 0 (SSB) at constant [1000, 2000, 3000] km</item>
    ///   <item>399 (Earth) relative to 3 (EMB) at constant [50, 100, 150] km</item>
    /// </list>
    /// </summary>
    private static string CreateMultiHopBsp()
    {
        string path = Path.Combine(Path.GetTempPath(), $"test_multihop_{Guid.NewGuid():N}.bsp");

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var bw = new BinaryWriter(fs);

        // Each segment uses 9 doubles (5 Chebyshev + 4 directory), RSIZE=5, NCOEF=1.
        // Record layout (1-based):
        //   1 = file header
        //   2 = summary record (3 segments)
        //   3 = name record
        //   4 = data record (3 segments × 9 doubles = 27 doubles, fit in one record)
        //
        // Addresses (1-based double words, record 4 = bytes 3072..4095):
        //   Seg 1: addresses  385..393  (rec4 doubles 0..8)
        //   Seg 2: addresses  394..402  (rec4 doubles 9..17)
        //   Seg 3: addresses  403..411  (rec4 doubles 18..26)
        //
        // Note: 1-based address N maps to byte offset (N-1)*8.
        // Record 4 starts at byte 3*1024 = 3072 → address = 3072/8+1 = 385.

        const int seg1Begin = 385;
        const int seg1End   = 393;
        const int seg2Begin = 394;
        const int seg2End   = 402;
        const int seg3Begin = 403;
        const int seg3End   = 411;

        // ----- Record 1: File header (1024 bytes) -----
        byte[] rec1 = new byte[1024];
        System.Text.Encoding.ASCII.GetBytes("NAIF/DAF").CopyTo(rec1, 0);
        BitConverter.GetBytes(2).CopyTo(rec1, 8);   // ND
        BitConverter.GetBytes(6).CopyTo(rec1, 12);  // NI
        byte[] locifn = System.Text.Encoding.ASCII.GetBytes("MULTIHOP_TEST");
        locifn.CopyTo(rec1, 16);
        for (int i = locifn.Length; i < 60; i++) rec1[16 + i] = (byte)' ';
        BitConverter.GetBytes(2).CopyTo(rec1, 76);  // FWARD
        BitConverter.GetBytes(2).CopyTo(rec1, 80);  // BWARD
        BitConverter.GetBytes(412).CopyTo(rec1, 84); // FREE
        System.Text.Encoding.ASCII.GetBytes("LTL-IEEE").CopyTo(rec1, 88);
        bw.Write(rec1);

        // ----- Record 2: Summary record (3 segments, 40 bytes each) -----
        byte[] rec2 = new byte[1024];
        BitConverter.GetBytes(0.0).CopyTo(rec2, 0);  // NEXT
        BitConverter.GetBytes(0.0).CopyTo(rec2, 8);  // PREV
        BitConverter.GetBytes(3.0).CopyTo(rec2, 16); // NSUM

        static void WriteDescriptor(byte[] buf, int offset,
            double startEt, double endEt, int target, int center, int begin, int end)
        {
            BitConverter.GetBytes(startEt).CopyTo(buf, offset);
            BitConverter.GetBytes(endEt).CopyTo(buf, offset + 8);
            BitConverter.GetBytes(target).CopyTo(buf, offset + 16);
            BitConverter.GetBytes(center).CopyTo(buf, offset + 20);
            BitConverter.GetBytes(1).CopyTo(buf, offset + 24);     // frame = J2000
            BitConverter.GetBytes(2).CopyTo(buf, offset + 28);     // type  = 2
            BitConverter.GetBytes(begin).CopyTo(buf, offset + 32);
            BitConverter.GetBytes(end).CopyTo(buf, offset + 36);
        }

        WriteDescriptor(rec2, 24,  -1e10, 1e10, 301, 3,   seg1Begin, seg1End);  // Moon / EMB
        WriteDescriptor(rec2, 64,  -1e10, 1e10, 3,   0,   seg2Begin, seg2End);  // EMB  / SSB
        WriteDescriptor(rec2, 104, -1e10, 1e10, 399, 3,   seg3Begin, seg3End);  // Earth / EMB
        bw.Write(rec2);

        // ----- Record 3: Name record (1024 bytes) -----
        byte[] rec3 = new byte[1024];
        BitConverter.GetBytes(3.0).CopyTo(rec3, 16); // NSUM
        bw.Write(rec3);

        // ----- Record 4: Segment data (3 × 9 doubles = 27 doubles) -----
        byte[] rec4 = new byte[1024];

        static void WriteSegData(byte[] buf, int byteOffset, double x, double y, double z)
        {
            BitConverter.GetBytes(0.0).CopyTo(buf, byteOffset);      // MID
            BitConverter.GetBytes(1e10).CopyTo(buf, byteOffset + 8); // RADIUS
            BitConverter.GetBytes(x).CopyTo(buf, byteOffset + 16);   // c0_X
            BitConverter.GetBytes(y).CopyTo(buf, byteOffset + 24);   // c0_Y
            BitConverter.GetBytes(z).CopyTo(buf, byteOffset + 32);   // c0_Z
            // Directory (INIT, INTLEN, RSIZE=5, N=1)
            BitConverter.GetBytes(-1e10).CopyTo(buf, byteOffset + 40);
            BitConverter.GetBytes(2e10).CopyTo(buf, byteOffset + 48);
            BitConverter.GetBytes(5.0).CopyTo(buf, byteOffset + 56);
            BitConverter.GetBytes(1.0).CopyTo(buf, byteOffset + 64);
        }

        WriteSegData(rec4, 0,  100.0, 200.0, 300.0);   // Moon / EMB
        WriteSegData(rec4, 72, 1000.0, 2000.0, 3000.0); // EMB  / SSB
        WriteSegData(rec4, 144, 50.0, 100.0, 150.0);   // Earth / EMB
        bw.Write(rec4);

        return path;
    }

    /// <summary>
    /// Moon (301) relative to Earth (399) requires path 301→3→399.
    /// pos(301,399) = pos(301,3) − pos(399,3) = [100,200,300] − [50,100,150] = [50,100,150]
    /// </summary>
    [Test]
    public async Task SpkReader_MultiHop_MoonRelativeToEarth_CorrectPosition()
    {
        string path = CreateMultiHopBsp();
        try
        {
            using var reader = new SpkReader(path);
            double[] pos = reader.GetPosition(301, 0.0, 399);

            await Assert.That(Math.Abs(pos[0] - 50.0)).IsLessThanOrEqualTo(1e-6);
            await Assert.That(Math.Abs(pos[1] - 100.0)).IsLessThanOrEqualTo(1e-6);
            await Assert.That(Math.Abs(pos[2] - 150.0)).IsLessThanOrEqualTo(1e-6);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Moon (301) relative to SSB (0) via path 301→3→0.
    /// pos(301,0) = pos(301,3) + pos(3,0) = [100,200,300] + [1000,2000,3000] = [1100,2200,3300]
    /// </summary>
    [Test]
    public async Task SpkReader_MultiHop_MoonRelativeToSSB_CorrectPosition()
    {
        string path = CreateMultiHopBsp();
        try
        {
            using var reader = new SpkReader(path);
            double[] pos = reader.GetPosition(301, 0.0, 0);

            await Assert.That(Math.Abs(pos[0] - 1100.0)).IsLessThanOrEqualTo(1e-6);
            await Assert.That(Math.Abs(pos[1] - 2200.0)).IsLessThanOrEqualTo(1e-6);
            await Assert.That(Math.Abs(pos[2] - 3300.0)).IsLessThanOrEqualTo(1e-6);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Earth (399) relative to Moon (301) is just the negation of Moon relative to Earth.
    /// = −[50,100,150] = [−50,−100,−150]
    /// </summary>
    [Test]
    public async Task SpkReader_MultiHop_EarthRelativeToMoon_IsNegated()
    {
        string path = CreateMultiHopBsp();
        try
        {
            using var reader = new SpkReader(path);
            double[] moonRelEarth  = reader.GetPosition(301, 0.0, 399);
            double[] earthRelMoon  = reader.GetPosition(399, 0.0, 301);

            await Assert.That(Math.Abs(moonRelEarth[0] + earthRelMoon[0])).IsLessThanOrEqualTo(1e-6);
            await Assert.That(Math.Abs(moonRelEarth[1] + earthRelMoon[1])).IsLessThanOrEqualTo(1e-6);
            await Assert.That(Math.Abs(moonRelEarth[2] + earthRelMoon[2])).IsLessThanOrEqualTo(1e-6);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Requesting a body not in the kernel at all must throw with a descriptive message
    /// listing the available body IDs.
    /// </summary>
    [Test]
    public async Task SpkReader_MultiHop_UnknownBody_ThrowsDescriptiveMessage()
    {
        string path = CreateMultiHopBsp();
        try
        {
            using var reader = new SpkReader(path);
            bool threw = false;
            string message = "";
            try
            {
                _ = reader.GetPosition(599, 0.0, 0); // Jupiter — not in file
            }
            catch (InvalidOperationException ex)
            {
                threw = true;
                message = ex.Message;
            }

            await Assert.That(threw).IsTrue();
            // Message should mention the target and list available body IDs
            await Assert.That(message).Contains("599");
            await Assert.That(message).Contains("301"); // Moon is in the file
        }
        finally
        {
            File.Delete(path);
        }
    }
}

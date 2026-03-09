// Updated: 2026-05-29
using Ephemeris.Geometry;

namespace Ephemeris.Import;

/// <summary>
/// Reads SE1 binary planet/Moon ephemeris files and evaluates body positions
/// via Chebyshev polynomial interpolation.
/// </summary>
/// <remarks>
/// <para>
/// SE1 files encode JPL DE-series ephemeris data (DE431, DE430, …) in a compact
/// Chebyshev-series format. Each file covers a fixed Julian-Day span, divided into
/// equal-length segments per body. Coefficients are stored using a variable-precision
/// integer-packing scheme (4 / 3 / 2 / 1 byte and nibble groups) described in full in
/// <c>docs/se1-format.md</c>.
/// </para>
/// <para>
/// <b>These files are NOT included in this repository.</b>
/// Supply the local file path to <see cref="Se1EphemerisReader"/>.
/// </para>
/// <para>
/// The algorithm was established by reverse-engineering the SE1 binary format empirically
/// and verifying output against JPL Horizons reference values.
/// </para>
/// </remarks>
public sealed class Se1EphemerisReader : IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════
    //  Constants
    // ═══════════════════════════════════════════════════════════════════════

    private const int HeaderSize = 116;         // ASCII text header bytes
    private const int MarkerSize = 4;           // endianness marker
    private const double TwoPI   = Math.PI * 2.0;

    // Body-flag bitmask constants (see docs/se1-format.md)
    private const byte FlagHelio   = 0x01;  // coordinates are heliocentric
    private const byte FlagRotate  = 0x02;  // apply rot_back (orbital → J2000 equatorial)
    private const byte FlagEllipse = 0x04;  // reference ellipse stored in file
    private const byte FlagEmbHel  = 0x08;  // used for SEI_SUNBARY/EMB combination

    // SE1 body IDs (SEI_* constants defined by the file format)

    /// <summary>SEI ID of the Earth-Moon Barycenter (barycentric).</summary>
    public const int SeiEmb = 0;

    /// <summary>SEI ID of the Moon (in <c>semo_NN.se1</c> files).</summary>
    public const int SeiMoon = 1;

    /// <summary>SEI ID of Mercury.</summary>
    public const int SeiMercury = 2;

    /// <summary>SEI ID of Venus.</summary>
    public const int SeiVenus = 3;

    /// <summary>SEI ID of Mars.</summary>
    public const int SeiMars = 4;

    /// <summary>SEI ID of Jupiter.</summary>
    public const int SeiJupiter = 5;

    /// <summary>SEI ID of Saturn.</summary>
    public const int SeiSaturn = 6;

    /// <summary>SEI ID of Uranus.</summary>
    public const int SeiUranus = 7;

    /// <summary>SEI ID of Neptune.</summary>
    public const int SeiNeptune = 8;

    /// <summary>SEI ID of Pluto.</summary>
    public const int SeiPluto = 9;

    /// <summary>
    /// SEI ID of the barycentric-Sun slot. In the file this actually stores the
    /// heliocentric Earth; the true barycentric Sun is computed as EMB − body10.
    /// </summary>
    public const int SeiSunBary = 10;

    // ═══════════════════════════════════════════════════════════════════════
    //  Internal data structures
    // ═══════════════════════════════════════════════════════════════════════

    private sealed class BodyMeta
    {
        public int    SeiId;      // SEI body ID from ipl[] array in the header
        public int    Lndx0;     // file offset of this body's segment index
        public byte   IFlg;      // body flags (FlagHelio / FlagRotate / …)
        public int    Ncoe;      // number of Chebyshev coefficients per coordinate
        public double Rmax;      // normalisation factor in AU (rmax_lng / 1000)

        // Segment boundaries
        public double TfStart;  // body's valid-from JD
        public double TfEnd;    // body's valid-to JD
        public double DSeg;     // segment duration in Julian days

        // Orbital element rates (for rot_back)
        public double TElem;    // reference epoch JD for orbital elements
        public double PROt;     // inclination param p at TElem
        public double DPROt;    // rate of p (radians / Julian millennium)
        public double QROt;     // inclination param q at TElem
        public double DQROt;    // rate of q
        public double Peri;     // argument of perihelion at TElem (radians)
        public double DPeri;    // rate of peri (radians / Julian day)

        // Reference ellipse (present when FlagEllipse is set)
        public double[]? RefEpX;
        public double[]? RefEpY;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Fields
    // ═══════════════════════════════════════════════════════════════════════

    private readonly byte[] _raw;    // entire file content in memory
    private readonly Dictionary<int, BodyMeta> _bodies = new();

    // File-level header fields (informational)
    private readonly int    _deVersion;
    private readonly double _fileStart;
    private readonly double _fileEnd;

    // ═══════════════════════════════════════════════════════════════════════
    //  Constructor / factory
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Opens and parses the header of an SE1 ephemeris file.
    /// The entire file is loaded into memory for fast random access.
    /// </summary>
    /// <param name="filePath">Absolute path to the <c>.se1</c> file.</param>
    /// <exception cref="FileNotFoundException">When the file does not exist.</exception>
    /// <exception cref="InvalidDataException">When the file header is malformed.</exception>
    public Se1EphemerisReader(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"SE1 file not found: {filePath}", filePath);

        _raw = File.ReadAllBytes(filePath);
        (_deVersion, _fileStart, _fileEnd) = ParseHeader();
    }

    /// <summary>Julian Day of the first date covered by this file.</summary>
    public double FileStart => _fileStart;

    /// <summary>Julian Day of the last date covered by this file.</summary>
    public double FileEnd => _fileEnd;

    /// <summary>JPL DE version number embedded in the file (e.g., 431).</summary>
    public int DeVersion => _deVersion;

    /// <summary>SEI body IDs present in this file.</summary>
    public IReadOnlyCollection<int> BodyIds => _bodies.Keys;

    // ═══════════════════════════════════════════════════════════════════════
    //  Public API — position queries
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the barycentric J2000-equatorial Cartesian position (X, Y, Z) in AU
    /// for a given body SEI ID at the specified Julian Day.
    /// </summary>
    /// <param name="seiId">
    /// SEI body ID (e.g., <see cref="SeiJupiter"/>). Use <see cref="SeiSunBary"/> for the
    /// barycentric Sun (computed as EMB − heliocentric-Earth).
    /// </param>
    /// <param name="julianDay">Target Julian Day (ET / TT).</param>
    /// <returns>Position vector as <see cref="CartesianPosition"/> in AU, J2000 equatorial (ICRS).</returns>
    /// <exception cref="ArgumentException">When the SEI ID is not in this file.</exception>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="julianDay"/> is outside the file range.</exception>
    public CartesianPosition GetBarycentricPosition(int seiId, double julianDay)
    {
        if (seiId == SeiSunBary)
            return ComputeBarycentricSun(julianDay);

        return EvaluateBody(seiId, julianDay);
    }

    /// <summary>
    /// Returns the geocentric J2000-equatorial Cartesian position (X, Y, Z) in AU
    /// for a given body, where "geocentric" means relative to the Earth-Moon Barycenter.
    /// </summary>
    /// <param name="seiId">SEI body ID (not EMB itself; use <see cref="GetBarycentricPosition"/> for EMB).</param>
    /// <param name="julianDay">Target Julian Day (ET / TT).</param>
    /// <returns>Geocentric position vector as <see cref="CartesianPosition"/> in AU, J2000 equatorial (ICRS).</returns>
    public CartesianPosition GetGeocentricPosition(int seiId, double julianDay)
    {
        var p = GetBarycentricPosition(seiId, julianDay);
        var e = EvaluateBody(SeiEmb, julianDay);

        return new CartesianPosition(p.X - e.X, p.Y - e.Y, p.Z - e.Z);
    }

    /// <summary>
    /// Converts a geocentric Cartesian position (AU, J2000 equatorial) to
    /// right ascension and declination in decimal degrees.
    /// </summary>
    /// <param name="x">X component in AU.</param>
    /// <param name="y">Y component in AU.</param>
    /// <param name="z">Z component in AU.</param>
    /// <returns>A tuple of (<see cref="EquatorialCoordinates"/> with RA in [0,360) and Dec in [−90,90]) and distance in AU.</returns>
    public static (EquatorialCoordinates Coordinates, double DistanceAu) CartesianToRaDec(double x, double y, double z)
    {
        double r   = Math.Sqrt(x * x + y * y + z * z);
        double ra  = (double.RadiansToDegrees(Math.Atan2(y, x)) + 360.0) % 360.0;
        double dec = (r > 0) ? double.RadiansToDegrees(Math.Asin(z / r)) : 0.0;
        return (new EquatorialCoordinates(ra, dec), r);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Header parsing
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Parses the file header: endianness marker, file-size, DE version, Julian Day range,
    /// body list, CRC, physical constants, and per-body metadata.
    /// </summary>
    private (int deVersion, double fileStart, double fileEnd) ParseHeader()
    {
        // ── Validate endianness marker ────────────────────────────────────────
        // Offset 116: "cba\0" (little-endian) or "abc\0" (big-endian).
        // Only little-endian files are supported; big-endian SE1 files are obsolete.
        if (_raw[HeaderSize + 0] != 'c' || _raw[HeaderSize + 1] != 'b' ||
            _raw[HeaderSize + 2] != 'a' || _raw[HeaderSize + 3] != 0)
            throw new InvalidDataException(
                "SE1 file is not little-endian or has a malformed endianness marker at offset 116.");

        // ── Fixed header fields (offsets 120..143) ────────────────────────────
        // These come immediately after the 4-byte marker.
        int pos = HeaderSize + MarkerSize;

        int    fileSize  = ReadInt32(pos);       pos += 4;   // [120..123] total file size
        int    deVer     = ReadInt32(pos);       pos += 4;   // [124..127] JPL DE version
        double tfStart   = ReadDouble(pos);      pos += 8;   // [128..135] file start JD
        double tfEnd     = ReadDouble(pos);      pos += 8;   // [136..143] file end JD
        _ = fileSize;   // stored but not currently validated

        // ── Body ID list ──────────────────────────────────────────────────────
        // [144..145] int16: number of bodies; [146..] int16[npl]: SEI IDs
        int npl = ReadInt16(pos);               pos += 2;
        int[] iplList = new int[npl];
        for (int k = 0; k < npl; k++)
        {
            iplList[k] = ReadInt16(pos);
            pos += 2;
        }

        // ── CRC32 + physical constants ────────────────────────────────────────
        pos += 4;   // skip CRC32 (4 bytes)
        pos += 40;  // skip 5 × double (clight, aunit, helgravconst, ratme, sunradius)

        // ── Per-body metadata ─────────────────────────────────────────────────
        // Records are tightly packed (no alignment padding).
        for (int k = 0; k < npl; k++)
        {
            var meta = new BodyMeta { SeiId = iplList[k] };

            // lndx0: file offset of this body's segment index
            meta.Lndx0  = ReadInt32(pos);    pos += 4;

            // iflg: bitmask flags (FlagHelio / FlagRotate / FlagEllipse / FlagEmbHel)
            meta.IFlg   = _raw[pos];         pos += 1;

            // ncoe: Chebyshev coefficients per coordinate per segment
            meta.Ncoe   = _raw[pos];         pos += 1;

            // rmax_lng: rmax × 1000; rmax is the AU normalization factor
            meta.Rmax   = ReadInt32(pos) / 1000.0;  pos += 4;

            // 10 doubles: tfstart, tfend, dseg, telem, prot, dprot, qrot, dqrot, peri, dperi
            meta.TfStart = ReadDouble(pos);  pos += 8;
            meta.TfEnd   = ReadDouble(pos);  pos += 8;
            meta.DSeg    = ReadDouble(pos);  pos += 8;
            meta.TElem   = ReadDouble(pos);  pos += 8;
            meta.PROt    = ReadDouble(pos);  pos += 8;
            meta.DPROt   = ReadDouble(pos);  pos += 8;
            meta.QROt    = ReadDouble(pos);  pos += 8;
            meta.DQROt   = ReadDouble(pos);  pos += 8;
            meta.Peri    = ReadDouble(pos);  pos += 8;
            meta.DPeri   = ReadDouble(pos);  pos += 8;

            // Reference ellipse: present when FlagEllipse is set.
            // Stored as 2 × ncoe raw doubles (not integer-packed like segment data).
            if ((meta.IFlg & FlagEllipse) != 0)
            {
                meta.RefEpX = new double[meta.Ncoe];
                meta.RefEpY = new double[meta.Ncoe];
                for (int i = 0; i < meta.Ncoe; i++)
                {
                    meta.RefEpX[i] = ReadDouble(pos);  pos += 8;
                }
                for (int i = 0; i < meta.Ncoe; i++)
                {
                    meta.RefEpY[i] = ReadDouble(pos);  pos += 8;
                }
            }

            _bodies[meta.SeiId] = meta;
        }

        return (deVer, tfStart, tfEnd);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Core evaluation pipeline
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Evaluates the Chebyshev series for a body at the given Julian Day and
    /// returns its J2000-equatorial Cartesian position in AU.
    /// </summary>
    private CartesianPosition EvaluateBody(int seiId, double julianDay)
    {
        if (!_bodies.TryGetValue(seiId, out BodyMeta? meta))
            throw new ArgumentException(
                $"Body with SEI ID {seiId} is not in this SE1 file.", nameof(seiId));

        if (julianDay < meta.TfStart || julianDay > meta.TfEnd)
            throw new ArgumentOutOfRangeException(
                nameof(julianDay),
                $"Julian Day {julianDay} is outside this body's range [{meta.TfStart}, {meta.TfEnd}].");

        // ── Step 1: Locate the segment for this Julian Day ────────────────────
        // Each body's segments are equal-length (dseg days). Integer division gives
        // the zero-based segment index.
        int    iseg  = (int)((julianDay - meta.TfStart) / meta.DSeg);
        double tseg0 = meta.TfStart + iseg * meta.DSeg;   // segment start JD
        // double tseg1 = tseg0 + meta.DSeg;                // segment end JD (not needed)

        // ── Step 2: Get the file offset of this segment's data ────────────────
        // The segment index at lndx0 contains nndx entries of 3 bytes each (LE int24).
        int idxPos  = meta.Lndx0 + iseg * 3;
        int segFpos = _raw[idxPos] | (_raw[idxPos + 1] << 8) | (_raw[idxPos + 2] << 16);

        // ── Step 3: Read and unpack integer-packed Chebyshev coefficients ─────
        // Coefficients for X, Y, Z are packed sequentially (all X, then Y, then Z).
        double[][] rawCoeffs = ReadPackedCoefficients(segFpos, meta.Ncoe, meta.Rmax);

        // ── Step 4: Apply rot_back if the body uses orbital-frame storage ─────
        // All bodies in sepl_NN.se1 have FlagRotate set.
        // rot_back:
        //   a) Optionally adds the reference ellipse (main Keplerian orbit) to the
        //      small-perturbation coefficients already read.
        //   b) Rotates each Chebyshev coefficient vector from the body's equinoctal
        //      orbital frame to J2000 equatorial (ICRS) coordinates.
        double[][] rotatedCoeffs = (meta.IFlg & FlagRotate) != 0
            ? RotateCoefficientsBack(rawCoeffs, meta, tseg0)
            : rawCoeffs;

        // ── Step 5: Evaluate the Chebyshev polynomial ─────────────────────────
        // Map tjd from [tseg0, tseg0+dseg] → [−1, +1] for Chebyshev evaluation.
        double tNorm = (julianDay - tseg0) / meta.DSeg;   // 0 → 1 across segment
        double t     = tNorm * 2.0 - 1.0;                 // −1 → +1

        int ncoe = meta.Ncoe;
        double x = EvalChebyshev(t, rotatedCoeffs[0], ncoe);
        double y = EvalChebyshev(t, rotatedCoeffs[1], ncoe);
        double z = EvalChebyshev(t, rotatedCoeffs[2], ncoe);

        return new CartesianPosition(x, y, z);
    }

    /// <summary>
    /// Computes the barycentric Sun position as EMB − heliocentric-Earth.
    /// </summary>
    /// <remarks>
    /// In SE1 planet files the slot labelled <see cref="SeiSunBary"/> (body 10) actually
    /// stores the HELIOCENTRIC Earth, not the barycentric Sun. The true barycentric Sun
    /// is derived by: BarycentricSun = EMB − HelioCentricEarth.
    /// </remarks>
    private CartesianPosition ComputeBarycentricSun(double julianDay)
    {
        var e = EvaluateBody(SeiEmb, julianDay);
        var h = EvaluateBody(SeiSunBary, julianDay);
        return new CartesianPosition(e.X - h.X, e.Y - h.Y, e.Z - h.Z);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Coefficient unpacking
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reads the integer-packed Chebyshev coefficients from the raw file bytes.
    /// Returns a 3-element array [X-coeffs, Y-coeffs, Z-coeffs], each with <paramref name="ncoe"/> values.
    /// </summary>
    /// <param name="fpos">File offset of the coefficient block.</param>
    /// <param name="ncoe">Number of coefficients per coordinate axis.</param>
    /// <param name="rmax">Normalisation factor in AU.</param>
    private double[][] ReadPackedCoefficients(int fpos, int ncoe, double rmax)
    {
        double[][] coeffs = [new double[ncoe], new double[ncoe], new double[ncoe]];

        int p = fpos;

        // Coefficients are packed identically for each coordinate axis.
        for (int coord = 0; coord < 3; coord++)
        {
            // ── Packing header ────────────────────────────────────────────────
            // The header is 2 bytes normally, or 4 bytes when c[0] bit 7 is set.
            // Each nibble of the header bytes gives the count for one precision group:
            //   group 0: 4-byte integers (highest precision)
            //   group 1: 3-byte integers
            //   group 2: 2-byte integers
            //   group 3: 1-byte integers
            //   group 4: half-nibble (4-bit) — two values per byte
            //   group 5: quarter-nibble (2-bit) — four values per byte
            byte c0 = _raw[p]; byte c1 = _raw[p + 1];
            int[] nsize;
            bool sixGroups = (c0 & 0x80) != 0;

            if (sixGroups)
            {
                // 4-byte header: c0 is only a flag byte (bit 7 set).
                // Group counts are packed into c1, c2, c3:
                //   c1 high nibble → group 0 (4-byte integers)
                //   c1 low  nibble → group 1 (3-byte integers)
                //   c2 high nibble → group 2 (2-byte integers)
                //   c2 low  nibble → group 3 (1-byte integers)
                //   c3 high nibble → group 4 (half-nibble, 4-bit)
                //   c3 low  nibble → group 5 (quarter-nibble, 2-bit)
                byte c2 = _raw[p + 2]; byte c3 = _raw[p + 3];
                nsize = [
                    c1 >> 4,             // group 0: 4-byte integers
                    c1 & 0x0F,           // group 1: 3-byte integers
                    c2 >> 4,             // group 2: 2-byte integers
                    c2 & 0x0F,           // group 3: 1-byte integers
                    c3 >> 4,             // group 4: half-nibble (4-bit)
                    c3 & 0x0F,           // group 5: quarter-nibble (2-bit)
                ];
                p += 4;
            }
            else
            {
                nsize = [
                    c0 >> 4,             // group 0: 4-byte integers
                    c0 & 0x0F,           // group 1: 3-byte integers
                    c1 >> 4,             // group 2: 2-byte integers
                    c1 & 0x0F,           // group 3: 1-byte integers
                ];
                p += 2;
            }

            // ── Decode each group ─────────────────────────────────────────────
            int idbl = 0;   // running index into coeffs[coord]

            for (int gi = 0; gi < nsize.Length; gi++)
            {
                int count = nsize[gi];
                if (count == 0) continue;

                if (gi < 4)
                {
                    // Groups 0–3: fixed-width integer bytes (4, 3, 2, 1 bytes per value).
                    int byteWidth = 4 - gi;
                    for (int m = 0; m < count && idbl < ncoe; m++)
                    {
                        // Read little-endian unsigned integer.
                        uint packed = 0;
                        for (int b = 0; b < byteWidth; b++)
                            packed |= (uint)_raw[p + b] << (b * 8);
                        p += byteWidth;

                        // Decode: LSB encodes the sign; remaining bits encode magnitude.
                        // coefficient = ±(magnitude / 2) / 1e9 × rmax / 2
                        coeffs[coord][idbl++] = UnpackInteger(packed, rmax);
                    }
                }
                else if (gi == 4)
                {
                    // Group 4: 4-bit (half-nibble) values, two per byte, high nibble first.
                    // Sign bit is bit 3 of the nibble; bits 2..0 are the magnitude.
                    int bytesNeeded = (count + 1) / 2;
                    for (int byteIdx = 0; byteIdx < bytesNeeded && idbl < ncoe; byteIdx++)
                    {
                        byte b = _raw[p++];
                        for (int nibIdx = 0; nibIdx < 2 && idbl < ncoe; nibIdx++)
                        {
                            int nib = nibIdx == 0 ? b >> 4 : b & 0x0F;
                            coeffs[coord][idbl++] = UnpackNibble(nib, rmax);
                        }
                    }
                }
                else // gi == 5
                {
                    // Group 5: 2-bit (quarter-nibble) values, four per byte, high pair first.
                    // Sign bit is bit 1; bit 0 is the magnitude.
                    int bytesNeeded = (count + 3) / 4;
                    for (int byteIdx = 0; byteIdx < bytesNeeded && idbl < ncoe; byteIdx++)
                    {
                        byte b = _raw[p++];
                        for (int pairIdx = 0; pairIdx < 4 && idbl < ncoe; pairIdx++)
                        {
                            int bits = (b >> (6 - pairIdx * 2)) & 0x03;
                            coeffs[coord][idbl++] = UnpackBitPair(bits, rmax);
                        }
                    }
                }
            }
        }

        return coeffs;
    }

    /// <summary>
    /// Decodes an integer-packed Chebyshev coefficient value.
    /// The least-significant bit encodes the sign; the remaining bits encode the magnitude.
    /// </summary>
    /// <remarks>
    /// Encoding convention (applies identically to 4-, 3-, 2-, and 1-byte integers):
    /// <list type="bullet">
    ///   <item>LSB = 0 → positive:  coefficient = (packed / 2) / 1e9 × rmax / 2</item>
    ///   <item>LSB = 1 → negative:  coefficient = −((packed + 1) / 2) / 1e9 × rmax / 2</item>
    /// </list>
    /// This is equivalent to:  magnitude = (packed >> 1) + (is_negative ? 1 : 0)
    /// </remarks>
    /// <param name="packed">Unsigned integer read from the file.</param>
    /// <param name="rmax">Body-specific normalisation factor in AU.</param>
    private static double UnpackInteger(uint packed, double rmax)
    {
        bool   negative     = (packed & 1) == 1;
        // For negative (odd packed): magnitude = (packed + 1) / 2 = (packed >> 1) + 1
        // For positive (even packed): magnitude = packed / 2 = packed >> 1
        double magnitudeInt = negative ? (packed >> 1) + 1.0 : (double)(packed >> 1);
        double value        = magnitudeInt / 1e9 * rmax / 2.0;
        return negative ? -value : value;
    }

    /// <summary>
    /// Decodes a 4-bit nibble-packed coefficient.
    /// Bit 3 (0x08) is the sign; the magnitude uses the full 4-bit value shifted right.
    /// </summary>
    /// <remarks>
    /// Encoding (same convention as full-width integers, applied to 4 bits):
    /// <list type="bullet">
    ///   <item>nib &amp; 0x08 == 0 (positive): magnitude = nib >> 1  → values {0,0,1,1,2,2,3,3}</item>
    ///   <item>nib &amp; 0x08 != 0 (negative): magnitude = (nib >> 1) + 1  → values {5,5,6,6,7,7,8,8}</item>
    /// </list>
    /// Note: values 4 and −4 are not representable in this scheme.
    /// </remarks>
    private static double UnpackNibble(int nib, double rmax)
    {
        bool   negative     = (nib & 0x08) != 0;
        // Use the FULL nib value (not masked to lower 3 bits) to match Python's nib//2 formula.
        double magnitudeInt = negative ? (nib >> 1) + 1.0 : (double)(nib >> 1);
        double value        = magnitudeInt / 1e9 * rmax / 2.0;
        return negative ? -value : value;
    }

    /// <summary>
    /// Decodes a 2-bit quarter-nibble packed coefficient.
    /// Bit 1 (0x02) is the sign; the magnitude uses the full 2-bit value.
    /// </summary>
    /// <remarks>
    /// Encoding:
    /// <list type="bullet">
    ///   <item>bits &amp; 0x02 == 0 (positive): magnitude = bits >> 1  (0 or 0)</item>
    ///   <item>bits &amp; 0x02 != 0 (negative): magnitude = (bits >> 1) + 1  (2 or 2)</item>
    /// </list>
    /// </remarks>
    private static double UnpackBitPair(int bits, double rmax)
    {
        bool   negative     = (bits & 0x02) != 0;
        double magnitudeInt = negative ? (bits >> 1) + 1.0 : (double)(bits >> 1);
        double value        = magnitudeInt / 1e9 * rmax / 2.0;
        return negative ? -value : value;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  rot_back: orbital frame → J2000 equatorial rotation
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Applies the <c>rot_back</c> algorithm to the raw Chebyshev coefficients:
    /// <list type="number">
    ///   <item>Optionally adds the reference ellipse (the main Keplerian orbit encoded as a
    ///         Chebyshev series) to the perturbation coefficients already read from the segment.</item>
    ///   <item>Rotates each coefficient vector from the body's equinoctal orbital frame to
    ///         J2000 equatorial (ICRS) Cartesian coordinates.</item>
    /// </list>
    /// </summary>
    /// <param name="rawCoeffs">Unpacked perturbation coefficients [X, Y, Z][0..ncoe-1].</param>
    /// <param name="meta">Body metadata (orbital element rates, reference ellipse).</param>
    /// <param name="tseg0">Segment start Julian Day.</param>
    /// <returns>Rotated coefficient arrays [X, Y, Z][0..ncoe-1] in J2000 equatorial AU.</returns>
    private static double[][] RotateCoefficientsBack(
        double[][] rawCoeffs, BodyMeta meta, double tseg0)
    {
        int ncoe = meta.Ncoe;

        // Working copies so we can modify without affecting the originals.
        double[] cx = (double[])rawCoeffs[0].Clone();
        double[] cy = (double[])rawCoeffs[1].Clone();
        double[] cz = (double[])rawCoeffs[2].Clone();

        // ── Step 1: Compute orbital element rates at the segment midpoint ─────
        //
        // The MIDPOINT of the segment is used as the reference epoch for evaluating
        // the slowly-varying orbital elements.  The difference from the stored
        // reference epoch (telem) is expressed in Julian millennia.
        double tMid  = tseg0 + meta.DSeg / 2.0;
        double tDiff = (tMid - meta.TElem) / 365250.0;   // Julian millennia

        double qav = meta.QROt + tDiff * meta.DQROt;
        double pav = meta.PROt + tDiff * meta.DPROt;

        // ── Step 2: Add reference ellipse if FlagEllipse is set ───────────────
        //
        // The reference ellipse encodes the main Keplerian orbit as a Chebyshev
        // series split into X and Y components (in the orbital frame).  Before
        // adding, the reference ellipse is rotated by the current argument of
        // perihelion (omtild) so the orbit lines up with the body's actual
        // perihelion direction at this epoch.
        if ((meta.IFlg & FlagEllipse) != 0 && meta.RefEpX is not null && meta.RefEpY is not null)
        {
            // Argument of perihelion at segment midpoint.
            double omtild = meta.Peri + tDiff * meta.DPeri;

            // Normalise to [0, 2π).
            omtild -= Math.Floor(omtild / TwoPI) * TwoPI;

            double com = Math.Cos(omtild);
            double som = Math.Sin(omtild);

            for (int i = 0; i < ncoe; i++)
            {
                double rx = meta.RefEpX[i];
                double ry = meta.RefEpY[i];
                // Rotate the reference ellipse vector by omtild and add to the perturbation.
                cx[i] += com * rx - som * ry;
                cy[i] += com * ry + som * rx;
                // cz stays as-is (out-of-plane component is in the perturbation only).
            }
        }

        // ── Step 3: Build the equinoctal rotation matrix ──────────────────────
        //
        // The equinoctal elements (p, q) parameterise the orbital plane's orientation
        // relative to the J2000 ecliptic.  Together they define a 3×3 rotation matrix
        // that maps orbital-frame coordinates to J2000 equatorial (ICRS).
        //
        // The matrix columns are the unit vectors uix, uiy, uiz — constructed as:
        double cosih2 = 1.0 / (1.0 + qav * qav + pav * pav);

        Span<double> uix = [(1.0 + qav * qav - pav * pav) * cosih2,
                             2.0 * qav * pav * cosih2,
                            -2.0 * pav * cosih2];

        Span<double> uiy = [2.0 * qav * pav * cosih2,
                            (1.0 - qav * qav + pav * pav) * cosih2,
                             2.0 * qav * cosih2];

        Span<double> uiz = [2.0 * pav * cosih2,
                            -2.0 * qav * cosih2,
                            (1.0 - qav * qav - pav * pav) * cosih2];

        // ── Step 4: Apply the rotation to every Chebyshev coefficient ────────
        //
        // Each coefficient triple (cx[i], cy[i], cz[i]) is a 3-D vector in the
        // orbital frame.  Multiplying by the rotation matrix gives the same
        // coefficient but in J2000 equatorial coordinates.
        double[] rx2 = new double[ncoe];
        double[] ry2 = new double[ncoe];
        double[] rz2 = new double[ncoe];

        for (int i = 0; i < ncoe; i++)
        {
            double vx = cx[i], vy = cy[i], vz = cz[i];
            rx2[i] = vx * uix[0] + vy * uiy[0] + vz * uiz[0];
            ry2[i] = vx * uix[1] + vy * uiy[1] + vz * uiz[1];
            rz2[i] = vx * uix[2] + vy * uiy[2] + vz * uiz[2];
        }

        return [rx2, ry2, rz2];
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Chebyshev evaluation — Clenshaw-Curtis normalization
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Evaluates a Chebyshev series at <paramref name="x"/> using
    /// Clenshaw-Curtis normalization:
    /// <code>
    ///   f(x) = c[0]/2 + c[1]·T₁(x) + c[2]·T₂(x) + …
    /// </code>
    /// The crucial difference from the textbook Clenshaw recursion is the final <c>× 0.5</c>.
    /// </summary>
    /// <param name="x">Normalised time in [−1, 1].</param>
    /// <param name="coef">Chebyshev coefficient array (c[0] = T₀ term, c[1] = T₁ term, …).</param>
    /// <param name="ncf">Number of coefficients to use (may be less than coef.Length).</param>
    /// <returns>Interpolated position in AU.</returns>
    private static double EvalChebyshev(double x, double[] coef, int ncf)
    {
        // Clenshaw downward recursion for Chebyshev polynomials.
        // Working variables:
        //   br   = current accumulator
        //   brpp = br from two iterations ago
        //   brp2 = br from one iteration ago (kept for the final subtraction)
        double x2   = x * 2.0;
        double br   = 0.0;
        double brpp = 0.0;
        double brp2 = 0.0;

        for (int j = ncf - 1; j >= 0; j--)
        {
            brp2 = brpp;
            brpp = br;
            br   = x2 * brpp - brp2 + coef[j];
        }

        // Returns (br − brp2) × 0.5, implementing the c[0]/2 normalization
        // of the zeroth Chebyshev coefficient.
        return (br - brp2) * 0.5;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Binary reading helpers (little-endian)
    // ═══════════════════════════════════════════════════════════════════════

    private int ReadInt32(int offset) =>
        _raw[offset]
        | (_raw[offset + 1] << 8)
        | (_raw[offset + 2] << 16)
        | (_raw[offset + 3] << 24);

    private int ReadInt16(int offset) =>
        (short)(_raw[offset] | (_raw[offset + 1] << 8));

    private double ReadDouble(int offset) =>
        BitConverter.ToDouble(_raw.AsSpan(offset, 8));

    // ═══════════════════════════════════════════════════════════════════════
    //  IDisposable
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Releases any managed resources. The file is fully in memory; there is no
    /// open file handle to release.
    /// </summary>
    public void Dispose() { /* nothing to release */ }
}

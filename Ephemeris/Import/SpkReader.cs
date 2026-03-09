// Updated: 2026-03-09
namespace Ephemeris.Import;

/// <summary>
/// Represents one indexed segment from a loaded DAF/SPK file.
/// </summary>
/// <param name="StartEt">Start epoch (ET seconds past J2000.0 TDB).</param>
/// <param name="EndEt">End epoch (ET seconds past J2000.0 TDB).</param>
/// <param name="TargetId">NAIF target body identifier.</param>
/// <param name="CenterId">NAIF center body identifier.</param>
/// <param name="FrameId">NAIF reference frame identifier (1 = J2000/ICRF).</param>
/// <param name="DataType">SPK data type (2 = Chebyshev position, 3 = Chebyshev position + velocity).</param>
/// <param name="BeginAddress">First DAF address (1-based double-word index) of segment data.</param>
/// <param name="EndAddress">Last DAF address (1-based, inclusive) of segment data.</param>
/// <param name="FilePath">Path to the SPK file containing this segment.</param>
internal record SpkSegmentInfo(
    double StartEt, double EndEt,
    int TargetId, int CenterId, int FrameId, int DataType,
    long BeginAddress, long EndAddress,
    string FilePath);

/// <summary>
/// Reads NAIF DAF/SPK binary kernel files and evaluates Chebyshev polynomial
/// ephemeris data (SPK Types 2 and 3) to compute body positions.
/// </summary>
/// <remarks>
/// <para>
/// The DAF (Double-precision Array File) format stores segment descriptors in
/// summary records and segment data in contiguous double-precision arrays.
/// Each SPK segment contains Chebyshev coefficients for X, Y, Z position
/// (and optionally velocity for Type 3) over a set of sub-intervals.
/// </para>
/// <para>
/// This reader supports both little-endian (<c>LTL-IEEE</c>) and big-endian
/// (<c>BIG-IEEE</c>) DAF files. Byte swapping is applied transparently when
/// the file endianness differs from the host.
/// </para>
/// <para>
/// Body chaining through the Solar System Barycenter (SSB, ID 0) is used when
/// no direct segment connects the requested target and center.
/// </para>
/// </remarks>
public sealed class SpkReader : IDisposable
{
    /// <summary>Expected magic bytes at offset 0 of every DAF file.</summary>
    private const string DafMagic = "NAIF/DAF";

    /// <summary>Little-endian format identifier.</summary>
    private const string LtlIeee = "LTL-IEEE";

    /// <summary>Big-endian format identifier.</summary>
    private const string BigIeee = "BIG-IEEE";

    /// <summary>Bytes per DAF record (128 double-precision words × 8 bytes).</summary>
    private const int RecordBytes = 1024;

    private readonly FileStream _stream;
    private readonly BinaryReader _reader;
    private readonly bool _needSwap;
    private readonly List<SpkSegmentInfo> _segments = [];
    private readonly string _filePath;
    private bool _disposed;

    /// <summary>
    /// Opens a DAF/SPK file, validates the header, and indexes all segment descriptors.
    /// </summary>
    /// <param name="filePath">Absolute or relative path to the <c>.bsp</c> kernel file.</param>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file is not a valid DAF/SPK file.</exception>
    public SpkReader(string filePath)
    {
        _filePath = Path.GetFullPath(filePath);

        if (!File.Exists(_filePath))
            throw new FileNotFoundException("SPK kernel file not found.", _filePath);

        _stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _reader = new BinaryReader(_stream);

        // --- File Record (record 1, bytes 0..1023) ---
        byte[] fileRecord = _reader.ReadBytes(RecordBytes);

        string locidw = System.Text.Encoding.ASCII.GetString(fileRecord, 0, 8).TrimEnd();
        if (locidw != DafMagic)
            throw new InvalidDataException($"Not a DAF file: expected '{DafMagic}', got '{locidw}'.");

        string locfmt = System.Text.Encoding.ASCII.GetString(fileRecord, 88, 8).TrimEnd();
        _needSwap = locfmt switch
        {
            LtlIeee => !BitConverter.IsLittleEndian,  // file is LE, host is BE → swap
            BigIeee => BitConverter.IsLittleEndian,    // file is BE, host is LE → swap
            _ => throw new InvalidDataException($"Unknown DAF format: '{locfmt}'."),
        };

        int nd = ReadInt32(fileRecord, 8);    // number of doubles per descriptor
        int ni = ReadInt32(fileRecord, 12);   // number of integers per descriptor
        int fward = ReadInt32(fileRecord, 76); // first summary record (1-based record number)

        if (nd != 2 || ni != 6)
            throw new InvalidDataException($"Expected SPK descriptor shape ND=2 NI=6, got ND={nd} NI={ni}.");

        int descriptorBytes = nd * 8 + ni * 4;  // 40 bytes for SPK

        // Walk the summary record chain
        int currentRecord = fward;
        while (currentRecord > 0)
        {
            SeekToRecord(currentRecord);
            byte[] sumRec = _reader.ReadBytes(RecordBytes);

            double nextDbl = ReadDouble(sumRec, 0);
            double nSumDbl = ReadDouble(sumRec, 16);
            int next = (int)nextDbl;
            int nSum = (int)nSumDbl;

            int offset = 24; // descriptors start after NEXT, PREV, NSUM (3 doubles = 24 bytes)
            for (int i = 0; i < nSum; i++)
            {
                double startEt = ReadDouble(sumRec, offset);
                double endEt = ReadDouble(sumRec, offset + 8);
                int target = ReadInt32(sumRec, offset + 16);
                int center = ReadInt32(sumRec, offset + 20);
                int frame = ReadInt32(sumRec, offset + 24);
                int dtype = ReadInt32(sumRec, offset + 28);
                int begin = ReadInt32(sumRec, offset + 32);
                int end = ReadInt32(sumRec, offset + 36);

                _segments.Add(new SpkSegmentInfo(
                    startEt, endEt, target, center, frame, dtype,
                    begin, end, _filePath));

                offset += descriptorBytes;
            }

            currentRecord = next;
        }
    }

    /// <summary>
    /// Gets the number of segment descriptors indexed from this SPK file.
    /// </summary>
    public int SegmentCount => _segments.Count;

    /// <summary>
    /// Computes the Cartesian position [x, y, z] in kilometres for a target body
    /// relative to a center body at the given ephemeris time.
    /// </summary>
    /// <param name="targetNaifId">NAIF integer ID of the target body.</param>
    /// <param name="et">Ephemeris time in seconds past J2000.0 TDB.</param>
    /// <param name="centerNaifId">NAIF integer ID of the center body (default 0 = SSB).</param>
    /// <returns>A three-element array <c>[x, y, z]</c> in kilometres (ICRF/J2000).</returns>
    /// <exception cref="InvalidOperationException">No matching segment is found.</exception>
    public double[] GetPosition(int targetNaifId, double et, int centerNaifId = 0)
    {
        // Try direct segment lookup first
        var seg = FindSegment(targetNaifId, centerNaifId, et);
        if (seg is not null)
            return EvaluateSegment(seg, et);

        // Try reverse direction
        var segReverse = FindSegment(centerNaifId, targetNaifId, et);
        if (segReverse is not null)
        {
            var pos = EvaluateSegment(segReverse, et);
            return [-pos[0], -pos[1], -pos[2]];
        }

        // Chain through SSB (body 0)
        if (centerNaifId != 0)
        {
            var segTarget = FindSegment(targetNaifId, 0, et);
            var segCenter = FindSegment(centerNaifId, 0, et);
            if (segTarget is not null && segCenter is not null)
            {
                var posT = EvaluateSegment(segTarget, et);
                var posC = EvaluateSegment(segCenter, et);
                return [posT[0] - posC[0], posT[1] - posC[1], posT[2] - posC[2]];
            }
        }

        throw new InvalidOperationException(
            $"No SPK segment found for target={targetNaifId} center={centerNaifId} at ET={et:F3}.");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _reader.Dispose();
            _stream.Dispose();
            _disposed = true;
        }
    }

    // -----------------------------------------------------------------------
    // NAIF Body ID resolution
    // -----------------------------------------------------------------------

    /// <summary>
    /// Common NAIF body name → ID mapping (case-insensitive).
    /// </summary>
    private static readonly Dictionary<string, int> BodyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SSB"]                          = 0,
        ["SOLAR_SYSTEM_BARYCENTER"]      = 0,
        ["SOLAR SYSTEM BARYCENTER"]      = 0,
        ["MERCURY_BARYCENTER"]           = 1,
        ["MERCURY BARYCENTER"]           = 1,
        ["VENUS_BARYCENTER"]             = 2,
        ["VENUS BARYCENTER"]             = 2,
        ["EMB"]                          = 3,
        ["EARTH_MOON_BARYCENTER"]        = 3,
        ["EARTH-MOON BARYCENTER"]        = 3,
        ["EARTH MOON BARYCENTER"]        = 3,
        ["MARS_BARYCENTER"]              = 4,
        ["MARS BARYCENTER"]              = 4,
        ["JUPITER_BARYCENTER"]           = 5,
        ["JUPITER BARYCENTER"]           = 5,
        ["SATURN_BARYCENTER"]            = 6,
        ["SATURN BARYCENTER"]            = 6,
        ["URANUS_BARYCENTER"]            = 7,
        ["URANUS BARYCENTER"]            = 7,
        ["NEPTUNE_BARYCENTER"]           = 8,
        ["NEPTUNE BARYCENTER"]           = 8,
        ["PLUTO_BARYCENTER"]             = 9,
        ["PLUTO BARYCENTER"]             = 9,
        ["SUN"]                          = 10,
        ["MERCURY"]                      = 199,
        ["VENUS"]                        = 299,
        ["EARTH"]                        = 399,
        ["MOON"]                         = 301,
        ["MARS"]                         = 499,
        ["JUPITER"]                      = 599,
        ["SATURN"]                       = 699,
        ["URANUS"]                       = 799,
        ["NEPTUNE"]                      = 899,
        ["PLUTO"]                        = 999,
    };

    /// <summary>
    /// Resolves a NAIF body name (case-insensitive) or numeric string to its integer ID.
    /// </summary>
    /// <param name="nameOrId">Body name (e.g. <c>"SUN"</c>) or numeric ID string (e.g. <c>"10"</c>).</param>
    /// <returns>The NAIF integer body identifier.</returns>
    /// <exception cref="ArgumentException">The name is not recognized and is not a valid integer.</exception>
    public static int ResolveBodyId(string nameOrId)
    {
        if (BodyNames.TryGetValue(nameOrId, out int id))
            return id;

        if (int.TryParse(nameOrId, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out int parsed))
            return parsed;

        throw new ArgumentException($"Unknown NAIF body name: '{nameOrId}'.", nameof(nameOrId));
    }

    // -----------------------------------------------------------------------
    // Segment lookup
    // -----------------------------------------------------------------------

    /// <summary>Finds the first segment matching target, center, and time.</summary>
    private SpkSegmentInfo? FindSegment(int target, int center, double et)
    {
        foreach (var seg in _segments)
        {
            if (seg.TargetId == target && seg.CenterId == center &&
                et >= seg.StartEt && et <= seg.EndEt)
            {
                return seg;
            }
        }

        return null;
    }

    // -----------------------------------------------------------------------
    // Segment evaluation (SPK Types 2 and 3)
    // -----------------------------------------------------------------------

    /// <summary>Evaluates Chebyshev position from a Type 2 or Type 3 segment.</summary>
    private double[] EvaluateSegment(SpkSegmentInfo seg, double et)
    {
        if (seg.DataType is not (2 or 3))
            throw new NotSupportedException($"SPK data type {seg.DataType} is not supported (only Types 2 and 3).");

        // Read the 4-double directory at the end of the segment
        long dirAddr = seg.EndAddress - 3; // 1-based address of INIT
        SeekToAddress(dirAddr);
        double init = ReadNextDouble();
        double intLen = ReadNextDouble();
        double rSizeDbl = ReadNextDouble();
        double nDbl = ReadNextDouble();
        int rSize = (int)rSizeDbl;
        int n = (int)nDbl;

        // Number of Chebyshev coefficients per component
        int nCoef = seg.DataType == 2
            ? (rSize - 2) / 3
            : (rSize - 2) / 6;

        // Locate the correct Chebyshev record
        int recordIndex = (int)Math.Floor((et - init) / intLen);
        recordIndex = Math.Clamp(recordIndex, 0, n - 1);

        long recordAddr = seg.BeginAddress + (long)recordIndex * rSize;
        SeekToAddress(recordAddr);

        double[] coeffs = new double[rSize];
        for (int i = 0; i < rSize; i++)
            coeffs[i] = ReadNextDouble();

        double mid = coeffs[0];
        double radius = coeffs[1];

        // Normalized time in [-1, 1]
        double xNorm = (et - mid) / radius;

        // Chebyshev coefficients start at index 2 in the record
        double x = EvalChebyshevSpk(xNorm, coeffs, nCoef, 2);
        double y = EvalChebyshevSpk(xNorm, coeffs, nCoef, 2 + nCoef);
        double z = EvalChebyshevSpk(xNorm, coeffs, nCoef, 2 + 2 * nCoef);

        return [x, y, z];
    }

    /// <summary>
    /// Evaluates a Chebyshev polynomial using standard Clenshaw recurrence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Computes P(x) = Σ c[k]·T_k(x) for k = 0..nCoef-1 where T_k is the
    /// k-th Chebyshev polynomial of the first kind.
    /// </para>
    /// <para>
    /// This is the <b>standard</b> convention (c[0] is NOT halved), which
    /// differs from the Clenshaw-Curtis half-normalization used in
    /// <c>Se1EphemerisReader.EvalChebyshev</c>.
    /// </para>
    /// </remarks>
    /// <param name="x">Normalized time in [−1, 1].</param>
    /// <param name="coeffs">Coefficient array (may contain other data; <paramref name="offset"/> selects the start).</param>
    /// <param name="nCoef">Number of Chebyshev coefficients to evaluate.</param>
    /// <param name="offset">Starting index in <paramref name="coeffs"/> for c[0].</param>
    /// <returns>The evaluated polynomial value.</returns>
    private static double EvalChebyshevSpk(double x, double[] coeffs, int nCoef, int offset)
    {
        if (nCoef == 1)
            return coeffs[offset];  // T₀(x) = 1, so P = c[0]

        double x2 = 2.0 * x;
        double dCurrent = 0.0;  // b[j+1] in Clenshaw notation
        double dPrev = 0.0;     // b[j+2] in Clenshaw notation

        // Downward recurrence from k = nCoef-1 down to k = 1:
        // b[k] = c[k] + 2x·b[k+1] − b[k+2]
        for (int k = nCoef - 1; k >= 1; k--)
        {
            double dNext = coeffs[offset + k] + x2 * dCurrent - dPrev;
            dPrev = dCurrent;
            dCurrent = dNext;
        }

        // Final: P(x) = c[0] + x·b[1] − b[2]
        return coeffs[offset] + x * dCurrent - dPrev;
    }

    // -----------------------------------------------------------------------
    // Binary I/O helpers (endian-aware)
    // -----------------------------------------------------------------------

    /// <summary>Seeks the file stream to the start of a 1-based record number.</summary>
    private void SeekToRecord(int recordNumber) =>
        _stream.Seek((long)(recordNumber - 1) * RecordBytes, SeekOrigin.Begin);

    /// <summary>Seeks the file stream to a 1-based DAF double-word address.</summary>
    private void SeekToAddress(long address) =>
        _stream.Seek((address - 1) * 8, SeekOrigin.Begin);

    /// <summary>Reads the next double-precision value from the current stream position.</summary>
    private double ReadNextDouble()
    {
        byte[] buf = _reader.ReadBytes(8);
        if (_needSwap) Array.Reverse(buf);
        return BitConverter.ToDouble(buf, 0);
    }

    /// <summary>Reads a 32-bit integer from a byte buffer at the given offset.</summary>
    private int ReadInt32(byte[] buffer, int offset)
    {
        byte[] buf = new byte[4];
        Buffer.BlockCopy(buffer, offset, buf, 0, 4);
        if (_needSwap) Array.Reverse(buf);
        return BitConverter.ToInt32(buf, 0);
    }

    /// <summary>Reads a 64-bit double from a byte buffer at the given offset.</summary>
    private double ReadDouble(byte[] buffer, int offset)
    {
        byte[] buf = new byte[8];
        Buffer.BlockCopy(buffer, offset, buf, 0, 8);
        if (_needSwap) Array.Reverse(buf);
        return BitConverter.ToDouble(buf, 0);
    }
}

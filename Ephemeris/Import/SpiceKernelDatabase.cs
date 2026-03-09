namespace Ephemeris.Import;

/// <summary>
/// Manages loaded SPICE kernels and provides ephemeris time conversion and state vector queries.
/// </summary>
public class SpiceKernelDatabase
{
    private readonly List<string> _loadedKernels = [];
    private readonly List<string> _furnshStatements = [];
    private readonly ISpaceKernelProvider _kernelProvider;
    private readonly ITimeConverter _timeConverter;
    private readonly IStateVectorProvider _stateProvider;

    /// <summary>
    /// Initializes a new instance using the default SPICE provider implementations.
    /// </summary>
    public SpiceKernelDatabase()
    {
        var loadedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _kernelProvider = new DefaultKernelProvider(loadedPaths);
        _timeConverter = new DefaultTimeConverter();
        _stateProvider = new DefaultStateVectorProvider(loadedPaths);
    }

    /// <summary>
    /// Loads a SPICE kernel file (.bsp/.bpc) into the kernel database.
    /// </summary>
    /// <param name="kernelPath">Absolute path to the SPICE kernel file.</param>
    /// <exception cref="FileNotFoundException">Thrown when the kernel file does not exist.</exception>
    public void LoadKernel(string kernelPath)
    {
        if (!_loadedKernels.Contains(kernelPath))
        {
            if (!File.Exists(kernelPath))
            {
                throw new FileNotFoundException("SPICE kernel not found", kernelPath);
            }

            _furnshStatements.Add($"furnsh '{kernelPath}'");
            _loadedKernels.Add(kernelPath);
            _kernelProvider.Load(kernelPath);
        }
    }

    /// <summary>
    /// Converts a UTC DateTime to SPICE Ephemeris Time (ET) in seconds past J2000.0.
    /// </summary>
    /// <param name="utc">The UTC time to convert.</param>
    /// <returns>Ephemeris Time in seconds past J2000.0 (JD 2451545.0).</returns>
    public double ConvertUtcToEphemerisTime(DateTime utc) => _timeConverter.UtcToEt(utc);

    /// <summary>
    /// Returns the Cartesian state vector [x, y, z, vx, vy, vz] for the specified target body.
    /// </summary>
    /// <param name="target">NAIF target body name or integer ID (e.g., "399" for Earth, "SUN" for the Sun).</param>
    /// <param name="ephemerisTime">Ephemeris Time in seconds past J2000.0.</param>
    /// <param name="frame">Reference frame (e.g., "J2000").</param>
    /// <param name="observer">NAIF observer body name or ID (e.g., "0" for Solar System Barycenter, "399" for Earth).</param>
    /// <returns>A six-element array [x, y, z, vx, vy, vz] in kilometres and km/s.</returns>
    public double[] GetPosition(string target, double ephemerisTime, string frame, string observer) =>
        _stateProvider.GetStateVector(target, ephemerisTime, frame, observer);
}

internal class DefaultKernelProvider(HashSet<string> loadedPaths) : ISpaceKernelProvider
{
    public void Load(string kernelPath)
    {
        // Validates the file is accessible and records it as loaded.
        // TODO: Implement full BSP binary parsing when a NAIF-compatible .NET library is available.
        if (!File.Exists(kernelPath))
            throw new FileNotFoundException("SPICE kernel not found.", kernelPath);

        loadedPaths.Add(Path.GetFullPath(kernelPath));
    }
}

internal class DefaultTimeConverter : ITimeConverter
{
    // J2000.0 epoch: 2000-Jan-1 12:00:00 TT ≈ UTC for this approximation
    private static readonly DateTime J2000Epoch = new(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public double UtcToEt(DateTime utc)
    {
        // ET ≈ (JD − 2451545.0) × 86400.0  seconds past J2000.0
        // Ignores leap seconds (~1 second/year drift). Acceptable for positional accuracy to ~arc-seconds.
        // TODO: incorporate IERS leap-second table for sub-arc-second precision applications.
        return (utc.ToUniversalTime() - J2000Epoch).TotalSeconds;
    }
}

internal class DefaultStateVectorProvider(HashSet<string> loadedKernels) : IStateVectorProvider
{
    public double[] GetStateVector(string target, double ephemerisTime, string frame, string observer)
    {
        if (loadedKernels.Count == 0)
            throw new InvalidOperationException(
                "No SPICE kernels loaded. Call LoadKernel() with a .bsp kernel file before querying state vectors.");

        // TODO: Implement BSP binary record parsing (SPK Type 2/3 Chebyshev coefficients) for full
        // state vector support. SpiceSharp-Parser targets circuit netlists and does not parse
        // binary planetary kernel (BSP) files. A NAIF-compatible library or custom SPK reader is required.
        // See: https://naif.jpl.nasa.gov/pub/naif/toolkit_docs/FORTRAN/src/spicelib/spkr02.f
        throw new NotSupportedException(
            "Binary BSP state vector extraction is not yet implemented. " +
            "Use DE430Importer for pre-converted binary records, or implement an SPK Type 2/3 reader.");
    }
}

internal interface IStateVectorProvider
{
    double[] GetStateVector(string target, double ephemerisTime, string frame, string observer);
}

internal interface ITimeConverter
{
    double UtcToEt(DateTime utc);
}

internal interface ISpaceKernelProvider
{
    void Load(string kernelPath);
}

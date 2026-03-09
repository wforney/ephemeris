// Updated: 2026-03-09
namespace Ephemeris.Import;

/// <summary>
/// Manages loaded SPICE kernels and provides ephemeris time conversion and state vector queries.
/// </summary>
public class SpiceKernelDatabase : IDisposable
{
    private readonly List<string> _loadedKernels = [];
    private readonly List<string> _furnshStatements = [];
    private readonly ISpaceKernelProvider _kernelProvider;
    private readonly ITimeConverter _timeConverter;
    private readonly IStateVectorProvider _stateProvider;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance using the default SPICE provider implementations.
    /// </summary>
    public SpiceKernelDatabase()
    {
        var readers = new List<SpkReader>();
        _kernelProvider = new DefaultKernelProvider(readers);
        _timeConverter = new DefaultTimeConverter();
        _stateProvider = new DefaultStateVectorProvider(readers);
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

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            (_kernelProvider as IDisposable)?.Dispose();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}

internal class DefaultKernelProvider(List<SpkReader> readers) : ISpaceKernelProvider, IDisposable
{
    public void Load(string kernelPath)
    {
        if (!File.Exists(kernelPath))
            throw new FileNotFoundException("SPICE kernel not found.", kernelPath);

        readers.Add(new SpkReader(kernelPath));
    }

    public void Dispose()
    {
        foreach (var reader in readers)
            reader.Dispose();

        readers.Clear();
    }
}

internal class DefaultTimeConverter : ITimeConverter
{
    public double UtcToEt(DateTime utc) => SpkLeapSeconds.UtcToEt(utc);
}

internal class DefaultStateVectorProvider(List<SpkReader> readers) : IStateVectorProvider
{
    public double[] GetStateVector(string target, double ephemerisTime, string frame, string observer)
    {
        if (readers.Count == 0)
            throw new InvalidOperationException(
                "No SPICE kernels loaded. Call LoadKernel() with a .bsp kernel file before querying state vectors.");

        int targetId = SpkReader.ResolveBodyId(target);
        int centerId = SpkReader.ResolveBodyId(observer);

        // Try each loaded reader until one can service the request
        foreach (var reader in readers)
        {
            try
            {
                double[] pos = reader.GetPosition(targetId, ephemerisTime, centerId);
                // Return 6-element state vector; velocity is zero (Type 2 provides position only)
                return [pos[0], pos[1], pos[2], 0.0, 0.0, 0.0];
            }
            catch (InvalidOperationException)
            {
                // This reader has no matching segment; try the next one.
            }
        }

        throw new InvalidOperationException(
            $"No loaded SPK kernel contains data for target={target} observer={observer} at ET={ephemerisTime:F3}.");
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

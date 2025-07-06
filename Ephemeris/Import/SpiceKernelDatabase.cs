namespace Ephemeris.Import;

public class SpiceKernelDatabase
{
    private readonly List<string> _loadedKernels = [];
    private readonly List<string> _furnshStatements = [];
    private readonly ISpaceKernelProvider _kernelProvider;
    private readonly ITimeConverter _timeConverter;
    private readonly IStateVectorProvider _stateProvider;

    public SpiceKernelDatabase()
    {
        // These interfaces would be defined in your application or provided via SpiceSharp-Parser integration
        _kernelProvider = new DefaultKernelProvider();
        _timeConverter = new DefaultTimeConverter();
        _stateProvider = new DefaultStateVectorProvider();
    }

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

    public double ConvertUtcToEphemerisTime(DateTime utc) => _timeConverter.UtcToEt(utc);

    public double[] GetPosition(string target, double ephemerisTime, string frame, string observer) => _stateProvider.GetStateVector(target, ephemerisTime, frame, observer);
}

internal class DefaultStateVectorProvider : IStateVectorProvider
{
    public double[] GetStateVector(string target, double ephemerisTime, string frame, string observer) => throw new NotImplementedException();
}

internal class DefaultTimeConverter : ITimeConverter
{
    public double UtcToEt(DateTime utc) => throw new NotImplementedException();
}

internal class DefaultKernelProvider : ISpaceKernelProvider
{
    public void Load(string kernelPath) => throw new NotImplementedException();
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

namespace DellServerSilencer.Environment;

public class EnvironmentRequestHandler
{
    private readonly EnvironmentLogger? _logger;
    private readonly int[] _inletOffsets;
    private readonly TemperatureThresholdSettings _inletThresholds;

    public EnvironmentRequestHandler(EnvironmentSettings settings, ILogger? logger = null)
    {
        _logger = new(logger);
        _inletOffsets = settings.InletOffsets;
        _inletThresholds = new(settings.InletThresholds);
    }

    public int GetOffset(int inletTemperature)
    {
        var index = _inletThresholds.GetIndex(inletTemperature);
        var inletOffset = _inletOffsets[index];
        _logger?.SetInletOffset(inletOffset, inletTemperature);
        
        return inletOffset;
    }
}
namespace DellServerSilencer.FanControl;

public class FanControlLogger
{
    private ILogger? _logger;

    public FanControlLogger(ILogger? logger = null)
    {
        _logger = logger;
    }
    
    public void LogSuccess(FanMode mode)
    {
        _logger?.LogInformation("Set fan control mode to {0}.", mode.ToString());
    }

    public void LogFailure(FanMode mode)
    {
        _logger?.LogError("Failed to set fan control mode to {0}.", mode.ToString());
    }
}
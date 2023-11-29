namespace DellServerSilencer.Environment;

public class EnvironmentLogger
{
    private readonly ILogger? _logger;

    public EnvironmentLogger(ILogger? logger = null)
    {
        _logger = logger;
    }
    
    public void SetInletOffset(int offset, int temperature)
    {
        _logger?.LogInformation("Set fan offset to {0} with temperature {1} C.",offset, temperature);
    }
}
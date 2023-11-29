namespace DellServerSilencer.Sensors;

public class TemperatureLogger
{
    private readonly ILogger? _logger;

    public TemperatureLogger(ILogger? logger)
    {
        _logger = logger;
    }
    
    public void LaunchTemperatureCheck()
    {
        _logger?.LogInformation("Temperature check at: {time}", DateTimeOffset.Now);
    }
}
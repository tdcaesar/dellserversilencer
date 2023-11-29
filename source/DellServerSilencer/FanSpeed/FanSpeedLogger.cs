namespace DellServerSilencer.FanSpeed;

public class FanSpeedLogger
{
    private readonly ILogger? _logger;

    public FanSpeedLogger(ILogger? logger)
    {
        _logger = logger;
    }
    
    public void SetFanSpeed(int cpuId, int fanSpeed, int temperature)
    {
        _logger?.LogDebug("Set fan speed to {0}% for CPU {1} with temperature {2}C.", 
            fanSpeed, cpuId, temperature);
    }
    public void SetFanSpeed(string pciType, int fanSpeed, int temperature)
    {
        _logger?.LogInformation("Set fan speed to {0}% for {1} with exhaust temperature of {2}C.", pciType, fanSpeed, temperature); 
    }

    public void SetFanSpeedFailure(int fanSpeed, string fanIdString)
    {
        _logger?.LogWarning($"Failed to set fan speed 0x{fanSpeed:X} for fan {fanIdString}");
    }

    public void UnknownCpu(int cpuId)
    {
        _logger?.LogError($"Unknown CPU {cpuId} selected");
    }
}
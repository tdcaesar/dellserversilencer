namespace DellServerSilencer.FanSpeed;

public class FanSpeedCommandHandler
{
    private readonly FanSpeedLogger _logger;
    private readonly IpmiTool _tool;

    public FanSpeedCommandHandler(IpmiTool tool, ILogger? logger = null)
    {
        _tool = tool;
        _logger = new(logger);
    }
    
    
    private async Task<bool> SetFanSpeed(int fanId, int fanSpeed, CancellationToken cancellationToken)
    {
        int FanIdNumber = fanId - 1;
        string FanIdString = $"0x0{FanIdNumber}";

        var result = await _tool.Execute($"raw 0x30 0x30 0x02 {FanIdString} {fanSpeed}", cancellationToken);
        if(!result.Contains("Error"))
        {
            _logger.SetFanSpeedFailure(fanSpeed, FanIdString);
            return false;
        }
        else
        {
            // Uncomment to log individual fan speed changes
            // LogMessage($"Set fan speed for fan {FanId} to 0x{fanSpeed:X} ({fanSpeed}%)");
            return true;
        }
    }

    private async Task<bool> SetFanSpeedCpu(int selectedCpuId, int cpuFanSpeed, CancellationToken cancellationToken)
    {
        if (selectedCpuId == 0)
            await SetFanSpeed(2, cpuFanSpeed, cancellationToken);
        else if (selectedCpuId == 1)
            await SetFanSpeed(5, cpuFanSpeed, cancellationToken);
        else
        {
            _logger.UnknownCpu(selectedCpuId);
            return false;
        }
        return true;
    }

    private async Task<bool> SetFanSpeedPci(int pciFanSpeed, int nicFanSpeed, CancellationToken cancellationToken)
    {
        await SetFanSpeed( 1, pciFanSpeed, cancellationToken);
        await SetFanSpeed( 3, pciFanSpeed, cancellationToken);
        await SetFanSpeed( 4, nicFanSpeed, cancellationToken);
        await SetFanSpeed( 6, pciFanSpeed, cancellationToken);

        return true;
    }

}
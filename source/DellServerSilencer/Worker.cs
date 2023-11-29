using DellServerSilencer.FanControl;
using DellServerSilencer.TemperatureReading;

namespace DellServerSilencer;

public class Worker : BackgroundService
{
    private const int DelayInterval = 1000;
    private const int CpuCount = 2;
    private const int MaxTemperature = 80;
    private const int MaxFanSpeed = 100;
    private const int MinFanSpeed = 10;
    private const int CPU1_OFFSET = 0;
    private const int CPU2_OFFSET = 8;
    private const int PCI_OFFSET = -5;
    private const int NIC_OFFSET = 4;
    private readonly int[] INLET_OFFSETS = { 0, 1, 2, 3, 4, 5, 6, 7 };
    private readonly int[] FANSPEEDSDEFAULT = { 20, 25, 30, 35, 40, 45, 50, 60 };
    private readonly ILogger<Worker> _logger;
    private bool AutomaticControl = false;
    private FanMode FanControlMode = FanMode.Automatic;
    private IpmiTool Tool;
    private TemperatureHandler _handler;

    public Worker(ILogger<Worker> logger, Settings settings)
    {
        _logger = logger;
        Tool = new(logger, settings);
        _handler = new TemperatureHandler(Tool);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fan Control started at: {time}", DateTimeOffset.Now);
        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Temperature check at: {time}", DateTimeOffset.Now);
            TemperatureReadings currentTemperatures = await _handler.GetReadings(cancellationToken);
            var inletOffset = GetInletOffset(currentTemperatures.Inlet);
            _logger.LogInformation("Set fan offset to {0} with temperature {1} C.",inletOffset, currentTemperatures.Inlet);

            for (int index = 0; index < CpuCount; index++)
            {
                int CpuId = index + 1;
                if (currentTemperatures.Cpu[index] > MaxTemperature)
                {
                    await SetFanControl(FanMode.Automatic, cancellationToken);
                    _logger.LogCritical("Set fan control to automatic due to temperature {0}C on CPU {1}", currentTemperatures.Cpu[index], CpuId);
                }
                else
                {
                    await SetFanControl(FanMode.Manual, cancellationToken);
                    int fanSpeedCpu = GetFanSpeedCpu(currentTemperatures.Cpu[index], index, inletOffset);
                    await SetFanSpeedCpu(index, fanSpeedCpu, cancellationToken);
                    _logger.LogInformation("Set fan speed to {0}% for CPU {1} with temperature {2}C.", fanSpeedCpu, index, currentTemperatures.Cpu[index]);
                }
            }

            if (!AutomaticControl)
            {
                int fanSpeedPci = GetFanSpeedPci(currentTemperatures.Exhaust, FanType.Other, inletOffset);
                int fanSpeedNic = GetFanSpeedPci(currentTemperatures.Exhaust, FanType.Nic, inletOffset);
                await SetFanSpeedPci(fanSpeedPci, fanSpeedNic, cancellationToken);
                _logger.LogInformation("Set fan speed to {0}% for PCI with exhaust temperature of {1}C.", fanSpeedPci, currentTemperatures.Exhaust);
                _logger.LogInformation("Set fan speed to {0}% for NIC with exhaust temperature of {1}C.", fanSpeedNic, currentTemperatures.Exhaust);
            }
            
            await Task.Delay(DelayInterval, cancellationToken);
        }
    }

    private int GetFanSpeed(TemperatureThresholdSettings thresholds, FanSpeedSettings fanSpeeds, int temperature, int offset)
    {
        int index = thresholds.GetIndex(temperature);
        FanSpeed matchingFanSpeed = fanSpeeds.GetSpeed(index);
        int adjustedFanSpeed = matchingFanSpeed.WithOffset(offset);
        
        return adjustedFanSpeed;
    }

    private int GetFanSpeedCpu(int cpuTemperature, int selectedCpuId, int InletOffset)
    {
        int FanSpeedOffset;
        int[] FanSpeeds = FANSPEEDSDEFAULT;

        if (selectedCpuId == 0)
            FanSpeedOffset = CPU1_OFFSET;
        else if (selectedCpuId == 1)
            FanSpeedOffset = CPU2_OFFSET;
        else
            FanSpeedOffset = 0;

        if (cpuTemperature > 75)
            return FanSpeeds[7] + FanSpeedOffset + InletOffset;
        else if (cpuTemperature > 70)
            return FanSpeeds[6] + FanSpeedOffset + InletOffset;
        else if (cpuTemperature > 65)
            return FanSpeeds[5] + FanSpeedOffset + InletOffset;
        else if (cpuTemperature > 60)
            return FanSpeeds[4] + FanSpeedOffset + InletOffset;
        else if (cpuTemperature > 55)
            return FanSpeeds[3] + FanSpeedOffset + InletOffset;
        else if (cpuTemperature > 50)
            return FanSpeeds[2] + FanSpeedOffset + InletOffset;
        else if (cpuTemperature > 45)
            return FanSpeeds[1] + FanSpeedOffset + InletOffset;
        else
            return FanSpeeds[0] + FanSpeedOffset + InletOffset;
    }

    private int GetFanSpeedPci(int exhaustTemperature, FanType fanSelection, int inletOffset)
    {
        int FanSpeedOffset;
        int[] FanSpeeds = FANSPEEDSDEFAULT;

        switch (fanSelection)
        {
            case FanType.Nic:
                FanSpeedOffset = NIC_OFFSET;
                break;
            case FanType.Other:
                FanSpeedOffset = PCI_OFFSET;
                break;
            default:
                FanSpeedOffset = 0;
                break;
        }

        switch (exhaustTemperature)
        {
            case > 75:
                return FanSpeeds[7] + FanSpeedOffset + inletOffset;
            case > 70:
                return FanSpeeds[6] + FanSpeedOffset + inletOffset;
            case > 65:
                return FanSpeeds[5] + FanSpeedOffset + inletOffset;
            case > 60:
                return FanSpeeds[4] + FanSpeedOffset + inletOffset;
            case > 55:
                return FanSpeeds[3] + FanSpeedOffset + inletOffset;
            case > 50:
                return FanSpeeds[2] + FanSpeedOffset + inletOffset;
            case > 45:
                return FanSpeeds[1] + FanSpeedOffset + inletOffset;
            default:
                return FanSpeeds[0] + FanSpeedOffset + inletOffset;
        }
    }

    private int GetInletOffset(int inletTemperature)
    {
        int[] Offsets = INLET_OFFSETS;

        switch(inletTemperature)
        {
            case > 39:
                return Offsets[7];
            case > 36:
                return Offsets[6];
            case > 33:
                return Offsets[5];
            case > 30:
                return Offsets[4];
            case > 27:
                return Offsets[3];
            case > 24:
                return Offsets[2];
            case > 21:
                return Offsets[1];
            default:
                return Offsets[0];
        }
    }

    private async Task<bool> SetFanControl(FanMode fanControl, CancellationToken cancellationToken)
    {
        string result;
        switch (fanControl)
        {
            case FanMode.Manual:
                result = await Tool.Execute("raw 0x30 0x30 0x01 0x00", cancellationToken);
                break;
            case FanMode.Automatic:
            default:
                result = await Tool.Execute("raw 0x30 0x30 0x01 0x01", cancellationToken);
                break;
        }

        if (!result.Contains("Error"))
        {
            //_logger.LogInformation("Set fan control mode to {0}.", fanControl);
            return true;
        }

        _logger.LogError("Failed to set fan control mode to {0}.", fanControl);
        return false;
    }

    private async Task<bool> SetFanSpeed(int fanId, int fanSpeed, CancellationToken cancellationToken)
    {
        int FanIdNumber = fanId - 1;
        string FanIdString = $"0x0{FanIdNumber}";

        var result = await Tool.Execute($"raw 0x30 0x30 0x02 {FanIdString} {fanSpeed}", cancellationToken);
        if(!result.Contains("Error"))
        {
            _logger.LogWarning($"Failed to set fan speed 0x{fanSpeed:X} for fan {FanIdString}");
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
            _logger.LogError("Unknown CPU selected");
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


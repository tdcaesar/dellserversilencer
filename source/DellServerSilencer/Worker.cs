using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DellServerSilencer;

public class Worker : BackgroundService
{
    private const int DelayInterval = 1000;
    private const int CpuCount = 2;
    private const int MaxTemperature = 80;
    private const int CPU1_OFFSET = 0;
    private const int CPU2_OFFSET = 8;
    private const int PCI_OFFSET = -5;
    private const int NIC_OFFSET = 4;
    private readonly int[] INLET_OFFSETS = { 0, 1, 2, 3, 4, 5, 6, 7 };
    private readonly int[] FANSPEEDSDEFAULT = { 20, 25, 30, 35, 40, 45, 50, 60 };
    private readonly TemperatureSensors Sensors = new TemperatureSensors("04h", "01h", new string[] {"0Eh", "0Fh"});
    private readonly ILogger<Worker> _logger;
    private bool AutomaticControl = false;
    private FanControl FanControlMode = FanControl.Automatic;
    private IpmiTool Tool;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        Tool = new(logger);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fan Control started at: {time}", DateTimeOffset.Now);
        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Temperature check at: {time}", DateTimeOffset.Now);
            TemperatureReadings currentTemperatures = await GetTemperatureReadings(cancellationToken);
            var inletOffset = GetInletOffset(currentTemperatures.Inlet);
            _logger.LogInformation("Set fan offset to {0} with temperature {1} C.",inletOffset, currentTemperatures.Inlet);

            for (int index = 0; index < CpuCount; index++)
            {
                int CpuId = index + 1;
                if (currentTemperatures.Cpu[index] > MaxTemperature)
                {
                    await SetFanControl(FanControl.Automatic, cancellationToken);
                    _logger.LogCritical("Set fan control to automatic due to temperature {0}C on CPU {1}", currentTemperatures.Cpu[index], CpuId);
                }
                else
                {
                    await SetFanControl(FanControl.Manual, cancellationToken);
                    int fanSpeedCpu = GetFanSpeedCpu(currentTemperatures.Cpu[index], index, inletOffset);
                    await SetFanSpeedCpu(index, fanSpeedCpu, cancellationToken);
                    _logger.LogInformation("Set fan speed to {0}% for CPU {1} with temperature {2}C.", fanSpeedCpu, index, currentTemperatures.Cpu[index]);
                }
            }

            if (!AutomaticControl)
            {
                int fanSpeedPci = GetFanSpeedPci(currentTemperatures.Exhaust, PciType.Other, inletOffset);
                int fanSpeedNic = GetFanSpeedPci(currentTemperatures.Exhaust, PciType.Nic, inletOffset);
                await SetFanSpeedPci(fanSpeedPci, fanSpeedNic, cancellationToken);
                _logger.LogInformation("Set fan speed to {0}% for PCI with exhaust temperature of {1}C.", fanSpeedPci, currentTemperatures.Exhaust);
                _logger.LogInformation("Set fan speed to {0}% for NIC with exhaust temperature of {1}C.", fanSpeedNic, currentTemperatures.Exhaust);
            }
            
            await Task.Delay(DelayInterval, cancellationToken);
        }
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

    private int GetFanSpeedPci(int ExhaustTemperature, PciType fanSelection, int inletOffset)
    {
        int FanSpeedOffset;
        int[] FanSpeeds = FANSPEEDSDEFAULT;

        switch (fanSelection)
        {
            case PciType.Nic:
                FanSpeedOffset = NIC_OFFSET;
                break;
            case PciType.Other:
                FanSpeedOffset = PCI_OFFSET;
                break;
            default:
                FanSpeedOffset = 0;
                break;
        }

        switch (ExhaustTemperature)
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


    private async Task<TemperatureReadings> GetTemperatureReadings(CancellationToken cancellationToken)
    {
        int InletTemperature = await Tool.GetTemperatureReading(Sensors.Inlet, cancellationToken);
        int ExhaustTemperature = await Tool.GetTemperatureReading(Sensors.Exhaust, cancellationToken);
        int Cpu1Temperature = await Tool.GetTemperatureReading(Sensors.Cpu[1], cancellationToken);
        int Cpu2Temperature = await Tool.GetTemperatureReading(Sensors.Cpu[2], cancellationToken);

        return new TemperatureReadings(InletTemperature, ExhaustTemperature,
            new[] { Cpu1Temperature, Cpu2Temperature });
    }

    private async Task<bool> SetFanControl(FanControl fanControl, CancellationToken cancellationToken)
    {
        string result;
        switch (fanControl)
        {
            case FanControl.Manual:
                result = await Tool.Execute("raw 0x30 0x30 0x01 0x00", cancellationToken);
                break;
            case FanControl.Automatic:
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


public record TemperatureReadings(int Inlet, int Exhaust, int[] Cpu);
public record TemperatureSensors(string Inlet, string Exhaust, string[] Cpu);

internal enum PciType
{
    Nic,
    Other
}

internal enum FanControl
{
    Automatic,
    Manual,
    Unknown
}
internal enum TemperatureSensor
{
    Inlet,
    Exhaust,
    Cpu
}
namespace DellServerSilencer;

public class Worker : BackgroundService
{
    private const int DelayInterval = 1000;
    private const int CpuCount = 2;
    private const int MaxTemperature = 80;
    private readonly ILogger<Worker> _logger;
    private bool AutomaticControl = false;
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
            TemperatureReadings currentTemperatures = GetTemperatureReadings(cancellationToken);
            var inletOffset = GetInletOffset(currentTemperatures.Inlet);
            _logger.LogInformation("Set fan offset to {0} with temperature {1} C.",inletOffset, currentTemperatures.Inlet);

            for (int index = 0; index < CpuCount; index++)
            {
                int CpuId = index + 1;
                if (currentTemperatures.Cpu[index] > MaxTemperature)
                {
                    SetFanControl(FanControl.Automatic);
                    _logger.LogCritical("Set fan control to automatic due to temperature {0}C on CPU {1}", currentTemperatures.Cpu[index], CpuId);
                }
                else
                {
                    SetFanControl(FanControl.Manual);
                    int fanSpeedCpu = GetFanSpeedCpu(currentTemperatures.Cpu[index], index, inletOffset);
                    SetFanSpeedCpu(index, fanSpeedCpu);
                    _logger.LogInformation("Set fan speed to {0}% for CPU {1} with temperature {2}C.", fanSpeedCpu, index, currentTemperatures.Cpu[index]);
                }
            }

            if (!AutomaticControl)
            {
                int fanSpeedPci = GetFanSpeedPci(currentTemperatures.Exhaust, PciType.Other, inletOffset);
                int fanSpeedNic = GetFanSpeedPci(currentTemperatures.Exhaust, PciType.Nic, inletOffset);
                SetFanSpeedPci(fanSpeedPci, fanSpeedNic);
                _logger.LogInformation("Set fan speed to {0}% for PCI with exhaust temperature of {1}C.", fanSpeedPci, TemperatureReadingExhaust);
                _logger.LogInformation("Set fan speed to {0}% for NIC with exhaust temperature of {1}C.", fanSpeedNic, TemperatureReadingExhaust);
            }
            
            await Task.Delay(DelayInterval, stoppingToken);
        }
    }

    private TemperatureReadings GetTemperatureReadings(CancellationToken cancellationToken)
    {
        int InletTemperature = Tool.GetTemperatureReading("04h", cancellationToken);
        int ExhaustTemperature = Tool.GetTemperatureReading("01h", cancellationToken);
        int Cpu1Temperature = Tool.GetTemperatureReading("0Eh", cancellationToken);
        int Cpu2Temperature = Tool.GetTemperatureReading("0Fh", cancellationToken);

        return new TemperatureReadings(InletTemperature, ExhaustTemperature,
            new[] { Cpu1Temperature, Cpu2Temperature });
    }
}

public record TemperatureReadings(int Inlet, int Exhaust, int[] Cpu);

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
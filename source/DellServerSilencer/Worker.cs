using DellServerSilencer.Environment;
using DellServerSilencer.FanControl;
using DellServerSilencer.FanSpeed;
using DellServerSilencer.Ipmi;
using DellServerSilencer.Sensors;

namespace DellServerSilencer;

public class Worker : BackgroundService
{
    private const int DelayInterval = 20000;
    private const int CpuCount = 2;
    private const int MaxTemperature = 80;
    private readonly ApplicationLogger _applicationLogger;
    private bool AutomaticControl = false;
    private FanMode FanControlMode = FanMode.Automatic;
    private IpmiTool Tool;
    private readonly TemperatureRequestHandler _temperatureRequestHandler;
    private readonly EnvironmentRequestHandler _environmentHandler;
    private readonly FanControlCommandHandler _fanControlCommandHandler;
    private readonly FanSpeedRequestHandler _fanSpeedRequestHandler;
    private readonly FanSpeedCommandHandler _fanSpeedCommandHandler;
    
    public Worker(ILogger<Worker> logger, Settings settings)
    {
        _applicationLogger = new(logger);
        Tool = new(logger, settings);
        _temperatureRequestHandler = new TemperatureRequestHandler(Tool, settings.Sensors, logger);
        _environmentHandler = new EnvironmentRequestHandler(settings.Environment, logger);
        _fanControlCommandHandler = new(Tool, logger);
        _fanSpeedRequestHandler = new(Tool,logger);
        _fanControlCommandHandler = new(Tool, logger);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _applicationLogger.Start();
        while (!cancellationToken.IsCancellationRequested)
        {
            TemperatureReadings currentTemperatures = await _temperatureRequestHandler.GetReadings(cancellationToken);
            var inletOffset = _environmentHandler.GetOffset(currentTemperatures.Inlet);
            
            for (int index = 0; index < CpuCount; index++)
            {
                int cpuId = index + 1;
                if (currentTemperatures.Cpu[index] > MaxTemperature)
                {
                    await _fanControlCommandHandler.SetFanControl(FanMode.Automatic, cancellationToken);
                    AutomaticControl = true;
                    break;
                }
                await _fanControlCommandHandler.SetFanControl(FanMode.Manual, cancellationToken);
                
                int fanSpeedCpu =
                    _fanSpeedRequestHandler.GetFanSpeedCpu(currentTemperatures.Cpu[index], index, inletOffset);
                await _fanSpeedCommandHandler.SetFanSpeedCpu(index, fanSpeedCpu, cancellationToken);
                _applicationLogger.SetFanSpeed(cpuId, fanSpeedCpu, currentTemperatures.Cpu[index]);
            }

            if (!AutomaticControl)
            {
                int fanSpeedPci = _fanSpeedRequestHandler.GetFanSpeedPci(currentTemperatures.Exhaust, FanType.Other, inletOffset);
                int fanSpeedNic = _fanSpeedRequestHandler.GetFanSpeedPci(currentTemperatures.Exhaust, FanType.Nic, inletOffset);
                await _fanSpeedCommandHandler.SetFanSpeedPci(fanSpeedPci, fanSpeedNic, cancellationToken);
                _applicationLogger.SetFanSpeed("PCI",fanSpeedPci, currentTemperatures.Exhaust);
                _applicationLogger.SetFanSpeed("Nic",fanSpeedNic, currentTemperatures.Exhaust);
            }
            
            await Task.Delay(DelayInterval, cancellationToken);
        }
    }



}


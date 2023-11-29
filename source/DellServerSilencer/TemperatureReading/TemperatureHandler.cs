namespace DellServerSilencer.TemperatureReading;

public class TemperatureHandler
{
    private readonly IpmiTool _tool;
    private readonly TemperatureSensors _sensors = new TemperatureSensors("04h", "01h", new string[] {"0Eh", "0Fh"});
    
    public TemperatureHandler(IpmiTool tool)
    {
        _tool = tool;
    }
    
    public async Task<TemperatureReadings> GetReadings(CancellationToken cancellationToken)
    {
        int inletTemperature = await _tool.GetTemperatureReading(_sensors.Inlet, cancellationToken);
        int exhaustTemperature = await _tool.GetTemperatureReading(_sensors.Exhaust, cancellationToken);
        int[] cpuTemperatures = new int[_sensors.Cpu.Length];

        for (int index = 0; index < _sensors.Cpu.Length; index++)
        {
            cpuTemperatures[index] = await _tool.GetTemperatureReading(_sensors.Cpu[index], cancellationToken);
        }

        return new TemperatureReadings(inletTemperature, exhaustTemperature, cpuTemperatures);
    }
}
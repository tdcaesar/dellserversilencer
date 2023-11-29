using DellServerSilencer.Ipmi;

namespace DellServerSilencer.Sensors;

public class TemperatureRequestHandler
{
    private readonly IpmiTool _tool;
    private readonly TemperatureLogger _logger;
    private readonly SensorSettings _settings;
    
    public TemperatureRequestHandler(IpmiTool tool, SensorSettings settings, ILogger? logger = null)
    {
        _tool = tool;
        _settings = settings;
        _logger = new(logger);
    }
    
    public async Task<TemperatureReadings> GetReadings(CancellationToken cancellationToken)
    {
        _logger.LaunchTemperatureCheck();
        int inletTemperature = await GetTemperatureReading(_settings.InletSensorId, cancellationToken);
        int exhaustTemperature = await GetTemperatureReading(_settings.ExhaustSensorId, cancellationToken);
        int[] cpuTemperatures = new int[_settings.CpuSensorId.Length];

        for (int index = 0; index < _settings.CpuSensorId.Length; index++)
        {
            cpuTemperatures[index] = await GetTemperatureReading(_settings.CpuSensorId[index], cancellationToken);
        }

        return new TemperatureReadings(inletTemperature, exhaustTemperature, cpuTemperatures);
    } 
    private async Task<int> GetTemperatureReading(string sensorId, CancellationToken cancellationToken)
    {
        string command = GetTemperatureReadingCommand(sensorId);
        var temperatureReading = await _tool.Execute(command, cancellationToken);

        bool valid = int.TryParse(temperatureReading, out var temperatureReadingInt);

        if (valid)
            return temperatureReadingInt;

        throw new InvalidTemperatureReadingException(temperatureReading);
    }
    private string GetTemperatureReadingCommand(string sensorId)
    {
        return $"sdr type temperature | grep \"{sensorId}\" | cut -d\"|\" -f5 | cut -d\" \" -f2 >&1";
    }
}
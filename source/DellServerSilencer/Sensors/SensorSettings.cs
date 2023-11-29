namespace DellServerSilencer.Sensors;

public class SensorSettings
{
    public string InletSensorId { get; } = "04h";
    public string ExhaustSensorId { get; } = "01h";
    public string[] CpuSensorId { get; } = { "0Eh", "0Fh" };
}
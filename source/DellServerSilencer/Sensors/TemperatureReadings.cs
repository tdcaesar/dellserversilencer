namespace DellServerSilencer.Sensors;

public record TemperatureReadings(int Inlet, int Exhaust, int[] Cpu);
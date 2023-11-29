namespace DellServerSilencer.TemperatureReading;

public record TemperatureReadings(int Inlet, int Exhaust, int[] Cpu);
namespace DellServerSilencer;

public class TemperatureThreshold
{
    private const int Minimum = 0;
    private const int Maximum = 100;
    private readonly int Value;

    public TemperatureThreshold(int temperature)
    {
        if (temperature < Minimum || temperature > Maximum)
            throw new ArgumentException($"Temperature Thresholds {temperature} must be between {Minimum} and {Maximum}.", nameof(temperature));
        
        Value = temperature;
    }
    
    public static implicit operator int(TemperatureThreshold temperature) => temperature.Value;
    public static explicit operator TemperatureThreshold(int temperature) => new(temperature);

    public override string ToString() => $"{Value}";
}
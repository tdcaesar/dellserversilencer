namespace DellServerSilencer;

public class FanSpeedSettings
{
    private const int DefaultOffset = 0;
    private const int DefaultSpeed = 15;
    private const int MaximumSpeed = 100;
    public FanSpeed[] Speeds { get; }

    public FanSpeedSettings(FanSpeed[] speeds, int offset = DefaultOffset)
    {
        FanSpeed priorSpeed = new(0);
        Speeds = new FanSpeed[8];

        if (speeds.Length != 8)
            throw new ArgumentException("Speeds must be 8 elements long", nameof(speeds));
            
        for(int index = 0; index < speeds.Length; index++)
        {
            FanSpeed currentSpeed = speeds[index];
            if (currentSpeed < priorSpeed)
                throw new ArgumentException("Speeds must be in ascending order", nameof(speeds));
            priorSpeed = currentSpeed;

            int speedWithOffset = currentSpeed + offset;
            
            if (speedWithOffset > MaximumSpeed)
                speedWithOffset = MaximumSpeed;

            Speeds[index] = new(speedWithOffset);
        }
    }

    public FanSpeed GetSpeed(int index)
    {
        if (index > 8)
            return new(MaximumSpeed);

        if (index < 0)
            return new(DefaultSpeed);

        return Speeds[index];
    }
}
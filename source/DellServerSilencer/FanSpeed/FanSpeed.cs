using System.Runtime.InteropServices;

namespace DellServerSilencer.FanSpeed;

public class FanSpeed
{
    private readonly FanSpeedRange _range = new();
    private int Value { get; }

    public FanSpeed(int speed)
    {
        if(!_range.ValueIsInRange(speed))
            throw new ArgumentException($"Fan Speeds must be between {_range.Minimum} and {_range.Maximum}", nameof(speed));
        
        Value = speed;
    }

    public int WithOffset(int offset)
    {
        int speedWithOffset = Value + offset;

        if (speedWithOffset > _range.Maximum)
            return _range.Maximum;
        if (speedWithOffset < _range.Minimum)
            return _range.Minimum;

        return speedWithOffset;
    }

    public static implicit operator int(FanSpeed speed) => speed.Value;
    public static explicit operator FanSpeed(int speed) => new(speed);

    public override string ToString() => $"{Value}";
}
namespace DellServerSilencer;

public class FanSpeedRange
{
    private const int DefaultMinimum = 0;
    private const int DefaultMaximum = 100;

    public int Minimum { get; }
    public int Maximum { get; }

    public FanSpeedRange(int minimum = DefaultMinimum, int maximum = DefaultMaximum)
    {
        if (minimum > maximum)
            throw new ArgumentException("The minimum value must be less than the maximum value.", nameof(minimum));
        Minimum = minimum;
        Maximum = maximum;
    }
    public bool ValueIsInRange(int value)
    {
        if (value < Minimum || value > Maximum)
            return false;

        return true;
    }
}
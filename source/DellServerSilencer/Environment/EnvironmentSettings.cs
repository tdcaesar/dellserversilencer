namespace DellServerSilencer.Environment;

public class EnvironmentSettings
{
    public int[] InletThresholds { get; } = { 20, 23, 26, 29, 32, 35, 38, 41 };
    public int[] InletOffsets { get; } = { 0, 1, 2, 3, 4, 5, 6, 7 };
}
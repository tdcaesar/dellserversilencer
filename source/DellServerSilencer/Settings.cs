using System.Runtime.InteropServices;
using DellServerSilencer.Environment;
using DellServerSilencer.Ipmi;
using DellServerSilencer.Sensors;

namespace DellServerSilencer;

public class Settings
{
    public IpmiSettings Tool { get; } = new();
    public PollySettings Polly { get; } = new();
    public SensorSettings Sensors { get; } = new();

    public EnvironmentSettings Environment { get; } = new();

    public int[] CpuThresholds { get; } = { 45, 50, 55, 60, 65, 70, 75, 80 };
    public int[] CpuFanSpeeds { get; } = { 20, 25, 30, 35, 40, 45, 50, 60 };

    public Platform Platform
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Platform.Windows;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Platform.Linux;
            }

            throw new PlatformNotSupportedException(
                "Only works on Windows or Linux.");
        }
    }

    public IpmiMode Mode
    {
        get
        {
            if (Tool.Host == "")
                return IpmiMode.Local;

            return IpmiMode.Remote;
        }
        
    }
    
}

public class PollySettings
{
    private const int DefaultRetryCount = 5;
    private const int DefaultInitialDelay = 1000;
    private const double DefaultFactor = 2.0;

    public int RetryCount { get; } = DefaultRetryCount;
    public int InitialDelay { get; } = DefaultInitialDelay;
    public double Factor { get; } = DefaultFactor;
}
namespace DellServerSilencer;

public class Settings
{
    public string PathToIpmiTool { get; } = "";
    public string IpmiHost { get; } = "";
    public string IpmiUser { get; } = "";
    public string IpmiPassword { get; } = "";
    public int Retries { get; } = 3;
    public int InitialDelayInMs { get; } = 1000;
    public int DelayIncreaseFactor { get; } = 2;
    public Platform Platform { get; } = Platform.Linux;
    public IpmiMode Mode { get; } = IpmiMode.Local;
}
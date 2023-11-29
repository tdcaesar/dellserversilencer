namespace DellServerSilencer.Ipmi;

public class IpmiSettings
{
    private const string DefaultPath = "";
    private const string DefaultHost = "";
    private const string DefaultUser = "";
    private const string DefaultPassword = "";
    
    public string Path { get; } = DefaultPath;
    public string Host { get; } = DefaultHost;
    public string User { get; } = DefaultUser;
    public string Password { get; } = DefaultPassword;
}
using System.ComponentModel;
using System.Diagnostics;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace DellServerSilencer.Ipmi;

public class IpmiTool
{
    private const string DefaultLinuxPath = "/usr/bin/ipmitool";
    private const string DefaultWindowsPath = @"C:\Program Files (x86)\Dell\SysMgt\bmc\ipmitool.exe";
    private ILogger? _logger;
    private Settings Settings { get; }
    public IpmiTool(ILogger? logger, Settings settings)
    {
        _logger = logger;
        Settings = settings;
    }
    private string GetPath()
    {
        return string.IsNullOrWhiteSpace(Settings.Tool.Path)
            ? Settings.Platform switch
            {
                Platform.Linux => "/usr/bin/ipmitool",
                Platform.Windows => @"C:\Program Files (x86)\Dell\SysMgt\bmc\ipmitool.exe",
                _ => throw new ArgumentOutOfRangeException()
            }
            : Settings.Tool.Path;
    }

    private string GetArgs()
    {
        return Settings.Mode switch
        {
            IpmiMode.Local => "",
            IpmiMode.Remote => $"-I lanplus -H {Settings.Tool.Host} -U {Settings.Tool.User} "
                               + $"-P {Settings.Tool.Password}",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public async Task<string> Execute(string command, CancellationToken cancellationToken)
    {
        int totalRetries = Settings.Polly.RetryCount;
        string toolPath = GetPath();
        string args = GetArgs();

        IEnumerable<TimeSpan> delay = Backoff.ExponentialBackoff(
            TimeSpan.FromMilliseconds(Settings.Polly.InitialDelay),
            totalRetries,
            Settings.Polly.Factor);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = toolPath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        PolicyResult<string> policyExecutionResult = await Policy 
            .Handle<Exception>()
            .WaitAndRetryAsync(
                delay,
                (exception, span, iteration, _) =>
                {
                    _logger.LogError(
                        "The Process {process} with args {args} threw an exception. Trying next of {retries} attempt(s) after {span} delay.",
                        process.StartInfo.FileName,
                        process.StartInfo.Arguments,
                        totalRetries - iteration + 1,
                        span);
                })
            .ExecuteAndCaptureAsync(
                async token =>
                {
                    process.Start();
                    await process.WaitForExitAsync(token);

                    return await process.StandardOutput.ReadToEndAsync(cancellationToken);
                }, cancellationToken);
        
        return policyExecutionResult.Result;
    } 
}
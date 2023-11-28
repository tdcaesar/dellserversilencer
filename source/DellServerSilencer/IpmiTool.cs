using System.ComponentModel;
using System.Diagnostics;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace DellServerSilencer;

public class IpmiTool
{
    private const string SettingsPathToIpmiTool = "";
    private const string SettingsIpmiHost = "";
    private const string SettingsIpmiUser = "";
    private const string SettingsIpmiPassword = "";
    private const int SettingsRetries = 3;
    private const int SettingsInitialDelayInMs = 1000;
    private const int SettingsDelayIncreaseFactor = 2;
    private const Platform DefaultPlatform = Platform.Linux;
    private const IpmiMode SettingsMode = IpmiMode.Local;
    private ILogger<Worker> _logger;
    
    public IpmiTool(ILogger<Worker> logger)
    {
        _logger = logger;
    }
    private string GetPath()
    {
        return string.IsNullOrWhiteSpace(SettingsPathToIpmiTool)
            ? DefaultPlatform switch
            {
                Platform.Linux => "/usr/bin/ipmitool",
                Platform.Windows => @"C:\Program Files (x86)\Dell\SysMgt\bmc\ipmitool.exe",
                _ => throw new ArgumentOutOfRangeException()
            }
            : SettingsPathToIpmiTool;
    }

    private string GetArgs()
    {
        return string.IsNullOrWhiteSpace(SettingsMode)
            ? SettingsMode switch
            {
                IpmiMode.Local => "",
                IpmiMode.Remote => $"-I lanplus -H {SettingsIpmiHost} -U {SettingsIpmiUser} " 
                                   + $"-P {SettingsIpmiPassword}",
                _ => throw new ArgumentOutOfRangeException()
            }
            : "";
    }

    public record IpmiToolResult(bool Success, string Output);

    
    public async Task<string> Execute(string command, CancellationToken cancellationToken)
    {
        int totalRetries = SettingsRetries;
        string toolPath = GetPath();
        string args = GetArgs();

        IEnumerable<TimeSpan> delay = Backoff.ExponentialBackoff(
            TimeSpan.FromMilliseconds(SettingsInitialDelayInMs),
            totalRetries,
            SettingsDelayIncreaseFactor);

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

                    return await process.StandardOutput.ReadToEndAsync();
                }, cancellationToken);
        
        return policyExecutionResult.Result;
    }  
    public async Task<int> GetTemperatureReading(string sensorId, CancellationToken cancellationToken)
    {
        int temperatureReadingInt = 0;
        string command = GetTemperatureReadingCommand(sensorId);
        var temperatureReading =
            await Execute(command,
                cancellationToken);

        bool Valid = int.TryParse(temperatureReading, out temperatureReadingInt);

        if (Valid)
            return temperatureReadingInt;

        throw new InvalidTemperatureReadingException(temperatureReading);
    }
    private string GetTemperatureReadingCommand(string sensorId)
    {
        return $"sdr type temperature | grep \"{sensorId}\" | cut -d\"|\" -f5 | cut -d\" \" -f2 >&1";
    }
}

internal enum Platform
{
    Linux,
    Windows
}

internal enum IpmiMode
{
    Local,
    Remote
}
using System.ComponentModel;
using System.Diagnostics;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace DellServerSilencer;

public class IpmiTool
{
    private ILogger<Worker> _logger;
    private Settings Settings { get; }
    public IpmiTool(ILogger<Worker> logger, Settings settings)
    {
        _logger = logger;
        Settings = settings;
    }
    private string GetPath()
    {
        return string.IsNullOrWhiteSpace(Settings.PathToIpmiTool)
            ? Settings.Platform switch
            {
                Platform.Linux => "/usr/bin/ipmitool",
                Platform.Windows => @"C:\Program Files (x86)\Dell\SysMgt\bmc\ipmitool.exe",
                _ => throw new ArgumentOutOfRangeException()
            }
            : Settings.PathToIpmiTool;
    }

    private string GetArgs()
    {
        return Settings.Mode switch
        {
            IpmiMode.Local => "",
            IpmiMode.Remote => $"-I lanplus -H {Settings.IpmiHost} -U {Settings.IpmiUser} "
                               + $"-P {Settings.IpmiPassword}",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public async Task<string> Execute(string command, CancellationToken cancellationToken)
    {
        int totalRetries = Settings.Retries;
        string toolPath = GetPath();
        string args = GetArgs();

        IEnumerable<TimeSpan> delay = Backoff.ExponentialBackoff(
            TimeSpan.FromMilliseconds(Settings.InitialDelayInMs),
            totalRetries,
            Settings.DelayIncreaseFactor);

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
    public async Task<int> GetTemperatureReading(string sensorId, CancellationToken cancellationToken)
    {
        int temperatureReadingInt = 0;
        string command = GetTemperatureReadingCommand(sensorId);
        var temperatureReading =
            await Execute(command,
                cancellationToken);

        bool valid = int.TryParse(temperatureReading, out temperatureReadingInt);

        if (valid)
            return temperatureReadingInt;

        throw new InvalidTemperatureReadingException(temperatureReading);
    }
    private string GetTemperatureReadingCommand(string sensorId)
    {
        return $"sdr type temperature | grep \"{sensorId}\" | cut -d\"|\" -f5 | cut -d\" \" -f2 >&1";
    }
}
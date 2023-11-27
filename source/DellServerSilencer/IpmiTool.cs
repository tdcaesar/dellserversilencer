using System.ComponentModel;
using System.Diagnostics;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace DellServerSilencer;

public class IpmiTool
{
    private ILogger<Worker> _logger;
    
    public IpmiTool(ILogger<Worker> logger)
    {
        _logger = logger;
    }
    private string GetPath()
    {
        return string.IsNullOrWhiteSpace(_settings.PathToIpmiTool)
            ? Settings.Platform switch
            {
                Platform.Linux => "/usr/bin/ipmitool",
                Platform.Windows => @"C:\Program Files (x86)\Dell\SysMgt\bmc\ipmitool.exe",
                _ => throw new ArgumentOutOfRangeException()
            }
            : _settings.PathToIpmiTool;
    }

    private string GetArgs()
    {
        return string.IsNullOrWhiteSpace(_settings.Mode)
            ? Settings.Mode switch
            {
                IpmiMode.Local => "",
                IpmiMode.Remote => $"-I lanplus -H {_settings.IpmiHost} -U {_settings.IpmiUser} " 
                                   + "-P {_settings.IpmiUser} {command}",
                _ => throw new ArgumentOutOfRangeException()
            }
            : "";
    }
    
    public async Task<string> Execute(string command, CancellationToken cancellationToken)
    {
        int totalRetries = _settings.Retries;
        string toolPath = GetPath();
        string args = GetArgs();

        IEnumerable<TimeSpan> delay = Backoff.ExponentialBackoff(
            TimeSpan.FromMilliseconds(_settings.InitialDelayInMs),
            totalRetries,
            _settings.DelayIncreaseFactor);

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
    public int GetTemperatureReading(string sensorId, CancellationToken cancellationToken)
    {
        var temperatureReading =
            Tool.Execute("sdr type temperature | grep \"{sensorId}\" | cut -d\"|\" -f5 | cut -d\" \" -f2 >&1",
                sensorId,
                cancellationToken);
        return (int)temperatureReading;
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
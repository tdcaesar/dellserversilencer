using Microsoft.Extensions.Logging;

namespace DellServerSilencer;

public class FanLogger
{
    private ILogger<Worker> _logger;

    public FanLogger(ILogger<Worker> logger)
    {
        _logger = logger;
    }
    
    public void LogDebug(string message)
    {
        _logger.LogDebug(message);
    }

}
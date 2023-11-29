using Microsoft.Extensions.Logging;

namespace DellServerSilencer;

public class ApplicationLogger
{
    private readonly ILogger<Worker> _logger;

    public ApplicationLogger(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    public void Start()
    {
        _logger.LogInformation("Fan Control started at: {time}", DateTimeOffset.Now);
    }

    public void Stop()
    {
        _logger.LogInformation("Fan Control stopped at: {time}", DateTimeOffset.Now);
    }
    

}
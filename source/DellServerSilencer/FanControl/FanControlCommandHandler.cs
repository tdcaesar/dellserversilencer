using DellServerSilencer.Ipmi;

namespace DellServerSilencer.FanControl;

public class FanControlCommandHandler
{
    private const string AutomaticCommand = "raw 0x30 0x30 0x01 0x00";
    private const string ManualCommand = "raw 0x30 0x30 0x01 0x01";
        
    private readonly FanControlLogger? _logger;
    private readonly IpmiTool _tool;

    public FanControlCommandHandler(IpmiTool tool, ILogger? logger = null)
    {
        _tool = tool;
        _logger = new(logger);
    }
    public async Task<bool> SetFanControl(FanMode fanControl, CancellationToken cancellationToken)
    {
        string result;
        switch (fanControl)
        {
            case FanMode.Manual:
                result = await _tool.Execute(AutomaticCommand, cancellationToken);
                break;
            case FanMode.Automatic:
            default:
                result = await _tool.Execute(ManualCommand, cancellationToken);
                break;
        }

        if (!result.Contains("Error"))
        {
            _logger?.LogSuccess(fanControl);
            return true;
        }

        _logger?.LogFailure(fanControl);
        return false;
    }
}
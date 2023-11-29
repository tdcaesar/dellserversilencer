using DellServerSilencer.Ipmi;

namespace DellServerSilencer.FanSpeed;

public class FanSpeedRequestHandler
{
    private readonly TemperatureThresholdSettings _thresholds;
    
    private readonly FanSpeedLogger _logger;
    private readonly IpmiTool _tool;

    public FanSpeedRequestHandler(IpmiTool tool, TemperatureThresholdSettings thresholds, ILogger? logger = null)
    {
        _tool = tool;
        _thresholds = thresholds;
        _logger = new(logger);
    }
    
    
    public int GetFanSpeed(TemperatureThresholdSettings thresholds, FanSpeedSettings fanSpeeds, int temperature, int offset)
    {
        int index = thresholds.GetIndex(temperature);
        FanSpeed matchingFanSpeed = fanSpeeds.GetSpeed(index);
        int adjustedFanSpeed = matchingFanSpeed.WithOffset(offset);
        
        return adjustedFanSpeed;
    }
    
    private int GetFanSpeedCpu(int cpuTemperature, int selectedCpuId, int InletOffset)
    {
        int FanSpeedOffset;
        int[] FanSpeeds = FANSPEEDSDEFAULT;

        if (selectedCpuId == 0)
            FanSpeedOffset = CPU1_OFFSET;
        else if (selectedCpuId == 1)
            FanSpeedOffset = CPU2_OFFSET;
        else
            FanSpeedOffset = 0;
      
        if (cpuTemperature > 75)
            return FanSpeeds[7] + FanSpeedOffset + InletOffset;
        else if (cpuTemperature > 70)
            return FanSpeeds[6] + FanSpeedOffset + InletOffset;
        else if (cpuTemperature > 65)
            return FanSpeeds[5] + FanSpeedOffset + InletOffset;
        else if (cpuTemperature > 60)
            return FanSpeeds[4] + FanSpeedOffset + InletOffset;
        else if (cpuTemperature > 55)
            return FanSpeeds[3] + FanSpeedOffset + InletOffset;
        else if (cpuTemperature > 50)
            return FanSpeeds[2] + FanSpeedOffset + InletOffset;
        else if (cpuTemperature > 45)
            return FanSpeeds[1] + FanSpeedOffset + InletOffset;
        else
            return FanSpeeds[0] + FanSpeedOffset + InletOffset;
    }

    private int GetFanSpeedPci(int exhaustTemperature, FanType fanSelection, int inletOffset)
    {
        int FanSpeedOffset;
        int[] FanSpeeds = FANSPEEDSDEFAULT;

        switch (fanSelection)
        {
            case FanType.Nic:
                FanSpeedOffset = NIC_OFFSET;
                break;
            case FanType.Other:
                FanSpeedOffset = PCI_OFFSET;
                break;
            default:
                FanSpeedOffset = 0;
                break;
        }

        switch (exhaustTemperature)
        {
            case > 75:
                return FanSpeeds[7] + FanSpeedOffset + inletOffset;
            case > 70:
                return FanSpeeds[6] + FanSpeedOffset + inletOffset;
            case > 65:
                return FanSpeeds[5] + FanSpeedOffset + inletOffset;
            case > 60:
                return FanSpeeds[4] + FanSpeedOffset + inletOffset;
            case > 55:
                return FanSpeeds[3] + FanSpeedOffset + inletOffset;
            case > 50:
                return FanSpeeds[2] + FanSpeedOffset + inletOffset;
            case > 45:
                return FanSpeeds[1] + FanSpeedOffset + inletOffset;
            default:
                return FanSpeeds[0] + FanSpeedOffset + inletOffset;
        }
    }


    
}
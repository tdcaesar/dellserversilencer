namespace DellServerSilencer;

public class TemperatureThresholdSettings
{
    public int[] Thresholds { get; }

    public TemperatureThresholdSettings(int[] thresholds)
    {
        Thresholds = new int[8];
        if (thresholds.Length != 8)
            throw new ArgumentException("Thresholds must be 8 elements long", nameof(thresholds));
        int priorThreshold = 0; 
        for(int index = 0; index < thresholds.Length; index++)
        {
            int currentThreshold = thresholds[index];
            if (currentThreshold < 0 || currentThreshold > 100)
                throw new ArgumentException("Thresholds must be between 0 and 100", nameof(thresholds));
            if (currentThreshold < priorThreshold)
                throw new ArgumentException("Thresholds must be in ascending order", nameof(thresholds));
            priorThreshold = currentThreshold;
            Thresholds[index] = currentThreshold;
        }
    }

    public int GetIndex(int value)
    {
        if (value > Thresholds[7])
            return 8 ;
        else if (value > Thresholds[6])
            return 7;
        else if (value > Thresholds[5])
            return 6;
        else if (value > Thresholds[4])
            return 5;
        else if (value > Thresholds[3])
            return 4;
        else if (value > Thresholds[2])
            return 3;
        else if (value > Thresholds[1])
            return 2;
        else if (value > Thresholds[0])
            return 1;
        else
            return 0;
    }
}
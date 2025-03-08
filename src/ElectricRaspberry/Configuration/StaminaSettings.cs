namespace ElectricRaspberry.Configuration;

public class StaminaSettings
{
    public const string Stamina = "StaminaSettings";
    
    public int MaxStamina { get; set; } = 100;
    public double MessageCost { get; set; } = 0.5;
    public double VoiceMinuteCost { get; set; } = 1.0;
    public double EmotionalSpikeCost { get; set; } = 2.0;
    public double RecoveryRatePerMinute { get; set; } = 0.2;
    public double SleepRecoveryMultiplier { get; set; } = 3.0;
    public int LowStaminaThreshold { get; set; } = 20;
}
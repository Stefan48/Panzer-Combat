public class Ability
{
    public AbilityType Type { get; private set; }
    public float Duration { get; private set; }
    private static readonly float s_tripleShellsDuration = 5f;
    private static readonly float s_deflectShellsDuration = 5f;
    private static readonly float s_laserBeamDuration = 2.5f;
    private static readonly float s_mineDuration = UiManager.AbilityPanelShrinkTime;
    public bool IsActive = false;
    public float Timer = 0f;


    public Ability(AbilityType type)
    {
        Type = type;
        Duration = GetDurationFromType(type);
    }

    private static float GetDurationFromType(AbilityType type)
    {
        switch (type)
        {
            case AbilityType.TripleShells:
                return s_tripleShellsDuration;
            case AbilityType.DeflectShells:
                return s_deflectShellsDuration;
            case AbilityType.LaserBeam:
                return s_laserBeamDuration;
            case AbilityType.Mine:
                return s_mineDuration;
            default:
                return 0f;
        }
    }
}


public enum AbilityType
{
    TripleShells,
    DeflectShells,
    LaserBeam,
    Mine
}

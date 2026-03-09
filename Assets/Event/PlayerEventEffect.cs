using UnityEngine;

public enum PlayerEffectType
{
    SpeedUp, SpeedDown, GravityUp, GravityDown, JumpBoost, Macroization,
    Microization, JumpInfinity, ChangeSkin, Invisibility, ReverseControl
}

[CreateAssetMenu(fileName = "PlayerEventEffect", menuName = "EventData/PlayerEventEffect")]
public class PlayerEventEffect : ScriptableObject
{
    public PlayerEffectType effectType;
    public float multiplier;
}

using UnityEngine;

[CreateAssetMenu(fileName = "EventData", menuName = "EventData/EventData")]
public class EventData : ScriptableObject
{
    public string eventName;

    public enum EventType
    {
        Sample
    }
    public EventType eventType = EventType.Sample;
    
    /*
    public ParticleEventEffect particleEffect;
    public PlayerEventEffect playerEffect;
    public RoomEventEffect roomEffect;
    */

    public int duration;
    public float particleWeightingRate = 1f;
}

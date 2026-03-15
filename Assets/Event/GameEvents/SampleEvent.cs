using UnityEngine;

public class SampleEvent : GameEvent
{
    public override void StartEvent()
    {
        Debug.Log("SampleEvent started.");
    }

    public override void EndEvent()
    {
        Debug.Log("SampleEvent ended.");
    }
}

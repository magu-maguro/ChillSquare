using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UniRx;

public class EventManager : MonoBehaviour
{
    public Subject<EventData> OnEventStart = new Subject<EventData>();
    public Subject<EventData> OnEventEnd = new Subject<EventData>();
    public float CurrentParticleWeightingRate { get; private set; } = 1f;

    [SerializeField]
    List<EventData> eventList;

    private Coroutine activeEventCoroutine;
    private GameEvent activeGameEvent;
    private EventData activeEventData;

    [Header("Helpers")]
    [SerializeField]
    private TerrainEventBridge terrainEventBridge;

    public void TriggerEvent()
    {
        Debug.Log("Event triggered!");

        if (eventList == null || eventList.Count == 0)
        {
            Debug.LogWarning("Event list is empty.");
            return;
        }

        FinishCurrentEvent();

        EventData eventData = SelectEvent();
        if (eventData == null)
        {
            Debug.LogWarning("Failed to select event data.");
            return;
        }

        activeEventData = eventData;
        
        
        SetParametersForEvent(activeEventData);

        OnEventStart.OnNext(activeEventData);

        activeGameEvent = CreateEvent(activeEventData);//GameEventを継承したクラスを作成
        activeGameEvent?.StartEvent();//作成したクラスのメソッドを実行

        activeEventCoroutine = StartCoroutine(EventTimer(activeEventData.duration));
    }

    IEnumerator EventTimer(int duration)
    {
        yield return new WaitForSeconds(duration);
        FinishCurrentEvent();
    }

    private void FinishCurrentEvent()
    {
        if (activeEventCoroutine != null)
        {
            StopCoroutine(activeEventCoroutine);
            activeEventCoroutine = null;
        }

        if (activeGameEvent != null)
        {
            activeGameEvent.EndEvent();
            activeGameEvent = null;
        }

        if (activeEventData != null)
        {
            OnEventEnd.OnNext(activeEventData);
            activeEventData = null;
        }

        ResetParametersAfterEvent();
    }

    EventData SelectEvent()
    {
        return eventList[Random.Range(0, eventList.Count)];
    }

    GameEvent CreateEvent(EventData data)
    {
        switch (data.eventType)
        {
            case EventData.EventType.Sample:
                return new SampleEvent();
        }

        return null;
    }

    private void SetParametersForEvent(EventData data)
    {
        CurrentParticleWeightingRate = data.particleWeightingRate;
        terrainEventBridge.ApplyTerrainEffect(data.terrainEffect);
    }

    private void ResetParametersAfterEvent()
    {
        CurrentParticleWeightingRate = 1f;
        terrainEventBridge.ResetTerrainEffect();
    }
}
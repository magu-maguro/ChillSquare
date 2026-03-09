using UnityEngine;
using System.Collections.Generic;
using UniRx;

public class EventManager : MonoBehaviour
{
    public Subject<EventData> OnEventStart = new Subject<EventData>();

    public void TriggerEvent()
    {
        EventData eventData = SelectEvent();

        OnEventStart.OnNext(eventData);
    }

    EventData SelectEvent()
    {
        return eventList[Random.Range(0, eventList.Count)];
    }

    [SerializeField]
    List<EventData> eventList;
}
using UnityEngine;
using UniRx;

public class ParticleEventBridge : MonoBehaviour
{
    [SerializeField] private ParticleManager particleManager;
    [SerializeField] private EventManager eventManager;
    
    long nextThreshold = 100;

    void Start()
    {
        particleManager.totalCollected
            .Subscribe(CheckThreshold)
            .AddTo(this);
    }

    void CheckThreshold(long total)
    {
        if (total >= nextThreshold)
        {
            eventManager.TriggerEvent();
            nextThreshold = CalculateNextThreshold(total);
        }
    }

    long CalculateNextThreshold(long current)
    {
        return current * 2;
    }
}

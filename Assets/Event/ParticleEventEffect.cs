using UnityEngine;

[CreateAssetMenu(fileName = "ParticleEventEffect", menuName = "EventData/ParticleEventEffect")]
public class ParticleEventEffect : ScriptableObject
{
    public GameObject particlePrefab;
    public ParticleBehavior particleBehavior;

    public float spawnInterval;
}

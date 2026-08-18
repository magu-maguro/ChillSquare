using UnityEngine;

[CreateAssetMenu(fileName = "ParticleEventEffect", menuName = "EventData/ParticleEventEffect")]
public class ParticleEventEffect : ScriptableObject
{
    //public GameObject particlePrefab;
    public GameObject spawnVfxPrefab;
    public ParticleBehavior behavior;

    //public float spawnInterval;
}

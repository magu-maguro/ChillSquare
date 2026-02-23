using UnityEngine;

[CreateAssetMenu(fileName = "ParticleData", menuName = "Scriptable Objects/ParticleData")]
public class ParticleData : ScriptableObject
{
    public int value;
    public Color color;
    public bool isNetworked;
    public bool isMoving;
    public float moveSpeed;
}

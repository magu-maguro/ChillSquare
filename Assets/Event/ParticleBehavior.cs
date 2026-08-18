using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(fileName = "ParticleBehavior", menuName = "EventData/ParticleBehavior")]
public class ParticleBehavior : ScriptableObject
{
    #region 生成時
    [Header("生成時の挙動パラメータ")]
    [Header("Scale")]
    public Vector3 startScale = Vector3.zero;
    public Vector3 endScale = Vector3.one;
    public float scaleDuration = 0.3f;

    [Header("Rotation")]
    public float startRotationZ = -180f;
    public float rotateAmountZ = 360f;
    public float rotationDuration = 0.4f;

    [Header("Timing")]
    public float delay = 0f;
    public Ease ease = Ease.OutBack;
    #endregion
}

using System;
using UnityEngine;

public sealed class RippleEffect : MonoBehaviour
{
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int ShapeId = Shader.PropertyToID("_Shape");
    private static readonly int RotationId = Shader.PropertyToID("_Rotation");

    [SerializeField] private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;

    private float elapsedTime;
    private float lifeTime = 2f;
    private Vector3 startScale;
    private Vector3 endScale;

    private Action<RippleEffect> onFinished;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void Play(
        Vector3 position,
        Color color,
        int shape,
        float rotation,
        float duration,
        float startSize,
        float endSize,
        Action<RippleEffect> finishedCallback)
    {
        Initialize();

        transform.position = position;
        transform.rotation = Quaternion.identity;

        elapsedTime = 0f;
        lifeTime = Mathf.Max(0.01f, duration);

        startScale = Vector3.one * startSize;
        endScale = Vector3.one * endSize;
        transform.localScale = startScale;

        onFinished = finishedCallback;

        propertyBlock.Clear();
        propertyBlock.SetFloat(ProgressId, 0f);
        propertyBlock.SetColor(ColorId, color);
        propertyBlock.SetFloat(ShapeId, shape);
        propertyBlock.SetFloat(RotationId, rotation);

        spriteRenderer.SetPropertyBlock(propertyBlock);

        gameObject.SetActive(true);
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        float progress = Mathf.Clamp01(elapsedTime / lifeTime);
        float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

        
        transform.localScale = Vector3.LerpUnclamped(
            startScale,
            endScale,
            easedProgress
        );

        spriteRenderer.color = new Color(
            spriteRenderer.color.r,
            spriteRenderer.color.g,
            spriteRenderer.color.b,
            0.7f - easedProgress
        );


        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(ProgressId, easedProgress);
        spriteRenderer.SetPropertyBlock(propertyBlock);
        //Debug.Log("RippleEffect progress: " + easedProgress);

        if (progress >= 1f)
        {
            Finish();
        }
    }

    private void Finish()
    {
        //Debug.Log("RippleEffect finished");
        gameObject.SetActive(false);

        Action<RippleEffect> callback = onFinished;
        onFinished = null;

        callback?.Invoke(this);
    }
}
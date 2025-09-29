using UnityEngine;

public class PostEffectManager : MonoBehaviour
{
    public PostEffectRendererFeature postEffectFeature;
    [Header("Materials")]
    [SerializeField] private Material glayscaleMat;
    private Material currentMat = null;
    private PlayerInputActions inputActions;
    void Awake()
    {
        inputActions = new PlayerInputActions();
        inputActions.PostEffect.Enable();
        inputActions.PostEffect.ChangeEffect.performed += ctx => ChangePostEffect();
    }

    private void ChangePostEffect()
    {
        if (currentMat == null)
        {
            postEffectFeature.SetMaterial(glayscaleMat);
            currentMat = glayscaleMat;
        }
        else
        {
            postEffectFeature.SetMaterial(null);
            currentMat = null;
        }
    }
}

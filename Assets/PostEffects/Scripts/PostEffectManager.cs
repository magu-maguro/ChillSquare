using System.Collections.Generic;
using UnityEngine;

public class PostEffectManager : MonoBehaviour
{
    public PostEffectRendererFeature postEffectFeature;
    [Header("常に適用するベースマテリアル")]
    [SerializeField] private Material baselineMaterial;
    [Header("重ねられる追加マテリアル")]
    [SerializeField] private List<Material> additionalMaterials;
    
    private int currentAdditionalIndex = -1;  // -1 = なし, 0以上 = インデックス

    private PlayerInputActions inputActions;
    void Awake()
    {
        // ベースマテリアルを初期設定
        if (postEffectFeature != null && baselineMaterial != null)
        {
            postEffectFeature.SetBaselineMaterial(baselineMaterial);
        }

        inputActions = new PlayerInputActions();
        inputActions.PostEffect.Enable();
        inputActions.PostEffect.ChangeEffect.performed += ctx => ChangePostEffect();
    }

    private void ChangePostEffect()
    {
        if (additionalMaterials == null || additionalMaterials.Count == 0)
            return;

        // 次のインデックスに進む
        currentAdditionalIndex++;
        if (currentAdditionalIndex >= additionalMaterials.Count)
        {
            currentAdditionalIndex = -1;
        }

        // 追加マテリアルを更新
        postEffectFeature.ClearAdditionalMaterials();
        if (currentAdditionalIndex >= 0)
        {
            postEffectFeature.AddAdditionalMaterial(additionalMaterials[currentAdditionalIndex]);
        }
    }
}

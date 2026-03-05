using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class PostEffectRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material baselineMaterial;
        [SerializeField] public List<Material> additionalMaterials = new List<Material>();
        public string profilerTag = "CustomPostEffect";
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    class CustomRenderPass : ScriptableRenderPass
    {
        private Material baselineMaterial;
        private List<Material> additionalMaterials;
        private string profilerTag;

        public CustomRenderPass(string tag)
        {
            profilerTag = tag;
        }

        public void Setup(Material baseline, List<Material> additional)
        {
            baselineMaterial = baseline;
            additionalMaterials = additional;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // マテリアルが無ければ何もしない
            if (baselineMaterial == null && (additionalMaterials == null || additionalMaterials.Count == 0))
                return;

            // フレームのリソースを取得
            var resourceData = frameData.Get<UniversalResourceData>();
            var src = resourceData.activeColorTexture;
            if (!src.IsValid())
                return;

            // 基本マテリアルを適用
            TextureHandle current = src;
            if (baselineMaterial != null)
            {
                TextureHandle destination = renderGraph.CreateTexture(src, profilerTag + "_Baseline", false);
                var blitParams = new RenderGraphUtils.BlitMaterialParameters(current, destination, baselineMaterial, 0);
                renderGraph.AddBlitPass(blitParams, profilerTag + " Baseline Blit");
                current = destination;
            }

            // 追加マテリアルを順番に重ねる
            if (additionalMaterials != null)
            {
                for (int i = 0; i < additionalMaterials.Count; i++)
                {
                    if (additionalMaterials[i] == null)
                        continue;

                    TextureHandle destination = renderGraph.CreateTexture(src, profilerTag + "_Additional_" + i, false);
                    var blitParams = new RenderGraphUtils.BlitMaterialParameters(current, destination, additionalMaterials[i], 0);
                    renderGraph.AddBlitPass(blitParams, profilerTag + " Additional Blit " + i);
                    current = destination;
                }
            }

            // 最終結果を cameraColor として使うように差し替える
            resourceData.cameraColor = current;
        }


        private class PassData
        {
            public TextureHandle source;
            public Material material;
        }

    }

    public Settings settings = new Settings();
    private CustomRenderPass customPass;

    public override void Create()
    {
        customPass = new CustomRenderPass(settings.profilerTag)
        {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        customPass.Setup(settings.baselineMaterial, settings.additionalMaterials);
        renderer.EnqueuePass(customPass);
    }

    // --- API: 外部から基本マテリアル設定 ---
    public void SetBaselineMaterial(Material newMat)
    {
        settings.baselineMaterial = newMat;
    }

    // --- API: 外部から追加マテリアルリストを設定 ---
    public void SetAdditionalMaterials(List<Material> mats)
    {
        settings.additionalMaterials = mats ?? new List<Material>();
    }

    // --- API: 追加マテリアルをクリア ---
    public void ClearAdditionalMaterials()
    {
        settings.additionalMaterials.Clear();
    }

    // --- API: 追加マテリアルを追加 ---
    public void AddAdditionalMaterial(Material mat)
    {
        if (mat != null)
            settings.additionalMaterials.Add(mat);
    }
}

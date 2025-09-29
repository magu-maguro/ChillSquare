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
        public Material blitMaterial;
        public string profilerTag = "CustomPostEffect";
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    class CustomRenderPass : ScriptableRenderPass
    {
        private Material blitMaterial;
        private string profilerTag;

        public CustomRenderPass(string tag)
        {
            profilerTag = tag;
        }

        public void Setup(Material material)
        {
            blitMaterial = material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // マテリアルが無ければ何もしない（nullで無害化できるように）
            if (blitMaterial == null)
                return;

            // フレームのリソースを取得
            var resourceData = frameData.Get<UniversalResourceData>();
            var src = resourceData.activeColorTexture;
            if (!src.IsValid())
                return;

            // src と同じ定義の一時テクスチャを作る（名前はデバッグ用）
            // renderGraph.CreateTexture(TextureHandle) のオーバーロードを使うと簡単
            TextureHandle destination = renderGraph.CreateTexture(src, profilerTag + "_Temp", false);

            // マテリアルが設定されているなら BlitPass を使う
            var blitParams = new RenderGraphUtils.BlitMaterialParameters(src, destination, blitMaterial, 0);
            renderGraph.AddBlitPass(blitParams, profilerTag + " Blit");

            // 以降のパス（URP内部や他の RendererFeature）がこの destination を cameraColor として使うように差し替える
            resourceData.cameraColor = destination;

            // ※ ここで return せずに他の AddBlitPass / AddCopyPass を続ければ多段処理も可能です。
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
        customPass.Setup(settings.blitMaterial);
        renderer.EnqueuePass(customPass);
    }

    // --- API: 外部からマテリアル差し替え ---
    public void SetMaterial(Material newMat)
    {
        settings.blitMaterial = newMat;
    }
}

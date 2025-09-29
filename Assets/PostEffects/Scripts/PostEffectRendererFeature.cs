using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

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
            if (blitMaterial == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();

            using (var builder = renderGraph.AddRenderPass<PassData>(profilerTag, out var passData))
            {
                // カメラのカラーターゲットを読み書きで使用
                passData.source = resourceData.activeColorTexture;
                builder.WriteTexture(passData.source);

                builder.SetRenderFunc((PassData data, RenderGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), blitMaterial, 0);
                });
            }
        }

        private class PassData
        {
            public TextureHandle source;
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

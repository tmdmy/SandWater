using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace SandWater.SsfRendering
{
    /// <summary>
    /// Render Graph implementation of Screen-Space Fluid (SSF) rendering for URP 17 / Unity 6.
    /// Pipeline:
    ///   1. Particle billboard depth render (filtered by ShaderTagId "SsfBillboardSphereDepth")
    ///   2. Bilateral blur chain (down + up sampling)
    ///   3. Depth -> normal reconstruction (packed into RGBA8)
    ///   4. Camera color copy (read-write workaround)
    ///   5. Final composite blit back to camera color, with refraction + edge detection
    /// </summary>
    public class SsfPass : ScriptableRenderPass, IDisposable
    {
        private static readonly ShaderTagId k_SsfDepthShaderTagId = new ShaderTagId("SsfBillboardSphereDepth");

        private static readonly int k_DepthThresholdId = Shader.PropertyToID("_DepthThreshold");
        private static readonly int k_MatrixClipToViewId = Shader.PropertyToID("_MatrixClipToView");
        private static readonly int k_SsfDepthNormalTextureId = Shader.PropertyToID("_SsfDepthNormalTexture");

        private const int k_DownSamplingPass = 0;
        private const int k_UpSamplingPass = 1;
        private const int k_DepthNormalPass = 2;
        private const int k_LitPass = 3;

        private readonly SsfRendererFeature.SsfSettings _settings;
        private readonly Material _material;
        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Ssf");

        private RTHandle _cameraColorCopy;

        public SsfPass(SsfRendererFeature.SsfSettings settings, Material material)
        {
            _settings = settings;
            _material = material;
            renderPassEvent = settings.Event;
        }

        public void Dispose()
        {
            _cameraColorCopy?.Release();
            _cameraColorCopy = null;
        }

        private class DrawSpheresPassData
        {
            public RendererListHandle rendererList;
        }

        private class LitPassData
        {
            public TextureHandle source;
            public TextureHandle depthNormal;
            public Material material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();

            if (cameraData.camera.cameraType != CameraType.Game) return;
            if (resourceData.isActiveTargetBackBuffer) return;

            var cameraColor = resourceData.activeColorTexture;
            if (!cameraColor.IsValid()) return;

            var camDesc = cameraData.cameraTargetDescriptor;
            int width = camDesc.width;
            int height = camDesc.height;
            if (width <= 0 || height <= 0) return;

            // ---- 1. Particle billboard depth ----
            var depthDesc = new TextureDesc(width, height)
            {
                name = "_SsfParticleDepth",
                format = GraphicsFormat.R32_SFloat,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                clearBuffer = true,
                clearColor = Color.black,
                msaaSamples = MSAASamples.None,
            };
            var particleDepthHandle = renderGraph.CreateTexture(depthDesc);

            using (var builder = renderGraph.AddRasterRenderPass<DrawSpheresPassData>(
                "Ssf Particle Depth", out var passData, _profilingSampler))
            {
                var sortFlags = cameraData.defaultOpaqueSortFlags;
                var drawSettings = RenderingUtils.CreateDrawingSettings(
                    k_SsfDepthShaderTagId, renderingData, cameraData, lightData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;

                var filterSettings = new FilteringSettings(
                    new RenderQueueRange(_settings.RenderQueueLowerBound, _settings.RenderQueueUpperBound),
                    _settings.LayerMask);

                var rendererListParams = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
                rendererListParams.filteringSettings.batchLayerMask = uint.MaxValue;
                passData.rendererList = renderGraph.CreateRendererList(rendererListParams);

                builder.UseRendererList(passData.rendererList);
                builder.SetRenderAttachment(particleDepthHandle, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((DrawSpheresPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }

            // ---- 2. Bilateral blur (down + up sampling chain) ----
            TextureHandle blurredDepth = particleDepthHandle;
            int iterations = Mathf.Max(0, _settings.BlurryIterations);

            if (iterations > 0)
            {
                var blurChain = new TextureHandle[iterations];
                int curW = width, curH = height;

                for (int i = 0; i < iterations; i++)
                {
                    curW = Mathf.Max(1, curW / 2);
                    curH = Mathf.Max(1, curH / 2);
                    var bDesc = new TextureDesc(curW, curH)
                    {
                        name = "_SsfBlur_" + i,
                        format = GraphicsFormat.R32_SFloat,
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        msaaSamples = MSAASamples.None,
                    };
                    blurChain[i] = renderGraph.CreateTexture(bDesc);

                    var downPara = new RenderGraphUtils.BlitMaterialParameters(blurredDepth, blurChain[i], _material, k_DownSamplingPass);
                    renderGraph.AddBlitPass(downPara, "Ssf DownSample " + i);
                    blurredDepth = blurChain[i];
                }

                for (int i = iterations - 2; i >= 0; i--)
                {
                    var upPara = new RenderGraphUtils.BlitMaterialParameters(blurredDepth, blurChain[i], _material, k_UpSamplingPass);
                    renderGraph.AddBlitPass(upPara, "Ssf UpSample " + i);
                    blurredDepth = blurChain[i];
                }
            }

            // ---- 3. Reconstruct depth + normal -> RGBA8 ----
            var dnDesc = new TextureDesc(width, height)
            {
                name = "_SsfDepthNormal",
                format = GraphicsFormat.R8G8B8A8_UNorm,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                msaaSamples = MSAASamples.None,
            };
            var depthNormalHandle = renderGraph.CreateTexture(dnDesc);

            // Per-camera material parameters set before queueing the blit
            var clipToView = GL.GetGPUProjectionMatrix(cameraData.camera.projectionMatrix, true).inverse;
            _material.SetMatrix(k_MatrixClipToViewId, clipToView);
            _material.SetFloat(k_DepthThresholdId, _settings.DepthThreshold);

            var dnPara = new RenderGraphUtils.BlitMaterialParameters(blurredDepth, depthNormalHandle, _material, k_DepthNormalPass);
            renderGraph.AddBlitPass(dnPara, "Ssf DepthNormal");

            // ---- 4. Camera color copy (read-write workaround) ----
            var copyDesc = camDesc;
            copyDesc.depthBufferBits = 0;
            copyDesc.msaaSamples = 1;
            RenderingUtils.ReAllocateHandleIfNeeded(ref _cameraColorCopy, copyDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_SsfCameraColorCopy");
            var cameraCopyHandle = renderGraph.ImportTexture(_cameraColorCopy);

            renderGraph.AddBlitPass(cameraColor, cameraCopyHandle, Vector2.one, Vector2.zero, passName: "Ssf CameraColorCopy");

            // ---- 5. SSF lit composite back to camera color ----
            using (var builder = renderGraph.AddRasterRenderPass<LitPassData>("Ssf Lit", out var passData, _profilingSampler))
            {
                passData.source = cameraCopyHandle;
                passData.depthNormal = depthNormalHandle;
                passData.material = _material;

                builder.UseTexture(cameraCopyHandle);
                builder.UseTexture(depthNormalHandle);
                builder.SetRenderAttachment(cameraColor, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((LitPassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(k_SsfDepthNormalTextureId, data.depthNormal);
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, k_LitPass);
                });
            }
        }
    }
}

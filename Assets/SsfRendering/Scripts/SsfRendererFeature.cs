using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SandWater.SsfRendering
{
    /// <summary>
    /// Screen-Space Fluid (SSF) renderer feature for URP 17 / Unity 6.
    /// Reads particles tagged "SsfBillboardSphereDepth" and renders them as a smooth fluid surface.
    /// Original author: hadashiA / 2020. Adapted for Render Graph API by SandWater project.
    /// </summary>
    [DisallowMultipleRendererFeature("Ssf")]
    public class SsfRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public sealed class SsfSettings
        {
            public Shader SsfShader;
            public RenderPassEvent Event = RenderPassEvent.BeforeRenderingTransparents;

            public LayerMask LayerMask = -1;

            public Color Tint = new Color(0.2f, 0.55f, 0.9f, 1f);
            public Color AmbientColor = new Color(0.4f, 0.5f, 0.6f, 1f);
            public Color SpecularColor = Color.white;

            [Range(0.5f, 5f)] public float Glossiness = 2f;

            [Range(0f, 1f)] public float DepthThreshold = 0.001f;
            [Range(0, 20)] public int DepthScaleFactor = 1;

            public float DistortionStrength = 0.05f;

            [Range(0, 16)] public int BlurryIterations = 3;

            public Color EdgeColor = Color.black;
            [Range(1, 10)] public int EdgeScaleFactor = 2;
            [Range(0f, 1f)] public float EdgeDepthThreshold = 0.2f;
            [Range(0f, 1f)] public float EdgeNormalThreshold = 0.2f;

            [Range(0, 5000)] public int RenderQueueLowerBound = 0;
            [Range(0, 5000)] public int RenderQueueUpperBound = 5000;
        }

        [SerializeField] private SsfSettings settings = new SsfSettings();

        private Material _ssfMaterial;
        private SsfPass _pass;

        public override void Create()
        {
            var shader = settings.SsfShader != null ? settings.SsfShader : Shader.Find("SampleSsf/Hidden/Ssf");
            if (shader == null)
            {
                Debug.LogWarning("[SsfRendererFeature] SSF shader not found. Assign it on the renderer feature or ensure 'SampleSsf/Hidden/Ssf' compiles.");
                return;
            }

            _ssfMaterial = CoreUtils.CreateEngineMaterial(shader);
            _pass = new SsfPass(settings, _ssfMaterial);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null || _ssfMaterial == null) return;
            if (renderingData.cameraData.cameraType != CameraType.Game) return;

            ApplyMaterialSettings();
            _pass.renderPassEvent = settings.Event;
            renderer.EnqueuePass(_pass);
        }

        private void ApplyMaterialSettings()
        {
            _ssfMaterial.SetColor("_Tint", settings.Tint);
            _ssfMaterial.SetColor("_AmbientColor", settings.AmbientColor);
            _ssfMaterial.SetColor("_SpecColor", settings.SpecularColor);
            _ssfMaterial.SetFloat("_Gloss", settings.Glossiness);
            _ssfMaterial.SetFloat("_DepthThreshold", settings.DepthThreshold);
            _ssfMaterial.SetFloat("_DepthScaleFactor", settings.DepthScaleFactor);
            _ssfMaterial.SetFloat("_DistortionStrength", settings.DistortionStrength);
            _ssfMaterial.SetColor("_EdgeColor", settings.EdgeColor);
            _ssfMaterial.SetFloat("_EdgeScaleFactor", settings.EdgeScaleFactor);
            _ssfMaterial.SetFloat("_EdgeDepthThreshold", settings.EdgeDepthThreshold);
            _ssfMaterial.SetFloat("_EdgeNormalThreshold", settings.EdgeNormalThreshold);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
            _pass = null;

            if (_ssfMaterial != null)
            {
                CoreUtils.Destroy(_ssfMaterial);
                _ssfMaterial = null;
            }
        }
    }
}

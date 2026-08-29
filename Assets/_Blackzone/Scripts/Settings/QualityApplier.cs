using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Blackzone.Settings
{
    /// <summary>
    /// Applies the three graphics presets (LOW 30fps / MEDIUM 45fps / HIGH 60fps)
    /// to the active URP asset and runtime settings. Safe to call at boot and
    /// again from the settings screen.
    /// </summary>
    public static class QualityApplier
    {
        private static Light sunLight;

        /// <summary>Registered by the map builder so presets can toggle sun shadows.</summary>
        public static void RegisterSun(Light light) => sunLight = light;

        public static void Apply(int quality)
        {
            quality = Mathf.Clamp(quality, 0, 2);
            int[] frameRates = { 30, 45, 60 };
            float[] renderScales = { 0.7f, 0.85f, 1f };
            float[] shadowDistances = { 12f, 25f, 45f };
            int[] msaa = { 0, 2, 4 };
            bool[] shadows = { false, true, true };

            Application.targetFrameRate = frameRates[quality];
            QualitySettings.vSyncCount = 0;

            var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null)
                urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;

            if (urp != null)
            {
                urp.renderScale = renderScales[quality];
                urp.msaaSampleCount = msaa[quality];
                urp.shadowDistance = shadowDistances[quality];
                urp.mainLightCastShadows = shadows[quality] ? LightShadows.Soft : LightShadows.None;
            }

            if (sunLight != null)
                sunLight.shadows = shadows[quality] ? LightShadows.Soft : LightShadows.None;

            OnQualityChanged?.Invoke(quality);
        }

        public static event System.Action<int> OnQualityChanged;
    }
}

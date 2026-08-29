using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Blackzone.World
{
    /// <summary>
    /// Sets up a global URP post-processing volume with desert military atmosphere:
    /// color grading (warm desert tones), bloom, vignette, and depth of field.
    /// Called once at boot after the player camera is created.
    /// </summary>
    public static class PostProcessingSetup
    {
        private static Volume volume;
        private static VolumeProfile profile;

        /// <summary>
        /// Creates a global Volume with post-processing effects and enables
        /// post-processing on the given camera.
        /// </summary>
        public static void Setup(Camera camera)
        {
            // --- Enable post-processing on the camera ---
            if (camera != null)
            {
                var camData = camera.GetComponent<UniversalAdditionalCameraData>();
                if (camData == null)
                    camData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                camData.renderPostProcessing = true;
            }

            // --- Create global volume ---
            var volumeGo = new GameObject("[PostProcessing Volume]");
            volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;

            // --- Desert atmosphere color grading ---
            var color = profile.Add<ColorAdjustments>();
            color.active = true;
            color.postExposure.Override(0.15f);      // slight brightness boost
            color.contrast.Override(8f);              // slightly punchier
            color.saturation.Override(-12f);           // slightly desaturated desert feel
            color.hueShift.Override(4f);               // warm hue shift
            color.temperature.Override(8f);            // warm temperature
            color.tint.Override(3f);                   // warm tint

            // --- Bloom (sun glare, weapon flashes) ---
            var bloom = profile.Add<Bloom>();
            bloom.active = true;
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.4f);
            bloom.scatter.Override(0.6f);              // soft bloom
            bloom.tint.Override(new Color(1f, 0.95f, 0.85f)); // warm bloom tint
            bloom.highQualityFiltering.Override(false); // medium quality for mobile perf
            bloom.skipIterations.Override(1);

            // --- Vignette (FPS immersion, focus attention to crosshair) ---
            var vignette = profile.Add<Vignette>();
            vignette.active = true;
            vignette.intensity.Override(0.25f);
            vignette.smoothness.Override(0.35f);
            vignette.roundness.Override(1f);
            vignette.color.Override(new Color(0.05f, 0.03f, 0.01f)); // dark warm edge

            // --- Depth of Field (subtle background blur for tactical focus) ---
            var dof = profile.Add<DepthOfField>();
            dof.active = true;
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianFocusStart.Override(8f);       // focus starts near player
            dof.gaussianFocusEnd.Override(120f);        // far objects blur subtly
            dof.gaussianMaxRadius.Override(1.2f);      // gentle bokeh
            dof.gaussianNearSampleCount.Override(4);   // mobile-friendly
            dof.gaussianFarSampleCount.Override(4);
        }

        /// <summary>Adjusts post-processing quality per quality preset.</summary>
        public static void SetQuality(int quality)
        {
            if (profile == null) return;

            quality = Mathf.Clamp(quality, 0, 2);

            // Bloom intensity scales with quality
            float[] bloomIntensities = { 0.2f, 0.35f, 0.5f };
            if (profile.TryGet<Bloom>(out var bloom))
                bloom.intensity.Override(bloomIntensities[quality]);

            // Vignette fades on LOW
            float[] vignetteIntensities = { 0.15f, 0.22f, 0.28f };
            if (profile.TryGet<Vignette>(out var vignette))
                vignette.intensity.Override(vignetteIntensities[quality]);

            // Depth of field off on LOW, subtle on MED, full on HIGH
            if (profile.TryGet<DepthOfField>(out var dof))
            {
                if (quality == 0)
                {
                    dof.active = false;
                }
                else
                {
                    dof.active = true;
                    dof.gaussianMaxRadius.Override(quality == 1 ? 0.8f : 1.2f);
                }
            }
        }

        /// <summary>Full reset: reapply all effects for current quality.</summary>
        public static void Refresh(int quality)
        {
            SetQuality(quality);
        }
    }
}

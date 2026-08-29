using UnityEngine;

namespace Blackzone.World
{
    /// <summary>
    /// Procedural desert skybox using Unity's built-in Skybox/Procedural shader.
    /// Warm tan horizon fading to a pale desert sky, with atmospheric haze.
    /// URP-compatible — no pink/magenta shaders.
    /// </summary>
    public static class DesertSkybox
    {
        private const string ProceduralShader = "Skybox/Procedural";

        /// <summary>
        /// Applies a desert military skybox to RenderSettings.
        /// Falls back to flat color sky if shader is unavailable.
        /// </summary>
        public static void Apply()
        {
            var shader = Shader.Find(ProceduralShader);
            if (shader != null)
            {
                var mat = new Material(shader);

                // Desert sky: warm tan at horizon, pale blue-gray at zenith
                mat.SetFloat("_SunDisk", 1f);          // 0=None 1=Simple 2=HighQuality
                mat.SetFloat("_SunSize", 0.04f);       // small tactical sun

                mat.SetColor("_SkyTint", new Color(0.85f, 0.88f, 0.92f));    // pale sky tint
                mat.SetColor("_GroundColor", new Color(0.78f, 0.73f, 0.65f)); // warm desert ground
                mat.SetFloat("_Exposure", 1.1f);       // slightly brighter
                mat.SetFloat("_AtmosphereThickness", 1.2f); // thicker atmosphere = more haze

                // Skybox rotation for sun position alignment
                mat.SetFloat("_Rotation", 328f);       // ~-32° to match sun rotation in MapBuilder

                RenderSettings.skybox = mat;
            }
            else
            {
                // Fallback: set ambient skybox and fog-only sky
                var fallback = Shader.Find("Universal Render Pipeline/Unlit");
                if (fallback != null)
                {
                    var mat = new Material(fallback);
                    mat.color = new Color(0.72f, 0.78f, 0.84f); // pale blue fallback
                    RenderSettings.skybox = mat;
                }
            }

            // Ambient lighting from skybox
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.0f;

            // Update reflection settings for metallic surfaces
            RenderSettings.reflectionIntensity = 0.6f;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = 128;
        }
    }
}

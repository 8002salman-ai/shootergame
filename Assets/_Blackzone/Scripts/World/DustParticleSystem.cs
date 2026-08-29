using UnityEngine;

namespace Blackzone.World
{
    /// <summary>
    /// Desert dust particle system driven by the WindManager. Two layers:
    ///  - Ambient ground dust: small, slow, always present — fine grit blowing
    ///    low across the terrain. Emission and velocity scale with wind strength.
    ///  - Gust dust cloud: larger, faster particles that surge during gusts,
    ///    creating visible wind-carried dust clouds.
    ///
    /// Both layers use a box emitter covering the playable area. Particles
    /// are billboard-rendered, short-lived, and low-count for mobile perf.
    /// Total particle budget: ~180 max (100 ambient + 80 gust).
    /// </summary>
    public sealed class DustParticleSystem : MonoBehaviour
    {
        private ParticleSystem ambientDust;
        private ParticleSystem gustDust;

        private ParticleSystem.MainModule ambientMain;
        private ParticleSystem.EmissionModule ambientEmission;
        private ParticleSystem.VelocityOverLifetimeModule ambientVelocity;

        private ParticleSystem.MainModule gustMain;
        private ParticleSystem.EmissionModule gustEmission;
        private ParticleSystem.VelocityOverLifetimeModule gustVelocity;

        private Material ambientMaterial;
        private Material gustMaterial;

        // Box emitter covers the 140x90m play area
        private static readonly Vector3 EmitBoxSize = new Vector3(130f, 1f, 80f);

        private void Awake()
        {
            CreateMaterials();
            BuildAmbientLayer();
            BuildGustLayer();
        }

        private void Update()
        {
            if (WindManager.Instance == null) return;

            float strength = WindManager.Instance.CurrentStrength;
            Vector3 dir = WindManager.Instance.WindDirection;

            UpdateAmbient(strength, dir);
            UpdateGust(strength, dir);
        }

        // ==============================================================
        // AMBIENT GROUND DUST
        // ==============================================================

        private void BuildAmbientLayer()
        {
            var go = new GameObject("AmbientDust");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            ambientDust = go.AddComponent<ParticleSystem>();
            ambientDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ambientMain = ambientDust.main;
            ambientMain.loop = true;
            ambientMain.startLifetime = 2.5f;
            ambientMain.startSpeed = 0.8f;
            ambientMain.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            ambientMain.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.78f, 0.72f, 0.58f, 0.35f),   // light tan
                new Color(0.68f, 0.62f, 0.50f, 0.25f));  // slightly darker
            ambientMain.maxParticles = 100;
            ambientMain.simulationSpace = ParticleSystemSimulationSpace.World;
            ambientMain.gravityModifier = -0.05f;          // float upward slightly
            ambientMain.playOnAwake = true;
            ambientMain.scalingMode = ParticleSystemScimationMode.Hierarchy;

            var ambientShape = ambientDust.shape;
            ambientShape.shapeType = ParticleSystemShapeType.Box;
            ambientShape.scale = EmitBoxSize;

            ambientEmission = ambientDust.emission;
            ambientEmission.rateOverTime = 15f;

            ambientVelocity = ambientDust.velocityOverLifetime;
            ambientVelocity.enabled = true;
            ambientVelocity.space = ParticleSystemSimulationSpace.World;
            ambientVelocity.x = new ParticleSystem.MinMaxCurve(0.3f);
            ambientVelocity.y = new ParticleSystem.MinMaxCurve(0.05f);
            ambientVelocity.z = new ParticleSystem.MinMaxCurve(0.1f);

            // Fade in/out over lifetime
            var ambientSizeOverLifetime = ambientDust.sizeOverLifetime;
            ambientSizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.3f),
                new Keyframe(0.2f, 1f),
                new Keyframe(0.8f, 1f),
                new Keyframe(1f, 0f));
            ambientSizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Color fade over lifetime
            var ambientColorOverLifetime = ambientDust.colorOverLifetime;
            ambientColorOverLifetime.enabled = true;
            Gradient alphaGradient = new Gradient();
            alphaGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f) });
            ambientColorOverLifetime.color = alphaGradient;

            // Renderer
            var ambientRenderer = go.GetComponent<ParticleSystemRenderer>();
            ambientRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            ambientRenderer.material = ambientMaterial;
            ambientRenderer.sortingOrder = -1;

            ambientDust.Play();
        }

        private void UpdateAmbient(float strength, Vector3 dir)
        {
            // Emission scales with wind: 5/sec calm → 25/sec during gust
            float emissionRate = Mathf.Lerp(5f, 25f, strength);
            ambientEmission.rateOverTime = emissionRate;

            // Velocity follows wind direction, scales with strength
            float speed = Mathf.Lerp(0.2f, 1.8f, strength);
            ambientVelocity.x = new ParticleSystem.MinMaxCurve(dir.x * speed);
            ambientVelocity.y = new ParticleSystem.MinMaxCurve(0.05f + strength * 0.1f);
            ambientVelocity.z = new ParticleSystem.MinMaxCurve(dir.z * speed);

            // Slight size increase during gusts
            ambientMain.startSize = new ParticleSystem.MinMaxCurve(
                0.03f + strength * 0.02f,
                0.08f + strength * 0.04f);
        }

        // ==============================================================
        // GUST DUST CLOUD
        // ==============================================================

        private void BuildGustLayer()
        {
            var go = new GameObject("GustDust");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.0f, 0f);

            gustDust = go.AddComponent<ParticleSystem>();
            gustDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            gustMain = gustDust.main;
            gustMain.loop = true;
            gustMain.startLifetime = 1.8f;
            gustMain.startSpeed = 2.5f;
            gustMain.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.20f);
            gustMain.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.82f, 0.76f, 0.62f, 0.45f),   // bright tan cloud
                new Color(0.72f, 0.66f, 0.54f, 0.30f));  // softer edge
            gustMain.maxParticles = 80;
            gustMain.simulationSpace = ParticleSystemSimulationSpace.World;
            gustMain.gravityModifier = -0.08f;            // dust rises in gusts
            gustMain.playOnAwake = true;
            gustMain.scalingMode = ParticleSystemScimationMode.Hierarchy;

            var gustShape = gustDust.shape;
            gustShape.shapeType = ParticleSystemShapeType.Box;
            gustShape.scale = EmitBoxSize;

            gustEmission = gustDust.emission;
            gustEmission.rateOverTime = 0f;               // starts at 0, ramps with gusts

            gustVelocity = gustDust.velocityOverLifetime;
            gustVelocity.enabled = true;
            gustVelocity.space = ParticleSystemSimulationSpace.World;
            gustVelocity.x = new ParticleSystem.MinMaxCurve(1.0f);
            gustVelocity.y = new ParticleSystem.MinMaxCurve(0.2f);
            gustVelocity.z = new ParticleSystem.MinMaxCurve(0.5f);

            // Turbulence via velocity noise
            gustVelocity.enabled = true;

            // Size pulse during gusts
            var gustSizeOverLifetime = gustDust.sizeOverLifetime;
            gustSizeOverLifetime.enabled = true;
            AnimationCurve gustSizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(0.15f, 1.2f),
                new Keyframe(0.6f, 1f),
                new Keyframe(1f, 0f));
            gustSizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, gustSizeCurve);

            // Strong alpha fade
            var gustColorOverLifetime = gustDust.colorOverLifetime;
            gustColorOverLifetime.enabled = true;
            Gradient gustAlpha = new Gradient();
            gustAlpha.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.1f),
                    new GradientAlphaKey(0.6f, 0.5f),
                    new GradientAlphaKey(0f, 1f) });
            gustColorOverLifetime.color = gustAlpha;

            // Renderer
            var gustRenderer = go.GetComponent<ParticleSystemRenderer>();
            gustRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            gustRenderer.material = gustMaterial;
            gustRenderer.sortingOrder = 0;

            gustDust.Play();
        }

        private void UpdateGust(float strength, Vector3 dir)
        {
            // Gust dust emission: 0 at base, surges to 40/sec during gusts
            float gustFactor = Mathf.InverseLerp(0.6f, 1.4f, strength);
            float emissionRate = Mathf.Lerp(0f, 40f, gustFactor);
            gustEmission.rateOverTime = emissionRate;

            // Gust velocity: fast, in wind direction
            float speed = Mathf.Lerp(1.0f, 4.0f, strength);
            gustVelocity.x = new ParticleSystem.MinMaxCurve(dir.x * speed);
            gustVelocity.y = new ParticleSystem.MinMaxCurve(0.15f + strength * 0.3f);
            gustVelocity.z = new ParticleSystem.MinMaxCurve(dir.z * speed);

            // Larger particles during strong gusts
            gustMain.startSize = new ParticleSystem.MinMaxCurve(
                0.08f + gustFactor * 0.10f,
                0.20f + gustFactor * 0.15f);
        }

        // ==============================================================
        // MATERIALS (unlit, semi-transparent, wind-tinted)
        // ==============================================================

        private void CreateMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");

            ambientMaterial = new Material(shader);
            ambientMaterial.color = new Color(0.75f, 0.70f, 0.56f, 0.30f);
            ambientMaterial.SetFloat("_Smoothness", 0f);
            SetMaterialTransparent(ambientMaterial);

            gustMaterial = new Material(shader);
            gustMaterial.color = new Color(0.80f, 0.74f, 0.60f, 0.40f);
            gustMaterial.SetFloat("_Smoothness", 0f);
            SetMaterialTransparent(gustMaterial);
        }

        private static void SetMaterialTransparent(Material mat)
        {
            // Standard transparency setup — works with both URP Unlit and Standard
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private void OnDestroy()
        {
            if (ambientMaterial != null) Destroy(ambientMaterial);
            if (gustMaterial != null) Destroy(gustMaterial);
        }
    }
}

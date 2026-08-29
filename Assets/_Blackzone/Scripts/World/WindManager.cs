using System.Collections.Generic;
using Blackzone.Utilities;
using UnityEngine;

namespace Blackzone.World
{
    /// <summary>
    /// Global wind controller for the desert military base. Drives URP shader
    /// global properties (_WindDir, _WindStrength, _WindSpeed) that the
    /// Blackzone/WindUnlit shader reads each frame for vertex displacement.
    /// Also manages sandbag barrier sway (gentle rotation, no shader needed).
    ///
    /// Desert wind: predominantly from one direction with gusts, carrying fine
    /// dust — subtle but constant. The wind speed varies between a base crawl
    /// and occasional gusts that make grass tufts lean and bushes sway.
    /// </summary>
    public sealed class WindManager : MonoBehaviour
    {
        public static WindManager Instance { get; private set; }

        [Header("Wind Direction (horizontal, normalised at runtime)")]
        [SerializeField] private Vector3 windDirection = new Vector3(0.7f, 0f, 0.3f);

        [Header("Wind Strength")]
        [SerializeField] private float baseStrength = 0.5f;
        [SerializeField] private float gustStrength = 1.4f;
        [SerializeField] private float gustFrequency = 0.12f;    // gusts per second
        [SerializeField] private float gustDuration = 2.5f;      // seconds a gust lasts

        [Header("Sandbag Sway")]
        [SerializeField] private float sandbagSwayAngle = 1.2f;  // max degrees
        [SerializeField] private float sandbagSwaySpeed = 0.8f;

        // Shader property IDs (cached for performance)
        private static readonly int PropWindDir = Shader.PropertyToID("_WindDir");
        private static readonly int PropWindStrength = Shader.PropertyToID("_WindStrength");
        private static readonly int PropWindSpeed = Shader.PropertyToID("_WindSpeed");

        private float currentStrength;
        private float gustTimer;
        private bool inGust;
        private float gustTimeLeft;

        private Vector3 normalizedDir;
        private float globalTime;

        // Sandbag barriers registered by TerrainProps for sway animation
        private readonly List<Transform> sandbagRoots = new List<Transform>();
        private readonly List<Vector3> sandbagBaseEulers = new List<Vector3>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            normalizedDir = windDirection.normalized;
            currentStrength = baseStrength;
            gustTimer = 0f;
        }

        private void Update()
        {
            globalTime += Time.deltaTime;
            UpdateGustCycle();
            UpdateShaderGlobals();
            UpdateSandbagSway();
        }

        // ---------------------------------------------------------------
        // Gust cycle
        // ---------------------------------------------------------------

        private void UpdateGustCycle()
        {
            gustTimer += Time.deltaTime;

            if (inGust)
            {
                gustTimeLeft -= Time.deltaTime;
                if (gustTimeLeft <= 0f)
                {
                    inGust = false;
                    gustTimer = 0f;
                }
                // Smooth ramp up then taper during a gust
                float gustProgress = 1f - (gustTimeLeft / gustDuration);
                float gustEnvelope = Mathf.Sin(gustProgress * Mathf.PI); // smooth bell curve
                currentStrength = Mathf.Lerp(baseStrength, gustStrength, gustEnvelope);
            }
            else
            {
                currentStrength = Mathf.Lerp(currentStrength, baseStrength, Time.deltaTime * 2f);
                if (gustTimer > 1f / gustFrequency)
                {
                    inGust = true;
                    gustTimeLeft = gustDuration;
                    gustTimer = 0f;
                }
            }
        }

        // ---------------------------------------------------------------
        // Shader globals (set once per frame — negligible cost)
        // ---------------------------------------------------------------

        private void UpdateShaderGlobals()
        {
            Shader.SetGlobalVector(PropWindDir, (Vector4)normalizedDir);
            Shader.SetGlobalFloat(PropWindStrength, currentStrength);
            Shader.SetGlobalFloat(PropWindSpeed, sandbagSwaySpeed + currentStrength * 0.4f);
        }

        // ---------------------------------------------------------------
        // Sandbag sway (rotation-based, no shader required)
        // ---------------------------------------------------------------

        /// <summary>Register a sandbag barrier root for gentle sway animation.</summary>
        public void RegisterSandbag(Transform sandbagRoot)
        {
            if (sandbagRoot == null || sandbagRoots.Contains(sandbagRoot)) return;
            sandbagRoots.Add(sandbagRoot);
            sandbagBaseEulers.Add(sandbagRoot.localEulerAngles);
        }

        private void UpdateSandbagSway()
        {
            if (sandbagRoots.Count == 0) return;

            float swayAmount = sandbagSwayAngle * currentStrength * 0.3f;
            float swayPhase = globalTime * sandbagSwaySpeed;

            for (int i = sandbagRoots.Count - 1; i >= 0; i--)
            {
                if (sandbagRoots[i] == null)
                {
                    sandbagRoots.RemoveAt(i);
                    sandbagBaseEulers.RemoveAt(i);
                    continue;
                }

                // Sway in the wind direction (tilt around the axis perpendicular to wind)
                float swayX = Mathf.Sin(swayPhase + sandbagRoots[i].position.x * 0.1f) * swayAmount;
                float swayZ = Mathf.Cos(swayPhase * 0.7f + sandbagRoots[i].position.z * 0.15f) * swayAmount * 0.6f;

                Vector3 baseEuler = sandbagBaseEulers[i];
                sandbagRoots[i].localEulerAngles = new Vector3(
                    baseEuler.x + swayZ,
                    baseEuler.y,
                    baseEuler.z + swayX
                );
            }
        }

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        /// <summary>Change wind direction at runtime (e.g. for weather events).</summary>
        public void SetWindDirection(Vector3 dir)
        {
            windDirection = dir;
            normalizedDir = dir.normalized;
        }

        /// <summary>Change base wind intensity (0 = calm, 1 = strong desert wind).</summary>
        public void SetBaseStrength(float strength)
        {
            baseStrength = Mathf.Clamp01(strength);
        }

        /// <summary>Current effective wind strength (base + gust).</summary>
        public float CurrentStrength => currentStrength;

        /// <summary>True when a gust is active (strength ramping above base).</summary>
        public bool IsGusting => inGust;

        /// <summary>Gust progress 0→1 during a gust, 0 otherwise.</summary>
        public float GustProgress => inGust ? 1f - (gustTimeLeft / gustDuration) : 0f;

        /// <summary>Normalized wind direction.</summary>
        public Vector3 WindDirection => normalizedDir;
    }
}

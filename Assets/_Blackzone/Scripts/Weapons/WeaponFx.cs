using Blackzone.Utilities;
using UnityEngine;

namespace Blackzone.Weapons
{
    /// <summary>
    /// Enhanced pooled visual effects for weapons:
    ///  - Muzzle flash: multi-quad flash with point light for weapon illumination
    ///  - Tracer: glowing stretched quad with trail
    ///  - Impact: spark shower + dust puff + decal placeholder
    ///  - Shell ejection: spent casing particle burst
    ///
    /// All materials use URP Unlit — no pink shaders. Pooled via ObjectPool.
    /// </summary>
    public static class WeaponFx
    {
        // Prefabs
        private static GameObject tracerPrefab;
        private static GameObject muzzlePrefab;
        private static GameObject impactPrefab;
        private static GameObject sparkPrefab;
        private static GameObject dustPrefab;
        private static GameObject shellPrefab;

        // Materials
        private static Material tracerMat;
        private static Material flashMat;
        private static Material impactMat;
        private static Material sparkMat;
        private static Material dustMat;
        private static Material shellMat;

        // Muzzle light (shared, reused)
        private static GameObject muzzleLightGo;
        private static Light muzzleLight;

        /// <summary>Check if FX system has been initialized.</summary>
        public static bool HasInit() => tracerPrefab != null;

        public static void EnsureInit()
        {
            if (tracerPrefab != null) return;

            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) unlit = Shader.Find("Unlit/Color");

            // --- Tracer: bright yellow-white streak ---
            tracerMat = new Material(unlit);
            tracerMat.color = new Color(1f, 0.92f, 0.6f);
            tracerPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tracerPrefab.name = "Tracer";
            Object.Destroy(tracerPrefab.GetComponent<Collider>());
            tracerPrefab.GetComponent<MeshRenderer>().sharedMaterial = tracerMat;

            // --- Muzzle flash: bright hot flash ---
            flashMat = new Material(unlit);
            flashMat.color = new Color(1f, 0.95f, 0.75f);
            muzzlePrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
            muzzlePrefab.name = "MuzzleFlash";
            Object.Destroy(muzzlePrefab.GetComponent<Collider>());
            muzzlePrefab.GetComponent<MeshRenderer>().sharedMaterial = flashMat;

            // --- Impact: dark spark ---
            impactMat = new Material(unlit);
            impactMat.color = new Color(0.9f, 0.75f, 0.4f); // hot spark color
            impactPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            impactPrefab.name = "ImpactSpark";
            Object.Destroy(impactPrefab.GetComponent<Collider>());
            impactPrefab.GetComponent<MeshRenderer>().sharedMaterial = impactMat;

            // --- Spark shower: small bright particles ---
            sparkMat = new Material(unlit);
            sparkMat.color = new Color(1f, 0.85f, 0.5f);
            sparkPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sparkPrefab.name = "Spark";
            Object.Destroy(sparkPrefab.GetComponent<Collider>());
            sparkPrefab.GetComponent<MeshRenderer>().sharedMaterial = sparkMat;

            // --- Dust puff: tan translucent ---
            dustMat = new Material(unlit);
            dustMat.color = new Color(0.72f, 0.66f, 0.52f, 0.5f);
            SetTransparent(dustMat);
            dustPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dustPrefab.name = "DustPuff";
            Object.Destroy(dustPrefab.GetComponent<Collider>());
            dustPrefab.GetComponent<MeshRenderer>().sharedMaterial = dustMat;

            // --- Shell casing: small metallic cylinder ---
            shellMat = new Material(unlit);
            shellMat.color = new Color(0.75f, 0.65f, 0.35f); // brass
            shellPrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shellPrefab.name = "ShellCasing";
            Object.Destroy(shellPrefab.GetComponent<Collider>());
            shellPrefab.GetComponent<MeshRenderer>().sharedMaterial = shellMat;

            // --- Muzzle light (shared point light, toggled per shot) ---
            muzzleLightGo = new GameObject("MuzzleLight");
            muzzleLightGo.AddComponent<MuzzleLightFader>();
            muzzleLight = muzzleLightGo.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.color = new Color(1f, 0.88f, 0.6f);
            muzzleLight.intensity = 0f; // off by default
            muzzleLight.range = 4f;
            muzzleLight.renderMode = LightRenderMode.ForceVertex; // cheap
            muzzleLightGo.SetActive(false);
        }

        // ==============================================================
        // TRACER
        // ==============================================================

        public static void SpawnTracer(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            float length = dir.magnitude;
            if (length < 0.01f) return;

            var item = ObjectPool.Instance.Spawn(tracerPrefab, Vector3.zero, Quaternion.identity, 0.08f);
            if (item == null) return;
            item.transform.position = from + dir * 0.5f;
            item.transform.rotation = Quaternion.LookRotation(dir);
            item.transform.localScale = new Vector3(0.015f, 0.015f, length);
        }

        // ==============================================================
        // MUZZLE FLASH (enhanced: multi-quad + point light)
        // ==============================================================

        public static void SpawnMuzzleFlash(Vector3 position, Vector3 forward)
        {
            // Primary flash quad
            var item = ObjectPool.Instance.Spawn(muzzlePrefab, position, Quaternion.LookRotation(forward), 0.05f);
            if (item != null)
            {
                float sz = Random.Range(0.16f, 0.26f);
                item.transform.localScale = new Vector3(sz, sz, 1f);
            }

            // Secondary flash quad (perpendicular, for volume)
            var item2 = ObjectPool.Instance.Spawn(muzzlePrefab, position, Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 0f, 90f), 0.04f);
            if (item2 != null)
            {
                float sz = Random.Range(0.12f, 0.20f);
                item2.transform.localScale = new Vector3(sz, sz, 1f);
            }

            // Quick point light flash
            if (muzzleLightGo != null)
            {
                muzzleLightGo.transform.position = position + forward * 0.3f;
                muzzleLightGo.SetActive(true);
                muzzleLight.intensity = Random.Range(1.5f, 2.5f);
                // Schedule light off via a coroutine-like approach: just set it off next frame
                muzzleLightGo.SendMessage("CancelInvoke");
                muzzleLightGo.SendMessageDelayed("DeactivateFlash", 0.04f);
            }
        }

        // ==============================================================
        // IMPACT (enhanced: sparks + dust puff)
        // ==============================================================

        public static void SpawnImpact(Vector3 position, Vector3 normal)
        {
            // Primary impact spark
            var item = ObjectPool.Instance.Spawn(impactPrefab, position + normal * 0.02f,
                Quaternion.LookRotation(normal), 0.25f);
            if (item != null)
                item.transform.localScale = new Vector3(0.08f, 0.08f, 0.02f);

            // Spark shower (3-5 small sparks)
            int sparkCount = Random.Range(3, 6);
            for (int i = 0; i < sparkCount; i++)
            {
                var spark = ObjectPool.Instance.Spawn(sparkPrefab, position + normal * 0.03f,
                    Quaternion.identity, 0.18f);
                if (spark == null) break;

                Vector3 sparkDir = normal + Random.insideUnitSphere * 0.8f;
                sparkDir.Normalize();
                spark.transform.rotation = Quaternion.LookRotation(sparkDir);
                spark.transform.localScale = new Vector3(0.02f, 0.02f, 0.04f);

                // Animate spark: small velocity burst via transform
                var mover = spark.AddComponent<SparkMotion>();
                mover.Init(sparkDir * Random.Range(1.5f, 4f));
            }

            // Dust puff (for concrete/sand impacts)
            var dust = ObjectPool.Instance.Spawn(dustPrefab, position + normal * 0.05f,
                Quaternion.identity, 0.35f);
            if (dust != null)
            {
                float sz = Random.Range(0.15f, 0.30f);
                dust.transform.localScale = new Vector3(sz, sz * 0.6f, sz);
            }
        }

        // ==============================================================
        // SHELL EJECTION
        // ==============================================================

        public static void SpawnShellCasing(Vector3 position, Vector3 ejectionDir)
        {
            var shell = ObjectPool.Instance.Spawn(shellPrefab, position, Quaternion.identity, 0.6f);
            if (shell == null) return;

            shell.transform.localScale = new Vector3(0.012f, 0.018f, 0.012f);
            shell.transform.rotation = Random.rotation;

            var mover = shell.AddComponent<SparkMotion>();
            mover.Init(ejectionDir * Random.Range(1.5f, 3f) + Vector3.up * Random.Range(1f, 2f));
            mover.gravity = 9.8f;
        }

        // ==============================================================
        // HELPERS
        // ==============================================================

        private static void SetTransparent(Material mat)
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }

    /// <summary>
    /// Simple velocity+gravity mover for sparks and shell casings.
    /// Self-deactivates when pooled. No allocations in Update.
    /// </summary>
    public sealed class SparkMotion : MonoBehaviour
    {
        private Vector3 velocity;
        private float lifetime;
        private float timer;
        public float gravity;

        public void Init(Vector3 vel)
        {
            velocity = vel;
            timer = 0f;
            lifetime = 0.15f; // short-lived
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                gameObject.SetActive(false);
                return;
            }

            velocity.y -= gravity * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(Vector3.right, 720f * Time.deltaTime);
        }

        private void OnDisable()
        {
            velocity = Vector3.zero;
            timer = 0f;
        }
    }

    /// <summary>
    /// Helper to deactivate the muzzle light after a delay.
    /// Attached to the muzzle light GameObject at init time.
    /// </summary>
    public sealed class MuzzleLightFader : MonoBehaviour
    {
        private Light lightComponent;

        private void Awake()
        {
            lightComponent = GetComponent<Light>();
        }

        public void DeactivateFlash()
        {
            if (lightComponent != null) lightComponent.intensity = 0f;
            gameObject.SetActive(false);
        }
    }
}

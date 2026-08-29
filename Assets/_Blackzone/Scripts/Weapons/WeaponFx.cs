using Blackzone.Utilities;
using UnityEngine;

namespace Blackzone.Weapons
{
    /// <summary>
    /// Pooled visual effects for weapons: tracers, muzzle flashes and impact
    /// sparks. Prefabs are built once from primitives with unlit URP materials.
    /// </summary>
    public static class WeaponFx
    {
        private static GameObject tracerPrefab;
        private static GameObject muzzlePrefab;
        private static GameObject impactPrefab;
        private static Material tracerMat;
        private static Material flashMat;
        private static Material impactMat;

        public static void EnsureInit()
        {
            if (tracerPrefab != null) return;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            tracerMat = new Material(shader) { color = new Color(1f, 0.9f, 0.55f) };
            flashMat = new Material(shader) { color = new Color(1f, 0.95f, 0.7f) };
            impactMat = new Material(shader) { color = new Color(0.25f, 0.22f, 0.18f) };

            // Tracer: thin stretched box aligned along its forward (z).
            tracerPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tracerPrefab.name = "Tracer";
            Object.Destroy(tracerPrefab.GetComponent<Collider>());
            tracerPrefab.GetComponent<MeshRenderer>().sharedMaterial = tracerMat;

            // Muzzle flash: small flat quad.
            muzzlePrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
            muzzlePrefab.name = "MuzzleFlash";
            Object.Destroy(muzzlePrefab.GetComponent<Collider>());
            muzzlePrefab.GetComponent<MeshRenderer>().sharedMaterial = flashMat;

            // Impact spark: tiny cube.
            impactPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            impactPrefab.name = "ImpactSpark";
            Object.Destroy(impactPrefab.GetComponent<Collider>());
            impactPrefab.GetComponent<MeshRenderer>().sharedMaterial = impactMat;
        }

        public static void SpawnTracer(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            float length = dir.magnitude;
            if (length < 0.01f) return;
            var item = ObjectPool.Instance.Spawn(tracerPrefab, Vector3.zero, Quaternion.identity, 0.06f);
            if (item == null) return;
            item.transform.position = from + dir * 0.5f;
            item.transform.rotation = Quaternion.LookRotation(dir);
            item.transform.localScale = new Vector3(0.018f, 0.018f, length);
        }

        public static void SpawnMuzzleFlash(Vector3 position, Vector3 forward)
        {
            var item = ObjectPool.Instance.Spawn(muzzlePrefab, position, Quaternion.LookRotation(forward), 0.045f);
            if (item == null) return;
            item.transform.localScale = new Vector3(Random.Range(0.14f, 0.22f), Random.Range(0.14f, 0.22f), 1f);
        }

        public static void SpawnImpact(Vector3 position, Vector3 normal)
        {
            var item = ObjectPool.Instance.Spawn(impactPrefab, position + normal * 0.02f,
                Quaternion.LookRotation(normal), 0.22f);
            if (item == null) return;
            item.transform.localScale = new Vector3(0.06f, 0.06f, 0.02f);
        }
    }
}

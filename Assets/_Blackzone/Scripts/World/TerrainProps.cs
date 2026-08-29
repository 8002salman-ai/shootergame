using Blackzone.Utilities;
using UnityEngine;

namespace Blackzone.World
{
    /// <summary>
    /// Detailed terrain decoration for the desert military base: sandbag barriers,
    /// rock formations, ground debris, H-barriers, and vegetation detail.
    /// All geometry is procedural primitives — URP Lit shader, no pink materials.
    /// </summary>
    public static class TerrainProps
    {
        private static Material sandbagMat;
        private static Material rockMat;
        private static Material dirtMat;
        private static Material woodMat;
        private static Material corrugatedMat;

        /// <summary>Creates all required materials (call once before building).</summary>
        public static void CreateMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            sandbagMat = Mat(shader, new Color(0.82f, 0.76f, 0.60f), 0.08f);   // tan sandbag
            rockMat = Mat(shader, new Color(0.48f, 0.44f, 0.38f), 0.20f);     // sandstone rock
            dirtMat = Mat(shader, new Color(0.55f, 0.48f, 0.36f), 0.05f);     // dark dirt
            woodMat = Mat(shader, new Color(0.45f, 0.34f, 0.22f), 0.10f);     // weathered wood
            corrugatedMat = Mat(shader, new Color(0.52f, 0.50f, 0.46f), 0.30f); // corrugated metal
        }

        /// <summary>Builds sandbag barrier walls at specified positions.</summary>
        public static void BuildSandbagBarrier(Transform parent, Vector3 position,
            Quaternion rotation, int rows, int columns)
        {
            var root = new GameObject("SandbagBarrier").transform;
            root.SetParent(parent, false);
            root.position = position;
            root.rotation = rotation;

            for (int row = 0; row < rows; row++)
            {
                float y = row * 0.22f;
                for (int col = 0; col < columns; col++)
                {
                    float x = (col - columns * 0.5f + 0.5f) * 0.55f;
                    // Alternate offset for realistic stacking
                    float xOff = (row % 2 == 0) ? 0f : 0.275f;

                    var bag = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    bag.name = "Sandbag";
                    bag.layer = GameConstants.LayerWorld;
                    bag.isStatic = true;
                    bag.transform.SetParent(root, false);
                    bag.transform.localPosition = new Vector3(x + xOff, y + 0.12f, 0f);
                    bag.transform.localScale = new Vector3(0.52f, 0.13f, 0.30f);
                    bag.transform.localRotation = Quaternion.Euler(0f, Random.Range(-8f, 8f), 0f);
                    bag.GetComponent<MeshRenderer>().sharedMaterial = sandbagMat;
                }
            }

            // Register with WindManager for gentle sway animation
            if (WindManager.Instance != null)
                WindManager.Instance.RegisterSandbag(root);
        }

        /// <summary>Scatters natural rock formations.</summary>
        public static void BuildRockCluster(Transform parent, Vector3 center,
            int count, float spread)
        {
            var rng = new System.Random((int)(center.x * 17f + center.z * 31f));
            for (int i = 0; i < count; i++)
            {
                float x = center.x + (float)(rng.NextDouble() * 2.0 - 1.0) * spread;
                float z = center.z + (float)(rng.NextDouble() * 2.0 - 1.0) * spread;
                float scale = 0.3f + (float)rng.NextDouble() * 1.2f;

                var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = "Rock";
                rock.layer = GameConstants.LayerWorld;
                rock.isStatic = true;
                rock.transform.SetParent(parent, false);
                rock.transform.position = new Vector3(x, scale * 0.35f, z);
                rock.transform.localScale = new Vector3(
                    scale * (0.8f + (float)rng.NextDouble() * 0.4f),
                    scale * (0.5f + (float)rng.NextDouble() * 0.5f),
                    scale * (0.8f + (float)rng.NextDouble() * 0.4f));
                rock.transform.rotation = Quaternion.Euler(
                    (float)rng.NextDouble() * 20f,
                    (float)rng.NextDouble() * 360f,
                    (float)rng.NextDouble() * 15f);
                rock.GetComponent<MeshRenderer>().sharedMaterial = rockMat;
            }
        }

        /// <summary>H-Barrier: two concrete pillars with a plank/panel between.</summary>
        public static void BuildHBarrier(Transform parent, Vector3 position, Quaternion rotation)
        {
            var root = new GameObject("HBarrier").transform;
            root.SetParent(parent, false);
            root.position = position;
            root.rotation = rotation;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var concrete = Mat(shader, new Color(0.52f, 0.51f, 0.48f), 0.25f);

            // Left pillar
            Box(root, "Pillar", new Vector3(0.2f, 2.0f, 0.2f),
                new Vector3(-1.0f, 1.0f, 0f), concrete);
            // Right pillar
            Box(root, "Pillar", new Vector3(0.2f, 2.0f, 0.2f),
                new Vector3(1.0f, 1.0f, 0f), concrete);
            // Cross panel (metal)
            Box(root, "Panel", new Vector3(2.0f, 0.8f, 0.08f),
                new Vector3(0f, 1.4f, 0f), corrugatedMat);
            // Base plate
            Box(root, "Base", new Vector3(2.4f, 0.15f, 0.4f),
                new Vector3(0f, 0.075f, 0f), concrete);
        }

        /// <summary>Wooden pallet / crate cluster for industrial debris.</summary>
        public static void BuildCrateCluster(Transform parent, Vector3 center,
            int count, float spread)
        {
            var rng = new System.Random((int)(center.x * 11f + center.z * 23f));
            for (int i = 0; i < count; i++)
            {
                float x = center.x + (float)(rng.NextDouble() * 2.0 - 1.0) * spread;
                float z = center.z + (float)(rng.NextDouble() * 2.0 - 1.0) * spread;
                float s = 0.6f + (float)rng.NextDouble() * 0.8f;
                float rot = (float)rng.NextDouble() * 30f - 15f;

                var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crate.name = "Crate";
                crate.layer = GameConstants.LayerWorld;
                crate.isStatic = true;
                crate.transform.SetParent(parent, false);
                crate.transform.position = new Vector3(x, s * 0.35f, z);
                crate.transform.localScale = new Vector3(s, s * 0.7f, s * 0.9f);
                crate.transform.rotation = Quaternion.Euler(0f, rot, 0f);
                crate.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            }
        }

        /// <summary>Scattered ground debris: small rocks, dirt mounds, tire tracks.</summary>
        public static void BuildGroundDetail(Transform parent, int detailCount)
        {
            var rng = new System.Random(42);
            for (int i = 0; i < detailCount; i++)
            {
                float x = (float)(rng.NextDouble() * 2.0 - 1.0) * 60f;
                float z = (float)(rng.NextDouble() * 2.0 - 1.0) * 40f;
                float typeRoll = (float)rng.NextDouble();

                if (typeRoll < 0.6f)
                {
                    // Small pebble
                    var pebble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    pebble.name = "Pebble";
                    pebble.layer = GameConstants.LayerWorld;
                    pebble.isStatic = true;
                    pebble.transform.SetParent(parent, false);
                    pebble.transform.position = new Vector3(x, 0.04f, z);
                    float s = 0.06f + (float)rng.NextDouble() * 0.18f;
                    pebble.transform.localScale = new Vector3(s, s * 0.5f, s);
                    pebble.transform.rotation = Quaternion.Euler(
                        (float)rng.NextDouble() * 30f,
                        (float)rng.NextDouble() * 360f, 0f);
                    pebble.GetComponent<MeshRenderer>().sharedMaterial = rockMat;
                }
                else if (typeRoll < 0.85f)
                {
                    // Dirt mound
                    var mound = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    mound.name = "DirtMound";
                    mound.layer = GameConstants.LayerWorld;
                    mound.isStatic = true;
                    mound.transform.SetParent(parent, false);
                    mound.transform.position = new Vector3(x, 0.12f, z);
                    float s = 0.3f + (float)rng.NextDouble() * 0.6f;
                    mound.transform.localScale = new Vector3(s, s * 0.25f, s);
                    mound.GetComponent<MeshRenderer>().sharedMaterial = dirtMat;
                }
                else
                {
                    // Flat debris panel
                    var debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    debris.name = "Debris";
                    debris.layer = GameConstants.LayerWorld;
                    debris.isStatic = true;
                    debris.transform.SetParent(parent, false);
                    debris.transform.position = new Vector3(x, 0.02f, z);
                    float w = 0.3f + (float)rng.NextDouble() * 0.5f;
                    debris.transform.localScale = new Vector3(w, 0.03f, w * 0.7f);
                    debris.transform.rotation = Quaternion.Euler(
                        0f, (float)rng.NextDouble() * 360f, 0f);
                    debris.GetComponent<MeshRenderer>().sharedMaterial = corrugatedMat;
                }
            }
        }

        /// <summary>Oil/drum barrels for industrial clutter.</summary>
        public static void BuildBarrelCluster(Transform parent, Vector3 center, int count)
        {
            var rng = new System.Random((int)(center.x * 13f + center.z * 19f));
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var barrelMat = Mat(shader, new Color(0.35f, 0.38f, 0.28f), 0.35f); // olive barrel

            for (int i = 0; i < count; i++)
            {
                float x = center.x + (float)(rng.NextDouble() * 2.0 - 1.0) * 2f;
                float z = center.z + (float)(rng.NextDouble() * 2.0 - 1.0) * 2f;
                bool tipped = rng.NextDouble() > 0.6;

                var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                barrel.name = "Barrel";
                barrel.layer = GameConstants.LayerWorld;
                barrel.isStatic = true;
                barrel.transform.SetParent(parent, false);
                barrel.transform.position = new Vector3(x, tipped ? 0.35f : 0.55f, z);
                barrel.transform.localScale = tipped
                    ? new Vector3(0.45f, 0.7f, 0.45f)
                    : new Vector3(0.45f, 0.55f, 0.45f);
                barrel.transform.rotation = tipped
                    ? Quaternion.Euler(90f + (float)rng.NextDouble() * 20f - 10f, 0f, 0f)
                    : Quaternion.identity;
                barrel.GetComponent<MeshRenderer>().sharedMaterial = barrelMat;
            }
        }

        // --- Helpers ---

        private static Material Mat(Shader shader, Color color, float smoothness)
        {
            var m = new Material(shader);
            m.color = color;
            m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        private static void Box(Transform parent, string name, Vector3 size,
            Vector3 pos, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = GameConstants.LayerWorld;
            go.isStatic = true;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}

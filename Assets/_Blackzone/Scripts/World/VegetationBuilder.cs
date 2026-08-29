using Blackzone.Utilities;
using UnityEngine;

namespace Blackzone.World
{
    /// <summary>
    /// Desert vegetation builder: grass tufts, scrub bushes, dead bushes, and
    /// dried flowers — all using the Blackzone/WindUnlit shader for procedural
    /// wind animation. Vegetation is NOT static (must animate per frame).
    ///
    /// The desert military base has sparse, hardy vegetation: clumps of dry
    /// grass around rock formations, scrub bushes near building perimeters,
    /// dead bushes scattered along the terrain rim, and occasional dried
    /// wildflowers near sheltered areas.
    /// </summary>
    public static class VegetationBuilder
    {
        private static Material grassMat;
        private static Material shrubMat;
        private static Material deadBushMat;
        private static Material driedFlowerMat;

        /// <summary>Creates wind-animated materials. Call once before building.</summary>
        public static void CreateMaterials()
        {
            var shader = Shader.Find("Blackzone/WindUnlit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Standard");

            bool isWindShader = shader != null && shader.name == "Blackzone/WindUnlit";

            // Desert grass: yellow-brown, sparse and dry
            grassMat = WindMat(shader, new Color(0.68f, 0.62f, 0.40f), 0.06f, isWindShader);

            // Desert scrub bush: olive-green with dusty edges
            shrubMat = WindMat(shader, new Color(0.42f, 0.48f, 0.30f), 0.10f, isWindShader);

            // Dead bush: bleached brown, completely dry
            deadBushMat = WindMat(shader, new Color(0.52f, 0.42f, 0.30f), 0.08f, isWindShader);

            // Dried flower: faded purple-brown
            driedFlowerMat = WindMat(shader, new Color(0.55f, 0.38f, 0.42f), 0.12f, isWindShader);
        }

        // ==================================================================
        // VEGETATION BUILDERS
        // ==================================================================

        /// <summary>
        /// Scatters grass tufts around rock formations and terrain rims.
        /// Each tuft: 3-5 vertical capsules as grass blades, slightly varied height.
        /// </summary>
        public static void BuildGrassTuft(Transform parent, Vector3 center,
            int bladeCount, float spread)
        {
            var root = new GameObject("GrassTuft").transform;
            root.SetParent(parent, false);
            root.position = center;

            var rng = new System.Random((int)(center.x * 23f + center.z * 37f));

            for (int i = 0; i < bladeCount; i++)
            {
                float x = (float)(rng.NextDouble() * 2.0 - 1.0) * spread;
                float z = (float)(rng.NextDouble() * 2.0 - 1.0) * spread;
                float height = 0.25f + (float)rng.NextDouble() * 0.40f;
                float tilt = (float)rng.NextDouble() * 20f - 10f;

                var blade = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                blade.name = "GrassBlade";
                // NOT static — must animate with wind shader
                blade.transform.SetParent(root, false);
                blade.transform.localPosition = new Vector3(x, height * 0.5f, z);
                blade.transform.localScale = new Vector3(0.04f, height, 0.04f);
                blade.transform.localRotation = Quaternion.Euler(tilt, (float)rng.NextDouble() * 360f, tilt * 0.5f);
                blade.GetComponent<MeshRenderer>().sharedMaterial = grassMat;
            }
        }

        /// <summary>
        /// Dense grass scatter at a position — 8-15 blades in a tighter cluster.
        /// </summary>
        public static void BuildGrassCluster(Transform parent, Vector3 center, float radius)
        {
            var rng = new System.Random((int)(center.x * 19f + center.z * 29f));
            int count = 8 + rng.Next(0, 8);
            BuildGrassTuft(parent, center, count, radius);
        }

        /// <summary>
        /// Desert scrub bush: a low trunk with a canopy of spheres.
        /// Moderate wind animation.
        /// </summary>
        public static void BuildDesertShrub(Transform parent, Vector3 position, float scale)
        {
            var root = new GameObject("DesertShrub").transform;
            root.SetParent(parent, false);
            root.position = position;

            // Main trunk (short, thick)
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(root, false);
            trunk.transform.localPosition = new Vector3(0f, 0.25f * scale, 0f);
            trunk.transform.localScale = new Vector3(0.12f * scale, 0.30f * scale, 0.12f * scale);
            trunk.GetComponent<MeshRenderer>().sharedMaterial = deadBushMat;

            // Canopy spheres (irregular cluster)
            int canopyCount = 3 + Random.Range(0, 3);
            for (int i = 0; i < canopyCount; i++)
            {
                float cx = Random.Range(-0.3f, 0.3f) * scale;
                float cy = (0.45f + Random.Range(-0.1f, 0.15f)) * scale;
                float cz = Random.Range(-0.3f, 0.3f) * scale;
                float cs = (0.25f + Random.Range(0f, 0.2f)) * scale;

                var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                canopy.name = "Canopy";
                canopy.transform.SetParent(root, false);
                canopy.transform.localPosition = new Vector3(cx, cy, cz);
                canopy.transform.localScale = new Vector3(cs, cs * 0.7f, cs);
                canopy.GetComponent<MeshRenderer>().sharedMaterial = shrubMat;
            }
        }

        /// <summary>
        /// Dead bush: bleached skeletal branches ( capsules radiating outward).
        /// Strong wind animation (skeleton flexes visibly).
        /// </summary>
        public static void BuildDeadBush(Transform parent, Vector3 position, float scale)
        {
            var root = new GameObject("DeadBush").transform;
            root.SetParent(parent, false);
            root.position = position;

            // Central stem
            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Stem";
            stem.transform.SetParent(root, false);
            stem.transform.localPosition = new Vector3(0f, 0.20f * scale, 0f);
            stem.transform.localScale = new Vector3(0.06f * scale, 0.25f * scale, 0.06f * scale);
            stem.GetComponent<MeshRenderer>().sharedMaterial = deadBushMat;

            // Branches radiating outward
            int branchCount = 4 + Random.Range(0, 3);
            for (int i = 0; i < branchCount; i++)
            {
                float angle = i * (360f / branchCount) + Random.Range(-15f, 15f);
                float length = (0.3f + Random.Range(0f, 0.3f)) * scale;
                float tilt = 35f + Random.Range(-10f, 20f);

                var branch = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                branch.name = "Branch";
                branch.transform.SetParent(root, false);
                branch.transform.localPosition = new Vector3(0f, 0.35f * scale, 0f);
                branch.transform.localScale = new Vector3(0.03f * scale, length, 0.03f * scale);
                branch.transform.localRotation = Quaternion.Euler(tilt, angle, 0f);
                branch.GetComponent<MeshRenderer>().sharedMaterial = deadBushMat;
            }
        }

        /// <summary>
        /// Dried wildflower cluster: thin stems with small round buds.
        /// Very strong wind animation (light and thin).
        /// </summary>
        public static void BuildDriedFlowers(Transform parent, Vector3 center, int count)
        {
            var root = new GameObject("DriedFlowers").transform;
            root.SetParent(parent, false);
            root.position = center;

            var rng = new System.Random((int)(center.x * 41f + center.z * 53f));

            for (int i = 0; i < count; i++)
            {
                float x = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.6f;
                float z = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.6f;
                float height = 0.20f + (float)rng.NextDouble() * 0.30f;

                // Stem
                var stem = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                stem.name = "FlowerStem";
                stem.transform.SetParent(root, false);
                stem.transform.localPosition = new Vector3(x, height * 0.5f, z);
                stem.transform.localScale = new Vector3(0.02f, height, 0.02f);
                stem.GetComponent<MeshRenderer>().sharedMaterial = deadBushMat;

                // Bud (small sphere at top)
                var bud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bud.name = "FlowerBud";
                bud.transform.SetParent(root, false);
                bud.transform.localPosition = new Vector3(x, height + 0.03f, z);
                bud.transform.localScale = Vector3.one * 0.05f;
                bud.GetComponent<MeshRenderer>().sharedMaterial = driedFlowerMat;
            }
        }

        // ==================================================================
        // SCATTER METHODS (convenience wrappers for MapBuilder integration)
        // ==================================================================

        /// <summary>Scatter vegetation across the entire map — called by MapBuilder.</summary>
        public static void ScatterVegetation(Transform parent)
        {
            var rng = new System.Random(333);

            // --- Grass tufts near rock formations ---
            ScatterNear(parent, rng, new Vector3(-58f, 0f, -38f), 4, 5f, ScatterType.Grass);
            ScatterNear(parent, rng, new Vector3(58f, 0f, 38f), 3, 4f, ScatterType.Grass);
            ScatterNear(parent, rng, new Vector3(-55f, 0f, 35f), 5, 5f, ScatterType.Grass);
            ScatterNear(parent, rng, new Vector3(55f, 0f, -35f), 4, 5f, ScatterType.Grass);
            ScatterNear(parent, rng, new Vector3(62f, 0f, -20f), 3, 3f, ScatterType.Grass);
            ScatterNear(parent, rng, new Vector3(-50f, 0f, -10f), 2, 3f, ScatterType.Grass);

            // --- Desert shrubs near buildings and warehouse perimeter ---
            ScatterNear(parent, rng, new Vector3(-42f, 0f, -14f), 2, 4f, ScatterType.Shrub);
            ScatterNear(parent, rng, new Vector3(-20f, 0f, 30f), 2, 3f, ScatterType.Shrub);
            ScatterNear(parent, rng, new Vector3(28f, 0f, -38f), 2, 4f, ScatterType.Shrub);
            ScatterNear(parent, rng, new Vector3(-8f, 0f, -36f), 1, 3f, ScatterType.Shrub);

            // --- Dead bushes along terrain rim edges ---
            ScatterNear(parent, rng, new Vector3(-40f, 0f, 46f), 3, 8f, ScatterType.DeadBush);
            ScatterNear(parent, rng, new Vector3(30f, 0f, 46f), 3, 8f, ScatterType.DeadBush);
            ScatterNear(parent, rng, new Vector3(-40f, 0f, -46f), 2, 6f, ScatterType.DeadBush);
            ScatterNear(parent, rng, new Vector3(35f, 0f, -46f), 2, 6f, ScatterType.DeadBush);
            ScatterNear(parent, rng, new Vector3(64f, 0f, 0f), 2, 5f, ScatterType.DeadBush);
            ScatterNear(parent, rng, new Vector3(-64f, 0f, 0f), 2, 5f, ScatterType.DeadBush);

            // --- Dried flowers in sheltered corners ---
            ScatterNear(parent, rng, new Vector3(-36f, 0f, 26f), 2, 3f, ScatterType.Flower);
            ScatterNear(parent, rng, new Vector3(46f, 0f, 30f), 2, 3f, ScatterType.Flower);
            ScatterNear(parent, rng, new Vector3(-52f, 0f, -28f), 1, 2f, ScatterType.Flower);

            // --- Extra grass along container yard edges ---
            ScatterNear(parent, rng, new Vector3(30f, 0f, 38f), 3, 4f, ScatterType.Grass);
            ScatterNear(parent, rng, new Vector3(66f, 0f, -10f), 2, 3f, ScatterType.Grass);
        }

        private enum ScatterType { Grass, Shrub, DeadBush, Flower }

        private static void ScatterNear(Transform parent, System.Random rng,
            Vector3 center, int count, float spread, ScatterType type)
        {
            for (int i = 0; i < count; i++)
            {
                float x = center.x + (float)(rng.NextDouble() * 2.0 - 1.0) * spread;
                float z = center.z + (float)(rng.NextDouble() * 2.0 - 1.0) * spread;
                Vector3 pos = new Vector3(x, 0f, z);

                switch (type)
                {
                    case ScatterType.Grass:
                        int blades = 5 + rng.Next(0, 6);
                        BuildGrassCluster(parent, pos, 0.8f);
                        break;
                    case ScatterType.Shrub:
                        float shrubScale = 0.7f + (float)rng.NextDouble() * 0.6f;
                        BuildDesertShrub(parent, pos, shrubScale);
                        break;
                    case ScatterType.DeadBush:
                        float bushScale = 0.8f + (float)rng.NextDouble() * 0.5f;
                        BuildDeadBush(parent, pos, bushScale);
                        break;
                    case ScatterType.Flower:
                        int buds = 3 + rng.Next(0, 4);
                        BuildDriedFlowers(parent, pos, buds);
                        break;
                }
            }
        }

        // ==================================================================
        // MATERIAL HELPERS
        // ==================================================================

        private static Material WindMat(Shader shader, Color color, float smoothness, bool isWindShader)
        {
            var m = new Material(shader);
            m.color = color;

            if (isWindShader)
            {
                // Tune wind parameters per vegetation type
                if (color.r > 0.6f) // grass (yellow-brown)
                {
                    m.SetFloat("_WindAmplitude", 0.18f);
                    m.SetFloat("_WindFrequency", 2.2f);
                    m.SetFloat("_WindFlutter", 1.5f);
                    m.SetFloat("_WindFlutterFreq", 5.5f);
                    m.SetFloat("_BendStiffness", 0.7f);
                }
                else if (color.g > 0.4f) // shrub (olive-green)
                {
                    m.SetFloat("_WindAmplitude", 0.10f);
                    m.SetFloat("_WindFrequency", 1.8f);
                    m.SetFloat("_WindFlutter", 0.8f);
                    m.SetFloat("_WindFlutterFreq", 4.0f);
                    m.SetFloat("_BendStiffness", 1.0f);
                }
                else if (color.b > 0.4f) // dried flower (purple-brown)
                {
                    m.SetFloat("_WindAmplitude", 0.22f);
                    m.SetFloat("_WindFrequency", 2.8f);
                    m.SetFloat("_WindFlutter", 2.0f);
                    m.SetFloat("_WindFlutterFreq", 7.0f);
                    m.SetFloat("_BendStiffness", 0.5f);
                }
                else // dead bush (bleached brown)
                {
                    m.SetFloat("_WindAmplitude", 0.14f);
                    m.SetFloat("_WindFrequency", 2.0f);
                    m.SetFloat("_WindFlutter", 1.0f);
                    m.SetFloat("_WindFlutterFreq", 5.0f);
                    m.SetFloat("_BendStiffness", 0.9f);
                }

                m.SetFloat("_Smoothness", smoothness);
            }
            else
            {
                m.SetFloat("_Smoothness", smoothness);
            }

            return m;
        }
    }
}

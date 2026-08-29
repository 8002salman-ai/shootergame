using System.Collections.Generic;
using Blackzone.Settings;
using Blackzone.Utilities;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Blackzone.World
{
    /// <summary>Layout data produced by the map builder and consumed by spawners.</summary>
    public struct MapLayout
    {
        public Vector3 PlayerSpawn;
        public Vector3[] EnemySpawns;
        public Vector3[] Waypoints;
    }

    /// <summary>
    /// BLACKZONE TRAINING OUTPOST: a ~140x90m desert + industrial combat yard
    /// built entirely from primitives at runtime (replaceable later with real
    /// art without gameplay changes). Zones:
    ///  - container yard with narrow passages (east)
    ///  - two warehouses (west)
    ///  - barriers / cover clusters (center)
    ///  - watchtower with elevated position (north-east)
    ///  - one long open sightline north->south
    /// </summary>
    public static class MapBuilder
    {
        private static Material sand;
        private static Material concrete;
        private static Material containerRed;
        private static Material containerSteel;
        private static Material metalDark;
        private static Material crateMat;

        public static MapLayout Build(Transform parent)
        {
            CreateMaterials();
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.62f, 0.58f, 0.50f);
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance = 160f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);

            var world = new GameObject("[World]").transform;
            world.SetParent(parent, false);

            Ground(world);
            Perimeter(world);
            ContainerYard(world);
            Warehouse(world, new Vector3(-32f, 0f, -8f), new Vector3(26f, 5.5f, 18f), 0);
            Warehouse(world, new Vector3(-16f, 0f, 26f), new Vector3(16f, 4.5f, 12f), 90);
            Barriers(world);
            Watchtower(world);
            ScatterCrates(world);
            Sun(world);

            var layout = new MapLayout
            {
                PlayerSpawn = new Vector3(0f, 1f, 38f),
                EnemySpawns = new[]
                {
                    new Vector3(-42f, 0f, -28f),
                    new Vector3(34f, 0f, -26f),
                    new Vector3(52f, 0f, 8f),
                    new Vector3(-30f, 0f, 2f),
                    new Vector3(56f, 0f, -6f),
                    new Vector3(-8f, 0f, 28f),
                    new Vector3(38f, 0f, 30f),
                    new Vector3(-52f, 0f, 6f)
                },
                Waypoints = new[]
                {
                    new Vector3(-55f, 0f, -30f),
                    new Vector3(-55f, 0f, 10f),
                    new Vector3(-36f, 0f, 28f),
                    new Vector3(-4f, 0f, 22f),
                    new Vector3(34f, 0f, 30f),
                    new Vector3(56f, 0f, 12f),
                    new Vector3(56f, 0f, -22f),
                    new Vector3(32f, 0f, -32f),
                    new Vector3(2f, 0f, -30f),
                    new Vector3(-30f, 0f, -36f)
                }
            };

            // NavMesh baked from the generated colliders.
            var surface = world.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.layerMask = GameConstants.WorldMask;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.defaultArea = 0;
            surface.BuildNavMesh();

            return layout;
        }

        // ---------------------------------------------------------------
        // Zones
        // ---------------------------------------------------------------

        private static void Ground(Transform parent)
        {
            Box(parent, "Ground", new Vector3(140f, 1f, 90f), new Vector3(0f, -0.5f, 0f), sand);
        }

        private static void Perimeter(Transform parent)
        {
            const float h = 3.5f;
            const float t = 0.6f;
            // North wall with gate
            Wall(parent, new Vector3(-70f + 24f, h * 0.5f, 45f), new Vector3(48f, h, t));
            Wall(parent, new Vector3(70f - 24f, h * 0.5f, 45f), new Vector3(48f, h, t));
            // South wall with gate
            Wall(parent, new Vector3(-70f + 24f, h * 0.5f, -45f), new Vector3(48f, h, t));
            Wall(parent, new Vector3(70f - 24f, h * 0.5f, -45f), new Vector3(48f, h, t));
            // West wall with gate
            Wall(parent, new Vector3(-70f, h * 0.5f, 0f), new Vector3(t, h, 90f));
            // East wall (solid)
            Wall(parent, new Vector3(70f, h * 0.5f, 0f), new Vector3(t, h, 90f));

            // Gate blocks (angled jersey) flanking the north/south lanes
            Barrier(parent, new Vector3(-5.5f, 0f, 43.5f), Quaternion.identity, new Vector3(3.4f, 1.1f, 0.9f));
            Barrier(parent, new Vector3(5.5f, 0f, 43.5f), Quaternion.identity, new Vector3(3.4f, 1.1f, 0.9f));
            Barrier(parent, new Vector3(-5.5f, 0f, -43.5f), Quaternion.identity, new Vector3(3.4f, 1.1f, 0.9f));
            Barrier(parent, new Vector3(5.5f, 0f, -43.5f), Quaternion.identity, new Vector3(3.4f, 1.1f, 0.9f));
        }

        private static void ContainerYard(Transform parent)
        {
            // Rows of standard 12.2 x 2.44 x 2.59m containers with 2m lanes.
            // x from 30 to 66, two columns; some stacked; gaps form passages.
            float lane = 12.2f + 2.0f;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 2; col++)
                {
                    float x = 34f + row * lane;
                    float z = -18f + col * 14.5f;
                    var c = Container(parent, new Vector3(x, 1.3f, z), Quaternion.identity,
                        row % 2 == 0 ? containerRed : containerSteel);
                    // stacked container on the even rows
                    if (row % 2 == 0)
                        Container(parent, new Vector3(x, 1.3f + 2.59f, z), Quaternion.identity, containerSteel);
                }
            }
            // A few rotated containers forming a corridor near the south end
            Container(parent, new Vector3(48f, 1.3f, -32f), Quaternion.Euler(0f, 22f, 0f), containerRed);
            Container(parent, new Vector3(62f, 1.3f, -34f), Quaternion.Euler(0f, -18f, 0f), containerSteel);
        }

        private static void Warehouse(Transform parent, Vector3 center, Vector3 size, float yaw)
        {
            var root = new GameObject($"Warehouse_{center.x:0}_{center.z:0}").transform;
            root.SetParent(parent, false);
            root.position = center;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Floor slab
            Box(root, "Floor", new Vector3(size.x, 0.2f, size.z), new Vector3(0f, 0.1f, 0f), concrete);
            // Walls with 4m openings on the long sides
            float w = size.x, d = size.z, h = size.y;
            Wall(root, new Vector3(-w * 0.5f + 2f, h * 0.5f, 0f), new Vector3(w - 8f, h, 0.5f));
            Wall(root, new Vector3(w * 0.5f - 2f, h * 0.5f, 0f), new Vector3(w - 8f, h, 0.5f));
            Wall(root, new Vector3(0f, h * 0.5f, d * 0.5f - 0.25f), new Vector3(w, h, 0.5f));
            Wall(root, new Vector3(0f, h * 0.5f, -d * 0.5f + 0.25f), new Vector3(w, h, 0.5f));
            // Roof slab
            Box(root, "Roof", new Vector3(w + 1f, 0.25f, d + 1f), new Vector3(0f, h + 0.1f, 0f), metalDark);
            // Interior crates
            int seed = 1000 + (int)(center.x * 7f) + (int)(center.z * 13f);
            var rng = new System.Random(seed);
            for (int i = 0; i < 6; i++)
            {
                float cx = (float)(rng.NextDouble() * (w - 4f)) - (w - 4f) * 0.5f;
                float cz = (float)(rng.NextDouble() * (d - 4f)) - (d - 4f) * 0.5f;
                Box(root, "Crate", new Vector3(1.6f, 1.1f, 1.0f), new Vector3(cx, 0.55f, cz), crateMat);
            }
        }

        private static void Barriers(Transform parent)
        {
            // Center-lane cover with gaps (the sightline stays open on x ~ [-4,4])
            var rng = new System.Random(2026);
            for (int i = 0; i < 10; i++)
            {
                float x = -16f + i * 3.4f;
                if (x > -4.5f && x < 4.5f) continue; // leave the kill lane open
                Barrier(parent, new Vector3(x, 0f, -8f), Quaternion.identity, new Vector3(2.2f, 1.0f, 0.9f));
            }
            for (int i = 0; i < 8; i++)
            {
                float x = -13f + i * 3.8f;
                if (x > -4.5f && x < 4.5f) continue;
                Barrier(parent, new Vector3(x, 0f, 12f), Quaternion.identity, new Vector3(2.2f, 1.0f, 0.9f));
            }
            // Low concrete walls around the container yard edge
            Wall(parent, new Vector3(27f, 0.6f, -40f), new Vector3(6f, 1.2f, 1f));
            Wall(parent, new Vector3(27f, 0.6f, 40f), new Vector3(6f, 1.2f, 1f));
            // Scattered single barriers
            for (int i = 0; i < 6; i++)
            {
                float x = -60f + (float)rng.NextDouble() * 40f;
                float z = 18f + (float)rng.NextDouble() * 18f;
                Barrier(parent, new Vector3(x, 0f, z), Quaternion.Euler(0f, rng.Next(0, 180), 0f),
                    new Vector3(2.2f, 1.0f, 0.9f));
            }
        }

        private static void Watchtower(Transform parent)
        {
            var root = new GameObject("Watchtower").transform;
            root.SetParent(parent, false);
            root.position = new Vector3(40f, 0f, 38f);

            // Pillars
            for (int i = 0; i < 4; i++)
            {
                float px = (i % 2 == 0 ? -1.9f : 1.9f);
                float pz = (i < 2 ? -1.9f : 1.9f);
                Box(root, "Pillar", new Vector3(0.3f, 3.2f, 0.3f), new Vector3(px, 1.6f, pz), concrete);
            }
            // Platform
            Box(root, "Platform", new Vector3(4.4f, 0.25f, 4.4f), new Vector3(0f, 3.3f, 0f), concrete);
            // Railing
            for (int i = 0; i < 4; i++)
            {
                float px = (i % 2 == 0 ? -2.2f : 2.2f);
                float pz = (i < 2 ? -2.2f : 2.2f);
                Box(root, "Rail", new Vector3(0.08f, 1.0f, 0.08f), new Vector3(px, 3.8f, pz), metalDark);
            }
            // Ramp from the south (slope ~38 deg, walkable by navmesh)
            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Ramp";
            ramp.transform.SetParent(root, false);
            ramp.transform.localPosition = new Vector3(0f, 1.6f, 3.6f);
            ramp.transform.localRotation = Quaternion.Euler(38f, 0f, 0f);
            ramp.transform.localScale = new Vector3(1.6f, 0.15f, 5.2f);
            ramp.GetComponent<MeshRenderer>().sharedMaterial = concrete;
            ramp.layer = GameConstants.LayerWorld;
            ramp.isStatic = true;
        }

        private static void ScatterCrates(Transform parent)
        {
            var rng = new System.Random(77);
            float[,] clusters =
            {
                { 8f, -18f }, { -10f, -20f }, { 14f, 20f }, { -24f, 14f },
                { 24f, 2f }, { -2f, 30f }, { 12f, -32f }, { -40f, 24f }
            };
            for (int c = 0; c < clusters.GetLength(0); c++)
            {
                int n = 3 + rng.Next(0, 3);
                for (int i = 0; i < n; i++)
                {
                    float x = clusters[c, 0] + (float)(rng.NextDouble() * 3.0 - 1.5);
                    float z = clusters[c, 1] + (float)(rng.NextDouble() * 3.0 - 1.5);
                    float s = 0.8f + (float)rng.NextDouble() * 0.8f;
                    Box(parent, "Crate", new Vector3(s, s * 0.7f, s * 0.9f), new Vector3(x, s * 0.35f, z), crateMat);
                }
            }
        }

        private static void Sun(Transform parent)
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(parent, false);
            sunGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            var light = sunGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.92f, 0.82f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
            QualityApplier.RegisterSun(light);

            // Dim fill so shadowed areas stay readable
            var fillGo = new GameObject("FillLight");
            fillGo.transform.SetParent(parent, false);
            fillGo.transform.rotation = Quaternion.Euler(20f, 150f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.45f, 0.55f, 0.7f);
            fill.intensity = 0.25f;
            fill.shadows = LightShadows.None;
        }

        // ---------------------------------------------------------------
        // Primitives
        // ---------------------------------------------------------------

        private static void Wall(Transform parent, Vector3 pos, Vector3 size)
        {
            Box(parent, "Wall", size, pos, concrete);
        }

        private static void Barrier(Transform parent, Vector3 pos, Quaternion rot, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Barrier";
            go.layer = GameConstants.LayerWorld;
            go.isStatic = true;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = concrete;
        }

        private static GameObject Container(Transform parent, Vector3 pos, Quaternion rot, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Container";
            go.layer = GameConstants.LayerWorld;
            go.isStatic = true;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = new Vector3(12.2f, 2.59f, 2.44f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static void Box(Transform parent, string name, Vector3 size, Vector3 pos, Material mat)
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

        private static void CreateMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            sand = Mat(shader, new Color(0.72f, 0.66f, 0.52f), 0.1f);
            concrete = Mat(shader, new Color(0.52f, 0.51f, 0.48f), 0.25f);
            containerRed = Mat(shader, new Color(0.62f, 0.30f, 0.26f), 0.35f);
            containerSteel = Mat(shader, new Color(0.45f, 0.50f, 0.55f), 0.5f);
            metalDark = Mat(shader, new Color(0.22f, 0.22f, 0.24f), 0.6f);
            crateMat = Mat(shader, new Color(0.45f, 0.36f, 0.24f), 0.15f);
        }

        private static Material Mat(Shader shader, Color color, float smoothness)
        {
            var m = new Material(shader);
            m.color = color;
            m.SetFloat("_Smoothness", smoothness);
            return m;
        }
    }
}

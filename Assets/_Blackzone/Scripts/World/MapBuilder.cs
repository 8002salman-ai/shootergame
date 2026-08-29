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
    /// BLACKZONE TRAINING OUTPOST — PREMIUM TACTICAL FPS QUALITY.
    /// ~140×90m desert military base built from procedural primitives at runtime.
    /// Upgraded with:
    ///  - Detailed military buildings with windows, doors, interior props
    ///  - Expanded container yard with corrugated detail and corridors
    ///  - Sandbag barriers, H-barriers, and rock formations
    ///  - Barrel clusters, crate debris, ground scatter
    ///  - Enhanced lighting: warm directional sun + cool fill + rim light
    ///  - Desert procedural skybox, linear fog, and skybox ambient
    ///  - URP post-processing volume (bloom, vignette, color grading, DOF)
    ///  - All URP-compatible materials — no pink/magenta shaders
    ///  - Static-batched geometry for mobile performance
    /// </summary>
    public static class MapBuilder
    {
        private static Material sand;
        private static Material concrete;
        private static Material concreteDark;
        private static Material containerRed;
        private static Material containerSteel;
        private static Material metalDark;
        private static Material crateMat;
        private static Material wallMat;
        private static Material roofMat;
        private static Material sandbagMat;
        private static Material rockMat;
        private static Material dirtMat;

        public static MapLayout Build(Transform parent)
        {
            CreateMaterials();
            TerrainProps.CreateMaterials();
            VegetationBuilder.CreateMaterials();

            // --- Desert atmosphere ---
            DesertSkybox.Apply();

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.68f, 0.62f); // warm desert haze
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 140f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.0f;

            // --- Wind system (created early so sandbags can register during build) ---
            var windGo = new GameObject("[WindManager]");
            windGo.transform.SetParent(parent, false);
            windGo.AddComponent<WindManager>();

            // --- Dust particles (driven by WindManager) ---
            var dustGo = new GameObject("[DustParticles]");
            dustGo.transform.SetParent(parent, false);
            dustGo.AddComponent<DustParticleSystem>();

            var world = new GameObject("[World]").transform;
            world.SetParent(parent, false);

            // --- Core terrain ---
            Ground(world);
            TerrainRim(world);
            Perimeter(world);

            // --- Main zones ---
            ContainerYard(world);
            WarehouseComplex(world);
            Watchtower(world);
            CenterCover(world);
            SandbagPositions(world);
            BarrierLines(world);

            // --- Decorative detail ---
            RockFormations(world);
            BarrelClutter(world);
            CrateDebris(world);
            TerrainProps.BuildGroundDetail(world, 90);

            // --- Environment dressing ---
            Cables(world);
            Signs(world);
            DebrisPiles(world);

            // --- Vegetation (wind-animated) ---
            VegetationBuilder.ScatterVegetation(world);

            // --- Lighting ---
            Lighting(world);

            // --- Gameplay spawns (unchanged from Phase 1) ---
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

            // NavMesh from physics colliders (same as Phase 1)
            var surface = world.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.layerMask = GameConstants.WorldMask;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.defaultArea = 0;
            surface.BuildNavMesh();

            return layout;
        }

        // ==============================================================
        // TERRAIN & GROUND
        // ==============================================================

        private static void Ground(Transform parent)
        {
            // Main ground plane
            Box(parent, "Ground", new Vector3(140f, 0.8f, 90f),
                new Vector3(0f, -0.4f, 0f), sand);
        }

        /// <summary>Low elevated terrain at map edges — visual depth backdrop.</summary>
        private static void TerrainRim(Transform parent)
        {
            float rimH = 1.2f;
            // North elevated strip
            Box(parent, "TerrainRimN", new Vector3(140f, rimH, 6f),
                new Vector3(0f, rimH * 0.5f - 0.5f, 48f), dirtMat);
            // South elevated strip
            Box(parent, "TerrainRimS", new Vector3(140f, rimH, 6f),
                new Vector3(0f, rimH * 0.5f - 0.5f, -48f), dirtMat);
            // East elevated strip (higher — looks like desert cliff face)
            Box(parent, "TerrainRimE", new Vector3(8f, 2.5f, 90f),
                new Vector3(68f, 1.25f - 0.5f, 0f), rockMat);
            // West elevated strip
            Box(parent, "TerrainRimW", new Vector3(8f, 1.8f, 90f),
                new Vector3(-68f, 0.9f - 0.5f, 0f), rockMat);
        }

        /// <summary>Perimeter concrete walls with gate openings.</summary>
        private static void Perimeter(Transform parent)
        {
            const float h = 3.8f;
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

            // Gate jersey barriers
            Barrier(parent, new Vector3(-5.5f, 0f, 43.5f), Quaternion.identity, new Vector3(3.4f, 1.2f, 0.9f));
            Barrier(parent, new Vector3(5.5f, 0f, 43.5f), Quaternion.identity, new Vector3(3.4f, 1.2f, 0.9f));
            Barrier(parent, new Vector3(-5.5f, 0f, -43.5f), Quaternion.identity, new Vector3(3.4f, 1.2f, 0.9f));
            Barrier(parent, new Vector3(5.5f, 0f, -43.5f), Quaternion.identity, new Vector3(3.4f, 1.2f, 0.9f));

            // Perimeter concrete wall detail strips (visual)
            for (int i = 0; i < 14; i++)
            {
                float x = -65f + i * 10f;
                Box(parent, "WallStrip", new Vector3(0.15f, 0.3f, 1.2f),
                    new Vector3(x, h + 0.15f, 45f), concreteDark);
                Box(parent, "WallStrip", new Vector3(0.15f, 0.3f, 1.2f),
                    new Vector3(x, h + 0.15f, -45f), concreteDark);
            }
        }

        // ==============================================================
        // CONTAINER YARD (east side, premium quality)
        // ==============================================================

        private static void ContainerYard(Transform parent)
        {
            float lane = 12.2f + 2.0f;

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 2; col++)
                {
                    float x = 34f + row * lane;
                    float z = -18f + col * 14.5f;
                    bool stacked = row % 2 == 0;
                    var mat = row % 2 == 0 ? containerRed : containerSteel;

                    // Ground container with full detail
                    ContainerDetailed(parent, new Vector3(x, 1.3f, z), Quaternion.identity, mat);

                    // Stacked container (alternate color)
                    if (stacked)
                        ContainerDetailed(parent, new Vector3(x, 1.3f + 2.59f, z),
                            Quaternion.identity, containerSteel);
                }
            }

            // Corridor containers at south end
            ContainerDetailed(parent, new Vector3(48f, 1.3f, -32f),
                Quaternion.Euler(0f, 22f, 0f), containerRed);
            ContainerDetailed(parent, new Vector3(62f, 1.3f, -34f),
                Quaternion.Euler(0f, -18f, 0f), containerSteel);

            // Additional detail: leaning container, damaged container
            ContainerDetailed(parent, new Vector3(58f, 1.0f, 22f),
                Quaternion.Euler(0f, -10f, 5f), containerRed);
            ContainerDetailed(parent, new Vector3(44f, 1.3f, 34f),
                Quaternion.Euler(0f, 35f, 0f), containerSteel);

            // Barrel cluster near containers
            TerrainProps.BuildBarrelCluster(parent, new Vector3(52f, 0f, -14f), 4);
            TerrainProps.BuildCrateCluster(parent, new Vector3(38f, 0f, -30f), 4, 2f);
        }

        /// <summary>Full-detail container with corrugated ribs, corner beams, and markings.</summary>
        private static void ContainerDetailed(Transform parent, Vector3 pos,
            Quaternion rot, Material mat)
        {
            var root = new GameObject("Container").transform;
            root.SetParent(parent, false);
            root.position = pos;
            root.rotation = rot;
            root.gameObject.layer = GameConstants.LayerWorld;
            root.gameObject.isStatic = true;

            float w = 12.2f, h = 2.59f, d = 2.44f;

            // Main body
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.layer = GameConstants.LayerWorld;
            body.isStatic = true;
            body.transform.SetParent(root, false);
            body.transform.localScale = new Vector3(w, h, d);
            body.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Corrugated ribs along the side (visual detail, no collider)
            int ribCount = 10;
            for (int i = 0; i < ribCount; i++)
            {
                float xp = -w * 0.5f + (i + 0.5f) * (w / ribCount);
                var rib = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rib.name = "Rib";
                rib.layer = GameConstants.LayerWorld;
                rib.isStatic = true;
                rib.transform.SetParent(root, false);
                rib.transform.localPosition = new Vector3(xp, 0f, d * 0.501f);
                rib.transform.localScale = new Vector3(0.04f, h * 0.92f, 0.03f);
                rib.GetComponent<MeshRenderer>().sharedMaterial = concreteDark;
            }

            // Backside ribs
            for (int i = 0; i < ribCount; i++)
            {
                float xp = -w * 0.5f + (i + 0.5f) * (w / ribCount);
                var rib = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rib.name = "RibBack";
                rib.layer = GameConstants.LayerWorld;
                rib.isStatic = true;
                rib.transform.SetParent(root, false);
                rib.transform.localPosition = new Vector3(xp, 0f, -d * 0.501f);
                rib.transform.localScale = new Vector3(0.04f, h * 0.92f, 0.03f);
                rib.GetComponent<MeshRenderer>().sharedMaterial = concreteDark;
            }

            // Corner beams
            float hw = w * 0.5f, hd = d * 0.5f;
            Vector3[] corners = {
                new Vector3(-hw, 0, -hd), new Vector3(-hw, 0, hd),
                new Vector3(hw, 0, -hd),  new Vector3(hw, 0, hd)
            };
            foreach (var c in corners)
            {
                var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beam.name = "CornerBeam";
                beam.layer = GameConstants.LayerWorld;
                beam.isStatic = true;
                beam.transform.SetParent(root, false);
                beam.transform.localPosition = new Vector3(c.x, 0f, c.z);
                beam.transform.localScale = new Vector3(0.12f, h + 0.06f, 0.12f);
                beam.GetComponent<MeshRenderer>().sharedMaterial = metalDark;
            }

            // Top edge beams
            Box(root, "TopRail", new Vector3(w + 0.12f, 0.08f, 0.08f),
                new Vector3(0f, h * 0.5f, hd), metalDark);
            Box(root, "TopRail", new Vector3(w + 0.12f, 0.08f, 0.08f),
                new Vector3(0f, h * 0.5f, -hd), metalDark);

            // Door end (one flat end has corrugated door detail)
            Box(root, "DoorEnd", new Vector3(0.08f, h * 0.45f, d * 0.7f),
                new Vector3(-hw + 0.04f, -h * 0.05f, 0f), concreteDark);
        }

        // ==============================================================
        // WAREHOUSE COMPLEX (west side, detailed buildings)
        // ==============================================================

        private static void WarehouseComplex(Transform parent)
        {
            // Main warehouse (large)
            BuildingDetailed(parent, new Vector3(-32f, 0f, -8f),
                new Vector3(26f, 5.5f, 18f), 0f);

            // Secondary warehouse (smaller, rotated)
            BuildingDetailed(parent, new Vector3(-16f, 0f, 26f),
                new Vector3(16f, 4.5f, 12f), 90f);

            // Barrel clusters near warehouses
            TerrainProps.BuildBarrelCluster(parent, new Vector3(-40f, 0f, 8f), 5);
            TerrainProps.BuildBarrelCluster(parent, new Vector3(-22f, 0f, -22f), 3);
        }

        /// <summary>Detailed military warehouse: walls, windows, doorway, interior props.</summary>
        private static void BuildingDetailed(Transform parent, Vector3 center,
            Vector3 size, float yaw)
        {
            var root = new GameObject("Building").transform;
            root.SetParent(parent, false);
            root.position = center;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);

            float w = size.x, d = size.z, h = size.y;

            // Floor slab with thickness
            Box(root, "Floor", new Vector3(w + 0.4f, 0.3f, d + 0.4f),
                new Vector3(0f, 0.15f, 0f), concrete);

            // === Long walls (x-direction) with window openings ===
            float wallY = h * 0.5f;
            float windowH = 2.0f;
            float wallBotY = 0.3f + windowH * 0.5f; // above window bottom

            // Wall sections with gaps for windows (3 windows per long wall)
            float segWidth = w / 6f;
            for (int i = 0; i < 6; i++)
            {
                float xPos = -w * 0.5f + (i + 0.5f) * segWidth;
                // Solid wall section below windows
                Box(root, "Wall", new Vector3(segWidth * 0.92f, h * 0.55f, 0.5f),
                    new Vector3(xPos, h * 0.3f + 0.3f, d * 0.5f - 0.25f), wallMat);
                Box(root, "Wall", new Vector3(segWidth * 0.92f, h * 0.55f, 0.5f),
                    new Vector3(xPos, h * 0.3f + 0.3f, -d * 0.5f + 0.25f), wallMat);
                // Upper wall section above windows
                Box(root, "Wall", new Vector3(segWidth * 0.92f, h * 0.35f, 0.5f),
                    new Vector3(xPos, h - h * 0.175f, d * 0.5f - 0.25f), wallMat);
                Box(root, "Wall", new Vector3(segWidth * 0.92f, h * 0.35f, 0.5f),
                    new Vector3(xPos, h - h * 0.175f, -d * 0.5f + 0.25f), wallMat);
                // Window frame detail
                Box(root, "WindowFrame", new Vector3(segWidth * 0.94f, 0.12f, 0.56f),
                    new Vector3(xPos, h * 0.3f + 0.3f + h * 0.28f, d * 0.5f - 0.25f), metalDark);
                Box(root, "WindowFrame", new Vector3(segWidth * 0.94f, 0.12f, 0.56f),
                    new Vector3(xPos, h * 0.3f + 0.3f + h * 0.28f, -d * 0.5f + 0.25f), metalDark);
            }

            // === End walls (z-direction) with large doorway on one end ===
            // Back wall (solid)
            Wall(root, new Vector3(0f, wallY, -d * 0.5f + 0.25f), new Vector3(w, h, 0.5f));
            // Front wall with 4m doorway in center
            float doorW = 4f;
            Box(root, "Wall", new Vector3((w - doorW) * 0.5f, h, 0.5f),
                new Vector3(-w * 0.5f + (w - doorW) * 0.25f, wallY, d * 0.5f - 0.25f), wallMat);
            Box(root, "Wall", new Vector3((w - doorW) * 0.5f, h, 0.5f),
                new Vector3(w * 0.5f - (w - doorW) * 0.25f, wallY, d * 0.5f - 0.25f), wallMat);
            // Door frame header
            Box(root, "DoorHeader", new Vector3(doorW + 0.3f, 0.25f, 0.56f),
                new Vector3(0f, h * 0.72f, d * 0.5f - 0.25f), metalDark);
            // Door frame sides
            Box(root, "DoorFrame", new Vector3(0.15f, h * 0.72f, 0.56f),
                new Vector3(-doorW * 0.5f, h * 0.36f, d * 0.5f - 0.25f), metalDark);
            Box(root, "DoorFrame", new Vector3(0.15f, h * 0.72f, 0.56f),
                new Vector3(doorW * 0.5f, h * 0.36f, d * 0.5f - 0.25f), metalDark);

            // === Roof with overhang ===
            Box(root, "Roof", new Vector3(w + 1.2f, 0.25f, d + 1.2f),
                new Vector3(0f, h + 0.1f, 0f), roofMat);
            // Roof ridge beam
            Box(root, "Ridge", new Vector3(w + 1.6f, 0.12f, 0.15f),
                new Vector3(0f, h + 0.28f, 0f), metalDark);
            // Roof eave strips
            Box(root, "Eave", new Vector3(w + 1.6f, 0.08f, 0.08f),
                new Vector3(0f, h + 0.05f, d * 0.5f + 0.6f), metalDark);
            Box(root, "Eave", new Vector3(w + 1.6f, 0.08f, 0.08f),
                new Vector3(0f, h + 0.05f, -d * 0.5f - 0.6f), metalDark);

            // === Elevated walkway (ring around upper level) ===
            float walkW = 1.2f;
            float walkY = h - 0.4f;
            // Front walkway
            Box(root, "Walkway", new Vector3(w + 0.3f, 0.12f, walkW),
                new Vector3(0f, walkY, d * 0.5f + walkW * 0.5f + 0.15f), concrete);
            // Side walkway
            Box(root, "Walkway", new Vector3(walkW, 0.12f, d + 0.3f),
                new Vector3(w * 0.5f + walkW * 0.5f + 0.15f, walkY, 0f), concrete);
            // Guard railing on walkway
            Box(root, "Railing", new Vector3(w + 0.3f, 0.8f, 0.05f),
                new Vector3(0f, walkY + 0.46f, d * 0.5f + walkW + 0.25f), metalDark);
            Box(root, "Railing", new Vector3(0.05f, 0.8f, d + 0.3f),
                new Vector3(w * 0.5f + walkW + 0.25f, walkY + 0.46f, 0f), metalDark);

            // === Interior props (crates + barrels) ===
            int seed = 1000 + (int)(center.x * 7f) + (int)(center.z * 13f);
            var rng = new System.Random(seed);
            for (int i = 0; i < 8; i++)
            {
                float cx = (float)(rng.NextDouble() * (w - 6f)) - (w - 6f) * 0.5f;
                float cz = (float)(rng.NextDouble() * (d - 6f)) - (d - 6f) * 0.5f;
                Box(root, "Crate", new Vector3(1.6f, 1.1f, 1.0f),
                    new Vector3(cx, 0.85f, cz), crateMat);
            }
            for (int i = 0; i < 3; i++)
            {
                float cx = (float)(rng.NextDouble() * (w - 6f)) - (w - 6f) * 0.5f;
                float cz = (float)(rng.NextDouble() * (d - 6f)) - (d - 6f) * 0.5f;
                Box(root, "OilDrum", new Vector3(0.55f, 0.9f, 0.55f),
                    new Vector3(cx, 0.75f, cz), metalDark);
            }
        }

        // ==============================================================
        // WATCHTOWER (premium, with ladder and platform details)
        // ==============================================================

        private static void Watchtower(Transform parent)
        {
            var root = new GameObject("Watchtower").transform;
            root.SetParent(parent, false);
            root.position = new Vector3(40f, 0f, 38f);

            float towerH = 4.0f;
            float platW = 4.8f;

            // Four concrete pillars
            for (int i = 0; i < 4; i++)
            {
                float px = (i % 2 == 0 ? -2.0f : 2.0f);
                float pz = (i < 2 ? -2.0f : 2.0f);
                Box(root, "Pillar", new Vector3(0.35f, towerH, 0.35f),
                    new Vector3(px, towerH * 0.5f, pz), concrete);
                // Pillar base plate
                Box(root, "BasePlate", new Vector3(0.8f, 0.12f, 0.8f),
                    new Vector3(px, 0.06f, pz), concrete);
            }

            // Platform
            Box(root, "Platform", new Vector3(platW, 0.25f, platW),
                new Vector3(0f, towerH + 0.1f, 0f), concrete);
            // Platform support beams underneath
            Box(root, "SupportBeam", new Vector3(platW, 0.15f, 0.15f),
                new Vector3(0f, towerH - 0.1f, -platW * 0.45f), metalDark);
            Box(root, "SupportBeam", new Vector3(platW, 0.15f, 0.15f),
                new Vector3(0f, towerH - 0.1f, platW * 0.45f), metalDark);
            Box(root, "SupportBeam", new Vector3(0.15f, 0.15f, platW),
                new Vector3(-platW * 0.45f, towerH - 0.1f, 0f), metalDark);
            Box(root, "SupportBeam", new Vector3(0.15f, 0.15f, platW),
                new Vector3(platW * 0.45f, towerH - 0.1f, 0f), metalDark);

            // Railing on all four sides
            float railH = 1.0f;
            float railY = towerH + 0.1f + 0.125f + railH * 0.5f;
            Box(root, "RailFront", new Vector3(platW + 0.12f, railH, 0.06f),
                new Vector3(0f, railY, platW * 0.5f), metalDark);
            Box(root, "RailBack", new Vector3(platW + 0.12f, railH, 0.06f),
                new Vector3(0f, railY, -platW * 0.5f), metalDark);
            Box(root, "RailLeft", new Vector3(0.06f, railH, platW + 0.12f),
                new Vector3(-platW * 0.5f, railY, 0f), metalDark);
            Box(root, "RailRight", new Vector3(0.06f, railH, platW + 0.12f),
                new Vector3(platW * 0.5f, railY, 0f), metalDark);
            // Mid-rails (safety bars)
            Box(root, "MidRailFront", new Vector3(platW + 0.12f, 0.06f, 0.06f),
                new Vector3(0f, railY - railH * 0.35f, platW * 0.5f), metalDark);
            Box(root, "MidRailBack", new Vector3(platW + 0.12f, 0.06f, 0.06f),
                new Vector3(0f, railY - railH * 0.35f, -platW * 0.5f), metalDark);

            // Ramp from south side (walkable slope for NavMesh)
            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Ramp";
            ramp.layer = GameConstants.LayerWorld;
            ramp.isStatic = true;
            ramp.transform.SetParent(root, false);
            ramp.transform.localPosition = new Vector3(0f, towerH * 0.5f, 4.2f);
            ramp.transform.localRotation = Quaternion.Euler(42f, 0f, 0f);
            ramp.transform.localScale = new Vector3(1.8f, 0.18f, 5.6f);
            ramp.GetComponent<MeshRenderer>().sharedMaterial = concrete;
            // Ramp railings
            Box(root, "RampRail", new Vector3(0.06f, 0.8f, 5.6f),
                new Vector3(-0.9f, towerH * 0.5f + 0.45f, 4.2f), metalDark);
            Box(root, "RampRail", new Vector3(0.06f, 0.8f, 5.6f),
                new Vector3(0.9f, towerH * 0.5f + 0.45f, 4.2f), metalDark);

            // Ladder rungs (north side, decorative for visual)
            for (int r = 0; r < 12; r++)
            {
                float ry = r * (towerH / 11f);
                Box(root, "LadderRung", new Vector3(0.55f, 0.05f, 0.06f),
                    new Vector3(0f, ry + 0.2f, -platW * 0.5f - 0.2f), metalDark);
            }
            // Ladder rails
            Box(root, "LadderRail", new Vector3(0.06f, towerH, 0.06f),
                new Vector3(-0.28f, towerH * 0.5f, -platW * 0.5f - 0.2f), metalDark);
            Box(root, "LadderRail", new Vector3(0.06f, towerH, 0.06f),
                new Vector3(0.28f, towerH * 0.5f, -platW * 0.5f - 0.2f), metalDark);

            // Sandbag cover at platform edges
            TerrainProps.BuildSandbagBarrier(root, new Vector3(-1.2f, towerH + 0.25f, platW * 0.5f - 0.15f),
                Quaternion.Euler(0f, 0f, 0f), 1, 4);
            TerrainProps.BuildSandbagBarrier(root, new Vector3(1.2f, towerH + 0.25f, platW * 0.5f - 0.15f),
                Quaternion.Euler(0f, 0f, 0f), 1, 4);
        }

        // ==============================================================
        // COVER & BARRIERS (sandbags, H-barriers, center tactical cover)
        // ==============================================================

        private static void CenterCover(Transform parent)
        {
            var rng = new System.Random(2026);

            // Center lane cover — leave the kill lane open (x ≈ [-4,4])
            for (int i = 0; i < 12; i++)
            {
                float x = -20f + i * 3.2f;
                if (x > -4.8f && x < 4.8f) continue;

                // Mix of barrier types
                if (rng.NextDouble() > 0.5f)
                {
                    // Jersey barrier (concrete block)
                    Barrier(parent, new Vector3(x, 0f, -8f), Quaternion.identity,
                        new Vector3(2.2f, 1.1f, 0.9f));
                }
                else
                {
                    // H-barrier for variety
                    TerrainProps.BuildHBarrier(parent, new Vector3(x, 0f, -8f),
                        Quaternion.Euler(0f, rng.Next(0, 10) - 5f, 0f));
                }
            }

            for (int i = 0; i < 10; i++)
            {
                float x = -16f + i * 3.6f;
                if (x > -4.8f && x < 4.8f) continue;

                if (rng.NextDouble() > 0.5f)
                {
                    Barrier(parent, new Vector3(x, 0f, 12f), Quaternion.identity,
                        new Vector3(2.2f, 1.1f, 0.9f));
                }
                else
                {
                    TerrainProps.BuildHBarrier(parent, new Vector3(x, 0f, 12f),
                        Quaternion.Euler(0f, rng.Next(0, 10) - 5f, 0f));
                }
            }

            // Low concrete walls around container yard edge
            Wall(parent, new Vector3(27f, 0.65f, -40f), new Vector3(7f, 1.3f, 1.0f));
            Wall(parent, new Vector3(27f, 0.65f, 40f), new Vector3(7f, 1.3f, 1.0f));

            // Scattered solo barriers
            for (int i = 0; i < 8; i++)
            {
                float x = -60f + (float)rng.NextDouble() * 40f;
                float z = 18f + (float)rng.NextDouble() * 18f;
                Barrier(parent, new Vector3(x, 0f, z),
                    Quaternion.Euler(0f, rng.Next(0, 180), 0f),
                    new Vector3(2.2f, 1.0f, 0.9f));
            }
        }

        /// <summary>Strategic sandbag positions at key gameplay locations.</summary>
        private static void SandbagPositions(Transform parent)
        {
            // Sandbag wall north gate (defensive position)
            TerrainProps.BuildSandbagBarrier(parent, new Vector3(-12f, 0f, 42f),
                Quaternion.Euler(0f, 0f, 0f), 3, 8);
            TerrainProps.BuildSandbagBarrier(parent, new Vector3(12f, 0f, 42f),
                Quaternion.Euler(0f, 0f, 0f), 3, 8);

            // Sandbag corner at watchtower base
            TerrainProps.BuildSandbagBarrier(parent, new Vector3(37f, 0f, 36f),
                Quaternion.Euler(0f, 30f, 0f), 2, 6);
            TerrainProps.BuildSandbagBarrier(parent, new Vector3(43f, 0f, 36f),
                Quaternion.Euler(0f, -30f, 0f), 2, 6);

            // Sandbag line along south perimeter
            TerrainProps.BuildSandbagBarrier(parent, new Vector3(-20f, 0f, -42f),
                Quaternion.Euler(0f, 0f, 0f), 2, 10);
            TerrainProps.BuildSandbagBarrier(parent, new Vector3(20f, 0f, -42f),
                Quaternion.Euler(0f, 0f, 0f), 2, 10);

            // Sandbag defensive pit near player spawn
            TerrainProps.BuildSandbagBarrier(parent, new Vector3(-3f, 0f, 36f),
                Quaternion.Euler(0f, 0f, 0f), 2, 5);
            TerrainProps.BuildSandbagBarrier(parent, new Vector3(3f, 0f, 36f),
                Quaternion.Euler(0f, 180f, 0f), 2, 5);
        }

        /// <summary>Extended barrier lines at strategic points.</summary>
        private static void BarrierLines(Transform parent)
        {
            // Barrier line along east side of container yard
            for (int i = 0; i < 5; i++)
            {
                Barrier(parent, new Vector3(60f, 0f, -10f + i * 5f),
                    Quaternion.Euler(0f, 90f, 0f),
                    new Vector3(2.0f, 1.0f, 0.9f));
            }

            // Barrier line at warehouse approach
            TerrainProps.BuildHBarrier(parent, new Vector3(-25f, 0f, -15f),
                Quaternion.Euler(0f, 0f, 0f));
            TerrainProps.BuildHBarrier(parent, new Vector3(-25f, 0f, -12f),
                Quaternion.Euler(0f, 0f, 0f));
        }

        // ==============================================================
        // NATURAL DECORATION (rocks, barrels, crates)
        // ==============================================================

        private static void RockFormations(Transform parent)
        {
            // Rock clusters at terrain rim edges
            TerrainProps.BuildRockCluster(parent, new Vector3(-58f, 0f, -38f), 5, 3f);
            TerrainProps.BuildRockCluster(parent, new Vector3(58f, 0f, 38f), 4, 3f);
            TerrainProps.BuildRockCluster(parent, new Vector3(-55f, 0f, 35f), 6, 4f);
            TerrainProps.BuildRockCluster(parent, new Vector3(55f, 0f, -35f), 5, 3.5f);

            // Rock clusters along east wall backdrop
            TerrainProps.BuildRockCluster(parent, new Vector3(62f, 0f, -20f), 4, 2f);
            TerrainProps.BuildRockCluster(parent, new Vector3(64f, 0f, 10f), 3, 2f);

            // Scattered rocks in corners (natural detail)
            TerrainProps.BuildRockCluster(parent, new Vector3(-50f, 0f, -10f), 3, 2f);
            TerrainProps.BuildRockCluster(parent, new Vector3(45f, 0f, 20f), 3, 2f);
        }

        private static void BarrelClutter(Transform parent)
        {
            TerrainProps.BuildBarrelCluster(parent, new Vector3(-10f, 0f, -28f), 4);
            TerrainProps.BuildBarrelCluster(parent, new Vector3(30f, 0f, 28f), 3);
            TerrainProps.BuildBarrelCluster(parent, new Vector3(-48f, 0f, 20f), 3);
            TerrainProps.BuildBarrelCluster(parent, new Vector3(50f, 0f, -15f), 2);
        }

        private static void CrateDebris(Transform parent)
        {
            var rng = new System.Random(77);
            float[,] clusters =
            {
                { 8f, -18f }, { -10f, -20f }, { 14f, 20f }, { -24f, 14f },
                { 24f, 2f }, { -2f, 30f }, { 12f, -32f }, { -40f, 24f },
                { -52f, -22f }, { 42f, 16f }
            };
            for (int c = 0; c < clusters.GetLength(0); c++)
            {
                int n = 3 + rng.Next(0, 4);
                for (int i = 0; i < n; i++)
                {
                    float x = clusters[c, 0] + (float)(rng.NextDouble() * 3.0 - 1.5);
                    float z = clusters[c, 1] + (float)(rng.NextDouble() * 3.0 - 1.5);
                    float s = 0.8f + (float)rng.NextDouble() * 0.8f;
                    Box(parent, "Crate", new Vector3(s, s * 0.7f, s * 0.9f),
                        new Vector3(x, s * 0.35f, z), crateMat);
                }
            }
        }

        // ==============================================================
        // ENVIRONMENT DRESSING (cables, signs, debris)
        // ==============================================================

        /// <summary>Catenary cables between buildings and structures.</summary>
        private static void Cables(Transform parent)
        {
            var cableMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            cableMat.color = new Color(0.18f, 0.18f, 0.20f);
            cableMat.SetFloat("_Smoothness", 0.3f);
            cableMat.SetFloat("_Metallic", 0.6f);

            // Cable from warehouse roof to watchtower
            CatenaryCable(parent, cableMat,
                new Vector3(-19f, 5.6f, -8f),   // warehouse roof edge
                new Vector3(40f, 4.1f, 38f),     // watchtower platform
                8, 0.15f);

            // Cable from warehouse to east wall
            CatenaryCable(parent, cableMat,
                new Vector3(-19f, 5.6f, -8f),
                new Vector3(70f, 3.0f, -10f),
                12, 0.2f);

            // Cable along container yard (low, between containers)
            CatenaryCable(parent, cableMat,
                new Vector3(34f, 3.9f, -18f),
                new Vector3(58f, 3.9f, -18f),
                5, 0.1f);

            // Power line from north wall to south wall (high, across the map)
            CatenaryCable(parent, cableMat,
                new Vector3(-5f, 4.0f, 45f),
                new Vector3(-5f, 4.0f, -45f),
                16, 0.3f);
        }

        private static void CatenaryCable(Transform parent, Material mat, Vector3 a, Vector3 b, int segments, float sag)
        {
            var root = new GameObject("Cable").transform;
            root.SetParent(parent, false);
            root.position = Vector3.zero;

            Vector3 dir = b - a;
            float length = dir.magnitude;
            dir.Normalize();

            // Catenary approximation using parabola
            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments;
                float t1 = (float)(i + 1) / segments;

                Vector3 p0 = Vector3.Lerp(a, b, t0);
                Vector3 p1 = Vector3.Lerp(a, b, t1);

                // Parabolic sag: maximum at midpoint
                float sag0 = -sag * 4f * t0 * (1f - t0);
                float sag1 = -sag * 4f * t1 * (1f - t1);
                p0.y += sag0;
                p1.y += sag1;

                Vector3 segDir = p1 - p0;
                float segLen = segDir.magnitude;
                if (segLen < 0.01f) continue;

                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = "CableSegment";
                seg.layer = GameConstants.LayerWorld;
                seg.isStatic = true;
                seg.transform.SetParent(root, false);
                seg.transform.position = (p0 + p1) * 0.5f;
                seg.transform.rotation = Quaternion.LookRotation(segDir);
                seg.transform.localScale = new Vector3(0.02f, 0.02f, segLen);
                seg.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }

        /// <summary>Military signage: EXIT, DANGER ZONE, restricted area markers.</summary>
        private static void Signs(Transform parent)
        {
            var signMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            signMat.color = new Color(0.75f, 0.65f, 0.20f); // warning yellow
            signMat.SetFloat("_Smoothness", 0.3f);

            var signPostMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            signPostMat.color = new Color(0.35f, 0.35f, 0.38f);
            signPostMat.SetFloat("_Smoothness", 0.4f);
            signPostMat.SetFloat("_Metallic", 0.5f);

            // EXIT signs near gates
            MilitarySign(parent, signMat, signPostMat, new Vector3(-5.5f, 2.8f, 43.5f), Quaternion.Euler(0f, 0f, 0f));
            MilitarySign(parent, signMat, signPostMat, new Vector3(5.5f, 2.8f, 43.5f), Quaternion.Euler(0f, 0f, 0f));
            MilitarySign(parent, signMat, signPostMat, new Vector3(-5.5f, 2.8f, -43.5f), Quaternion.Euler(0f, 180f, 0f));

            // DANGER ZONE signs near container yard
            var dangerMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            dangerMat.color = new Color(0.80f, 0.20f, 0.15f); // danger red
            dangerMat.SetFloat("_Smoothness", 0.3f);

            MilitarySign(parent, dangerMat, signPostMat, new Vector3(30f, 2.5f, 0f), Quaternion.Euler(0f, 90f, 0f));
            MilitarySign(parent, dangerMat, signPostMat, new Vector3(-40f, 2.5f, 8f), Quaternion.Euler(0f, -90f, 0f));

            // Barricade tape markers (thin strips between posts)
            var tapeMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            tapeMat.color = new Color(0.85f, 0.75f, 0.15f);
            tapeMat.SetFloat("_Smoothness", 0.1f);

            // Tape near south perimeter
            Box(parent, "TapeStrip", new Vector3(0.03f, 0.08f, 12f),
                new Vector3(-10f, 1.0f, -42f), tapeMat);
            Box(parent, "TapeStrip", new Vector3(0.03f, 0.08f, 12f),
                new Vector3(10f, 1.0f, -42f), tapeMat);
        }

        private static void MilitarySign(Transform parent, Material signMat, Material postMat,
            Vector3 pos, Quaternion rot)
        {
            var root = new GameObject("Sign").transform;
            root.SetParent(parent, false);
            root.position = pos;
            root.rotation = rot;

            // Post
            Box(root, "Post", postMat, new Vector3(0.06f, 2.0f, 0.06f),
                new Vector3(0f, -1.0f, 0f));
            // Sign board
            Box(root, "Board", signMat, new Vector3(0.8f, 0.4f, 0.04f),
                new Vector3(0f, 0f, 0f));
            // Border
            Box(root, "Border", postMat, new Vector3(0.84f, 0.04f, 0.05f),
                new Vector3(0f, 0.2f, 0f));
            Box(root, "Border", postMat, new Vector3(0.84f, 0.04f, 0.05f),
                new Vector3(0f, -0.2f, 0f));
        }

        /// <summary>Rubble and debris piles near buildings and impact zones.</summary>
        private static void DebrisPiles(Transform parent)
        {
            var rng = new System.Random(555);
            var debrisMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            debrisMat.color = new Color(0.48f, 0.44f, 0.38f);
            debrisMat.SetFloat("_Smoothness", 0.05f);

            // Debris near warehouse entrances
            DebrisCluster(parent, debrisMat, rng, new Vector3(-20f, 0f, -2f), 6, 2f);
            DebrisCluster(parent, debrisMat, rng, new Vector3(-8f, 0f, 26f), 5, 1.5f);

            // Rubble near perimeter walls
            DebrisCluster(parent, debrisMat, rng, new Vector3(-60f, 0f, 20f), 4, 2f);
            DebrisCluster(parent, debrisMat, rng, new Vector3(55f, 0f, -30f), 4, 2f);

            // Impact craters (shallow depressions with rubble ring)
            for (int i = 0; i < 5; i++)
            {
                float x = -40f + (float)rng.NextDouble() * 80f;
                float z = -30f + (float)rng.NextDouble() * 60f;
                ImpactCrater(parent, debrisMat, new Vector3(x, 0f, z), 0.8f + (float)rng.NextDouble() * 0.6f);
            }
        }

        private static void DebrisCluster(Transform parent, Material mat, System.Random rng,
            Vector3 center, int count, float spread)
        {
            for (int i = 0; i < count; i++)
            {
                float x = center.x + (float)(rng.NextDouble() * 2.0 - 1.0) * spread;
                float z = center.z + (float)(rng.NextDouble() * 2.0 - 1.0) * spread;
                float s = 0.15f + (float)rng.NextDouble() * 0.35f;

                var debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
                debris.name = "Debris";
                debris.layer = GameConstants.LayerWorld;
                debris.isStatic = true;
                debris.transform.SetParent(parent, false);
                debris.transform.position = new Vector3(x, s * 0.3f, z);
                debris.transform.localScale = new Vector3(s, s * 0.5f, s * 0.8f);
                debris.transform.rotation = Quaternion.Euler(
                    (float)rng.NextDouble() * 20f,
                    (float)rng.NextDouble() * 360f,
                    (float)rng.NextDouble() * 15f);
                debris.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }

        private static void ImpactCrater(Transform parent, Material mat, Vector3 center, float radius)
        {
            // Shallow ring of rubble
            int segments = 8;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * (360f / segments);
                float rad = angle * Mathf.Deg2Rad;
                float x = center.x + Mathf.Cos(rad) * radius;
                float z = center.z + Mathf.Sin(rad) * radius;
                float s = 0.1f + Random.Range(0f, 0.15f);

                var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = "CraterRock";
                rock.layer = GameConstants.LayerWorld;
                rock.isStatic = true;
                rock.transform.SetParent(parent, false);
                rock.transform.position = new Vector3(x, s * 0.2f, z);
                rock.transform.localScale = new Vector3(s, s * 0.4f, s);
                rock.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }

        // ==============================================================
        // LIGHTING (warm desert sun + cool fill + rim)
        // ==============================================================

        private static void Lighting(Transform parent)
        {
            // Primary directional sun — warm desert light
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(parent, false);
            sunGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.92f, 0.80f);     // warm sunlight
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            sun.shadowBias = 0.02f;
            sun.shadowNormalBias = 0.4f;
            QualityApplier.RegisterSun(sun);

            // Cool fill light (opposite direction, for shadow readability)
            var fillGo = new GameObject("FillLight");
            fillGo.transform.SetParent(parent, false);
            fillGo.transform.rotation = Quaternion.Euler(22f, 148f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.48f, 0.58f, 0.72f);   // cool blue fill
            fill.intensity = 0.28f;
            fill.shadows = LightShadows.None;

            // Rim/edge light (backlight for enemy silhouettes)
            var rimGo = new GameObject("RimLight");
            rimGo.transform.SetParent(parent, false);
            rimGo.transform.rotation = Quaternion.Euler(15f, -160f, 0f);
            var rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = new Color(1.0f, 0.95f, 0.88f);     // warm rim
            rim.intensity = 0.18f;
            rim.shadows = LightShadows.None;
        }

        // ==============================================================
        // PRIMITIVES & HELPERS
        // ==============================================================

        private static void Wall(Transform parent, Vector3 pos, Vector3 size)
        {
            Box(parent, "Wall", size, pos, wallMat);
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

        // ==============================================================
        // MATERIALS (URP Lit shader — all desert military palette)
        // ==============================================================

        private static void CreateMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            // Desert ground
            sand = Mat(shader, new Color(0.72f, 0.66f, 0.52f), 0.08f);
            dirtMat = Mat(shader, new Color(0.52f, 0.44f, 0.32f), 0.05f);

            // Concrete structures
            concrete = Mat(shader, new Color(0.55f, 0.54f, 0.51f), 0.22f);
            concreteDark = Mat(shader, new Color(0.38f, 0.37f, 0.35f), 0.30f);
            wallMat = Mat(shader, new Color(0.60f, 0.58f, 0.54f), 0.18f);

            // Containers
            containerRed = Mat(shader, new Color(0.58f, 0.28f, 0.24f), 0.38f);
            containerSteel = Mat(shader, new Color(0.48f, 0.52f, 0.56f), 0.52f);

            // Metal details
            metalDark = Mat(shader, new Color(0.22f, 0.22f, 0.24f), 0.62f);
            roofMat = Mat(shader, new Color(0.30f, 0.29f, 0.28f), 0.40f);

            // Props
            crateMat = Mat(shader, new Color(0.44f, 0.35f, 0.24f), 0.12f);
            sandbagMat = Mat(shader, new Color(0.82f, 0.76f, 0.60f), 0.08f);
            rockMat = Mat(shader, new Color(0.48f, 0.44f, 0.38f), 0.20f);
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

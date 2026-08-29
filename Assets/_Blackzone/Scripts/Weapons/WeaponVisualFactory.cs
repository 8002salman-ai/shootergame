using System.Collections.Generic;
using UnityEngine;

namespace Blackzone.Weapons
{
    /// <summary>
    /// Builds detailed tactical weapon viewmodels from primitives. Each weapon
    /// class has a unique silhouette with attachment points, rails, stocks,
    /// and weapon-specific accents. All URP Lit materials — no pink shaders.
    ///
    /// KESTREL K-17 (AR): stock, rail system, foregrip, iron sights, 30-round mag
    /// VIPER V-9 (SMG): compact, vertical grip, red dot, extended mag
    /// ANVIL A-12 (Shotgun): pump action, tube mag, bead sight, wide stock
    /// LONGBOW LB-7 (DMR): scope, bipod, long barrel, detachable mag
    /// </summary>
    public static class WeaponVisualFactory
    {
        private static Material bodyMat;
        private static Material barrelMat;
        private static Material railMat;
        private static Material gripMat;
        private static Material scopeMat;
        private static Material stockMat;
        private static readonly Dictionary<Color, Material> AccentMats = new Dictionary<Color, Material>();
        private static readonly Dictionary<Color, Material> DarkMats = new Dictionary<Color, Material>();

        public static Transform Build(WeaponDefinition def, Transform parent)
        {
            var root = new GameObject(def.displayName + "_Visual").transform;
            root.SetParent(parent, false);

            InitMaterials();

            Material accent = GetAccent(def.accentColor);
            Material dark = GetDark(def.accentColor);

            switch (def.weaponClass)
            {
                case WeaponClass.AssaultRifle:
                    BuildAssaultRifle(root, def, accent, dark);
                    break;
                case WeaponClass.SMG:
                    BuildSmg(root, def, accent, dark);
                    break;
                case WeaponClass.Shotgun:
                    BuildShotgun(root, def, accent, dark);
                    break;
                case WeaponClass.MarksmanRifle:
                    BuildMarksman(root, def, accent, dark);
                    break;
                default:
                    BuildAssaultRifle(root, def, accent, dark);
                    break;
            }

            // Muzzle point (always at the tip of the barrel)
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(root, false);
            muzzle.localPosition = GetMuzzlePos(def.weaponClass);
            muzzle.localRotation = Quaternion.identity;

            return muzzle;
        }

        // ==============================================================
        // KESTREL K-17 (Assault Rifle)
        // ==============================================================

        private static void BuildAssaultRifle(Transform root, WeaponDefinition def, Material accent, Material dark)
        {
            root.localScale = Vector3.one;

            // Receiver (main body)
            Box(root, "Receiver", bodyMat, new Vector3(0.085f, 0.10f, 0.38f), new Vector3(0f, 0.015f, 0f));
            // Receiver top rail
            Box(root, "TopRail", railMat, new Vector3(0.04f, 0.02f, 0.44f), new Vector3(0f, 0.075f, -0.01f));
            // Receiver side panels (visual detail)
            Box(root, "SidePanel", dark, new Vector3(0.001f, 0.06f, 0.20f), new Vector3(0.045f, 0.01f, 0.02f));
            Box(root, "SidePanel", dark, new Vector3(0.001f, 0.06f, 0.20f), new Vector3(-0.045f, 0.01f, 0.02f));

            // Barrel
            Cylinder(root, "Barrel", barrelMat, 0.016f, 0.38f, new Vector3(0f, 0.042f, 0.32f));
            // Barrel shroud / handguard
            Box(root, "Handguard", bodyMat, new Vector3(0.072f, 0.072f, 0.22f), new Vector3(0f, 0.015f, 0.19f));
            // Bottom rail on handguard
            Box(root, "BottomRail", railMat, new Vector3(0.04f, 0.015f, 0.20f), new Vector3(0f, -0.022f, 0.19f));
            // Side rails
            Box(root, "SideRail", railMat, new Vector3(0.015f, 0.015f, 0.18f), new Vector3(0.042f, 0.015f, 0.19f));
            Box(root, "SideRail", railMat, new Vector3(0.015f, 0.015f, 0.18f), new Vector3(-0.042f, 0.015f, 0.19f));

            // Foregrip (vertical grip under handguard)
            Box(root, "Foregrip", accent, new Vector3(0.038f, 0.10f, 0.038f), new Vector3(0f, -0.045f, 0.16f));
            Box(root, "ForegripCap", dark, new Vector3(0.042f, 0.015f, 0.042f), new Vector3(0f, -0.10f, 0.16f));

            // Magazine (STANAG-style)
            Box(root, "MagBody", accent, new Vector3(0.042f, 0.16f, 0.055f), new Vector3(0f, -0.10f, 0.02f));
            Box(root, "MagBase", dark, new Vector3(0.046f, 0.018f, 0.058f), new Vector3(0f, -0.18f, 0.02f));
            // Mag well
            Box(root, "MagWell", bodyMat, new Vector3(0.06f, 0.025f, 0.065f), new Vector3(0f, -0.02f, 0.02f));

            // Pistol grip
            Box(root, "Grip", accent, new Vector3(0.042f, 0.11f, 0.042f), new Vector3(0f, -0.075f, -0.10f));
            Box(root, "GripTexture", dark, new Vector3(0.044f, 0.08f, 0.008f), new Vector3(0f, -0.075f, -0.08f));

            // Stock (6-position collapsible)
            Box(root, "StockTube", barrelMat, new Vector3(0.025f, 0.025f, 0.22f), new Vector3(0f, 0.03f, -0.28f));
            Box(root, "StockPad", stockMat, new Vector3(0.055f, 0.09f, 0.04f), new Vector3(0f, 0.015f, -0.40f));
            Box(root, "StockBrace", dark, new Vector3(0.04f, 0.06f, 0.02f), new Vector3(0f, -0.02f, -0.35f));

            // Iron sights (front post + rear aperture)
            Box(root, "FrontSight", railMat, new Vector3(0.012f, 0.045f, 0.012f), new Vector3(0f, 0.10f, 0.28f));
            Box(root, "RearSight", railMat, new Vector3(0.035f, 0.035f, 0.02f), new Vector3(0f, 0.095f, -0.08f));

            // Muzzle device (flash hider)
            Cylinder(root, "FlashHider", dark, 0.02f, 0.04f, new Vector3(0f, 0.042f, 0.53f));
        }

        // ==============================================================
        // VIPER V-9 (SMG)
        // ==============================================================

        private static void BuildSmg(Transform root, WeaponDefinition def, Material accent, Material dark)
        {
            root.localScale = Vector3.one * 0.92f;

            // Compact receiver
            Box(root, "Receiver", bodyMat, new Vector3(0.072f, 0.088f, 0.30f), new Vector3(0f, 0.012f, 0f));
            // Top rail
            Box(root, "TopRail", railMat, new Vector3(0.035f, 0.018f, 0.34f), new Vector3(0f, 0.065f, -0.01f));
            // Side detail
            Box(root, "SideDetail", dark, new Vector3(0.001f, 0.05f, 0.15f), new Vector3(0.038f, 0.01f, 0.01f));

            // Short barrel
            Cylinder(root, "Barrel", barrelMat, 0.014f, 0.22f, new Vector3(0f, 0.038f, 0.24f));
            // Barrel shroud (vented)
            Box(root, "Shroud", bodyMat, new Vector3(0.06f, 0.06f, 0.14f), new Vector3(0f, 0.012f, 0.14f));

            // Vertical foregrip
            Box(root, "VertGrip", accent, new Vector3(0.035f, 0.09f, 0.035f), new Vector3(0f, -0.038f, 0.12f));
            Box(root, "VertGripBase", dark, new Vector3(0.04f, 0.012f, 0.04f), new Vector3(0f, -0.085f, 0.12f));

            // Extended magazine (straight, stick-type)
            Box(root, "Magazine", accent, new Vector3(0.038f, 0.19f, 0.048f), new Vector3(0f, -0.11f, 0.01f));
            Box(root, "MagBase", dark, new Vector3(0.042f, 0.015f, 0.052f), new Vector3(0f, -0.205f, 0.01f));

            // Pistol grip (rubberized)
            Box(root, "Grip", accent, new Vector3(0.04f, 0.10f, 0.04f), new Vector3(0f, -0.065f, -0.08f));
            Box(root, "GripRubber", dark, new Vector3(0.042f, 0.07f, 0.008f), new Vector3(0f, -0.065f, -0.062f));

            // Folding stock (collapsed against receiver)
            Box(root, "StockArm", barrelMat, new Vector3(0.02f, 0.02f, 0.18f), new Vector3(0.04f, 0.04f, -0.18f));
            Box(root, "StockPad", stockMat, new Vector3(0.045f, 0.07f, 0.025f), new Vector3(0.04f, 0.02f, -0.28f));

            // Red dot sight
            Box(root, "RedDotBase", railMat, new Vector3(0.03f, 0.015f, 0.06f), new Vector3(0f, 0.08f, -0.04f));
            Box(root, "RedDotBody", dark, new Vector3(0.028f, 0.035f, 0.05f), new Vector3(0f, 0.10f, -0.04f));
            Box(root, "RedDotLens", accent, new Vector3(0.026f, 0.025f, 0.005f), new Vector3(0f, 0.10f, -0.015f));

            // Muzzle brake
            Cylinder(root, "MuzzleBrake", dark, 0.018f, 0.03f, new Vector3(0f, 0.038f, 0.37f));
        }

        // ==============================================================
        // ANVIL A-12 (Shotgun)
        // ==============================================================

        private static void BuildShotgun(Transform root, WeaponDefinition def, Material accent, Material dark)
        {
            root.localScale = Vector3.one;

            // Receiver (wider, more robust)
            Box(root, "Receiver", bodyMat, new Vector3(0.09f, 0.11f, 0.36f), new Vector3(0f, 0.018f, 0f));
            Box(root, "ReceiverTop", dark, new Vector3(0.092f, 0.02f, 0.362f), new Vector3(0f, 0.078f, 0f));

            // Main barrel (large bore)
            Cylinder(root, "Barrel", barrelMat, 0.022f, 0.48f, new Vector3(0f, 0.048f, 0.36f));
            // Magazine tube (under barrel)
            Cylinder(root, "MagTube", barrelMat, 0.016f, 0.40f, new Vector3(0f, 0.018f, 0.30f));
            // Barrel clamp
            Box(root, "BarrelClamp", dark, new Vector3(0.06f, 0.025f, 0.025f), new Vector3(0f, 0.035f, 0.42f));

            // Pump action forend (sliding)
            Box(root, "PumpForend", accent, new Vector3(0.068f, 0.065f, 0.16f), new Vector3(0f, 0.015f, 0.18f));
            // Pump grooves (visual texture)
            for (int i = 0; i < 5; i++)
            {
                float z = 0.12f + i * 0.03f;
                Box(root, "PumpGroove", dark, new Vector3(0.07f, 0.008f, 0.012f), new Vector3(0f, 0.048f, z));
            }

            // Wide pistol grip
            Box(root, "Grip", accent, new Vector3(0.048f, 0.12f, 0.048f), new Vector3(0f, -0.078f, -0.10f));
            Box(root, "GripTexture", dark, new Vector3(0.05f, 0.09f, 0.008f), new Vector3(0f, -0.078f, -0.078f));

            // Shell holder (side saddle)
            for (int i = 0; i < 4; i++)
            {
                float y = 0.02f + i * 0.028f;
                Cylinder(root, "Shell", accent, 0.008f, 0.024f, new Vector3(0.052f, y, -0.04f));
            }

            // Stock (fixed, wide)
            Box(root, "StockBody", bodyMat, new Vector3(0.05f, 0.08f, 0.28f), new Vector3(0f, 0.01f, -0.30f));
            Box(root, "StockPad", stockMat, new Vector3(0.058f, 0.10f, 0.03f), new Vector3(0f, 0.005f, -0.45f));
            Box(root, "StockComb", dark, new Vector3(0.04f, 0.03f, 0.26f), new Vector3(0f, 0.055f, -0.30f));

            // Bead sight
            Cylinder(root, "BeadSight", railMat, 0.005f, 0.008f, new Vector3(0f, 0.075f, 0.50f));

            // Muzzle (wide bore)
            Cylinder(root, "Muzzle", dark, 0.025f, 0.035f, new Vector3(0f, 0.048f, 0.61f));
        }

        // ==============================================================
        // LONGBOW LB-7 (Marksman Rifle)
        // ==============================================================

        private static void BuildMarksman(Transform root, WeaponDefinition def, Material accent, Material dark)
        {
            root.localScale = Vector3.one;

            // Long receiver
            Box(root, "Receiver", bodyMat, new Vector3(0.082f, 0.10f, 0.44f), new Vector3(0f, 0.015f, 0f));
            // Receiver reinforcement
            Box(root, "Reinforce", dark, new Vector3(0.084f, 0.025f, 0.30f), new Vector3(0f, 0.078f, 0.02f));
            // Bolt handle
            Box(root, "BoltHandle", barrelMat, new Vector3(0.015f, 0.015f, 0.06f), new Vector3(0.055f, 0.05f, -0.02f));

            // Long precision barrel
            Cylinder(root, "Barrel", barrelMat, 0.015f, 0.55f, new Vector3(0f, 0.042f, 0.40f));
            // Barrel fluting (weight reduction grooves)
            for (int i = 0; i < 4; i++)
            {
                float z = 0.25f + i * 0.10f;
                Box(root, "Flute", dark, new Vector3(0.008f, 0.02f, 0.06f), new Vector3(0.018f, 0.042f, z));
            }
            // Muzzle brake (large, multi-port)
            Cylinder(root, "MuzzleBrake", dark, 0.02f, 0.06f, new Vector3(0f, 0.042f, 0.70f));
            for (int i = 0; i < 3; i++)
            {
                float z = 0.68f + i * 0.018f;
                Box(root, "BrakePort", barrelMat, new Vector3(0.005f, 0.035f, 0.008f), new Vector3(0.022f, 0.042f, z));
            }

            // Scope (large, with sunshade)
            Box(root, "ScopeBody", scopeMat, new Vector3(0.042f, 0.042f, 0.26f), new Vector3(0f, 0.10f, -0.04f));
            Cylinder(root, "ScopeFront", scopeMat, 0.024f, 0.08f, new Vector3(0f, 0.10f, 0.12f));
            Cylinder(root, "ScopeRear", scopeMat, 0.022f, 0.05f, new Vector3(0f, 0.10f, -0.18f));
            // Scope turrets
            Cylinder(root, "TurretTop", dark, 0.012f, 0.025f, new Vector3(0f, 0.13f, -0.02f));
            Cylinder(root, "TurretSide", dark, 0.012f, 0.025f, new Vector3(0.035f, 0.10f, -0.02f));
            // Scope mount
            Box(root, "ScopeMount", railMat, new Vector3(0.038f, 0.015f, 0.08f), new Vector3(0f, 0.078f, -0.04f));
            // Top rail
            Box(root, "TopRail", railMat, new Vector3(0.038f, 0.015f, 0.40f), new Vector3(0f, 0.072f, 0f));

            // Bipod (folded forward)
            Box(root, "BipodMount", dark, new Vector3(0.035f, 0.02f, 0.03f), new Vector3(0f, -0.01f, 0.20f));
            Box(root, "BipodLeg", barrelMat, new Vector3(0.012f, 0.15f, 0.012f), new Vector3(0.02f, -0.06f, 0.22f));
            Box(root, "BipodLeg", barrelMat, new Vector3(0.012f, 0.15f, 0.012f), new Vector3(-0.02f, -0.06f, 0.22f));
            Box(root, "BipodFoot", dark, new Vector3(0.02f, 0.01f, 0.025f), new Vector3(0.02f, -0.135f, 0.22f));
            Box(root, "BipodFoot", dark, new Vector3(0.02f, 0.01f, 0.025f), new Vector3(-0.02f, -0.135f, 0.22f));

            // Detachable box magazine
            Box(root, "Magazine", accent, new Vector3(0.04f, 0.13f, 0.05f), new Vector3(0f, -0.088f, 0.04f));
            Box(root, "MagBase", dark, new Vector3(0.044f, 0.015f, 0.054f), new Vector3(0f, -0.155f, 0.04f));

            // Pistol grip (ergonomic)
            Box(root, "Grip", accent, new Vector3(0.04f, 0.11f, 0.04f), new Vector3(0f, -0.072f, -0.12f));
            Box(root, "GripTexture", dark, new Vector3(0.042f, 0.08f, 0.008f), new Vector3(0f, -0.072f, -0.102f));

            // Precision stock (adjustable cheek rest)
            Box(root, "StockBody", bodyMat, new Vector3(0.048f, 0.075f, 0.30f), new Vector3(0f, 0.012f, -0.30f));
            Box(root, "CheekRest", accent, new Vector3(0.04f, 0.03f, 0.12f), new Vector3(0f, 0.055f, -0.26f));
            Box(root, "ButtPad", stockMat, new Vector3(0.052f, 0.09f, 0.025f), new Vector3(0f, 0.01f, -0.46f));
            // Adjustable length-of-pull spacers
            Box(root, "LOPSpace", dark, new Vector3(0.044f, 0.07f, 0.015f), new Vector3(0f, 0.012f, -0.42f));
        }

        // ==============================================================
        // HELPERS
        // ==============================================================

        private static Vector3 GetMuzzlePos(WeaponClass cls)
        {
            switch (cls)
            {
                case WeaponClass.SMG: return new Vector3(0f, 0.038f, 0.40f);
                case WeaponClass.Shotgun: return new Vector3(0f, 0.048f, 0.65f);
                case WeaponClass.MarksmanRifle: return new Vector3(0f, 0.042f, 0.74f);
                default: return new Vector3(0f, 0.042f, 0.56f); // AR
            }
        }

        private static void InitMaterials()
        {
            if (bodyMat != null) return;

            var litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            bodyMat = new Material(litShader);
            bodyMat.color = new Color(0.14f, 0.14f, 0.155f); // matte black
            bodyMat.SetFloat("_Smoothness", 0.55f);
            bodyMat.SetFloat("_Metallic", 0.3f);

            barrelMat = new Material(litShader);
            barrelMat.color = new Color(0.18f, 0.18f, 0.20f); // dark steel
            barrelMat.SetFloat("_Smoothness", 0.65f);
            barrelMat.SetFloat("_Metallic", 0.7f);

            railMat = new Material(litShader);
            railMat.color = new Color(0.22f, 0.22f, 0.24f); // Picatinny rail
            railMat.SetFloat("_Smoothness", 0.40f);
            railMat.SetFloat("_Metallic", 0.5f);

            gripMat = new Material(litShader);
            gripMat.color = new Color(0.12f, 0.12f, 0.13f); // rubber grip
            gripMat.SetFloat("_Smoothness", 0.15f);

            scopeMat = new Material(litShader);
            scopeMat.color = new Color(0.16f, 0.16f, 0.18f); // scope body
            scopeMat.SetFloat("_Smoothness", 0.70f);
            scopeMat.SetFloat("_Metallic", 0.4f);

            stockMat = new Material(litShader);
            stockMat.color = new Color(0.20f, 0.19f, 0.18f); // rubber buttpad
            stockMat.SetFloat("_Smoothness", 0.10f);
        }

        private static Material GetAccent(Color color)
        {
            if (AccentMats.TryGetValue(color, out var m)) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            m = new Material(shader);
            m.color = color;
            m.SetFloat("_Smoothness", 0.4f);
            m.SetFloat("_Metallic", 0.15f);
            AccentMats[color] = m;
            return m;
        }

        private static Material GetDark(Color baseColor)
        {
            var dark = baseColor * 0.35f;
            dark.a = 1f;
            if (DarkMats.TryGetValue(dark, out var m)) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            m = new Material(shader);
            m.color = dark;
            m.SetFloat("_Smoothness", 0.25f);
            DarkMats[dark] = m;
            return m;
        }

        private static void Box(Transform parent, string name, Material mat, Vector3 size, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
        }

        private static void Cylinder(Transform parent, string name, Material mat, float radius, float length, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
        }
    }
}

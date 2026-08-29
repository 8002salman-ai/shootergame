using System.Collections.Generic;
using UnityEngine;

namespace Blackzone.Weapons
{
    /// <summary>
    /// Builds a placeholder gun viewmodel from primitives, one visual per
    /// weapon definition. Everything is replaceable later with real meshes
    /// without touching gameplay code.
    /// </summary>
    public static class WeaponVisualFactory
    {
        private static readonly Dictionary<Color, Material> AccentMaterials = new Dictionary<Color, Material>();
        private static Material bodyMat;

        public static Transform Build(WeaponDefinition def, Transform parent)
        {
            var root = new GameObject(def.displayName + "_Visual").transform;
            root.SetParent(parent, false);

            if (bodyMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                bodyMat = new Material(shader);
                bodyMat.color = new Color(0.13f, 0.13f, 0.145f);
                bodyMat.SetFloat("_Smoothness", 0.55f);
            }

            Material accent = GetAccent(def.accentColor);

            float scale = 1f;
            float barrelLen = 0.30f;
            if (def.weaponClass == WeaponClass.SMG) { scale = 0.92f; barrelLen = 0.20f; }
            else if (def.weaponClass == WeaponClass.Shotgun) { barrelLen = 0.42f; }
            else if (def.weaponClass == WeaponClass.MarksmanRifle) { barrelLen = 0.48f; }

            root.localScale = Vector3.one * scale;

            Box(root, "Receiver", bodyMat, new Vector3(0.09f, 0.11f, 0.42f), new Vector3(0f, 0.012f, 0f));
            Cylinder(root, "Barrel", bodyMat, 0.017f, barrelLen, new Vector3(0f, 0.045f, 0.30f + barrelLen * 0.5f));
            Box(root, "Handguard", bodyMat, new Vector3(0.075f, 0.09f, 0.16f), new Vector3(0f, 0.01f, 0.16f));
            Box(root, "Grip", accent, new Vector3(0.05f, 0.13f, 0.05f), new Vector3(0f, -0.085f, -0.10f));
            Box(root, "Magazine", accent, new Vector3(0.05f, 0.17f, 0.07f), new Vector3(0f, -0.115f, 0.02f));
            Box(root, "Sight", bodyMat, new Vector3(0.035f, 0.05f, 0.13f), new Vector3(0f, 0.09f, -0.02f));

            if (def.weaponClass == WeaponClass.MarksmanRifle)
                Box(root, "Scope", bodyMat, new Vector3(0.05f, 0.06f, 0.22f), new Vector3(0f, 0.10f, -0.05f));

            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(root, false);
            muzzle.localPosition = new Vector3(0f, 0.05f, 0.30f + barrelLen + 0.02f);
            muzzle.localRotation = Quaternion.identity;

            return muzzle;
        }

        private static Material GetAccent(Color color)
        {
            Material mat;
            if (!AccentMaterials.TryGetValue(color, out mat))
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                mat.color = color;
                mat.SetFloat("_Smoothness", 0.4f);
                AccentMaterials[color] = mat;
            }
            return mat;
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

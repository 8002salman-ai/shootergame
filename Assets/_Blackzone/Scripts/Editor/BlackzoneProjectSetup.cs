using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Blackzone.EditorTools
{
    /// <summary>
    /// One-time project setup menus. The runtime game builds itself from code,
    /// so these items only configure editor-level data:
    ///  01 - URP render pipeline asset + renderer + quality levels (Low/Medium/High)
    ///  02 - Android Player Settings (ARM64, landscape, IL2CPP, package id)
    ///  03 - ScriptableObject data assets from the code catalogs
    /// </summary>
    public static class BlackzoneProjectSetup
    {
        private const string URP_FOLDER = "Assets/_Blackzone/Settings/URP";
        private const string UrpAssetPath = URP_FOLDER + "/BlackzoneURP.asset";
        private const string RendererAssetPath = URP_FOLDER + "/BlackzoneForwardRenderer.asset";
        private const string WeaponsResPath = "Assets/_Blackzone/Resources/Weapons";
        private const string AiResPath = "Assets/_Blackzone/Resources/AI";

        // ---------------------------------------------------------------
        // 01 — URP Pipeline + Renderer + Quality Levels
        // ---------------------------------------------------------------

        [MenuItem("Blackzone/01 - Create URP Asset + Quality Levels")]
        public static void CreateUrpAndQuality()
        {
            EnsureFolder(URP_FOLDER);

            // =============================================================
            // PHASE 1: Ensure renderer asset exists with valid data
            // =============================================================
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (rendererData == null)
            {
                Debug.Log("Blackzone: Creating new Forward Renderer Data...");
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Blackzone: Renderer saved at " + RendererAssetPath);
            }

            // =============================================================
            // PHASE 2: Ensure pipeline asset exists
            // =============================================================
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (urp == null)
            {
                Debug.Log("Blackzone: Creating new URP Pipeline Asset...");
                urp = UniversalRenderPipelineAsset.Create();
                AssetDatabase.CreateAsset(urp, UrpAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Blackzone: Pipeline saved at " + UrpAssetPath);
            }

            // =============================================================
            // PHASE 3: CRITICAL — Link renderer to pipeline via SerializedObject
            // This is the ONLY reliable way to set m_RendererDataList in code.
            // SetRenderer() may not persist to the serialized .asset file.
            // =============================================================
            LinkRendererToPipeline(urp, rendererData);

            // =============================================================
            // PHASE 4: Configure URP settings
            // =============================================================
            ConfigureUrpSettings(urp);

            // =============================================================
            // PHASE 5: Save everything
            // =============================================================
            EditorUtility.SetDirty(urp);
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // =============================================================
            // PHASE 6: Assign to GraphicsSettings
            // =============================================================
            GraphicsSettings.defaultRenderPipeline = urp;
            GraphicsSettings.renderPipelineAsset = urp;

            // =============================================================
            // PHASE 7: Configure Quality Levels
            // =============================================================
            ConfigureQualityLevels(urp);

            QualitySettings.vSyncCount = 0;
            AssetDatabase.SaveAssets();

            // =============================================================
            // PHASE 8: VERIFY — Read back from disk to confirm
            // =============================================================
            bool verified = VerifyRendererPersisted(urp);
            if (verified)
            {
                Debug.Log("Blackzone: ✓✓✓ URP SETUP VERIFIED — renderer is linked and persisted.");
            }
            else
            {
                Debug.LogError("Blackzone: ✗✗✗ RENDERER NOT PERSISTED! " +
                    "The .asset file still has null renderer. " +
                    "Try: Delete " + UrpAssetPath + " and " + RendererAssetPath +
                    ", then run menu Blackzone > 01 again.");
            }

            // Log full diagnostic
            LogDiagnostic(urp, rendererData);
        }

        /// <summary>
        /// Links the renderer data to the pipeline asset using SerializedObject.
        /// This writes directly to the serialized fields, which is the only
        /// reliable way to set m_RendererDataList from code in Unity 6 URP 17.
        /// </summary>
        private static void LinkRendererToPipeline(
            UniversalRenderPipelineAsset urp,
            UniversalRendererData rendererData)
        {
            var so = new SerializedObject(urp);

            // === Set m_RendererDataList[0] = rendererData ===
            var rendererListProp = so.FindProperty("m_RendererDataList");
            if (rendererListProp == null)
            {
                // Try alternate property name
                rendererListProp = so.FindProperty("m_RendererData");
            }

            if (rendererListProp != null)
            {
                if (rendererListProp.isArray)
                {
                    // Ensure array has at least one element
                    if (rendererListProp.arraySize < 1)
                        rendererListProp.arraySize = 1;

                    // Set element 0 to our renderer
                    var element0 = rendererListProp.GetArrayElementAtIndex(0);
                    element0.objectReferenceValue = rendererData;

                    Debug.Log("Blackzone: Set m_RendererDataList[0] = " + rendererData.name +
                        " (fileID=" + rendererData.GetInstanceID() + ")");
                }
                else
                {
                    // Non-array property — set directly
                    rendererListProp.objectReferenceValue = rendererData;
                    Debug.Log("Blackzone: Set m_RendererData = " + rendererData.name);
                }
            }
            else
            {
                Debug.LogWarning("Blackzone: Could not find m_RendererDataList or m_RendererData. " +
                    "Listing all serialized properties for diagnosis:");
                ListSerializedProperties(so);
            }

            // === Set m_DefaultRendererIndex = 0 ===
            var defaultIndexProp = so.FindProperty("m_DefaultRendererIndex");
            if (defaultIndexProp != null)
            {
                defaultIndexProp.intValue = 0;
                Debug.Log("Blackzone: Set m_DefaultRendererIndex = 0");
            }
            else
            {
                Debug.LogWarning("Blackzone: Could not find m_DefaultRendererIndex property.");
            }

            // Apply changes — this writes to the in-memory serialized object
            so.ApplyModifiedProperties();

            // Mark dirty so Unity knows to save it
            EditorUtility.SetDirty(urp);
        }

        /// <summary>
        /// Verifies that the renderer reference actually persisted to the asset.
        /// Re-reads the asset from the SerializedObject after save.
        /// </summary>
        private static bool VerifyRendererPersisted(UniversalRenderPipelineAsset urp)
        {
            // Force refresh from disk
            AssetDatabase.Refresh();

            // Re-load the asset from disk
            var reloaded = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (reloaded == null)
            {
                Debug.LogError("Blackzone: Could not reload URP asset from disk!");
                return false;
            }

            // Read back the renderer list via SerializedObject
            var so = new SerializedObject(reloaded);
            var rendererListProp = so.FindProperty("m_RendererDataList");
            if (rendererListProp == null)
                rendererListProp = so.FindProperty("m_RendererData");

            if (rendererListProp == null)
            {
                Debug.LogError("Blackzone: m_RendererDataList not found on reloaded asset.");
                return false;
            }

            if (rendererListProp.isArray)
            {
                if (rendererListProp.arraySize == 0)
                {
                    Debug.LogError("Blackzone: m_RendererDataList is empty after reload!");
                    return false;
                }

                var element0 = rendererListProp.GetArrayElementAtIndex(0);
                if (element0.objectReferenceValue == null)
                {
                    Debug.LogError("Blackzone: m_RendererDataList[0] is null after reload! " +
                        "The renderer reference was NOT persisted.");
                    return false;
                }

                Debug.Log("Blackzone: Verified m_RendererDataList[0] = " +
                    element0.objectReferenceValue.name + " (type=" +
                    element0.objectReferenceValue.GetType().Name + ")");
                return true;
            }
            else
            {
                // Non-array: check if it has a value
                return rendererListProp.objectReferenceValue != null;
            }
        }

        /// <summary>Lists all SerializedProperty names on the asset for debugging.</summary>
        private static void ListSerializedProperties(SerializedObject so)
        {
            var prop = so.GetIterator();
            bool first = true;
            while (prop.NextVisible(first))
            {
                Debug.Log("  Property: " + prop.name + " (type=" + prop.propertyType + ")");
                first = false;
            }
        }

        /// <summary>Configure URP pipeline settings for BLACKZONE.</summary>
        private static void ConfigureUrpSettings(UniversalRenderPipelineAsset urp)
        {
            var so = new SerializedObject(urp);

            // HDR: enable for post-processing color accuracy
            SetProp(so, "m_SupportsHDR", true);

            // MSAA: off by default (quality presets override)
            SetProp(so, "m_MSAA", 1);
            SetProp(so, "m_MSAA SampleCount", 1);

            // Shadow distance: covers the 140×90m map
            SetProp(so, "m_ShadowDistance", 45f);

            // Shadow cascades: 2 for mobile perf
            SetProp(so, "m_ShadowCascadeCount", 2);

            // Additional lights: per-pixel, up to 4
            SetProp(so, "m_AdditionalLightsRenderingMode", 1);
            SetProp(so, "m_AdditionalLightsPerObjectLimit", 4);

            // Reflection probes: blended
            SetProp(so, "m_ReflectionProbeBlending", 1);

            // Render scale: 1.0 (quality presets override)
            SetProp(so, "m_RenderScale", 1.0f);

            // Main light shadows
            SetProp(so, "m_MainLightShadowsSupported", true);
            SetProp(so, "m_MainLightShadowmapResolution", 2048);

            // Additional shadows: off for mobile
            SetProp(so, "m_AdditionalLightShadowsSupported", false);

            so.ApplyModifiedProperties();
        }

        /// <summary>Configure quality levels pointing at the URP asset.</summary>
        private static void ConfigureQualityLevels(UniversalRenderPipelineAsset urp)
        {
            // Unity 6: configure existing quality levels
            int count = QualitySettings.names.Length;
            if (count < 3)
                Debug.LogWarning("Blackzone: " + count + " quality levels found; " +
                    "add 3+ in Project Settings > Quality for LOW/MED/HIGH.");

            for (int i = 0; i < count; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
            }

            Debug.Log("Blackzone: Assigned URP asset to " + count + " quality levels.");
        }

        /// <summary>Logs diagnostic info about the URP setup.</summary>
        private static void LogDiagnostic(UniversalRenderPipelineAsset urp, UniversalRendererData rendererData)
        {
            Debug.Log("=== BLACKZONE URP DIAGNOSTIC ===");
            Debug.Log("Pipeline asset: " + UrpAssetPath + " (exists=" + File.Exists(UrpAssetPath) + ")");
            Debug.Log("Renderer asset: " + RendererAssetPath + " (exists=" + File.Exists(RendererAssetPath) + ")");
            Debug.Log("GraphicsSettings.defaultRenderPipeline: " +
                (GraphicsSettings.defaultRenderPipeline != null ? GraphicsSettings.defaultRenderPipeline.name : "NULL"));
            Debug.Log("QualitySettings.renderPipeline: " +
                (QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline.name : "NULL"));
            Debug.Log("Quality levels: " + QualitySettings.names.Length);

            // Try GetRenderer
            try
            {
                var r = urp.GetRenderer(0);
                Debug.Log("urp.GetRenderer(0): " + (r != null ? r.name : "NULL"));
            }
            catch (System.Exception ex)
            {
                Debug.Log("urp.GetRenderer(0) threw: " + ex.Message);
            }

            Debug.Log("=== END DIAGNOSTIC ===");
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static void SetProp(SerializedObject so, string name, bool value)
        {
            var p = so.FindProperty(name);
            if (p != null && p.propertyType == SerializedPropertyType.Boolean)
                p.boolValue = value;
        }

        private static void SetProp(SerializedObject so, string name, int value)
        {
            var p = so.FindProperty(name);
            if (p != null && p.propertyType == SerializedPropertyType.Integer)
                p.intValue = value;
        }

        private static void SetProp(SerializedObject so, string name, float value)
        {
            var p = so.FindProperty(name);
            if (p != null && p.propertyType == SerializedPropertyType.Float)
                p.floatValue = value;
        }

        // ---------------------------------------------------------------
        // 02 — Android Player Settings
        // ---------------------------------------------------------------

        [MenuItem("Blackzone/02 - Configure Android Player Settings")]
        public static void ConfigureAndroid()
        {
            PlayerSettings.companyName = "Blackzone Studios";
            PlayerSettings.productName = "BLACKZONE";
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android, "com.blackzone.tactical");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.Android.androidTVCompatibility = false;
            PlayerSettings.SplashScreen.show = false;

            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            AssetDatabase.SaveAssets();
            Debug.Log("Blackzone: Android settings configured (ARM64/IL2CPP/landscape).");
        }

        // ---------------------------------------------------------------
        // 03 — Weapon + AI Data Assets
        // ---------------------------------------------------------------

        [MenuItem("Blackzone/03 - Create Weapon + AI Data Assets")]
        public static void CreateDataAssets()
        {
            EnsureFolder("Assets/_Blackzone/Resources");
            EnsureFolder(WeaponsResPath);
            EnsureFolder(AiResPath);

            var weapons = WeaponCatalog.GetWeaponDefinitions();
            int weaponCount = 0;
            foreach (var def in weapons)
            {
                string path = WeaponsResPath + "/" + def.weaponId + ".asset";
                if (File.Exists(path)) continue;
                var asset = ScriptableObject.CreateInstance<WeaponDefinition>();
                EditorUtility.CopySerialized(def, asset);
                AssetDatabase.CreateAsset(asset, path);
                weaponCount++;
            }

            var diffs = AIDifficultyCatalog.GetDifficulties();
            int aiCount = 0;
            foreach (var d in diffs)
            {
                string path = AiResPath + "/" + d.difficultyId + ".asset";
                if (File.Exists(path)) continue;
                var asset = ScriptableObject.CreateInstance<AIDifficultyDefinition>();
                EditorUtility.CopySerialized(d, asset);
                AssetDatabase.CreateAsset(asset, path);
                aiCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Blackzone: created {weaponCount} weapon + {aiCount} AI assets.");
        }

        // ---------------------------------------------------------------
        // Utility
        // ---------------------------------------------------------------

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}

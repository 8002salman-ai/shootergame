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

            Debug.Log("========================================");
            Debug.Log("BLACKZONE URP SETUP — Starting...");
            Debug.Log("========================================");

            // ===========================================================
            // PHASE 1: FORCE-DELETE any existing broken assets.
            // The previous approach created a pipeline via Create() which
            // embeds an unsaved internal renderer that becomes fileID:0.
            // The only reliable fix is to delete and recreate from scratch.
            // ===========================================================
            ForceDeleteAsset(UrpAssetPath);
            ForceDeleteAsset(RendererAssetPath);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            // ===========================================================
            // PHASE 2: Create the FORWARD RENDERER DATA first.
            // This MUST be a saved asset on disk so the pipeline can
            // reference it with a valid fileID/GUID.
            // ===========================================================
            Debug.Log("Blackzone: Creating Forward Renderer Data...");
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();

            // Configure renderer defaults for mobile tactical FPS
            rendererData.renderingPath = RenderingPath.Forward;
            rendererData.sortingCriteria = SortingCriteria.CommonOpaque;

            AssetDatabase.CreateAsset(rendererData, RendererAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Verify renderer asset exists on disk
            var verifyRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (verifyRenderer == null)
            {
                Debug.LogError("BLACKZONE: FAILED to create renderer asset at " + RendererAssetPath);
                return;
            }
            Debug.Log("Blackzone: ✓ Renderer asset created: " + RendererAssetPath +
                " (instanceID=" + verifyRenderer.GetInstanceID() +
                ", name=" + verifyRenderer.name + ")");

            // ===========================================================
            // PHASE 3: Create the URP PIPELINE ASSET.
            // IMPORTANT: We use CreateInstance (NOT UniversalRenderPipelineAsset.Create())
            // because Create() embeds an internal unsaved renderer that causes
            // the fileID:0 bug. We configure everything manually via SerializedObject.
            // ===========================================================
            Debug.Log("Blackzone: Creating URP Pipeline Asset via CreateInstance...");
            var urp = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            AssetDatabase.CreateAsset(urp, UrpAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Verify pipeline asset exists on disk
            var verifyPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (verifyPipeline == null)
            {
                Debug.LogError("BLACKZONE: FAILED to create pipeline asset at " + UrpAssetPath);
                return;
            }
            Debug.Log("Blackzone: ✓ Pipeline asset created: " + UrpAssetPath);

            // ===========================================================
            // PHASE 4: LINK RENDERER TO PIPELINE — multiple approaches
            // to guarantee persistence across URP API versions.
            // ===========================================================
            LinkRendererMultiApproach(verifyPipeline, verifyRenderer);

            // ===========================================================
            // PHASE 5: Configure all URP pipeline settings
            // ===========================================================
            ConfigureUrpSettings(verifyPipeline);

            // ===========================================================
            // PHASE 6: Save, refresh, and save again
            // ===========================================================
            EditorUtility.SetDirty(verifyPipeline);
            EditorUtility.SetDirty(verifyRenderer);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ===========================================================
            // PHASE 7: Assign to GraphicsSettings
            // ===========================================================
            GraphicsSettings.defaultRenderPipeline = verifyPipeline;
            GraphicsSettings.renderPipelineAsset = verifyPipeline;

            // ===========================================================
            // PHASE 8: Configure Quality Levels
            // ===========================================================
            ConfigureQualityLevels(verifyPipeline);

            QualitySettings.vSyncCount = 0;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ===========================================================
            // PHASE 9: VERIFY — Re-read from disk and confirm
            // ===========================================================
            bool verified = VerifyRendererPersisted();

            if (verified)
            {
                Debug.Log("========================================");
                Debug.Log("BLACKZONE: ✓✓✓ URP SETUP VERIFIED");
                Debug.Log("  Renderer is linked and persisted.");
                Debug.Log("  No 'Default Renderer is missing' error expected.");
                Debug.Log("========================================");
            }
            else
            {
                Debug.LogError("========================================");
                Debug.LogError("BLACKZONE: ✗✗✗ RENDERER NOT PERSISTED!");
                Debug.LogError("  Manual steps to fix:");
                Debug.LogError("  1. Window > Rendering > Universal RP Asset");
                Debug.LogError("  2. Select " + UrpAssetPath);
                Debug.LogError("  3. In Inspector, set 'Scripted Renderer Feature List'");
                Debug.LogError("     to contain " + RendererAssetPath);
                Debug.LogError("  4. Set 'Default Renderer Index' to 0");
                Debug.LogError("  5. Save the asset");
                Debug.LogError("========================================");
            }

            // Full diagnostic output
            LogFullDiagnostic(verifyPipeline, verifyRenderer);
        }

        /// <summary>
        /// Attempts to link the renderer to the pipeline using multiple approaches
        /// to handle different URP API versions. This is the critical step that
        /// ensures m_RendererDataList[0] points to a saved renderer asset.
        /// </summary>
        private static void LinkRendererMultiApproach(
            UniversalRenderPipelineAsset urp,
            UniversalRendererData rendererData)
        {
            // ------ APPROACH A: Public API SetRenderer ------
            Debug.Log("Blackzone: Approach A — SetRenderer(0, rendererData)...");
            try
            {
                urp.SetRenderer(0, rendererData);
                Debug.Log("Blackzone: SetRenderer(0) succeeded in memory.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Blackzone: SetRenderer(0) failed: " + ex.Message);
            }

            // ------ APPROACH B: SerializedObject m_RendererDataList ------
            Debug.Log("Blackzone: Approach B — SerializedObject m_RendererDataList...");
            var so = new SerializedObject(urp);

            // First, enumerate ALL properties for diagnostic
            Debug.Log("Blackzone: Enumerating all URP serialized properties...");
            EnumerateProperties(so);

            // Try m_RendererDataList (array form — most common)
            bool linked = false;
            linked = TrySetRendererProperty(so, "m_RendererDataList", rendererData, true);

            // Fallback: try m_RendererData (non-array form)
            if (!linked)
            {
                linked = TrySetRendererProperty(so, "m_RendererData", rendererData, false);
            }

            // Fallback: try m_RendererSettings
            if (!linked)
            {
                linked = TrySetRendererProperty(so, "m_RendererSettings", rendererData, false);
            }

            // Set default renderer index
            var defaultIndexProp = so.FindProperty("m_DefaultRendererIndex");
            if (defaultIndexProp != null)
            {
                defaultIndexProp.intValue = 0;
                Debug.Log("Blackzone: Set m_DefaultRendererIndex = 0");
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(urp);

            // ------ APPROACH C: Direct API fallback via reflection ------
            if (!linked)
            {
                Debug.Log("Blackzone: Approach C — Reflection fallback...");
                linked = TrySetRendererViaReflection(urp, rendererData);
            }

            if (!linked)
            {
                Debug.LogError("Blackzone: ALL linking approaches failed. " +
                    "See property enumeration above for the correct property name.");
            }
        }

        /// <summary>
        /// Tries to set a renderer property by name, handling both array and non-array forms.
        /// </summary>
        private static bool TrySetRendererProperty(
            SerializedObject so, string propertyName,
            UniversalRendererData rendererData, bool isArray)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.Log("Blackzone: Property '" + propertyName + "' not found on URP asset.");
                return false;
            }

            if (isArray)
            {
                if (prop.isArray)
                {
                    if (prop.arraySize < 1)
                        prop.arraySize = 1;

                    var element0 = prop.GetArrayElementAtIndex(0);
                    element0.objectReferenceValue = rendererData;
                    Debug.Log("Blackzone: ✓ Set " + propertyName + "[0] = " + rendererData.name);
                    return true;
                }
                else
                {
                    Debug.LogWarning("Blackzone: Property '" + propertyName +
                        "' exists but is NOT an array (type=" + prop.propertyType + ").");
                    return false;
                }
            }
            else
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference ||
                    prop.propertyType == SerializedPropertyType.ExposedReference)
                {
                    prop.objectReferenceValue = rendererData;
                    Debug.Log("Blackzone: ✓ Set " + propertyName + " = " + rendererData.name);
                    return true;
                }
                else
                {
                    Debug.LogWarning("Blackzone: Property '" + propertyName +
                        "' is not an object reference (type=" + prop.propertyType + ").");
                    return false;
                }
            }
        }

        /// <summary>
        /// Reflection fallback: try to find and invoke any method that sets the renderer.
        /// </summary>
        private static bool TrySetRendererViaReflection(
            UniversalRenderPipelineAsset urp,
            UniversalRendererData rendererData)
        {
            var type = urp.GetType();
            var methods = type.GetMethods(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (method.Name.Contains("Renderer") && parameters.Length == 2)
                {
                    if (parameters[0].ParameterType == typeof(int) &&
                        parameters[1].ParameterType == typeof(ScriptableObject))
                    {
                        try
                        {
                            method.Invoke(urp, new object[] { 0, rendererData });
                            Debug.Log("Blackzone: ✓ Reflection invoked " + method.Name + "(0, rendererData)");
                            return true;
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning("Blackzone: Reflection " + method.Name + " failed: " + ex.Message);
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Enumerates and logs ALL serialized properties on the URP asset.
        /// This helps identify the correct property name if it differs across versions.
        /// </summary>
        private static void EnumerateProperties(SerializedObject so)
        {
            var prop = so.GetIterator();
            bool first = true;
            int count = 0;
            while (prop.NextVisible(first))
            {
                count++;
                string value = "";
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.ObjectReference:
                        value = prop.objectReferenceValue != null
                            ? prop.objectReferenceValue.name + " (" + prop.objectReferenceValue.GetType().Name + ")"
                            : "null";
                        break;
                    case SerializedPropertyType.Integer:
                        value = prop.intValue.ToString();
                        break;
                    case SerializedPropertyType.Boolean:
                        value = prop.boolValue.ToString();
                        break;
                    case SerializedPropertyType.Float:
                        value = prop.floatValue.ToString("F3");
                        break;
                    case SerializedPropertyType.ArraySize:
                        value = "size=" + prop.arraySize;
                        break;
                }
                Debug.Log("  [" + count + "] " + prop.name +
                    " (type=" + prop.propertyType + ", path=" + prop.propertyPath +
                    (string.IsNullOrEmpty(value) ? "" : ", value=" + value) + ")");
                first = false;
            }
            Debug.Log("Blackzone: Total serialized properties: " + count);
        }

        /// <summary>
        /// Verifies renderer persistence by re-reading assets from disk.
        /// </summary>
        private static bool VerifyRendererPersisted()
        {
            // Force refresh from disk
            AssetDatabase.Refresh();

            // Re-load pipeline from disk
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (urp == null)
            {
                Debug.LogError("Blackzone: VERIFY FAIL — Could not reload pipeline from " + UrpAssetPath);
                return false;
            }

            // Re-load renderer from disk
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (renderer == null)
            {
                Debug.LogError("Blackzone: VERIFY FAIL — Could not reload renderer from " + RendererAssetPath);
                return false;
            }

            // Method 1: Try GetRenderer(0)
            try
            {
                var r = urp.GetRenderer(0);
                if (r != null)
                {
                    Debug.Log("Blackzone: VERIFY — urp.GetRenderer(0) = " + r.name +
                        " (type=" + r.GetType().Name + ") ✓");
                    return true;
                }
                Debug.LogWarning("Blackzone: VERIFY — urp.GetRenderer(0) returned null, trying SerializedObject...");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Blackzone: VERIFY — urp.GetRenderer(0) threw: " + ex.Message +
                    ", trying SerializedObject...");
            }

            // Method 2: Read back via SerializedObject
            var so = new SerializedObject(urp);
            string[] propertyNames = { "m_RendererDataList", "m_RendererData", "m_RendererSettings" };

            foreach (var name in propertyNames)
            {
                var prop = so.FindProperty(name);
                if (prop == null) continue;

                if (prop.isArray)
                {
                    if (prop.arraySize > 0)
                    {
                        var elem = prop.GetArrayElementAtIndex(0);
                        if (elem.objectReferenceValue != null)
                        {
                            Debug.Log("Blackzone: VERIFY — " + name + "[0] = " +
                                elem.objectReferenceValue.name + " ✓");
                            return true;
                        }
                    }
                }
                else if (prop.objectReferenceValue != null)
                {
                    Debug.Log("Blackzone: VERIFY — " + name + " = " +
                        prop.objectReferenceValue.name + " ✓");
                    return true;
                }
            }

            Debug.LogError("Blackzone: VERIFY FAIL — No renderer found in any property. " +
                "See property enumeration in log for diagnosis.");
            return false;
        }

        /// <summary>
        /// Force-deletes an asset file (both .asset and .meta).
        /// </summary>
        private static void ForceDeleteAsset(string path)
        {
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log("Blackzone: Deleted existing asset: " + path);
            }
            string metaPath = path + ".meta";
            if (File.Exists(metaPath))
            {
                AssetDatabase.DeleteAsset(path + ".meta");
            }
        }

        /// <summary>
        /// Configure URP pipeline settings for BLACKZONE.
        /// </summary>
        private static void ConfigureUrpSettings(UniversalRenderPipelineAsset urp)
        {
            var so = new SerializedObject(urp);

            SetProp(so, "m_SupportsHDR", true);
            SetProp(so, "m_MSAA", 1);
            SetProp(so, "m_ShadowDistance", 45f);
            SetProp(so, "m_ShadowCascadeCount", 2);
            SetProp(so, "m_AdditionalLightsRenderingMode", 1);
            SetProp(so, "m_AdditionalLightsPerObjectLimit", 4);
            SetProp(so, "m_RenderScale", 1.0f);
            SetProp(so, "m_MainLightShadowsSupported", true);
            SetProp(so, "m_MainLightShadowmapResolution", 2048);
            SetProp(so, "m_AdditionalLightShadowsSupported", false);

            so.ApplyModifiedProperties();
            Debug.Log("Blackzone: URP settings configured (HDR, shadows, additional lights).");
        }

        /// <summary>
        /// Configure quality levels pointing at the URP asset.
        /// </summary>
        private static void ConfigureQualityLevels(UniversalRenderPipelineAsset urp)
        {
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

        /// <summary>
        /// Logs full diagnostic information.
        /// </summary>
        private static void LogFullDiagnostic(
            UniversalRenderPipelineAsset urp,
            UniversalRendererData rendererData)
        {
            Debug.Log("========================================");
            Debug.Log("BLACKZONE URP FULL DIAGNOSTIC");
            Debug.Log("========================================");
            Debug.Log("Pipeline asset path: " + UrpAssetPath);
            Debug.Log("Pipeline asset exists on disk: " + File.Exists(UrpAssetPath));
            Debug.Log("Pipeline asset instanceID: " + urp.GetInstanceID());
            Debug.Log("Renderer asset path: " + RendererAssetPath);
            Debug.Log("Renderer asset exists on disk: " + File.Exists(RendererAssetPath));
            Debug.Log("Renderer asset instanceID: " + rendererData.GetInstanceID());
            Debug.Log("Renderer asset name: " + rendererData.name);
            Debug.Log("GraphicsSettings.defaultRenderPipeline: " +
                (GraphicsSettings.defaultRenderPipeline != null
                    ? GraphicsSettings.defaultRenderPipeline.name + " (" +
                      GraphicsSettings.defaultRenderPipeline.GetInstanceID() + ")"
                    : "NULL"));
            Debug.Log("GraphicsSettings.renderPipelineAsset: " +
                (GraphicsSettings.renderPipelineAsset != null
                    ? GraphicsSettings.renderPipelineAsset.name
                    : "NULL"));
            Debug.Log("QualitySettings.renderPipeline: " +
                (QualitySettings.renderPipeline != null
                    ? QualitySettings.renderPipeline.name
                    : "NULL"));
            Debug.Log("QualitySettings.names: " + QualitySettings.names.Length);
            Debug.Log("Unity version: " + Application.unityVersion);

            // GetRenderer test
            try
            {
                var r = urp.GetRenderer(0);
                Debug.Log("urp.GetRenderer(0): " +
                    (r != null ? r.name + " ✓" : "NULL ✗"));
            }
            catch (System.Exception ex)
            {
                Debug.Log("urp.GetRenderer(0) exception: " + ex.Message);
            }

            // Pipeline asset file content check
            if (File.Exists(UrpAssetPath))
            {
                string content = File.ReadAllText(UrpAssetPath);
                bool hasFileID0 = content.Contains("fileID: 0") || content.Contains("fileID:0");
                Debug.Log("URP .asset contains 'fileID: 0': " + hasFileID0 +
                    (hasFileID0 ? " (WARNING — may indicate null references)" : " ✓"));
            }

            Debug.Log("========================================");
            Debug.Log("END BLACKZONE URP DIAGNOSTIC");
            Debug.Log("========================================");
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

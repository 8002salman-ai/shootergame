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
    ///
    /// URP 17 notes (Unity 6000.0.x):
    ///  - UniversalRenderPipelineAsset.Create(ScriptableRendererData) is the
    ///    documented factory that embeds a SAVED renderer into m_RendererDataList[0].
    ///    The renderer asset MUST exist on disk BEFORE Create() is called, otherwise
    ///    the serialized reference becomes fileID:0 ("Default Renderer is missing").
    ///  - There is NO public SetRenderer(...) method and NO renderingPath property
    ///    on UniversalRendererData (it is 'renderingMode'), so this script avoids
    ///    those APIs entirely.
    /// </summary>
    public static class BlackzoneProjectSetup
    {
        private const string URP_FOLDER = "Assets/_Blackzone/Settings/URP";
        private const string UrpAssetPath = URP_FOLDER + "/BlackzoneURP.asset";
        private const string RendererAssetPath = URP_FOLDER + "/BlackzoneForwardRenderer.asset";
        private const string PackagePostProcessDataPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";
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

            // -----------------------------------------------------------
            // PHASE 1: Detect a broken pipeline asset (renderer fileID:0)
            // and delete it so we can recreate from scratch.
            // -----------------------------------------------------------
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (existing != null && !HasValidRenderer(existing))
            {
                Debug.LogWarning("Blackzone: Existing BlackzoneURP.asset has NO valid renderer. " +
                    "Deleting broken assets and recreating fresh...");
                AssetDatabase.DeleteAsset(UrpAssetPath);
                AssetDatabase.DeleteAsset(RendererAssetPath);
                AssetDatabase.Refresh();
            }

            // -----------------------------------------------------------
            // PHASE 2: Ensure the FORWARD RENDERER asset exists on disk.
            // It MUST be saved before the pipeline is created so the
            // serialized m_RendererDataList[0] reference resolves to a
            // real GUID instead of fileID:0.
            // -----------------------------------------------------------
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (rendererData == null)
            {
                Debug.Log("Blackzone: Creating Forward Renderer Data...");
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.postProcessData = LoadDefaultPostProcessData();
                AssetDatabase.CreateAsset(rendererData, RendererAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
                Debug.Log("Blackzone: Renderer saved at " + RendererAssetPath);
            }
            else
            {
                // Repair any missing post-process data reference (covers GUID drift).
                if (rendererData.postProcessData == null)
                {
                    rendererData.postProcessData = LoadDefaultPostProcessData();
                    EditorUtility.SetDirty(rendererData);
                    AssetDatabase.SaveAssets();
                }
                Debug.Log("Blackzone: Loaded existing renderer: " + RendererAssetPath);
            }

            if (rendererData == null)
            {
                Debug.LogError("BLACKZONE: FAILED to create/load renderer asset at " + RendererAssetPath);
                return;
            }

            // -----------------------------------------------------------
            // PHASE 3: Create the URP PIPELINE asset.
            // DOCUMENTED PATH: UniversalRenderPipelineAsset.Create(rendererData)
            // embeds the SAVED renderer into m_RendererDataList[0] — the same
            // approach Unity's own "Assets > Create > Rendering > URP Asset"
            // menu uses internally.
            // -----------------------------------------------------------
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (urp == null)
            {
                Debug.Log("Blackzone: Creating URP Pipeline via Create(rendererData)...");
                urp = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(urp, UrpAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
                Debug.Log("Blackzone: Pipeline saved at " + UrpAssetPath);
            }

            if (urp == null)
            {
                Debug.LogError("BLACKZONE: FAILED to create/load pipeline asset at " + UrpAssetPath);
                return;
            }

            // -----------------------------------------------------------
            // PHASE 4: Safety — if the loaded pipeline is missing the
            // renderer (e.g. an old broken file), link it via SerializedObject.
            // -----------------------------------------------------------
            if (!HasValidRenderer(urp))
            {
                Debug.LogWarning("Blackzone: Existing pipeline missing renderer — linking via SerializedObject...");
                LinkRendererViaSerializedObject(urp, rendererData);
            }

            // -----------------------------------------------------------
            // PHASE 5: Configure URP settings (HDR, shadows, mobile perf)
            // -----------------------------------------------------------
            ConfigureUrpSettings(urp);

            // -----------------------------------------------------------
            // PHASE 6: Save everything
            // -----------------------------------------------------------
            EditorUtility.SetDirty(urp);
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // -----------------------------------------------------------
            // PHASE 7: Assign to GraphicsSettings
            // -----------------------------------------------------------
            GraphicsSettings.defaultRenderPipeline = urp;
            GraphicsSettings.renderPipelineAsset = urp;

            // -----------------------------------------------------------
            // PHASE 8: Assign to QualitySettings (each level)
            // -----------------------------------------------------------
            ConfigureQualityLevels(urp);

            QualitySettings.vSyncCount = 0;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // -----------------------------------------------------------
            // PHASE 9: VERIFY — re-read from disk and confirm
            // -----------------------------------------------------------
            bool verified = VerifyRendererPersisted();
            if (verified)
            {
                Debug.Log("========================================");
                Debug.Log("BLACKZONE: ✓✓✓ URP SETUP VERIFIED");
                Debug.Log("  Renderer is linked and persisted to disk.");
                Debug.Log("  No 'Default Renderer is missing' error expected.");
                Debug.Log("========================================");
            }
            else
            {
                Debug.LogError("========================================");
                Debug.LogError("BLACKZONE: ✗✗✗ RENDERER NOT PERSISTED!");
                Debug.LogError("  Manual fix: select " + UrpAssetPath);
                Debug.LogError("  → Inspector → Renderer List → drag " +
                    RendererAssetPath + " into slot 0");
                Debug.LogError("  → Default Renderer Index = 0 → Save");
                Debug.LogError("========================================");
            }

            LogFullDiagnostic(urp, rendererData);
        }

        /// <summary>Loads URP's default PostProcessData asset from the package.</summary>
        private static PostProcessData LoadDefaultPostProcessData()
        {
            var data = AssetDatabase.LoadAssetAtPath<PostProcessData>(PackagePostProcessDataPath);
            if (data == null)
                Debug.LogWarning("Blackzone: Could not load URP default PostProcessData at " +
                    PackagePostProcessDataPath + " (post-processing resources will be null).");
            return data;
        }

        /// <summary>
        /// Checks whether the pipeline has a non-null renderer at m_RendererDataList[0]
        /// using the public GetRenderer(0) API.
        /// </summary>
        private static bool HasValidRenderer(UniversalRenderPipelineAsset urp)
        {
            if (urp == null) return false;
            try
            {
                return urp.GetRenderer(0) != null;
            }
            catch
            {
                // Fall back to SerializedObject check
                var so = new SerializedObject(urp);
                var list = so.FindProperty("m_RendererDataList");
                if (list != null && list.isArray && list.arraySize > 0)
                    return list.GetArrayElementAtIndex(0).objectReferenceValue != null;
                return false;
            }
        }

        /// <summary>
        /// Links renderer data to an existing pipeline via SerializedObject.
        /// This writes m_RendererDataList[0] and m_DefaultRendererIndex directly
        /// to the asset's serialized fields.
        /// </summary>
        private static void LinkRendererViaSerializedObject(
            UniversalRenderPipelineAsset urp,
            UniversalRendererData rendererData)
        {
            var so = new SerializedObject(urp);

            var rendererListProp = so.FindProperty("m_RendererDataList");
            if (rendererListProp == null)
                rendererListProp = so.FindProperty("m_RendererData");

            if (rendererListProp != null)
            {
                if (rendererListProp.isArray)
                {
                    if (rendererListProp.arraySize < 1)
                        rendererListProp.arraySize = 1;
                    var element0 = rendererListProp.GetArrayElementAtIndex(0);
                    element0.objectReferenceValue = rendererData;
                    Debug.Log("Blackzone: Set m_RendererDataList[0] = " + rendererData.name);
                }
                else
                {
                    rendererListProp.objectReferenceValue = rendererData;
                    Debug.Log("Blackzone: Set m_RendererData = " + rendererData.name);
                }
            }
            else
            {
                Debug.LogError("Blackzone: Could not find m_RendererDataList/m_RendererData property. " +
                    "The installed URP version may serialize renderers differently.");
                ListSerializedProperties(so);
                return;
            }

            var defaultIndexProp = so.FindProperty("m_DefaultRendererIndex");
            if (defaultIndexProp != null)
            {
                defaultIndexProp.intValue = 0;
                Debug.Log("Blackzone: Set m_DefaultRendererIndex = 0");
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(urp);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Lists all serialized properties for diagnostics.</summary>
        private static void ListSerializedProperties(SerializedObject so)
        {
            var prop = so.GetIterator();
            bool first = true;
            while (prop.NextVisible(first))
            {
                Debug.Log("  Property: " + prop.name + " (type=" + prop.propertyType +
                    ", path=" + prop.propertyPath + ")");
                first = false;
            }
        }

        /// <summary>
        /// Verifies the renderer reference persisted to disk by re-loading the
        /// asset and checking via both GetRenderer(0) and SerializedObject.
        /// </summary>
        private static bool VerifyRendererPersisted()
        {
            AssetDatabase.Refresh();

            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (urp == null)
            {
                Debug.LogError("Blackzone: VERIFY FAIL — Could not reload pipeline from " + UrpAssetPath);
                return false;
            }

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (renderer == null)
            {
                Debug.LogError("Blackzone: VERIFY FAIL — Could not reload renderer from " + RendererAssetPath);
                return false;
            }

            // Method 1: public API
            try
            {
                var r = urp.GetRenderer(0);
                if (r != null)
                {
                    Debug.Log("Blackzone: VERIFY — urp.GetRenderer(0) = " + r.name +
                        " (type=" + r.GetType().Name + ") ✓");
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Blackzone: VERIFY — GetRenderer(0) threw: " + ex.Message);
            }

            // Method 2: SerializedObject read-back
            var so = new SerializedObject(urp);
            string[] propertyNames = { "m_RendererDataList", "m_RendererData" };
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

            Debug.LogError("Blackzone: VERIFY FAIL — No renderer found in serialized asset.");
            return false;
        }

        /// <summary>Configures URP pipeline settings for BLACKZONE (mobile tactical FPS).</summary>
        private static void ConfigureUrpSettings(UniversalRenderPipelineAsset urp)
        {
            var so = new SerializedObject(urp);

            SetProp(so, "m_SupportsHDR", true);
            SetProp(so, "m_MSAA", 1);
            SetProp(so, "m_ShadowDistance", 45f);
            SetProp(so, "m_ShadowCascadeCount", 2);
            SetProp(so, "m_AdditionalLightsRenderingMode", 1); // PerPixel
            SetProp(so, "m_AdditionalLightsPerObjectLimit", 4);
            SetProp(so, "m_RenderScale", 1.0f);
            SetProp(so, "m_MainLightShadowsSupported", true);
            SetProp(so, "m_MainLightShadowmapResolution", 2048);
            SetProp(so, "m_AdditionalLightShadowsSupported", false);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(urp);
            Debug.Log("Blackzone: URP settings configured (HDR, shadows, additional lights).");
        }

        /// <summary>Assigns the URP asset to every quality level.</summary>
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

        /// <summary>Logs full diagnostics about the URP setup.</summary>
        private static void LogFullDiagnostic(
            UniversalRenderPipelineAsset urp,
            UniversalRendererData rendererData)
        {
            Debug.Log("========================================");
            Debug.Log("BLACKZONE URP FULL DIAGNOSTIC");
            Debug.Log("========================================");
            Debug.Log("Pipeline asset path: " + UrpAssetPath + " (exists=" + File.Exists(UrpAssetPath) + ")");
            Debug.Log("Renderer asset path: " + RendererAssetPath + " (exists=" + File.Exists(RendererAssetPath) + ")");
            Debug.Log("Renderer asset name: " + rendererData.name);
            Debug.Log("GraphicsSettings.defaultRenderPipeline: " +
                (GraphicsSettings.defaultRenderPipeline != null
                    ? GraphicsSettings.defaultRenderPipeline.name
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

            try
            {
                var r = urp.GetRenderer(0);
                Debug.Log("urp.GetRenderer(0): " + (r != null ? r.name + " ✓" : "NULL ✗"));
            }
            catch (System.Exception ex)
            {
                Debug.Log("urp.GetRenderer(0) exception: " + ex.Message);
            }

            if (File.Exists(UrpAssetPath))
            {
                string content = File.ReadAllText(UrpAssetPath);
                bool rendererRefOk = content.Contains(RendererAssetPath.Replace("Assets/", "")) ||
                                     content.Contains("cb567ed48ac259298818a8c68d22ede8");
                Debug.Log("URP .asset renderer GUID reference present: " + rendererRefOk);
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

using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARCore;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;

namespace SimpleARPlacement.EditorTools
{
    /// <summary>
    /// Tool editor untuk membangun scene, prefab, material, konfigurasi build,
    /// dan APK dari aplikasi SimpleARPlacement.
    ///
    /// Dipakai dari menu: Tools > SimpleARPlacement > ...
    /// Atau dari command line:
    ///   Unity -batchmode -projectPath "..." -executeMethod SimpleARPlacement.EditorTools.SimpleARPlacementTool.CreateSceneAndPrefabs -quit
    ///   Unity -batchmode -projectPath "..." -buildTarget Android -executeMethod SimpleARPlacement.EditorTools.SimpleARPlacementTool.BuildApk -quit
    /// </summary>
    public static class SimpleARPlacementTool
    {
        private const string FolderRoot = "Assets/SimpleARPlacement";
        private const string FolderPrefabs = FolderRoot + "/Prefabs";
        private const string FolderMaterials = FolderRoot + "/Materials";
        private const string FolderTextures = FolderRoot + "/Textures";

        private const string ScenePath = "Assets/Scenes/SimpleARPlacement.unity";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string RigPrefabPath =
            "Assets/Samples/XR Interaction Toolkit/3.5.1/AR Starter Assets/Prefabs/XR Origin (AR Rig).prefab";

        private const string PlanePrefabPath = FolderPrefabs + "/SimpleARPlaneVisualizer.prefab";
        private const string ChairPrefabPath = FolderPrefabs + "/SimpleChair.prefab";
        private const string PlaneMaterialPath = FolderMaterials + "/SimpleARPlaneMaterial.mat";
        private const string SeatMaterialPath = FolderMaterials + "/SimpleChairSeat.mat";
        private const string LegMaterialPath = FolderMaterials + "/SimpleChairLeg.mat";
        private const string IndicatorMaterialPath = FolderMaterials + "/SimpleARIndicator.mat";
        private const string GridTexturePath = FolderTextures + "/PlaneGridTexture.asset";

        private const string ApkPath = "Builds/SimpleARPlacement.apk";

        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color ButtonNormal = new Color(0.10f, 0.12f, 0.16f, 0.95f);
        private static readonly Color ButtonHighlight = new Color(0.16f, 0.35f, 0.60f, 1f);
        private static readonly Color ButtonPressed = new Color(0.08f, 0.20f, 0.38f, 1f);
        private static readonly Color ButtonDisabled = new Color(0.25f, 0.25f, 0.25f, 0.6f);

        // ------------------------------------------------------------------
        // Menu
        // ------------------------------------------------------------------

        [MenuItem("Tools/SimpleARPlacement/1. Buat Scene dan Prefab", priority = 1)]
        public static void CreateSceneAndPrefabsMenu()
        {
            try
            {
                CreateSceneAndPrefabs();
                Debug.Log("[SimpleARPlacement] Scene dan prefab berhasil dibuat/diperbarui.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleARPlacement] Gagal membuat scene/prefab: {e}");
                EditorUtility.DisplayDialog("SimpleARPlacement", $"Gagal: {e.Message}", "OK");
            }
        }

        [MenuItem("Tools/SimpleARPlacement/2. Validasi Konfigurasi", priority = 2)]
        public static void ValidateSetupMenu()
        {
            try
            {
                ValidateSetup();
                Debug.Log("[SimpleARPlacement] Validasi lulus. Proyek siap dibangun untuk Android.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleARPlacement] Validasi gagal: {e.Message}");
                EditorUtility.DisplayDialog("SimpleARPlacement", $"Validasi gagal: {e.Message}", "OK");
            }
        }

        [MenuItem("Tools/SimpleARPlacement/3. Build APK Android", priority = 3)]
        public static void BuildApkMenu()
        {
            try
            {
                BuildApk();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleARPlacement] Build gagal: {e}");
                EditorUtility.DisplayDialog("SimpleARPlacement", $"Build gagal: {e.Message}", "OK");
            }
        }

        // ------------------------------------------------------------------
        // Entry point batchmode
        // ------------------------------------------------------------------

        // ------------------------------------------------------------------
        // Entry point batchmode (kode keluar eksplisit)
        // ------------------------------------------------------------------

        public static void CreateSceneAndPrefabsBatch()
        {
            try
            {
                CreateSceneAndPrefabs();
                Debug.Log("[SimpleARPlacement] OK: scene, prefab, dan material dibuat.");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleARPlacement] GAGAL membuat scene/prefab: {e}");
                EditorApplication.Exit(1);
            }
        }

        public static void ValidateSetupBatch()
        {
            try
            {
                ValidateSetup();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleARPlacement] GAGAL validasi: {e.Message}");
                EditorApplication.Exit(1);
            }
        }

        public static void BuildApkBatch()
        {
            try
            {
                BuildApk();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleARPlacement] GAGAL build APK: {e}");
                EditorApplication.Exit(1);
            }
        }

        // ------------------------------------------------------------------
        // Entry point utama
        // ------------------------------------------------------------------

        public static void CreateSceneAndPrefabs()
        {
            EnsureFolders();

            Texture2D gridTexture = CreateGridTexture();
            Material planeMaterial = CreatePlaneMaterial(gridTexture);
            Material seatMaterial = CreateLitMaterial(SeatMaterialPath, "SimpleChairSeat",
                new Color(0.62f, 0.43f, 0.26f, 1f), 0.3f);
            Material legMaterial = CreateLitMaterial(LegMaterialPath, "SimpleChairLeg",
                new Color(0.28f, 0.19f, 0.12f, 1f), 0.25f);
            Material indicatorMaterial = CreateIndicatorMaterial();

            GameObject planePrefab = CreatePlanePrefab(planeMaterial);
            GameObject chairPrefab = CreateChairPrefab(seatMaterial, legMaterial);

            BuildScene(planePrefab, chairPrefab, planeMaterial, indicatorMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void ValidateSetup()
        {
            var errors = new System.Collections.Generic.List<string>();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                errors.Add($"Scene {ScenePath} belum ada. Jalankan menu '1. Buat Scene dan Prefab'.");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath) == null)
            {
                errors.Add("Prefab XR Origin (AR Rig) dari sampel XRI tidak ditemukan.");
            }

            GameObject planePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlanePrefabPath);
            if (planePrefab == null)
            {
                errors.Add("Prefab visualisasi plane belum dibuat.");
            }
            else if (planePrefab.GetComponent<ARPlane>() == null ||
                     planePrefab.GetComponent<ARPlaneMeshVisualizer>() == null ||
                     planePrefab.GetComponent<MeshRenderer>() == null)
            {
                errors.Add("Prefab visualisasi plane belum memiliki ARPlane / ARPlaneMeshVisualizer / MeshRenderer.");
            }

            GameObject chairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChairPrefabPath);
            if (chairPrefab == null)
            {
                errors.Add("Prefab objek (kursi) belum dibuat.");
            }
            else if (chairPrefab.GetComponentInChildren<Collider>() == null)
            {
                errors.Add("Prefab objek tidak memiliki collider.");
            }

            if (FindPackage("com.unity.xr.arfoundation") == null)
            {
                errors.Add("Paket AR Foundation tidak terpasang.");
            }

            if (FindPackage("com.unity.xr.arcore") == null)
            {
                errors.Add("Paket ARCore XR Plugin tidak terpasang.");
            }

            bool arcoreLoaderActive = false;
            if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,
                    out XRGeneralSettingsPerBuildTarget generalSettings) &&
                generalSettings != null)
            {
                XRGeneralSettings androidSettings =
                    generalSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
                arcoreLoaderActive = androidSettings != null &&
                                     androidSettings.Manager != null &&
                                     androidSettings.Manager.activeLoaders != null &&
                                     androidSettings.Manager.activeLoaders.Any(l => l is ARCoreLoader);
            }

            if (!arcoreLoaderActive)
            {
                errors.Add("Loader ARCore tidak aktif untuk build Android " +
                           "(Project Settings > XR Plug-in Management).");
            }

            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel25)
            {
                errors.Add("Android Min SDK lebih rendah dari syarat ARCore (minimal API 25).");
            }

            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
            {
                errors.Add("Arsitektur ARM64 belum diaktifkan untuk Android.");
            }

            if (errors.Count > 0)
            {
                throw new Exception(string.Join("\n", errors));
            }

            Debug.Log(
                "[SimpleARPlacement] Validasi lulus:\n" +
                $"- Scene: {ScenePath}\n" +
                "- Prefab plane, prefab kursi, material: OK\n" +
                "- AR Foundation + ARCore terpasang, loader ARCore aktif\n" +
                $"- Min SDK: {PlayerSettings.Android.minSdkVersion}\n" +
                "- ARM64 aktif");
        }

        public static void BuildApk()
        {
            CreateSceneAndPrefabs();
            ValidateSetup();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Android, BuildTarget.Android);
            }

            ApplyAndroidPlayerSettings();

            string directory = Path.GetDirectoryName(ApkPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(ApkPath))
            {
                File.Delete(ApkPath);
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log($"[SimpleARPlacement] Hasil build: {summary.result}, " +
                      $"ukuran {summary.totalSize} byte, waktu {summary.totalTime}, " +
                      $"error {summary.totalErrors}.");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Build Android gagal dengan hasil {summary.result} " +
                    $"({summary.totalErrors} error). Lihat log editor.");
            }

            Debug.Log($"[SimpleARPlacement] APK berhasil dibuat: {Path.GetFullPath(ApkPath)}");
        }

        public static void ApplyAndroidPlayerSettings()
        {
            PlayerSettings.companyName = "SimpleAR";
            PlayerSettings.productName = "SimpleARPlacement";
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android, "com.simplearplacement.app");
            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // ARCore (pada Unity 6000.3) mensyaratkan min API 25. Nilai 26 agar
            // kompatibel dengan mayoritas perangkat yang mendukung ARCore.
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;

            ConfigureAndroidToolsPaths();
        }

        /// <summary>
        /// Menunjuk tool Android (JDK, SDK, NDK) ke instalasi bawaan editor Unity.
        /// Menggunakan refleksi agar skrip tetap bisa dikompilasi pada instalasi
        /// Unity yang belum memiliki dukungan Android.
        /// </summary>
        private static void ConfigureAndroidToolsPaths()
        {
            Type type = Type.GetType(
                "UnityEditor.Android.AndroidExternalToolsSettings, UnityEditor.Android.Extensions");

            if (type == null)
            {
                Debug.LogWarning("[SimpleARPlacement] UnityEditor.Android tidak tersedia. " +
                                 "Pastikan Android Build Support terpasang.");
                return;
            }

            string androidPlayer = Path.Combine(
                EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer");

            SetAndroidToolsProperty(type, "jdkRootPath", Path.Combine(androidPlayer, "OpenJDK"));
            SetAndroidToolsProperty(type, "sdkRootPath", Path.Combine(androidPlayer, "SDK"));
            SetAndroidToolsProperty(type, "ndkRootPath", Path.Combine(androidPlayer, "NDK"));
        }

        private static void SetAndroidToolsProperty(Type type, string propertyName, string path)
        {
            var property = type.GetProperty(propertyName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (property == null || !property.CanWrite)
            {
                Debug.LogWarning($"[SimpleARPlacement] Properti {propertyName} tidak ditemukan.");
                return;
            }

            string current = property.GetValue(null, null) as string;
            if (string.Equals(current, path, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                property.SetValue(null, path);
                Debug.Log($"[SimpleARPlacement] {propertyName} = {path}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SimpleARPlacement] Gagal mengatur {propertyName}: {e.Message}");
            }
        }

        // ------------------------------------------------------------------
        // Aset: folder, tekstur, material, prefab
        // ------------------------------------------------------------------

        private static void EnsureFolders()
        {
            CreateFolderRecursive("Assets", "SimpleARPlacement");
            CreateFolderRecursive(FolderRoot, "Prefabs");
            CreateFolderRecursive(FolderRoot, "Materials");
            CreateFolderRecursive(FolderRoot, "Textures");
            CreateFolderRecursive(FolderRoot, "Editor");
            CreateFolderRecursive("Assets", "Scenes");
        }

        private static void CreateFolderRecursive(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static UnityEditor.PackageManager.PackageInfo FindPackage(string packageName)
        {
            return UnityEditor.PackageManager.PackageInfo.FindForAssetPath(
                $"Packages/{packageName}/package.json");
        }

        private static Texture2D CreateGridTexture()
        {
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(GridTexturePath);
            if (existing != null)
            {
                return existing;
            }

            const int size = 64;
            const int cell = 32;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PlaneGridTexture",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };

            var background = new Color32(255, 255, 255, 55);
            var line = new Color32(255, 255, 255, 230);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isLine = x % cell == 0 || y % cell == 0;
                    pixels[y * size + x] = isLine ? line : background;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            AssetDatabase.CreateAsset(texture, GridTexturePath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(GridTexturePath);
        }

        private static Material CreatePlaneMaterial(Texture2D texture)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(PlaneMaterialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                throw new Exception("Shader 'Universal Render Pipeline/Unlit' tidak ditemukan.");
            }

            Material material = existing != null ? existing : new Material(shader);
            material.shader = shader;
            material.name = "SimpleARPlaneMaterial";

            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetColor("_BaseColor", new Color(0.25f, 0.85f, 1f, 0.45f));

            ConfigureTransparent(material);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(material, PlaneMaterialPath);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateIndicatorMaterial()
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(IndicatorMaterialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                throw new Exception("Shader 'Universal Render Pipeline/Unlit' tidak ditemukan.");
            }

            Material material = existing != null ? existing : new Material(shader);
            material.shader = shader;
            material.name = "SimpleARIndicator";
            material.SetColor("_BaseColor", new Color(0.1f, 1f, 0.85f, 0.9f));

            ConfigureTransparent(material);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(material, IndicatorMaterialPath);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureTransparent(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static Material CreateLitMaterial(string path, string materialName, Color color,
            float smoothness)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new Exception("Shader 'Universal Render Pipeline/Lit' tidak ditemukan.");
            }

            Material material = existing != null ? existing : new Material(shader);
            material.shader = shader;
            material.name = materialName;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(material, path);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreatePlanePrefab(Material material)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlanePrefabPath);
            if (existing != null)
            {
                ConfigurePlanePrefab(existing, material);
                return existing;
            }

            var go = new GameObject("SimpleARPlaneVisualizer");
            go.AddComponent<MeshFilter>();
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            go.AddComponent<ARPlaneMeshVisualizer>();
            go.AddComponent<ARPlane>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PlanePrefabPath);
            UnityEngine.Object.DestroyImmediate(go);

            Debug.Log($"[SimpleARPlacement] Prefab plane dibuat: {PlanePrefabPath}");
            return prefab;
        }

        private static void ConfigurePlanePrefab(GameObject prefab, Material material)
        {
            var meshRenderer = prefab.GetComponent<MeshRenderer>();
            if (meshRenderer != null && meshRenderer.sharedMaterial != material)
            {
                meshRenderer.sharedMaterial = material;
                EditorUtility.SetDirty(meshRenderer);
            }
        }

        private static GameObject CreateChairPrefab(Material seatMaterial, Material legMaterial)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ChairPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject("SimpleChair");

            // Pivot berada di titik sentuh permukaan (dasar kaki), sehingga objek
            // ditempatkan tepat di permukaan dan tidak tenggelam/melayang.
            AddPrimitivePart(root.transform, "Seat", PrimitiveType.Cube,
                new Vector3(0f, 0.44f, 0f), new Vector3(0.44f, 0.06f, 0.44f), seatMaterial);
            AddPrimitivePart(root.transform, "Backrest", PrimitiveType.Cube,
                new Vector3(0f, 0.74f, -0.19f), new Vector3(0.44f, 0.5f, 0.06f), seatMaterial);

            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -0.185f : 0.185f;
                float z = (i < 2) ? -0.185f : 0.185f;
                AddPrimitivePart(root.transform, $"Leg {i + 1}", PrimitiveType.Cube,
                    new Vector3(x, 0.205f, z), new Vector3(0.05f, 0.41f, 0.05f), legMaterial);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ChairPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            Debug.Log($"[SimpleARPlacement] Prefab kursi dibuat: {ChairPrefabPath}");
            return prefab;
        }

        private static void AddPrimitivePart(Transform parent, string partName,
            PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        // ------------------------------------------------------------------
        // Scene
        // ------------------------------------------------------------------

        private static void BuildScene(GameObject planePrefab, GameObject chairPrefab,
            Material planeMaterial, Material indicatorMaterial)
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLight();
            GameObject sessionObject = BuildARSession();
            GameObject rig = BuildRig(planePrefab);

            GameObject indicator = BuildIndicator(indicatorMaterial);
            GameObject uiRoot = BuildUI(rig);

            // Referensi silang antar komponen (serialized, tersimpan di scene).
            ARPlaneController planeController = rig.GetComponent<ARPlaneController>();
            ARPlacementManager placementManager = rig.GetComponent<ARPlacementManager>();
            ARUIController uiController = uiRoot.GetComponent<ARUIController>();

            var placementSerialized = new SerializedObject(placementManager);
            SetReference(placementSerialized, "placeablePrefab", chairPrefab);
            SetReference(placementSerialized, "placementIndicator", indicator.transform);
            SetReference(placementSerialized, "planeController", planeController);
            SetReference(placementSerialized, "uiController", uiController);
            placementSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new BuildFailedException($"Gagal menyimpan scene {ScenePath}");
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(SampleScenePath, false)
            };

            ApplyAndroidPlayerSettings();

            Debug.Log($"[SimpleARPlacement] Scene disimpan: {ScenePath}");
            Debug.Log($"[SimpleARPlacement] Objek AR Session: {sessionObject.name}, " +
                      $"Rig: {rig.name}, UI: {uiRoot.name}");
        }

        private static GameObject BuildRig(GameObject planePrefab)
        {
            GameObject rigPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
            if (rigPrefabAsset == null)
            {
                throw new BuildFailedException(
                    "Prefab XR Origin (AR Rig) tidak ditemukan. Pastikan sampel " +
                    "'XR Interaction Toolkit > AR Starter Assets' sudah diimpor.");
            }

            GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefabAsset);
            rig.name = "XR Origin (AR Rig)";
            rig.transform.position = Vector3.zero;
            rig.transform.rotation = Quaternion.identity;

            ARPlaneManager planeManager = rig.GetComponent<ARPlaneManager>();
            if (planeManager == null)
            {
                planeManager = rig.AddComponent<ARPlaneManager>();
            }

            planeManager.planePrefab = planePrefab;
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;

            if (rig.GetComponent<ARRaycastManager>() == null)
            {
                rig.AddComponent<ARRaycastManager>();
            }

            if (rig.GetComponent<ARAnchorManager>() == null)
            {
                rig.AddComponent<ARAnchorManager>();
            }

            if (rig.GetComponent<ARPlaneController>() == null)
            {
                rig.AddComponent<ARPlaneController>();
            }

            if (rig.GetComponent<ARPlacementManager>() == null)
            {
                rig.AddComponent<ARPlacementManager>();
            }

            return rig;
        }

        private static GameObject BuildARSession()
        {
            var sessionObject = new GameObject("AR Session");
            ARSession session = sessionObject.AddComponent<ARSession>();
            session.attemptUpdate = true;
            session.matchFrameRateRequested = true;
            session.requestedTrackingMode = TrackingMode.PositionAndRotation;
            sessionObject.AddComponent<ARInputManager>();
            return sessionObject;
        }

        private static void BuildLight()
        {
            var lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.96f, 0.9f);
            lightObject.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
        }

        private static GameObject BuildIndicator(Material material)
        {
            var root = new GameObject("Placement Indicator");

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            DestroyCollider(ring);
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.004f, 0f);
            ring.transform.localScale = new Vector3(0.24f, 0.002f, 0.24f);
            ring.GetComponent<Renderer>().sharedMaterial = material;

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            DestroyCollider(pole);
            pole.transform.SetParent(root.transform, false);
            pole.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            pole.transform.localScale = new Vector3(0.01f, 0.05f, 0.01f);
            pole.GetComponent<Renderer>().sharedMaterial = material;

            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = "Dot";
            DestroyCollider(dot);
            dot.transform.SetParent(root.transform, false);
            dot.transform.localPosition = new Vector3(0f, 0.13f, 0f);
            dot.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
            dot.GetComponent<Renderer>().sharedMaterial = material;

            root.SetActive(false);
            return root;
        }

        private static void DestroyCollider(GameObject go)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        // ------------------------------------------------------------------
        // UI
        // ------------------------------------------------------------------

        private static GameObject BuildUI(GameObject rig)
        {
            var uiRoot = new GameObject("SimpleAR Placement UI",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            uiRoot.transform.SetParent(null, false);

            Canvas canvas = uiRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = uiRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            ARUIController uiController = uiRoot.AddComponent<ARUIController>();

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;

            // ---- Panel atas: status + instruksi ----
            RectTransform topPanel = CreatePanel(uiRoot.transform, "Top Panel", PanelColor);
            topPanel.anchorMin = new Vector2(0f, 1f);
            topPanel.anchorMax = new Vector2(1f, 1f);
            topPanel.pivot = new Vector2(0.5f, 1f);
            topPanel.sizeDelta = new Vector2(0f, 240f);
            topPanel.anchoredPosition = Vector2.zero;

            var topLayout = topPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            topLayout.padding = new RectOffset(24, 24, 20, 20);
            topLayout.spacing = 6f;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = true;
            topLayout.childForceExpandHeight = false;
            topLayout.childAlignment = TextAnchor.UpperLeft;

            TextMeshProUGUI statusText =
                CreateText(topPanel, "Status Text", font, 34f, new Color(0.55f, 0.95f, 1f),
                    TextAlignmentOptions.MidlineLeft, "Status AR");
            var statusLayout = statusText.gameObject.AddComponent<LayoutElement>();
            statusLayout.minHeight = 48f;
            statusLayout.preferredHeight = 48f;

            TextMeshProUGUI instructionText =
                CreateText(topPanel, "Instruction Text", font, 46f, Color.white,
                    TextAlignmentOptions.Center,
                    "Arahkan kamera ke lantai atau meja, lalu gerakkan perlahan.");
            var instructionLayout = instructionText.gameObject.AddComponent<LayoutElement>();
            instructionLayout.minHeight = 80f;
            instructionLayout.preferredHeight = 130f;
            instructionText.enableAutoSizing = true;
            instructionText.fontSizeMin = 28f;
            instructionText.fontSizeMax = 52f;

            // ---- Toast ----
            RectTransform toastRect = CreateRect(uiRoot.transform, "Toast Text");
            toastRect.anchorMin = new Vector2(0f, 0f);
            toastRect.anchorMax = new Vector2(1f, 0f);
            toastRect.pivot = new Vector2(0.5f, 0f);
            toastRect.sizeDelta = new Vector2(0f, 90f);
            toastRect.anchoredPosition = new Vector2(0f, 220f);

            TextMeshProUGUI toastText = toastRect.gameObject.AddComponent<TextMeshProUGUI>();
            toastText.text = string.Empty;
            if (font != null)
            {
                toastText.font = font;
            }

            toastText.fontSize = 38f;
            toastText.color = new Color(1f, 0.9f, 0.35f, 1f);
            toastText.alignment = TextAlignmentOptions.Center;
            toastText.raycastTarget = false;
            toastText.enableAutoSizing = true;
            toastText.fontSizeMin = 24f;
            toastText.fontSizeMax = 42f;

            // ---- Panel bawah: tombol ----
            RectTransform bottomPanel = CreatePanel(uiRoot.transform, "Bottom Panel", PanelColor);
            bottomPanel.anchorMin = new Vector2(0f, 0f);
            bottomPanel.anchorMax = new Vector2(1f, 0f);
            bottomPanel.pivot = new Vector2(0.5f, 0f);
            bottomPanel.sizeDelta = new Vector2(0f, 200f);
            bottomPanel.anchoredPosition = Vector2.zero;

            var bottomLayout = bottomPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            bottomLayout.padding = new RectOffset(16, 16, 24, 24);
            bottomLayout.spacing = 10f;
            bottomLayout.childControlWidth = true;
            bottomLayout.childControlHeight = true;
            bottomLayout.childForceExpandWidth = false;
            bottomLayout.childForceExpandHeight = false;
            bottomLayout.childAlignment = TextAnchor.MiddleCenter;

            Button rotateLeft = CreateButton(bottomPanel, "Button Rotate Left", font, "\u25C0", 56f, 150f);
            Button rotateRight = CreateButton(bottomPanel, "Button Rotate Right", font, "\u25B6", 56f, 150f);
            Button scaleDown = CreateButton(bottomPanel, "Button Scale Down", font, "\u2212", 60f, 150f);
            Button scaleUp = CreateButton(bottomPanel, "Button Scale Up", font, "+", 60f, 150f);
            Button reset = CreateButton(bottomPanel, "Button Reset", font, "Reset", 34f, 200f);
            Button planeToggle = CreateButton(bottomPanel, "Button Planes", font, "Grid", 34f, 200f);

            // EventSystem dengan modul input sesuai proyek (Input System).
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystemObject =
                    new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                var inputModule = eventSystemObject.GetComponent<InputSystemUIInputModule>();
                if (inputModule != null)
                {
                    inputModule.AssignDefaultActions();
                }
            }

            // ---- Wiring ----
            var serialized = new SerializedObject(uiController);
            SetReference(serialized, "statusText", statusText);
            SetReference(serialized, "instructionText", instructionText);
            SetReference(serialized, "toastText", toastText);
            SetReference(serialized, "rotateLeftButton", rotateLeft);
            SetReference(serialized, "rotateRightButton", rotateRight);
            SetReference(serialized, "scaleDownButton", scaleDown);
            SetReference(serialized, "scaleUpButton", scaleUp);
            SetReference(serialized, "resetButton", reset);
            SetReference(serialized, "planeToggleButton", planeToggle);
            SetReference(serialized, "placementManager", rig.GetComponent<ARPlacementManager>());
            SetReference(serialized, "planeController", rig.GetComponent<ARPlaneController>());
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return uiRoot;
        }

        private static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            RectTransform rect = CreateRect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return rect;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name,
            TMP_FontAsset font, float fontSize, Color color, TextAlignmentOptions alignment,
            string content)
        {
            RectTransform rect = CreateRect(parent, name);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name,
            TMP_FontAsset font, string label, float fontSize, float width)
        {
            RectTransform rect = CreateRect(parent, name);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = ButtonNormal;
            colors.highlightedColor = ButtonHighlight;
            colors.pressedColor = ButtonPressed;
            colors.selectedColor = ButtonHighlight;
            colors.disabledColor = ButtonDisabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = 140f;
            layoutElement.minWidth = width;
            layoutElement.minHeight = 120f;

            TextMeshProUGUI text = CreateText(rect, "Label", font, fontSize, Color.white,
                TextAlignmentOptions.Center, label);
            text.raycastTarget = false;

            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        private static void SetReference(SerializedObject serializedObject, string fieldName,
            UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                throw new BuildFailedException(
                    $"Field '{fieldName}' tidak ditemukan pada {serializedObject.targetObject.GetType().Name}. " +
                    "Periksa nama field pada script runtime.");
            }

            property.objectReferenceValue = value;
        }
    }
}

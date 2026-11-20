#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FaultLine.EditorTools
{
    /// <summary>One-click project setup and Android builds. Menu: Fault Line ▸ …</summary>
    public static class FaultLineSetup
    {
        const string ScenePath = "Assets/FaultLine/Main.unity";

        [MenuItem("Fault Line/1 - Set up project for Android", priority = 1)]
        public static void Setup()
        {
            // --- identity ---
            PlayerSettings.companyName = "Sooqista Studio";
            PlayerSettings.productName = "Fault Line";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, Config.BundleId);
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1" || PlayerSettings.bundleVersion == "0.1.0") PlayerSettings.bundleVersion = "1.0.0";
            if (PlayerSettings.Android.bundleVersionCode < 1) PlayerSettings.Android.bundleVersionCode = 1;
            // --- Android requirements for Google Play ---
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SplashScreen.backgroundColor = new Color32(9, 16, 18, 255);
            try { PlayerSettings.SplashScreen.show = false; } catch { }
            // --- icon ---
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/FaultLine/Resources/tex/icon.png");
            if (icon != null) { try { PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any); } catch (Exception e) { Debug.LogWarning("Icon: " + e.Message); } }
            // --- input: allow both input backends (UI Toolkit touch + legacy) ---
            try
            {
                var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();
                if (ps != null) { var so = new SerializedObject(ps); var p = so.FindProperty("activeInputHandler"); if (p != null && p.intValue != 2) { p.intValue = 2; so.ApplyModifiedProperties(); Debug.Log("Fault Line: Active Input Handling set to 'Both' (Unity may ask to restart)."); } }
            }
            catch (Exception e) { Debug.LogWarning("Input handling: " + e.Message); }
            // --- import settings ---
            FixTextures("Assets/FaultLine/Resources/pics", 1024);
            FixTextures("Assets/FaultLine/Resources/home", 1024);
            FixTextures("Assets/FaultLine/Resources/tex", 2048);
            FixTextures("Assets/FaultLine/Resources/icons", 256);
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/FaultLine/Resources/sfx" }))
            {
                var ai = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as AudioImporter; if (ai == null) continue;
                var s = ai.defaultSampleSettings; s.loadType = AudioClipLoadType.DecompressOnLoad; ai.defaultSampleSettings = s; ai.SaveAndReimport();
            }
            // --- Google AdMob: enable real ads automatically when the Google Mobile Ads plugin is installed ---
            bool hasAdMob = AppDomain.CurrentDomain.GetAssemblies().Any(a => { try { return a.GetType("GoogleMobileAds.Api.MobileAds") != null; } catch { return false; } });
            var defs = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
            var list = defs.Split(';').Where(x => x.Length > 0).ToList();
            list.Remove("FAULTLINE_MAX"); // legacy AppLovin MAX define
            if (hasAdMob && !list.Contains("FAULTLINE_ADMOB")) list.Add("FAULTLINE_ADMOB");
            if (!hasAdMob) list.Remove("FAULTLINE_ADMOB");
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, string.Join(";", list));
            // --- scene ---
            if (!File.Exists(ScenePath))
            {
                var scn = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scn, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Fault Line", "Project set up for Android.\n\nAds: " + (hasAdMob ? "Google AdMob detected — real ads enabled on device (set the App ID in Assets ▸ Google Mobile Ads ▸ Settings)." : "mock ads only (install the Google Mobile Ads plugin, then run this again).") +
                "\n\nIMPORTANT: Edit ▸ Project Settings ▸ UI Toolkit ▸ tick 'Enable Advanced Text Generator' (needed for Arabic letters to join).", "OK");
        }
        static void FixTextures(string folder, int max)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid); var ti = AssetImporter.GetAtPath(path) as TextureImporter; if (ti == null) continue;
                bool dirty = false;
                if (ti.mipmapEnabled) { ti.mipmapEnabled = false; dirty = true; }
                if (ti.maxTextureSize != max) { ti.maxTextureSize = max; dirty = true; }
                if (ti.npotScale != TextureImporterNPOTScale.None) { ti.npotScale = TextureImporterNPOTScale.None; dirty = true; }
                if (ti.wrapMode != TextureWrapMode.Clamp) { ti.wrapMode = TextureWrapMode.Clamp; dirty = true; }
                if (!ti.alphaIsTransparency && path.EndsWith(".png")) { ti.alphaIsTransparency = true; dirty = true; }
                if (dirty) ti.SaveAndReimport();
            }
        }

        [MenuItem("Fault Line/2 - Build test APK (install on your phone)", priority = 20)]
        public static void BuildApk() { Build(false); }
        [MenuItem("Fault Line/3 - Build Google Play bundle (.aab)", priority = 21)]
        public static void BuildAab() { Build(true); }

        static void Build(bool aab)
        {
            if (aab && !PlayerSettings.Android.useCustomKeystore)
            {
                EditorUtility.DisplayDialog("Fault Line", "Google Play needs a signed bundle.\n\nCreate your key first: Edit ▸ Project Settings ▸ Player ▸ Android ▸ Publishing Settings ▸ Keystore Manager ▸ Create New. Then build again.", "OK");
                return;
            }
            Directory.CreateDirectory("Builds");
            EditorUserBuildSettings.buildAppBundle = aab;
            var opts = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = aab ? "Builds/FaultLine.aab" : "Builds/FaultLine.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            if (opts.scenes.Length == 0) { Setup(); opts.scenes = new[] { ScenePath }; }
            var rep = BuildPipeline.BuildPlayer(opts);
            if (rep.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(opts.locationPathName);
                EditorUtility.DisplayDialog("Fault Line", "Built: " + opts.locationPathName + "\n(" + (rep.summary.totalSize / (1024 * 1024)) + " MB)", "OK");
            }
            else EditorUtility.DisplayDialog("Fault Line", "Build failed — open the Console window (Window ▸ General ▸ Console) and send the first red error to Claude.", "OK");
        }

        [MenuItem("Fault Line/Bump version (before each Play upload)", priority = 40)]
        public static void Bump()
        {
            PlayerSettings.Android.bundleVersionCode += 1;
            var v = PlayerSettings.bundleVersion.Split('.'); int patch = 0; if (v.Length == 3 && int.TryParse(v[2], out patch)) PlayerSettings.bundleVersion = v[0] + "." + v[1] + "." + (patch + 1);
            EditorUtility.DisplayDialog("Fault Line", "Version " + PlayerSettings.bundleVersion + " (code " + PlayerSettings.Android.bundleVersionCode + ")", "OK");
        }
    }
}
#endif

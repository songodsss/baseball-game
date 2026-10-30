using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BaseballGame.EditorTools
{
    /// <summary>
    /// Menu items to set up and build the Android version: Baseball > 안드로이드 설정 적용 / APK 빌드.
    /// Settings live here rather than in hand-edited ProjectSettings so they're easy to read and change.
    /// </summary>
    public static class AndroidBuild
    {
        const string PackageName = "com.songodsss.baseballgame";
        const string ProductName = "프로야구 선수 키우기"; // working title
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string ApkPath = "Builds/Android/baseball.apk";

        [MenuItem("Baseball/안드로이드 설정 적용")]
        public static void ApplySettings()
        {
            EnsureScene();

            PlayerSettings.companyName = "songodsss";
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "1.0")
                PlayerSettings.bundleVersion = "0.1.0";

            // The app icon, once Art/App/app_icon.png has been drawn.
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Art/App/app_icon.png");
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            // Portrait only: every screen is laid out for 1080x1920.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Google Play needs 64-bit builds, which need IL2CPP.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;

            AssetDatabase.SaveAssets();
            Debug.Log($"Android settings applied: {PackageName}, portrait, IL2CPP ARM64, scene {ScenePath}.");
        }

        [MenuItem("Baseball/안드로이드 APK 빌드")]
        public static void BuildApk()
        {
            ApplySettings();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
            EditorUserBuildSettings.buildAppBundle = false; // an APK installs straight onto a test phone
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"APK built: {Path.GetFullPath(ApkPath)}");
                EditorUtility.RevealInFinder(ApkPath);
            }
            else
            {
                Debug.LogError($"Android build failed: {report.summary.result}. See the Console for details.");
            }
        }

        /// <summary>
        /// The game builds all its screens from code at startup, so it only needs one plain scene,
        /// but a build needs that scene to exist and be listed.
        /// </summary>
        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildScript
{
    public static void Quest() => Run("quest", ApplyQuest, "apk");
    public static void Phone() => Run("phone", ApplyPhone, "aab");

    // ---------- variants ----------

    private static void ApplyCommon()
    {
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        PlayerSettings.bundleVersion = Env("APP_VERSION", "0.1.0");
        PlayerSettings.Android.bundleVersionCode = int.Parse(Env("BUILD_NUMBER", "1"));
        ConfigureKeystore();
    }

    private static void ApplyQuest()
    {
        ApplyCommon();
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.yourco.game.quest");
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)32;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)32;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        EditorUserBuildSettings.buildAppBundle = false;          // APK for sideload / Quest store
        SetDefines("QUEST_BUILD;XR_BUILD");
        XrConfig.SetOpenXrEnabled(true);
    }

    private static void ApplyPhone()
    {
        ApplyCommon();
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.yourco.game");
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
            new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
        EditorUserBuildSettings.buildAppBundle = true;           // AAB for Play Store
        SetDefines("PHONE_BUILD");
        XrConfig.SetOpenXrEnabled(false);
    }


    private static void Run(string variant, Action apply, string ext)
    {
        try
        {
            apply();
            AssetDatabase.SaveAssets();

            string outDir = Path.Combine(Env("OUTPUT_ROOT", "Builds"), variant);
            Directory.CreateDirectory(outDir);
            string file = Path.Combine(outDir, $"Game-{variant}-{Env("BUILD_NUMBER", "0")}.{ext}");

            var opts = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled)
                                                   .Select(s => s.path).ToArray(),
                locationPathName = file,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = Env("RELEASE", "false").ToLower() == "true"
                    ? BuildOptions.None
                    : BuildOptions.Development | BuildOptions.AllowDebugging
            };

            var s = BuildPipeline.BuildPlayer(opts).summary;
            Debug.Log($"[{variant}] {s.result} | {s.totalErrors} errors | " +
                      $"{s.totalSize / 1048576} MB | {s.totalTime} | {file}");
            EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{variant}] build threw: {e}");
            EditorApplication.Exit(2);
        }
    }

    private static void ConfigureKeystore()
    {
        string ks = Env("KEYSTORE_PATH", null);
        if (string.IsNullOrEmpty(ks)) { PlayerSettings.Android.useCustomKeystore = false; return; }
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = ks;
        PlayerSettings.Android.keystorePass = Env("KEYSTORE_PASS", "");
        PlayerSettings.Android.keyaliasName = Env("KEY_ALIAS", "");
        PlayerSettings.Android.keyaliasPass = Env("KEY_ALIAS_PASS", "");
    }

    private static void SetDefines(string defines) =>
        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, defines);

    private static string Env(string k, string fallback) =>
        Environment.GetEnvironmentVariable(k) ?? fallback;
}
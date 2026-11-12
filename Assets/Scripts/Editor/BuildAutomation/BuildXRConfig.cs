using System;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.Management;

public static class XrConfig
{
    private const string OpenXrLoader = "UnityEngine.XR.OpenXR.OpenXRLoader";

    public static void SetOpenXrEnabled(bool enabled)
    {
        EditorBuildSettings.TryGetConfigObject(
            XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget);
        if (perTarget == null)
            throw new Exception("XR Management settings missing. Open Project Settings > XR Plug-in Management once, then commit ProjectSettings/.");

        if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
            perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

        var manager = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android).Manager;

        bool ok = enabled
            ? XRPackageMetadataStore.AssignLoader(manager, OpenXrLoader, BuildTargetGroup.Android)
            : XRPackageMetadataStore.RemoveLoader(manager, OpenXrLoader, BuildTargetGroup.Android);
        if (!ok) throw new Exception($"Failed to {(enabled ? "assign" : "remove")} OpenXR loader.");

        manager.automaticLoading = enabled;
        EditorUtility.SetDirty(manager);
        AssetDatabase.SaveAssets();
    }
}
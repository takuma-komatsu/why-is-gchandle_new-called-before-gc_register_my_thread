using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class BuildButtonHook
{
    static BuildButtonHook()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);
    }

    private static void OnBuildPlayer(BuildPlayerOptions options)
    {
        // Build the AssetBundles.
        EditorMenu.BuildAssetBundles();

        var useLocalIl2Cpp = false;
        try
        {
            // Set the environment variables to use my customized IL2CPP.
            const string localIl2CppPath = "il2cpp";
            if (Directory.Exists(localIl2CppPath))
            {
                useLocalIl2Cpp = true;
                Environment.SetEnvironmentVariable("UNITY_IL2CPP_PATH", localIl2CppPath);
            }

            // Build the player.
            Debug.Log("Building Player...");
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
        }
        finally
        {
            if (useLocalIl2Cpp)
            {
                // Reset the IL2CPP folder.
                Environment.SetEnvironmentVariable("UNITY_IL2CPP_PATH", null);
            }
        }
    }
}

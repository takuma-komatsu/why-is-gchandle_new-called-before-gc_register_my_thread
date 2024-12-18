using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class BuildButtonHook
{
    private const string LocalIl2CppPath = "Library/il2cpp";
    private const string Il2CppPatchPath = "il2cpp-patch";

    static BuildButtonHook()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);
    }

    private static void OnBuildPlayer(BuildPlayerOptions options)
    {
        // Build the AssetBundles.
        EditorMenu.BuildAssetBundles();

        try
        {
            // Install IL2CPP locally if necessary.
            if (!Directory.Exists(LocalIl2CppPath))
            {
                InstallLocalIl2Cpp();
            }

            // Set the environment variables to use my customized IL2CPP.
            var localIl2CppFullPath = Path.Combine(Directory.GetCurrentDirectory(), LocalIl2CppPath);
            Environment.SetEnvironmentVariable("UNITY_IL2CPP_PATH", localIl2CppFullPath);
            Debug.Log("UNITY_IL2CPP_PATH = " + localIl2CppFullPath);

            // Build the player.
            Debug.Log("Building Player...");
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
        }
        finally
        {
            // Reset the IL2CPP folder.
            Environment.SetEnvironmentVariable("UNITY_IL2CPP_PATH", null);
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        foreach (var filePath in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(filePath);
            var destFilePath = Path.Combine(destinationDir, fileName);
            File.Copy(filePath, destFilePath, true);
        }

        foreach (var dirPath in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dirPath);
            var destDirPath = Path.Combine(destinationDir, dirName);
            CopyDirectory(dirPath, destDirPath);
        }
    }

    [MenuItem("Tools/[Re]Install Local IL2CPP")]
    public static void InstallLocalIl2Cpp()
    {
        DeleteLocalIl2Cpp();

        // Copy the entire IL2CPP locally.
        var il2CppPath = Path.Combine(EditorApplication.applicationContentsPath, "il2cpp");
        Debug.Log("Copying IL2CPP from " + il2CppPath + " to " + LocalIl2CppPath);
        CopyDirectory(il2CppPath, LocalIl2CppPath);

        // Copy the executables to the path referenced when UNITY_IL2CPP_PATH is specified.
        // https://github.com/Unity-Technologies/UnityCsReference/blob/2022.3/Editor/Mono/BuildPipeline/Il2Cpp/IL2CPPUtils.cs#L568
        // * It's a pain to select the files you need, so I just copy them all (they're not that big...)
        var binPath = Path.Combine(LocalIl2CppPath, "build/deploy");
        CopyDirectory(binPath, Path.Combine(LocalIl2CppPath, "il2cpp/bin/build/deploy"));
        CopyDirectory(binPath, Path.Combine(LocalIl2CppPath, "UnityLinker/bin/build/deploy"));
        CopyDirectory(binPath, Path.Combine(LocalIl2CppPath, "Analytics/bin/build/deploy"));

        // Apply the patch that adds the verification code.
        Debug.Log("Applying IL2CPP patch...");
        CopyDirectory(Il2CppPatchPath, LocalIl2CppPath);
    }

    [MenuItem("Tools/Delete Local IL2CPP")]
    public static void DeleteLocalIl2Cpp()
    {
        if (Directory.Exists(LocalIl2CppPath))
        {
            Directory.Delete(LocalIl2CppPath, true);
            Debug.Log("Deleted " + LocalIl2CppPath);
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static class EditorMenu
{
    private const string TextureNamePrefix = "Texture_";
    private const int GeneratedTextureCount = 32;

    /// <summary>
    /// Generate 32 randomly colored 128x128 textures.
    /// </summary>
    [MenuItem("Tools/Generate Textures")]
    public static void GenerateTextures()
    {
        const int texSize = 128;
        var tex = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);
        var pixels = new Color[texSize * texSize];
        for (var n = 0; n < GeneratedTextureCount; n++)
        {
            var fillColor = new Color(Random.value, Random.value, Random.value);
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = fillColor;
            }

            tex.SetPixels(pixels);
            tex.Apply();

            // Save as png.
            File.WriteAllBytes($"Assets/Data/{TextureNamePrefix}{n:D2}.png", tex.EncodeToPNG());
        }

        Object.DestroyImmediate(tex);
        AssetDatabase.Refresh();

        // Configure the AssetImporter to build each texture as a separate AssetBundle.
        for (var i = 0; i < GeneratedTextureCount; i++)
        {
            var importer = AssetImporter.GetAtPath($"Assets/Data/{TextureNamePrefix}{i:D2}.png") as TextureImporter;
            if (importer != null)
            {
                importer.assetBundleName = $"{i:D2}.bundle";
                importer.SaveAndReimport();
            }
        }
    }

    /// <summary>
    /// Build AssetBundles for the active build target.
    /// </summary>
    [MenuItem("Tools/Build AssetBundles")]
    public static void BuildAssetBundles()
    {
        const string assetBundleDirectory = "Assets/StreamingAssets/AssetBundles";
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }

        Debug.Log("Building AssetBundles...");
        BuildPipeline.BuildAssetBundles(
            assetBundleDirectory,
            BuildAssetBundleOptions.ChunkBasedCompression,
            EditorUserBuildSettings.activeBuildTarget);
        AssetDatabase.Refresh();
    }
}

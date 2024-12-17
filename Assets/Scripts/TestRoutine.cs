using System;
using System.Collections;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class TestRoutine : MonoBehaviour
{
    [SerializeField] private RawImage _rawImage;

    private IEnumerator Start()
    {
#if UNITY_EDITOR
        if (!File.Exists("Assets/StreamingAssets/AssetBundles/00.bundle"))
        {
            Debug.LogError("Please build the AssetBundles first. (Tools/Build AssetBundles)");
            yield break;
        }
#endif

        var streamingAssetsUri = Application.streamingAssetsPath;
        if (!streamingAssetsUri.Contains("://"))
        {
            streamingAssetsUri = $"file://{streamingAssetsUri}";
        }

        // It keeps 32 AssetBundles loaded simultaneously.
        const int textureCount = 32;
        var requests = new UnityWebRequest[textureCount];
        while (true)
        {
            for (var i = 0; i < requests.Length; i++)
            {
                if (requests[i] == null)
                {
                    StartCoroutine(Coroutine(i));
                }
            }

            yield return null;
        }

        IEnumerator Coroutine(int index)
        {
            // Load the AssetBundle file into memory as is.
            var uri = $"{streamingAssetsUri}/AssetBundles/{index:D2}.bundle";
            Debug.Log("UnityWebRequest.Get: " + uri);
            requests[index] = UnityWebRequest.Get(uri);
            yield return requests[index].SendWebRequest();

            if (requests[index].result == UnityWebRequest.Result.Success)
            {
                // Load an AssetBundle from memory using a custom implemented stream.
                var stream = new InMemoryStream(requests[index].downloadHandler.data);
                var assetBundleRequest = AssetBundle.LoadFromStreamAsync(stream);
                yield return assetBundleRequest;

                // Load a texture from the AssetBundle.
                var assetBundle = assetBundleRequest.assetBundle;
                var assetName = assetBundle.GetAllAssetNames()[0];
                var textureRequest = assetBundle.LoadAssetAsync<Texture2D>(assetName);
                yield return textureRequest;

                // Replace the displayed texture.
                // * This is to check if the AssetBundle is loaded correctly.
                var texture = textureRequest.asset as Texture2D;
                _rawImage.texture = texture;

                // Wait for a texture to be replaced by another coroutine.
                while (_rawImage.texture == texture)
                {
                    yield return null;
                }

                // Unload the AssetBundle.
                assetBundle.Unload(true);
            }
            else
            {
                Debug.LogError(requests[index].error);
            }

            requests[index].Dispose();
            requests[index] = null;
        }

        // ReSharper disable once IteratorNeverReturns
    }

    /// <summary>
    /// A Stream to read from memory.
    /// </summary>
    private class InMemoryStream : Stream
    {
        private readonly byte[] _buffer;
        private readonly int _randomSleepMilliseconds;

        public InMemoryStream(byte[] buffer)
        {
            _buffer = buffer;

            // UnityEngine.Random can only be used from the main thread...
            _randomSleepMilliseconds = Random.Range(100, 500);
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _buffer.Length;
        public override long Position { get; set; }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // Sleep randomly to simulate big file loading
            Thread.Sleep(_randomSleepMilliseconds);

            var copySize = Mathf.Min(count, _buffer.Length - (int)Position);
            Array.Copy(_buffer, Position, buffer, offset, copySize);
            Position += copySize;
            return copySize;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = _buffer.Length + offset;
                    break;
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}

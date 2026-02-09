using System;
using System.IO;
using UnityEngine;
using Unity.Collections;
using ExposedBindings;

public class TestTextureEncoding : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool runTestsOnStart = true;
    [SerializeField] private bool saveToDisk = false;
    [SerializeField] private string saveDirectory = "TestOutput";

    void Start()
    {
        if (runTestsOnStart)
        {
            RunEncodingTests();
        }
    }

    [ContextMenu("Run Encoding Tests")]
    public void RunEncodingTests()
    {
        Debug.Log("[Texture Encoding Test] Starting encoding tests...");

        // Create a test texture with a gradient
        Texture2D testTexture = CreateTestTexture();

        // Test PNG encoding
        TestPNGEncoding(testTexture);

        // Test JPG encoding  
        TestJPGEncoding(testTexture);

        // Test TGA encoding
        TestTGAEncoding(testTexture);

        // Test EXR encoding
        TestEXREncoding(testTexture);

        // Cleanup
        DestroyImmediate(testTexture);

        Debug.Log("[Texture Encoding Test] All tests completed!");
    }

    private Texture2D CreateTestTexture()
    {
        int width = 256;
        int height = 256;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // Create a gradient pattern
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float r = (float)x / width;
                float g = (float)y / height;
                float b = 1f - ((r + g) * 0.5f);
                texture.SetPixel(x, y, new Color(r, g, b, 1f));
            }
        }

        texture.Apply();
        Debug.Log($"[Texture Encoding Test] Created test texture: {width}x{height}");
        return texture;
    }

    private void TestPNGEncoding(Texture2D texture)
    {
        try
        {
            Debug.Log("[Texture Encoding Test] Testing PNG encoding...");

            // Test NativeArray encoding
            using (NativeArray<byte> pngData = texture.EncodeToPNGNative(Allocator.Temp))
            {
                if (pngData.IsCreated && pngData.Length > 0)
                {
                    Debug.Log($"[Texture Encoding Test] PNG encoding SUCCESS - Size: {pngData.Length} bytes");
                    
                    if (saveToDisk)
                    {
                        SaveToFile(pngData, "test_native.png");
                    }

                    // Verify we can load it back
                    Texture2D verifyTexture = new Texture2D(2, 2);
                    if (verifyTexture.LoadImageSpan(pngData.AsReadOnlySpan()))
                    {
                        Debug.Log($"[Texture Encoding Test] PNG verification SUCCESS - Loaded: {verifyTexture.width}x{verifyTexture.height}");
                    }
                    else
                    {
                        Debug.LogError("[Texture Encoding Test] PNG verification FAILED");
                    }
                    DestroyImmediate(verifyTexture);
                }
                else
                {
                    Debug.LogError("[Texture Encoding Test] PNG encoding FAILED - No data returned");
                }
            }

            // Test Span encoding
            Span<byte> pngSpan = texture.EncodeToPNGSpan();
            if (pngSpan.Length > 0)
            {
                Debug.Log($"[Texture Encoding Test] PNG Span encoding SUCCESS - Size: {pngSpan.Length} bytes");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Texture Encoding Test] PNG encoding exception: {e.Message}");
        }
    }

    private void TestJPGEncoding(Texture2D texture)
    {
        try
        {
            Debug.Log("[Texture Encoding Test] Testing JPG encoding...");

            // Test with different quality levels
            int[] qualities = { 25, 75, 100 };
            
            foreach (int quality in qualities)
            {
                using (NativeArray<byte> jpgData = texture.EncodeToJPGNative(quality, Allocator.Temp))
                {
                    if (jpgData.IsCreated && jpgData.Length > 0)
                    {
                        Debug.Log($"[Texture Encoding Test] JPG encoding SUCCESS (quality={quality}) - Size: {jpgData.Length} bytes");
                        
                        if (saveToDisk && quality == 75)
                        {
                            SaveToFile(jpgData, "test_native.jpg");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[Texture Encoding Test] JPG encoding FAILED (quality={quality})");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Texture Encoding Test] JPG encoding exception: {e.Message}");
        }
    }

    private void TestTGAEncoding(Texture2D texture)
    {
        try
        {
            Debug.Log("[Texture Encoding Test] Testing TGA encoding...");

            using (NativeArray<byte> tgaData = texture.EncodeToTGANative(Allocator.Temp))
            {
                if (tgaData.IsCreated && tgaData.Length > 0)
                {
                    Debug.Log($"[Texture Encoding Test] TGA encoding SUCCESS - Size: {tgaData.Length} bytes");
                    
                    if (saveToDisk)
                    {
                        SaveToFile(tgaData, "test_native.tga");
                    }
                }
                else
                {
                    Debug.LogError("[Texture Encoding Test] TGA encoding FAILED");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Texture Encoding Test] TGA encoding exception: {e.Message}");
        }
    }

    private void TestEXREncoding(Texture2D texture)
    {
        try
        {
            Debug.Log("[Texture Encoding Test] Testing EXR encoding...");

            // Create HDR texture for EXR
            Texture2D hdrTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBAFloat, false);
            hdrTexture.SetPixels(texture.GetPixels());
            hdrTexture.Apply();

            using (NativeArray<byte> exrData = hdrTexture.EncodeToEXRNative(Texture2D.EXRFlags.None, Allocator.Temp))
            {
                if (exrData.IsCreated && exrData.Length > 0)
                {
                    Debug.Log($"[Texture Encoding Test] EXR encoding SUCCESS - Size: {exrData.Length} bytes");
                    
                    if (saveToDisk)
                    {
                        SaveToFile(exrData, "test_native.exr");
                    }
                }
                else
                {
                    Debug.LogError("[Texture Encoding Test] EXR encoding FAILED");
                }
            }

            DestroyImmediate(hdrTexture);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Texture Encoding Test] EXR encoding exception: {e.Message}");
        }
    }

    private void SaveToFile(NativeArray<byte> data, string filename)
    {
#if UNITY_EDITOR
        try
        {
            string path = Path.Combine(Application.dataPath, saveDirectory);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string filePath = Path.Combine(path, filename);
            
            // Convert NativeArray to byte array for file writing
            byte[] bytes = data.ToArray();
            File.WriteAllBytes(filePath, bytes);
            
            Debug.Log($"[Texture Encoding Test] Saved to: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Texture Encoding Test] Failed to save file: {e.Message}");
        }
#endif
    }
}
using System;
using System.Collections;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;
using ExposedBindings;

public class TestUnityInternalBindings : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool runTestsOnStart = true;
    [SerializeField] private string testImageUrl = "";
    [SerializeField] private string testAssetBundleUrl = ""; // Set this to a valid AssetBundle URL for testing
    
    [Header("Test Results")]
    [SerializeField] private bool imageTestPassed;
    [SerializeField] private bool assetBundleTestPassed;
    [SerializeField] private string lastError;
    
    private Texture2D testTexture;
    private AssetBundle loadedBundle;

    void Start()
    {
        // Check Unity version compatibility
        var compatibility = UnityVersionChecker.GetCompatibility();
        Debug.Log($"[ExposedBindings Test] Version Check: {compatibility.Message}");
        Debug.Log($"[ExposedBindings Test] Current: {compatibility.CurrentVersion}, Target: {compatibility.TargetVersion}");
        
        if (!compatibility.IsCompatible)
        {
            Debug.LogError("[ExposedBindings Test] Unity version is not compatible!");
            return;
        }

        if (runTestsOnStart)
        {
            StartCoroutine(RunAllTests());
        }
    }

    IEnumerator RunAllTests()
    {
        Debug.Log("[ExposedBindings Test] Starting tests...");
        
        // Test 1: LoadImage with Span
        yield return TestLoadImageWithSpan();
        
        // Test 2: LoadImage with NativeArray
        yield return TestLoadImageWithNativeArray();
        
        // Test 3: AssetBundle loading (if URL is provided)
        if (!string.IsNullOrEmpty(testAssetBundleUrl))
        {
            yield return TestAssetBundleLoading();
        }
        
        // Report results
        ReportTestResults();
    }

    IEnumerator TestLoadImageWithSpan()
    {
        Debug.Log("[ExposedBindings Test] Testing LoadImage with Span...");
        
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(testImageUrl))
        {
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                lastError = $"Failed to download test image: {request.error}";
                Debug.LogError($"[ExposedBindings Test] {lastError}");
                yield break;
            }
            
            // Create texture
            testTexture = new Texture2D(2, 2);
            
            // Test with Span
            try
            {
                bool success = testTexture.LoadImageSpan(request.downloadHandler.nativeData, false);
                
                if (success)
                {
                    Debug.Log($"[ExposedBindings Test] LoadImageSpan SUCCESS - Texture size: {testTexture.width}x{testTexture.height}");
                    imageTestPassed = true;
                }
                else
                {
                    lastError = "LoadImageSpan returned false";
                    Debug.LogError($"[ExposedBindings Test] {lastError}");
                }
            }
            catch (Exception e)
            {
                lastError = $"LoadImageSpan exception: {e.Message}";
                Debug.LogError($"[ExposedBindings Test] {lastError}");
            }
        }
    }

    IEnumerator TestLoadImageWithNativeArray()
    {
        Debug.Log("[ExposedBindings Test] Testing LoadImage with NativeArray...");
        
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(testImageUrl))
        {
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                lastError = $"Failed to download test image: {request.error}";
                Debug.LogError($"[ExposedBindings Test] {lastError}");
                yield break;
            }
            
            try
            {
                // Create a temporary NativeArray for testing LoadImageNative (Allocator.Temp auto-disposes at end of frame)
                NativeArray<byte> nativeData = new NativeArray<byte>(request.downloadHandler.nativeData.Length, Allocator.Temp);
                
                // Copy data from ReadOnly to writable NativeArray (zero-allocation native memory copy)
                request.downloadHandler.nativeData.CopyTo(nativeData);
                
                // Create new texture for this test
                var nativeTexture = new Texture2D(2, 2);
                
                bool success = nativeTexture.LoadImageNative(nativeData, false);
                
                if (success)
                {
                    Debug.Log($"[ExposedBindings Test] LoadImageNative SUCCESS - Texture size: {nativeTexture.width}x{nativeTexture.height}");
                    imageTestPassed = true;
                    
                    // Clean up texture
                    Destroy(nativeTexture);
                }
                else
                {
                    lastError = "LoadImageNative returned false";
                    Debug.LogError($"[ExposedBindings Test] {lastError}");
                }
                
                // No need to dispose Allocator.Temp - Unity handles it automatically
            }
            catch (Exception e)
            {
                lastError = $"LoadImageNative exception: {e.Message}";
                Debug.LogError($"[ExposedBindings Test] {lastError}");
            }
        }
    }

    IEnumerator TestAssetBundleLoading()
    {
        Debug.Log("[ExposedBindings Test] Testing AssetBundle loading with Span...");
        
        using (UnityWebRequest request = UnityWebRequest.Get(testAssetBundleUrl))
        {
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                lastError = $"Failed to download test AssetBundle: {request.error}";
                Debug.LogError($"[ExposedBindings Test] {lastError}");
                yield break;
            }
            
            
            // Test async loading with Span
            AssetBundleCreateRequest createRequest = null;
            try
            {
                createRequest = AssetBundleExtensions.LoadFromMemoryAsyncSpan(request.downloadHandler.nativeData);
                
                if (createRequest == null)
                {
                    lastError = "LoadFromMemoryAsyncSpan returned null";
                    Debug.LogError($"[ExposedBindings Test] {lastError}");
                }
            }
            catch (Exception e)
            {
                lastError = $"LoadFromMemoryAsyncSpan exception: {e.Message}";
                Debug.LogError($"[ExposedBindings Test] {lastError}");
            }
            
            // Yield outside of try-catch
            if (createRequest != null)
            {
                yield return createRequest;
                
                loadedBundle = createRequest.assetBundle;
                if (loadedBundle != null)
                {
                    Debug.Log($"[ExposedBindings Test] LoadFromMemoryAsyncSpan SUCCESS - Bundle: {loadedBundle.name}");
                    assetBundleTestPassed = true;
                    
                    // Check if this is a scene bundle
                    string[] scenePaths = loadedBundle.GetAllScenePaths();
                    
                    if (scenePaths.Length > 0)
                    {
                        // This is a scene bundle
                        Debug.Log($"[ExposedBindings Test] Bundle contains {scenePaths.Length} scene(s)");
                        
                        foreach (string scenePath in scenePaths)
                        {
                            Debug.Log($"[ExposedBindings Test]   - Scene: {scenePath}");
                        }
                        
                        Debug.Log($"[ExposedBindings Test] This is a streamed scene bundle - cannot list individual assets");
                        Debug.Log($"[ExposedBindings Test] Scene bundle is ready to load with SceneManager.LoadSceneAsync()");
                    }
                    else
                    {
                        // This is a regular asset bundle
                        string[] assetNames = loadedBundle.GetAllAssetNames();
                        Debug.Log($"[ExposedBindings Test] Bundle contains {assetNames.Length} assets");
                        
                        if (assetNames.Length > 0)
                        {
                            // Get all objects/assets from the bundle
                            UnityEngine.Object[] allObjects = loadedBundle.LoadAllAssets();
                            Debug.Log($"[ExposedBindings Test] Loaded {allObjects.Length} objects");
                            
                            // Count object types
                            int gameObjectCount = 0;
                            int materialCount = 0;
                            int textureCount = 0;
                            int otherCount = 0;
                            
                            foreach (var obj in allObjects)
                            {
                                if (obj != null)
                                {
                                    if (obj is GameObject) gameObjectCount++;
                                    else if (obj is Material) materialCount++;
                                    else if (obj is Texture2D) textureCount++;
                                    else otherCount++;
                                    
                                    // Log first few objects as examples
                                    if (allObjects.Length <= 10)
                                    {
                                        Debug.Log($"[ExposedBindings Test]   - {obj.GetType().Name}: {obj.name}");
                                    }
                                }
                            }
                            
                            Debug.Log($"[ExposedBindings Test] Object types: GameObjects({gameObjectCount}), Materials({materialCount}), Textures({textureCount}), Other({otherCount})");
                        }
                    }
                }
                else
                {
                    lastError = "AssetBundle is null after loading";
                    Debug.LogError($"[ExposedBindings Test] {lastError}");
                }
            }
        }
    }

    void ReportTestResults()
    {
        Debug.Log("====================================");
        Debug.Log("[ExposedBindings Test] TEST RESULTS:");
        Debug.Log($"  Image Loading (Span): {(imageTestPassed ? "PASSED" : "FAILED")}");
        Debug.Log($"  AssetBundle Loading: {(assetBundleTestPassed ? "PASSED" : "FAILED/SKIPPED")}");
        
        if (!string.IsNullOrEmpty(lastError))
        {
            Debug.Log($"  Last Error: {lastError}");
        }
        
        Debug.Log("====================================");
        
        // Platform info for debugging
        Debug.Log($"[ExposedBindings Test] Platform: {Application.platform}");
        Debug.Log($"[ExposedBindings Test] Unity Version: {Application.unityVersion}");
        
#if ENABLE_IL2CPP
        Debug.Log("[ExposedBindings Test] IL2CPP: ENABLED");
#else
        Debug.Log("[ExposedBindings Test] IL2CPP: DISABLED (Mono)");
#endif
    }

    void OnDestroy()
    {
        if (testTexture != null)
            Destroy(testTexture);
            
        if (loadedBundle != null)
            loadedBundle.Unload(true);
    }

    [ContextMenu("Run Tests")]
    public void RunTestsManually()
    {
        StartCoroutine(RunAllTests());
    }
}

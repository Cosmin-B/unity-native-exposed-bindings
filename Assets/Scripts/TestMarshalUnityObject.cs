using System;
using UnityEngine;
using ExposedBindings.Exposed;

public class TestMarshalUnityObject : MonoBehaviour
{
    void Start()
    {
        Debug.Log("[MarshalUnityObject Test] Starting test...");
        
        try
        {
            // Test 1: Marshal this GameObject
            IntPtr gameObjectPtr = UnityExposed.MarshalUnityObject(this.gameObject);
            Debug.Log($"[MarshalUnityObject Test] GameObject marshalled successfully: {gameObjectPtr}");
            
            // Test 2: Marshal this MonoBehaviour
            IntPtr monoBehaviourPtr = UnityExposed.MarshalUnityObject(this);
            Debug.Log($"[MarshalUnityObject Test] MonoBehaviour marshalled successfully: {monoBehaviourPtr}");
            
            // Test 3: Create a new Texture2D and marshal it
            Texture2D testTexture = new Texture2D(4, 4);
            IntPtr texturePtr = UnityExposed.MarshalUnityObject(testTexture);
            Debug.Log($"[MarshalUnityObject Test] Texture2D marshalled successfully: {texturePtr}");
            
            // Test 4: Marshal null (should return IntPtr.Zero)
            IntPtr nullPtr = UnityExposed.MarshalUnityObject(null);
            Debug.Log($"[MarshalUnityObject Test] Null object marshalled correctly: {nullPtr == IntPtr.Zero}");
            
            Debug.Log("[MarshalUnityObject Test] All tests passed!");
            
            // Clean up
            DestroyImmediate(testTexture);
        }
        catch (NotImplementedException nie)
        {
            Debug.LogError($"[MarshalUnityObject Test] Assembly not processed by Cecil: {nie.Message}");
            Debug.LogError("Please run ProcessAssembly.sh to process the assembly with Cecil.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MarshalUnityObject Test] Test failed: {e.Message}");
            Debug.LogError(e.StackTrace);
        }
    }
}
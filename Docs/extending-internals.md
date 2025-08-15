# Extending Unity Exposed Bindings

## Table of Contents
1. [Introduction](#introduction)
2. [Finding Unity Internal Methods](#finding-unity-internal-methods)
3. [IL Injection Basics](#il-injection-basics)
4. [Step-by-Step Guide to Adding New Methods](#step-by-step-guide-to-adding-new-methods)
5. [Example: Exposing a New Unity Method](#example-exposing-a-new-unity-method)
6. [Common Patterns and Best Practices](#common-patterns-and-best-practices)
7. [Troubleshooting](#troubleshooting)
8. [Resources and References](#resources-and-references)

## Introduction

The Unity Exposed Bindings library provides access to Unity's internal methods that are not exposed in the public API. This is achieved through IL (Intermediate Language) injection using the Cecil library to modify compiled assemblies at build time.

### Purpose and Approach

Unity's engine contains many high-performance internal methods marked with `_Injected` suffix that bypass managed overhead by working directly with native pointers. These methods are used internally by Unity but not exposed to developers. This library:

1. **Creates stub methods** in managed code that match Unity's internal signatures
2. **Uses Cecil to replace** the stub method bodies with IL that calls Unity's internal methods
3. **Provides convenient extension methods** that wrap the exposed functionality

The approach allows for:
- **Zero-allocation** operations when working with spans and native arrays
- **Direct access** to Unity's optimized native implementations
- **Type safety** through proper C# wrapper methods
- **Cross-platform compatibility** with Unity's internal architecture

## Finding Unity Internal Methods

### Identifying `_Injected` Methods

Unity's internal methods typically follow these patterns:

1. **Naming Convention**: Methods ending with `_Injected` suffix
2. **Parameter Types**: Often use `ManagedSpanWrapper` for data spans
3. **Visibility**: Usually `internal` or `private` within Unity assemblies
4. **Location**: Found in Unity's module assemblies (e.g., `UnityEngine.CoreModule.dll`)

### Using ILSpy or dotPeek

To explore Unity's internal methods:

1. **Install ILSpy** (free) or **dotPeek** (JetBrains)
2. **Navigate to Unity installation**:
   - **macOS**: `/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/Managed/UnityEngine/`
   - **Windows**: `{Unity Installation}\Editor\Data\Managed\UnityEngine\`
3. **Open relevant DLL files**:
   - `UnityEngine.CoreModule.dll` - Core functionality, Object marshalling
   - `UnityEngine.AssetBundleModule.dll` - AssetBundle operations
   - `UnityEngine.ImageConversionModule.dll` - Texture loading/conversion

### Example: Finding ImageConversion Methods

```csharp
// In UnityEngine.ImageConversionModule.dll
namespace UnityEngine
{
    public static class ImageConversion
    {
        // Public API method
        public static bool LoadImage(Texture2D tex, byte[] data, bool markNonReadable)
        {
            // ... validation code ...
            return LoadImage_Injected(GetNativeTexturePtr(tex), 
                                    new ManagedSpanWrapper(data), 
                                    markNonReadable);
        }

        // Internal method we want to expose
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool LoadImage_Injected(IntPtr tex, 
                                                     ref ManagedSpanWrapper data, 
                                                     bool markNonReadable);
    }
}
```

### Understanding ManagedSpanWrapper Structure

Unity's `ManagedSpanWrapper` is a key structure for passing data spans:

```csharp
// Unity's internal structure
namespace UnityEngine.Bindings
{
    public struct ManagedSpanWrapper
    {
        public unsafe void* begin;
        public int length;
        
        public unsafe ManagedSpanWrapper(void* begin, int length)
        {
            this.begin = begin;
            this.length = length;
        }
    }
}
```

Our library defines a compatible wrapper:

```csharp
// Our compatible structure
namespace ExposedBindings.Internal
{
    public unsafe struct ManagedSpanWrapper
    {
        public void* begin;
        public int length;
        
        public ManagedSpanWrapper(void* begin, int length)
        {
            this.begin = begin;
            this.length = length;
        }
    }
}
```

## IL Injection Basics

### Brief Crash Course on IL (Intermediate Language)

IL is the bytecode that C# compiles to. Key concepts:

- **Stack-based**: Operations push/pop values to/from an evaluation stack
- **Typed**: Each stack slot has a specific type
- **Method calls**: Load arguments onto stack, call method, result pushed to stack

### Common IL Opcodes

| Opcode | Description | Example |
|--------|-------------|---------|
| `ldarg.0` | Load argument 0 (this for instance methods) | Load first parameter |
| `ldarg.1` | Load argument 1 | Load second parameter |
| `ldloc.0` | Load local variable 0 | Load first local variable |
| `stloc.0` | Store to local variable 0 | Store stack top to local |
| `ldfld` | Load field value | Load field from object |
| `ldloca.s` | Load local variable address | Get address of local |
| `call` | Call method | Invoke method call |
| `ret` | Return from method | End method execution |
| `brtrue.s` | Branch if true (short form) | Conditional jump |
| `ldc.i4.0` | Load constant integer 0 | Push 0 onto stack |
| `conv.i` | Convert to native int | Type conversion |

### How Cecil Manipulates IL Code

Cecil provides a high-level API for IL manipulation:

```csharp
// Get IL processor for method
var il = method.Body.GetILProcessor();

// Clear existing instructions
method.Body.Instructions.Clear();

// Add new instructions
il.Emit(OpCodes.Ldarg_0);           // Load first argument
il.Emit(OpCodes.Call, targetMethod); // Call target method
il.Emit(OpCodes.Ret);               // Return
```

### Learning Resources

- **ECMA-335 Standard**: Official IL specification
- **Cecil Documentation**: https://github.com/jbevain/cecil/wiki
- **IL Instruction Reference**: https://docs.microsoft.com/en-us/dotnet/api/system.reflection.emit.opcodes

## Step-by-Step Guide to Adding New Methods

### 1. Creating the Stub Method in UnityExposed.cs

First, add a stub method to the `UnityExposed` class:

```csharp
// In temp_manual/ExposedBindings/UnityExposed.cs
public static class UnityExposed
{
    /// <summary>
    /// Your new method description here
    /// </summary>
    public static ReturnType YourNewMethod(ParameterType param1, ParameterType param2)
    {
        // This will be replaced by Cecil to call Unity's internal method
        throw new NotImplementedException("Assembly not processed by Cecil. Run ProcessAssembly.sh");
    }
}
```

**Key Guidelines**:
- Method signature must **exactly match** Unity's internal method
- Use `ManagedSpanWrapper` for span parameters
- Include comprehensive XML documentation
- Always throw `NotImplementedException` in stub

### 2. Adding the Extension Method Wrapper

Create a user-friendly extension method:

```csharp
// In separate extension class file
public static class YourFeatureExtensions
{
    /// <summary>
    /// High-level description of what this method does
    /// </summary>
    public static ReturnType YourConvenientMethod(this UnityObjectType target, 
                                                 Span<DataType> data, 
                                                 bool additionalParam = false)
    {
        // Validation
        if (target == null)
        {
            Debug.LogError("Target object is null");
            return default;
        }

        if (data.Length == 0)
        {
            Debug.LogError("Data span is empty");
            return default;
        }

        // Get Unity object pointer
        IntPtr objectPtr = UnityExposed.MarshalUnityObject(target);
        if (objectPtr == IntPtr.Zero)
        {
            Debug.LogError("Failed to marshal Unity object");
            return default;
        }

        // Convert span to ManagedSpanWrapper
        unsafe
        {
            fixed (DataType* dataPtr = &data.GetPinnableReference())
            {
                ManagedSpanWrapper spanWrapper = new ManagedSpanWrapper(dataPtr, data.Length);
                return UnityExposed.YourNewMethod(objectPtr, ref spanWrapper, additionalParam);
            }
        }
    }
}
```

### 3. Modifying CecilProcessor/Program.cs

Add processing logic to replace your stub method:

```csharp
// In ProcessAssembly method, add a call to your processor
static void ProcessAssembly(string inputPath, string outputPath, ...)
{
    // ... existing code ...
    
    // Add your new method processing
    ReplaceYourNewMethod(assembly, exposedType, relevantUnityModule, unitySpanWrapper, ourSpanWrapper);
    
    // ... rest of existing code ...
}

// Add your method replacement logic
static void ReplaceYourNewMethod(AssemblyDefinition assembly, TypeDefinition exposedType,
    AssemblyDefinition unityModule, TypeDefinition unitySpanWrapper, TypeDefinition ourSpanWrapper)
{
    // Find our stub method
    var method = exposedType.Methods.FirstOrDefault(m => m.Name == "YourNewMethod");
    if (method == null)
    {
        Console.WriteLine("Warning: Could not find stub method YourNewMethod");
        return;
    }

    // Find Unity's internal method
    var unityType = unityModule.MainModule.GetType("UnityEngine.YourTargetType");
    var unityMethod = unityType?.Methods.FirstOrDefault(m => m.Name == "YourInternalMethod_Injected");
    
    if (unityMethod == null)
    {
        Console.Error.WriteLine("Could not find UnityEngine.YourTargetType.YourInternalMethod_Injected");
        return;
    }

    // Clear existing method body
    method.Body.Instructions.Clear();
    method.Body.Variables.Clear();
    method.Body.ExceptionHandlers.Clear();

    var il = method.Body.GetILProcessor();

    // Create local variable for Unity's ManagedSpanWrapper if needed
    if (method.Parameters.Any(p => p.ParameterType.Name.Contains("ManagedSpanWrapper")))
    {
        var localSpanWrapper = new VariableDefinition(assembly.MainModule.ImportReference(unitySpanWrapper));
        method.Body.Variables.Add(localSpanWrapper);

        // Convert our wrapper to Unity's wrapper
        il.Emit(OpCodes.Ldloca_S, localSpanWrapper);
        il.Emit(OpCodes.Ldarg_1); // Assuming second parameter is our wrapper
        il.Emit(OpCodes.Ldfld, ourSpanWrapper.Fields.First(f => f.Name == "begin"));
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldfld, ourSpanWrapper.Fields.First(f => f.Name == "length"));
        
        var unitySpanWrapperCtor = unitySpanWrapper.Methods.FirstOrDefault(m => m.IsConstructor);
        if (unitySpanWrapperCtor != null)
        {
            il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(unitySpanWrapperCtor));
        }

        // Call Unity's internal method with converted parameters
        il.Emit(OpCodes.Ldarg_0); // First parameter
        il.Emit(OpCodes.Ldloca_S, localSpanWrapper); // Converted wrapper
        il.Emit(OpCodes.Ldarg_2); // Third parameter if exists
        il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(unityMethod));
        il.Emit(OpCodes.Ret);
    }
    else
    {
        // Simple parameter forwarding
        for (int i = 0; i < method.Parameters.Count; i++)
        {
            il.Emit(OpCodes.Ldarg, i);
        }
        il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(unityMethod));
        il.Emit(OpCodes.Ret);
    }

    Console.WriteLine("Replaced YourNewMethod");
}
```

### 4. Testing the New Method

Create a test script:

```csharp
// In Assets/Scripts/TestYourNewMethod.cs
using UnityEngine;
using ExposedBindings;
using System;

public class TestYourNewMethod : MonoBehaviour
{
    void Start()
    {
        try
        {
            // Test your new method
            var testData = new byte[] { /* test data */ };
            var result = someUnityObject.YourConvenientMethod(testData.AsSpan());
            
            Debug.Log($"Method result: {result}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Test failed: {ex.Message}");
        }
    }
}
```

## Example: Exposing a New Unity Method

Let's walk through exposing Unity's `Texture2D.SetPixelDataImpl_Injected`:

### 1. Unity Internal Method Analysis

From ILSpy, we find in `UnityEngine.CoreModule.dll`:

```csharp
namespace UnityEngine
{
    public sealed class Texture2D : Texture
    {
        // Internal method we want to expose
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void SetPixelDataImpl_Injected(IntPtr tex, 
                                                            ref ManagedSpanWrapper data, 
                                                            int mipLevel, 
                                                            int elementSize, 
                                                            int dataSize);
    }
}
```

### 2. Stub Implementation

Add to `UnityExposed.cs`:

```csharp
/// <summary>
/// Sets pixel data for a texture using a managed span wrapper.
/// This bypasses managed array overhead for better performance.
/// </summary>
public static unsafe void Texture2D_SetPixelDataImpl_Injected(IntPtr tex, 
                                                             ref ManagedSpanWrapper data, 
                                                             int mipLevel, 
                                                             int elementSize, 
                                                             int dataSize)
{
    // This will be replaced by Cecil to call Unity's internal method
    throw new NotImplementedException("Assembly not processed by Cecil. Run ProcessAssembly.sh");
}
```

### 3. Extension Method Wrapper

Create `TextureExtensions.cs`:

```csharp
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using ExposedBindings.Internal;
using ExposedBindings.Exposed;

namespace ExposedBindings
{
    public static class TextureExtensions
    {
        /// <summary>
        /// Sets pixel data from a Span directly into a Texture2D.
        /// This method bypasses managed array overhead for better performance.
        /// </summary>
        /// <param name="texture">The Texture2D to set pixel data for</param>
        /// <param name="data">Span containing the pixel data</param>
        /// <param name="mipLevel">Mip level to set data for</param>
        /// <param name="elementSize">Size of each pixel element in bytes</param>
        public static unsafe void SetPixelDataSpan<T>(this Texture2D texture, 
                                                    Span<T> data, 
                                                    int mipLevel = 0, 
                                                    int elementSize = 0) where T : struct
        {
            if (texture == null)
            {
                Debug.LogError("SetPixelDataSpan: Texture2D is null");
                return;
            }

            if (data.Length == 0)
            {
                Debug.LogError("SetPixelDataSpan: Data span is empty");
                return;
            }

            // Calculate element size if not provided
            if (elementSize <= 0)
            {
                elementSize = UnsafeUtility.SizeOf<T>();
            }

            // Get Unity object pointer
            IntPtr texPtr = UnityExposed.MarshalUnityObject(texture);
            if (texPtr == IntPtr.Zero)
            {
                Debug.LogError("SetPixelDataSpan: Failed to marshal Unity object");
                return;
            }

            fixed (T* dataPtr = &data.GetPinnableReference())
            {
                ManagedSpanWrapper spanWrapper = new ManagedSpanWrapper(dataPtr, data.Length * elementSize);
                UnityExposed.Texture2D_SetPixelDataImpl_Injected(texPtr, ref spanWrapper, 
                                                               mipLevel, elementSize, 
                                                               data.Length * elementSize);
            }
        }

        /// <summary>
        /// Sets pixel data from a NativeArray directly into a Texture2D.
        /// </summary>
        public static unsafe void SetPixelDataNative<T>(this Texture2D texture, 
                                                       NativeArray<T> data, 
                                                       int mipLevel = 0, 
                                                       int elementSize = 0) where T : struct
        {
            if (texture == null)
            {
                Debug.LogError("SetPixelDataNative: Texture2D is null");
                return;
            }

            if (!data.IsCreated || data.Length == 0)
            {
                Debug.LogError("SetPixelDataNative: NativeArray is not created or empty");
                return;
            }

            // Calculate element size if not provided
            if (elementSize <= 0)
            {
                elementSize = UnsafeUtility.SizeOf<T>();
            }

            // Get Unity object pointer
            IntPtr texPtr = UnityExposed.MarshalUnityObject(texture);
            if (texPtr == IntPtr.Zero)
            {
                Debug.LogError("SetPixelDataNative: Failed to marshal Unity object");
                return;
            }

            void* dataPtr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);
            ManagedSpanWrapper spanWrapper = new ManagedSpanWrapper(dataPtr, data.Length * elementSize);
            
            UnityExposed.Texture2D_SetPixelDataImpl_Injected(texPtr, ref spanWrapper, 
                                                           mipLevel, elementSize, 
                                                           data.Length * elementSize);
        }
    }
}
```

### 4. Cecil Processing Code

Add to `CecilProcessor/Program.cs`:

```csharp
// In ProcessAssembly method
ReplaceTexture2DSetPixelDataMethod(assembly, exposedType, coreModule, unitySpanWrapper, ourSpanWrapper);

// Method implementation
static void ReplaceTexture2DSetPixelDataMethod(AssemblyDefinition assembly, TypeDefinition exposedType,
    AssemblyDefinition coreModule, TypeDefinition unitySpanWrapper, TypeDefinition ourSpanWrapper)
{
    // Find our stub method
    var method = exposedType.Methods.FirstOrDefault(m => m.Name == "Texture2D_SetPixelDataImpl_Injected");
    if (method == null)
    {
        Console.WriteLine("Warning: Could not find stub method Texture2D_SetPixelDataImpl_Injected");
        return;
    }

    // Find Unity's internal method
    var texture2DType = coreModule.MainModule.GetType("UnityEngine.Texture2D");
    var unityMethod = texture2DType?.Methods.FirstOrDefault(m => m.Name == "SetPixelDataImpl_Injected");
    
    if (unityMethod == null)
    {
        Console.Error.WriteLine("Could not find UnityEngine.Texture2D.SetPixelDataImpl_Injected");
        return;
    }

    // Clear existing method body
    method.Body.Instructions.Clear();
    method.Body.Variables.Clear();
    method.Body.ExceptionHandlers.Clear();

    var il = method.Body.GetILProcessor();

    // Create local variable for Unity's ManagedSpanWrapper
    var localSpanWrapper = new VariableDefinition(assembly.MainModule.ImportReference(unitySpanWrapper));
    method.Body.Variables.Add(localSpanWrapper);

    // Convert our wrapper to Unity's wrapper
    il.Emit(OpCodes.Ldloca_S, localSpanWrapper);
    il.Emit(OpCodes.Ldarg_1); // Load our ManagedSpanWrapper ref
    il.Emit(OpCodes.Ldfld, ourSpanWrapper.Fields.First(f => f.Name == "begin"));
    il.Emit(OpCodes.Ldarg_1);
    il.Emit(OpCodes.Ldfld, ourSpanWrapper.Fields.First(f => f.Name == "length"));
    
    var unitySpanWrapperCtor = unitySpanWrapper.Methods.FirstOrDefault(m => m.IsConstructor);
    if (unitySpanWrapperCtor != null)
    {
        il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(unitySpanWrapperCtor));
    }

    // Call Unity's internal method
    il.Emit(OpCodes.Ldarg_0); // IntPtr tex
    il.Emit(OpCodes.Ldloca_S, localSpanWrapper); // ref Unity's ManagedSpanWrapper
    il.Emit(OpCodes.Ldarg_2); // int mipLevel
    il.Emit(OpCodes.Ldarg_3); // int elementSize
    il.Emit(OpCodes.Ldarg, 4); // int dataSize
    il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(unityMethod));
    il.Emit(OpCodes.Ret);

    Console.WriteLine("Replaced Texture2D_SetPixelDataImpl_Injected");
}
```

### 5. Final Usage

Now developers can use the new functionality:

```csharp
using UnityEngine;
using ExposedBindings;
using Unity.Collections;

public class TextureExample : MonoBehaviour
{
    void Start()
    {
        // Create texture
        var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        
        // Prepare pixel data
        var pixelData = new NativeArray<Color32>(256 * 256, Allocator.Temp);
        
        // Fill with test pattern
        for (int i = 0; i < pixelData.Length; i++)
        {
            pixelData[i] = new Color32((byte)(i % 255), (byte)((i * 2) % 255), (byte)((i * 3) % 255), 255);
        }
        
        // Use our new high-performance method
        texture.SetPixelDataNative(pixelData, 0);
        texture.Apply();
        
        // Clean up
        pixelData.Dispose();
    }
}
```

## Common Patterns and Best Practices

### Working with Different Parameter Types

#### Simple Value Types
```csharp
// For methods with simple parameters (int, float, bool, IntPtr)
// Direct parameter forwarding works:

il.Emit(OpCodes.Ldarg_0); // Load first parameter
il.Emit(OpCodes.Ldarg_1); // Load second parameter
il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(unityMethod));
il.Emit(OpCodes.Ret);
```

#### Reference Types
```csharp
// For Unity object parameters, always marshal first:
IntPtr objectPtr = UnityExposed.MarshalUnityObject(unityObject);

// Then pass the pointer to the internal method
il.Emit(OpCodes.Ldarg_0); // The marshalled IntPtr
```

#### Span and Array Parameters
```csharp
// Always convert to ManagedSpanWrapper:
unsafe
{
    fixed (T* dataPtr = &span.GetPinnableReference())
    {
        ManagedSpanWrapper wrapper = new ManagedSpanWrapper(dataPtr, span.Length * sizeof(T));
        // Pass ref wrapper to internal method
    }
}
```

### Handling Return Values

#### Void Methods
```csharp
// Simply call and return
il.Emit(OpCodes.Call, unityMethod);
il.Emit(OpCodes.Ret);
```

#### Value Type Returns
```csharp
// Return value is automatically on stack
il.Emit(OpCodes.Call, unityMethod);
il.Emit(OpCodes.Ret); // Returns the value from stack
```

#### Reference Type Returns
```csharp
// For Unity objects, may need to wrap IntPtr back to managed object
// This is complex and often Unity handles it internally
il.Emit(OpCodes.Call, unityMethod);
il.Emit(OpCodes.Ret);
```

### Error Checking and Logging

Always validate inputs in extension methods:

```csharp
public static bool YourMethod(this UnityEngine.Object target, Span<byte> data)
{
    // Null checks
    if (target == null)
    {
        Debug.LogError("Target object is null");
        return false;
    }

    // Data validation
    if (data.Length == 0)
    {
        Debug.LogError("Data span is empty");
        return false;
    }

    // Size validation
    if (data.Length > MaxSupportedSize)
    {
        Debug.LogError($"Data size {data.Length} exceeds maximum {MaxSupportedSize}");
        return false;
    }

    // Unity object marshalling validation
    IntPtr objectPtr = UnityExposed.MarshalUnityObject(target);
    if (objectPtr == IntPtr.Zero)
    {
        Debug.LogError("Failed to marshal Unity object - object may be destroyed");
        return false;
    }

    // Proceed with operation...
}
```

### Platform Compatibility Considerations

#### IL2CPP Compatibility
- **Avoid reflection** on injected methods in IL2CPP builds
- **Pre-declare generic types** that will be used
- **Test thoroughly** on target platforms

#### Assembly Stripping
```csharp
// Use link.xml to preserve required types
[assembly: Preserve]

namespace ExposedBindings
{
    [Preserve]
    public static class YourExtensions
    {
        [Preserve]
        public static void YourMethod() { }
    }
}
```

#### Unity Version Compatibility
```csharp
// Check Unity version compatibility
#if UNITY_2022_1_OR_NEWER
    // Use newer API
#else
    // Fallback for older versions
#endif
```

## Troubleshooting

### Common Errors and Solutions

#### 1. "Assembly not processed by Cecil"
**Error**: `NotImplementedException: Assembly not processed by Cecil. Run ProcessAssembly.sh`

**Cause**: The stub method wasn't replaced by Cecil processing.

**Solutions**:
- Ensure `ProcessAssembly.sh` was run successfully
- Check that the method name matches exactly in Cecil processor
- Verify the Unity path is correct
- Check console output for Cecil processing errors

#### 2. "Could not find Unity internal method"
**Error**: `Console.Error.WriteLine("Could not find UnityEngine.SomeType.SomeMethod_Injected")`

**Cause**: Unity's internal method signature changed or doesn't exist.

**Solutions**:
- Use ILSpy to verify the method still exists
- Check method signature matches exactly
- Verify you're looking in the correct Unity module
- Consider Unity version differences

#### 3. "Invalid IL code" or crashes
**Error**: Unity crashes or IL verification fails.

**Cause**: Incorrect IL generation or stack imbalance.

**Solutions**:
- Verify parameter loading order matches method signature
- Check stack balance (pushes must equal pops)
- Ensure all code paths end with `ret`
- Use `il.Emit` consistently for all instructions

#### 4. "ManagedSpanWrapper field not found"
**Error**: `Could not find field 'begin' or 'length'`

**Cause**: ManagedSpanWrapper structure mismatch.

**Solutions**:
- Verify field names match Unity's internal structure
- Check for Unity version changes in ManagedSpanWrapper
- Ensure proper assembly references

### Debugging Cecil Processing

Enable verbose logging in Cecil processor:

```csharp
static void ProcessAssembly(string inputPath, string outputPath, ...)
{
    Console.WriteLine("=== Cecil Processing Debug Info ===");
    
    // Log assembly loading
    Console.WriteLine($"Loading assembly: {inputPath}");
    var assembly = AssemblyDefinition.ReadAssembly(inputPath, ...);
    Console.WriteLine($"Assembly loaded: {assembly.FullName}");
    
    // Log type discovery
    var exposedType = assembly.MainModule.GetType("ExposedBindings.Exposed.UnityExposed");
    Console.WriteLine($"Found UnityExposed type: {exposedType != null}");
    
    // Log method discovery
    foreach (var method in exposedType.Methods)
    {
        Console.WriteLine($"Found method: {method.Name}");
    }
    
    // Log Unity assembly loading
    Console.WriteLine($"Loading Unity assembly: {coreModulePath}");
    var coreModule = AssemblyDefinition.ReadAssembly(coreModulePath);
    Console.WriteLine($"Unity assembly loaded: {coreModule.FullName}");
    
    // Continue with processing...
}
```

### IL2CPP Specific Issues

#### Generic Type AOT Issues
```csharp
// In a MonoBehaviour that will be included in builds
public class AOTHelper : MonoBehaviour
{
    void ForceAOTGeneration()
    {
        // Force AOT compilation of generic methods you'll use
        new Span<byte>().GetPinnableReference();
        new Span<Color32>().GetPinnableReference();
        new Span<float>().GetPinnableReference();
        
        // Force compilation of your extension methods
        var texture = new Texture2D(1, 1);
        texture.SetPixelDataSpan(new Span<Color32>());
    }
}
```

#### Native Pointer Validation
```csharp
// Add runtime validation for IL2CPP
public static bool ValidateNativePointer(IntPtr ptr)
{
    #if !UNITY_EDITOR && ENABLE_IL2CPP
        // Additional validation for IL2CPP builds
        if (ptr == IntPtr.Zero)
            return false;
            
        // Check if pointer is in valid range (platform-specific)
        // This is a simplified check - implement based on your needs
        return ptr.ToInt64() > 0x1000; // Basic null pointer check
    #else
        return ptr != IntPtr.Zero;
    #endif
}
```

## Resources and References

### IL Instruction Reference
- **Microsoft IL Documentation**: https://docs.microsoft.com/en-us/dotnet/api/system.reflection.emit.opcodes
- **ECMA-335 Standard**: https://www.ecma-international.org/publications/standards/Ecma-335.htm
- **IL Tutorial**: https://www.codeproject.com/Articles/3778/Introduction-to-IL-Assembly-Language

### Cecil Documentation
- **GitHub Repository**: https://github.com/jbevain/cecil
- **Wiki**: https://github.com/jbevain/cecil/wiki
- **API Documentation**: https://www.mono-project.com/docs/tools+libraries/libraries/Mono.Cecil/

### Unity Internals Resources
- **Unity Native Plugin Interface**: https://docs.unity3d.com/Manual/NativePlugins.html
- **Unity Low-Level Native Plugin Interface**: https://docs.unity3d.com/Manual/NativePluginInterface.html
- **Unity Collections Package**: https://docs.unity3d.com/Packages/com.unity.collections@latest

### Related Tools
- **ILSpy** (Free): https://github.com/icsharpcode/ILSpy
- **dotPeek** (JetBrains): https://www.jetbrains.com/decompiler/
- **Reflexil** (IL Editor): https://github.com/sailro/Reflexil
- **dnSpy** (Debugger + Editor): https://github.com/dnSpy/dnSpy

### Performance Resources
- **Unity Performance Best Practices**: https://docs.unity3d.com/Manual/BestPracticeGuides.html
- **C# Span and Memory**: https://docs.microsoft.com/en-us/dotnet/standard/memory-and-spans/
- **Unity Collections Performance**: https://docs.unity3d.com/Packages/com.unity.collections@latest/manual/allocation.html

### Community Resources
- **Unity Forums - Scripting**: https://forum.unity.com/forums/scripting.12/
- **Stack Overflow Unity Tag**: https://stackoverflow.com/questions/tagged/unity3d
- **Unity Discord Community**: Various Unity development Discord servers

---

This documentation provides a comprehensive guide to extending the Unity Exposed Bindings library. Remember to always test thoroughly on your target platforms and Unity versions, as internal APIs can change between releases.
<div align="center">
  
  # Unity Exposed Bindings
  
  ### High-Performance Unity Internal Bindings for Zero-Allocation Operations
  
  [![Unity](https://img.shields.io/badge/Unity-6000.0.31f1-black.svg?style=flat&logo=unity)](https://unity3d.com/)
  [![C#](https://img.shields.io/badge/C%23-11.0-239120.svg?style=flat&logo=c-sharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
  [![IL2CPP](https://img.shields.io/badge/IL2CPP-Supported-00D4AA.svg?style=flat)](https://docs.unity3d.com/Manual/IL2CPP.html)
  [![Cross Platform](https://img.shields.io/badge/Platform-iOS_Android_WebGL_Windows_macOS_Linux-blue.svg?style=flat)](https://unity.com/)
  [![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat)](LICENSE)
  
</div>

## Overview

`com.cosminb.unity-internals` is a high-performance Unity library that exposes internal bindings to enable zero-allocation operations using `Span<byte>` and `NativeArray<byte>`. This package allows direct interaction with Unity's native code methods, eliminating managed array overhead for critical performance-sensitive operations.

## How It Works

This library uses a sophisticated build pipeline to safely expose Unity's internal methods:

```
Runtime~/ (Source Code) → ProcessAssembly.sh → Compilation → Cecil IL Injection → ExposedBindings.dll
```

1. **Source Code** (`Runtime~/`): Contains C# extension methods and stub implementations
2. **ProcessAssembly.sh**: Orchestrates the build process
3. **Compilation**: Creates a standard .NET assembly
4. **Cecil Processing**: Injects IL code to call Unity's internal `_Injected` methods
5. **Final DLL**: The processed assembly with direct native bindings

The `Runtime~` folder (with `~` suffix) prevents Unity from compiling the source directly, as the code requires Cecil processing to function properly.

## Key Features

- **Zero-Allocation Image Loading**: Load textures directly from `Span<byte>` or `NativeArray<byte>`
- **Optimized AssetBundle Loading**: Load asset bundles without unnecessary memory allocations
- **Cross-Platform Compatibility**: Tested on Android, WebGL, macOS, Windows, Linux, and iOS
- **IL2CPP Support**: Seamless integration with development and release builds
- **Minimal Performance Overhead**: Utilizes Mono.Cecil for efficient method exposure

## Requirements

- Unity 6000.0.31f1 or later
- .NET 6.0 SDK (for Cecil processing)
- Mono.Cecil NuGet package

## Installation

### Option 1: Unity Package Manager (Recommended)

1. Open Unity Package Manager (Window > Package Manager)
2. Click the `+` button and select "Add package from git URL"
3. Enter the following URL:
   ```
   https://github.com/Cosmin-B/unity-exposed-internals.git
   ```

### Option 2: Manual Package Installation

1. Add to your project's `Packages/manifest.json`:
   ```json
   {
     "dependencies": {
       "com.cosminb.unity-internals": "https://github.com/Cosmin-B/unity-exposed-internals.git"
     }
   }
   ```

### Option 3: Download and Import

1. Download or clone this repository
2. Copy the `Assets/Packages/com.cosminb.unity-internals` folder to your project's `Assets` directory
3. Run the Cecil processor before using:
   ```bash
   ./ProcessAssembly.sh
   ```

## Usage Examples

```csharp
using Unity.Collections;
using ExposedBindings;

// Load texture from NativeArray
NativeArray<byte> imageData = ...;
Texture2D texture = new Texture2D(2, 2);
bool success = texture.LoadImageNative(imageData);

// Load AssetBundle from Span
Span<byte> bundleData = ...;
var request = AssetBundleExtensions.LoadFromMemoryAsyncSpan(bundleData);
```

## Technical Implementation

The library uses Mono.Cecil to:
- Recreate Unity's internal `ManagedSpanWrapper`
- Replace stub method bodies
- Enable direct access to Unity's internal methods
- Minimize runtime performance impact

## Compatibility

- **Platforms**: Android, WebGL, macOS, Windows, Linux, iOS
- **Build Types**: Mono and IL2CPP
- **Unity Version**: 6000.0.31f1 and later

## Performance Benefits

- Reduced GC pressure
- Eliminated array copying overhead
- Direct NativeArray usage in Jobs
- Potential Burst compatibility

## Why Cecil Over Reflection?

Unlike reflection-based approaches, this library uses Mono.Cecil for IL injection because:

- **Zero Runtime Overhead**: IL injection happens at build time, not runtime
- **No Allocations**: Direct method calls without boxing/unboxing
- **Burst Compatible**: Potential for Burst compilation (reflection blocks this)
- **IL2CPP Safe**: Works seamlessly with IL2CPP builds
- **Performance**: Direct native calls without reflection overhead

## Limitations

- **Unity Version Dependency**: Internal method signatures can change between Unity versions
- **Cecil Processing Required**: Must run ProcessAssembly.sh after package updates
- **Blittable Types Only**: Only supports types that can be directly mapped to memory
- **No Multi-dimensional Arrays**: Limited to single-dimensional arrays
- **Platform Testing Required**: Test on each target platform before shipping

## Safety & Compatibility

⚠️ **Important: Unity's internal layouts can change between major AND minor versions!**

- Always test thoroughly after Unity updates
- Rebuild the assembly when upgrading Unity versions
- The CecilProcessor may need adjustments for new Unity versions
- Consider maintaining version-specific branches for different Unity releases

## Setup and Processing

### Initial Setup
1. Clone this repository
2. Ensure you have .NET 6.0 SDK installed
3. Build the CecilProcessor:
   ```bash
   cd CecilProcessor
   dotnet build
   ```

### Processing Unity Assemblies
After importing the package, you must process Unity's assemblies:

```bash
# Make the script executable
chmod +x ProcessAssembly.sh

# Run the processor
./ProcessAssembly.sh
```

This will:
- Build the CecilProcessor if needed
- Process ExposedBindings.dll to expose internal methods
- Copy the processed assembly to the correct location

### Build and Distribution (For Maintainers)

1. Process assembly with Cecil
2. Include processed assembly in `Plugins/`
3. Update `package.json` version
4. Create GitHub release
5. Publish to UPM registry (optional)

## Credits & Inspiration

This project is inspired by Sebastian Schöner's excellent blog post: [Unmanaging Unity](https://blog.s-schoener.com/2024-11-02-unmanaging-unity/)

**Key insights from Sebastian's work:**
- Unity's internal `ManagedSpanWrapper` structure for zero-copy operations
- The existence of `_Injected` methods that accept spans directly
- How Unity's internal marshalling works with native code

**Our improvements:**
- Using Mono.Cecil for compile-time IL injection instead of runtime reflection
- Full IL2CPP compatibility without runtime overhead
- Cleaner API with extension methods
- Comprehensive platform testing

## Extending the Library

Want to expose more Unity internal methods? Check out our [guide on extending internals](Docs/extending-internals.md) which covers:
- Finding Unity's internal methods
- Understanding IL injection with Cecil
- Adding new exposed methods
- Testing and compatibility

## License

MIT License - See LICENSE file for details

## Author

Cosmin Bararu
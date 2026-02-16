#!/bin/bash

# Build script for processing Unity internal bindings assembly with Cecil
#
# Usage:
#   ./ProcessAssembly.sh                              # uses default Unity path
#   ./ProcessAssembly.sh /path/to/unity/editor        # custom Unity install
#   UNITY_PATH=/path/to/unity ./ProcessAssembly.sh    # via env var

# Accept Unity path as first argument, fall back to env var, then to default.
UNITY_PATH="${1:-${UNITY_PATH:-/Applications/Unity/Hub/Editor/6000.0.31f1}}"
PROJECT_PATH="$(pwd)"
ASSEMBLY_NAME="ExposedBindings"

echo "=== ExposedBindings Assembly Processor ==="
echo "Unity Path: $UNITY_PATH"
echo "Project Path: $PROJECT_PATH"

# Resolve the managed assemblies directory (macOS vs Windows layout)
if [ -d "$UNITY_PATH/Unity.app/Contents/Managed/UnityEngine" ]; then
    MANAGED_DIR="$UNITY_PATH/Unity.app/Contents/Managed"
    ENGINE_DIR="$MANAGED_DIR/UnityEngine"
elif [ -d "$UNITY_PATH/Editor/Data/Managed/UnityEngine" ]; then
    MANAGED_DIR="$UNITY_PATH/Editor/Data/Managed"
    ENGINE_DIR="$MANAGED_DIR/UnityEngine"
else
    echo "Error: Could not locate Unity managed assemblies under $UNITY_PATH"
    echo "Tried:"
    echo "  $UNITY_PATH/Unity.app/Contents/Managed/UnityEngine  (macOS)"
    echo "  $UNITY_PATH/Editor/Data/Managed/UnityEngine          (Windows)"
    exit 1
fi

echo "Managed Dir: $MANAGED_DIR"

# Step 1: Build the Cecil processor
echo ""
echo "Step 1: Building Cecil processor..."
cd CecilProcessor
dotnet restore
dotnet build -c Release
if [ $? -ne 0 ]; then
    echo "Error: Failed to build Cecil processor"
    exit 1
fi
cd ..

# Step 2: Build assembly from source using dotnet
echo ""
echo "Step 2: Building assembly from source..."

# Create temporary build directory
TEMP_BUILD="$PROJECT_PATH/temp_build"
rm -rf "$TEMP_BUILD"
mkdir -p "$TEMP_BUILD/TempAssembly"
cd "$TEMP_BUILD/TempAssembly"

# Create project file — references are resolved from the detected Unity install
cat > TempAssembly.csproj << CSPROJ_EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <AssemblyName>ExposedBindings</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>${ENGINE_DIR}/UnityEngine.CoreModule.dll</HintPath>
    </Reference>
    <Reference Include="UnityEngine.AssetBundleModule">
      <HintPath>${ENGINE_DIR}/UnityEngine.AssetBundleModule.dll</HintPath>
    </Reference>
    <Reference Include="UnityEngine.ImageConversionModule">
      <HintPath>${ENGINE_DIR}/UnityEngine.ImageConversionModule.dll</HintPath>
    </Reference>
    <Reference Include="Unity.Collections">
      <HintPath>${MANAGED_DIR}/Unity.Collections.dll</HintPath>
    </Reference>
    <Reference Include="mscorlib">
      <HintPath>${MANAGED_DIR}/mscorlib.dll</HintPath>
    </Reference>
    <Reference Include="System">
      <HintPath>${MANAGED_DIR}/System.dll</HintPath>
    </Reference>
    <Reference Include="System.Core">
      <HintPath>${MANAGED_DIR}/System.Core.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
CSPROJ_EOF

# Copy source files from package Runtime~ directory
cp -r "$PROJECT_PATH/Assets/Packages/com.cosminb.unity-internals/Runtime~"/* .

# Build the assembly
dotnet build -c Release

if [ $? -ne 0 ]; then
    echo "Error: Failed to build source assembly"
    exit 1
fi

COMPILED_ASSEMBLY="$TEMP_BUILD/TempAssembly/bin/Release/netstandard2.1/ExposedBindings.dll"
cd "$PROJECT_PATH"

# Step 3: Process the assembly with Cecil
echo ""
echo "Step 3: Processing assembly with Cecil..."
OUTPUT_ASSEMBLY="$PROJECT_PATH/Assets/Packages/com.cosminb.unity-internals/Plugins/ExposedBindings.dll"

# Create Plugins directory if it doesn't exist
mkdir -p "$PROJECT_PATH/Assets/Packages/com.cosminb.unity-internals/Plugins"

dotnet run --project CecilProcessor/CecilProcessor.csproj -- "$UNITY_PATH" "$COMPILED_ASSEMBLY" "$OUTPUT_ASSEMBLY"

if [ $? -ne 0 ]; then
    echo "Error: Failed to process assembly"
    rm -rf "$TEMP_BUILD"
    exit 1
fi

# Clean up temporary build directory
rm -rf "$TEMP_BUILD"

echo ""
echo "=== Processing Complete ==="
echo "Processed assembly saved to: $OUTPUT_ASSEMBLY"
echo ""
echo "Next steps:"
echo "1. The processed assembly is in Assets/Packages/com.cosminb.unity-internals/Plugins/"
echo "2. Unity will automatically reimport it"
echo "3. Test the functionality with the TestUnityInternalBindings MonoBehaviour"
echo ""
echo "All methods processed successfully:"
echo "  - MarshalUnityObject - Direct access to Unity object pointers without allocations"
echo "  - ImageConversion_LoadImage_Injected - Direct Span-based image loading"
echo "  - AssetBundle_LoadFromMemoryAsync_Internal_Injected - Async bundle loading from Span"
echo "  - AssetBundle_LoadFromMemory_Internal_Injected - Sync bundle loading from Span"
echo "  - Texture encoding methods (PNG, JPG, TGA, EXR)"

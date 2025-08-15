using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ExposedBindingsProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: CecilProcessor <UnityPath> <InputAssemblyPath> <OutputAssemblyPath>");
                Console.WriteLine("Example: CecilProcessor \"/Applications/Unity/Hub/Editor/6000.0.31f1\" \"./Library/ScriptAssemblies/ExposedBindings.dll\" \"./ProcessedAssembly.dll\"");
                return;
            }

            string unityPath = args[0];
            string inputAssemblyPath = args[1];
            string outputAssemblyPath = args[2];

            // Unity assembly paths
            string unityCoreModulePath = Path.Combine(unityPath, "Unity.app/Contents/Managed/UnityEngine/UnityEngine.CoreModule.dll");
            string unityAssetBundleModulePath = Path.Combine(unityPath, "Unity.app/Contents/Managed/UnityEngine/UnityEngine.AssetBundleModule.dll");
            string unityImageConversionModulePath = Path.Combine(unityPath, "Unity.app/Contents/Managed/UnityEngine/UnityEngine.ImageConversionModule.dll");

            // Check for Windows paths
            if (!File.Exists(unityCoreModulePath))
            {
                unityCoreModulePath = Path.Combine(unityPath, @"Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll");
                unityAssetBundleModulePath = Path.Combine(unityPath, @"Editor\Data\Managed\UnityEngine\UnityEngine.AssetBundleModule.dll");
                unityImageConversionModulePath = Path.Combine(unityPath, @"Editor\Data\Managed\UnityEngine\UnityEngine.ImageConversionModule.dll");
            }

            if (!File.Exists(unityCoreModulePath))
            {
                Console.Error.WriteLine($"Could not find Unity assemblies at {unityPath}");
                return;
            }

            try
            {
                ProcessAssembly(inputAssemblyPath, outputAssemblyPath, 
                    unityCoreModulePath, unityAssetBundleModulePath, unityImageConversionModulePath);
                
                Console.WriteLine($"Successfully processed assembly: {outputAssemblyPath}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing assembly: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
            }
        }

        static void ProcessAssembly(string inputPath, string outputPath, 
            string coreModulePath, string assetBundleModulePath, string imageConversionModulePath)
        {
            // Load Unity assemblies
            var coreModule = AssemblyDefinition.ReadAssembly(coreModulePath);
            var assetBundleModule = AssemblyDefinition.ReadAssembly(assetBundleModulePath);
            var imageConversionModule = AssemblyDefinition.ReadAssembly(imageConversionModulePath);

            // Create a custom resolver that knows where Unity assemblies are
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.GetDirectoryName(coreModulePath));
            
            // Load our assembly with the custom resolver
            var assembly = AssemblyDefinition.ReadAssembly(inputPath, new ReaderParameters
            {
                ReadWrite = false,
                InMemory = true,
                AssemblyResolver = resolver
            });

            // Find Unity's internal ManagedSpanWrapper
            var unitySpanWrapper = coreModule.MainModule.GetType("UnityEngine.Bindings.ManagedSpanWrapper");
            if (unitySpanWrapper == null)
            {
                Console.Error.WriteLine("Could not find UnityEngine.Bindings.ManagedSpanWrapper");
                return;
            }

            // Find our ManagedSpanWrapper
            var ourSpanWrapper = assembly.MainModule.GetType("ExposedBindings.Internal.ManagedSpanWrapper");
            if (ourSpanWrapper == null)
            {
                Console.Error.WriteLine("Could not find our ManagedSpanWrapper");
                return;
            }

            // Find or create the UnityExposed type
            var exposedType = assembly.MainModule.GetType("ExposedBindings.Exposed.UnityExposed");
            if (exposedType == null)
            {
                Console.Error.WriteLine("Could not find UnityExposed type");
                return;
            }

            // Replace stub methods in UnityExposed class
            AddMarshalUnityObjectMethod(assembly, exposedType, coreModule);
            ReplaceImageConversionMethod(assembly, exposedType, imageConversionModule, unitySpanWrapper, ourSpanWrapper);
            ReplaceAssetBundleMethods(assembly, exposedType, assetBundleModule, unitySpanWrapper, ourSpanWrapper);

            // Remove System.Private.CoreLib reference (added by dotnet SDK but not needed in Unity)
            var coreLibRef = assembly.MainModule.AssemblyReferences.FirstOrDefault(r => r.Name == "System.Private.CoreLib");
            if (coreLibRef != null)
            {
                assembly.MainModule.AssemblyReferences.Remove(coreLibRef);
                Console.WriteLine("Removed System.Private.CoreLib reference");
            }

            // Write the modified assembly with proper resolver
            var writerParams = new WriterParameters
            {
                WriteSymbols = false
            };
            assembly.Write(outputPath, writerParams);
        }

        static void AddMarshalUnityObjectMethod(AssemblyDefinition assembly, TypeDefinition exposedType, AssemblyDefinition coreModule)
        {
            // Find our stub method
            var method = exposedType.Methods.FirstOrDefault(m => m.Name == "MarshalUnityObject");
            if (method == null)
            {
                Console.WriteLine("Warning: Could not find stub method MarshalUnityObject");
                return;
            }

            // Find UnityEngine.Object type
            var unityObjectType = coreModule.MainModule.GetType("UnityEngine.Object");
            if (unityObjectType == null)
            {
                Console.Error.WriteLine("Could not find UnityEngine.Object");
                return;
            }

            // Find the m_CachedPtr field
            var cachedPtrField = unityObjectType.Fields.FirstOrDefault(f => f.Name == "m_CachedPtr");
            if (cachedPtrField == null)
            {
                Console.Error.WriteLine("Could not find m_CachedPtr field");
                return;
            }

            // Find the GetInstanceID method instead of the private field
            var getInstanceIdMethod = unityObjectType.Methods.FirstOrDefault(m => m.Name == "GetInstanceID" && m.Parameters.Count == 0);
            if (getInstanceIdMethod == null)
            {
                Console.Error.WriteLine("Could not find GetInstanceID method");
                return;
            }

            // Find GetPtrFromInstanceID method
            var getPtrMethod = unityObjectType.Methods.FirstOrDefault(m => m.Name == "GetPtrFromInstanceID");
            if (getPtrMethod == null)
            {
                Console.Error.WriteLine("Could not find GetPtrFromInstanceID method");
                return;
            }

            // Clear existing method body
            method.Body.Instructions.Clear();
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();

            var il = method.Body.GetILProcessor();

            // Add local variables
            var cachedPtrLocal = new VariableDefinition(assembly.MainModule.TypeSystem.IntPtr);
            var isMonoBehaviourLocal = new VariableDefinition(assembly.MainModule.TypeSystem.Boolean);
            method.Body.Variables.Add(cachedPtrLocal);
            method.Body.Variables.Add(isMonoBehaviourLocal);

            // Check if obj is null
            var notNullLabel = il.Create(OpCodes.Nop);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Brtrue_S, notNullLabel);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Ret);

            // obj is not null - get m_CachedPtr
            il.Append(notNullLabel);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, assembly.MainModule.ImportReference(cachedPtrField));
            il.Emit(OpCodes.Stloc_0); // Store in local

            // Check if m_CachedPtr != IntPtr.Zero
            var needsInstanceIdLabel = il.Create(OpCodes.Nop);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Beq_S, needsInstanceIdLabel);
            
            // m_CachedPtr is valid, return it
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);

            // m_CachedPtr is zero, need to use instance ID
            il.Append(needsInstanceIdLabel);
            
            // Call GetInstanceID() to get the instance ID
            var instanceIdLocal = new VariableDefinition(assembly.MainModule.TypeSystem.Int32);
            method.Body.Variables.Add(instanceIdLocal);
            
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(getInstanceIdMethod));
            il.Emit(OpCodes.Stloc, instanceIdLocal);
            
            // Check if instance ID == 0
            var instanceIdNotZeroLabel = il.Create(OpCodes.Nop);
            il.Emit(OpCodes.Ldloc, instanceIdLocal);
            il.Emit(OpCodes.Brtrue_S, instanceIdNotZeroLabel);
            
            // Instance ID is 0, return IntPtr.Zero
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Ret);
            
            // Instance ID is not 0, call GetPtrFromInstanceID
            il.Append(instanceIdNotZeroLabel);
            il.Emit(OpCodes.Ldloc, instanceIdLocal);
            
            // For simplicity, pass null for Type (Unity will handle it)
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldloca_S, isMonoBehaviourLocal);
            il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(getPtrMethod));
            il.Emit(OpCodes.Ret);

            Console.WriteLine("Replaced MarshalUnityObject");
        }

        static void ReplaceImageConversionMethod(AssemblyDefinition assembly, TypeDefinition exposedType, 
            AssemblyDefinition imageConversionModule, TypeDefinition unitySpanWrapper, TypeDefinition ourSpanWrapper)
        {
            // Find our existing stub method
            var method = exposedType.Methods.FirstOrDefault(m => m.Name == "ImageConversion_LoadImage_Injected");
            if (method == null)
            {
                Console.Error.WriteLine("Could not find stub method ImageConversion_LoadImage_Injected");
                return;
            }

            // Find Unity's internal method
            var imageConversionType = imageConversionModule.MainModule.GetType("UnityEngine.ImageConversion");
            var unityMethod = imageConversionType?.Methods.FirstOrDefault(m => m.Name == "LoadImage_Injected");
            
            if (unityMethod == null)
            {
                Console.Error.WriteLine("Could not find UnityEngine.ImageConversion.LoadImage_Injected");
                return;
            }

            // Clear the existing method body
            method.Body.Instructions.Clear();
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();

            var il = method.Body.GetILProcessor();

            // Create a local variable for Unity's ManagedSpanWrapper
            var localSpanWrapper = new VariableDefinition(assembly.MainModule.ImportReference(unitySpanWrapper));
            method.Body.Variables.Add(localSpanWrapper);

            // Construct Unity's ManagedSpanWrapper from our wrapper
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
            il.Emit(OpCodes.Ldarg_2); // bool markNonReadable
            il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(unityMethod));
            il.Emit(OpCodes.Ret);

            Console.WriteLine("Replaced ImageConversion_LoadImage_Injected");
        }

        static void ReplaceAssetBundleMethods(AssemblyDefinition assembly, TypeDefinition exposedType,
            AssemblyDefinition assetBundleModule, TypeDefinition unitySpanWrapper, TypeDefinition ourSpanWrapper)
        {
            ReplaceAssetBundleMethod(assembly, exposedType, assetBundleModule, unitySpanWrapper, ourSpanWrapper,
                "AssetBundle_LoadFromMemoryAsync_Internal_Injected", "LoadFromMemoryAsync_Internal_Injected");
            
            ReplaceAssetBundleMethod(assembly, exposedType, assetBundleModule, unitySpanWrapper, ourSpanWrapper,
                "AssetBundle_LoadFromMemory_Internal_Injected", "LoadFromMemory_Internal_Injected");
        }

        static void ReplaceAssetBundleMethod(AssemblyDefinition assembly, TypeDefinition exposedType,
            AssemblyDefinition assetBundleModule, TypeDefinition unitySpanWrapper, TypeDefinition ourSpanWrapper,
            string exposedMethodName, string unityMethodName)
        {
            // Find our existing stub method
            var method = exposedType.Methods.FirstOrDefault(m => m.Name == exposedMethodName);
            if (method == null)
            {
                Console.Error.WriteLine($"Could not find stub method {exposedMethodName}");
                return;
            }

            // Find Unity's internal method
            var assetBundleType = assetBundleModule.MainModule.GetType("UnityEngine.AssetBundle");
            var unityMethod = assetBundleType?.Methods.FirstOrDefault(m => m.Name == unityMethodName);
            
            if (unityMethod == null)
            {
                Console.Error.WriteLine($"Could not find UnityEngine.AssetBundle.{unityMethodName}");
                return;
            }

            // Clear the existing method body
            method.Body.Instructions.Clear();
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();

            var il = method.Body.GetILProcessor();

            // Create a local variable for Unity's ManagedSpanWrapper
            var localSpanWrapper = new VariableDefinition(assembly.MainModule.ImportReference(unitySpanWrapper));
            method.Body.Variables.Add(localSpanWrapper);

            // Construct Unity's ManagedSpanWrapper from our wrapper
            il.Emit(OpCodes.Ldloca_S, localSpanWrapper);
            il.Emit(OpCodes.Ldarg_0); // Load our ManagedSpanWrapper ref
            il.Emit(OpCodes.Ldfld, ourSpanWrapper.Fields.First(f => f.Name == "begin"));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ourSpanWrapper.Fields.First(f => f.Name == "length"));
            
            var unitySpanWrapperCtor = unitySpanWrapper.Methods.FirstOrDefault(m => m.IsConstructor);
            if (unitySpanWrapperCtor != null)
            {
                il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(unitySpanWrapperCtor));
            }

            // Call Unity's internal method
            il.Emit(OpCodes.Ldloca_S, localSpanWrapper); // ref Unity's ManagedSpanWrapper
            il.Emit(OpCodes.Ldarg_1); // uint crc
            il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(unityMethod));
            il.Emit(OpCodes.Ret);

            Console.WriteLine($"Replaced {exposedMethodName}");
        }

        static void ProcessImageConversion(AssemblyDefinition assembly, AssemblyDefinition imageConversionModule, TypeDefinition unitySpanWrapper)
        {
            var extensionsType = assembly.MainModule.GetType("ExposedBindings.ImageConversionExtensions");
            if (extensionsType == null)
            {
                Console.WriteLine("Warning: Could not find ImageConversionExtensions type");
                return;
            }

            var method = extensionsType.Methods.FirstOrDefault(m => m.Name == "LoadImage_Injected");
            if (method == null)
            {
                Console.WriteLine("Warning: Could not find LoadImage_Injected method");
                return;
            }

            // Find Unity's internal method
            var imageConversionType = imageConversionModule.MainModule.GetType("UnityEngine.ImageConversion");
            var unityMethod = imageConversionType?.Methods.FirstOrDefault(m => m.Name == "LoadImage_Injected");
            
            if (unityMethod == null)
            {
                Console.Error.WriteLine("Could not find UnityEngine.ImageConversion.LoadImage_Injected");
                return;
            }

            // Clear existing method body
            method.Body.Instructions.Clear();
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();

            var il = method.Body.GetILProcessor();

            // Create a local variable for Unity's ManagedSpanWrapper
            var localSpanWrapper = new VariableDefinition(assembly.MainModule.ImportReference(unitySpanWrapper));
            method.Body.Variables.Add(localSpanWrapper);

            // Construct Unity's ManagedSpanWrapper from our wrapper
            il.Emit(OpCodes.Ldloca_S, localSpanWrapper);
            il.Emit(OpCodes.Ldarg_1); // Load our ManagedSpanWrapper ref
            il.Emit(OpCodes.Ldfld, GetFieldReference(assembly, "ExposedBindings.Internal.ManagedSpanWrapper", "begin"));
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldfld, GetFieldReference(assembly, "ExposedBindings.Internal.ManagedSpanWrapper", "length"));
            
            var unitySpanWrapperCtor = unitySpanWrapper.Methods.FirstOrDefault(m => m.IsConstructor);
            if (unitySpanWrapperCtor != null)
            {
                il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(unitySpanWrapperCtor));
            }

            // Call Unity's internal method
            il.Emit(OpCodes.Ldarg_0); // IntPtr tex
            il.Emit(OpCodes.Ldloca_S, localSpanWrapper); // ref ManagedSpanWrapper
            il.Emit(OpCodes.Ldarg_2); // bool markNonReadable
            il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(unityMethod));
            il.Emit(OpCodes.Ret);

            Console.WriteLine("Processed LoadImage_Injected");
        }

        static void ProcessAssetBundle(AssemblyDefinition assembly, AssemblyDefinition assetBundleModule, TypeDefinition unitySpanWrapper)
        {
            var extensionsType = assembly.MainModule.GetType("ExposedBindings.AssetBundleExtensions");
            if (extensionsType == null)
            {
                Console.WriteLine("Warning: Could not find AssetBundleExtensions type");
                return;
            }

            // Process LoadFromMemoryAsync_Internal_Injected
            ProcessAssetBundleMethod(assembly, assetBundleModule, extensionsType, unitySpanWrapper,
                "LoadFromMemoryAsync_Internal_Injected", "LoadFromMemoryAsync_Internal_Injected");

            // Process LoadFromMemory_Internal_Injected
            ProcessAssetBundleMethod(assembly, assetBundleModule, extensionsType, unitySpanWrapper,
                "LoadFromMemory_Internal_Injected", "LoadFromMemory_Internal_Injected");
        }

        static void ProcessAssetBundleMethod(AssemblyDefinition assembly, AssemblyDefinition assetBundleModule, 
            TypeDefinition extensionsType, TypeDefinition unitySpanWrapper, string methodName, string unityMethodName)
        {
            var method = extensionsType.Methods.FirstOrDefault(m => m.Name == methodName);
            if (method == null)
            {
                Console.WriteLine($"Warning: Could not find {methodName} method");
                return;
            }

            // Find Unity's internal method
            var assetBundleType = assetBundleModule.MainModule.GetType("UnityEngine.AssetBundle");
            var unityMethod = assetBundleType?.Methods.FirstOrDefault(m => m.Name == unityMethodName);
            
            if (unityMethod == null)
            {
                Console.Error.WriteLine($"Could not find UnityEngine.AssetBundle.{unityMethodName}");
                return;
            }

            // Clear existing method body
            method.Body.Instructions.Clear();
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();

            var il = method.Body.GetILProcessor();

            // Create a local variable for Unity's ManagedSpanWrapper
            var localSpanWrapper = new VariableDefinition(assembly.MainModule.ImportReference(unitySpanWrapper));
            method.Body.Variables.Add(localSpanWrapper);

            // Construct Unity's ManagedSpanWrapper from our wrapper
            il.Emit(OpCodes.Ldloca_S, localSpanWrapper);
            il.Emit(OpCodes.Ldarg_0); // Load our ManagedSpanWrapper ref
            il.Emit(OpCodes.Ldfld, GetFieldReference(assembly, "ExposedBindings.Internal.ManagedSpanWrapper", "begin"));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, GetFieldReference(assembly, "ExposedBindings.Internal.ManagedSpanWrapper", "length"));
            
            var unitySpanWrapperCtor = unitySpanWrapper.Methods.FirstOrDefault(m => m.IsConstructor);
            if (unitySpanWrapperCtor != null)
            {
                il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(unitySpanWrapperCtor));
            }

            // Call Unity's internal method
            il.Emit(OpCodes.Ldloca_S, localSpanWrapper); // ref ManagedSpanWrapper
            il.Emit(OpCodes.Ldarg_1); // uint crc
            il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(unityMethod));
            il.Emit(OpCodes.Ret);

            Console.WriteLine($"Processed {methodName}");
        }

        static FieldReference GetFieldReference(AssemblyDefinition assembly, string typeName, string fieldName)
        {
            var type = assembly.MainModule.GetType(typeName);
            if (type == null)
            {
                // Try to find in nested types
                foreach (var t in assembly.MainModule.Types)
                {
                    type = t.NestedTypes.FirstOrDefault(nt => nt.FullName == typeName);
                    if (type != null) break;
                }
            }

            var field = type?.Fields.FirstOrDefault(f => f.Name == fieldName);
            return field != null ? assembly.MainModule.ImportReference(field) : null;
        }
    }
}
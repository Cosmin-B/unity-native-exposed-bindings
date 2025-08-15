using System;
using System.Runtime.CompilerServices;
using ExposedBindings.Internal;

namespace ExposedBindings.Exposed
{
    /// <summary>
    /// Placeholder class that will be processed by Cecil to expose Unity's internal methods.
    /// These methods will have their bodies replaced with IL that calls Unity's actual internal methods.
    /// </summary>
    public static class UnityExposed
    {
        // Dummy field to ensure the assembly compiles
        internal static int Dummy = 0;
        
        // These stub methods will be replaced by Cecil post-processing to call Unity's internal methods
        
        /// <summary>
        /// Gets the marshalled Unity object pointer. This avoids allocations compared to reflection.
        /// </summary>
        public static IntPtr MarshalUnityObject(UnityEngine.Object obj)
        {
            // This will be replaced by Cecil to access obj.m_CachedPtr directly
            // and call Unity's internal GetPtrFromInstanceID if needed
            throw new NotImplementedException("Assembly not processed by Cecil. Run ProcessAssembly.sh");
        }
        
        public static unsafe bool ImageConversion_LoadImage_Injected(IntPtr tex, ref ManagedSpanWrapper data, bool markNonReadable)
        {
            // This will be replaced by Cecil to call UnityEngine.ImageConversion.LoadImage_Injected
            throw new NotImplementedException("Assembly not processed by Cecil. Run ProcessAssembly.sh");
        }
        
        public static unsafe IntPtr AssetBundle_LoadFromMemoryAsync_Internal_Injected(ref ManagedSpanWrapper binary, uint crc)
        {
            // This will be replaced by Cecil to call UnityEngine.AssetBundle.LoadFromMemoryAsync_Internal_Injected
            throw new NotImplementedException("Assembly not processed by Cecil. Run ProcessAssembly.sh");
        }
        
        public static unsafe IntPtr AssetBundle_LoadFromMemory_Internal_Injected(ref ManagedSpanWrapper binary, uint crc)
        {
            // This will be replaced by Cecil to call UnityEngine.AssetBundle.LoadFromMemory_Internal_Injected
            throw new NotImplementedException("Assembly not processed by Cecil. Run ProcessAssembly.sh");
        }
    }
}
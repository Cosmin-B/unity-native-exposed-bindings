using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using ExposedBindings.Internal;

namespace ExposedBindings
{
    /// <summary>
    /// High-performance AssetBundle loading extensions that bypass managed array overhead.
    /// Uses Unity's internal native bindings directly with Span support.
    /// </summary>
    public static class AssetBundleExtensions
    {
        /// <summary>
        /// Asynchronously loads an AssetBundle from a Span of bytes.
        /// This method bypasses managed array pinning overhead.
        /// </summary>
        /// <param name="binary">Span containing the AssetBundle data</param>
        /// <param name="crc">Optional CRC for data validation</param>
        /// <returns>AssetBundleCreateRequest for async loading, or null on error</returns>
        public static unsafe AssetBundleCreateRequest LoadFromMemoryAsyncSpan(Span<byte> binary, uint crc = 0)
        {
            if (binary.Length == 0)
            {
                Debug.LogError("LoadFromMemoryAsyncSpan: Binary span is empty");
                return null;
            }

            fixed (byte* dataPtr = &binary.GetPinnableReference())
            {
                ManagedSpanWrapper spanWrapper = new ManagedSpanWrapper(dataPtr, binary.Length);
                IntPtr requestPtr = LoadFromMemoryAsync_Internal_Injected(ref spanWrapper, crc);
                
                if (requestPtr == IntPtr.Zero)
                {
                    Debug.LogError("LoadFromMemoryAsyncSpan: Failed to create AssetBundleCreateRequest");
                    return null;
                }

                return AssetBundleCreateRequestMarshaller.ConvertToManaged(requestPtr);
            }
        }

        /// <summary>
        /// Asynchronously loads an AssetBundle from a ReadOnlySpan of bytes.
        /// </summary>
        /// <param name="binary">ReadOnlySpan containing the AssetBundle data</param>
        /// <param name="crc">Optional CRC for data validation</param>
        /// <returns>AssetBundleCreateRequest for async loading, or null on error</returns>
        public static unsafe AssetBundleCreateRequest LoadFromMemoryAsyncSpan(ReadOnlySpan<byte> binary, uint crc = 0)
        {
            if (binary.Length == 0)
            {
                Debug.LogError("LoadFromMemoryAsyncSpan: Binary span is empty");
                return null;
            }

            fixed (byte* dataPtr = &binary.GetPinnableReference())
            {
                ManagedSpanWrapper spanWrapper = new ManagedSpanWrapper(dataPtr, binary.Length);
                IntPtr requestPtr = LoadFromMemoryAsync_Internal_Injected(ref spanWrapper, crc);
                
                if (requestPtr == IntPtr.Zero)
                {
                    Debug.LogError("LoadFromMemoryAsyncSpan: Failed to create AssetBundleCreateRequest");
                    return null;
                }

                return AssetBundleCreateRequestMarshaller.ConvertToManaged(requestPtr);
            }
        }

        /// <summary>
        /// Asynchronously loads an AssetBundle from a NativeArray.
        /// This method is optimized for Unity's NativeArray without copying.
        /// </summary>
        /// <param name="binary">NativeArray containing the AssetBundle data</param>
        /// <param name="crc">Optional CRC for data validation</param>
        /// <returns>AssetBundleCreateRequest for async loading, or null on error</returns>
        public static unsafe AssetBundleCreateRequest LoadFromMemoryAsyncNative(NativeArray<byte> binary, uint crc = 0)
        {
            if (!binary.IsCreated || binary.Length == 0)
            {
                Debug.LogError("LoadFromMemoryAsyncNative: NativeArray is not created or empty");
                return null;
            }

            void* dataPtr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(binary);
            ManagedSpanWrapper spanWrapper = new ManagedSpanWrapper(dataPtr, binary.Length);
            IntPtr requestPtr = LoadFromMemoryAsync_Internal_Injected(ref spanWrapper, crc);
            
            if (requestPtr == IntPtr.Zero)
            {
                Debug.LogError("LoadFromMemoryAsyncNative: Failed to create AssetBundleCreateRequest");
                return null;
            }

            return AssetBundleCreateRequestMarshaller.ConvertToManaged(requestPtr);
        }

        /// <summary>
        /// Synchronously loads an AssetBundle from a Span of bytes.
        /// This method bypasses managed array pinning overhead.
        /// </summary>
        /// <param name="binary">Span containing the AssetBundle data</param>
        /// <param name="crc">Optional CRC for data validation</param>
        /// <returns>Loaded AssetBundle, or null on error</returns>
        public static unsafe AssetBundle LoadFromMemorySpan(Span<byte> binary, uint crc = 0)
        {
            if (binary.Length == 0)
            {
                Debug.LogError("LoadFromMemorySpan: Binary span is empty");
                return null;
            }

            fixed (byte* dataPtr = &binary.GetPinnableReference())
            {
                ManagedSpanWrapper spanWrapper = new ManagedSpanWrapper(dataPtr, binary.Length);
                IntPtr bundlePtr = LoadFromMemory_Internal_Injected(ref spanWrapper, crc);
                
                if (bundlePtr == IntPtr.Zero)
                {
                    Debug.LogError("LoadFromMemorySpan: Failed to load AssetBundle");
                    return null;
                }

                return UnmarshalUnityObject<AssetBundle>(bundlePtr);
            }
        }

        /// <summary>
        /// Synchronously loads an AssetBundle from a NativeArray.
        /// </summary>
        /// <param name="binary">NativeArray containing the AssetBundle data</param>
        /// <param name="crc">Optional CRC for data validation</param>
        /// <returns>Loaded AssetBundle, or null on error</returns>
        public static unsafe AssetBundle LoadFromMemoryNative(NativeArray<byte> binary, uint crc = 0)
        {
            if (!binary.IsCreated || binary.Length == 0)
            {
                Debug.LogError("LoadFromMemoryNative: NativeArray is not created or empty");
                return null;
            }

            void* dataPtr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(binary);
            ManagedSpanWrapper spanWrapper = new ManagedSpanWrapper(dataPtr, binary.Length);
            IntPtr bundlePtr = LoadFromMemory_Internal_Injected(ref spanWrapper, crc);
            
            if (bundlePtr == IntPtr.Zero)
            {
                Debug.LogError("LoadFromMemoryNative: Failed to load AssetBundle");
                return null;
            }

            return UnmarshalUnityObject<AssetBundle>(bundlePtr);
        }

        /// <summary>
        /// Calls the exposed Unity internal method.
        /// The UnityExposed class is processed by Cecil to expose the internal method.
        /// </summary>
        internal static unsafe IntPtr LoadFromMemoryAsync_Internal_Injected(ref ManagedSpanWrapper binary, uint crc)
        {
            // This will call the method exposed by Cecil processing
            return Exposed.UnityExposed.AssetBundle_LoadFromMemoryAsync_Internal_Injected(ref binary, crc);
        }

        /// <summary>
        /// Calls the exposed Unity internal method.
        /// The UnityExposed class is processed by Cecil to expose the internal method.
        /// </summary>
        internal static unsafe IntPtr LoadFromMemory_Internal_Injected(ref ManagedSpanWrapper binary, uint crc)
        {
            // This will call the method exposed by Cecil processing
            return Exposed.UnityExposed.AssetBundle_LoadFromMemory_Internal_Injected(ref binary, crc);
        }

        /// <summary>
        /// Helper method to unmarshal Unity objects from IntPtr.
        /// </summary>
        private static T UnmarshalUnityObject<T>(IntPtr ptr) where T : UnityEngine.Object
        {
            if (ptr == IntPtr.Zero)
                return null;

            GCHandle handle = GCHandle.FromIntPtr(ptr);
            return handle.Target as T;
        }
    }

    /// <summary>
    /// Internal marshaller for AssetBundleCreateRequest.
    /// Recreates the functionality of Unity's internal BindingsMarshaller.
    /// </summary>
    internal static class AssetBundleCreateRequestMarshaller
    {
        /// <summary>
        /// Converts an IntPtr to AssetBundleCreateRequest.
        /// Uses reflection to access the internal constructor.
        /// </summary>
        public static AssetBundleCreateRequest ConvertToManaged(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return null;

            // Use reflection to create AssetBundleCreateRequest with internal constructor
            var constructorInfo = typeof(AssetBundleCreateRequest).GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new Type[] { typeof(IntPtr) },
                null);

            if (constructorInfo != null)
            {
                return (AssetBundleCreateRequest)constructorInfo.Invoke(new object[] { ptr });
            }

            // Fallback: try to use default constructor and set internal pointer
            var request = new AssetBundleCreateRequest();
            var ptrField = typeof(AsyncOperation).GetField("m_Ptr", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (ptrField != null)
            {
                ptrField.SetValue(request, ptr);
                return request;
            }

            Debug.LogError("AssetBundleCreateRequestMarshaller: Failed to create AssetBundleCreateRequest from IntPtr");
            return null;
        }

        /// <summary>
        /// Converts an AssetBundleCreateRequest to IntPtr.
        /// </summary>
        public static IntPtr ConvertToNative(AssetBundleCreateRequest request)
        {
            if (request == null)
                return IntPtr.Zero;

            var ptrField = typeof(AsyncOperation).GetField("m_Ptr",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (ptrField != null)
            {
                return (IntPtr)ptrField.GetValue(request);
            }

            return IntPtr.Zero;
        }
    }
}
using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using ExposedBindings.Internal;

namespace ExposedBindings
{
    /// <summary>
    /// High-performance image loading extensions that bypass managed array overhead.
    /// Uses Unity's internal native bindings directly with Span support.
    /// </summary>
    public static class ImageConversionExtensions
    {
        /// <summary>
        /// Loads an image from a Span of bytes directly into a Texture2D.
        /// This method bypasses managed array pinning overhead.
        /// </summary>
        /// <param name="tex">The Texture2D to load the image into</param>
        /// <param name="data">Span containing the image data</param>
        /// <param name="markNonReadable">Whether to mark the texture as non-readable after loading</param>
        /// <returns>True if the image was loaded successfully, false otherwise</returns>
        public static unsafe bool LoadImageSpan(this Texture2D tex, Span<byte> data, bool markNonReadable = false)
        {
            if (tex == null)
            {
                Debug.LogError("LoadImageSpan: Texture2D is null");
                return false;
            }

            if (data.Length == 0)
            {
                Debug.LogError("LoadImageSpan: Data span is empty");
                return false;
            }

            // Use Unity's internal marshalling method instead of GetNativeTexturePtr
            // This gets the correct Unity object pointer, not the texture data pointer
            IntPtr texPtr = Exposed.UnityExposed.MarshalUnityObject(tex);
            if (texPtr == IntPtr.Zero)
            {
                Debug.LogError("LoadImageSpan: Failed to marshal Unity object");
                return false;
            }

            fixed (byte* dataPtr = &data.GetPinnableReference())
            {
                ManagedSpanWrapper spanWrapper = new ManagedSpanWrapper(dataPtr, data.Length);
                return LoadImage_Injected(texPtr, ref spanWrapper, markNonReadable);
            }
        }

        /// <summary>
        /// Loads an image from a NativeArray directly into a Texture2D.
        /// This method is optimized for Unity's NativeArray without copying.
        /// </summary>
        /// <param name="tex">The Texture2D to load the image into</param>
        /// <param name="data">NativeArray containing the image data</param>
        /// <param name="markNonReadable">Whether to mark the texture as non-readable after loading</param>
        /// <returns>True if the image was loaded successfully, false otherwise</returns>
        public static unsafe bool LoadImageNative(this Texture2D tex, NativeArray<byte> data, bool markNonReadable = false)
        {
            if (tex == null)
            {
                Debug.LogError("LoadImageNative: Texture2D is null");
                return false;
            }

            if (!data.IsCreated || data.Length == 0)
            {
                Debug.LogError("LoadImageNative: NativeArray is not created or empty");
                return false;
            }

            // Use Unity's internal marshalling method
            IntPtr texPtr = Exposed.UnityExposed.MarshalUnityObject(tex);
            if (texPtr == IntPtr.Zero)
            {
                Debug.LogError("LoadImageNative: Failed to marshal Unity object");
                return false;
            }

            void* dataPtr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);
            ManagedSpanWrapper spanWrapper = new ManagedSpanWrapper(dataPtr, data.Length);
            return LoadImage_Injected(texPtr, ref spanWrapper, markNonReadable);
        }

        /// <summary>
        /// Loads an image from a ReadOnlySpan of bytes directly into a Texture2D.
        /// </summary>
        /// <param name="tex">The Texture2D to load the image into</param>
        /// <param name="data">ReadOnlySpan containing the image data</param>
        /// <param name="markNonReadable">Whether to mark the texture as non-readable after loading</param>
        /// <returns>True if the image was loaded successfully, false otherwise</returns>
        public static unsafe bool LoadImageSpan(this Texture2D tex, ReadOnlySpan<byte> data, bool markNonReadable = false)
        {
            if (tex == null)
            {
                Debug.LogError("LoadImageSpan: Texture2D is null");
                return false;
            }

            if (data.Length == 0)
            {
                Debug.LogError("LoadImageSpan: Data span is empty");
                return false;
            }

            // Use Unity's internal marshalling method instead of GetNativeTexturePtr
            // This gets the correct Unity object pointer, not the texture data pointer
            IntPtr texPtr = Exposed.UnityExposed.MarshalUnityObject(tex);
            if (texPtr == IntPtr.Zero)
            {
                Debug.LogError("LoadImageSpan: Failed to marshal Unity object");
                return false;
            }

            fixed (byte* dataPtr = &data.GetPinnableReference())
            {
                ManagedSpanWrapper spanWrapper = new ManagedSpanWrapper(dataPtr, data.Length);
                return LoadImage_Injected(texPtr, ref spanWrapper, markNonReadable);
            }
        }

        /// <summary>
        /// Calls the exposed Unity internal method.
        /// The UnityExposed class is processed by Cecil to expose the internal method.
        /// </summary>
        internal static unsafe bool LoadImage_Injected(IntPtr tex, ref ManagedSpanWrapper data, bool markNonReadable)
        {
            // This will call the method exposed by Cecil processing
            return Exposed.UnityExposed.ImageConversion_LoadImage_Injected(tex, ref data, markNonReadable);
        }
    }
}
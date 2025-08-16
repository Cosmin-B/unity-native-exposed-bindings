using System;
using Unity.Collections;
using UnityEngine;
using ExposedBindings.Internal;
using ExposedBindings.Exposed;

namespace ExposedBindings
{
    /// <summary>
    /// Extension methods for encoding Texture2D to various image formats with zero allocations.
    /// Returns NativeArray instead of byte[] to avoid managed memory allocations.
    /// </summary>
    public static class ImageEncodingExtensions
    {
        /// <summary>
        /// Encodes texture to PNG format returning a NativeArray.
        /// Caller is responsible for disposing the NativeArray.
        /// </summary>
        /// <param name="texture">The texture to encode</param>
        /// <param name="allocator">The allocator for the resulting NativeArray</param>
        /// <returns>NativeArray containing PNG data, or empty array on failure</returns>
        public static unsafe NativeArray<byte> EncodeToPNGNative(this Texture2D texture, Allocator allocator = Allocator.Persistent)
        {
            if (texture == null)
            {
                Debug.LogError("EncodeToPNGNative: Texture is null");
                return new NativeArray<byte>();
            }

            IntPtr texPtr = UnityExposed.MarshalUnityObject(texture);
            if (texPtr == IntPtr.Zero)
            {
                Debug.LogError("EncodeToPNGNative: Failed to marshal texture");
                return new NativeArray<byte>();
            }

            BlittableArrayWrapper wrapper;
            UnityExposed.EncodeToPNG_Injected(texPtr, out wrapper);

            // Unity owns the memory, so we need to copy it
            return wrapper.CopyToNativeArray<byte>(allocator);
        }

        /// <summary>
        /// Encodes texture to JPG format with specified quality returning a NativeArray.
        /// Caller is responsible for disposing the NativeArray.
        /// </summary>
        /// <param name="texture">The texture to encode</param>
        /// <param name="quality">JPG quality (1-100, default 75)</param>
        /// <param name="allocator">The allocator for the resulting NativeArray</param>
        /// <returns>NativeArray containing JPG data, or empty array on failure</returns>
        public static unsafe NativeArray<byte> EncodeToJPGNative(this Texture2D texture, int quality = 75, Allocator allocator = Allocator.Persistent)
        {
            if (texture == null)
            {
                Debug.LogError("EncodeToJPGNative: Texture is null");
                return new NativeArray<byte>();
            }

            if (quality < 1 || quality > 100)
            {
                Debug.LogWarning($"EncodeToJPGNative: Quality {quality} clamped to range 1-100");
                quality = Mathf.Clamp(quality, 1, 100);
            }

            IntPtr texPtr = UnityExposed.MarshalUnityObject(texture);
            if (texPtr == IntPtr.Zero)
            {
                Debug.LogError("EncodeToJPGNative: Failed to marshal texture");
                return new NativeArray<byte>();
            }

            BlittableArrayWrapper wrapper;
            UnityExposed.EncodeToJPG_Injected(texPtr, quality, out wrapper);

            // Unity owns the memory, so we need to copy it
            return wrapper.CopyToNativeArray<byte>(allocator);
        }

        /// <summary>
        /// Encodes texture to TGA format returning a NativeArray.
        /// Caller is responsible for disposing the NativeArray.
        /// </summary>
        /// <param name="texture">The texture to encode</param>
        /// <param name="allocator">The allocator for the resulting NativeArray</param>
        /// <returns>NativeArray containing TGA data, or empty array on failure</returns>
        public static unsafe NativeArray<byte> EncodeToTGANative(this Texture2D texture, Allocator allocator = Allocator.Persistent)
        {
            if (texture == null)
            {
                Debug.LogError("EncodeToTGANative: Texture is null");
                return new NativeArray<byte>();
            }

            IntPtr texPtr = UnityExposed.MarshalUnityObject(texture);
            if (texPtr == IntPtr.Zero)
            {
                Debug.LogError("EncodeToTGANative: Failed to marshal texture");
                return new NativeArray<byte>();
            }

            BlittableArrayWrapper wrapper;
            UnityExposed.EncodeToTGA_Injected(texPtr, out wrapper);

            // Unity owns the memory, so we need to copy it
            return wrapper.CopyToNativeArray<byte>(allocator);
        }

        /// <summary>
        /// Encodes texture to EXR format with specified flags returning a NativeArray.
        /// Caller is responsible for disposing the NativeArray.
        /// </summary>
        /// <param name="texture">The texture to encode</param>
        /// <param name="flags">EXR encoding flags</param>
        /// <param name="allocator">The allocator for the resulting NativeArray</param>
        /// <returns>NativeArray containing EXR data, or empty array on failure</returns>
        public static unsafe NativeArray<byte> EncodeToEXRNative(this Texture2D texture, Texture2D.EXRFlags flags = Texture2D.EXRFlags.None, Allocator allocator = Allocator.Persistent)
        {
            if (texture == null)
            {
                Debug.LogError("EncodeToEXRNative: Texture is null");
                return new NativeArray<byte>();
            }

            IntPtr texPtr = UnityExposed.MarshalUnityObject(texture);
            if (texPtr == IntPtr.Zero)
            {
                Debug.LogError("EncodeToEXRNative: Failed to marshal texture");
                return new NativeArray<byte>();
            }

            BlittableArrayWrapper wrapper;
            UnityExposed.EncodeToEXR_Injected(texPtr, (int)flags, out wrapper);

            // Unity owns the memory, so we need to copy it
            return wrapper.CopyToNativeArray<byte>(allocator);
        }

        // Span-based versions for convenience

        /// <summary>
        /// Encodes texture to PNG format returning a Span.
        /// Warning: The span is only valid temporarily as Unity owns the memory.
        /// Copy the data if you need to keep it.
        /// </summary>
        public static unsafe Span<byte> EncodeToPNGSpan(this Texture2D texture)
        {
            if (texture == null)
            {
                Debug.LogError("EncodeToPNGSpan: Texture is null");
                return Span<byte>.Empty;
            }

            IntPtr texPtr = UnityExposed.MarshalUnityObject(texture);
            if (texPtr == IntPtr.Zero)
            {
                Debug.LogError("EncodeToPNGSpan: Failed to marshal texture");
                return Span<byte>.Empty;
            }

            BlittableArrayWrapper wrapper;
            UnityExposed.EncodeToPNG_Injected(texPtr, out wrapper);

            return wrapper.AsSpan<byte>();
        }

        /// <summary>
        /// Encodes texture to JPG format returning a Span.
        /// Warning: The span is only valid temporarily as Unity owns the memory.
        /// Copy the data if you need to keep it.
        /// </summary>
        public static unsafe Span<byte> EncodeToJPGSpan(this Texture2D texture, int quality = 75)
        {
            if (texture == null)
            {
                Debug.LogError("EncodeToJPGSpan: Texture is null");
                return Span<byte>.Empty;
            }

            if (quality < 1 || quality > 100)
            {
                Debug.LogWarning($"EncodeToJPGSpan: Quality {quality} clamped to range 1-100");
                quality = Mathf.Clamp(quality, 1, 100);
            }

            IntPtr texPtr = UnityExposed.MarshalUnityObject(texture);
            if (texPtr == IntPtr.Zero)
            {
                Debug.LogError("EncodeToJPGSpan: Failed to marshal texture");
                return Span<byte>.Empty;
            }

            BlittableArrayWrapper wrapper;
            UnityExposed.EncodeToJPG_Injected(texPtr, quality, out wrapper);

            return wrapper.AsSpan<byte>();
        }

        /// <summary>
        /// Encodes texture to TGA format returning a Span.
        /// Warning: The span is only valid temporarily as Unity owns the memory.
        /// Copy the data if you need to keep it.
        /// </summary>
        public static unsafe Span<byte> EncodeToTGASpan(this Texture2D texture)
        {
            if (texture == null)
            {
                Debug.LogError("EncodeToTGASpan: Texture is null");
                return Span<byte>.Empty;
            }

            IntPtr texPtr = UnityExposed.MarshalUnityObject(texture);
            if (texPtr == IntPtr.Zero)
            {
                Debug.LogError("EncodeToTGASpan: Failed to marshal texture");
                return Span<byte>.Empty;
            }

            BlittableArrayWrapper wrapper;
            UnityExposed.EncodeToTGA_Injected(texPtr, out wrapper);

            return wrapper.AsSpan<byte>();
        }

        /// <summary>
        /// Encodes texture to EXR format returning a Span.
        /// Warning: The span is only valid temporarily as Unity owns the memory.
        /// Copy the data if you need to keep it.
        /// </summary>
        public static unsafe Span<byte> EncodeToEXRSpan(this Texture2D texture, Texture2D.EXRFlags flags = Texture2D.EXRFlags.None)
        {
            if (texture == null)
            {
                Debug.LogError("EncodeToEXRSpan: Texture is null");
                return Span<byte>.Empty;
            }

            IntPtr texPtr = UnityExposed.MarshalUnityObject(texture);
            if (texPtr == IntPtr.Zero)
            {
                Debug.LogError("EncodeToEXRSpan: Failed to marshal texture");
                return Span<byte>.Empty;
            }

            BlittableArrayWrapper wrapper;
            UnityExposed.EncodeToEXR_Injected(texPtr, (int)flags, out wrapper);

            return wrapper.AsSpan<byte>();
        }
    }
}
using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ExposedBindings.Internal
{
    /// <summary>
    /// Wrapper for Unity's internal BlittableArrayWrapper used for encoding operations.
    /// This struct is returned by Unity's encode methods and contains native memory.
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe struct BlittableArrayWrapper
    {
        public void* data;
        public int size;
        public UpdateFlags updateFlags;

        public enum UpdateFlags
        {
            NoUpdateNeeded = 0,
            SizeChanged = 1,
            DataIsNativePointer = 2,
            DataIsNativeOwnedMemory = 3,
            DataIsEmpty = 4,
            DataIsNull = 5,
        }

        /// <summary>
        /// Converts the native data to a NativeArray without copying.
        /// The caller is responsible for disposing the NativeArray.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArray<T> ToNativeArray<T>(Allocator allocator) where T : unmanaged
        {
            if (data == null || size <= 0)
            {
                return new NativeArray<T>();
            }

            // Calculate element count
            int elementSize = UnsafeUtility.SizeOf<T>();
            int elementCount = size / elementSize;

            // Create NativeArray that wraps the data
            var nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                data, elementCount, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Set safety handle for the native array
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, 
                AtomicSafetyHandle.Create());
#endif

            return nativeArray;
        }

        /// <summary>
        /// Copies the native data to a new NativeArray.
        /// Use this when Unity owns the memory and will free it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArray<T> CopyToNativeArray<T>(Allocator allocator) where T : unmanaged
        {
            if (data == null || size <= 0)
            {
                return new NativeArray<T>();
            }

            // Calculate element count
            int elementSize = UnsafeUtility.SizeOf<T>();
            int elementCount = size / elementSize;

            // Create new NativeArray and copy data
            var nativeArray = new NativeArray<T>(elementCount, allocator);
            UnsafeUtility.MemCpy(nativeArray.GetUnsafePtr(), data, size);

            return nativeArray;
        }

        /// <summary>
        /// Gets the data as a Span without copying.
        /// Warning: The span is only valid while Unity hasn't freed the memory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan<T>() where T : unmanaged
        {
            if (data == null || size <= 0)
            {
                return Span<T>.Empty;
            }

            int elementSize = UnsafeUtility.SizeOf<T>();
            int elementCount = size / elementSize;

            return new Span<T>(data, elementCount);
        }
    }
}
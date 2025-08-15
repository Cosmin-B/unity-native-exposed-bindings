using System;
using System.Runtime.CompilerServices;

namespace ExposedBindings.Internal
{
    /// <summary>
    /// Matches Unity's internal ManagedSpanWrapper structure from UnityEngine.CoreModule.
    /// This struct is used internally by Unity to pass spans to native code.
    /// Built for Unity 6000.0.31f1.
    /// </summary>
    public readonly ref struct ManagedSpanWrapper
    {
        public readonly unsafe void* begin;
        public readonly int length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ManagedSpanWrapper(void* begin, int length)
        {
            this.begin = begin;
            this.length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Span<T> ToSpan<T>(ManagedSpanWrapper spanWrapper)
        {
            return new Span<T>(spanWrapper.begin, spanWrapper.length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ReadOnlySpan<T> ToReadOnlySpan<T>(ManagedSpanWrapper spanWrapper)
        {
            return new ReadOnlySpan<T>(spanWrapper.begin, spanWrapper.length);
        }
    }
}
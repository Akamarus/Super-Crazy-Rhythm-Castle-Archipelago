using System.Runtime.CompilerServices;

namespace Il2CppInterop.Runtime.InteropTypes
{
    public class Il2CppObjectBase
    {
        private readonly object? _tryCastResult;

        public Il2CppObjectBase(IntPtr pointer, object? tryCastResult = null)
        {
            Pointer = pointer;
            _tryCastResult = tryCastResult;
        }

        public IntPtr Pointer { get; }
        public bool ThrowOnTryCast { get; set; }
        public int TryCastCalls { get; private set; }
        public Type? LastTryCastType { get; private set; }

        public T? TryCast<T>() where T : Il2CppObjectBase
        {
            TryCastCalls++;
            LastTryCastType = typeof(T);
            if (ThrowOnTryCast) throw new InvalidOperationException("il2cpp-try-cast");
            return _tryCastResult as T;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CreateGCHandle() => throw new NullReferenceException();
    }
}

namespace Il2CppSystem
{
    internal sealed class Nullable<T>
    {
        private readonly bool _hasValue;
        private readonly T _value;

        internal Nullable(bool hasValue, T value)
        {
            _hasValue = hasValue;
            _value = value;
        }

        public bool HasValue => _hasValue;
        public T Value => _value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Nullable<T> EmptyFromInterop()
        {
            Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase.CreateGCHandle();
            throw new InvalidOperationException("unreachable");
        }
    }
}

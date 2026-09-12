using System.Runtime.CompilerServices;

namespace Il2CppInterop.Runtime.InteropTypes
{
    internal static class Il2CppObjectBase
    {
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

internal enum CharacterIdentifier
{
    MouseLeader,
}

internal enum TestMouseCharacterMode
{
    InteropEmpty,
    ExplicitEmpty,
    Present,
    UnrelatedFailure,
}

internal sealed class SpawnMeatAnimalCharacterOnDemand
{
    private readonly TestMouseCharacterMode _mode;

    internal SpawnMeatAnimalCharacterOnDemand(TestMouseCharacterMode mode)
    {
        _mode = mode;
    }

    public Il2CppSystem.Nullable<CharacterIdentifier> Character => _mode switch
    {
        TestMouseCharacterMode.InteropEmpty =>
            Il2CppSystem.Nullable<CharacterIdentifier>.EmptyFromInterop(),
        TestMouseCharacterMode.ExplicitEmpty => new(false, default),
        TestMouseCharacterMode.Present => new(true, CharacterIdentifier.MouseLeader),
        _ => throw new NullReferenceException("unrelated-character-failure"),
    };
}

internal sealed class WrongMouseSpawnerType
{
    public Il2CppSystem.Nullable<CharacterIdentifier> Character => new(false, default);
}

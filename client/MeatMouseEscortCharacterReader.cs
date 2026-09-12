using System.Reflection;

namespace RhythmCastleAP;

internal readonly record struct MeatMouseEscortCharacterState(
    bool Readable,
    bool Present,
    string Stage);

internal static class MeatMouseEscortCharacterReader
{
    private const BindingFlags PublicInstance =
        BindingFlags.Public | BindingFlags.Instance;

    internal static MeatMouseEscortCharacterState Read(object? nativeSpawner)
    {
        if (nativeSpawner == null ||
            !string.Equals(
                nativeSpawner.GetType().FullName,
                "SpawnMeatAnimalCharacterOnDemand",
                StringComparison.Ordinal))
            return new(false, false, "exact-spawner-type-unavailable");

        PropertyInfo? characterProperty = nativeSpawner.GetType().GetProperty(
            "Character", PublicInstance);
        if (characterProperty?.GetMethod?.IsPublic != true ||
            !IsExactCharacterNullable(characterProperty.PropertyType))
            return new(false, false, "character-nullable-contract-unavailable");

        object? character;
        try
        {
            character = characterProperty.GetValue(nativeSpawner);
        }
        catch (TargetInvocationException ex)
            when (IsEmptyIl2CppNullableReturn(characterProperty.PropertyType, ex))
        {
            return new(true, false, "empty-il2cpp-nullable");
        }
        catch
        {
            return new(false, false, "character-get-failed");
        }

        if (character == null)
            return new(false, false, "character-null");

        PropertyInfo? hasValueProperty = character.GetType().GetProperty(
            "HasValue", PublicInstance);
        if (hasValueProperty?.GetMethod?.IsPublic != true)
            return new(false, false, "has-value-contract-unavailable");

        try
        {
            return hasValueProperty.GetValue(character) is bool present
                ? new(true, present, "success")
                : new(false, false, "has-value-invalid");
        }
        catch
        {
            return new(false, false, "has-value-get-failed");
        }
    }

    private static bool IsExactCharacterNullable(Type returnType) =>
        returnType.IsGenericType &&
        string.Equals(
            returnType.GetGenericTypeDefinition().FullName,
            "Il2CppSystem.Nullable`1",
            StringComparison.Ordinal) &&
        returnType.GetGenericArguments().Length == 1 &&
        string.Equals(
            returnType.GetGenericArguments()[0].FullName,
            "CharacterIdentifier",
            StringComparison.Ordinal);

    private static bool IsEmptyIl2CppNullableReturn(
        Type returnType,
        TargetInvocationException exception)
    {
        if (!IsExactCharacterNullable(returnType))
            return false;

        Exception? inner = exception.InnerException;
        return inner is NullReferenceException &&
            string.Equals(inner.TargetSite?.Name, "CreateGCHandle", StringComparison.Ordinal) &&
            string.Equals(
                inner.TargetSite?.DeclaringType?.FullName,
                "Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase",
                StringComparison.Ordinal);
    }
}

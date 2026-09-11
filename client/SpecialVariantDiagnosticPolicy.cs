namespace RhythmCastleAP;

internal enum SpecialVariantKind
{
    None,
    BeeNectarParty,
    BeeAct1BNectar,
    DevilDemonicRoom,
    DevilDemonicTower,
    DevilDemonicEscape,
    DevilDemonicLockers,
}

internal static class SpecialVariantDiagnosticPolicy
{
    private static readonly IReadOnlyDictionary<
        (string InternalLevel, string Variant),
        SpecialVariantKind> ExactVariants =
        new Dictionary<(string InternalLevel, string Variant), SpecialVariantKind>(
            VariantIdentityComparer.Instance)
        {
            [("Level_06", "LevelVariant_BeeMode")] = SpecialVariantKind.BeeNectarParty,
            [("Level_12", "LevelVariant_BeeMode")] = SpecialVariantKind.BeeAct1BNectar,
            [("Level_02", "LevelVariant_DevilMode")] = SpecialVariantKind.DevilDemonicRoom,
            [("Level_11", "LevelVariant_DevilMode")] = SpecialVariantKind.DevilDemonicTower,
            [("Level_13", "LevelVariant_DevilMode")] = SpecialVariantKind.DevilDemonicEscape,
            [("Level_14", "LevelVariant_DevilMode")] = SpecialVariantKind.DevilDemonicLockers,
        };

    private static readonly string[] RelevantTokens =
    {
        "BIZZLE",
        "CLIVE",
        "BEE",
        "NECTAR",
        "DEMON",
        "DEVIL",
        "SEAL",
        "BUNKER",
    };

    internal static SpecialVariantKind ClassifyVariant(
        string? internalLevel,
        string? variant)
    {
        if (string.IsNullOrWhiteSpace(internalLevel) ||
            string.IsNullOrWhiteSpace(variant))
            return SpecialVariantKind.None;

        return ExactVariants.TryGetValue((internalLevel, variant), out SpecialVariantKind kind)
            ? kind
            : SpecialVariantKind.None;
    }

    internal static bool IsRelevantProgressionFlag(string? flag) =>
        ContainsRelevantToken(flag);

    internal static bool IsRelevantSceneObject(
        string? hierarchyPath,
        IEnumerable<string>? managedComponentNames)
    {
        if (ContainsRelevantToken(hierarchyPath))
            return true;

        if (managedComponentNames == null)
            return false;

        foreach (string? componentName in managedComponentNames)
        {
            if (ContainsRelevantToken(componentName))
                return true;
        }

        return false;
    }

    private static bool ContainsRelevantToken(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        RelevantTokens.Any(token =>
            value.Contains(token, StringComparison.OrdinalIgnoreCase));

    private sealed class VariantIdentityComparer :
        IEqualityComparer<(string InternalLevel, string Variant)>
    {
        internal static readonly VariantIdentityComparer Instance = new();

        public bool Equals(
            (string InternalLevel, string Variant) x,
            (string InternalLevel, string Variant) y) =>
            string.Equals(x.InternalLevel, y.InternalLevel, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Variant, y.Variant, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string InternalLevel, string Variant) value) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(value.InternalLevel),
                StringComparer.OrdinalIgnoreCase.GetHashCode(value.Variant));
    }
}

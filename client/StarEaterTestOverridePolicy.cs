namespace RhythmCastleAP;

internal static class StarEaterTestOverridePolicy
{
    internal static IReadOnlyDictionary<string, int>? Create(bool enabled, int royal, int bunker)
    {
        if (!enabled) return null;
        var overrides = new Dictionary<string, int>();
        royal = Math.Clamp(royal, 0, 40);
        bunker = Math.Clamp(bunker, 0, 66);
        if (royal != 40) overrides["Royal Corridor"] = royal;
        if (bunker != 66) overrides["Secret Bunker"] = bunker;
        return overrides.Count == 0 ? null : overrides;
    }
}

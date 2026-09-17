namespace RhythmCastleAP;

internal sealed record CampaignLevelDescriptor(
    int Number,
    string DisplayName,
    string InternalId,
    string Area)
{
    internal string Location(string tier) => $"Level {Number} - {tier}";
}

internal static class CampaignLevelCatalog
{
    internal static readonly IReadOnlyList<CampaignLevelDescriptor> All =
    [
        new(1, "Light Humor", "Level_05", "Roots"),
        new(2, "Pop Party", "Level_06", "Roots"),
        new(3, "The Megafying Ritual", "Level_07", "Roots"),
        new(4, "DJ Eggplant", "Level_08", "Roots"),
        new(5, "Lift Quest", "Level_09", "Roots"),
        new(6, "Boring Room", "Level_02", "Lobby"),
        new(7, "Demolition Training", "Level_19", "Lobby"),
        new(8, "Minim Tower", "Level_11", "Lobby"),
        new(9, "School Trip", "Level_20", "Lobby"),
        new(10, "The Vault", "Level_01", "Lobby"),
        new(11, "Act 1: Flavor", "Level_12", "Meat Dimension"),
        new(12, "Act 2: Sauce and Spice", "Level_15", "Meat Dimension"),
        new(13, "Act 3: Montage", "Level_22", "Meat Dimension"),
        new(14, "Act 4: Habanero", "Level_23", "Meat Dimension"),
        new(15, "Central Mainframe", "Level_16", "Cell Tower"),
        new(16, "The Thief Prince", "Level_24", "Cell Tower"),
        new(17, "Cold Storage", "Level_21", "Lobby"),
        new(18, "The Darkness", "Level_03", "Tower of Fear"),
        new(19, "Escape", "Level_13", "Tower of Fear"),
        new(20, "Loneliness", "Level_25", "Tower of Fear"),
        new(21, "Locker Room", "Level_14", "Royal Corridor"),
        new(22, "King Ferdinand I", "Level_28", "Royal Corridor"),
    ];

    private static readonly IReadOnlyDictionary<string, CampaignLevelDescriptor> ByInternalId = BuildByInternalId();

    internal static bool TryGet(string? internalId, out CampaignLevelDescriptor descriptor)
    {
        if (internalId is not null && ByInternalId.TryGetValue(internalId, out CampaignLevelDescriptor? found))
        {
            descriptor = found;
            return true;
        }

        descriptor = null!;
        return false;
    }

    private static IReadOnlyDictionary<string, CampaignLevelDescriptor> BuildByInternalId()
    {
        Dictionary<string, CampaignLevelDescriptor> byInternalId = new(StringComparer.OrdinalIgnoreCase);
        HashSet<int> numbers = [];

        foreach (CampaignLevelDescriptor level in All)
        {
            if (!numbers.Add(level.Number))
                throw new InvalidOperationException($"Duplicate campaign level number: {level.Number}");
            if (!byInternalId.TryAdd(level.InternalId, level))
                throw new InvalidOperationException($"Duplicate campaign internal ID: {level.InternalId}");
        }

        return byInternalId;
    }
}

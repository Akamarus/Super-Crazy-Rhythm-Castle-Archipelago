namespace RhythmCastleAP;

internal sealed class MusicLabChest(int threshold, string objectName)
{
    internal int Threshold { get; } = threshold;
    internal string ObjectName { get; } = objectName;
    internal string InteractionPath { get; } = $"Root/GameRoom_Hub6_Logic/Objects/RewardChests/{objectName}/Interaction";
    internal string ConfigKey { get; } = FormattableString.Invariant($"MusicLab{threshold}PointChestLocationName");
    internal string DefaultLocationName { get; } = FormattableString.Invariant($"Music Lab - {threshold} Point Chest");
    internal string? ConfiguredLocationName { get; set; }
    internal string LocationName => ConfiguredLocationName ?? DefaultLocationName;
}

internal sealed class MusicLabChestMap
{
    internal readonly MusicLabChest[] Chests =
    {
        new(5, "ManiacMemoryCardChest"), new(10, "GarageCartridgeChest_Gradius"),
        new(20, "CatBatteryChest"), new(32, "SongChest_QuickSand"),
        new(46, "GarageCartridgeChest_BloodyTears"), new(64, "SongChest_Flamenco"),
        new(89, "SongChest_TenFourGoodBuddy"), new(111, "SongChest_Zen"), new(140, "SongChest_Wiggle"),
    };

    private readonly object _sync = new();
    private readonly Dictionary<int, MusicLabChest> _byThreshold;
    private readonly Dictionary<int, int> _flagByThreshold = new();
    private readonly Dictionary<int, int> _thresholdByFlag = new();
    private readonly HashSet<int> _confirmed = new();
    private long _visit;

    internal MusicLabChestMap() => _byThreshold = Chests.ToDictionary(chest => chest.Threshold);
    internal MusicLabChest At(int threshold) => _byThreshold[threshold];

    internal void BeginVisit()
    {
        lock (_sync)
        {
            _visit++;
            _confirmed.Clear();
            // Keep known numeric IDs available to early events while the new room is still loading.
        }
    }

    internal int ThresholdFor(int? flag)
    {
        lock (_sync) return flag.HasValue ? _thresholdByFlag.GetValueOrDefault(flag.Value) : 0;
    }

    internal bool TryGetFlag(int threshold, out int flag)
    {
        lock (_sync) return _flagByThreshold.TryGetValue(threshold, out flag);
    }

    internal bool Discover(Func<MusicLabChest, int?> readFlag)
    {
        long visit;
        lock (_sync)
        {
            if (_confirmed.Count == Chests.Length) return true;
            visit = _visit;
        }
        foreach (var chest in Chests)
        {
            lock (_sync)
            {
                if (visit != _visit) return false;
                if (_confirmed.Contains(chest.Threshold)) continue;
            }
            // Read the live scene outside the lock. Only validated numeric IDs are retained.
            if (readFlag(chest) is not int flag) continue;
            lock (_sync)
            {
                if (visit != _visit) return false;
                if (_flagByThreshold.TryGetValue(chest.Threshold, out int previous) &&
                    previous != flag && _thresholdByFlag.GetValueOrDefault(previous) == chest.Threshold)
                    _thresholdByFlag.Remove(previous);
                _flagByThreshold[chest.Threshold] = flag;
                _thresholdByFlag[flag] = chest.Threshold;
                _confirmed.Add(chest.Threshold);
            }
        }
        lock (_sync) return visit == _visit && _confirmed.Count == Chests.Length;
    }
}

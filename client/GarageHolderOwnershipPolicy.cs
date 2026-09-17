namespace RhythmCastleAP;
internal static class GarageHolderOwnershipPolicy
{
    internal static bool? Resolve(string flag, bool enabled, bool inHolderInitialization, Func<string, bool> ownsSong)
    {
        if (!enabled || !inHolderInitialization) return null;
        var cartridge = GarageCartridgeNativePolicy.RandomizedCartridges.FirstOrDefault(c => c.NativeCollectedFlag == flag);
        return string.IsNullOrEmpty(cartridge.Song) ? null : ownsSong(cartridge.Song);
    }
}

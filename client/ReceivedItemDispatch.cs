namespace RhythmCastleAP;

internal static class ReceivedItemDispatch
{
    internal static bool TryApply(
        string itemName,
        Func<string, bool> areaAccessHandler,
        Func<string, bool> garageCartridgeHandler,
        Func<string, bool> weedKillerHandler,
        Func<string, bool> plantPipesHandler,
        Func<string, bool> previewAbilityHandler,
        Func<string, bool> rootsBucketHandler,
        Func<string, bool> cassetteHandler,
        Func<string, bool> musicLabPointHandler)
    {
        // The first matching handler owns this delivery. It may queue work
        // until a save is available. A handled receipt can still be pending in-game.
        return
            areaAccessHandler(itemName) ||
            garageCartridgeHandler(itemName) ||
            weedKillerHandler(itemName) ||
            plantPipesHandler(itemName) ||
            previewAbilityHandler(itemName) ||
            rootsBucketHandler(itemName) ||
            cassetteHandler(itemName) ||
            musicLabPointHandler(itemName);
    }
}

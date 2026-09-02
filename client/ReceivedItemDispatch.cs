namespace RhythmCastleAP;

internal static class ReceivedItemDispatch
{
    internal static bool TryApply(
        string itemName,
        bool applyReceivedProgression,
        Func<string, bool> areaAccessHandler,
        Func<string, bool> garageCartridgeHandler,
        Func<string, bool> weedKillerHandler,
        Func<string, bool> plantPipesHandler,
        Func<string, bool> previewAbilityHandler,
        Func<string, bool> rootsBucketHandler,
        Func<string, bool> cassetteHandler,
        Action<string> experimentalFallback)
    {
        bool handled =
            areaAccessHandler(itemName) ||
            garageCartridgeHandler(itemName) ||
            weedKillerHandler(itemName) ||
            plantPipesHandler(itemName) ||
            previewAbilityHandler(itemName) ||
            rootsBucketHandler(itemName) ||
            cassetteHandler(itemName);

        if (handled)
            return true;

        if (!applyReceivedProgression)
            return false;

        experimentalFallback(itemName);
        return true;
    }
}

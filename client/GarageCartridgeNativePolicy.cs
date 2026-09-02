using System;
using System.Collections.Generic;
using System.Linq;

namespace RhythmCastleAP;

internal readonly record struct GarageCartridgeNativeDefinition(
    string Song,
    string ItemName,
    string RelativePath,
    string NativeBagFlag,
    string NativeRegisteredFlag,
    bool UsesPhysicalVanillaEntrance);

internal enum GarageCartridgeNativeDecision
{
    None,
    ApplyBagItem,
    AlreadyHeld,
    AlreadyRegistered,
}

internal static class GarageCartridgeNativePolicy
{
    internal static readonly GarageCartridgeNativeDefinition[] AllCartridges =
    {
        new(
            "Bloody Tears",
            "Bloody Tears Cartridge",
            "CartridgeHolder_BloodyTears/GR27_GameCartridge_BloodyTears",
            "LEVEL_27_CARTRIDGE_BLOODYTEARS_BAG_ITEM",
            "LEVEL_27_CARTRIDGE_BLOODYTEARS",
            false),
        new(
            "Gradius Remix",
            "Gradius Remix Cartridge",
            "CartridgeHolder_LoveShine/GR27_GameCartridge_Gradius",
            "LEVEL_27_CARTRIDGE_GRADIUS_BAG_ITEM",
            "LEVEL_27_CARTRIDGE_GRADIUS",
            false),
        new(
            "Smooch",
            "Smooch Cartridge",
            "CartridgeHolder_Smooch/GR27_GameCartridge_Smooch",
            "LEVEL_27_CARTRIDGE_SMOOCH_BAG_ITEM",
            "LEVEL_27_CARTRIDGE_SMOOCH",
            false),
        new(
            "Superstar",
            "Superstar Cartridge",
            "CartridgeHolder_StarEater/GR27_GameCartridge_Superstar",
            "LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM",
            "LEVEL_27_CARTRIDGE_STAR_EATER",
            false),
        new(
            "Vampire Killer",
            "Vampire Killer Cartridge",
            "CartridgeHolder_VampireKiller/GR27_GameCartridge_VampireKiller",
            "LEVEL_27_CARTRIDGE_VAMPIREKILLER_BAG_ITEM",
            "LEVEL_27_CARTRIDGE_VAMPIREKILLER",
            true),
        new(
            "Wag the Dog",
            "Wag the Dog Cartridge",
            "CartridgeHolder_SuperCrazyRhythmCastle/GR27_GameCartridge_WagTheDog",
            "LEVEL_27_CARTRIDGE_SUPER_CRAZY_RHYTHM_CASTLE_BAG_ITEM",
            "LEVEL_27_CARTRIDGE_SUPER_CRAZY_RHYTHM_CASTLE",
            false),
    };

    internal static IReadOnlyList<GarageCartridgeNativeDefinition> RandomizedCartridges { get; } =
        AllCartridges.Where(cartridge => !cartridge.UsesPhysicalVanillaEntrance).ToArray();

    internal static GarageCartridgeNativeDecision Decide(
        bool enabled,
        int receivedCount,
        bool nativeBagItemHeld,
        bool nativeCartridgeRegistered)
    {
        if (!enabled || receivedCount <= 0)
            return GarageCartridgeNativeDecision.None;
        if (nativeCartridgeRegistered)
            return GarageCartridgeNativeDecision.AlreadyRegistered;
        if (nativeBagItemHeld)
            return GarageCartridgeNativeDecision.AlreadyHeld;
        return GarageCartridgeNativeDecision.ApplyBagItem;
    }
}

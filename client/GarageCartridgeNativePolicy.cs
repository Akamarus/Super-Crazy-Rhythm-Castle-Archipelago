using System;
using System.Collections.Generic;
using System.Linq;

namespace RhythmCastleAP;

internal readonly record struct GarageCartridgeNativeDefinition(
    string Song,
    string ItemName,
    string RelativePath,
    string NativeBagFlag,
    string NativeCollectedFlag,
    string NativeCartridgeType,
    bool UsesPhysicalVanillaEntrance,
    string ServerInsertionKey);

internal enum GarageCartridgeProgressionFlagKind
{
    BagItem,
    Collected,
}

internal readonly record struct GarageCartridgeProgressionFlag(
    GarageCartridgeNativeDefinition Cartridge,
    GarageCartridgeProgressionFlagKind Kind);

internal static class GarageCartridgeNativePolicy
{
    internal static readonly GarageCartridgeNativeDefinition[] AllCartridges =
    {
        new(
            "Bloody Tears",
            "Bloody Tears Cartridge",
            "CartridgeHolder_BloodyTears/GR27_GameCartridge_BloodyTears",
            "LEVEL_27_CARTRIDGE_BLOODYTEARS_BAG_ITEM",
            "LEVEL_27_CARTRIDGE_BLOODYTEARS_COLLECTED",
            "BLOODY_TEARS",
            false,
            "scrc:garage_inserted:v1:bloody_tears"),
        new(
            "Gradius Remix",
            "Gradius Remix Cartridge",
            "CartridgeHolder_LoveShine/GR27_GameCartridge_Gradius",
            "LEVEL_27_CARTRIDGE_GRADIUS_BAG_ITEM",
            "LEVEL_27_CARTRIDGE_GRADIUS_COLLECTED",
            "GRADIUS_REMIX",
            false,
            "scrc:garage_inserted:v1:gradius_remix"),
        new(
            "Smooch",
            "Smooch Cartridge",
            "CartridgeHolder_Smooch/GR27_GameCartridge_Smooch",
            "LEVEL_27_CARTRIDGE_SMOOCH_BAG_ITEM",
            "LEVEL_27_CARTRIDGE_SMOOCH_COLLECTED",
            "SMOOCH",
            false,
            "scrc:garage_inserted:v1:smooch"),
        new(
            "Superstar",
            "Superstar Cartridge",
            "CartridgeHolder_StarEater/GR27_GameCartridge_Superstar",
            "LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM",
            "LEVEL_27_CARTRIDGE_STAR_EATER_COLLECTED",
            "SUPERSTAR",
            false,
            "scrc:garage_inserted:v1:superstar"),
        new(
            "Vampire Killer",
            "Vampire Killer Cartridge",
            "CartridgeHolder_VampireKiller/GR27_GameCartridge_VampireKiller",
            "LEVEL_27_CARTRIDGE_VAMPIREKILLER_BAG_ITEM",
            "LEVEL_27_CARTRIDGE_VAMPIREKILLER_COLLECTED",
            "VAMPIRE_KILLER",
            true,
            ""),
        new(
            "Wag the Dog",
            "Wag the Dog Cartridge",
            "CartridgeHolder_SuperCrazyRhythmCastle/GR27_GameCartridge_WagTheDog",
            "LEVEL_27_CARTRIDGE_SUPER_CRAZY_RHYTHM_CASTLE_BAG_ITEM",
            "LEVEL_27_CARTRIDGE_SUPER_CRAZY_RHYTHM_CASTLE_COLLECTED",
            "WAG_THE_DOG",
            false,
            "scrc:garage_inserted:v1:wag_the_dog"),
    };

    internal static IReadOnlyList<GarageCartridgeNativeDefinition> RandomizedCartridges { get; } =
        AllCartridges.Where(cartridge => !cartridge.UsesPhysicalVanillaEntrance).ToArray();

    internal static GarageCartridgeProgressionFlag? ClassifyProgressionFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag))
            return null;

        foreach (GarageCartridgeNativeDefinition cartridge in AllCartridges)
        {
            if (string.Equals(flag, cartridge.NativeBagFlag, StringComparison.OrdinalIgnoreCase))
                return new GarageCartridgeProgressionFlag(
                    cartridge,
                    GarageCartridgeProgressionFlagKind.BagItem);
            if (string.Equals(flag, cartridge.NativeCollectedFlag, StringComparison.OrdinalIgnoreCase))
                return new GarageCartridgeProgressionFlag(
                    cartridge,
                    GarageCartridgeProgressionFlagKind.Collected);
        }

        return null;
    }

    internal static bool ShouldSuppressVanillaSourceGrant(string flag, bool vanillaEntranceEnabled)
    {
        GarageCartridgeProgressionFlag? classification = ClassifyProgressionFlag(flag);
        return classification is { Kind: GarageCartridgeProgressionFlagKind.BagItem } &&
            GarageVanillaEntrancePolicy.ShouldSuppressSourceGrant(
                vanillaEntranceEnabled,
                classification.Value.Cartridge.Song);
    }

}

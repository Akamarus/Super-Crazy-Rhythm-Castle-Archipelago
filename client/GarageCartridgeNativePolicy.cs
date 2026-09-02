using System;
using System.Collections.Generic;
using System.Linq;

namespace RhythmCastleAP;

internal readonly record struct GarageCartridgeNativeDefinition(
    string Song,
    string ItemName,
    string RelativePath,
    string NativeBagFlag,
    string NativeCartridgeType,
    bool UsesPhysicalVanillaEntrance,
    string ServerInsertionKey);

internal static class GarageCartridgeNativePolicy
{
    internal static readonly GarageCartridgeNativeDefinition[] AllCartridges =
    {
        new(
            "Bloody Tears",
            "Bloody Tears Cartridge",
            "CartridgeHolder_BloodyTears/GR27_GameCartridge_BloodyTears",
            "LEVEL_27_CARTRIDGE_BLOODYTEARS_BAG_ITEM",
            "BLOODY_TEARS",
            false,
            "scrc:garage_inserted:v1:bloody_tears"),
        new(
            "Gradius Remix",
            "Gradius Remix Cartridge",
            "CartridgeHolder_LoveShine/GR27_GameCartridge_Gradius",
            "LEVEL_27_CARTRIDGE_GRADIUS_BAG_ITEM",
            "GRADIUS_REMIX",
            false,
            "scrc:garage_inserted:v1:gradius_remix"),
        new(
            "Smooch",
            "Smooch Cartridge",
            "CartridgeHolder_Smooch/GR27_GameCartridge_Smooch",
            "LEVEL_27_CARTRIDGE_SMOOCH_BAG_ITEM",
            "SMOOCH",
            false,
            "scrc:garage_inserted:v1:smooch"),
        new(
            "Superstar",
            "Superstar Cartridge",
            "CartridgeHolder_StarEater/GR27_GameCartridge_Superstar",
            "LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM",
            "SUPERSTAR",
            false,
            "scrc:garage_inserted:v1:superstar"),
        new(
            "Vampire Killer",
            "Vampire Killer Cartridge",
            "CartridgeHolder_VampireKiller/GR27_GameCartridge_VampireKiller",
            "LEVEL_27_CARTRIDGE_VAMPIREKILLER_BAG_ITEM",
            "VAMPIRE_KILLER",
            true,
            ""),
        new(
            "Wag the Dog",
            "Wag the Dog Cartridge",
            "CartridgeHolder_SuperCrazyRhythmCastle/GR27_GameCartridge_WagTheDog",
            "LEVEL_27_CARTRIDGE_SUPER_CRAZY_RHYTHM_CASTLE_BAG_ITEM",
            "WAG_THE_DOG",
            false,
            "scrc:garage_inserted:v1:wag_the_dog"),
    };

    internal static IReadOnlyList<GarageCartridgeNativeDefinition> RandomizedCartridges { get; } =
        AllCartridges.Where(cartridge => !cartridge.UsesPhysicalVanillaEntrance).ToArray();

}

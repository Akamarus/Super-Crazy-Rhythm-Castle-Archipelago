using System.Collections.ObjectModel;

namespace RhythmCastleAP;

internal enum CassetteSourceType { LevelEarnedReward, MusicLabPointChest }
internal sealed record CassetteTrigger(string Level, string Variant);
internal sealed record CassetteDefinition(string DisplaySong, string NativeSong, string ItemName, string SourceName, CassetteSourceType SourceType, bool ReusesExistingLocation, IReadOnlyList<CassetteTrigger> Triggers)
{
    internal string? Level => Triggers.Count == 0 ? null : Triggers[0].Level;
    internal string? Variant => Triggers.Count == 0 ? null : Triggers[0].Variant;
}

internal static class CassetteCatalog
{
    internal static IReadOnlyList<CassetteDefinition> All { get; }
    internal static IReadOnlyDictionary<string, CassetteDefinition> ByItemName { get; }
    internal static IReadOnlyDictionary<string, CassetteDefinition> ByNativeSong { get; }

    static CassetteCatalog()
    {
        All = Array.AsReadOnly(new[]
        {
            L("The Little Things","THE_LITTLE_THINGS","The Little Things Cassette","Cassette Source - The Little Things",("Level_24","LevelVariant_Default")),
            L("No Plan B","NO_PLAN_B","No Plan B Cassette","Cassette Source - No Plan B",("Level_22","LevelVariant_Default")),
            L("Jolt City","JOLT_CITY","Jolt City Cassette","Cassette Source - Jolt City",("Level_23","LevelVariant_Default")),
            L("Quieres Bailar","QUIERES_BAILAR","Quieres Bailar Cassette","Cassette Source - Quieres Bailar",("Level_16","LevelVariant_Default")),
            C("Quicksand","QUICKSAND","Music Lab - 32 Point Chest"),
            L("Gold","GOLD","Gold Cassette","Cassette Source - Gold",("Level_05","LevelVariant_Default"),("Level_11","LevelVariant_Default"),("Level_11","LevelVariant_DevilMode")),
            L("I Got Money","I_GOT_MONEY","Money Cassette","Level 2 - Money Cassette",true,("Level_06","LevelVariant_Default"),("Level_06","LevelVariant_BeeMode")),
            L("Hippo and Frog","HIPPO_AND_FROG","Hippo and Frog Cassette","Cassette Source - Hippo and Frog",("Level_07","LevelVariant_Default")),
            L("On the Way","ON_THE_WAY","On the Way Cassette","Cassette Source - On the Way",("Level_08","LevelVariant_Default"),("Level_11","LevelVariant_Default"),("Level_11","LevelVariant_DevilMode")),
            L("Badass","BADASS","Badass Cassette","Cassette Source - Badass",("Level_09","LevelVariant_Default")),
            L("Heavy Metal","HEAVY_METAL","Heavy Metal Cassette","Cassette Source - Heavy Metal",("Level_09","LevelVariant_Default")),
            L("AOK","AOK","AOK Cassette","Cassette Source - AOK",("Level_02","LevelVariant_Default"),("Level_02","LevelVariant_DevilMode")),
            L("Rainbow Melodies","RAINBOW_MELODIES","Rainbow Melodies Cassette","Cassette Source - Rainbow Melodies",("Level_11","LevelVariant_Default"),("Level_11","LevelVariant_DevilMode"),("Level_19","LevelVariant_Default")),
            L("Sneaking","SNEAKING_LOOP","Sneaking Cassette","Cassette Source - Sneaking",("Level_19","LevelVariant_Default"),("Level_20","LevelVariant_Default")),
            L("The Heist","THE_HEIST","The Heist Cassette","Cassette Source - The Heist",("Level_20","LevelVariant_Default")),
            L("Money","MONEY_DUB","Money Dub Cassette","Cassette Source - Money",("Level_01","LevelVariant_Default")),
            L("Lets Go","LETS_GO","Lets Go Cassette","Cassette Source - Lets Go",("Level_12","LevelVariant_Default"),("Level_12","LevelVariant_BeeMode")),
            L("Bounce","BOUNCE","Bounce Cassette","Cassette Source - Bounce",("Level_15","LevelVariant_Default")),
            L("Epical","THE_EPICAL","Epical Cassette","Cassette Source - Epical",("Level_21","LevelVariant_Default")),
            L("Hollywood Trailer","HOLLYWOOD_TRAILER","Hollywood Trailer Cassette","Cassette Source - Hollywood Trailer",("Level_21","LevelVariant_Default")),
            L("False Data","FALSE_DATA","False Data Cassette","Cassette Source - False Data",("Level_21","LevelVariant_Default")),
            L("Gotta Get Up","GOTTA_GET_UP","Gotta Get Up Cassette","Cassette Source - Gotta Get Up",("Level_03","LevelVariant_Default")),
            L("Fumblin Around","FUMBLIN_AROUND","Fumblin Around Cassette","Cassette Source - Fumblin Around",("Level_13","LevelVariant_Default"),("Level_13","LevelVariant_DevilMode")),
            L("Party Non Stop","PARTY_NON_STOP","Party Non Stop Cassette","Cassette Source - Party Non Stop",("Level_25","LevelVariant_Default")),
            L("Keep On Hustlin","KEEP_ON_HUSTLIN","Keep On Hustlin Cassette","Cassette Source - Keep On Hustlin",("Level_14","LevelVariant_Default"),("Level_14","LevelVariant_DevilMode")),
            L("Another Day In Paradise","ANOTHER_DAY_IN_PARADISE","Another Day In Paradise Cassette","Cassette Source - Another Day In Paradise",("Level_28","LevelVariant_Default")),
            C("Flamenco","FLAMENCO","Music Lab - 64 Point Chest"),
            C("Ten-Four Good Buddy","TEN_FOUR_GOOD_BUDDY","Music Lab - 89 Point Chest"),
            C("Zen","ZEN","Music Lab - 111 Point Chest"),
            C("Wiggle","WIGGLE","Music Lab - 140 Point Chest"),
        });
        ValidateUnique("display song", All.Select(x=>x.DisplaySong));
        ValidateUnique("native song", All.Select(x=>x.NativeSong));
        ValidateUnique("item name", All.Select(x=>x.ItemName));
        ValidateUnique("source identity", All.Select(x=>x.SourceName));
        if (All.Count != 30) throw new InvalidOperationException($"cassette catalog count is {All.Count}, expected 30");
        ByItemName = new ReadOnlyDictionary<string,CassetteDefinition>(All.ToDictionary(x=>x.ItemName,StringComparer.Ordinal));
        ByNativeSong = new ReadOnlyDictionary<string,CassetteDefinition>(All.ToDictionary(x=>x.NativeSong,StringComparer.Ordinal));
    }

    internal static IReadOnlyList<CassetteDefinition> ForLevelSource(string? level, string? variant) =>
        All.Where(x=>x.SourceType==CassetteSourceType.LevelEarnedReward && x.Triggers.Any(t=>string.Equals(t.Level,level,StringComparison.OrdinalIgnoreCase)&&string.Equals(t.Variant,variant,StringComparison.OrdinalIgnoreCase))).ToArray();

    private static CassetteDefinition L(string d,string n,string i,string s,params (string Level,string Variant)[] t)=>L(d,n,i,s,false,t);
    private static CassetteDefinition L(string d,string n,string i,string s,bool reused,params (string Level,string Variant)[] t)=>new(d,n,i,s,CassetteSourceType.LevelEarnedReward,reused,Array.AsReadOnly(t.Select(x=>new CassetteTrigger(x.Level,x.Variant)).ToArray()));
    private static CassetteDefinition C(string d,string n,string s)=>new(d,n,$"{d} Cassette",s,CassetteSourceType.MusicLabPointChest,true,Array.Empty<CassetteTrigger>());
    private static void ValidateUnique(string label,IEnumerable<string> values) { var duplicate=values.GroupBy(x=>x,StringComparer.Ordinal).FirstOrDefault(g=>g.Count()>1); if(duplicate!=null) throw new InvalidOperationException($"duplicate cassette {label}: {duplicate.Key}"); }
}

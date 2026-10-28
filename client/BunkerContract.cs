using Newtonsoft.Json.Linq;
namespace RhythmCastleAP;
internal sealed record BunkerItemDefinition(string Name, long Id, string BagFlag);
internal sealed record BunkerCheckDefinition(string Name, long Id, string Flag);
internal static class BunkerCatalog
{
    internal const string TrueEndingFlag = "OVERALL_PROGRESS_BEAT_KING_TWO";
    internal static readonly BunkerItemDefinition[] Items = {
        new("Bunker Keycard",187256179,"LIFT_KEY_BAG_ITEM"),
        new("Demon Key",187256180,"BUNKER_HUB_DEMON_KEY"),
        new("Virus",187256181,"KING_VIRUS_DATA_STICK_BAG_ITEM"),
        new("Cheat Codes",187256182,"KING_VIRUS_CHEAT_CODES_BAG_ITEM"),
        new("Game Ender",187256183,"KING_VIRUS_GAME_ENDER_BAG_ITEM"),
    };
    internal static readonly BunkerCheckDefinition[] Checks = {
        new("Lift Quest - Virus Retrieved",187256336,"OVERALL_PROGRESS_RETRIEVED_VIRUS"),
        new("School Trip - Cheat Codes Retrieved",187256337,"OVERALL_PROGRESS_RETRIEVED_CHEAT_CODES"),
        new("Meat Dimension - Game Ender Created",187256338,"OVERALL_PROGRESS_CREATED_GAME_ENDER"),
        new("Central Mainframe - Game Ender Used",187256339,"OVERALL_PROGRESS_USED_GAME_ENDER"),
        new("Secret Bunker - Demon Key Pickup",187256340,"BUNKER_HUB_DEMON_KEY"),
        new("True Ending - Completion",187256341,TrueEndingFlag),
    };
}
internal static class BunkerContract
{
    internal static bool Validate(JObject root)
    {
        try {
            bool active=root["schema_version"]?.Type==JTokenType.Integer && (long)root["schema_version"]! ==21;
            string? version=root["implementation_version"]?.Type==JTokenType.String?(string?)root["implementation_version"]:null;
            bool marker=version?.Contains("-bunker-1",StringComparison.Ordinal)==true;
            string[] fields={"bunker_progression","goal","bunker_item_ids","bunker_check_ids","true_ending_native_flag"};
            if(!active && !marker && fields.All(f=>root[f]==null))return true;
            if(marker!=active || root["bunker_progression"]?.Type!=JTokenType.Boolean || (bool)root["bunker_progression"]! !=active)return false;
            if(root["goal"]?.Type!=JTokenType.String || (string?)root["goal"] is not ("king_ferdinand" or "true_ending"))return false;
            if(!active && (string?)root["goal"]!="king_ferdinand")return false;
            if(root["true_ending_native_flag"]?.Type!=JTokenType.String || (string?)root["true_ending_native_flag"]!=(active?BunkerCatalog.TrueEndingFlag:""))return false;
            return Map(root["bunker_item_ids"],active?BunkerCatalog.Items.Select(i=>(i.Name,i.Id)):Array.Empty<(string,long)>()) &&
                Map(root["bunker_check_ids"],active?BunkerCatalog.Checks.Select(i=>(i.Name,i.Id)):Array.Empty<(string,long)>());
        } catch {return false;}
    }
    private static bool Map(JToken? token,IEnumerable<(string Name,long Id)> expected)
    {
        var entries=expected.ToArray();
        return token is JObject map && map.Count==entries.Length && entries.All(e=>map[e.Name]?.Type==JTokenType.Integer && (long?)map[e.Name]==e.Id);
    }
}

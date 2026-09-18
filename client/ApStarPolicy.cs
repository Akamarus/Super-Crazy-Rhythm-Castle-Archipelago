using Newtonsoft.Json.Linq;
namespace RhythmCastleAP;

internal enum ApStarMode { Native, Awaiting, Ready, Disconnected, Incompatible }
internal sealed record ApStarSettings(ApStarMode Mode, int Goal = 0,
    IReadOnlyDictionary<string,int>? Gates = null, IReadOnlyDictionary<string,int>? Eaters = null,
    string Detail = "");
internal sealed record ApStarResult(string Identity, string NativeSave, bool Qualified, long Attempt);
internal static class ApStarContract
{
    internal const string Implementation = "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22-music-lab-points-0.23-full-level-mapping-0.24-character-quest-items-0.25-quest-checks-0.26-check-expansion-0.27-ap-stars-0.28";
    internal const long StarId = 187256118;
    internal static readonly string[] EaterAreas = {"Roots","Lobby","Cell Tower","Royal Corridor","Secret Bunker"};
    internal static ApStarSettings Validate(Dictionary<string,object>? data)
    {
        if (data == null) return new(ApStarMode.Native);
        try
        {
            JObject obj = JObject.FromObject(data);
            string? version = obj["implementation_version"]?.Type == JTokenType.String ? (string?)obj["implementation_version"] : null;
            bool claims = obj["schema_version"]?.Type == JTokenType.Integer && (long)obj["schema_version"]! >= 19 ||
                obj["star_items_active"]?.Type == JTokenType.Boolean && (bool)obj["star_items_active"]! ||
                obj.ContainsKey("star_victory_schema") || obj.ContainsKey("post_threshold_victory_active");
            if (!claims && version != null && Implementation.StartsWith(version + "-",StringComparison.Ordinal))
                return new(ApStarMode.Native);
            if (version != Implementation || Number(obj,"schema_version") != 19 || Number(obj,"star_victory_schema") != 1 ||
                Number(obj,"campaign_level_mapping_schema") != 1 || Number(obj,"star_item_id") != StarId ||
                Number(obj,"star_item_count") != 66 || Number(obj,"star_item_maximum") != 66 ||
                (string?)obj["star_item_name"] != "Star" || (string?)obj["victory_level_internal_id"] != "Level_28" ||
                !True(obj,"star_items_active") || !True(obj,"client_star_gate_enforcement_active") || !True(obj,"post_threshold_victory_active"))
                return new(ApStarMode.Incompatible,Detail:"AP Star contract identity or enabled fields mismatch");
            long? goal = Number(obj,"required_stars");
            if (goal is null or <1 or >66) return new(ApStarMode.Incompatible,Detail:"Invalid Star goal");
            var gates = Map(obj["generated_star_requirements"],Enumerable.Range(1,22).Select(n=>"Level "+n),0,(int)goal-1);
            var eaters = Map(obj["star_eater_requirements"],EaterAreas,0,66);
            if (gates == null || eaters == null || gates["Level 1"] != 0)
                return new(ApStarMode.Incompatible,Detail:"Incomplete, out-of-range or non-opening Star gates");
            return new(ApStarMode.Awaiting,(int)goal,gates,eaters,"compatible");
        }
        catch { return new(ApStarMode.Incompatible,Detail:"Malformed AP Star contract"); }
    }
    private static long? Number(JObject obj,string name) => obj[name]?.Type == JTokenType.Integer ? (long?)obj[name] : null;
    private static bool True(JObject obj,string name) => obj[name]?.Type == JTokenType.Boolean && (bool)obj[name]!;
    private static Dictionary<string,int>? Map(JToken? token,IEnumerable<string> expected,int min,int max)
    {
        if(token is not JObject map) return null;
        var names=expected.ToHashSet(StringComparer.Ordinal);
        if(!names.SetEquals(map.Properties().Select(p=>p.Name))) return null;
        var result=new Dictionary<string,int>(StringComparer.Ordinal);
        foreach(var p in map.Properties())
        {
            if(p.Value.Type!=JTokenType.Integer) return null;
            long value=(long)p.Value;if(value<min||value>max)return null;result.Add(p.Name,(int)value);
        }
        return result;
    }
}

internal sealed class ApStarState
{
    private readonly object _sync=new();
    private long _generation, _attempt, _consumedAttempt;
    private string _identity="";
    private int _total;
    private bool _synced, _goalPending, _goalSent;
    private ApStarSettings _settings=new(ApStarMode.Native);
    private ApStarMode _mode=ApStarMode.Native;
    internal ApStarMode Mode {get{lock(_sync)return _mode;}}
    internal bool Active {get{lock(_sync)return _mode!=ApStarMode.Native;}}
    internal int Total {get{lock(_sync)return _total;}}
    internal string Identity {get{lock(_sync)return _identity;}}
    internal ApStarSettings Settings {get{lock(_sync)return _settings;}}
    internal bool GoalPending {get{lock(_sync)return _goalPending&&!_goalSent;}}
    internal void Configure(long generation,string identity,ApStarSettings settings)
    {
        lock(_sync)
        {
            if(generation<_generation)return;
            bool same=_identity==identity && _settings.Goal==settings.Goal && settings.Mode==ApStarMode.Awaiting && _settings.Mode==ApStarMode.Awaiting;
            _generation=generation;_identity=identity;_settings=settings;
            if(same)_goalSent=false; // Goal status is idempotently resent on reconnect.
            if(!same){_total=0;_synced=false;_goalPending=_goalSent=false;_consumedAttempt=_attempt;}
            _mode=settings.Mode==ApStarMode.Awaiting && _synced ? ApStarMode.Disconnected : settings.Mode;
        }
    }
    internal void Publish(long generation,IEnumerable<long> history)
    {
        lock(_sync)
        {
            if(generation!=_generation||_settings.Mode!=ApStarMode.Awaiting)return;
            _total=Math.Min(66,history.Count(id=>id==ApStarContract.StarId));_synced=true;_mode=ApStarMode.Ready;
        }
    }
    internal void Disconnect(long generation){lock(_sync)if(generation==_generation&&_settings.Mode==ApStarMode.Awaiting)_mode=_synced?ApStarMode.Disconnected:ApStarMode.Awaiting;}
    internal void Reset(){lock(_sync){_generation=0;_identity="";_total=0;_synced=_goalPending=_goalSent=false;_settings=new(ApStarMode.Native);_mode=ApStarMode.Native;_consumedAttempt=_attempt;}}
    internal int ResolveTotal(int native){lock(_sync)return _mode==ApStarMode.Native?native:(_synced?_total:0);}
    internal int ResolveHud(string room,int native)=>room=="GameRoom_Hub6"?native:ResolveTotal(native);
    internal int? Requirement(string level,string variant)
    {
        lock(_sync)
        {
            if(_mode==ApStarMode.Native||variant!="LevelVariant_Default"||!CampaignLevelCatalog.TryGet(level,out var entry))return null;
            return _settings.Gates?.GetValueOrDefault("Level "+entry.Number);
        }
    }
    internal bool CanEnter(string level,string variant)
    {
        lock(_sync)
        {
            if(_mode==ApStarMode.Native)return true;
            if(string.IsNullOrEmpty(level)||string.IsNullOrEmpty(variant)||level.StartsWith("<")||variant.StartsWith("<"))return false;
            if(variant!="LevelVariant_Default"||!CampaignLevelCatalog.TryGet(level,out var entry))return true;
            return _synced&&_settings.Gates?.TryGetValue("Level "+entry.Number,out int required)==true&&_total>=required;
        }
    }
    internal ApStarResult? Capture(string nativeSave,string level,string variant,int? stars)
    {
        lock(_sync)
        {
            if(_settings.Mode!=ApStarMode.Awaiting||level!="Level_28"||variant!="LevelVariant_Default")return null;
            return new(_identity,nativeSave,_synced&&_total>=_settings.Goal&&stars>=1,++_attempt);
        }
    }
    internal bool Commit(ApStarResult? result,string nativeSave)
    {
        lock(_sync)
        {
            if(result==null||result.Identity!=_identity||result.NativeSave!=nativeSave||result.Attempt<=_consumedAttempt||result.Attempt>_attempt||_settings.Mode!=ApStarMode.Awaiting)return false;
            _consumedAttempt=result.Attempt;
            if(!result.Qualified||_goalPending||_goalSent)return false;
            _goalPending=true;return true;
        }
    }
    internal void RestoreGoal(string identity){lock(_sync)if(identity==_identity&&_settings.Mode==ApStarMode.Awaiting)_goalPending=true;}
    internal void MarkGoalSent(string identity){lock(_sync)if(identity==_identity&&_goalPending)_goalSent=true;}
}

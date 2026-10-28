using Newtonsoft.Json.Linq;
using RhythmCastleAP;
internal static class BunkerContractTests
{
 internal static void Run(Dictionary<string,object> data)
 {
  void Check(bool ok,string name){if(!ok)throw new Exception("Bunker: "+name);}
  JObject Fresh()=>JObject.FromObject(data);
  bool Accepted(JObject o)=>ApStarContract.Validate(o.ToObject<Dictionary<string,object>>()!).Mode==ApStarMode.Awaiting;
  Check(Accepted(Fresh()),"JSON metadata");
  Check(!ApStarContract.Validate(data,bunkerReady:false).BunkerEnabled,"missing runtime capability");
  Check(ApStarContract.Validate(data,bunkerReady:false).Mode==ApStarMode.Incompatible,"hooks required");
  var result=ApStarContract.Validate(data);Check(result.BunkerEnabled&&result.Ending=="true_ending","selected ending exposed");
  foreach(var field in new[]{"bunker_item_ids","bunker_check_ids"}) {
   var source=(JObject)Fresh()[field]!;
   foreach(var key in source.Properties().Select(p=>p.Name)) {
    var missing=Fresh();((JObject)missing[field]!).Remove(key);Check(!Accepted(missing),"missing "+key);
    foreach(JToken value in new JToken[]{new JValue("187256179"),new JValue(true),new JValue(187256179.0),JValue.CreateNull(),new JValue(long.MaxValue)}) {
     var bad=Fresh();bad[field]![key]=value.DeepClone();Check(!Accepted(bad),"bad ID "+key);
    }
   }
   var extra=Fresh();extra[field]!["Unexpected"]=0;Check(!Accepted(extra),"extra "+field);
  }
  foreach(var field in new[]{"goal","true_ending_native_flag","bunker_progression"}) {
   foreach(JToken value in new JToken[]{new JValue(1),JValue.CreateNull(),new JArray(),new JValue("wrong")}) {var bad=Fresh();bad[field]=value;Check(!Accepted(bad),"bad metadata "+field);}
  }
  var normal=Fresh();normal["goal"]="king_ferdinand";Check(Accepted(normal),"normal goal supported");
  var duplicate=Fresh();duplicate["implementation_version"]=((string)duplicate["implementation_version"]!).Replace("-bunker-1","-bunker-1-bunker-1");Check(!Accepted(duplicate),"duplicate marker rejected");
  var min=Fresh();min["required_stars"]=2;foreach(var gate in ((JObject)min["generated_star_requirements"]!).Properties())gate.Value=gate.Name=="Level 1"?0:1;min["star_eater_requirements"]!["Secret Bunker"]=0;Check(Accepted(min),"minimum two-Star seed");
  min["generated_star_requirements"]!["Level 22"]=0;Check(!Accepted(min),"zero final gate rejected");
  var legacy=Fresh();legacy["schema_version"]=19;legacy["implementation_version"]=ApStarContract.Implementation;legacy["bunker_progression"]=false;legacy["goal"]="king_ferdinand";legacy["bunker_item_ids"]=new JObject();legacy["bunker_check_ids"]=new JObject();legacy["true_ending_native_flag"]="";Check(Accepted(legacy),"explicit disabled legacy metadata");
  legacy["goal"]="true_ending";Check(!Accepted(legacy),"legacy cannot select True Ending");
  Console.WriteLine("Bunker metadata rejection and boundary tests passed.");
 }
}

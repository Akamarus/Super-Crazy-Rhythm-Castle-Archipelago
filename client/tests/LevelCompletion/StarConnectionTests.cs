using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Enums;
namespace RhythmCastleAP;
internal static partial class ConnectionTests
{
    private static Dictionary<string,object> StarContract(Func<Dictionary<string,object>> campaign)
    {
        var data=campaign();
        data["implementation_version"]=ApStarContract.Implementation; data["schema_version"]=19;
        data["star_victory_schema"]=1; data["star_item_name"]="Star"; data["star_item_id"]=187256118;
        data["star_item_count"]=66; data["star_item_maximum"]=66; data["required_stars"]=2;
        data["star_items_active"]=true; data["client_star_gate_enforcement_active"]=true;
        data["post_threshold_victory_active"]=true; data["victory_level_internal_id"]="Level_28";
        data["generated_star_requirements"]=Enumerable.Range(1,22).ToDictionary(n=>"Level "+n,_=>0);
        data["star_eater_requirements"]=new Dictionary<string,int>{{"Roots",0},{"Lobby",0},{"Cell Tower",0},{"Royal Corridor",0},{"Secret Bunker",66}};
        data["character_quest_item_schema"]=1; data["randomize_character_quest_items"]=true;
        data["character_quest_items"]=CharacterQuestItemPolicy.All.ToDictionary(e=>e.Name,e=>e.BagFlag);
        data["character_quest_item_locations"]=CharacterQuestItemPolicy.All.ToDictionary(e=>e.Name,e=>e.Location);
        data["quest_checks_schema"]=1; data["quest_items"]=QuestChecksPolicy.Items; data["quest_locations"]=QuestChecksPolicy.Locations;
        data["expanded_checks_schema"]=1; data["expanded_check_locations"]=ExpandedCheckCatalog.StarEntries.ToDictionary(e=>e.Name,e=>e.Id);
        return data;
    }
    private static ReceivedItemsPacket Stars(int index,int count)=>new(){Index=index,Items=Enumerable.Range(0,count).Select(_=>new NetworkItem{Item=ApStarContract.StarId}).ToArray()};
    private static void RunStars(Func<Dictionary<string,object>> campaign)
    {
        // Each independent client below represents a new app lifetime; retain real state across its reconnects.
        ExpandedChecks.State=new();
        var client=new ArchipelagoClient("test","slot","",false);
        var first=Login(StarContract(campaign));
        first.BeforeLoginReturns=()=>first.Socket.Receive(Stars(0,1));
        Connect(client,first,true);
        string identity=Plugin.GameName+"|seed|0|1";
        Check(ApStarMode.Ready,ApStars.State.Mode,"schema19 login applies prelogin complete packet history");
        Check(1,ApStars.State.Total,"one prelogin Star counted once");
        Check(true,ExpandedChecks.State.Handles(187256335),"schema19 enables new source scope at production login");
        var early=ApStars.State.Capture("native1","Level_28","LevelVariant_Default",1);
        first.Socket.Receive(Stars(1,1));
        Check(2,ApStars.State.Total,"incremental production packet updates Star count");
        Check(false,ApStars.State.Commit(early,"native1"),"receipt after early clear cannot qualify earlier result");
        Check(false,client.SendStarGoal(identity),"received Stars alone never send goal");
        Check(0,first.Socket.SentPackets.Count,"no goal packet before qualifying persisted clear");
        Check(true,ApStars.State.Commit(ApStars.State.Capture("native1","Level_28","LevelVariant_Default",1),"native1"),"real state accepts qualified persisted clear");
        Check(false,client.SendStarGoal(Plugin.GameName+"|other|0|1"),"goal rejects mismatching expected identity");
        first.Socket.Close();
        Check(false,client.SendStarGoal(identity),"offline qualified clear waits for authenticated transport");
        var reconnect=Login(StarContract(campaign));
        reconnect.BeforeLoginReturns=()=>reconnect.Socket.Receive(Stars(0,2));
        Connect(client,reconnect,true);
        Check(true,ApStars.State.GoalPending,"same identity reconnect retains qualified pending goal");
        first.Socket.Receive(Stars(0,66));
        Check(2,ApStars.State.Total,"retired transport cannot publish stale Star history");
        Check(true,client.SendStarGoal(identity),"current authenticated identity sends qualified goal");
        Check(1,reconnect.Socket.SentPackets.Count,"one goal packet sent on current transport");
        Check(ArchipelagoClientState.ClientGoal,((StatusUpdatePacket)reconnect.Socket.SentPackets.Single()).Status,"goal uses real AP client status packet");
        ApStars.State.MarkGoalSent(identity);
        Check(false,client.SendStarGoal(identity),"acknowledged goal cannot send again");
        var other=Login(StarContract(campaign),seed:"different");
        Connect(client,other,true);
        Check(0,ApStars.State.Total,"new identity does not inherit Stars");
        Check(false,ApStars.State.GoalPending,"new identity does not inherit pending goal");
        reconnect.Socket.Receive(Stars(0,66));
        Check(0,ApStars.State.Total,"previous identity packet ignored by production lease");
        other.Socket.Receive(Stars(0,2));
        Check(2,ApStars.State.Total,"new identity accepts its own authoritative history");
        Check(true,ApStars.State.Commit(ApStars.State.Capture("native2","Level_28","LevelVariant_Default",1),"native2"),"new identity can earn own qualifying result");
        Check(false,client.SendStarGoal(identity),"old expected identity cannot send new pending goal");
        Check(true,client.SendStarGoal(Plugin.GameName+"|different|0|1"),"new identity can send own goal");
        client.Shutdown();
        other.Socket.Receive(Stars(0,66));
        Check(ApStarMode.Native,ApStars.State.Mode,"shutdown prevents late history reviving Stars");
        Check(false,client.SendStarGoal(identity),"shutdown cannot send goal");

        var oldData=StarContract(campaign);
        oldData["schema_version"]=18;oldData["implementation_version"]=ApStarContract.Implementation[..^14];
        oldData.Remove("star_victory_schema");oldData.Remove("post_threshold_victory_active");
        oldData["star_items_active"]=false;oldData["client_star_gate_enforcement_active"]=false;
        oldData["expanded_check_locations"]=ExpandedCheckCatalog.Entries.ToDictionary(e=>e.Name,e=>e.Id);
        ExpandedChecks.State=new();
        var oldClient=new ArchipelagoClient("test","slot","",false);var old=Login(oldData);
        old.BeforeLoginReturns=()=>old.Socket.Receive(Stars(0,66));Connect(oldClient,old,true);
        Check(ApStarMode.Native,ApStars.State.Mode,"schema18 production login retains native Star mode");
        Check(false,ExpandedChecks.State.Handles(187256335),"schema18 production login excludes new checks");
        Check(false,oldClient.SendStarGoal(identity),"schema18 does not send AP Star victory");oldClient.Shutdown();

        ApStarNativeHooks.Ready=false;
        var noHooks=new ArchipelagoClient("test","slot","",false);Connect(noHooks,Login(StarContract(campaign)),false);
        Check(false,noHooks.Connected,"missing required native hooks reject schema19 login");noHooks.Shutdown();ApStarNativeHooks.Ready=true;
        var invalid=StarContract(campaign);invalid["star_item_count"]=65;
        var malformed=new ArchipelagoClient("test","slot","",false);Connect(malformed,Login(invalid),false);
        Check(false,malformed.Connected,"malformed Star contract rejected through production login");malformed.Shutdown();
        Console.WriteLine("PASS: production schema19 login, receipt history, stale transport isolation and qualified identity-scoped goal packets; schema18 unchanged");
    }
}

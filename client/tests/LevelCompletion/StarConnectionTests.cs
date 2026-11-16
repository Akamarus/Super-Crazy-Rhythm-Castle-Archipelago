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
    private static Dictionary<string, object> BunkerSlotData(Func<Dictionary<string, object>> campaign)
    {
        var data = StarContract(campaign);
        data["schema_version"] = 21;
        data["implementation_version"] = ApStarContract.Implementation.Replace("-generation-foundation", "-bunker-1-generation-foundation");
        data["bunker_progression"] = true;
        data["goal"] = "true_ending";
        data["true_ending_native_flag"] = BunkerCatalog.TrueEndingFlag;
        data["bunker_item_ids"] = BunkerCatalog.Items.ToDictionary(item => item.Name, item => item.Id);
        data["bunker_check_ids"] = BunkerCatalog.Checks.ToDictionary(check => check.Name, check => check.Id);
        ((Dictionary<string, int>)data["generated_star_requirements"])["Level 22"] = 1;
        ((Dictionary<string, int>)data["star_eater_requirements"])["Secret Bunker"] = 0;
        data["cassette_bundle_size"] = 1;
        data["cassette_bundles"] = new Dictionary<string, string[]>();
        return data;
    }
    private static ReceivedItemsPacket Stars(int index,int count)=>new(){Index=index,Items=Enumerable.Range(0,count).Select(_=>new NetworkItem{Item=ApStarContract.StarId}).ToArray()};
    private static void ReloadInSameProcess(Func<Dictionary<string, object>> campaign)
    {
        var data = StarContract(campaign);
        data["randomize_game_garage_cartridges"] = true;
        var client = NewClient();
        var first = Login(data, seed: "reload-A");
        Connect(client, first, true);
        first.Socket.Receive(new ReceivedItemsPacket { Index = 0, Items =
            [new NetworkItem { Item = 187256116 }, new NetworkItem { Item = ApStarContract.StarId }] });
        Check(true, WeedKillerRandomization.History.Owned, "room A owns its Weed Killer");
        Check(1, ApStars.State.Total, "room A owns one Star");
        GarageCartridgeAccess.InsertionCoordinator.NoteInserted("Superstar");
        Check(1, first.GarageWrites, "room A has a pending insertion after its write fails");
        client.Shutdown();

        // Unloading before any login must never advance a subsystem past the next connection.
        for (int i = 0; i < 3; i++) NewClient().Shutdown();
        var sameClient = NewClient();
        var same = Login(data, seed: "reload-A");
        Connect(sameClient, same, true, "first Garage login after a same-process reload");
        Check(true, ExpandedChecks.State.Handles(187256335), "first reload enables expanded checks after idle unloads");
        Check(1, same.GarageWrites, "reload into room A retries its own pending insertion");
        sameClient.Shutdown();

        var refusedClient = NewClient();
        Connect(refusedClient, Login(data, success: false, seed: "reload-B"), false);
        refusedClient.Shutdown();
        var otherClient = NewClient();
        var other = Login(data, seed: "reload-B");
        Connect(otherClient, other, true, "first Garage login after a refused attempt in the previous lifetime");
        Check(0, other.GarageWrites, "room B receives no pending write from room A");
        Check(GarageInsertionServerValue.NotInserted, GarageCartridgeAccess.InsertionCoordinator.GetServerValue("Superstar"),
            "room B starts with its own insertion state");
        Check(false, WeedKillerRandomization.History.Owned, "room B starts without the previous room's Weed Killer");
        other.Socket.Receive(new ReceivedItemsPacket { Index = 0, Items = [new NetworkItem { Item = 187256116 }] });
        Check(true, WeedKillerRandomization.History.Owned, "the new room's item history is live after reload");
        first.Socket.Receive(Stars(0, 66));
        same.Socket.Close();
        bool loginRetried = false;
        other.BeforeLoginReturns = () => loginRetried = true;
        client.Connect();
        Check(false, loginRetried, "a delayed connect on the retired client cannot log in after reload");
        Check(true, otherClient.Connected, "retired socket close leaves the new room connected");
        Check(0, ApStars.State.Total, "retired socket packet cannot publish old room Stars");
        Check(true, WeedKillerRandomization.History.Owned, "retired callback cannot revoke new room ownership");
        Check(Plugin.GameName + "|reload-B|0|1", ExpandedChecks.State.Identity, "expanded checks belong to room B");
        otherClient.Shutdown();
    }
    private static void RunStars(Func<Dictionary<string,object>> campaign)
    {
        var client=NewClient();
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
        var oldClient=NewClient();var old=Login(oldData);
        old.BeforeLoginReturns=()=>old.Socket.Receive(Stars(0,66));Connect(oldClient,old,true);
        Check(ApStarMode.Native,ApStars.State.Mode,"schema18 production login retains native Star mode");
        Check(false,ExpandedChecks.State.Handles(187256335),"schema18 production login excludes new checks");
        Check(false,oldClient.SendStarGoal(identity),"schema18 does not send AP Star victory");oldClient.Shutdown();

        ApStarNativeHooks.Ready=false;
        var noHooks=NewClient();Connect(noHooks,Login(StarContract(campaign)),false);
        Check(false,noHooks.Connected,"missing required native hooks reject schema19 login");noHooks.Shutdown();ApStarNativeHooks.Ready=true;
        var routeData=StarContract(campaign);
        routeData["implementation_version"]=ApStarContract.Implementation.Replace("-generation-foundation", "-cell-tower-path-1-generation-foundation");
        routeData["cell_tower_open_route"]=true;
        CellTowerPathPatches.Ready=false;
        var noRoute=NewClient();Connect(noRoute,Login(routeData),false);
        Check(false,noRoute.Connected,"new route rejects missing native path hook at production login");noRoute.Shutdown();
        CellTowerPathPatches.Ready=true;
        var withRoute=NewClient();Connect(withRoute,Login(routeData),true);
        Check(true,withRoute.Connected,"new route accepted with installed native path hook");
        Check(true,ExpandedChecks.State.Handles(187256335),"new route retains expanded checks");withRoute.Shutdown();
        var invalid=StarContract(campaign);invalid["star_item_count"]=65;
        var malformed=NewClient();Connect(malformed,Login(invalid),false);
        Check(false,malformed.Connected,"malformed Star contract rejected through production login");malformed.Shutdown();
        var bundles=StarContract(campaign);
        bundles["schema_version"]=20;
        bundles["cassette_bundle_size"]=2;
        bundles["cassette_bundles"]=Enumerable.Range(0,15).ToDictionary(n=>$"Cassette Bundle {n+1:00}",n=>CassetteCatalog.All.Skip(n*2).Take(2).Select(c=>c.ItemName).ToArray());
        var bundleClient=NewClient();
        var bundleSession=Login(bundles);
        bundleSession.BeforeLoginReturns=()=>bundleSession.Socket.Receive(new ReceivedItemsPacket{Index=0,Items=new[]{new NetworkItem{Item=187256164}}});
        Connect(bundleClient,bundleSession,true);
        CassetteBundleSessionHistory.WithSongs((managed,songs)=> { Check(true,managed,"schema20 enables managed bundle ownership at real login");Check(2,songs?.Count,"real prelogin receipt expands after authentication"); });
        Check(ApStarMode.Ready,ApStars.State.Mode,"schema20 retains authoritative Star contract");
        Check(true,ExpandedChecks.State.Handles(187256335),"schema20 retains expanded check scope");
        bundleClient.Shutdown();
        CassetteBundleSessionHistory.WithSongs((managed,songs)=>Check(true,songs == null,"production shutdown suspends bundle authority"));
        bundles.Remove("cassette_bundles");
        var invalidBundles=NewClient();Connect(invalidBundles,Login(bundles),false);
        Check(false,invalidBundles.Connected,"schema20 missing bundle map rejects real login");invalidBundles.Shutdown();
        var bunker=BunkerSlotData(campaign);
        BunkerRuntime.Ready=false;var missingBunker=NewClient();Connect(missingBunker,Login(bunker),false);missingBunker.Shutdown();BunkerRuntime.Ready=true;
        var bunkerClient=NewClient();var bunkerSession=Login(bunker);
        bunkerSession.BeforeLoginReturns=()=>bunkerSession.Socket.Receive(new ReceivedItemsPacket{Index=0,Items=new[]{new NetworkItem{Item=187256179}}});
        Connect(bunkerClient,bunkerSession,true);Check(true,BunkerRuntime.State.Ready,"schema21 publishes authenticated Bunker history");
        Check(true,BunkerRuntime.State.Bind(2,_=>false,BunkerRuntime.Journal.Save),"fresh native slot binds for schema21");
        Check(true,BunkerRuntime.State.HasUnconsumedReceipt(187256179),"production connection delivers Keycard ownership");
        Check("true_ending",ApStars.State.Settings.Ending,"production connection preserves selected ending");
        bunkerSession.Socket.Receive(new ReceivedItemsPacket{Index=5,Items=Array.Empty<NetworkItem>()});Check(false,BunkerRuntime.State.Ready,"history gap suspends Bunker authority");
        bunkerClient.Shutdown();Check(false,BunkerRuntime.State.Enabled,"shutdown removes Bunker state");
        Console.WriteLine("PASS: production schema19 login, receipt history, stale transport isolation and qualified identity-scoped goal packets; schema18 unchanged");
    }
}

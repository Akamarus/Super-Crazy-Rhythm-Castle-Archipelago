using RhythmCastleAP;
static class Program {
 const string Battery="CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM", Memory="LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM";
 static readonly long[] Items={187256159,187256160};
 static readonly TimeSpan Beat=TimeSpan.FromSeconds(2);
 static int passed; static long generation; static string identity="";
 static void Check(bool value,string label){if(!value)throw new Exception(label);passed++;}
 static void Start(params long[] owned) {
  QuestChecks.State.Reset();CharacterQuestItems.State.Reset();
  generation++;identity="seed-"+Guid.NewGuid().ToString("N");
  RootsBucketRandomization.Flags.Clear();
  foreach(string f in new[]{Battery,Memory,"PLUNGER_BAG_ITEM","LOBBY_HUB_MEAT_DOOR_BLOCKER_PLUNGERED","OUTSIDE_HUB_PLUNGER_COLLECTED","ROOTS_HUB_STAR_EATER_FED","PLAYABLE_CHARACTER_UNLOCKED_MEOO"})RootsBucketRandomization.Flags[f]=false;
  CurrentPlayerSaveEnquiries.Unlocked.Clear();Plugin.AP.Queued.Clear();
  CassetteReceiptRandomization.Stable=true;CassetteReceiptRandomization.Slot=4;
  CassetteReceiptRandomization.Identity="native-"+Guid.NewGuid().ToString("N");CassetteReceiptRandomization.Processor=new();
  DeveloperHarness.CurrentRoomId="GameRoom_Hub6";WeedKillerRandomization.Grants=0;
  QuestChecks.State.Configure(generation,identity,true);QuestChecks.State.Publish(generation,owned,Array.Empty<long>());
  CharacterQuestItems.State.Configure(generation,true);CharacterQuestItems.State.Publish(generation,Items);
 }
 static void Tick(){QuestChecks.Tick(Beat);CharacterQuestItems.TickUnity(Beat);}
 static void Reload(params long[] owned) {
  var restored=QuestChecks.Journal.Load(identity);
  QuestChecks.State.Reset();CharacterQuestItems.State.Reset();generation++;
  CassetteReceiptRandomization.Identity="reload-"+Guid.NewGuid().ToString("N");
  QuestChecks.State.Configure(generation,identity,true,restored);QuestChecks.State.Publish(generation,owned,Array.Empty<long>());
  CharacterQuestItems.State.Configure(generation,true);CharacterQuestItems.State.Publish(generation,Items);
 }
 static void HandIn(string room,string flag,bool emitEvent=true) {
  DeveloperHarness.CurrentRoomId=room;
  Check(QuestChecks.BeforeProgressionRequest(new Request{Value=false},flag),"hand-in admitted");
  RootsBucketRandomization.Flags[flag]=false;
  if(emitEvent){var e=new Changed{FlagWasSet=true,FlagIsSet=false};CharacterQuestItems.RecordConsumption(e,flag);QuestChecks.Observe(e,flag);}
 }
 static void Main() {
  try {
   foreach (string freshnessFlag in new[] {"OUTSIDE_HUB_PLUNGER_COLLECTED", "ROOTS_HUB_STAR_EATER_FED"}) {
    Start(QuestChecksPolicy.Meoo);
    RootsBucketRandomization.Flags.Remove(freshnessFlag);
    Tick();
    Check(QuestChecks.State.NativeSlot==null,"unknown freshness must defer binding: "+freshnessFlag);
    Check(CassetteReceiptRandomization.Processor.Grants==0&&WeedKillerRandomization.Grants==0,"unknown freshness must defer native grants");
    RootsBucketRandomization.Flags[freshnessFlag]=false;
    Tick();
    Check(QuestChecks.State.NativeSlot==4,"readable fresh flags permit retry binding");
    Check(CurrentPlayerSaveEnquiries.IsCharacterUnlocked(ePlayableCharacter.GHOST_CAT),"deferred character grant recovers");
    RootsBucketRandomization.Flags.Remove(freshnessFlag);
    Check(QuestChecks.IsCurrentSaveBound(),"already bound save survives unavailable freshness read");
    QuestChecks.State.Publish(generation,new[]{QuestChecksPolicy.Meoo,QuestChecksPolicy.Maniac},Array.Empty<long>());
    Tick();
    Check(QuestChecks.State.NativeSlot==4,"reconciliation retains prior save binding");
    Check(CurrentPlayerSaveEnquiries.IsCharacterUnlocked(ePlayableCharacter.GUITAR_MANIAC),"already bound save still grants later receipts");

    Start(QuestChecksPolicy.Meoo);
    RootsBucketRandomization.Flags[freshnessFlag]=true;
    Tick();
    Check(QuestChecks.State.NativeSlot==null,"completed source must reject initial binding: "+freshnessFlag);
    Check(Plugin.AP.Queued.Count==0&&CassetteReceiptRandomization.Processor.Grants==0,"completed source cannot grant or queue on unbound save");
   }
   foreach(var target in new[] {
    ("Root/GameRoom_Hub1_Logic/GameRoom_Hub1A_Script/MiscSequences/UnlockGhostCat/UnlockGhostCatCharacter", ePlayableCharacter.GHOST_CAT),
    ("Root/GameRoom_27_Logic/GameRoom_27_Script/MiscSequences/UnlockManiac/UnlockManiacCharacter", ePlayableCharacter.GUITAR_MANIAC) }) {
    Start();Tick();
    // Native AcceptProcessor inlines the write, bypassing SuppressCharacter.
    if(!QuestCharacterRewardPolicy.SuppressNativeStep(QuestChecks.Enabled,QuestChecks.IsCurrentSaveBound(),target.Item1))
     CurrentPlayerSaveEnquiries.Unlocked.Add(target.Item2);
    Check(!CurrentPlayerSaveEnquiries.IsCharacterUnlocked(target.Item2),"vanilla sequence cannot leak randomized character: "+target.Item2);
    Check(!QuestCharacterRewardPolicy.SuppressNativeStep(false,true,target.Item1),"legacy sequence retained");
    Check(!QuestCharacterRewardPolicy.SuppressNativeStep(true,false,target.Item1),"unbound native save retained");
    Check(!QuestCharacterRewardPolicy.SuppressNativeStep(true,true,target.Item1+"Other"),"unrelated sequence retained");
   }
   Start(QuestChecksPolicy.Meoo,QuestChecksPolicy.Maniac);Tick();
   Check(CurrentPlayerSaveEnquiries.IsCharacterUnlocked(ePlayableCharacter.GHOST_CAT)&&CurrentPlayerSaveEnquiries.IsCharacterUnlocked(ePlayableCharacter.GUITAR_MANIAC),"AP character grants reach global native request");
   Check(RootsBucketRandomization.Flags[Battery]&&RootsBucketRandomization.Flags[Memory],"early character ownership must not consume quest items");
   Check(Plugin.AP.Queued.Count==0,"character receipts cannot complete hand-ins");
   Tick();Check(CassetteReceiptRandomization.Processor.Grants==2,"repeated reconciliation grants each character once");
   HandIn("GameRoom_Hub1A",Battery);HandIn("GameRoom_27",Memory);
   Check(Plugin.AP.Queued.Count==2,"two physical hand-ins queue distinct checks");
   Reload(QuestChecksPolicy.Meoo,QuestChecksPolicy.Maniac);Tick();
   Check(!RootsBucketRandomization.Flags[Battery]&&!RootsBucketRandomization.Flags[Memory],"completed journal survives new native pointer without item refill");

   Start();Tick();HandIn("GameRoom_Hub1A",Battery,false);
   Check(QuestChecks.Journal.Load(identity).Pending.Contains(QuestChecksPolicy.BatteryHandIn),"pre-clear intent is durable");
   Reload();Tick();
   Check(!RootsBucketRandomization.Flags[Battery],"crash after consumption cannot refill pending item");
   Check(QuestChecks.State.Snapshot.Checked.Contains(QuestChecksPolicy.BatteryHandIn),"pending consumed hand-in recovers completion after reload");
   Check(Plugin.AP.Queued.Contains("Lobby - Car Battery Hand-In"),"recovered completion queued to AP");
   Check(!CurrentPlayerSaveEnquiries.Unlocked.Any(),"hand-in can complete without character reward");

   Start(QuestChecksPolicy.Meoo,QuestChecksPolicy.Plunger);QuestChecks.Tick(Beat);
   CassetteReceiptRandomization.Slot=3;CassetteReceiptRandomization.Identity="other-slot";
   RootsBucketRandomization.Flags[Battery]=false;RootsBucketRandomization.Flags[Memory]=false;
   RootsBucketRandomization.Flags["PLUNGER_BAG_ITEM"]=false;CurrentPlayerSaveEnquiries.Unlocked.Clear();
   int grants=WeedKillerRandomization.Grants;int characters=CassetteReceiptRandomization.Processor.Grants;
   DeveloperHarness.CurrentRoomId="GameRoom_Hub2";
   QuestChecks.Observe(new Changed{FlagWasSet=false,FlagIsSet=true},"ROOTS_HUB_STAR_EATER_FED");Tick();
   Check(Plugin.AP.Queued.Count==0,"wrong selected slot cannot submit quest checks");
   Check(WeedKillerRandomization.Grants==grants&&CassetteReceiptRandomization.Processor.Grants==characters,"wrong selected slot cannot receive inventory or character grants");

   Start(QuestChecksPolicy.Plunger);Tick();Check(RootsBucketRandomization.Flags["PLUNGER_BAG_ITEM"],"AP Plunger grants native inventory");
   RootsBucketRandomization.Flags["PLUNGER_BAG_ITEM"]=false;RootsBucketRandomization.Flags["LOBBY_HUB_MEAT_DOOR_BLOCKER_PLUNGERED"]=true;
   Reload(QuestChecksPolicy.Plunger);Tick();Check(!RootsBucketRandomization.Flags["PLUNGER_BAG_ITEM"],"used Plunger stays consumed after restart");
   Check(Plugin.AP.Queued.Count==0,"using Plunger cannot complete its pickup check");
   DeveloperHarness.CurrentRoomId="GameRoom_Hub1B";RootsBucketRandomization.Flags["OUTSIDE_HUB_PLUNGER_COLLECTED"]=true;
   QuestChecks.Observe(new Changed{FlagWasSet=false,FlagIsSet=true},"OUTSIDE_HUB_PLUNGER_COLLECTED");Tick();
   Check(Plugin.AP.Queued.Count==1&&Plugin.AP.Queued[0]=="Lobby - Plunger Pickup","source pickup queues once independently of prior use");
   Start();
   DeveloperHarness.CurrentRoomId="GameRoom_Hub1B";
   Check(QuestChecks.SuppressPlunger(new Request{Value=true},"PLUNGER_BAG_ITEM"),"first pickup before first tick is suppressed and binds save");
   Check(QuestChecks.State.Snapshot.NativeSlot==4,"source inventory grant binds before collected marker");

   Start();Tick();DeveloperHarness.CurrentRoomId="GameRoom_Hub1A";
   string journalTemp=Path.Combine(BepInEx.Paths.ConfigPath,"RhythmCastleAP","quest-checks",
    Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(identity)))+".json.tmp");
   Directory.CreateDirectory(journalTemp);
   bool sequenceExecuted=false;
   if(QuestChecks.AdmitHandIn(QuestChecksPolicy.BatteryHandIn)) { sequenceExecuted=true;HandIn("GameRoom_Hub1A",Battery); }
   Check(!sequenceExecuted&&RootsBucketRandomization.Flags[Battery],"failed journal admission prevents entire sequence and retains battery");
   Check(!QuestChecks.State.Snapshot.Pending.Any(),"failed admission creates no consumable pending record");
   Directory.Delete(journalTemp);
   Check(QuestChecks.AdmitHandIn(QuestChecksPolicy.BatteryHandIn),"handin admitted after storage recovery");
   Tick();Check(QuestChecks.State.Snapshot.Pending.Contains(QuestChecksPolicy.BatteryHandIn),"admission survives native sequence waiting before consumption");
   Directory.CreateDirectory(journalTemp);
   HandIn("GameRoom_Hub1A",Battery);Tick();
   Check(!RootsBucketRandomization.Flags[Battery]&&QuestChecks.State.Snapshot.Pending.Contains(QuestChecksPolicy.BatteryHandIn),"completion write failure remains recoverable without refill");
   Directory.Delete(journalTemp);Tick();
   Check(QuestChecks.State.Snapshot.Checked.Contains(QuestChecksPolicy.BatteryHandIn),"completion persists when storage recovers");
   Console.WriteLine($"QuestRuntime passed {passed} assertions.");
  } finally {if(Directory.Exists(BepInEx.Paths.ConfigPath)) {
    string path=Path.GetFullPath(BepInEx.Paths.ConfigPath), tempRoot=Path.GetFullPath(Path.GetTempPath());
    if(!path.StartsWith(Path.Combine(tempRoot,"QuestRuntime-"),StringComparison.OrdinalIgnoreCase))throw new Exception("Unsafe test cleanup path");
    Directory.Delete(path,true);
   }}
 }
}

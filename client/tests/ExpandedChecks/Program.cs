using RhythmCastleAP;
using Newtonsoft.Json.Linq;
static void Check(bool ok,string name) { if(!ok)throw new Exception(name); Console.WriteLine("PASS "+name); }
var entry = ExpandedCheckCatalog.Entries[0];
var checks = new ExpandedChecksState();
var saved = new List<ExpandedJournalRecord>(); var queued = new List<string>();
void Persist(string _, ExpandedJournalRecord r) => saved.Add(r);
checks.Configure(1,"a",true,Array.Empty<long>(),_=>new(null,Array.Empty<long>()));
Check(!checks.Visit(1,_=>true,Persist,queued.Add,(_,_)=>false),"old save fails closed");
Check(!checks.Visit(1,_=>null,Persist,queued.Add,(_,_)=>false),"unreadable first bind fails closed");
Check(checks.Visit(1,_=>false,Persist,queued.Add,(_,_)=>false),"fresh save bound durably");
Check(!checks.Visit(2,_=>true,Persist,queued.Add,(_,_)=>false),"wrong slot rejected");
checks.Observe(1,"wrong",entry.Flag,true,Persist,queued.Add);
Check(queued.Count==0,"wrong room ignored");
try {checks.Observe(1,entry.Room,entry.Flag,true,(_,_)=>throw new IOException(),queued.Add);}catch(IOException){}
Check(queued.Count==0,"persistence failure never queues");
checks.Visit(1,flag=>flag==entry.Flag,Persist,queued.Add,(_,_)=>false);
Check(queued.Count==1 && saved.Last().Completed.Contains(entry.Id),"native reconciliation retries failed persistence");
checks.Observe(1,entry.Room,entry.Flag,true,Persist,queued.Add);
Check(queued.Count==1,"duplicate event ignored");
checks.Configure(2,"a",true,Array.Empty<long>(),_=>throw new Exception("must retain state"));
Check(queued.Count==1,"reconnect does not replay before save bound");
checks.Visit(2,_=>true,Persist,queued.Add,(_,_)=>false);
Check(queued.Count==1,"wrong slot cannot replay journal");
checks.Visit(1,flag=>flag==entry.Flag,Persist,queued.Add,(_,_)=>false);
Check(queued.Count==2,"same identity replay after binding");
var restored = saved.Last(); var restart = new ExpandedChecksState();
restart.Configure(1,"a",true,Array.Empty<long>(),_=>restored);
restart.Visit(1,flag=>flag==entry.Flag,Persist,queued.Add,(_,_)=>false);
Check(queued.Count==3,"restart replays durable checks");
checks.Configure(3,"b",true,Array.Empty<long>(),_=>new(null,Array.Empty<long>()));
Check(!checks.Visit(1,_=>true,Persist,queued.Add,(_,_)=>false),"new identity rejects old native sources");
checks.Configure(2,"a",true,Array.Empty<long>(),_=>restored);
Check(!checks.Visit(1,_=>true,Persist,queued.Add,(_,_)=>false),"stale generation cannot replace identity");
Check(ExpandedChecksPolicy.Validate(null)==ExpandedChecksMode.Legacy,"legacy contract");
var data = new Dictionary<string,object> { ["implementation_version"]="full-level-mapping-0.24-character-quest-items-0.25-quest-checks-0.26-check-expansion-0.27",["schema_version"]=18,["expanded_checks_schema"]=1,["expanded_check_locations"]=ExpandedCheckCatalog.Entries.ToDictionary(e=>e.Name,e=>e.Id)};
Check(ExpandedChecksPolicy.Validate(data)==ExpandedChecksMode.Enabled,"valid exact contract");
data["expanded_checks_schema"]="1";
Check(ExpandedChecksPolicy.Validate(data)==ExpandedChecksMode.Invalid,"malformed schema rejected");
data.Remove("expanded_checks_schema");data.Remove("implementation_version");
Check(ExpandedChecksPolicy.Validate(data)==ExpandedChecksMode.Invalid,"map only claim rejected");
foreach(var bad in new[]{new ExpandedJournalRecord(0,Array.Empty<long>()),new ExpandedJournalRecord(5,Array.Empty<long>()),new ExpandedJournalRecord(null,new[]{entry.Id}),new ExpandedJournalRecord(1,new[]{entry.Id,entry.Id}),new ExpandedJournalRecord(1,new[]{long.MaxValue})}) {
 bool rejected=false; try {ExpandedChecksJournal.Validate(bad);}catch(InvalidDataException){rejected=true;} Check(rejected,"invalid journal rejected");
}
Console.WriteLine("Expanded checks regression suite passed.");
var broken = new ExpandedChecksState();
broken.Configure(1,"a",true,Array.Empty<long>(),_=>new(null,Array.Empty<long>()));
try {broken.Configure(3,"b",true,Array.Empty<long>(),_=>throw new InvalidDataException());} catch(InvalidDataException) {}
broken.Configure(2,"a",true,Array.Empty<long>(),_=>new(1,Array.Empty<long>()));
Check(!broken.Enabled,"failed newer configuration fences older sessions");
var directory = Path.Combine(Path.GetTempPath(),"expanded-check-tests-" + Guid.NewGuid().ToString("N"));
try {
 var journal = new ExpandedChecksJournal(directory);
 journal.Save("identity",new(1,new[]{entry.Id}));
 Check(journal.Load("identity").Completed.SequenceEqual(new[]{entry.Id}),"atomic journal roundtrip");
 var file = Directory.GetFiles(directory,"*.json").Single();
 foreach(var corrupt in new[]{"{}","null","{not json}","{\"Schema\":1,\"Identity\":\"wrong\",\"Slot\":1,\"Completed\":[]}","{\"Schema\":1,\"Identity\":\"identity\",\"Slot\":1,\"Slot\":2,\"Completed\":[]}","{\"Schema\":1,\"Identity\":\"identity\",\"Slot\":\"1\",\"Completed\":[]}"}) {
  File.WriteAllText(file,corrupt); bool rejected=false;
  try {journal.Load("identity");}catch(InvalidDataException){rejected=true;}
  Check(rejected,"corrupt journal fails closed");
 }
} finally { if(Directory.Exists(directory)) Directory.Delete(directory,true); }
var readCount=0;
restart.Visit(1,flag=>{readCount++;return false;},Persist,queued.Add,(_,_)=>false);
Check(readCount==ExpandedCheckCatalog.Entries.Count(e=>e.Flag.Length>0)-1,"completed native flags not polled again");
Console.WriteLine("All expanded check regressions passed.");
try {
 ExpandedChecks.Configure(10,"runtime",true,Array.Empty<long>());
 CassetteReceiptRandomization.Stable=false;
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 Check(QuestChecks.Reads==0,"unstable save never reads native markers");
 CassetteReceiptRandomization.Stable=true;
 DeveloperHarness.CurrentRoomId=entry.Room;
 ExpandedChecks.BeforeProgressionRequest(new {Value=true},entry.Flag);
 Check(ExpandedChecks.Journal.Load("runtime").Slot==1,"request prebind persisted before native action");
 QuestChecks.Flags[entry.Flag]=true;
 DeveloperHarness.CurrentRoomId="wrong";
 ExpandedChecks.Observe(new{FlagIsSet=true},entry.Flag);
 Check(Plugin.AP!.Queued.Count==0,"runtime wrongroom ignored");
 DeveloperHarness.CurrentRoomId=entry.Room;
 ExpandedChecks.Observe(new{FlagIsSet=true},entry.Flag);
 Check(Plugin.AP.Queued.SequenceEqual(new[]{entry.Name}),"runtime queues true native source");
 ExpandedChecks.Observe(new{FlagIsSet=true},entry.Flag);
 Check(Plugin.AP.Queued.Count==1,"runtime deduplicates events");
 ExpandedChecks.Configure(11,"runtime",true,Array.Empty<long>());
 Check(Plugin.AP.Queued.Count==1,"runtime configure does not replay");
 CassetteReceiptRandomization.Slot=2;
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 Check(Plugin.AP.Queued.Count==1,"runtime wrongslot does not replay");
 CassetteReceiptRandomization.Slot=1;
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 Check(Plugin.AP.Queued.Count==2,"runtime reconnect replays after stable bind");
 int nativeReads=QuestChecks.Reads;
 ExpandedChecks.Tick(TimeSpan.FromMilliseconds(1));
 Check(QuestChecks.Reads==nativeReads,"native reconciliation rate limited");
 ExpandedChecks.Reset();
 ExpandedChecks.Configure(11,"runtime",true,Array.Empty<long>());
 Check(!ExpandedChecks.State.Enabled,"reset fences stale transport");
} finally {if(Directory.Exists(BepInEx.Paths.ConfigPath))Directory.Delete(BepInEx.Paths.ConfigPath,true);}
Console.WriteLine("Runtime adapter regressions passed.");
var special = ExpandedCheckCatalog.Entries.First(e=>e.Level!=null);
var results = new ExpandedChecksState();
results.Configure(1,"results",true,Array.Empty<long>(),_=>new(null,Array.Empty<long>()));
Check(results.CaptureResultContext(1,"saveA")==null,"result context requires bound save");
results.Prebind(1,_=>false,Persist,(_,_)=>false);
string token=results.CaptureResultContext(1,"saveA")!;
int resultBefore=queued.Count;
results.ObserveResult(1,"saveA",special.Level!,special.Variant!,false,token,Persist,queued.Add);
results.ObserveResult(1,"saveA",special.Level!,special.Variant!,null,token,Persist,queued.Add);
results.ObserveResult(1,"saveB",special.Level!,special.Variant!,true,token,Persist,queued.Add);
results.ObserveResult(2,"saveA",special.Level!,special.Variant!,true,token,Persist,queued.Add);
results.ObserveResult(1,"saveA",special.Level!,"wrong",true,token,Persist,queued.Add);
Check(queued.Count==resultBefore,"failed unknown stale-save wrongslot wrongvariant results ignored");
results.Configure(2,"results",true,Array.Empty<long>(),_=>throw new Exception());
results.ObserveResult(1,"saveA",special.Level!,special.Variant!,true,token,Persist,queued.Add);
Check(queued.Count==resultBefore,"stale generation result ignored");
token=results.CaptureResultContext(1,"saveA")!;
results.ObserveResult(1,"saveA",special.Level!,special.Variant!,true,token,Persist,queued.Add);
results.ObserveResult(1,"saveA",special.Level!,special.Variant!,true,token,Persist,queued.Add);
Check(queued.Count==resultBefore+1 && saved.Last().Completed.Contains(special.Id),"special result persisted and deduplicated");

var unreadableResult = new ExpandedChecksState();
unreadableResult.Configure(1,"unreadable-result",true,Array.Empty<long>(),_=>new(null,Array.Empty<long>()));
Check(!unreadableResult.Prebind(1,_=>false,Persist,(_,_)=>null),"unreadable special result prevents first bind");
Check(!unreadableResult.Prebind(1,_=>false,Persist,(_,_)=>true),"old native special result prevents first bind");
Check(unreadableResult.Prebind(1,_=>false,Persist,(_,_)=>false),"all fresh sources permit binding");
int beforeRecover=queued.Count;
unreadableResult.Visit(1,_=>false,Persist,queued.Add,(level,variant)=>level==special.Level && variant==special.Variant);
Check(queued.Count==beforeRecover+1 && queued.Last()==special.Name,"durable native special result reconciles after missed event");
try {
 ExpandedChecks.Configure(20,"guarded-result",true,Array.Empty<long>());
 QuestChecks.Flags.Clear(); CassetteReceiptRandomization.Slot=1;
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 string context=ExpandedChecks.CaptureResultContext()!;
 ExpandedCheckResults.Reads=0;
 ExpandedChecks.ObservePersistedResult(special.Level!,special.Variant!,null);
 CassetteReceiptRandomization.Slot=2;
 ExpandedChecks.ObservePersistedResult(special.Level!,special.Variant!,context);
 CassetteReceiptRandomization.Slot=1;
 ExpandedChecks.Configure(21,"guarded-result",true,Array.Empty<long>());
 ExpandedChecks.ObservePersistedResult(special.Level!,special.Variant!,context);
 Check(ExpandedCheckResults.Reads==0,"null stale and wrongslot callbacks never read native results");
 context=ExpandedChecks.CaptureResultContext()!;
 ExpandedChecks.ObservePersistedResult(special.Level!,special.Variant!,context);
 Check(ExpandedCheckResults.Reads==1,"current persisted callback reads native result");
 ExpandedChecks.Reset();
 ExpandedChecks.ObservePersistedResult(special.Level!,special.Variant!,context);
 Check(ExpandedCheckResults.Reads==1,"disabled callback never reads native result");
 ExpandedChecks.Configure(30,"binding-diagnostic",true,Array.Empty<long>());
 QuestChecks.Flags[entry.Flag]=true;
 int logBefore=Plugin.LoggerInstance!.Errors.Count;
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 Check(Plugin.LoggerInstance.Errors.Count==logBefore+1 && Plugin.LoggerInstance.Errors.Last().Contains(entry.Name) && Plugin.LoggerInstance.Errors.Last().Contains("completed"),"completed binding diagnostic names source once");
 QuestChecks.Flags[entry.Flag]=null;
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 Check(Plugin.LoggerInstance.Errors.Count==logBefore+2 && Plugin.LoggerInstance.Errors.Last().Contains(entry.Name) && Plugin.LoggerInstance.Errors.Last().Contains("unreadable"),"unreadable binding diagnostic distinguished and bounded");
} finally {if(Directory.Exists(BepInEx.Paths.ConfigPath))Directory.Delete(BepInEx.Paths.ConfigPath,true);}
try {
 QuestChecks.Flags.Clear();
 ExpandedChecks.Configure(40,"queueguard",true,Array.Empty<long>());
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 Plugin.AP!.AcceptedIdentity="different";
 int queueBefore=Plugin.AP.Queued.Count;
 QuestChecks.Flags[entry.Flag]=true;
 DeveloperHarness.CurrentRoomId=entry.Room;
 ExpandedChecks.Observe(new{FlagIsSet=true},entry.Flag);
 Check(Plugin.AP.Queued.Count==queueBefore,"AP identity replacement rejects old check queue");
 Plugin.AP.AcceptedIdentity="queueguard";
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 Check(Plugin.AP.Queued.Count==queueBefore+1,"rejected queue retries without falsely marking sent");
} finally {if(Directory.Exists(BepInEx.Paths.ConfigPath))Directory.Delete(BepInEx.Paths.ConfigPath,true);}

var starMap = ExpandedCheckCatalog.Entries.ToDictionary(e=>e.Name,e=>e.Id);
starMap["Cell Tower - Star Eater Fed"] = 187256334;
starMap["Royal Corridor - King Ferdinand Unlocked"] = 187256335;
var starData = new Dictionary<string,object> {
 ["implementation_version"] = ExpandedChecksPolicy.VersionSuffix + "-ap-stars-0.28",
 ["schema_version"] = 19, ["expanded_checks_schema"] = 1, ["expanded_check_locations"] = starMap
};
Check(ExpandedChecksPolicy.Validate(starData)==ExpandedChecksMode.Enabled,"schema19 exact39 accepted");
starData["schema_version"] = 18;
Check(ExpandedChecksPolicy.Validate(starData)==ExpandedChecksMode.Invalid,"schema18 cannot claim new sources");
starData["schema_version"] = 19;
starData["expanded_check_locations"] = ExpandedCheckCatalog.Entries.ToDictionary(e=>e.Name,e=>e.Id);
Check(ExpandedChecksPolicy.Validate(starData)==ExpandedChecksMode.Invalid,"schema19 rejects legacy37 map");

try {
 Plugin.AP!.AcceptedIdentity=null;
 QuestChecks.Flags.Clear(); CassetteReceiptRandomization.Slot=1; CassetteReceiptRandomization.Stable=true;
 CurrentPlayerSaveEnquiries.KingUnlocked=false;
 int start=Plugin.AP.Queued.Count;
 ExpandedChecks.Configure(50,"legacy-new-flags",true,Array.Empty<long>());
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 DeveloperHarness.CurrentRoomId="GameRoom_Hub5B";
 ExpandedChecks.Observe(new{FlagIsSet=true},"PRISON_HUB_STAR_EATER_FED");
 Check(Plugin.AP.Queued.Count==start,"legacy runtime cannot emit Cell source");
 ExpandedChecks.Configure(51,"old-king",true,Array.Empty<long>(),true);
 CurrentPlayerSaveEnquiries.KingUnlocked=true;
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 Check(ExpandedChecks.CaptureResultContext()==null,"old King ownership blocks first schema19 binding");
 CurrentPlayerSaveEnquiries.KingUnlocked=null;
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 Check(ExpandedChecks.CaptureResultContext()==null,"unreadable King ownership blocks binding");
 CurrentPlayerSaveEnquiries.KingUnlocked=false;
 ExpandedChecks.Configure(52,"new-sources",true,Array.Empty<long>(),true);
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 Check(ExpandedChecks.CaptureResultContext()!=null,"fresh schema19 sources bind");
 CurrentPlayerSaveEnquiries.KingUnlocked=true;
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 Check(Plugin.AP.Queued.Count==start,"received ownership alone never counts as King native source");
 CurrentPlayerSaveEnquiries.KingUnlocked=false;
 DeveloperHarness.CurrentRoomId="GameRoom_28B";
 int originalCalls=0;
 KingUnlockSource.RunNative("unrelated",()=>{originalCalls++; CurrentPlayerSaveEnquiries.KingUnlocked=true;});
 Check(originalCalls==1 && Plugin.AP.Queued.Count==start,"unrelated unlock preserved without source");
 CurrentPlayerSaveEnquiries.KingUnlocked=false;
 KingUnlockSource.RunNative(KingUnlockSource.SourcePath,()=>originalCalls++);
 Check(originalCalls==2 && Plugin.AP.Queued.Count==start,"unsuccessful native unlock cannot report");
 KingUnlockSource.RunNative(KingUnlockSource.SourcePath,()=>{originalCalls++; CurrentPlayerSaveEnquiries.KingUnlocked=true;});
 Check(originalCalls==3 && Plugin.AP.Queued.Last()=="Royal Corridor - King Ferdinand Unlocked","native source unlock persisted and queued");
 Check(ExpandedChecks.Journal.Load("new-sources").Completed.Contains(187256335),"King source has durable journal independent of ownership");
 KingUnlockSource.RunNative(KingUnlockSource.SourcePath,()=>originalCalls++);
 Check(originalCalls==4 && Plugin.AP.Queued.Count==start+1,"duplicate native unlock preserves action without duplicate check");
 DeveloperHarness.CurrentRoomId="GameRoom_Hub5B";
 QuestChecks.Flags["PRISON_HUB_STAR_EATER_FED"]=true;
 ExpandedChecks.Observe(new{FlagIsSet=true},"PRISON_HUB_STAR_EATER_FED");
 Check(Plugin.AP.Queued.Last()=="Cell Tower - Star Eater Fed","Cell native full flag reports new source");
 ExpandedChecks.Reset();
 ExpandedChecks.Configure(60,"new-sources",true,Array.Empty<long>(),true);
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 Check(Plugin.AP.Queued.Count==start+4,"restart replays both durable new sources");
} finally {if(Directory.Exists(BepInEx.Paths.ConfigPath))Directory.Delete(BepInEx.Paths.ConfigPath,true);}

try {
 Plugin.AP!.AcceptedIdentity=null; QuestChecks.Flags.Clear(); CurrentPlayerSaveEnquiries.KingUnlocked=false;
 ExpandedChecks.Configure(70,"delayed-king",true,Array.Empty<long>(),true);
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 DeveloperHarness.CurrentRoomId="GameRoom_28B";
 int before=Plugin.AP.Queued.Count;
 KingUnlockSource.RunNative(KingUnlockSource.SourcePath,()=>{});
 ExpandedChecks.Reset();
 ExpandedChecks.Configure(80,"delayed-king",true,Array.Empty<long>(),true);
 CurrentPlayerSaveEnquiries.KingUnlocked=true;
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 Check(Plugin.AP.Queued.Count==before+1 && Plugin.AP.Queued.Last()=="Royal Corridor - King Ferdinand Unlocked","native King bundle source survives restart before saved unlock confirmation");
} finally {if(Directory.Exists(BepInEx.Paths.ConfigPath))Directory.Delete(BepInEx.Paths.ConfigPath,true);}

var pendingRetry = new ExpandedChecksState();
pendingRetry.Configure(1,"pending-retry",true,Array.Empty<long>(),_=>new(null,Array.Empty<long>()),true);
pendingRetry.Prebind(1,_=>false,Persist,(_,_)=>false,_=>false);
string kingContext = pendingRetry.CaptureResultContext(1,"native")!;
try { pendingRetry.ObserveCharacterSource(1,"native","KING",kingContext,(_,_)=>throw new IOException()); } catch(IOException){}
int retryBefore=queued.Count;
pendingRetry.RetryResults(1,"other-native",Persist,queued.Add);
pendingRetry.Visit(1,_=>false,Persist,queued.Add,(_,_)=>false,_=>true);
Check(queued.Count==retryBefore,"failed King journal write cannot silently turn ownership into source");
pendingRetry.RetryResults(1,"native",Persist,queued.Add);
pendingRetry.Visit(1,_=>false,Persist,queued.Add,(_,_)=>false,_=>true);
Check(queued.Count==retryBefore+1,"King source persistence failure retries under original native save only");
try {
 QuestChecks.Flags.Clear(); CurrentPlayerSaveEnquiries.KingUnlocked=false;
 ExpandedChecks.Configure(90,"king-guards",true,Array.Empty<long>(),true);
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1)); DeveloperHarness.CurrentRoomId="GameRoom_28B";
 int before=Plugin.AP!.Queued.Count;
 bool threw=false;
 try { KingUnlockSource.RunNative(KingUnlockSource.SourcePath,()=>throw new IOException("native failure")); } catch(IOException) {threw=true;}
 CurrentPlayerSaveEnquiries.KingUnlocked=true;
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 Check(threw && Plugin.AP.Queued.Count==before,"native throw preserved and never armed as King source");
 CurrentPlayerSaveEnquiries.KingUnlocked=false;
 KingUnlockSource.RunNative(KingUnlockSource.SourcePath,()=>{CassetteReceiptRandomization.Slot=2;CurrentPlayerSaveEnquiries.KingUnlocked=true;});
 CassetteReceiptRandomization.Slot=1; ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 Check(Plugin.AP.Queued.Count==before,"save change inside native unlock invalidates source context");
 CurrentPlayerSaveEnquiries.KingUnlocked=false;
 KingUnlockSource.RunNative(KingUnlockSource.SourcePath,()=>{ExpandedChecks.Configure(91,"king-other",true,Array.Empty<long>(),true);CurrentPlayerSaveEnquiries.KingUnlocked=true;});
 ExpandedChecks.Tick(TimeSpan.FromSeconds(1));
 Check(Plugin.AP.Queued.Count==before,"identity change inside native unlock invalidates source context");
} finally {if(Directory.Exists(BepInEx.Paths.ConfigPath))Directory.Delete(BepInEx.Paths.ConfigPath,true);}

var compatibilityDir=Path.Combine(Path.GetTempPath(),"expanded-journal-compat-"+Guid.NewGuid().ToString("N"));
try {
 var journal=new ExpandedChecksJournal(compatibilityDir);
 journal.Save("old",new(1,new[]{entry.Id}));
 string file=Directory.GetFiles(compatibilityDir).Single();
 File.WriteAllText(file,"{\"Schema\":1,\"Identity\":\"old\",\"Slot\":1,\"Completed\":["+entry.Id+"]}");
 Check(journal.Load("old").Completed.SequenceEqual(new[]{entry.Id}),"schema1 journal remains readable");
 journal.Save("old",new(1,Array.Empty<long>(),new[]{187256335L}));
 Check(journal.Load("old").PendingCharacters!.SequenceEqual(new[]{187256335L}),"pending King native source roundtrips durably");
 foreach(var record in new[]{new ExpandedJournalRecord(null,Array.Empty<long>(),new[]{187256335L}),new ExpandedJournalRecord(1,Array.Empty<long>(),new[]{entry.Id}),new ExpandedJournalRecord(1,new[]{187256335L},new[]{187256335L}),new ExpandedJournalRecord(1,Array.Empty<long>(),new[]{187256335L,187256335L})}) {
  bool rejected=false;try {journal.Save("bad",record);}catch(InvalidDataException){rejected=true;}
  Check(rejected,"malformed pending King journal rejected");
 }
} finally {if(Directory.Exists(compatibilityDir))Directory.Delete(compatibilityDir,true);}
Console.WriteLine("All missing-check source and compatibility regressions passed.");

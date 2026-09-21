using RhythmCastleAP;
using UnityEngine;
int checks=0,sends=0;string dir=Path.Combine(Path.GetTempPath(),"ap-star-runtime-fixtures-"+Guid.NewGuid());
void Check(bool value,string label){if(!value)throw new Exception(label);checks++;}
void Setup(string id,int stars=25,string? path=null){ApStars.State.Reset();ApStars.Configure(_=>{sends++;return true;},path??dir);ApStars.State.Configure(1,id,new(ApStarMode.Awaiting,25,new Dictionary<string,int>{{"Level 22",1}},new Dictionary<string,int>()));ApStars.State.Publish(1,Enumerable.Repeat(ApStarContract.StarId,stars));CassetteReceiptRandomization.Save="epoch:a";CassetteReceiptRandomization.Ready=true;Time.unscaledTime+=2;}
void Start(){ApStars.OnAdmittedStart("Level_28","LevelVariant_Default");}
void Complete(bool success=true,bool save=true){ApStars.BeforePersistResult(new {DidPlayersSucceed=success,ShouldSaveScore=save});ApStars.BeforeApplyResult(new {LevelIdentifier="Level_28",LevelVariantIdentifier="LevelVariant_Default"});}
void Persist(string variant="LevelVariant_Default"){ApStars.OnResultPersisted(new {Level="Level_28",LevelVariant=variant});}
void Tick(){Time.unscaledTime+=2;ApStars.TickUnity();}
Setup("early",24);Start();Complete();ApStars.State.Publish(1,Enumerable.Repeat(ApStarContract.StarId,25));Persist();Tick();Check(sends==0,"receipt after completion cannot qualify old attempt");
Complete();Persist();Tick();Check(sends==0,"duplicate completion request cannot rearm consumed early start");
Start();Complete();Persist();Tick();Check(sends==1,"qualified replay sends after matching persistence");Persist();Tick();Check(sends==1,"duplicate persisted event cannot send again");
Setup("early");Tick();Check(sends==2,"qualified durable goal restores after process-state reset");
Setup("other");Tick();Check(sends==2,"other AP identity cannot inherit durable goal");
Start();Complete();CassetteReceiptRandomization.Save="epoch:b";Persist();Tick();Check(sends==2,"native save epoch change rejects completion");
Setup("failed");Start();Complete(false);Persist();Tick();Check(sends==2,"failed result cannot win");
Setup("no-save");Start();Complete(true,false);Persist();Tick();Check(sends==2,"nonpersistent result cannot win");
Setup("variant");Start();Complete();Persist("LevelVariant_DevilMode");Tick();Check(sends==2,"mismatched event variant rejected");Persist();Tick();Check(sends==2,"mismatched event consumes candidate");
Setup("boundary");Start();Complete();ApStars.OnNativeBoundary();Persist();Tick();Check(sends==2,"explicit lifecycle boundary drops attempt");
Setup("apply-mismatch");Start();Complete();ApStars.BeforeApplyResult(new {LevelIdentifier="Level_14",LevelVariantIdentifier="LevelVariant_Default"});Persist();Tick();Check(sends==2,"unrelated apply cannot bind candidate");
Setup("unready");CassetteReceiptRandomization.Ready=false;Start();Complete();Persist();Tick();Check(sends==2,"unstable native save cannot qualify");
Directory.CreateDirectory(dir);string bad=Path.Combine(dir,"not-a-directory");File.WriteAllText(bad,"block");Setup("diskfail",25,bad);Start();Complete();Persist();Tick();Check(sends==2,"journal failure prevents goal delivery");
Setup("getter");DeveloperHarness.CurrentRoomId="GameRoom_Hub2";ApStarEaterThresholds.CurrentRoomReady=false;Check(ApStars.ResolveTotal(66)==0,"unapplied eater threshold fails closed");ApStarEaterThresholds.CurrentRoomReady=true;Check(ApStars.ResolveTotal(66)==25,"ready eater uses AP currency");Check(!ApStars.CanEnter("",""),"active unknown identity denied");
Setup("blocked-banner",3);
ApStars.State.Configure(2,"blocked-banner",new(ApStarMode.Awaiting,40,new Dictionary<string,int>{{"Level 11",8}},new Dictionary<string,int>()));
ApStars.State.Publish(2,Enumerable.Repeat(ApStarContract.StarId,3));
ApStars.ReportBlocked("Level_12","LevelVariant_Default");GUI.Last="";ApStars.RenderOverlay();
Check(GUI.Last.Contains("Act 1: Flavor") && GUI.Last.Contains("8 AP Stars") && GUI.Last.Contains("5 more"),"blocked entry explains level, requirement and missing Stars");
Time.unscaledTime+=3;GUI.Last="";ApStars.RenderOverlay();Check(GUI.Last.Contains("Act 1: Flavor"),"blocked notice remains visible beyond old two-second preview");
ApStars.ReportBlocked("Level_12","LevelVariant_Default");Time.unscaledTime+=4;GUI.Last="";ApStars.RenderOverlay();Check(GUI.Last.Contains("Act 1: Flavor"),"repeated attempt refreshes one notice");
ApStars.State.Publish(2,Enumerable.Repeat(ApStarContract.StarId,8));GUI.Last="";ApStars.RenderOverlay();Check(!GUI.Last.Contains("collect"),"receiving enough Stars removes stale denial");
ApStars.State.Publish(2,Enumerable.Repeat(ApStarContract.StarId,3));ApStars.ReportBlocked("Level_12","LevelVariant_Default");Time.unscaledTime+=7;GUI.Last="";ApStars.RenderOverlay();Check(GUI.Last=="","blocked notice expires");
ApStars.ReportBlocked("Level_12","LevelVariant_Default");ApStars.OnNativeBoundary();GUI.Last="";ApStars.RenderOverlay();Check(GUI.Last=="","save boundary clears stale entry feedback");
ApStars.State.Configure(3,"awaiting-banner",new(ApStarMode.Awaiting,40,new Dictionary<string,int>{{"Level 11",8}},new Dictionary<string,int>()));ApStars.ReportBlocked("Level_12","LevelVariant_Default");GUI.Last="";ApStars.RenderOverlay();Check(GUI.Last.Contains("synchronize") && !GUI.Last.Contains("collect"),"unsynchronized state explains waiting instead of inventing a shortage");
DeveloperHarness.CurrentRoomId="GameRoom_Hub6";GUI.Last="";ApStars.RenderOverlay();Check(!GUI.Last.Contains("Entry locked"),"room change hides old blocked banner");
ApStars.State.Reset();GUI.Last="";ApStars.RenderOverlay();Check(GUI.Last=="","native play shows no AP denial banner");
Setup("delayed-save");Start();Complete();int baseline=sends;
CassetteReceiptRandomization.Ready=false;Persist();Tick();Check(sends==baseline,"unverified save cannot send");
CassetteReceiptRandomization.Ready=true;Tick();Check(sends==baseline+1,"confirmed clear survives temporary save unavailability");
Persist();Tick();Check(sends==baseline+1,"deferred clear sends once");
Setup("delayed-other-save");Start();Complete();baseline=sends;
CassetteReceiptRandomization.Ready=false;Persist();CassetteReceiptRandomization.Save="epoch:b";CassetteReceiptRandomization.Ready=true;Tick();Check(sends==baseline,"deferred clear rejects changed save");
Setup("delayed-boundary");Start();Complete();baseline=sends;
CassetteReceiptRandomization.Ready=false;Persist();ApStars.OnNativeBoundary();CassetteReceiptRandomization.Ready=true;Tick();Check(sends==baseline,"explicit boundary invalidates deferred evidence");
Setup("delayed-early",24);Start();Complete();baseline=sends;
CassetteReceiptRandomization.Ready=false;Persist();ApStars.State.Publish(1,Enumerable.Repeat(ApStarContract.StarId,25));CassetteReceiptRandomization.Ready=true;Tick();Check(sends==baseline,"later Stars do not qualify deferred early clear");
Setup("delayed-start-again");Start();Complete();baseline=sends;
CassetteReceiptRandomization.Ready=false;Persist();CassetteReceiptRandomization.Ready=true;Start();Tick();Check(sends==baseline+1,"starting another level preserves already confirmed completion");
Setup("native-null-variant");Start();Complete();baseline=sends;
ApStars.OnResultPersisted(new {Level="Level_28",LevelVariant=(string?)null});Tick();Check(sends==baseline+1,"native omitted variant uses verified applied result");
ApStars.OnResultPersisted(new {Level="Level_28",LevelVariant=(string?)null});Tick();Check(sends==baseline+1,"null variant duplicate sends once");
Setup("null-variant-no-apply");Start();ApStars.BeforePersistResult(new {DidPlayersSucceed=true,ShouldSaveScore=true});baseline=sends;
ApStars.OnResultPersisted(new {Level="Level_28",LevelVariant=(string?)null});Tick();Check(sends==baseline,"null variant requires bound applied result");
Setup("null-variant-wrong-level");Start();Complete();baseline=sends;
ApStars.OnResultPersisted(new {Level="Level_14",LevelVariant=(string?)null});Tick();Check(sends==baseline,"null variant cannot authorize different level");
Setup("native-nullable-variant");Start();Complete();baseline=sends;
ApStars.OnResultPersisted(new {Level="Level_28",LevelVariant=new NativeFixture.Nullable<string>(false, null)});Tick();Check(sends==baseline+1,"native nullable without value accepts bound completion");
Setup("native-nullable-conflict");Start();Complete();baseline=sends;
ApStars.OnResultPersisted(new {Level="Level_28",LevelVariant=new NativeFixture.Nullable<string>(true, "LevelVariant_DevilMode")});Tick();Check(sends==baseline,"native nullable conflicting variant rejected");
Console.WriteLine($"{checks} AP Star runtime checks passed");
namespace RhythmCastleAP {
 internal static class Plugin {internal static Log? LoggerInstance;}internal class Log{public void LogInfo(string s){}public void LogError(string s){}}
 internal static class DeveloperHarness{internal static string CurrentRoomId="GameRoom_Hub7";}
 internal static class CassetteReceiptRandomization{internal static bool Ready=true;internal static string Save="epoch:a";internal static void WithStableQuestItemSave(Action<object,string>a){if(Ready)a(new object(),Save);}}
 internal static class ApStarEaterThresholds{internal static bool CurrentRoomReady=true;internal static bool IsReadyForRoom(string room)=>CurrentRoomReady;internal static void Tick(string r,IReadOnlyDictionary<string,int>? m){}}
}
namespace BepInEx{public static class Paths{public static string ConfigPath=>Path.GetTempPath();}}
namespace UnityEngine{public static class Time{public static float unscaledTime;}public static class Screen{public static int height=>1080;public static int width=>1920;}public struct Rect{public Rect(int a,int b,int c,int d){}}public enum TextAnchor{MiddleCenter}public class GUIStyle{public GUIStyle(object o){}public int fontSize;public bool wordWrap,richText;public TextAnchor alignment;}public class GUISkin{public object box=new();}public static class GUI{public static int depth;public static GUISkin skin=new();public static string Last="";public static void Box(Rect r,string s){Last=s;}public static void Box(Rect r,string s,GUIStyle style){Last=s;}}}
namespace NativeFixture { internal sealed record Nullable<T>(bool HasValue, T? Value); }

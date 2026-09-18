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
Console.WriteLine($"{checks} AP Star runtime checks passed");
namespace RhythmCastleAP {
 internal static class Plugin {internal static Log? LoggerInstance;}internal class Log{public void LogInfo(string s){}public void LogError(string s){}}
 internal static class DeveloperHarness{internal static string CurrentRoomId="GameRoom_Hub7";}
 internal static class CassetteReceiptRandomization{internal static bool Ready=true;internal static string Save="epoch:a";internal static void WithStableQuestItemSave(Action<object,string>a){if(Ready)a(new object(),Save);}}
 internal static class ApStarEaterThresholds{internal static bool CurrentRoomReady=true;internal static bool IsReadyForRoom(string room)=>CurrentRoomReady;internal static void Tick(string r,IReadOnlyDictionary<string,int>? m){}}
 internal static class ReflectionUtil{internal static object? ReadMember(object? o,string m)=>o?.GetType().GetProperty(m)?.GetValue(o);internal static bool? ReadBool(object?o,string m)=>ReadMember(o,m) as bool?;internal static string? ExtractIdentifier(object?o)=>o?.ToString();}
}
namespace BepInEx{public static class Paths{public static string ConfigPath=>Path.GetTempPath();}}
namespace UnityEngine{public static class Time{public static float unscaledTime;}public static class Screen{public static int height=>1080;}public struct Rect{public Rect(int a,int b,int c,int d){}}public static class GUI{public static void Box(Rect r,string s){}}}

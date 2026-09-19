using System.Runtime.InteropServices;
using RhythmCastleAP;
using Il2CppInterop.Runtime;
using UnityEngine;
int checks=0;
void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
var map=new Dictionary<string,int>{{"Roots",1}};
ApStarEaterThresholds.Tick("GameRoom_Hub2",map);
Check(ApStarEaterThresholds.IsReadyForRoom("GameRoom_Hub2"),"threshold successfully applied");
Check(IL2CPP.Current==IL2CPP.NewObject && IL2CPP.Value==1,"exact constructed object pointer assigned");
Check(IL2CPP.Writes==1,"single write");
ApStarEaterThresholds.Tick("GameRoom_Hub2",map);
Check(IL2CPP.Writes==1,"stable tick does not repeat allocation/write");
ApStarEaterThresholds.Tick("other",map);
Check(IL2CPP.Current==IL2CPP.Original,"room change restores original object");
ApStarEaterThresholds.Tick("GameRoom_Hub2",map);
ApStarEaterThresholds.Tick("GameRoom_Hub2",null);
Check(IL2CPP.Current==IL2CPP.Original && !ApStarEaterThresholds.CurrentRoomReady,"contract removal restores original");
ApStarEaterThresholds.Tick("GameRoom_Hub2",map);
IL2CPP.Current=(IntPtr)999;
ApStarEaterThresholds.Restore();
Check(IL2CPP.Current==(IntPtr)999,"restoration preserves another owner's replacement");
IL2CPP.Current=IL2CPP.Original;
Time.frameCount+=100;
IL2CPP.RejectOnce=true;
ApStarEaterThresholds.Tick("GameRoom_Hub2",map);
Check(!ApStarEaterThresholds.CurrentRoomReady && IL2CPP.Current==IL2CPP.Original,"failed verification rolls back immediately");
Time.frameCount+=100;
ApStarEaterThresholds.Tick("GameRoom_Hub2",map);
Check(ApStarEaterThresholds.CurrentRoomReady,"retry succeeds after failed write");
ApStarEaterThresholds.Restore();
Console.WriteLine($"{checks} Star Eater reference lifecycle checks passed");
namespace Il2CppSystem {public class Object {public IntPtr Pointer; public Object(IntPtr p){Pointer=p;}}}
namespace UnityEngine {
 public class Component {public IntPtr Pointer=(IntPtr)101;}
 public class GameObject {public static GameObject? Find(string p)=>new();public T[] GetComponents<T>() where T:new()=>new[]{new T()};}
 public static class Time {public static int frameCount=100;}
}
namespace RhythmCastleAP {public static class Plugin {public static Logger? LoggerInstance=new();} public class Logger {public void LogInfo(string s){} public void LogError(string s){Console.WriteLine(s);}}}
namespace Il2CppInterop.Runtime {
 public static class IL2CPP {
  public static IntPtr Original=(IntPtr)201,NewObject=(IntPtr)202,Current=Original;
  public static int Writes,Value; public static bool RejectOnce;
  static IntPtr Name1=Marshal.StringToHGlobalAnsi("StarEaterInteraction"),Name2=Marshal.StringToHGlobalAnsi("DefinedInt"),Box=Marshal.AllocHGlobal(4);
  public static IntPtr il2cpp_object_get_class(IntPtr p)=>p==(IntPtr)101?(IntPtr)11:(IntPtr)12;
  public static IntPtr il2cpp_class_get_name(IntPtr p)=>p==(IntPtr)11?Name1:Name2;
  public static IntPtr il2cpp_class_get_field_from_name(IntPtr p,string n)=>n=="fullFlag"?(IntPtr)31:(IntPtr)32;
  public static IntPtr il2cpp_class_get_parent(IntPtr p)=>IntPtr.Zero;
  public static IntPtr il2cpp_field_get_value_object(IntPtr f,IntPtr o){if(f==(IntPtr)31){Marshal.WriteInt32(Box,302);return Box;}return Current;}
  public static IntPtr il2cpp_object_unbox(IntPtr p)=>p;
  public static IntPtr il2cpp_class_get_method_from_name(IntPtr p,string n,int c)=>(IntPtr)41;
  public static IntPtr il2cpp_object_new(IntPtr p)=>NewObject;
  public static unsafe IntPtr il2cpp_runtime_invoke(IntPtr m,IntPtr o,void** args,ref IntPtr exception){Value=*(int*)args[0];exception=IntPtr.Zero;return IntPtr.Zero;}
  public static void il2cpp_field_set_value_object(IntPtr instance,IntPtr field,IntPtr value){
   if(instance!=(IntPtr)101 || field!=(IntPtr)32 || (value!=Original && value!=NewObject))throw new Exception("native object-reference ABI violation");
   Writes++;
   if(RejectOnce){RejectOnce=false;Current=(IntPtr)888;}else Current=value;
  }
 }
}

using System.Runtime.InteropServices;
namespace Il2CppInterop.Runtime;
public static class IL2CPP {
 public static int Flag=116001; public static bool WrongClass, RejectWrite; public static IntPtr Reference=(IntPtr)2; public static int Writes;
 private static readonly IntPtr SwitchName=Marshal.StringToHGlobalAnsi("Switch"), ConditionName=Marshal.StringToHGlobalAnsi("IsGameProgressionFlagSetCondition"), OtherName=Marshal.StringToHGlobalAnsi("Other");
 private static readonly IntPtr Box=Marshal.AllocHGlobal(4);
 public static IntPtr il2cpp_object_get_class(IntPtr p)=>p;
 public static IntPtr il2cpp_class_get_name(IntPtr p)=>WrongClass?OtherName:p==(IntPtr)1?SwitchName:ConditionName;
 public static IntPtr il2cpp_class_get_parent(IntPtr p)=>IntPtr.Zero;
 public static IntPtr il2cpp_class_get_field_from_name(IntPtr p,string n)=>n=="requiredCondition"?(IntPtr)10:n=="flag"?(IntPtr)11:IntPtr.Zero;
 public static IntPtr il2cpp_field_get_value_object(IntPtr f,IntPtr p) {if(f==(IntPtr)10)return Reference;Marshal.WriteInt32(Box,Flag);return Box;}
 public static IntPtr il2cpp_object_unbox(IntPtr p)=>p;
 public static void il2cpp_field_set_value_object(IntPtr obj,IntPtr field,IntPtr value){if(obj!=(IntPtr)1 || field!=(IntPtr)10 || value!=IntPtr.Zero)throw new Exception("wrong native reference write");Writes++;if(!RejectWrite)Reference=value;}
}

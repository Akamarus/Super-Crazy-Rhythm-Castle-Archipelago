using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
namespace RhythmCastleAP;

internal static class LobbyDoorPatches
{
    internal static bool Ready {get;private set;}
    internal static int Install(Harmony harmony, Assembly? assembly)
    {
        Ready=false;
        var type=assembly==null?null:ReflectionUtil.SafeGetTypes(assembly).FirstOrDefault(t=>t.Name=="LevelEntranceDoorView");
        if(type==null){Plugin.LoggerInstance?.LogWarning("[SCRC-AP] LOBBY DOORS view type unavailable; repair inactive.");return 0;}
        int count=0;
        foreach(var name in new[]{"Awake","RefreshToMatchState"})
        {
            var methods=type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly).Where(m=>m.Name==name).ToArray();
            if(methods.Length!=1)continue;
            try{harmony.Patch(methods[0],prefix:new HarmonyMethod(typeof(LobbyDoorPatches),nameof(BeforeDoor)));count++;}
            catch(Exception e){Plugin.LoggerInstance?.LogWarning("[SCRC-AP] LOBBY DOORS hook failed: "+e.GetType().Name);}
        }
        Ready=count==2;
        Plugin.LoggerInstance?.LogInfo("[SCRC-AP] LOBBY DOORS hooks ready="+Ready);
        return count;
    }
    public static void BeforeDoor(object __instance)
    {
        if(!Ready || !AreaAccessPrototype.Enabled || !AreaAccessPrototype.AuthenticatedCompatibleSession || !AreaAccessPrototype.HasArea("Lobby") || __instance is not Component c)return;
        if(c.name is not ("Level19Door" or "Level20Door"))return;
        try
        {
            var names=new List<string>();for(Transform? t=c.transform;t!=null;t=t.parent)names.Add(t.name);names.Reverse();
            var path=string.Join("/",names);
            string? coverName=LobbyDoorPolicy.Cover(true,true,true,path);
            if(coverName==null)return;
            var cover=c.transform.parent?.Find(coverName);
            if(cover==null)return;
            Component? door=c.gameObject.GetComponents<Component>().FirstOrDefault(x=>x!=null && Name(IL2CPP.il2cpp_object_get_class(x.Pointer))=="LevelEntranceDoor");
            if(door==null || !ClearRequirement(door.Pointer,path))return;
            if(cover.gameObject.activeSelf){cover.gameObject.SetActive(false);Plugin.LoggerInstance?.LogInfo("[SCRC-AP] LOBBY DOOR OPEN path='"+path+"'; story and completion unchanged, Star gate retained.");}
        }
        catch(Exception e){Plugin.LoggerInstance?.LogWarning("[SCRC-AP] LOBBY DOOR repair failed safely: "+e.GetType().Name);}
    }
    private static bool ClearRequirement(IntPtr instance,string path)
    {
        var klass=IL2CPP.il2cpp_object_get_class(instance);
        if(Name(klass)!="LevelEntranceDoor")return false;
        var field=FindField(klass,"requiredCondition");if(field==IntPtr.Zero)return false;
        var condition=IL2CPP.il2cpp_field_get_value_object(field,instance);if(condition==IntPtr.Zero)return true;
        var ck=IL2CPP.il2cpp_object_get_class(condition);
        int? flag=null;
        if(Name(ck)=="IsGameProgressionFlagSetCondition") {
            var ff=FindField(ck,"flag");if(ff==IntPtr.Zero)return false;
            var boxed=IL2CPP.il2cpp_field_get_value_object(ff,condition);if(boxed==IntPtr.Zero)return false;
            flag=Marshal.ReadInt32(IL2CPP.il2cpp_object_unbox(boxed));
        }
        if(!LobbyDoorPolicy.MatchesCondition(path,Name(ck)??"",flag))return false;
        IL2CPP.il2cpp_field_set_value_object(instance,field,IntPtr.Zero);
        return IL2CPP.il2cpp_field_get_value_object(field,instance)==IntPtr.Zero;
    }
    private static string? Name(IntPtr k)=>k==IntPtr.Zero?null:Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(k));
    private static IntPtr FindField(IntPtr k,string name){for(int i=0;k!=IntPtr.Zero && i<12;i++){var f=IL2CPP.il2cpp_class_get_field_from_name(k,name);if(f!=IntPtr.Zero)return f;k=IL2CPP.il2cpp_class_get_parent(k);}return IntPtr.Zero;}
}


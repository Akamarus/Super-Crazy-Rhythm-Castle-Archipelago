using RhythmCastleAP;
static void Eq<T>(T expected,T actual,string label) { if(!EqualityComparer<T>.Default.Equals(expected,actual)) throw new Exception(label); }
const string root="Root/GameRoom_Hub5A_Logic/Objects/ExternalDoor/ExternalEntrance/";
Eq<bool?>(true,HubEscapePolicy.GateActive(true,true,true,root+"DoorToHR5B_Open"),"return gate open");
Eq<bool?>(false,HubEscapePolicy.GateActive(true,true,true,root+"DoorToHR5B_Closed"),"closed collider hidden");
foreach(var path in new[]{root+"OldTom_PrisonDoor_Closed",root+"DoorToHR5B_Closed/Child","Other/"+root+"DoorToHR5B_Closed"})
 Eq<bool?>(null,HubEscapePolicy.GateActive(true,true,true,path),"other objects unchanged");
foreach(var state in new[]{(false,true,true),(true,false,true),(true,true,false)})
 Eq<bool?>(null,HubEscapePolicy.GateActive(state.Item1,state.Item2,state.Item3,root+"DoorToHR5B_Open"),"gate authorization");
foreach(var destination in new[]{"GameRoom_Hub1A","GameRoom_Hub1B"}) {
 foreach(bool? tears in new bool?[]{false,null}) Eq(true,HubEscapePolicy.RedirectLobbyExit(true,true,"GameRoom_Hub5A",destination,tears),"unsafe lobby exit");
 Eq(false,HubEscapePolicy.RedirectLobbyExit(true,true,"GameRoom_Hub5A",destination,true),"completed lobby route preserved");
}
foreach(var origin in new[]{"GameRoom_Hub6","GameRoom_Hub5B","GameRoom_Hub1A"}) Eq(false,HubEscapePolicy.RedirectLobbyExit(true,true,origin,"GameRoom_Hub1A",false),"other origins preserved");
Eq(false,HubEscapePolicy.RedirectLobbyExit(false,true,"GameRoom_Hub5A","GameRoom_Hub1A",false),"disabled");
Eq(false,HubEscapePolicy.RedirectLobbyExit(true,false,"GameRoom_Hub5A","GameRoom_Hub1A",false),"incompatible");
Eq(false,HubEscapePolicy.RedirectLobbyExit(true,true,"GameRoom_Hub5A","GameRoom_Hub5B",false),"phone return preserved");


Eq(true,HubEscapePolicy.ClearExitRequirement(true,true,true,root.TrimEnd('/')), "exact exit switch requirement cleared");
foreach(var path in new[]{root+"OldTom_PrisonDoor", root+"OnTrigger", "Root/GameRoom_Hub5A_Logic/Objects/EntranceDoor/ExternalEntrance"})
 Eq(false,HubEscapePolicy.ClearExitRequirement(true,true,true,path), "other interactions retain requirements");
foreach(var state in new[]{(false,true,true),(true,false,true),(true,true,false)})
 Eq(false,HubEscapePolicy.ClearExitRequirement(state.Item1,state.Item2,state.Item3,root.TrimEnd('/')), "exit requirement authorization");

Console.WriteLine("Hub escape regressions passed");

Eq(true,HubEscapeNative.TryClear((IntPtr)1,out _),"native switch cleared");
Eq(1,Il2CppInterop.Runtime.IL2CPP.Writes,"one reference write");
Eq(true,HubEscapeNative.TryClear((IntPtr)1,out _),"already clear idempotent");
Eq(1,Il2CppInterop.Runtime.IL2CPP.Writes,"no repeat write");
Il2CppInterop.Runtime.IL2CPP.Reference=(IntPtr)2;
Il2CppInterop.Runtime.IL2CPP.Flag=999;
Eq(false,HubEscapeNative.TryClear((IntPtr)1,out _),"wrong flag rejected");
Eq(1,Il2CppInterop.Runtime.IL2CPP.Writes,"wrong flag unchanged");
Il2CppInterop.Runtime.IL2CPP.Flag=116001;
Il2CppInterop.Runtime.IL2CPP.WrongClass=true;
Eq(false,HubEscapeNative.TryClear((IntPtr)1,out _),"wrong class rejected");
Il2CppInterop.Runtime.IL2CPP.WrongClass=false;
Il2CppInterop.Runtime.IL2CPP.RejectWrite=true;
Eq(false,HubEscapeNative.TryClear((IntPtr)1,out _),"failed write detected");
Console.WriteLine("Native exit switch regressions passed");

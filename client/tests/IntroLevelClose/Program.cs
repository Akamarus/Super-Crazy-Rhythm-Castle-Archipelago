using RhythmCastleAP;
using System.Reflection;
void Check(bool pass,string name){if(!pass)throw new Exception(name);}
Check(!IntroLevelClose.TryQueue(null,out _),"missing game fails closed");
Check(!IntroLevelClose.TryQueue(typeof(string).Assembly,out _),"missing public contract fails closed");
Check(RequestSystem.Calls==0,"missing APIs submit nothing");
Check(IntroLevelClose.TryQueue(Assembly.GetExecutingAssembly(),out _),"redirect queues native Close action");
Check(RequestSystem.Calls==1 && ReferenceEquals(RequestSystem.Last?.ActionForNextRoom,LevelChangeAction.Close),"exact close action through native dispatcher");
RequestSystem.Throw=true;
Check(!IntroLevelClose.TryQueue(Assembly.GetExecutingAssembly(),out var error) && error.Contains("fixture rejection"),"dispatch rejection is reported");
Console.WriteLine("Intro level closure: 6 checks passed");
public sealed class LevelChangeAction {public static LevelChangeAction Close {get;}=new();}
public sealed class QueueLevelChangeForNextRoomChangeRequest {public LevelChangeAction ActionForNextRoom;public QueueLevelChangeForNextRoomChangeRequest(LevelChangeAction action){ActionForNextRoom=action;}}
public static class RequestSystem {public static int Calls;public static bool Throw;public static QueueLevelChangeForNextRoomChangeRequest? Last;public static void SubmitRequest<T>(T request){if(Throw)throw new Exception("fixture rejection");Calls++;Last=request as QueueLevelChangeForNextRoomChangeRequest;}}

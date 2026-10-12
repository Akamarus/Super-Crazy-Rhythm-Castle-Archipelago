using System.Reflection;
namespace RhythmCastleAP;

// Called only after a confirmed intro-to-hub rewrite. Let native room loading
// close the skipped level instead of retaining its IN_LEVEL state in the hub.
internal static class IntroLevelClose
{
    internal static bool TryQueue(Assembly? game, out string detail)
    {
        detail = "native close request unavailable";
        try
        {
            Type? actionType = game?.GetType("LevelChangeAction");
            Type? requestType = game?.GetType("QueueLevelChangeForNextRoomChangeRequest");
            Type? dispatcher = game?.GetType("RequestSystem");
            if (actionType == null || requestType == null || dispatcher == null) return false;
            object? action = actionType.GetProperty("Close", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            ConstructorInfo? constructor = requestType.GetConstructor(new[] { actionType });
            MethodInfo? submit = dispatcher.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .SingleOrDefault(m => m.Name == "SubmitRequest" && m.IsGenericMethodDefinition &&
                    m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 1);
            if (action == null || constructor == null || submit == null) return false;
            object request = constructor.Invoke(new[] { action });
            submit.MakeGenericMethod(requestType).Invoke(null, new[] { request });
            detail = "native Close action queued for redirected room transition";
            return true;
        }
        catch (Exception ex) { detail = ex.GetBaseException().Message; return false; }
    }
}

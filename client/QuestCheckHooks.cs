using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RhythmCastleAP;

internal static class QuestCheckHooks
{
    internal static bool Ready { get; private set; }
    internal static int Install(Harmony harmony)
    {
        int count = QuestNativeVirtualHooks.Install();
        foreach (var target in new[] {
            ("PlayerSaveRequestProcessor", "ProcessRequest", "RecordCharacterUnlockedValueInSaveDataRequest", nameof(CharacterPrefix)),
            ("SIUtil.Scripting.Sequence", "ResetAndBegin", "", nameof(HandInSequencePrefix)),
        }) {
            try {
                Type? owner = ReflectionUtil.SafeGetTypes(ReflectionUtil.GameAssembly!).SingleOrDefault(t => t.Name == target.Item1 || t.FullName == target.Item1);
                MethodInfo? method = owner?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .SingleOrDefault(m => m.Name == target.Item2 && (target.Item3 == "" ? m.GetParameters().Length == 0 :
                        m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.Name == target.Item3));
                if (method == null) throw new MissingMethodException(target.Item1, target.Item2);
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(QuestCheckHooks).GetMethod(target.Item4, BindingFlags.NonPublic | BindingFlags.Static)!));
                count++;
            } catch (Exception ex) { Plugin.LoggerInstance?.LogError($"[SCRC-AP] Quest hook {target.Item1}.{target.Item2} unavailable: {ex.GetBaseException().Message}"); }
        }
        Ready = count == 7 && QuestSharedConditionHook.Ready;
        Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] QUEST HOOKS ready={Ready} dedicated={count}/7 sharedFlagCondition={QuestSharedConditionHook.Ready}.");
        return count;
    }
    private static bool CharacterPrefix(object[] __args) => __args.Length != 1 || !QuestChecks.SuppressCharacter(__args[0]);
    internal static bool HandInSequencePrefix(object __instance)
    {
        if (!QuestChecks.Enabled) return true;
        return PathFor(__instance) switch {
            "Root/GameRoom_Hub1_Logic/GameRoom_Hub1A_Script/MiscSequences/UnlockGhostCat" => QuestChecks.AdmitHandIn(QuestChecksPolicy.BatteryHandIn),
            "Root/GameRoom_27_Logic/GameRoom_27_Script/MiscSequences/UnlockManiac" => QuestChecks.AdmitHandIn(QuestChecksPolicy.MemoryHandIn),
            _ => true,
        };
    }
    internal static bool CharacterUnlockStepPrefix(object instance) =>
        !QuestCharacterRewardPolicy.SuppressNativeStep(QuestChecks.Enabled,
            QuestChecks.Enabled && QuestChecks.IsCurrentSaveBound(), PathFor(instance));

    internal static bool PopupPrefix(object __instance)
    {
        if (!QuestChecks.Enabled || !QuestChecks.IsCurrentSaveBound()) return true;
        string path = PathFor(__instance);
        return path != "Root/GameRoom_Hub1_Logic/GameRoom_Hub1A_Script/MiscSequences/UnlockGhostCat/PopupCharacterUnlocked" &&
            path != "Root/GameRoom_27_Logic/GameRoom_27_Script/MiscSequences/UnlockManiac/PopupExpositionDetailsUI";
    }
    internal static bool ConditionPrefix(object __instance, ref bool __result)
    {
        if (!QuestChecks.Enabled || !QuestChecksPolicy.IsConditionRoom(DeveloperHarness.CurrentRoomId)) return true;
        if (__instance is not Component component || component.name is not ("MeooIsUnlockedCondition" or "HaveMeooBatteryCondition" or "AlreadyHaveCatCharacter" or "Condition")) return true;
        if (!QuestChecks.IsCurrentSaveBound()) return true;
        var state = QuestChecks.State.Snapshot;
        string path = PathFor(__instance);
        bool? result = QuestChecksPolicy.ConditionResult(path, state.Checked,
            () => QuestChecks.ReadFlag("CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM"),
            () => QuestChecks.ReadFlag("OUTSIDE_HUB_PLUNGER_COLLECTED"));
        if (!result.HasValue) return true;
        __result = result.Value; return false;
    }
    internal static string PathFor(object instance)
    {
        if (instance is not Component component) throw new InvalidOperationException("Quest hook instance is not a Component");
        var names = new List<string>();
        Transform? current = component.transform;
        for (; current != null && names.Count < 40; current = current.parent)
            names.Add(current.name);
        if (current != null || names.Count == 0) throw new InvalidOperationException("Quest object hierarchy is unreadable or exceeds path limit");
        names.Reverse(); return string.Join("/", names);
    }
}

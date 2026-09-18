namespace RhythmCastleAP;

internal static class QuestCharacterRewardPolicy
{
    // Suppress the derived Trigger only: base sequence completion and hand-in persist remain native.
    internal static bool SuppressNativeStep(bool enabled, bool boundSave, string path) =>
        enabled && boundSave && path is
            "Root/GameRoom_Hub1_Logic/GameRoom_Hub1A_Script/MiscSequences/UnlockGhostCat/UnlockGhostCatCharacter" or
            "Root/GameRoom_27_Logic/GameRoom_27_Script/MiscSequences/UnlockManiac/UnlockManiacCharacter";
}

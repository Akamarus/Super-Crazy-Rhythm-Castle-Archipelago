using RhythmCastleAP;

static void True(bool value, string name)
{
    if (!value) throw new InvalidOperationException($"Expected true: {name}");
}

static void False(bool value, string name)
{
    if (value) throw new InvalidOperationException($"Expected false: {name}");
}

True(NativeIdentityDiagnosticPolicy.IsHub6Candidate(
    "Root/GameRoom_Hub6_Logic/Objects/ConstructionBarrier_01", Array.Empty<string>()),
    "construction barrier path");
True(NativeIdentityDiagnosticPolicy.IsHub6Candidate(
    "Root/GameRoom_Hub6_Logic/UI/StarCounter", new[] { "UnityEngine.RectTransform" }),
    "star HUD path");
True(NativeIdentityDiagnosticPolicy.IsHub6Candidate(
    "Root/GameRoom_Hub6_Logic/UI/Status", new[] { "StarCounterDisplay" }),
    "star component type");
True(NativeIdentityDiagnosticPolicy.IsHub6Candidate(
    "Root/UI/HUD/StarCounter", new[] { "UnityEngine.RectTransform" }),
    "global star HUD path");
False(NativeIdentityDiagnosticPolicy.IsHub6Candidate(
    "Root/GameRoom_Hub6_Logic/Objects/RewardChests/Chest", new[] { "Hub06MedalScoreRewardChest" }),
    "reward chest excluded");
False(NativeIdentityDiagnosticPolicy.IsHub6Candidate(
    "Root/GameRoom_Hub2_Logic/Objects/ConstructionBarrier", Array.Empty<string>()),
    "wrong room excluded");

True(NativeIdentityDiagnosticPolicy.IsDifficultyType("AssignAndCommentOnMusicDifficultySequenceStep"),
    "known difficulty sequence");
True(NativeIdentityDiagnosticPolicy.IsDifficultyMember("IsProDifficultyAvailable"),
    "availability member");
False(NativeIdentityDiagnosticPolicy.IsDifficultyMember("SetScore"),
    "unrelated member");

Console.WriteLine("Native identity diagnostic policy tests passed.");

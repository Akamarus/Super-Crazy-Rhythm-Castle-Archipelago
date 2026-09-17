using System.Reflection;
using RhythmCastleAP;

internal static class PostLevelOneTests
{
    internal static void Run()
    {
        var sequence = new AssignAndCommentOnMusicDifficultySequenceStep();
        MethodInfo begin = typeof(AssignAndCommentOnMusicDifficultySequenceStep).GetMethod("Begin")!;
        bool Before(bool enabled = true, bool compatible = true) =>
            RootsPostLevelOnePresentation.BeforeSequence(enabled, compatible, begin, sequence);
        RootsPostLevelOnePresentation.NewSaveCreated();
        Check(Before() && sequence.Completions == 0, "new save preserves original sequence");
        RootsPostLevelOnePresentation.RecordLevelPersisted("Level_06");
        Check(Before(), "another level does not arm suppression");
        RootsPostLevelOnePresentation.RecordLevelPersisted("Level_05");
        Check(Before(enabled: false), "disabled Area Access preserves sequence");
        Check(Before(compatible: false), "incompatible slot preserves sequence");
        Check(!Before() && sequence.Completions == 1, "persisted Level 1 skips and completes exact sequence");
        Check(RootsPostLevelOnePresentation.BeforeSequence(true, true,
            typeof(AssignAndCommentOnMusicDifficultySequenceStep).GetMethod("ResetToStart")!, sequence),
            "non-Begin method remains vanilla");
        RootsPostLevelOnePresentation.NewSaveCreated();
        Check(Before() && sequence.Completions == 1, "new-save lifecycle clears previous completion state");
    }
    private static void Check(bool condition, string scenario)
    {
        if (!condition) throw new InvalidOperationException(scenario);
    }
}
internal sealed class AssignAndCommentOnMusicDifficultySequenceStep
{
    public int Completions { get; private set; }
    public void Begin() { }
    public void ResetToStart() { }
    private void MarkAsComplete() => Completions++;
}

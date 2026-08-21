using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

static PlantPipesDecision Decide(
    bool compatible = true,
    bool synchronized = true,
    int received = 0,
    bool save = true,
    bool processor = true,
    bool nativeOwned = false,
    bool attemptOutstanding = false,
    int retryCount = 0) =>
    PlantPipesReconciler.Decide(new PlantPipesSnapshot(
        compatible,
        synchronized,
        received,
        save,
        processor,
        nativeOwned,
        attemptOutstanding,
        retryCount));

Equal(PlantPipesDecision.Ignore, Decide(received: 0), "not owned");
Equal(PlantPipesDecision.Ignore, Decide(compatible: false, received: 1), "incompatible seed");
Equal(PlantPipesDecision.Ignore, Decide(synchronized: false, received: 1), "history not synchronized");
Equal(PlantPipesDecision.WaitForSave, Decide(received: 1, save: false), "history before save");
Equal(PlantPipesDecision.WaitForProcessor, Decide(received: 1, processor: false), "save before processor");
Equal(PlantPipesDecision.Apply, Decide(received: 1), "grant missing ability");
Equal(PlantPipesDecision.Verify, Decide(received: 1, attemptOutstanding: true), "verify submitted grant");
Equal(PlantPipesDecision.Satisfied, Decide(received: 2, nativeOwned: true), "duplicate history idempotent");

Equal<TimeSpan?>(TimeSpan.FromMilliseconds(250), PlantPipesReconciler.NextRetryDelay(0), "first retry");
Equal<TimeSpan?>(TimeSpan.FromMilliseconds(500), PlantPipesReconciler.NextRetryDelay(1), "second retry");
Equal<TimeSpan?>(TimeSpan.FromSeconds(1), PlantPipesReconciler.NextRetryDelay(2), "third retry");
Equal<TimeSpan?>(TimeSpan.FromSeconds(2), PlantPipesReconciler.NextRetryDelay(3), "fourth retry");
Equal<TimeSpan?>(TimeSpan.FromSeconds(4), PlantPipesReconciler.NextRetryDelay(4), "last retry");
Equal<TimeSpan?>(null, PlantPipesReconciler.NextRetryDelay(5), "retry is bounded");
Equal<TimeSpan?>(null, PlantPipesReconciler.NextRetryDelay(-1), "negative retry rejected");

Console.WriteLine("Plant Pipes reconciler policy tests passed.");

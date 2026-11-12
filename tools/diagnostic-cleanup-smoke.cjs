const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');

const client = path.join(process.argv[2] || process.cwd(), 'client');
const source = fs.readFileSync(path.join(client, 'Plugin.cs'), 'utf8');

// These probes used Unity's update loop or observed every native result. The
// movement and medal repairs below use some of the same entry points.
for (const hook of [
    'LogTransitionConstructors(request.GetType());',
    'string state = PlantPipesRandomization.ReadDiagnosticState();',
    'Level6Discovery.Record', 'Level8Discovery.Record', 'AddComponent<MovementCapture>',
    'MovementTimingProbe.Shutdown', 'PatchRootsOwnerDiagnostic();',
    'ClientPerformance.Measure', 'ClientPerformance.Frame', 'SelectorAvailabilityDiagnostics.TickUnity',
    'AddComponent<DeveloperHotkeys>', 'AddComponent<BottomHudDiagnosticKeeper>',
    'EnableTestHarness', 'EnableVerboseDiscovery', 'EnablePerformanceDiagnostics',
    'EnableCassettePointerBoundPersistenceAcceptance', 'TryEmitExactConditionDiagnostic(room);',
    'EmitNativeBindingDiagnostic();', 'LogPersistenceTargetOwnershipDiagnostic(diagnosticWave);',
    'nameof(CassetteSaveTransactionPatches.PlayerSaveWriteCompletedEventPostfix)',
    'MusicLabDiscovery.RecordPersistedEvent(evt,',
]) {
    assert.ok(!source.includes(hook), 'Retired diagnostic hook reintroduced: ' + hook);
}

for (const call of [
    'PlayerMovementTiming.Install();', 'PlayerMovementTiming.Reset();', 'PlayerMovementTiming.Shutdown();',
    'MontageAnimalTick.Install();', 'MusicLabDiscovery.ReconcileCollectedRewardChests()',
    'MusicLabDiscovery.PollGarageCartridgeSelection()', 'AddComponent<MusicLabProgressionKeeper>()',
    '_productionPersistence.TryEnqueue(identity, diagnosticWave.Songs);',
    'ProbeGarageScoringState("result");', 'ProbeCleanMedalState(level, variant, score);',
]) {
    assert.ok(source.includes(call), 'Functional movement or progression call missing: ' + call);
}

for (const file of [
    'MovementCapture.cs', 'MovementTimingProbe.cs', 'MovementCaptureFormatting.cs',
    'MovementTimingBuffer.cs', 'ClientPerformance.cs', 'SelectorAvailabilityDiagnostics.cs',
]) {
    assert.ok(!fs.existsSync(path.join(client, file)), 'Retired diagnostic source restored: ' + file);
}

const musicLab = source.indexOf('internal static class MusicLabDiscovery');
assert.ok(musicLab >= 0, 'Music Lab callbacks missing');
for (const method of ['RecordResultRequest', 'RecordScoredSongRequest']) {
    const start = source.indexOf('public static void ' + method + '(', musicLab);
    const end = source.indexOf('\n    private static', start);
    assert.ok(start >= 0 && end > start, 'Cannot inspect Music Lab callback: ' + method);
    assert.ok(!source.slice(start, end).includes('DumpRelevantMembers('),
        'Live Music Lab callback dumps members: ' + method);
}

console.log('Passed diagnostic cleanup checks for movement, cassettes and medals.');

const fs=require('fs'),p=require('path');
const root=process.argv[2] || process.cwd();
const source=fs.readFileSync(p.join(root,'client/Plugin.cs'),'utf8');
const forbidden=['LogTransitionConstructors(request.GetType());','string state = PlantPipesRandomization.ReadDiagnosticState();','Level6Discovery.Record','Level8Discovery.Record','AddComponent<MovementCapture>','MovementTimingProbe.Shutdown','PatchRootsOwnerDiagnostic();','ClientPerformance.Measure','ClientPerformance.Frame','SelectorAvailabilityDiagnostics.TickUnity','AddComponent<DeveloperHotkeys>','AddComponent<BottomHudDiagnosticKeeper>','EnableTestHarness','EnableVerboseDiscovery','EnablePerformanceDiagnostics','EnableCassettePointerBoundPersistenceAcceptance','TryEmitExactConditionDiagnostic(room);','EmitNativeBindingDiagnostic();','LogPersistenceTargetOwnershipDiagnostic(diagnosticWave);','nameof(CassetteSaveTransactionPatches.PlayerSaveWriteCompletedEventPostfix)'];
for(const token of forbidden) if(source.includes(token)) throw Error('Retired diagnostic hook reintroduced: '+token);
for(const token of ['PlayerMovementTiming.Install();','PlayerMovementTiming.Reset();','PlayerMovementTiming.Shutdown();','MontageAnimalTick.Install();','MusicLabDiscovery.ReconcileCollectedRewardChests()','MusicLabDiscovery.PollGarageCartridgeSelection()','AddComponent<MusicLabProgressionKeeper>();','_productionPersistence.TryEnqueue(identity, diagnosticWave.Songs);']) if(!source.includes(token)) throw Error('Functional repair/check removed: '+token);
const project=fs.readFileSync(p.join(root,'client/RhythmCastleAP.csproj'),'utf8');
for(const file of ['MovementCaptureFormatting.cs','MovementTimingBuffer.cs','ClientPerformance.cs']) if(!project.includes(file)) throw Error('Historical recording helper could enter production: '+file);
for(const file of ['MovementCapture.cs','MovementTimingProbe.cs']) if(fs.existsSync(p.join(root,'client',file))) throw Error('Retired recorder implementation restored: '+file);
console.log('PASS: retired runtime diagnostics absent; accepted movement and functional progression retained');

for (const method of ["RecordResultRequest", "RecordScoredSongRequest"]) {
 const start=source.indexOf("public static void "+method+"(", source.indexOf("internal static class MusicLabDiscovery"));
 const end=source.indexOf("\n    private static",start);
 const region=source.slice(start,end);
 if(region.includes("DumpRelevantMembers(")) throw Error("Live Music Lab callback dumps members: "+method);
}
if(source.includes("MusicLabDiscovery.RecordPersistedEvent(evt,")) throw Error("Diagnostic persisted-result callback remains wired");
for(const call of ["ProbeGarageScoringState(\"result\");", "ProbeCleanMedalState(level, variant, score);"]) if(!source.includes(call)) throw Error("Functional medal probe missing: "+call);

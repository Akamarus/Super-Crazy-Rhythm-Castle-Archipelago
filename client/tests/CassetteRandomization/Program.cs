using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}"); }
static void SequenceEqual(IEnumerable<string> expected, IEnumerable<string> actual, string scenario) { var e=expected.ToArray(); var a=actual.ToArray(); if (!e.SequenceEqual(a, StringComparer.Ordinal)) throw new InvalidOperationException($"{scenario}: expected [{string.Join(", ",e)}], got [{string.Join(", ",a)}]"); }

string[] expectedCatalog =
{
 "The Little Things|THE_LITTLE_THINGS|The Little Things Cassette|Cassette Source - The Little Things|Level_24:LevelVariant_Default",
 "No Plan B|NO_PLAN_B|No Plan B Cassette|Cassette Source - No Plan B|Level_22:LevelVariant_Default",
 "Jolt City|JOLT_CITY|Jolt City Cassette|Cassette Source - Jolt City|Level_23:LevelVariant_Default",
 "Quieres Bailar|QUIERES_BAILAR|Quieres Bailar Cassette|Cassette Source - Quieres Bailar|Level_16:LevelVariant_Default",
 "Quicksand|QUICKSAND|Quicksand Cassette|Music Lab - 32 Point Chest|",
 "Gold|GOLD|Gold Cassette|Cassette Source - Gold|Level_05:LevelVariant_Default,Level_11:LevelVariant_Default,Level_11:LevelVariant_DevilMode",
 "I Got Money|I_GOT_MONEY|Money Cassette|Level 2 - Money Cassette|Level_06:LevelVariant_Default,Level_06:LevelVariant_BeeMode",
 "Hippo and Frog|HIPPO_AND_FROG|Hippo and Frog Cassette|Cassette Source - Hippo and Frog|Level_07:LevelVariant_Default",
 "On the Way|ON_THE_WAY|On the Way Cassette|Cassette Source - On the Way|Level_08:LevelVariant_Default,Level_11:LevelVariant_Default,Level_11:LevelVariant_DevilMode",
 "Badass|BADASS|Badass Cassette|Cassette Source - Badass|Level_09:LevelVariant_Default",
 "Heavy Metal|HEAVY_METAL|Heavy Metal Cassette|Cassette Source - Heavy Metal|Level_09:LevelVariant_Default",
 "AOK|AOK|AOK Cassette|Cassette Source - AOK|Level_02:LevelVariant_Default,Level_02:LevelVariant_DevilMode",
 "Rainbow Melodies|RAINBOW_MELODIES|Rainbow Melodies Cassette|Cassette Source - Rainbow Melodies|Level_11:LevelVariant_Default,Level_11:LevelVariant_DevilMode,Level_19:LevelVariant_Default",
 "Sneaking|SNEAKING_LOOP|Sneaking Cassette|Cassette Source - Sneaking|Level_19:LevelVariant_Default,Level_20:LevelVariant_Default",
 "The Heist|THE_HEIST|The Heist Cassette|Cassette Source - The Heist|Level_20:LevelVariant_Default",
 "Money|MONEY_DUB|Money Dub Cassette|Cassette Source - Money|Level_01:LevelVariant_Default",
 "Lets Go|LETS_GO|Lets Go Cassette|Cassette Source - Lets Go|Level_12:LevelVariant_Default,Level_12:LevelVariant_BeeMode",
 "Bounce|BOUNCE|Bounce Cassette|Cassette Source - Bounce|Level_15:LevelVariant_Default",
 "Epical|THE_EPICAL|Epical Cassette|Cassette Source - Epical|Level_21:LevelVariant_Default",
 "Hollywood Trailer|HOLLYWOOD_TRAILER|Hollywood Trailer Cassette|Cassette Source - Hollywood Trailer|Level_21:LevelVariant_Default",
 "False Data|FALSE_DATA|False Data Cassette|Cassette Source - False Data|Level_21:LevelVariant_Default",
 "Gotta Get Up|GOTTA_GET_UP|Gotta Get Up Cassette|Cassette Source - Gotta Get Up|Level_03:LevelVariant_Default",
 "Fumblin Around|FUMBLIN_AROUND|Fumblin Around Cassette|Cassette Source - Fumblin Around|Level_13:LevelVariant_Default,Level_13:LevelVariant_DevilMode",
 "Party Non Stop|PARTY_NON_STOP|Party Non Stop Cassette|Cassette Source - Party Non Stop|Level_25:LevelVariant_Default",
 "Keep On Hustlin|KEEP_ON_HUSTLIN|Keep On Hustlin Cassette|Cassette Source - Keep On Hustlin|Level_14:LevelVariant_Default,Level_14:LevelVariant_DevilMode",
 "Another Day In Paradise|ANOTHER_DAY_IN_PARADISE|Another Day In Paradise Cassette|Cassette Source - Another Day In Paradise|Level_28:LevelVariant_Default",
 "Flamenco|FLAMENCO|Flamenco Cassette|Music Lab - 64 Point Chest|",
 "Ten-Four Good Buddy|TEN_FOUR_GOOD_BUDDY|Ten-Four Good Buddy Cassette|Music Lab - 89 Point Chest|",
 "Zen|ZEN|Zen Cassette|Music Lab - 111 Point Chest|",
 "Wiggle|WIGGLE|Wiggle Cassette|Music Lab - 140 Point Chest|",
};

Equal(30, CassetteCatalog.All.Count, "catalog count");
Equal(30, CassetteCatalog.All.Select(x=>x.DisplaySong).Distinct(StringComparer.Ordinal).Count(), "unique display songs");
Equal(30, CassetteCatalog.ByNativeSong.Count, "unique native songs");
Equal(30, CassetteCatalog.ByItemName.Count, "unique items");
Equal(30, CassetteCatalog.All.Select(x=>x.SourceName).Distinct(StringComparer.Ordinal).Count(), "unique sources");
SequenceEqual(expectedCatalog, CassetteCatalog.All.Select(x=>$"{x.DisplaySong}|{x.NativeSong}|{x.ItemName}|{x.SourceName}|{string.Join(",",x.Triggers.Select(t=>$"{t.Level}:{t.Variant}"))}"), "exact reviewed catalog");
var money=CassetteCatalog.ByItemName["Money Cassette"];
Equal("I_GOT_MONEY",money.NativeSong,"Money native identity"); Equal("Level 2 - Money Cassette",money.SourceName,"Money source"); Equal(true,money.ReusesExistingLocation,"Money reused location");
SequenceEqual(new[]{"Music Lab - 32 Point Chest","Music Lab - 64 Point Chest","Music Lab - 89 Point Chest","Music Lab - 111 Point Chest","Music Lab - 140 Point Chest"},CassetteCatalog.All.Where(x=>x.SourceType==CassetteSourceType.MusicLabPointChest).Select(x=>x.SourceName),"exact point chests");

var d=CassetteRandomizationPolicy.DecideLevelEvaluation("Level_06","LevelVariant_Default",true,new Dictionary<string,string>{{"I_GOT_MONEY",CassetteRandomizationPolicy.Invalid}});
Equal(false,d.AllowNative,"Money suppresses native"); SequenceEqual(new[]{"Level 2 - Money Cassette"},d.SourceLocationsToQueue,"Money source queued"); SequenceEqual(new[]{"I_GOT_MONEY"},d.NativeSongsToSuppress,"Money suppressed");
foreach(var status in new[]{CassetteRandomizationPolicy.Invalid,CassetteRandomizationPolicy.HaveNotEarned}) Equal(false,CassetteRandomizationPolicy.DecideLevelEvaluation("Level_06","LevelVariant_BeeMode",true,new Dictionary<string,string>{{"I_GOT_MONEY",status}}).AllowNative,$"Bee alias {status}");
foreach(var status in new[]{CassetteRandomizationPolicy.HaveInBag,CassetteRandomizationPolicy.HaveDeposited}) { d=CassetteRandomizationPolicy.DecideLevelEvaluation("Level_06","LevelVariant_Default",true,new Dictionary<string,string>{{"I_GOT_MONEY",status}}); Equal(true,d.AllowNative,$"owned {status}"); Equal(0,d.SourceLocationsToQueue.Count,"owned queues none"); }
Equal(true,CassetteRandomizationPolicy.DecideLevelEvaluation("Level_06","LevelVariant_Default",false,new Dictionary<string,string>{{"I_GOT_MONEY",CassetteRandomizationPolicy.Invalid}}).AllowNative,"failure native");
Equal(true,CassetteRandomizationPolicy.DecideLevelEvaluation("Level_06","LevelVariant_DevilMode",true,new Dictionary<string,string>{{"I_GOT_MONEY",CassetteRandomizationPolicy.Invalid}}).AllowNative,"wrong variant native");
Equal(true,CassetteRandomizationPolicy.DecideLevelEvaluation("Level_99","LevelVariant_Default",true,new Dictionary<string,string>()).AllowNative,"unrelated native");
Equal(true,CassetteRandomizationPolicy.DecideLevelEvaluation("Level_06","LevelVariant_Default",true,new Dictionary<string,string>{{"I_GOT_MONEY","UNREADABLE"}}).AllowNative,"unreadable native");
Equal(true,CassetteRandomizationPolicy.DecideLevelEvaluation("Level_06","LevelVariant_Default",true,new Dictionary<string,string>()).AllowNative,"missing native");
d=CassetteRandomizationPolicy.DecideLevelEvaluation("Level_09","LevelVariant_Default",true,new Dictionary<string,string>{{"BADASS",CassetteRandomizationPolicy.Invalid},{"HEAVY_METAL",CassetteRandomizationPolicy.HaveNotEarned}});
Equal(false,d.AllowNative,"multi suppresses"); SequenceEqual(new[]{"Cassette Source - Badass","Cassette Source - Heavy Metal"},d.SourceLocationsToQueue,"multi sources"); SequenceEqual(new[]{"BADASS","HEAVY_METAL"},d.NativeSongsToSuppress,"multi songs");
d=CassetteRandomizationPolicy.DecideLevelEvaluation("Level_09","LevelVariant_Default",true,new Dictionary<string,string>{{"BADASS",CassetteRandomizationPolicy.Invalid}}); Equal(true,d.AllowNative,"partial unreadable native"); Equal(0,d.SourceLocationsToQueue.Count,"unsafe partial queues none");
Equal(false,CassetteRandomizationPolicy.UseSelectedSaveChangedEventHook,"crashing hook disabled");
Equal(true, CassetteRandomizationPolicy.ShouldSuppressNativePointChestGrant("QUICKSAND", CassetteRandomizationPolicy.HaveInBag, fromArchipelago:false), "native point-chest Quicksand bag grant is randomized");
Equal(false, CassetteRandomizationPolicy.ShouldSuppressNativePointChestGrant("QUICKSAND", CassetteRandomizationPolicy.HaveInBag, fromArchipelago:true), "AP receipt may grant Quicksand later");
Equal(false, CassetteRandomizationPolicy.ShouldSuppressNativePointChestGrant("I_GOT_MONEY", CassetteRandomizationPolicy.HaveInBag, fromArchipelago:false), "level cassette grants are handled by the evaluator hook");
Equal(false, CassetteRandomizationPolicy.ShouldSuppressNativePointChestGrant("QUICKSAND", CassetteRandomizationPolicy.HaveDeposited, fromArchipelago:false), "deposited state is never intercepted");

foreach (var entry in CassetteCatalog.All)
{
    var runtime = new CassetteReceiptRuntime();
    runtime.Configure(true); runtime.OnLifecyclePoint();
    Equal(false, runtime.TryBeginReconcileAttempt(out _), $"{entry.ItemName} absent means no work");
    Equal(true, runtime.NoteReceived(entry.ItemName), $"{entry.ItemName} recognized");
    runtime.OnLifecyclePoint();
    Equal(true, runtime.TryBeginReconcileAttempt(out string? nativeSong), $"{entry.ItemName} begins reconciliation");
    Equal(entry.NativeSong, nativeSong, $"{entry.ItemName} resolves native song");
    foreach (string unowned in new[] { CassetteRandomizationPolicy.Invalid, CassetteRandomizationPolicy.HaveNotEarned })
    {
        Equal(CassetteReceiptDecision.RequestHaveInBag, runtime.ObserveNativeStatus(entry.NativeSong, unowned, true, true), $"{entry.ItemName} grants from {unowned}");
        Equal(CassetteRandomizationPolicy.HaveInBag, runtime.RequestedNativeStatus, $"{entry.ItemName} requests bag state");
    }
    foreach (string owned in new[] { CassetteRandomizationPolicy.HaveInBag, CassetteRandomizationPolicy.HaveDeposited })
    {
        Equal(owned == CassetteRandomizationPolicy.HaveDeposited ? CassetteReceiptDecision.VerifiedDeposited : CassetteReceiptDecision.VerifiedBag,
            runtime.ObserveNativeStatus(entry.NativeSong, owned, true, true), $"{entry.ItemName} preserves {owned}");
        Equal<string?>(null, runtime.RequestedNativeStatus, $"{entry.ItemName} makes no request for {owned}");
    }
    var unrelated = CassetteCatalog.All.First(x => x.ItemName != entry.ItemName);
    var other = new CassetteReceiptRuntime(); other.Configure(true); other.NoteReceived(unrelated.ItemName);
    Equal(false, other.OwnsNativeSong(entry.NativeSong), $"different item does not grant {entry.NativeSong}");
}

int nativeReads = 0, nativeWrites = 0;
var scheduler = new CassetteReceiptScheduler(); scheduler.Configure(true);
Equal(true, scheduler.NoteReceived("Money Cassette"), "scheduler recognizes receipt");
Equal(0, nativeReads, "receipt callback performs no native read");
Equal(0, nativeWrites, "receipt callback performs no native write");
var tick1 = scheduler.Tick(
    _ => { nativeReads++; return new CassetteNativeObservation(true, true, CassetteRandomizationPolicy.Invalid); },
    _ => { nativeWrites++; return true; });
Equal(CassetteReceiptDecision.RequestHaveInBag, tick1.Decision, "first tick requests native bag");
Equal(1, nativeReads, "first tick reads once"); Equal(1, nativeWrites, "first tick writes once");
var tick2 = scheduler.Tick(
    _ => { nativeReads++; return new CassetteNativeObservation(true, true, CassetteRandomizationPolicy.HaveInBag); },
    _ => { nativeWrites++; return true; });
Equal(CassetteReceiptDecision.VerifiedBag, tick2.Decision, "later tick verifies bag");
Equal(2, nativeReads, "later tick reads once"); Equal(1, nativeWrites, "later verification does not rewrite");

var lifecycle = new CassetteReceiptRuntime(); lifecycle.Configure(true);
Equal(false, lifecycle.NoteReceived("not a cassette"), "unknown item ignored");
Equal(true, lifecycle.NoteReceived("Money Cassette"), "Money receipt recognized");
Equal(true, lifecycle.NoteReceived("Money Cassette"), "duplicate history recognized idempotently");
Equal(1, lifecycle.OwnedCount, "duplicate history stores one ownership");
lifecycle.OnLifecyclePoint();
Equal(true, lifecycle.TryBeginReconcileAttempt(out string? lifecycleSong), "owned song begins attempt");
Equal(CassetteReceiptDecision.SaveUnavailable, lifecycle.ObserveNativeStatus(lifecycleSong!, null, false, true), "unavailable save retries later");
Equal(CassetteReceiptDecision.ProcessorUnavailable, lifecycle.ObserveNativeStatus(lifecycleSong!, CassetteRandomizationPolicy.Invalid, true, false), "unavailable processor retries later");
for (int i=1; i<CassetteReceiptRuntime.MaxRetryAttempts; i++)
{
    Equal(true, lifecycle.TryBeginReconcileAttempt(out lifecycleSong), $"retry attempt {i + 1} begins");
    lifecycle.ObserveNativeStatus(lifecycleSong!, null, false, true);
}
Equal(false, lifecycle.TryBeginReconcileAttempt(out _), "retry window bounded");
lifecycle.OnLifecyclePoint();
Equal(true, lifecycle.TryBeginReconcileAttempt(out _), "later lifecycle rearms work");

var replay = new CassetteReceiptRuntime(); replay.Configure(true);
foreach (var entry in CassetteCatalog.All) replay.NoteReceived(entry.ItemName);
Equal(30, replay.OwnedCount, "history reconstructs all ownership"); replay.OnLifecyclePoint();
var attempted = new HashSet<string>(StringComparer.Ordinal);
while (replay.TryBeginReconcileAttempt(out string? song)) attempted.Add(song!);
Equal(30, attempted.Count, "one lifecycle attempts all received cassettes");
replay.ObserveNativeStatus("ZEN", CassetteRandomizationPolicy.HaveDeposited, true, true); replay.OnLifecyclePoint();
var rearmed = new HashSet<string>(StringComparer.Ordinal);
while (replay.TryBeginReconcileAttempt(out string? song)) rearmed.Add(song!);
Equal(false, rearmed.Contains("ZEN"), "deposited is terminal after reconnect");
Equal(29, rearmed.Count, "other ownership rearms after reconnect");

var twoSaves = new CassetteReceiptRuntime(); twoSaves.Configure(true); twoSaves.NoteReceived("Money Cassette"); twoSaves.OnLifecyclePoint();
twoSaves.TryBeginReconcileAttempt(out string? saveASong);
Equal(CassetteReceiptDecision.VerifiedBag, twoSaves.ObserveNativeStatus(saveASong!, CassetteRandomizationPolicy.HaveInBag, true, true), "save A bag is satisfied");
Equal(false, twoSaves.TryBeginReconcileAttempt(out _), "save A does not repeat satisfied bag");
twoSaves.OnLifecyclePoint();
Equal(true, twoSaves.TryBeginReconcileAttempt(out string? saveBSong), "compatible save B rearms bag-owned cassette");
Equal(CassetteReceiptDecision.RequestHaveInBag, twoSaves.ObserveNativeStatus(saveBSong!, CassetteRandomizationPolicy.Invalid, true, true), "save B receives missing cassette");

var depositedAcrossSaves = new CassetteReceiptRuntime(); depositedAcrossSaves.Configure(true); depositedAcrossSaves.NoteReceived("Zen Cassette"); depositedAcrossSaves.OnLifecyclePoint();
depositedAcrossSaves.TryBeginReconcileAttempt(out string? depositedSong);
Equal(CassetteReceiptDecision.VerifiedDeposited, depositedAcrossSaves.ObserveNativeStatus(depositedSong!, CassetteRandomizationPolicy.HaveDeposited, true, true), "deposited observed");
depositedAcrossSaves.OnLifecyclePoint();
Equal(false, depositedAcrossSaves.TryBeginReconcileAttempt(out _), "deposited remains terminal across saves");

var disabledReceipt = new CassetteReceiptRuntime(); disabledReceipt.Configure(false); disabledReceipt.NoteReceived("Money Cassette"); disabledReceipt.OnLifecyclePoint();
Equal(false, disabledReceipt.TryBeginReconcileAttempt(out _), "disabled session preserves vanilla");
Equal(false, CassetteReceiptRuntime.UseSelectedSaveChangedEventHook, "unsafe save-slot hook prohibited");

foreach (var triggerGroup in CassetteCatalog.All
             .Where(x => x.SourceType == CassetteSourceType.LevelEarnedReward)
             .SelectMany(x => x.Triggers)
             .Distinct()
             .OrderBy(x => x.Level, StringComparer.Ordinal)
             .ThenBy(x => x.Variant, StringComparer.Ordinal))
{
    var mapped = CassetteCatalog.ForLevelSource(triggerGroup.Level, triggerGroup.Variant);
    var statuses = mapped.ToDictionary(x => x.NativeSong, _ => CassetteRandomizationPolicy.HaveNotEarned, StringComparer.Ordinal);
    var aliasDecision = CassetteRandomizationPolicy.DecideLevelEvaluation(triggerGroup.Level, triggerGroup.Variant, true, statuses);
    Equal(false, aliasDecision.AllowNative, $"verified alias {triggerGroup.Level}/{triggerGroup.Variant} intercepts");
    SequenceEqual(mapped.Select(x => x.SourceName), aliasDecision.SourceLocationsToQueue, $"verified alias {triggerGroup.Level}/{triggerGroup.Variant} queues exact sources");
}
Console.WriteLine("Cassette randomization catalog and source policy tests passed.");

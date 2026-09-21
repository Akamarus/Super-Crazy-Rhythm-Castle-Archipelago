using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace RhythmCastleAP;

internal sealed record ApStarGoalRecord(int Schema, string Identity, int RequiredStars, string NativeSave, bool Completed);
internal sealed class ApStarGoalJournal
{
    private readonly string _directory;
    internal ApStarGoalJournal(string directory) => _directory = directory;
    private string FileFor(string identity) => Path.Combine(_directory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))) + ".json");
    internal ApStarGoalRecord? Load(string identity, int required)
    {
        string path = FileFor(identity);
        if (!File.Exists(path)) return null;
        var record = JsonConvert.DeserializeObject<ApStarGoalRecord>(File.ReadAllText(path));
        if (record == null || record.Schema != 1 || record.Identity != identity || record.RequiredStars != required ||
            !record.Completed || string.IsNullOrWhiteSpace(record.NativeSave))
            throw new InvalidDataException("AP Star goal journal identity or completion mismatch");
        return record;
    }
    internal void Save(ApStarGoalRecord record)
    {
        Directory.CreateDirectory(_directory);
        string path = FileFor(record.Identity), temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonConvert.SerializeObject(record));
        File.Move(temporary, path, true);
    }
}

internal static class ApStars
{
    internal static readonly ApStarState State = new();
    private static ApStarGoalJournal _journal = new(Path.Combine(BepInEx.Paths.ConfigPath, "RhythmCastleAP", "ap-star-goals"));
    private static Func<string, bool> _sendGoal = _ => false;
    private static string _identity = "", _startedLevel = "", _startedVariant = "", _startedSave = "";
    private static string _previewLevel = "", _previewVariant = "", _lastError = "";
    private static ApStarResult? _candidate;
    private static ApStarResult? _persistedCandidate;
    private static ApStarGoalRecord? _unsavedGoal;
    private static bool _boundToAppliedResult, _journalLoaded, _durableGoal;
    private static float _nextRetry, _previewExpires, _blockedExpires;
    private static string _blockedLevel = "", _blockedVariant = "", _blockedRoom = "", _blockedIdentity = "";
    private static GUIStyle? _blockedStyle;
    private static int _blockedFontSize;
    private static readonly HashSet<string> EaterRooms = new(StringComparer.Ordinal)
        { "GameRoom_Hub2", "GameRoom_Hub1A", "GameRoom_Hub5B", "GameRoom_Hub7", "GameRoom_Hub8" };

    internal static void Configure(Func<string, bool> sendGoal, string? journalDirectory = null)
    {
        _sendGoal = sendGoal;
        if (journalDirectory != null) _journal = new(journalDirectory);
        _identity = ""; _journalLoaded = _durableGoal = false; _unsavedGoal = null;
        OnNativeBoundary();
    }
    private static void ObserveIdentity()
    {
        string identity = State.Identity;
        if (_identity == identity) return;
        _identity = identity;
        _journalLoaded = _durableGoal = false; _unsavedGoal = null; _nextRetry = 0;
        _previewLevel = _previewVariant = "";
        OnNativeBoundary();
    }
    internal static void OnNativeBoundary()
    {
        _persistedCandidate = null;
        ResetAttempt();
    }
    private static void ResetAttempt()
    {
        _startedLevel = _startedVariant = _startedSave = "";
        _blockedLevel = _blockedVariant = ""; _blockedExpires = _previewExpires = 0;
        _candidate = null; _boundToAppliedResult = false;
    }
    internal static int ResolveTotal(int native)
    {
        if (State.Active && EaterRooms.Contains(DeveloperHarness.CurrentRoomId) && !ApStarEaterThresholds.IsReadyForRoom(DeveloperHarness.CurrentRoomId)) return 0;
        return State.ResolveTotal(native);
    }
    internal static bool CanEnter(string level, string variant)
    {
        if (State.Active && (string.IsNullOrEmpty(level) || string.IsNullOrEmpty(variant))) return false;
        return State.CanEnter(level, variant);
    }
    internal static void ReportBlocked(string level, string variant)
    {
        ObserveIdentity();
        OnPreviewObserved(level, variant);
        bool repeated = _blockedLevel == level && _blockedVariant == variant &&
            _blockedIdentity == State.Identity && Time.unscaledTime < _blockedExpires;
        _blockedLevel = level; _blockedVariant = variant;
        _blockedRoom = DeveloperHarness.CurrentRoomId; _blockedIdentity = State.Identity;
        _blockedExpires = Time.unscaledTime + 6;
        if (repeated) return; // Refresh a single banner; do not spam logs on held interaction.
        Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] Campaign entry blocked level='{level}' APStars={State.Total} required={State.Requirement(level, variant)?.ToString() ?? "unavailable"}.");
    }
    internal static void OnPreviewObserved(string level, string variant)
    {
        _previewLevel = level; _previewVariant = variant; _previewExpires = Time.unscaledTime + 2;
    }
    internal static void RenderOverlay()
    {
        if (!State.Active) return;
        if (!string.IsNullOrEmpty(_blockedLevel) && Time.unscaledTime < _blockedExpires &&
            _blockedIdentity == State.Identity && _blockedRoom == DeveloperHarness.CurrentRoomId &&
            !State.CanEnter(_blockedLevel, _blockedVariant))
        {
            string title = CampaignLevelCatalog.TryGet(_blockedLevel, out var entry) ? entry.DisplayName : "Level entry";
            int? gate = State.Requirement(_blockedLevel, _blockedVariant);
            string detail = State.Mode == ApStarMode.Incompatible ? "Archipelago settings are incompatible. Check the client version." :
                State.Mode == ApStarMode.Awaiting ? "Waiting for AP Stars to synchronize. Try again once connected." :
                gate.HasValue && State.Total < gate.Value ?
                    $"{gate.Value} AP Stars required. You have {State.Total}; collect {gate.Value - State.Total} more." :
                    "Archipelago could not verify this entry requirement. Reconnect and try again.";
            int fontSize = Math.Max(16, (int)(22 * Math.Clamp(Screen.height / 1080f, 0.6f, 1.6f)));
            if (_blockedStyle == null || _blockedFontSize != fontSize)
            {
                _blockedFontSize = fontSize;
                _blockedStyle = new GUIStyle(GUI.skin.box) { fontSize = fontSize, wordWrap = true,
                    richText = false, alignment = TextAnchor.MiddleCenter };
            }
            int depth = GUI.depth;
            try
            {
                GUI.depth = -2000;
                int width = Math.Min(Screen.width - 24, 760);
                GUI.Box(new Rect((Screen.width - width) / 2, 36, width, Math.Max(100, fontSize * 5)),
                    $"{title} - Entry locked\n{detail}", _blockedStyle);
            }
            finally { GUI.depth = depth; }
            return;
        }
        if (Time.unscaledTime > _previewExpires) return;
        int? required = State.Requirement(_previewLevel, _previewVariant);
        if (!required.HasValue) return;
        GUI.Box(new Rect(16, Screen.height - 112, 340, 42), $"AP Stars: {State.Total} / {required.Value} required");
    }
    // Event-only diagnostics: no polling, native writes, or changes to qualification.
    private static void TraceGoal(string stage, string detail) =>
        Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] GOAL TRACE stage='{stage}' total={State.Total} required={State.Settings.Goal} mode={State.Mode} started='{_startedLevel}' variant='{_startedVariant}' saveBound={!string.IsNullOrEmpty(_startedSave)} {detail}");
    private static bool TracingGoal => _startedLevel == "Level_28" ||
        DeveloperHarness.CurrentRoomId is "GameRoom_28A" or "GameRoom_28B";
    internal static void OnAdmittedStart(string level, string variant)
    {
        ObserveIdentity(); ResetAttempt();
        if (!State.Active) return;
        CassetteReceiptRandomization.WithStableQuestItemSave((_, save) => {
            _startedLevel = level; _startedVariant = variant; _startedSave = save;
        });
        if (level == "Level_28") TraceGoal("admitted-start", $"requestedVariant='{variant}'");
    }
    internal static void BeforePersistResult(object? request)
    {
        ObserveIdentity(); _candidate = null; _boundToAppliedResult = false;
        if (TracingGoal) TraceGoal("persist-request", $"requestType='{request?.GetType().FullName ?? "null"}'");
        if (!State.Active || string.IsNullOrEmpty(_startedSave))
        {
            if (TracingGoal) TraceGoal("persist-rejected", "reason='inactive-or-no-admitted-save'");
            return;
        }
        bool? didSucceed = ReflectionUtil.ReadBool(request!, "DidPlayersSucceed");
        bool? shouldSave = ReflectionUtil.ReadBool(request!, "ShouldSaveScore");
        bool successful = didSucceed == true && shouldSave == true;
        CassetteReceiptRandomization.WithStableQuestItemSave((_, save) => {
            if (save == _startedSave)
                _candidate = State.Capture(save, _startedLevel, _startedVariant, successful ? 1 : 0);
        });
        if (TracingGoal) TraceGoal("candidate", $"didSucceed={didSucceed?.ToString() ?? "unreadable"} shouldSave={shouldSave?.ToString() ?? "unreadable"} captured={_candidate != null} qualified={_candidate?.Qualified.ToString() ?? "none"}");
    }
    internal static void BeforeApplyResult(object? request)
    {
        ObserveIdentity();
        string level = Identifier(request, "LevelIdentifier"), variant = Identifier(request, "LevelVariantIdentifier");
        _boundToAppliedResult = _candidate != null && level == _startedLevel && variant == _startedVariant;
        if (!_boundToAppliedResult) _candidate = null;
        if (TracingGoal || level == "Level_28") TraceGoal("apply-result", $"resultLevel='{level}' resultVariant='{variant}' bound={_boundToAppliedResult}");
    }
    internal static void OnResultPersisted(object? persistedEvent)
    {
        ObserveIdentity();
        ApStarResult? candidate = _candidate;
        bool bound = _boundToAppliedResult;
        string level = _startedLevel, variant = _startedVariant;
        bool trace = TracingGoal || Identifier(persistedEvent, "Level") == "Level_28";
        if (trace) TraceGoal("persisted-event", $"eventLevel='{Identifier(persistedEvent, "Level")}' eventVariant='{Identifier(persistedEvent, "LevelVariant")}' candidate={candidate != null} qualified={candidate?.Qualified.ToString() ?? "none"} bound={bound}");
        ResetAttempt(); // Consume admission once; retain matched persisted evidence separately for retry.
        // The native event carries a nullable variant. The applied score has already
        // bound this candidate to the exact admitted level and variant; an omitted
        // event variant does not undo that evidence. Explicit conflicts still reject.
        string eventVariant = Identifier(persistedEvent, "LevelVariant");
        if (!bound || candidate == null || Identifier(persistedEvent, "Level") != level ||
            (!string.IsNullOrEmpty(eventVariant) && eventVariant != variant)) return;
        if (!candidate.Qualified) return;
        _persistedCandidate ??= candidate;
        TryCommitPersistedGoal();
    }
    private static void TryCommitPersistedGoal()
    {
        ApStarResult? candidate = _persistedCandidate;
        if (candidate == null) return;
        // Qualification was frozen at completion. A later receipt cannot upgrade it.
        // Keep this evidence when readiness is temporarily unavailable; a different
        // verified save is a rejection, not permission to transfer the completion.
        CassetteReceiptRandomization.WithStableQuestItemSave((_, save) => {
            _persistedCandidate = null;
            bool committed = State.Commit(candidate, save);
            TraceGoal("commit", $"sameSave={candidate.NativeSave == save} committed={committed}");
            if (!committed) return;
            _unsavedGoal = new(1, candidate.Identity, State.Settings.Goal, candidate.NativeSave, true);
            TrySaveGoal();
        });
    }
    private static string Identifier(object? value, string member) =>
        ReflectionUtil.ExtractIdentifier(ReflectionUtil.ReadMember(value, member)) ?? "";
    private static void TrySaveGoal()
    {
        if (_unsavedGoal == null) return;
        try
        {
            _journal.Save(_unsavedGoal);
            _durableGoal = true; _journalLoaded = true; _unsavedGoal = null; _lastError = "";
            TraceGoal("journal-saved", "completed=True");
        }
        catch (Exception ex) { Error("Goal journal write failed; goal delivery deferred: " + ex.GetBaseException().Message); }
    }
    internal static void TickUnity()
    {
        ObserveIdentity();
        ApStarSettings settings = State.Settings;
        ApStarEaterThresholds.Tick(DeveloperHarness.CurrentRoomId,
            settings.Mode == ApStarMode.Awaiting ? settings.Eaters : null);
        if (!State.Active || settings.Mode != ApStarMode.Awaiting || string.IsNullOrEmpty(_identity)) return;
        if (Time.unscaledTime < _nextRetry) return;
        _nextRetry = Time.unscaledTime + 1;
        TryCommitPersistedGoal();
        if (_unsavedGoal != null) TrySaveGoal();
        if (!_journalLoaded && _unsavedGoal == null)
        {
            try
            {
                var restored = _journal.Load(_identity, settings.Goal);
                _journalLoaded = true;
                if (restored != null) { _durableGoal = true; State.RestoreGoal(_identity); }
            }
            catch (Exception ex) { Error("Goal journal read failed; goal delivery deferred: " + ex.GetBaseException().Message); return; }
        }
        if (!_durableGoal || !State.GoalPending || State.Mode != ApStarMode.Ready) return;
        try { if (_sendGoal(_identity)) State.MarkGoalSent(_identity); }
        catch (Exception ex) { Error("Goal delivery deferred: " + ex.GetBaseException().Message); }
    }
    private static void Error(string message)
    {
        if (_lastError == message) return;
        _lastError = message; Plugin.LoggerInstance?.LogError("[SCRC-AP] " + message);
    }
}

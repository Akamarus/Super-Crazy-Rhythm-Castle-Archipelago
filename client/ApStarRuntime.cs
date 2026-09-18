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
    private static ApStarGoalRecord? _unsavedGoal;
    private static bool _boundToAppliedResult, _journalLoaded, _durableGoal;
    private static float _nextRetry, _previewExpires;
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
        _startedLevel = _startedVariant = _startedSave = "";
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
        OnPreviewObserved(level, variant);
        Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] Campaign entry blocked level='{level}' APStars={State.Total} required={State.Requirement(level, variant)?.ToString() ?? "unavailable"}.");
    }
    internal static void OnPreviewObserved(string level, string variant)
    {
        _previewLevel = level; _previewVariant = variant; _previewExpires = Time.unscaledTime + 2;
    }
    internal static void RenderOverlay()
    {
        if (!State.Active || Time.unscaledTime > _previewExpires) return;
        int? required = State.Requirement(_previewLevel, _previewVariant);
        if (!required.HasValue) return;
        GUI.Box(new Rect(16, Screen.height - 112, 340, 42), $"AP Stars: {State.Total} / {required.Value} required");
    }
    internal static void OnAdmittedStart(string level, string variant)
    {
        ObserveIdentity(); OnNativeBoundary();
        if (!State.Active) return;
        CassetteReceiptRandomization.WithStableQuestItemSave((_, save) => {
            _startedLevel = level; _startedVariant = variant; _startedSave = save;
        });
    }
    internal static void BeforePersistResult(object? request)
    {
        ObserveIdentity(); _candidate = null; _boundToAppliedResult = false;
        if (!State.Active || string.IsNullOrEmpty(_startedSave)) return;
        bool successful = ReflectionUtil.ReadBool(request!, "DidPlayersSucceed") == true &&
            ReflectionUtil.ReadBool(request!, "ShouldSaveScore") == true;
        CassetteReceiptRandomization.WithStableQuestItemSave((_, save) => {
            if (save == _startedSave)
                _candidate = State.Capture(save, _startedLevel, _startedVariant, successful ? 1 : 0);
        });
    }
    internal static void BeforeApplyResult(object? request)
    {
        ObserveIdentity();
        string level = Identifier(request, "LevelIdentifier"), variant = Identifier(request, "LevelVariantIdentifier");
        _boundToAppliedResult = _candidate != null && level == _startedLevel && variant == _startedVariant;
        if (!_boundToAppliedResult) _candidate = null;
    }
    internal static void OnResultPersisted(object? persistedEvent)
    {
        ObserveIdentity();
        ApStarResult? candidate = _candidate;
        bool bound = _boundToAppliedResult;
        string level = _startedLevel, variant = _startedVariant;
        OnNativeBoundary(); // Consume the whole admitted attempt before callbacks or I/O can repeat it.
        if (!bound || candidate == null || Identifier(persistedEvent, "Level") != level ||
            Identifier(persistedEvent, "LevelVariant") != variant) return;
        CassetteReceiptRandomization.WithStableQuestItemSave((_, save) => {
            if (!State.Commit(candidate, save)) return;
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

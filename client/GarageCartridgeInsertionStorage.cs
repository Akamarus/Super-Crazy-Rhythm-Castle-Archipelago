using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json.Linq;

namespace RhythmCastleAP;

internal enum GarageInsertionReadStatus
{
    KnownFalse,
    KnownTrue,
    Malformed,
    Failed,
}

internal readonly record struct GarageInsertionReadResult(
    GarageInsertionReadStatus Status,
    string Detail)
{
    internal static readonly GarageInsertionReadResult KnownFalse =
        new(GarageInsertionReadStatus.KnownFalse, string.Empty);
    internal static readonly GarageInsertionReadResult KnownTrue =
        new(GarageInsertionReadStatus.KnownTrue, string.Empty);

    internal static GarageInsertionReadResult MalformedValue(string key, JTokenType valueType) =>
        new(
            GarageInsertionReadStatus.Malformed,
            $"key='{SafeField(key)}' valueType='{valueType}'");

    internal static GarageInsertionReadResult ReadFailure(string key, string failureType) =>
        new(
            GarageInsertionReadStatus.Failed,
            $"key='{SafeField(key)}' failureType='{SafeField(failureType)}'");

    internal bool IsKnown =>
        Status is GarageInsertionReadStatus.KnownFalse or GarageInsertionReadStatus.KnownTrue;

    private static string SafeField(string value) =>
        (value ?? string.Empty)
            .Replace("'", "_", StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
}

internal enum GarageInsertionWriteResult
{
    Succeeded,
    Failed,
}

internal interface IGarageInsertionDataStore
{
    Task<GarageInsertionReadResult> ReadAsync(string key, CancellationToken cancellationToken);
    Task<GarageInsertionWriteResult> WriteTrueAsync(string key, CancellationToken cancellationToken);
}

internal sealed record GarageInsertionEntryState(
    GarageInsertionServerValue ServerValue,
    bool InitialReadComplete,
    bool WritePending,
    bool WriteInFlight,
    bool DurableLogged);

internal sealed record GaragePendingInsertions(int Schema, string Identity, string[] Songs);

internal sealed class GarageCartridgeInsertionCoordinator
{
    private readonly object _gate = new();
    private readonly string? _journalDirectory;
    private bool _journalDirty;
    private readonly IReadOnlyDictionary<string, string> _keys;
    private readonly Dictionary<string, GarageInsertionEntryState> _entries;
    private readonly HashSet<string> _initialReadAttempts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _initialReadFailures = new(StringComparer.Ordinal);
    private long _generation = long.MinValue;
    private string? _roomIdentity;
    private IGarageInsertionDataStore? _store;
    private CancellationTokenSource? _connectionCancellation;
    private bool _initialSyncDiagnosticEmitted;

    internal GarageCartridgeInsertionCoordinator(IEnumerable<KeyValuePair<string, string>> keys, string? journalDirectory = null)
    {
        _journalDirectory = journalDirectory;
        var copiedKeys = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in keys)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                throw new ArgumentException("Garage insertion songs and slot keys must be non-empty.", nameof(keys));
            if (!copiedKeys.TryAdd(pair.Key, pair.Value))
                throw new ArgumentException($"Garage insertion song '{pair.Key}' appears more than once.", nameof(keys));
        }

        _keys = new ReadOnlyDictionary<string, string>(copiedKeys);
        _entries = copiedKeys.Keys.ToDictionary(
            song => song,
            _ => new GarageInsertionEntryState(GarageInsertionServerValue.Unknown, false, false, false, false),
            StringComparer.Ordinal);
    }

    internal event Action<string>? DurableInsertionConfirmed;
    internal event Action<GarageCartridgeDiagnostic>? DiagnosticEmitted;

    internal bool InitialSyncReady
    {
        get
        {
            lock (_gate)
            {
                return _store is not null && _entries.Values.All(entry =>
                    entry.InitialReadComplete && entry.ServerValue != GarageInsertionServerValue.Unknown);
            }
        }
    }

    internal void BeginConnection(long generation, IGarageInsertionDataStore store, string roomIdentity)
    {
        ArgumentNullException.ThrowIfNull(store);

        CancellationTokenSource? previousCancellation;
        CancellationToken cancellationToken;
        lock (_gate)
        {
            if (generation <= _generation)
                throw new ArgumentOutOfRangeException(nameof(generation), "Connection generations must increase.");

            var restored = ReadPending(roomIdentity);
            previousCancellation = _connectionCancellation;
            // Pending writes survive an offline interval or reload only for the same AP room and slot.
            bool replaceRoom = _roomIdentity != roomIdentity;
            _roomIdentity = roomIdentity;
            _generation = generation;
            _store = store;
            _connectionCancellation = new CancellationTokenSource();
            cancellationToken = _connectionCancellation.Token;
            _initialReadAttempts.Clear();
            _initialReadFailures.Clear();
            _initialSyncDiagnosticEmitted = false;

            foreach (var song in _keys.Keys)
            {
                var entry = _entries[song];
                _entries[song] = replaceRoom
                    ? new GarageInsertionEntryState(GarageInsertionServerValue.Unknown, false, false, false, false)
                    : entry with
                {
                    ServerValue = entry.ServerValue == GarageInsertionServerValue.Inserted
                        ? GarageInsertionServerValue.Inserted
                        : GarageInsertionServerValue.Unknown,
                    InitialReadComplete = false,
                    WriteInFlight = false,
                };
                if (restored.Contains(song)) _entries[song] = _entries[song] with
                {
                    ServerValue = GarageInsertionServerValue.Inserted,
                    WritePending = true,
                };
            }
            if (replaceRoom) _journalDirty = false;
        }

        CancelAndDispose(previousCancellation);
        DiagnosticEmitted?.Invoke(GarageCartridgeDiagnostics.SyncPending(generation));
        foreach (var pair in _keys)
            _ = ReadInitialValueAsync(generation, store, pair.Key, pair.Value, cancellationToken);
        RetryPendingWrites();
    }

    internal void EndConnection(long generation)
    {
        CancellationTokenSource? cancellation;
        GarageCartridgeDiagnostic? diagnostic = null;
        lock (_gate)
        {
            if (generation != _generation || _store is null)
                return;

            cancellation = _connectionCancellation;
            _connectionCancellation = null;
            _store = null;
            if (!_initialSyncDiagnosticEmitted)
            {
                _initialSyncDiagnosticEmitted = true;
                diagnostic = GarageCartridgeDiagnostics.SyncFailed(
                    generation,
                    "reason='connection ended before initial reads completed'.");
            }
            foreach (var song in _keys.Keys)
            {
                var entry = _entries[song];
                _entries[song] = entry with { WriteInFlight = false, InitialReadComplete = false };
            }
        }

        CancelAndDispose(cancellation);
        if (diagnostic is { } emitted)
            DiagnosticEmitted?.Invoke(emitted);
    }

    internal void NoteInserted(string song)
    {
        lock (_gate)
        {
            EnsureKnownSong(song);
            var entry = _entries[song];
            if (entry.ServerValue == GarageInsertionServerValue.Inserted && !entry.WritePending)
                return;

            _entries[song] = entry with
            {
                ServerValue = GarageInsertionServerValue.Inserted,
                WritePending = true,
            };
            _journalDirty = true;
            SavePending();
        }

        StartWriteIfRequired(song);
    }

    internal void RetryFailedOperations()
    {
        long generation;
        IGarageInsertionDataStore store;
        CancellationToken token;
        string[] songs;
        lock (_gate)
        {
            if (_store == null || _connectionCancellation == null) return;
            SavePending();
            generation = _generation;
            store = _store;
            token = _connectionCancellation.Token;
            songs = _initialReadFailures.Keys.ToArray();
            // A retry owns these entries until its result arrives.
            foreach (var song in songs) {
                _initialReadFailures.Remove(song);
                _initialReadAttempts.Remove(song);
            }
        }
        foreach (var song in songs)
            _ = ReadInitialValueAsync(generation, store, song, _keys[song], token);
        RetryPendingWrites();
    }

    internal void RetryPendingWrites()
    {
        List<string> pendingSongs;
        lock (_gate)
        {
            if (_store is null)
                return;
            pendingSongs = _entries
                .Where(pair => pair.Value.WritePending && !pair.Value.WriteInFlight)
                .Select(pair => pair.Key)
                .ToList();
        }

        foreach (var song in pendingSongs)
            StartWriteIfRequired(song);
    }

    internal GarageInsertionServerValue GetServerValue(string song)
    {
        lock (_gate)
        {
            EnsureKnownSong(song);
            return _entries[song].ServerValue;
        }
    }

    private async Task ReadInitialValueAsync(
        long generation,
        IGarageInsertionDataStore store,
        string song,
        string key,
        CancellationToken cancellationToken)
    {
        GarageInsertionReadResult result;
        try
        {
            result = await store.ReadAsync(key, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            result = GarageInsertionReadResult.ReadFailure(key, "data-store exception");
        }

        GarageCartridgeDiagnostic? diagnostic = null;
        lock (_gate)
        {
            if (!IsCurrentConnection(generation, store))
                return;

            var entry = _entries[song];
            _entries[song] = result.Status switch
            {
                GarageInsertionReadStatus.KnownTrue => entry with
                {
                    ServerValue = GarageInsertionServerValue.Inserted,
                    InitialReadComplete = true,
                    WritePending = false,
                    WriteInFlight = false,
                },
                GarageInsertionReadStatus.KnownFalse => entry with
                {
                    ServerValue = entry.ServerValue == GarageInsertionServerValue.Inserted
                        ? GarageInsertionServerValue.Inserted
                        : GarageInsertionServerValue.NotInserted,
                    InitialReadComplete = true,
                },
                _ => entry with
                {
                    ServerValue = entry.ServerValue == GarageInsertionServerValue.Inserted
                        ? GarageInsertionServerValue.Inserted
                        : GarageInsertionServerValue.Unknown,
                    InitialReadComplete = false,
                },
            };

            if (result.Status == GarageInsertionReadStatus.KnownTrue && entry.WritePending)
            {
                _journalDirty = true;
                SavePending();
            }
            _initialReadAttempts.Add(song);
            if (result.IsKnown)
            {
                _initialReadFailures.Remove(song);
            }
            else
            {
                string detail = string.IsNullOrWhiteSpace(result.Detail)
                    ? $"key='{key}' result='{result.Status}'"
                    : result.Detail;
                if (!detail.Contains("key='", StringComparison.Ordinal))
                    detail = $"key='{key}' {detail}";
                _initialReadFailures[song] = detail;
            }

            if (_initialSyncDiagnosticEmitted && result.IsKnown && !entry.InitialReadComplete &&
                _entries.Values.All(value => value.InitialReadComplete))
                diagnostic = GarageCartridgeDiagnostics.SyncReady(generation);
            if (!_initialSyncDiagnosticEmitted && _initialReadAttempts.Count == _keys.Count)
            {
                _initialSyncDiagnosticEmitted = true;
                if (_initialReadFailures.Count == 0)
                {
                    diagnostic = GarageCartridgeDiagnostics.SyncReady(generation);
                }
                else
                {
                    string failureDetail = string.Join(
                        "; ",
                        _keys.Keys
                            .Where(_initialReadFailures.ContainsKey)
                            .Select(failedSong => _initialReadFailures[failedSong]));
                    diagnostic = GarageCartridgeDiagnostics.SyncFailed(
                        generation,
                        $"reads=[{failureDetail}].");
                }
            }
        }

        if (diagnostic is { } emitted)
            DiagnosticEmitted?.Invoke(emitted);
    }

    private void StartWriteIfRequired(string song)
    {
        long generation;
        IGarageInsertionDataStore store;
        CancellationToken cancellationToken;
        string key;
        lock (_gate)
        {
            var entry = _entries[song];
            if (_store is null || _connectionCancellation is null || !entry.WritePending || entry.WriteInFlight)
                return;

            generation = _generation;
            store = _store;
            cancellationToken = _connectionCancellation.Token;
            key = _keys[song];
            _entries[song] = entry with { WriteInFlight = true };
        }

        _ = WriteInsertedValueAsync(generation, store, song, key, cancellationToken);
    }

    private async Task WriteInsertedValueAsync(
        long generation,
        IGarageInsertionDataStore store,
        string song,
        string key,
        CancellationToken cancellationToken)
    {
        GarageInsertionWriteResult result;
        try
        {
            result = await store.WriteTrueAsync(key, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            result = GarageInsertionWriteResult.Failed;
        }

        var raiseDurableTransition = false;
        lock (_gate)
        {
            if (!IsCurrentConnection(generation, store))
                return;

            var entry = _entries[song];
            if (result == GarageInsertionWriteResult.Succeeded)
            {
                raiseDurableTransition = !entry.DurableLogged;
                _entries[song] = entry with
                {
                    ServerValue = GarageInsertionServerValue.Inserted,
                    WritePending = false,
                    WriteInFlight = false,
                    DurableLogged = true,
                };
                _journalDirty |= entry.WritePending;
                SavePending();
            }
            else
            {
                _entries[song] = entry with { WriteInFlight = false };
            }
        }

        if (raiseDurableTransition)
            DurableInsertionConfirmed?.Invoke(song);
    }

    private string JournalPath(string identity) => Path.Combine(_journalDirectory!,
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))) + ".json");

    private HashSet<string> ReadPending(string identity)
    {
        if (_journalDirectory == null) return new();
        string path = JournalPath(identity);
        try
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            using var doc = JsonDocument.Parse(file);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 3 ||
                !root.TryGetProperty("Schema", out _) || !root.TryGetProperty("Identity", out _) || !root.TryGetProperty("Songs", out _))
                throw new InvalidDataException("Unexpected Garage journal fields.");
            var record = root.Deserialize<GaragePendingInsertions>();
            if (record?.Schema != 1 || record.Identity != identity || record.Songs == null ||
                record.Songs.Any(song => song == null || !_keys.ContainsKey(song)) ||
                record.Songs.Distinct(StringComparer.Ordinal).Count() != record.Songs.Length)
                throw new InvalidDataException("Invalid Garage insertion data.");
            return record.Songs.ToHashSet(StringComparer.Ordinal);
        }
        catch (FileNotFoundException) { return new(); }
        catch (DirectoryNotFoundException) when (!File.Exists(_journalDirectory)) { return new(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        {
            throw new InvalidDataException($"Cannot load garage-insertions/{Path.GetFileName(path)}. Check access or restore its matching .bak.", ex);
        }
    }

    // Called under the coordinator gate. A disk failure leaves memory and the
    // server write intact; the periodic retry also republishes the local journal.
    private void SavePending()
    {
        if (!_journalDirty || _journalDirectory == null || _roomIdentity == null) return;
        try
        {
            string[] songs = _entries.Where(pair => pair.Value.WritePending).Select(pair => pair.Key).ToArray();
            JournalPublication.Write(JournalPath(_roomIdentity), new GaragePendingInsertions(1, _roomIdentity, songs));
            _journalDirty = false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DiagnosticEmitted?.Invoke(new GarageCartridgeDiagnostic(GarageCartridgeDiagnosticLevel.Warning,
                "[SCRC-AP] Garage insertion journal could not be saved. Retrying local and server storage."));
        }
    }

    private bool IsCurrentConnection(long generation, IGarageInsertionDataStore store) =>
        generation == _generation && ReferenceEquals(store, _store);

    private void EnsureKnownSong(string song)
    {
        if (!_keys.ContainsKey(song))
            throw new ArgumentException($"Unknown garage cartridge song '{song}'.", nameof(song));
    }

    private static void CancelAndDispose(CancellationTokenSource? cancellation)
    {
        if (cancellation is null)
            return;
        cancellation.Cancel();
        cancellation.Dispose();
    }
}

internal readonly record struct GarageInsertionWriteConfirmation(
    bool CallbackConfirmed,
    JToken RereadValue);

internal interface IArchipelagoGarageInsertionStorageTransport
{
    Task<JToken> ReadAsync(string key, JToken initialValue, CancellationToken cancellationToken);
    Task<GarageInsertionWriteConfirmation> WriteTrueAndReadBackAsync(
        string key,
        CancellationToken cancellationToken);
}

internal sealed class ArchipelagoGarageInsertionDataStore : IGarageInsertionDataStore
{
    private readonly IArchipelagoGarageInsertionStorageTransport _transport;

    internal ArchipelagoGarageInsertionDataStore(ArchipelagoSession session)
        : this(new ArchipelagoGarageInsertionStorageTransport(session))
    {
    }

    internal ArchipelagoGarageInsertionDataStore(IArchipelagoGarageInsertionStorageTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public async Task<GarageInsertionReadResult> ReadAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            JToken value = await _transport
                .ReadAsync(key, JToken.FromObject(false), cancellationToken)
                .ConfigureAwait(false);
            return value.Type switch
            {
                JTokenType.Boolean when value.Value<bool>() => GarageInsertionReadResult.KnownTrue,
                JTokenType.Boolean => GarageInsertionReadResult.KnownFalse,
                _ => GarageInsertionReadResult.MalformedValue(key, value.Type),
            };
        }
        catch (OperationCanceledException)
        {
            return GarageInsertionReadResult.ReadFailure(key, "canceled");
        }
        catch (Exception exception)
        {
            return GarageInsertionReadResult.ReadFailure(key, exception.GetType().Name);
        }
    }

    public async Task<GarageInsertionWriteResult> WriteTrueAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            GarageInsertionWriteConfirmation confirmation = await _transport
                .WriteTrueAndReadBackAsync(key, cancellationToken)
                .ConfigureAwait(false);
            return confirmation.CallbackConfirmed &&
                   confirmation.RereadValue.Type == JTokenType.Boolean &&
                   confirmation.RereadValue.Value<bool>()
                ? GarageInsertionWriteResult.Succeeded
                : GarageInsertionWriteResult.Failed;
        }
        catch (OperationCanceledException)
        {
            return GarageInsertionWriteResult.Failed;
        }
        catch (Exception)
        {
            return GarageInsertionWriteResult.Failed;
        }
    }
}

internal sealed class ArchipelagoGarageInsertionStorageTransport : IArchipelagoGarageInsertionStorageTransport
{
    private readonly ArchipelagoSession _session;

    internal ArchipelagoGarageInsertionStorageTransport(ArchipelagoSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public async Task<JToken> ReadAsync(
        string key,
        JToken initialValue,
        CancellationToken cancellationToken)
    {
        DataStorageElement element = _session.DataStorage[Scope.Slot, key];
        element.Initialize(initialValue);
        return await element.GetAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<GarageInsertionWriteConfirmation> WriteTrueAndReadBackAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var callbackCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        DataStorageElement write = true;
        _session.DataStorage[Scope.Slot, key] = write + Callback.Add(
            (original, current, context) =>
                callbackCompletion.TrySetResult(
                    current.Type == JTokenType.Boolean && current.Value<bool>()));

        bool callbackConfirmed = await callbackCompletion.Task
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        JToken rereadValue = await _session.DataStorage[Scope.Slot, key]
            .GetAsync()
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        return new GarageInsertionWriteConfirmation(callbackConfirmed, rereadValue);
    }
}

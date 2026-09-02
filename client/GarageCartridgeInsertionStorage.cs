using System.Collections.ObjectModel;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json.Linq;

namespace RhythmCastleAP;

internal enum GarageInsertionReadResult
{
    KnownFalse,
    KnownTrue,
    Malformed,
    Failed,
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

internal sealed class GarageCartridgeInsertionCoordinator
{
    private readonly object _gate = new();
    private readonly IReadOnlyDictionary<string, string> _keys;
    private readonly Dictionary<string, GarageInsertionEntryState> _entries;
    private long _generation = long.MinValue;
    private IGarageInsertionDataStore? _store;
    private CancellationTokenSource? _connectionCancellation;

    internal GarageCartridgeInsertionCoordinator(IEnumerable<KeyValuePair<string, string>> keys)
    {
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

    internal void BeginConnection(long generation, IGarageInsertionDataStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        CancellationTokenSource? previousCancellation;
        CancellationToken cancellationToken;
        lock (_gate)
        {
            if (generation <= _generation)
                throw new ArgumentOutOfRangeException(nameof(generation), "Connection generations must increase.");

            previousCancellation = _connectionCancellation;
            _generation = generation;
            _store = store;
            _connectionCancellation = new CancellationTokenSource();
            cancellationToken = _connectionCancellation.Token;

            foreach (var song in _keys.Keys)
            {
                var entry = _entries[song];
                _entries[song] = entry with
                {
                    ServerValue = entry.ServerValue == GarageInsertionServerValue.Inserted
                        ? GarageInsertionServerValue.Inserted
                        : GarageInsertionServerValue.Unknown,
                    InitialReadComplete = false,
                    WriteInFlight = false,
                };
            }
        }

        CancelAndDispose(previousCancellation);
        foreach (var pair in _keys)
            _ = ReadInitialValueAsync(generation, store, pair.Key, pair.Value, cancellationToken);
        RetryPendingWrites();
    }

    internal void EndConnection(long generation)
    {
        CancellationTokenSource? cancellation;
        lock (_gate)
        {
            if (generation != _generation || _store is null)
                return;

            cancellation = _connectionCancellation;
            _connectionCancellation = null;
            _store = null;
            foreach (var song in _keys.Keys)
            {
                var entry = _entries[song];
                _entries[song] = entry with { WriteInFlight = false, InitialReadComplete = false };
            }
        }

        CancelAndDispose(cancellation);
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
        }

        StartWriteIfRequired(song);
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
            result = GarageInsertionReadResult.Failed;
        }

        lock (_gate)
        {
            if (!IsCurrentConnection(generation, store))
                return;

            var entry = _entries[song];
            _entries[song] = result switch
            {
                GarageInsertionReadResult.KnownTrue => entry with
                {
                    ServerValue = GarageInsertionServerValue.Inserted,
                    InitialReadComplete = true,
                    WritePending = false,
                    WriteInFlight = false,
                },
                GarageInsertionReadResult.KnownFalse => entry with
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
        }
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
            }
            else
            {
                _entries[song] = entry with { WriteInFlight = false };
            }
        }

        if (raiseDurableTransition)
            DurableInsertionConfirmed?.Invoke(song);
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

internal sealed class ArchipelagoGarageInsertionDataStore : IGarageInsertionDataStore
{
    private readonly ArchipelagoSession _session;

    internal ArchipelagoGarageInsertionDataStore(ArchipelagoSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public async Task<GarageInsertionReadResult> ReadAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            DataStorageElement element = _session.DataStorage[Scope.Slot, key];
            element.Initialize(JToken.FromObject(false));
            JToken value = await element.GetAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            return value.Type switch
            {
                JTokenType.Boolean when value.Value<bool>() => GarageInsertionReadResult.KnownTrue,
                JTokenType.Boolean => GarageInsertionReadResult.KnownFalse,
                _ => GarageInsertionReadResult.Malformed,
            };
        }
        catch (OperationCanceledException)
        {
            return GarageInsertionReadResult.Failed;
        }
        catch (Exception)
        {
            return GarageInsertionReadResult.Failed;
        }
    }

    public async Task<GarageInsertionWriteResult> WriteTrueAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var callbackCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            DataStorageElement write = true;
            _session.DataStorage[Scope.Slot, key] = write + Callback.Add(
                (original, current, context) =>
                    callbackCompletion.TrySetResult(current.Type == JTokenType.Boolean && current.Value<bool>()));

            var callbackTrue = await callbackCompletion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            JToken confirmed = await _session.DataStorage[Scope.Slot, key].GetAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            return callbackTrue && confirmed.Type == JTokenType.Boolean && confirmed.Value<bool>()
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

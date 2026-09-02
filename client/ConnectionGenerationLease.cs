namespace RhythmCastleAP;

internal sealed class LifecycleLease : IDisposable
{
    private Action? _release;

    internal LifecycleLease(Action release)
    {
        _release = release ?? throw new ArgumentNullException(nameof(release));
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _release, null)?.Invoke();
    }
}

internal sealed class GenerationLeaseGate
{
    private readonly ReaderWriterLockSlim _gate = new(LockRecursionPolicy.NoRecursion);
    private long _activeGeneration = long.MinValue;
    private long _endedThrough = long.MinValue;

    internal bool TryBegin(long generation, Action begin)
    {
        ArgumentNullException.ThrowIfNull(begin);
        _gate.EnterWriteLock();
        try
        {
            if (generation <= _endedThrough || _activeGeneration != long.MinValue)
                return false;
            begin();
            _activeGeneration = generation;
            return true;
        }
        finally
        {
            _gate.ExitWriteLock();
        }
    }

    internal bool End(long generation, Action end)
    {
        ArgumentNullException.ThrowIfNull(end);
        _gate.EnterWriteLock();
        try
        {
            if (generation > _endedThrough)
                _endedThrough = generation;
            if (_activeGeneration != generation)
                return false;
            _activeGeneration = long.MinValue;
            end();
            return true;
        }
        finally
        {
            _gate.ExitWriteLock();
        }
    }

    internal bool EndActive(Action<long> end)
    {
        ArgumentNullException.ThrowIfNull(end);
        _gate.EnterWriteLock();
        try
        {
            if (_activeGeneration == long.MinValue)
                return false;
            long generation = _activeGeneration;
            _activeGeneration = long.MinValue;
            if (generation > _endedThrough)
                _endedThrough = generation;
            end(generation);
            return true;
        }
        finally
        {
            _gate.ExitWriteLock();
        }
    }

    internal bool TryAcquire(long generation, out LifecycleLease? lease)
    {
        _gate.EnterReadLock();
        if (_activeGeneration != generation)
        {
            _gate.ExitReadLock();
            lease = null;
            return false;
        }
        lease = new LifecycleLease(_gate.ExitReadLock);
        return true;
    }

    internal bool TryAcquireCurrent(out long generation, out LifecycleLease? lease)
    {
        _gate.EnterReadLock();
        if (_activeGeneration == long.MinValue)
        {
            _gate.ExitReadLock();
            generation = long.MinValue;
            lease = null;
            return false;
        }
        generation = _activeGeneration;
        lease = new LifecycleLease(_gate.ExitReadLock);
        return true;
    }
}

internal readonly record struct GenerationSession<TSession>(TSession? Session, long Generation)
    where TSession : class;

internal sealed class SessionGenerationLeaseGate<TSession>
    where TSession : class
{
    private readonly ReaderWriterLockSlim _gate = new(LockRecursionPolicy.NoRecursion);
    private TSession? _session;
    private long _generation = long.MinValue;
    private long _admittedGeneration = long.MinValue;
    private long _highestGeneration = long.MinValue;
    private bool _shutdown;

    internal bool TryAdmit(long generation)
    {
        _gate.EnterWriteLock();
        try
        {
            if (_shutdown || generation <= _highestGeneration)
                return false;
            _admittedGeneration = generation;
            _highestGeneration = generation;
            return true;
        }
        finally
        {
            _gate.ExitWriteLock();
        }
    }

    internal bool TryPublish(
        TSession session,
        long generation,
        Action<GenerationSession<TSession>> publish,
        out GenerationSession<TSession> previous)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(publish);
        _gate.EnterWriteLock();
        try
        {
            if (_shutdown || _admittedGeneration != generation)
            {
                previous = default;
                return false;
            }
            previous = new GenerationSession<TSession>(_session, _generation);
            _session = session;
            _generation = generation;
            _admittedGeneration = long.MinValue;
            publish(previous);
            return true;
        }
        finally
        {
            _gate.ExitWriteLock();
        }
    }

    internal void Abandon(long generation)
    {
        _gate.EnterWriteLock();
        try
        {
            if (_admittedGeneration == generation)
                _admittedGeneration = long.MinValue;
        }
        finally
        {
            _gate.ExitWriteLock();
        }
    }

    internal bool TryAcquire(
        TSession session,
        long generation,
        out LifecycleLease? lease)
    {
        _gate.EnterReadLock();
        if (!ReferenceEquals(_session, session) || _generation != generation)
        {
            _gate.ExitReadLock();
            lease = null;
            return false;
        }
        lease = new LifecycleLease(_gate.ExitReadLock);
        return true;
    }

    internal bool TryAcquireCurrent(
        out TSession? session,
        out long generation,
        out LifecycleLease? lease)
    {
        _gate.EnterReadLock();
        if (_session is null)
        {
            _gate.ExitReadLock();
            session = null;
            generation = long.MinValue;
            lease = null;
            return false;
        }
        session = _session;
        generation = _generation;
        lease = new LifecycleLease(_gate.ExitReadLock);
        return true;
    }

    internal bool TryEnd(
        TSession session,
        long generation,
        Action? end = null)
    {
        _gate.EnterWriteLock();
        try
        {
            if (!ReferenceEquals(_session, session) || _generation != generation)
                return false;
            _session = null;
            _generation = long.MinValue;
            end?.Invoke();
            return true;
        }
        finally
        {
            _gate.ExitWriteLock();
        }
    }

    internal GenerationSession<TSession> Shutdown(Action<GenerationSession<TSession>> shutdown)
    {
        ArgumentNullException.ThrowIfNull(shutdown);
        _gate.EnterWriteLock();
        try
        {
            if (_shutdown)
                return default;
            _shutdown = true;
            _admittedGeneration = long.MinValue;
            var current = new GenerationSession<TSession>(_session, _generation);
            _session = null;
            _generation = long.MinValue;
            shutdown(current);
            return current;
        }
        finally
        {
            _gate.ExitWriteLock();
        }
    }
}

using System.Globalization;
using System.Text;
using System.Reflection;
using BepInEx;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace RhythmCastleAP;

// Opt-in, bounded observation. No motor hooks, physics writes, or recurring discovery.
internal sealed class MovementCapture : MonoBehaviour
{
    private const int MaxBodies = 12;
    private const int MaxSamples = 120;
    private readonly List<(Rigidbody Body, string Path)> _bodies = new();
    private readonly StringBuilder _output = new();
    private readonly List<(Rigidbody Body, object Motor, PropertyInfo[] Properties)> _motors = new();
    private readonly List<MovementContactProbe> _contacts = new();
    private static bool _contactTypeRegistered;
    private MethodInfo? _roomRunning;
    private string _room = "";
    private MethodInfo? _movementInput, _rawMovementInput;
    private readonly List<object> _playerIds = new();
    private string _inputStatus = "unavailable";
    private float _maxFrameDelta;
    private int _samples, _physicsTicks;
    private float _nextSample, _noticeUntil;
    private bool _active;
    private string _notice = "";
    public MovementCapture(IntPtr pointer) : base(pointer) { }

    private void FixedUpdate() { if (_active) _physicsTicks++; }

    private void Update()
    {
        try
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                if (_active) Finish("manual stop");
                else Begin();
            }
            if (!_active) return;
            _maxFrameDelta = Math.Max(_maxFrameDelta, Time.unscaledDeltaTime);
            if (!string.Equals(_room, DeveloperHarness.CurrentRoomId, StringComparison.Ordinal))
            { Finish("room changed"); return; }
            if (Time.unscaledTime < _nextSample) return;
            _nextSample = Time.unscaledTime + 0.1f;
            Sample();
            if (++_samples >= MaxSamples) Finish("sample limit reached");
        }
        catch (Exception ex)
        {
            _active = false;
            ReleaseProbes();
            _bodies.Clear();
            _output.Clear();
            Notice("Movement capture stopped: " + ex.GetBaseException().Message);
            Plugin.LoggerInstance?.LogWarning("[SCRC-AP] " + _notice);
        }
    }

    private void Begin()
    {
        _room = DeveloperHarness.CurrentRoomId;
        if (string.IsNullOrEmpty(_room) || !_room.StartsWith("GameRoom_", StringComparison.Ordinal))
        { Notice("Movement capture: enter a gameplay room first."); return; }
        _bodies.Clear();
        _output.Clear();
        var camera = Camera.main;
        Vector3 origin = camera == null ? Vector3.zero : camera.transform.position;
        // One explicit scan per key press. Player bodies take priority over nearby NPCs.
        var bodies = UnityEngine.Object.FindObjectsOfType<Rigidbody>()
            .Where(body => body != null && body.gameObject.activeInHierarchy)
            .Select(body => (Body: body, Path: BodyPath(body)))
            .OrderByDescending(entry => entry.Path.Contains("/PlayerCharacter", StringComparison.OrdinalIgnoreCase))
            .ThenBy(entry => (entry.Body.position - origin).sqrMagnitude).ToArray();
        foreach (var entry in bodies.Take(MaxBodies))
            _bodies.Add(entry);
        BindPlayerInputs();
        BindMotorProbes();
        _output.AppendLine("inputBinding=" + _inputStatus);
        _output.AppendLine($"version={Plugin.PluginVersion} room={_room} activeBodies={bodies.Length} capturedBodies={_bodies.Count} cap={MaxBodies}");
        _output.AppendLine("sample|realtime|frame|fixedTicks|timeScale|fixedDelta|delta|maxFrameDelta|focused|body|position|visualPosition|velocity|forward|kinematic|gravity|sleeping|constraints|detectCollisions");
        _samples = 0;
        _maxFrameDelta = 0;
        _physicsTicks = 0;
        _nextSample = Time.unscaledTime;
        _active = true;
        Notice("Movement capture: keep moving normally for 12 seconds. F8 stops early.");
    }

    private static string V(Vector3 value) => string.Format(CultureInfo.InvariantCulture,
        "{0:F4},{1:F4},{2:F4}", value.x, value.y, value.z);

    private void Sample()
    {
        string prefix = FormattableString.Invariant($"{_samples}|{Time.realtimeSinceStartup:F3}|{Time.frameCount}|{_physicsTicks}|{Time.timeScale:F4}|{Time.fixedDeltaTime:F5}|{Time.unscaledDeltaTime:F5}|{_maxFrameDelta:F5}|{Application.isFocused}|");
        CaptureInputs(prefix);
        CaptureMotors(prefix);
        _maxFrameDelta = 0;
        foreach (var entry in _bodies)
        {
            var body = entry.Body;
            if (body == null) continue;
            _output.Append(prefix).Append(entry.Path).Append('|')
                .Append(V(body.position)).Append('|').Append(V(body.transform.position)).Append('|')
                .Append(V(body.velocity)).Append('|').Append(V(body.transform.forward)).Append('|')
                .Append(body.isKinematic).Append('|').Append(body.useGravity).Append('|')
                .Append(body.IsSleeping()).Append('|').Append(body.constraints).Append('|').Append(body.detectCollisions).AppendLine();
        }
    }

    private static string BodyPath(Rigidbody body)
    {
        var names = new List<string>();
        Transform? current = body.transform;
        while (current != null && names.Count < 24)
        { names.Add(current.name); current = current.parent; }
        names.Reverse();
        return string.Join("/", names);
    }

    private void BindPlayerInputs()
    {
        _playerIds.Clear();
        _movementInput = null;
        _rawMovementInput = null;
        try
        {
            var game = ReflectionUtil.GameAssembly;
            var players = game?.GetType("PlayerEnquiries");
            var inputs = game?.GetType("PlayerInputEnquiries");
            var allLocal = players?.GetMethod("GetAllLocalPlayers", Type.EmptyTypes);
            object? localIds = allLocal?.Invoke(null, null);
            object? enumerator = localIds?.GetType().GetMethod("GetEnumerator", Type.EmptyTypes)?.Invoke(localIds, null);
            var moveNext = enumerator?.GetType().GetMethod("MoveNext", Type.EmptyTypes);
            var current = enumerator?.GetType().GetProperty("Current");
            if (enumerator == null || moveNext == null || current == null || inputs == null)
                throw new MissingMethodException("Local player enumeration unavailable");
            // Offline/single-player characters are not necessarily network-lobby players.
            for (int index = 0; index < 4 && Equals(moveNext.Invoke(enumerator, null), true); index++)
            {
                object? id = current.GetValue(enumerator);
                if (id == null) continue;
                _movementInput = inputs.GetMethod("GetMovementInput", new[] { id.GetType() });
                _rawMovementInput = inputs.GetMethod("GetRewiredMovementInput", new[] { id.GetType() });
                if (_movementInput == null || _rawMovementInput == null) continue;
                _playerIds.Add(id);
            }
            _inputStatus = _playerIds.Count > 0 ? "native player input bound" : "no valid player input found";
        }
        catch (Exception ex) { _inputStatus = "unavailable: " + ex.GetBaseException().Message; }
    }

    private static string InputVector(object? value) => value is Vector2 vector
        ? string.Format(CultureInfo.InvariantCulture, "{0:F4},{1:F4}", vector.x, vector.y)
        : "unavailable:" + (value?.GetType().Name ?? "null");

    private void CaptureInputs(string prefix)
    {
        for (int index = 0; index < _playerIds.Count; index++)
        {
            try
            {
                object[] args = { _playerIds[index] };
                object? accepted = _movementInput?.Invoke(null, args);
                object? raw = _rawMovementInput?.Invoke(null, args);
                _output.Append(prefix).Append("INPUT playerIndex=").Append(index)
                    .Append(" accepted=").Append(InputVector(accepted)).Append(" rewired=").Append(InputVector(raw)).AppendLine();
            }
            catch (Exception ex)
            {
                _output.AppendLine("inputReadUnavailable=" + ex.GetBaseException().Message);
                _playerIds.Clear(); // One failure, not recurring exceptions during movement.
                break;
            }
        }
    }

    [HideFromIl2Cpp]
    private void BindMotorProbes()
    {
        ReleaseProbes();
        if (!_contactTypeRegistered)
        {
            ClassInjector.RegisterTypeInIl2Cpp<MovementContactProbe>();
            _contactTypeRegistered = true;
        }
        var game = ReflectionUtil.GameAssembly;
        _roomRunning = game?.GetType("GameRoomEnquiries")?.GetMethod("InARunningRoom", Type.EmptyTypes);
        foreach (var entry in _bodies)
        {
            // Attach observers only to real moving bodies, not kinematic detector slaves.
            if (entry.Body.isKinematic) continue;
            foreach (string typeName in new[] { "PlayerControlCharacterMotor", "Motor" })
            {
                var type = game?.GetType(typeName);
                if (type == null) continue;
                var component = entry.Body.gameObject.GetComponent(Il2CppType.From(type));
                if (component == null) continue;
                object? motor = type.GetConstructor(new[] { typeof(IntPtr) })?.Invoke(new object[] { component.Pointer });
                if (motor == null) continue;
                var fields = new[] { "desiredVelocity", "desiredPosition", "fixedUpdateAction",
                    "ControlledInducedMovementLastUpdate", "MovementIntentionLastUpdate", "CanMoveSelf",
                    "CanBeControlled", "motorRestrictors", "mostRecentlyAppliedRigidbodyPropertiesId" };
                var properties = fields.Select(name => type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance))
                    .Where(property => property != null).Cast<PropertyInfo>().ToArray();
                _motors.Add((entry.Body, motor, properties));
                _output.AppendLine("motorBound=" + entry.Path + " type=" + typeName + " fields=" + string.Join(",", properties.Select(p => p.Name)));
                break;
            }
            var probe = entry.Body.gameObject.AddComponent<MovementContactProbe>();
            _contacts.Add(probe);
        }
    }

    [HideFromIl2Cpp]
    private void CaptureMotors(string prefix)
    {
        try { _output.Append(prefix).Append("roomRunning=").Append(_roomRunning?.Invoke(null, null) ?? "unavailable").AppendLine(); }
        catch (Exception ex) { _output.AppendLine("roomRunningUnavailable=" + ex.GetBaseException().Message); _roomRunning = null; }
        foreach (var entry in _motors)
        {
            if (entry.Body == null) continue;
            _output.Append(prefix).Append("MOTOR ").Append(entry.Body.name);
            foreach (var property in entry.Properties)
            {
                _output.Append(' ').Append(property.Name).Append('=');
                try
                {
                    object? value = property.GetValue(entry.Motor);
                    if (value is Vector3 vector) _output.Append(V(vector));
                    else if (property.Name == "motorRestrictors" && value != null)
                        _output.Append("count:").Append(value.GetType().GetProperty("Count")?.GetValue(value) ?? "unavailable");
                    else _output.Append(value ?? "unavailable");
                }
                catch { _output.Append("unavailable"); }
            }
            _output.AppendLine();
        }
        foreach (var probe in _contacts)
            if (probe != null) _output.Append(prefix).Append("CONTACT ").Append(probe.name).Append(' ').AppendLine(probe.Snapshot());
    }

    [HideFromIl2Cpp]
    private void ReleaseProbes()
    {
        foreach (var probe in _contacts)
            if (probe != null) { probe.enabled = false; UnityEngine.Object.Destroy(probe); }
        _contacts.Clear();
        _motors.Clear();
    }

    private void OnDestroy() => ReleaseProbes();

    private void Finish(string reason)
    {
        _active = false;
        ReleaseProbes();
        _output.AppendLine("end=" + reason);
        string directory = System.IO.Path.Combine(Paths.BepInExRootPath, "diagnostics");
        System.IO.Directory.CreateDirectory(directory);
        string path = System.IO.Path.Combine(directory, "movement-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".txt");
        System.IO.File.WriteAllText(path, _output.ToString());
        _bodies.Clear();
        _output.Clear();
        Plugin.LoggerInstance?.LogInfo("[SCRC-AP] MOVEMENT CAPTURE saved='" + path + "' reason='" + reason + "'.");
        Notice("Movement capture saved. Tell me whether movement was normal or wrong.");
    }

    private void Notice(string text) { _notice = text; _noticeUntil = Time.unscaledTime + 14f; }
    private void OnGUI()
    {
        if (Time.unscaledTime < _noticeUntil)
            GUI.Box(new Rect(20, Screen.height - 85, Math.Min(780, Screen.width - 40), 40), _notice);
    }
}


// Temporary callback observer; never alters collisions, transforms, forces or materials.
internal sealed class MovementContactProbe : MonoBehaviour
{
    private readonly Dictionary<int, (float Time, string Description)> _recent = new();
    private string _failure = "";
    private int _observations;
    public MovementContactProbe(IntPtr pointer) : base(pointer) { }
    private void OnCollisionEnter(Collision collision) => Observe(collision);
    private void OnCollisionStay(Collision collision) => Observe(collision);

    [HideFromIl2Cpp]
    private void Observe(Collision collision)
    {
        if (!enabled || _failure.Length != 0) return;
        try
        {
            _observations++;
            var other = collision.collider;
            if (other == null) return;
            int id = other.GetInstanceID();
            if (!_recent.ContainsKey(id) && _recent.Count >= 16)
            {
                int oldest = _recent.OrderBy(pair => pair.Value.Time).First().Key;
                _recent.Remove(oldest);
            }
            var names = new List<string>();
            Transform? current = other.transform;
            while (current != null && names.Count < 24) { names.Add(current.name); current = current.parent; }
            names.Reverse();
            var text = new StringBuilder(string.Join("/", names));
            text.Append(" layer=").Append(other.gameObject.layer).Append(" contacts=").Append(collision.contactCount);
            for (int index = 0; index < Math.Min(collision.contactCount, 4); index++)
            {
                var point = collision.GetContact(index);
                text.Append(FormattableString.Invariant($" n=({point.normal.x:F3},{point.normal.y:F3},{point.normal.z:F3}) separation={point.separation:F5}"));
            }
            _recent[id] = (Time.unscaledTime, text.ToString());
        }
        catch (Exception ex) { _failure = "unavailable:" + ex.GetBaseException().Message; }
    }

    [HideFromIl2Cpp]
    internal string Snapshot() => _failure.Length != 0 ? _failure : "observations=" + _observations + " recent=" + string.Join(" ; ",
        _recent.Values.Where(value => Time.unscaledTime - value.Time <= 0.15f).Select(value => value.Description));
}

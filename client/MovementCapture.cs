using System.Globalization;
using System.Text;
using System.Reflection;
using BepInEx;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace RhythmCastleAP;

// Opt-in, bounded observation. Primitive player update timing observer; no physics writes.
internal sealed class MovementCapture : MonoBehaviour
{
    private const int MaxBodies = 12;
    private const int MaxSamples = 120;
    private readonly List<(Rigidbody Body, string Path)> _bodies = new();
    private readonly StringBuilder _output = new();
    private readonly List<(Rigidbody Body, string Label, object Motor, (string Name, PropertyInfo? Property)[] Properties)> _motors = new();
    private readonly Dictionary<string, (Type Type, (string Name, PropertyInfo? Property)[] Properties)> _schemas = new();
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
            MovementTimingProbe.Stop();
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
            .OrderBy(entry => MovementCaptureFormatting.ActorPriority(entry.Path))
            .ThenBy(entry => (entry.Body.position - origin).sqrMagnitude).ToArray();
        foreach (var entry in bodies.Take(MaxBodies))
            _bodies.Add((entry.Body, MovementCaptureFormatting.ActorLabel(entry.Path, entry.Body.GetInstanceID())));
        _samples = 0;
        BindPlayerInputs();
        BindMotorProbes(true);
        MovementTimingProbe.Begin();
        _output.AppendLine($"targetFrameRate={Application.targetFrameRate} vSyncCount={QualitySettings.vSyncCount}");
        _output.AppendLine(MontageAnimalTick.Summary);
        _output.AppendLine(PlayerMovementTiming.Summary);
        _output.AppendLine("inputBinding=" + _inputStatus);
        _output.AppendLine("playerUpdateCallCount=recorded-at-end requestedDisplacementPerCall=unavailable Character state/leader/look demand require a bound native cachedState; missing fields are unavailable. Facing records show cached automatic rotation and last enacted look demand, not all current overrides. New actors after initial scene selection are not discovered. Rebind every 10 samples (~1s), at most 16 transforms per selected actor; fields/type metadata cached. rootForward is NOT visual facing.");
        _output.AppendLine("playerUpdateDeltaTime=recorded-at-end (native player update argument; Unity sample delta is not motor-call delta). Restrictor identities are sampled; pointer IDs are capture-local, not persistent. Contacts omit triggers/casts and do not establish absence of restrictions.");
        _output.AppendLine($"version={Plugin.PluginVersion} room={_room} activeBodies={bodies.Length} capturedBodies={_bodies.Count} cap={MaxBodies}");
        _output.AppendLine("sample|realtime|frame|fixedTicks|timeScale|fixedDelta|delta|maxFrameDelta|focused|body|position|rootTransformPosition|velocity|rootForward|kinematic|gravity|sleeping|constraints|detectCollisions");
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
        if (_samples > 0 && _samples % 10 == 0) BindMotorProbes(false);
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
    private void BindMotorProbes(bool initial)
    {
        if (initial)
        {
            ReleaseProbes();
            _schemas.Clear();
            if (!_contactTypeRegistered)
            {
                ClassInjector.RegisterTypeInIl2Cpp<MovementContactProbe>();
                _contactTypeRegistered = true;
            }
        }
        else _motors.Clear();
        var game = ReflectionUtil.GameAssembly;
        _roomRunning = game?.GetType("GameRoomEnquiries")?.GetMethod("InARunningRoom", Type.EmptyTypes);
        foreach (var entry in _bodies)
        {
            if (entry.Body == null) continue;
            bool motorFound = false;
            foreach (string typeName in new[] { "PlayerControlCharacterMotor", "DirectableCharacterMotor", "FollowingAIControlCharacterMotor", "Motor" })
            {
                var type = game?.GetType(typeName);
                if (type == null) continue;
                var component = entry.Body.gameObject.GetComponent(Il2CppType.From(type));
                if (component == null) continue;
                BindObservation(entry.Body, entry.Path, component.Pointer, type, new[] {
                    "desiredVelocity", "desiredPosition", "fixedUpdateAction", "ControlledInducedMovementLastUpdate",
                    "MovementIntentionLastUpdate", "CanMoveSelf", "CanBeControlled", "motorRestrictors",
                    "mostRecentlyAppliedRigidbodyPropertiesId", "moveSpeed", "moveSpeedRatio", "accelerationTime",
                    "timeSpendMoving", "speed", "Speed", "distanceToMaintainFromLeader", "canMoveSelf",
                    "dontFollowIfFloorAbsent", "currentTargetDistanceAlongSpline", "thisIdentifiableCharacter" });
                motorFound = true;
                break;
            }
            if (!motorFound) _output.AppendLine("binding sample=" + _samples + " actor=" + entry.Path + " motor=unavailable");
            // Search at most 16 transforms under this body; no repeated whole-scene scans.
            var facingType = game?.GetType("AutomaticCharacter2DRotationView");
            bool facingFound = false;
            if (facingType != null)
            {
                var pending = new Queue<Transform>();
                pending.Enqueue(entry.Body.transform);
                for (int visited = 0; pending.Count > 0 && visited < 16; visited++)
                {
                    var node = pending.Dequeue();
                    var facing = node.gameObject.GetComponent(Il2CppType.From(facingType));
                    if (facing != null)
                    {
                        BindObservation(entry.Body, entry.Path, facing.Pointer, facingType, new[] {
                            "cachedHorizontalMovingDirectionSign", "cachedWorldMovementDirection",
                            "timeSpentMovingInCachedHorizontalDirection", "movementTimeRequiredForDirectionChange",
                            "speedRequiredToBeConsideredMoving", "timeOfLastLookAtPositionDemandEnacted",
                            "lastLookDirectionApplied", "frameCountOfLastLookDirectionApplied",
                            "currentHorizontalFacingDirectionSign", "childToFlip", "childToPointAnyway" });
                        facingFound = true;
                        break;
                    }
                    for (int child = 0; child < node.childCount && pending.Count + visited < 16; child++)
                        pending.Enqueue(node.GetChild(child));
                }
            }
            if (!facingFound) _output.AppendLine("binding sample=" + _samples + " actor=" + entry.Path + " automatic2DFacing=unavailable (body/first16descendants)");
            if (initial && !entry.Body.isKinematic)
                _contacts.Add(entry.Body.gameObject.AddComponent<MovementContactProbe>());
        }
    }

    [HideFromIl2Cpp]
    private void BindObservation(Rigidbody body, string label, IntPtr pointer, Type type, string[] names)
    {
        if (!_schemas.TryGetValue(type.FullName!, out var schema))
        {
            schema = (type, names.Select(name => (name, type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance))).ToArray());
            _schemas.Add(type.FullName!, schema);
        }
        object? instance = type.GetConstructor(new[] { typeof(IntPtr) })?.Invoke(new object[] { pointer });
        if (instance == null) { _output.AppendLine("binding actor=" + label + " type=" + type.Name + " unavailable"); return; }
        _motors.Add((body, label, instance, schema.Properties));
        if (type.Name.EndsWith("Motor", StringComparison.Ordinal)) BindCharacterState(body, label, instance, type);
        _output.AppendLine("binding sample=" + _samples + " actor=" + label + " type=" + type.Name + " component=0x" + pointer.ToInt64().ToString("x"));
    }
    [HideFromIl2Cpp]
    private void BindCharacterState(Rigidbody body, string label, object motor, Type motorType)
    {
        try
        {
            object? character = motorType.GetProperty("thisIdentifiableCharacter")?.GetValue(motor);
            object? nativeType = character?.GetType().GetMethod("GetIl2CppType", Type.EmptyTypes)?.Invoke(character, null);
            string? name = nativeType?.GetType().GetProperty("FullName")?.GetValue(nativeType)?.ToString();
            var actualType = name == null ? null : ReflectionUtil.GameAssembly?.GetType(name);
            object? pointer = character?.GetType().GetProperty("Pointer")?.GetValue(character);
            object? actual = pointer is IntPtr address ? actualType?.GetConstructor(new[] { typeof(IntPtr) })?.Invoke(new object[] { address }) : null;
            object? state = actualType?.GetProperty("cachedState")?.GetValue(actual);
            if (state == null) throw new InvalidOperationException();
            var stateType = state.GetType();
            var names = new[] { "CurrentBehaviour", "BehaviourMode", "LeaderCharacterToFollow", "LeaderCharacterEntity",
                "LookAtPositionDemand", "MostRecentMotionReport", "MotionReportsTerminated", "RoomTimeOfLastSignificantMovement" };
            if (!_schemas.TryGetValue(stateType.FullName!, out var schema))
            {
                schema = (stateType, names.Select(field => (field, stateType.GetProperty(field))).ToArray());
                _schemas.Add(stateType.FullName!, schema);
            }
            _motors.Add((body, label, state, schema.Properties));
        }
        catch { _output.AppendLine("binding sample=" + _samples + " actor=" + label + " characterState=unavailable"); }
    }
    [HideFromIl2Cpp]
    private void CaptureMotors(string prefix)
    {
        try { _output.Append(prefix).Append("roomRunning=").Append(_roomRunning?.Invoke(null, null) ?? "unavailable").AppendLine(); }
        catch (Exception ex) { _output.AppendLine("roomRunningUnavailable=" + ex.GetBaseException().Message); _roomRunning = null; }
        foreach (var entry in _motors)
        {
            if (entry.Body == null) continue;
            _output.Append(prefix).Append("OBSERVATION ").Append(entry.Label).Append(" type=").Append(entry.Motor.GetType().Name);
            foreach (var property in entry.Properties)
            {
                _output.Append(' ').Append(property.Name).Append('=');
                try
                {
                    if (property.Property == null) { _output.Append("unavailable"); continue; }
                    object? value = property.Property.GetValue(entry.Motor);
                    if (value is Vector3 vector) _output.Append(V(vector));
                    else if (value is Vector2 vector2) _output.Append(InputVector(vector2));
                    else if (value is Transform visual) _output.Append("transform#").Append(visual.GetInstanceID()).Append(" localScale=").Append(V(visual.localScale)).Append(" forward=").Append(V(visual.forward));
                    else if (property.Name == "motorRestrictors")
                        _output.Append(MovementCaptureFormatting.Restrictors(value));
                    else if (property.Name == "mostRecentlyAppliedRigidbodyPropertiesId")
                        _output.Append(MovementCaptureFormatting.NullableInt(value));
                    else if (value is IFormattable scalar)
                        _output.Append(scalar.ToString(null, CultureInfo.InvariantCulture));
                    else _output.Append(property.Property == null ? "unavailable" : MovementCaptureFormatting.Details(value));
                }
                catch { _output.Append("unavailable"); }
            }
            _output.AppendLine();
        }
        foreach (var probe in _contacts)
            if (probe != null) _output.Append(prefix).Append("CONTACT ").Append(MovementCaptureFormatting.ActorLabel(BodyPath(probe.GetComponent<Rigidbody>()), probe.GetComponent<Rigidbody>().GetInstanceID())).Append(' ').AppendLine(probe.Snapshot());
    }

    [HideFromIl2Cpp]
    private void ReleaseProbes()
    {
        foreach (var probe in _contacts)
            if (probe != null) { probe.enabled = false; UnityEngine.Object.Destroy(probe); }
        _contacts.Clear();
        _motors.Clear();
    }

    private void OnDestroy() { MovementTimingProbe.Stop(); ReleaseProbes(); }

    private void Finish(string reason)
    {
        _active = false;
        ReleaseProbes();
        MovementTimingProbe.AppendTo(_output);
        _output.AppendLine("end=" + reason);
        _output.AppendLine(MontageAnimalTick.Summary);
        _output.AppendLine(PlayerMovementTiming.Summary);
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

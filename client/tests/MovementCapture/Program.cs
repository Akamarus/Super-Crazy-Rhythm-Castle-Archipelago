using System.Reflection;
if (args.Length > 0) { foreach (var path in args) MetadataInspection.Print(path); return 0; }
var type = Assembly.GetExecutingAssembly().GetType("RhythmCastleAP.MovementCaptureFormatting");
string Format(string method, object? value) => type == null ? "formatter unavailable" : (string)type.GetMethod(method)!.Invoke(null, new[] { value })!;
int failures = 0;
void Check(string label, string expected, string actual) { if (expected != actual) { failures++; Console.WriteLine($"FAIL {label}: expected {expected}; actual {actual}"); } }
Check("nullable value", "42", Format("NullableInt", new NativeNullable(true, 42)));
Check("nullable empty does not read Value", "null", Format("NullableInt", new NativeNullable(false, 0)));
Check("managed nullable null", "null", Format("NullableInt", null));
Check("managed nullable value", "-7", Format("NullableInt", (int?)-7));
Check("unreadable nullable", "unavailable", Format("NullableInt", new object()));
var list = new NativeList(1);
Check("runtime type and native identity", "count:1 [Native.Restrictor#ptr:0x10]", Format("Restrictors", list));
list.Offset = 16;
Check("same count replacement remains visible", "count:1 [Native.Restrictor#ptr:0x20]", Format("Restrictors", list));
var large = new NativeList(1000000);
var result = Format("Restrictors", large);
Check("bounded list reads", "16", large.Reads.ToString());
Check("truncation visible", "True", result.Contains("truncated").ToString());
Check("missing list is not empty", "unavailable", Format("Restrictors", null));
Check("empty list", "count:0 []", Format("Restrictors", new NativeList(0)));
Check("bad list", "unavailable", Format("Restrictors", new object()));
Check("NPC before detector", "True", ((int?)type?.GetMethod("ActorPriority")?.Invoke(null, new object[] { "Scene/MeatCatCharacter" }) < (int?)type?.GetMethod("ActorPriority")?.Invoke(null, new object[] { "Scene/PlayerCharacter/DetectorSlave" })).ToString());
Check("player before NPC", "True", ((int?)type?.GetMethod("ActorPriority")?.Invoke(null, new object[] { "Scene/PlayerCharacter" }) < (int?)type?.GetMethod("ActorPriority")?.Invoke(null, new object[] { "Scene/MeatCatCharacter" })).ToString());
Check("actor identity preserves duplicate paths", "dog#body:17", type?.GetMethod("ActorLabel")?.Invoke(null, new object[] { "dog", 17 })?.ToString() ?? "missing");
Check("motion report values rather than wrapper name", "{MovementSinceLastFrameMagnitude=0.25;SmoothedMeasuredVelocityMagnitude=2}", type?.GetMethod("Details")?.Invoke(null, new object?[] { new MotionReport() })?.ToString() ?? "missing");
Check("unknown wrapper explicit", "unavailable:Object", type?.GetMethod("Details")?.Invoke(null, new object?[] { new object() })?.ToString() ?? "missing");
Console.WriteLine($"Movement capture: {failures} failures");
return failures == 0 ? 0 : 1;
public sealed class NativeNullable(bool present, int value) { public bool HasValue => present; public int Value => present ? value : throw new InvalidOperationException("empty"); }
public sealed class NativeList(int count) { public int Count => count; public int Reads; public int Offset; public object this[int index] { get { Reads++; return new Restrictor(16 + Offset + index); } } }
public sealed class Restrictor(int id) { public IntPtr Pointer => (IntPtr)id; public NativeType GetIl2CppType() => new(); }
public sealed class NativeType { public string FullName => "Native.Restrictor"; }



public sealed class MotionReport { public float MovementSinceLastFrameMagnitude => 0.25f; public float SmoothedMeasuredVelocityMagnitude => 2f; }

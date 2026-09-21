using System.Reflection;
using RhythmCastleAP;
using UnityEngine;

internal static class OverlayTests
{
    internal static void Run()
    {
        var feed = ItemNotifications.Feed;
        feed.Reset(); feed.Connect(1, "overlay"); feed.Receive(1, 0, Array.Empty<NotificationReceipt>());
        feed.Receive(1, 0, new[] {
            new NotificationReceipt(new string('I', 160), new string('P', 160), new string('L', 160), false),
            new NotificationReceipt("Following reward", "Sam", "Source", false) });
        var overlay = new ItemNotificationOverlay(IntPtr.Zero);
        void Invoke(string method) => typeof(ItemNotificationOverlay).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(overlay, null);
        Invoke("Update"); Invoke("OnGUI");
        if (GUI.ScrollViews != 1 || (int)typeof(ItemNotificationOverlay).GetField("_drawnCount", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(overlay)! != 1)
            throw new Exception("an oversized first popup must draw a scrollable card and accrue visible time");
        for (int i = 0; i < 62; i++) { Invoke("Update"); Invoke("OnGUI"); }
        if (feed.Visible.Length != 1 || !feed.Visible[0].Text.Contains("Following reward"))
            throw new Exception("oversized popup must expire and allow the next reward through");
        GUI.LongLabels.Clear(); GUI.ScrollViews = 0;
        Input.Next = KeyCode.F6; Invoke("Update"); Invoke("OnGUI");
        if (GUI.ScrollViews != 1 || GUI.LongLabels.Count != 2 || GUI.LongLabels.Any(label => label.Height < label.Required))
            throw new Exception("history must retain complete wrapped reward and location text in a scrollable viewport");
        double outstanding = feed.OutstandingCount;
        for (int i = 0; i < 70; i++) { Invoke("Update"); Invoke("OnGUI"); }
        if (feed.OutstandingCount != outstanding) throw new Exception("scrollable history must continue pausing popup lifetimes");
        feed.Reset();
        Console.WriteLine("PASS: real overlay oversized-card progress, wrapped history, and history timer pause (fake Unity rendering boundary)");
    }
}

namespace RhythmCastleAP
{
    internal static class ClientPerformance { internal static IDisposable? Measure(string name) => null; }
}

// Deterministic font metrics let the real overlay exercise narrow-screen wrapping
// without launching Unity. Native font appearance and input still need game QA.
namespace UnityEngine
{
    public class MonoBehaviour { public MonoBehaviour(IntPtr pointer) { } }
    public struct Vector2 { public static Vector2 zero => default; }
    public readonly record struct Rect(float x, float y, float width, float height);
    public struct Color { public Color(float r, float g, float b) { } public static Color white => default; }
    public enum FontStyle { Bold }
    public enum KeyCode { F6, Escape }
    public enum EventType { Repaint }
    public sealed class Event { public static Event current = new(); public EventType type = EventType.Repaint; }
    public static class Input { public static KeyCode? Next; public static bool GetKeyDown(KeyCode key) { if (Next != key) return false; Next = null; return true; } }
    public static class Application { public static bool isFocused = true; }
    public static class Screen { public static int width = 180, height = 220; }
    public static class Time { public static float unscaledDeltaTime = 0.1f; }
    public sealed class GUIContent { public string Text; public GUIContent(string text) { Text = text; } }
    public sealed class GUIStyle
    {
        public int fontSize = 12; public bool wordWrap, richText; public FontStyle fontStyle;
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { fontSize = other.fontSize; }
        public float CalcHeight(GUIContent content, float width) => Math.Max(1, (float)Math.Ceiling(content.Text.Length / Math.Max(1, Math.Floor(width / (fontSize * 0.5f))))) * fontSize;
    }
    public sealed class GUISkin { public GUIStyle label = new(); }
    public static class GUI
    {
        public static GUISkin skin = new(); public static Color color; public static int depth, ScrollViews;
        public static readonly List<(float Height, float Required)> LongLabels = new();
        public static void Box(Rect rect, string text) { }
        public static void Label(Rect rect, string text, GUIStyle? style) { if (text.Length > 100) LongLabels.Add((rect.height, style!.CalcHeight(new GUIContent(text), rect.width))); }
        public static bool Button(Rect rect, string text) => false;
        public static Vector2 BeginScrollView(Rect viewport, Vector2 position, Rect content) { ScrollViews++; return position; }
        public static void EndScrollView() { }
    }
}

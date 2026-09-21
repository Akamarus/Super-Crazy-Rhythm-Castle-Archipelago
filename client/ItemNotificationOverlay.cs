using UnityEngine;

namespace RhythmCastleAP;

// A small persistent overlay; no hierarchy scans or native reward dialogs.
internal sealed class ItemNotificationOverlay : MonoBehaviour
{
    private GUIStyle? _text, _detail, _heading;
    private float _styleScale;
    private bool _historyOpen, _failed;
    private int _drawnCount;
    private Vector2 _historyScroll;
    private Vector2 _popupScroll;
    private ItemNotification? _scrollingEntry;
    private readonly float[] _historyTextHeights = new float[100], _historyDetailHeights = new float[100];
    private ItemNotification[] _visible = Array.Empty<ItemNotification>();
    private ItemNotification[] _history = Array.Empty<ItemNotification>();
    internal static bool PopupsEnabled = true;
    public ItemNotificationOverlay(IntPtr pointer) : base(pointer) { }
    private void Update()
    {
        using var timing = ClientPerformance.Measure("ItemNotificationOverlay.Update");
        if (Input.GetKeyDown(KeyCode.F6)) { _historyOpen = !_historyOpen; _historyScroll = Vector2.zero; }
        if (_historyOpen && Input.GetKeyDown(KeyCode.Escape)) _historyOpen = false;
        // Count only time actually presented, not history/disabled/unfocused time or a loading stall.
        int presented = !_historyOpen && PopupsEnabled && Application.isFocused ? _drawnCount : 0;
        ItemNotifications.Feed.Advance(Math.Min(Time.unscaledDeltaTime, 0.1f), presented);
        _drawnCount = 0;
        _visible = ItemNotifications.Feed.Visible;
        if (_historyOpen) _history = ItemNotifications.Feed.History;
    }
    private void OnGUI()
    {
        using var timing = ClientPerformance.Measure("ItemNotificationOverlay.OnGUI");
        if (_failed) return;
        try { Draw(); }
        catch (Exception ex)
        {
            _failed = true;
            Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] Notification overlay could not draw: {ex.Message}");
        }
    }
    private void Draw()
    {
        if (!_historyOpen && (!PopupsEnabled || _visible.Length == 0)) return;
        float scale = Math.Clamp(Screen.height / 1080f, 0.6f, 1.6f);
        if (_text == null || Math.Abs(_styleScale - scale) > 0.01f)
        {
            _styleScale = scale;
            _text = new GUIStyle(GUI.skin.label) { fontSize = (int)(20 * scale), wordWrap = true, richText = false };
            _detail = new GUIStyle(_text) { fontSize = (int)(15 * scale) };
            _heading = new GUIStyle(_text) { fontSize = (int)(23 * scale), fontStyle = FontStyle.Bold };
        }
        Color oldColor = GUI.color;
        int oldDepth = GUI.depth;
        try
        {
            GUI.depth = -1000;
            GUI.color = Color.white;
            if (_historyOpen)
            {
                float width = Math.Min(Screen.width - 30 * scale, 820 * scale);
                float x = (Screen.width - width) / 2;
                float y = 35 * scale;
                GUI.Box(new Rect(x, y, width, Screen.height - 70 * scale), string.Empty);
                GUI.Label(new Rect(x + 18 * scale, y + 12 * scale, width - 130 * scale, 40 * scale), "Archipelago item history", _heading);
                if (GUI.Button(new Rect(x + width - 95 * scale, y + 12 * scale, 78 * scale, 32 * scale), "Close / F6")) _historyOpen = false;
                const string historyHelp = "Latest 100 items • newest first • gameplay continues while this panel is open";
                float helpHeight = _detail!.CalcHeight(new GUIContent(historyHelp), width - 36 * scale);
                GUI.Label(new Rect(x + 18 * scale, y + 49 * scale, width - 36 * scale, helpHeight), historyHelp, _detail);
                float listTop = y + 61 * scale + helpHeight;
                float listHeight = Math.Max(1, Screen.height - 70 * scale - listTop);
                // Reserve scrollbar space when measuring, so wrapping stays stable.
                float rowWidth = Math.Max(1, width - 60 * scale);
                float contentHeight = 0;
                for (int i = 0; i < _history.Length; i++)
                {
                    _historyTextHeights[i] = _text!.CalcHeight(new GUIContent(_history[i].Text), rowWidth);
                    _historyDetailHeights[i] = _detail.CalcHeight(new GUIContent(_history[i].Location), rowWidth);
                    contentHeight += _historyTextHeights[i] + _historyDetailHeights[i] + 16 * scale;
                }
                const string emptyHistory = "No item notifications yet. Connect to your seed to load received-item history.";
                if (_history.Length == 0) contentHeight = _text!.CalcHeight(new GUIContent(emptyHistory), rowWidth);
                _historyScroll = GUI.BeginScrollView(new Rect(x + 14 * scale, listTop, width - 28 * scale, listHeight),
                    _historyScroll, new Rect(0, 0, rowWidth, Math.Max(listHeight, contentHeight)));
                try
                {
                    float rowY = 0;
                    if (_history.Length == 0) GUI.Label(new Rect(0, 0, rowWidth, contentHeight), emptyHistory, _text);
                    for (int i = 0; i < _history.Length; i++)
                    {
                        DrawEntry(_history[i], 0, rowY, rowWidth, scale, _historyTextHeights[i], _historyDetailHeights[i]);
                        rowY += _historyTextHeights[i] + _historyDetailHeights[i] + 16 * scale;
                    }
                }
                finally { GUI.EndScrollView(); }
                GUI.Label(new Rect(x + 18 * scale, Screen.height - 65 * scale, width - 36 * scale, 26 * scale),
                    "Scroll to browse • F6: close", _detail);
            }
            else if (PopupsEnabled)
            {
                float width = Math.Min(460 * scale, Screen.width - 32 * scale);
                float x = Screen.width - width - 20 * scale;
                float y = 92 * scale;
                float footerHeight = 30 * scale;
                int drawn = 0;
                bool scrolling = false;
                for (int i = 0; i < _visible.Length; i++)
                {
                    float innerWidth = width - 24 * scale;
                    float textHeight = _text!.CalcHeight(new GUIContent(_visible[i].Text), innerWidth);
                    float detailHeight = _detail!.CalcHeight(new GUIContent(_visible[i].Location), innerWidth);
                    float height = textHeight + detailHeight + 24 * scale;
                    float availableHeight = Screen.height - 16 * scale - footerHeight - y;
                    if (height > availableHeight)
                    {
                        if (drawn > 0 || availableHeight < 40 * scale) break;
                        // An oversized first reward must not hold the entire queue forever.
                        // Keep its complete text scrollable, with full detail also in F6 history.
                        if (!ReferenceEquals(_scrollingEntry, _visible[i]))
                        {
                            _scrollingEntry = _visible[i];
                            _popupScroll = Vector2.zero;
                        }
                        innerWidth = Math.Max(1, width - 52 * scale);
                        textHeight = _text.CalcHeight(new GUIContent(_visible[i].Text), innerWidth);
                        detailHeight = _detail.CalcHeight(new GUIContent(_visible[i].Location), innerWidth);
                        GUI.Box(new Rect(x, y, width, availableHeight), string.Empty);
                        _popupScroll = GUI.BeginScrollView(
                            new Rect(x + 12 * scale, y + 8 * scale, width - 24 * scale, availableHeight - 16 * scale),
                            _popupScroll, new Rect(0, 0, innerWidth, textHeight + detailHeight + 4 * scale));
                        try { DrawEntry(_visible[i], 0, 0, innerWidth, scale, textHeight, detailHeight); }
                        finally { GUI.EndScrollView(); }
                        y += availableHeight;
                        drawn++;
                        scrolling = true;
                        break;
                    }
                    GUI.Box(new Rect(x, y, width, height), string.Empty);
                    GUI.color = _visible[i].Sent ? new Color(0.65f, 0.85f, 1f) : new Color(0.65f, 1f, 0.72f);
                    GUI.Label(new Rect(x + 12 * scale, y + 8 * scale, innerWidth, textHeight), _visible[i].Text, _text);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(x + 12 * scale, y + textHeight + 12 * scale, innerWidth, detailHeight), _visible[i].Location, _detail);
                    y += height + 6 * scale;
                    drawn++;
                }
                if (Event.current.type == EventType.Repaint && Application.isFocused) _drawnCount = drawn;
                int queued = Math.Max(0, ItemNotifications.Feed.OutstandingCount - drawn);
                if (_visible.Length > 0)
                    GUI.Label(new Rect(x, y, width, footerHeight),
                        scrolling ? "Scroll reward • F6: full history" :
                        queued > 0 ? $"{queued} more queued • F6: recent items" : "F6: recent items", _detail);
            }
        }
        finally { GUI.color = oldColor; GUI.depth = oldDepth; }
    }
    private void DrawEntry(ItemNotification entry, float x, float y, float width, float scale, float textHeight, float detailHeight)
    {
        GUI.color = entry.Sent ? new Color(0.65f, 0.85f, 1f) : new Color(0.65f, 1f, 0.72f);
        GUI.Label(new Rect(x, y, width, textHeight), entry.Text, _text);
        GUI.color = Color.white;
        GUI.Label(new Rect(x, y + textHeight + 4 * scale, width, detailHeight), entry.Location, _detail);
    }
}

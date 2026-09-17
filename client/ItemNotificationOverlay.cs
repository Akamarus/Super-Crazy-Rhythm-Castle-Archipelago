using UnityEngine;

namespace RhythmCastleAP;

// A small persistent overlay; no hierarchy scans or native reward dialogs.
internal sealed class ItemNotificationOverlay : MonoBehaviour
{
    private GUIStyle? _text, _detail, _heading;
    private float _styleScale;
    private bool _historyOpen, _failed;
    private int _page;
    private ItemNotification[] _visible = Array.Empty<ItemNotification>();
    private ItemNotification[] _history = Array.Empty<ItemNotification>();
    internal static bool PopupsEnabled = true;
    public ItemNotificationOverlay(IntPtr pointer) : base(pointer) { }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F6)) { _historyOpen = !_historyOpen; _page = 0; }
        if (_historyOpen && Input.GetKeyDown(KeyCode.Escape)) _historyOpen = false;
        ItemNotifications.Feed.Advance(Time.unscaledDeltaTime);
        _visible = ItemNotifications.Feed.Visible;
        if (_historyOpen) _history = ItemNotifications.Feed.History;
    }
    private void OnGUI()
    {
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
                int perPage = Math.Max(1, (int)((Screen.height - 215 * scale) / (90 * scale)));
                int pages = Math.Max(1, (_history.Length + perPage - 1) / perPage);
                _page = Math.Clamp(_page, 0, pages - 1);
                GUI.Box(new Rect(x, y, width, Screen.height - 70 * scale), string.Empty);
                GUI.Label(new Rect(x + 18 * scale, y + 12 * scale, width - 130 * scale, 40 * scale), "Archipelago item history", _heading);
                if (GUI.Button(new Rect(x + width - 95 * scale, y + 12 * scale, 78 * scale, 32 * scale), "Close / F6")) _historyOpen = false;
                GUI.Label(new Rect(x + 18 * scale, y + 49 * scale, width - 36 * scale, 26 * scale), "Latest 100 items • newest first • gameplay continues while this panel is open", _detail);
                if (_history.Length == 0) GUI.Label(new Rect(x + 18 * scale, y + 100 * scale, width - 36 * scale, 60 * scale), "No item notifications yet. Connect to your seed to load received-item history.", _text);
                for (int i = 0; i < perPage; i++)
                {
                    int index = _page * perPage + i;
                    if (index >= _history.Length) break;
                    DrawEntry(_history[index], x + 14 * scale, y + (86 + i * 90) * scale, width - 28 * scale, scale);
                }
                float bottom = Screen.height - 78 * scale;
                if (_page > 0 && GUI.Button(new Rect(x + 18 * scale, bottom, 100 * scale, 30 * scale), "Newer")) _page--;
                GUI.Label(new Rect(x + width / 2 - 60 * scale, bottom, 150 * scale, 30 * scale), $"{_page + 1} / {pages}", _detail);
                if (_page + 1 < pages && GUI.Button(new Rect(x + width - 118 * scale, bottom, 100 * scale, 30 * scale), "Older")) _page++;
            }
            else if (PopupsEnabled)
            {
                float width = Math.Min(460 * scale, Screen.width - 32 * scale);
                float x = Screen.width - width - 20 * scale;
                for (int i = 0; i < _visible.Length; i++)
                {
                    float y = (92 + i * 112) * scale;
                    GUI.Box(new Rect(x, y, width, 106 * scale), string.Empty);
                    DrawEntry(_visible[i], x + 12 * scale, y + 8 * scale, width - 24 * scale, scale);
                }
                if (_visible.Length > 0) GUI.Label(new Rect(x, (94 + _visible.Length * 112) * scale, width, 26 * scale), "F6: recent items", _detail);
            }
        }
        finally { GUI.color = oldColor; GUI.depth = oldDepth; }
    }
    private void DrawEntry(ItemNotification entry, float x, float y, float width, float scale)
    {
        GUI.color = entry.Sent ? new Color(0.65f, 0.85f, 1f) : new Color(0.65f, 1f, 0.72f);
        GUI.Label(new Rect(x, y, width, 58 * scale), entry.Text, _text);
        GUI.color = Color.white;
        GUI.Label(new Rect(x, y + 60 * scale, width, 26 * scale), entry.Location, _detail);
    }
}

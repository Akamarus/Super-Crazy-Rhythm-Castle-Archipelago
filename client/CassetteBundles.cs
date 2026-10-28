using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json.Linq;
namespace RhythmCastleAP;
internal sealed class CassetteBundleContract
{
    internal bool Bundled { get; }
    private readonly Dictionary<string,string[]> _songs;
    private CassetteBundleContract(bool bundled, Dictionary<string,string[]> songs) { Bundled=bundled; _songs=songs; }
    internal static CassetteBundleContract Parse(Dictionary<string,object>? data)
    {
        var root=data==null?new JObject():JObject.FromObject(data);
        var schema=root["schema_version"];
        bool modern=schema?.Type==JTokenType.Integer && (long)schema is 20 or 21;
        var size=root["cassette_bundle_size"];
        var map=root["cassette_bundles"];
        if(!modern) {
            if(size!=null && (size.Type!=JTokenType.Integer || (long)size!=1) || map!=null && (map is not JObject old || old.Count!=0))
                throw new InvalidOperationException("Cassette bundles require schema 20");
            return new(false,new());
        }
        if(schema?.Type==JTokenType.Integer && (long)schema==21 && size?.Type==JTokenType.Integer && (long)size==1 && map is JObject individual && individual.Count==0)
            return new(false,new());
        if(size?.Type!=JTokenType.Integer || (long)size is not (2 or 3) || map is not JObject bundles)
            throw new InvalidOperationException("Invalid cassette_bundle_size or cassette_bundles");
        int count=30/(int)size;
        if(bundles.Count!=count)throw new InvalidOperationException("Incomplete cassette_bundles");
        var songs=new Dictionary<string,string[]>(StringComparer.Ordinal);
        var seen=new HashSet<string>(StringComparer.Ordinal);
        for(int n=1;n<=count;n++) {
            string name=$"Cassette Bundle {n:00}";
            if(bundles[name] is not JArray members || members.Count!=(int)size)throw new InvalidOperationException("Invalid bundle members: "+name);
            var values=new List<string>();
            foreach(var member in members) {
                if(member.Type!=JTokenType.String || !CassetteCatalog.ByItemName.TryGetValue((string)member!,out var cassette) || !seen.Add(cassette.ItemName))
                    throw new InvalidOperationException("Unknown or duplicate cassette in "+name);
                values.Add(cassette.NativeSong);
            }
            songs.Add(name,values.ToArray());
        }
        return new(true,songs);
    }
    internal IReadOnlyList<string> SongsFor(long id) => id>=187256164 && id<=187256178 && _songs.TryGetValue($"Cassette Bundle {id-187256163:00}",out var songs)?songs:Array.Empty<string>();
    internal IReadOnlyList<string> ReceiptSongsFor(long id) => CassetteCatalog.NativeSongByItemId.TryGetValue(id, out var song) ? new[]{song} : SongsFor(id);
    internal string Display(long id, string fallback) => SongsFor(id).Count > 0 ? Display($"Cassette Bundle {id-187256163:00}") : fallback;
    internal string Display(string name) => name != null && _songs.TryGetValue(name,out var songs)?name+": "+string.Join(", ",songs.Select(s=>CassetteCatalog.ByNativeSong[s].DisplaySong)):name!;
}
internal sealed class CassetteBundleSessionHistory
{
    internal static readonly object Sync=new();
    private static CassetteBundleSessionHistory? _current;
    private readonly long _generation;
    private readonly List<long> _items=new();
    private readonly HashSet<string> _pendingNames=new(StringComparer.Ordinal);
    private readonly Func<string,bool>? _legacyReceipt;
    private CassetteBundleContract? _contract;
    private bool _ready,_ended,_managed;
    private string[]? _cachedSongs;
    internal CassetteBundleSessionHistory(long generation, Func<string,bool>? legacyReceipt=null) { _generation=generation; _legacyReceipt=legacyReceipt; lock(Sync) { if(_current==null || generation>_current._generation) { _managed=_current?._managed==true; _current=this; } } }
    internal void Configure(Dictionary<string,object>? data) {
        lock(Sync) {
            if (_contract != null) { _ready=false; _items.Clear(); }
            _cachedSongs=null;
            _contract=null;
            try { _contract=CassetteBundleContract.Parse(data); _managed=_contract.Bundled; }
            catch { _managed=true; _ready=false; _items.Clear(); _pendingNames.Clear(); throw; }
            if (!_ended && ReferenceEquals(_current,this) && !_contract.Bundled)
                foreach (string name in _pendingNames) _legacyReceipt?.Invoke(name);
            _pendingNames.Clear();
        }
    }
    internal bool TryApplyItem(string name)
    {
        bool individual = name != null && CassetteCatalog.ByItemName.ContainsKey(name);
        bool bundle = Enumerable.Range(1,15).Any(n => name == $"Cassette Bundle {n:00}");
        if (!individual && !bundle) return false;
        lock (Sync)
        {
            if (_ended || !ReferenceEquals(_current,this)) return true;
            if (_contract == null) { if (individual) _pendingNames.Add(name!); return true; }
            if (!_contract.Bundled && individual) _legacyReceipt?.Invoke(name!);
            return true;
        }
    }
    internal void HandlePacket(object packet) {
        if(packet is not ReceivedItemsPacket received)return;
        lock(Sync) {
            if(_ended)return;
            _cachedSongs=null;
            if(received.Items==null || received.Index<0 || received.Index>_items.Count || received.Index>0&&!_ready) { _ready=false;return; }
            if(received.Index==0) { _items.Clear();_ready=true; }
            for(int n=0;n<received.Items.Length;n++) {
                int index=received.Index+n;long id=received.Items[n].Item;
                if(index<_items.Count) { if(_items[index]!=id) { _ready=false;return; } }
                else _items.Add(id);
            }
        }
    }
    internal static void End(long generation) { lock(Sync) if(_current?._generation==generation) { _current._ended=true;_current._ready=false; } }
    internal static void WithSongs(Action<bool,IReadOnlyList<string>> action) {
        lock(Sync) {
            var state=_current;
            bool managed=state?._managed==true;
            var songs=state!=null&&!state._ended&&state._ready&&state._contract?.Bundled==true
                ?(state._cachedSongs ??= state._items.SelectMany(state._contract.ReceiptSongsFor).Distinct(StringComparer.Ordinal).ToArray()):Array.Empty<string>();
            action(managed,songs);
        }
    }
}

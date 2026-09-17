using System.Collections;
using System.Reflection;
namespace RhythmCastleAP;

internal static class GaragePreviewMedals
{
    internal static void Apply(object previewData, bool enabled, IEnumerable<string> ownedSongs,
        Func<string, (int? Medal, int? Pro)> readSaved)
    {
        if (!enabled) return;
        object? results = previewData.GetType().GetProperty("PreviousGarageResults")?.GetValue(previewData);
        if (results == null) return;
        Type type = results.GetType();
        if (type.GetProperty("Count")?.GetValue(results) is not int count || count < 0 || count > 8) return;
        PropertyInfo? indexer = type.GetProperty("Item", new[]{typeof(int)});
        if (indexer == null) return;
        for (int i = 0; i < count; i++) {
            object? row = indexer.GetValue(results, new object[]{i});
            if (row == null) continue;
            string? nativeType = row.GetType().GetProperty("cartridgeType")?.GetValue(row)?.ToString();
            var cartridge = GarageCartridgeNativePolicy.RandomizedCartridges.FirstOrDefault(c => c.NativeCartridgeType == nativeType);
            if (string.IsNullOrEmpty(cartridge.Song) || !ownedSongs.Contains(cartridge.Song, StringComparer.OrdinalIgnoreCase)) continue;
            var saved = readSaved(cartridge.NativeCartridgeType);
            WriteMedal(row, "medalEarned", saved.Medal);
            WriteMedal(row, "proMedalEarned", saved.Pro);
        }
    }
    private static void WriteMedal(object row, string name, int? value)
    {
        if (!value.HasValue) return;
        PropertyInfo? property = row.GetType().GetProperty(name);
        if (property?.CanWrite != true || !property.PropertyType.IsEnum ||
            Enum.GetUnderlyingType(property.PropertyType) != typeof(int) || !Enum.IsDefined(property.PropertyType, value.Value)) return;
        property.SetValue(row, Enum.ToObject(property.PropertyType, value.Value));
    }
}

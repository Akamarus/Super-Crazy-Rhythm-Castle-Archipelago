using System.Reflection;
using System.Runtime.Loader;

// Optional read-only reproduction against installed interop metadata. No native
// method, static constructor, game launch, or generated-assembly write is invoked.
internal static class NativeInteropProbe
{
    internal static string ReadChestOwnerFailure(string gameDirectory)
    {
        var context = new AssemblyLoadContext("MusicLabBoundaryMetadataProbe", isCollectible: true);
        context.Resolving += (loader, name) =>
        {
            foreach (string directory in new[] { "interop", "core" })
            {
                string path = Path.Combine(gameDirectory, "BepInEx", directory, name.Name + ".dll");
                if (File.Exists(path)) return loader.LoadFromAssemblyPath(Path.GetFullPath(path));
            }
            return null;
        };
        try
        {
            Assembly assembly = context.LoadFromAssemblyPath(Path.GetFullPath(
                Path.Combine(gameDirectory, "BepInEx", "interop", "Assembly-CSharp.dll")));
            if (assembly.GetType("CurrentPlayerSaveEnquiries", false, false) == null)
                throw new InvalidOperationException("Control getter owner did not load.");
            try
            {
                assembly.GetType("Hub06MedalScoreRewardChest", false, false);
                return "No TypeLoadException: re-investigate this interop build.";
            }
            catch (TypeLoadException exception) { return exception.Message; }
        }
        finally { context.Unload(); }
    }
}

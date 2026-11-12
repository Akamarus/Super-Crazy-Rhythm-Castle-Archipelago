using RhythmCastleAP;
int passed=0;
void Check(bool condition,string label){if(!condition)throw new Exception(label);passed++;}
Check(StarEaterTestOverridePolicy.Select("GameRoom_Hub7",40,66)==null,"Royal vanilla remains inactive");
Check(StarEaterTestOverridePolicy.Select("GameRoom_Hub8",40,66)==null,"Bunker vanilla remains inactive");
var royal=StarEaterTestOverridePolicy.Select("GameRoom_Hub7",25,66);
Check(royal?.Required==25,"Royal test threshold");
Check(royal?.RootPath=="Root/GameRoom_Hub7_Logic/Objects/StarEaterAndGap/StarEater","Royal exact native root");
Check(StarEaterTestOverridePolicy.Select("GameRoom_Hub8",25,66)==null,"Royal override does not enable Bunker");
var bunker=StarEaterTestOverridePolicy.Select("GameRoom_Hub8",40,25);
Check(bunker?.Required==25,"Bunker test threshold");
Check(bunker?.RootPath=="Root/GameRoom_Hub8_Logic/Objects/NPCs/StarEater","Bunker exact native root");
Check(StarEaterTestOverridePolicy.Select("GameRoom_Hub7",40,25)==null,"Bunker override does not enable Royal");
foreach(var room in new[]{"GameRoom_Hub1A","GameRoom_Hub2","GameRoom_Hub5B","GameRoom_Hub70",""})Check(StarEaterTestOverridePolicy.Select(room,25,25)==null,"other rooms unaffected");
Check(StarEaterTestOverridePolicy.Select("GameRoom_Hub7",100,25)==null,"Royal clamps to native maximum");
Check(StarEaterTestOverridePolicy.Select("GameRoom_Hub8",25,100)==null,"Bunker clamps to native maximum");
Check(StarEaterTestOverridePolicy.Select("GameRoom_Hub7",-1,66)?.Required==0,"negative clamps to zero");
Console.WriteLine($"{passed} Star Eater override assertions passed.");

var original = new byte[] { 7, 8, 9 };
var applied = new byte[] { 1, 2, 3 };
IntPtr address = System.Runtime.InteropServices.Marshal.AllocHGlobal(applied.Length);
try
{
    var patch = new LegacyStarEaterPatch(address, original, applied);
    System.Runtime.InteropServices.Marshal.Copy(applied, 0, address, applied.Length);
    int writes = 0;
    bool Restore(IntPtr target, byte[] value)
    {
        writes++;
        System.Runtime.InteropServices.Marshal.Copy(value, 0, target, value.Length);
        return true;
    }
    Check(!patch.TryRestore((_, _) => false), "failed restore leaves the patch retryable");
    Check(!patch.TryRestore((_, _) => true), "a reported write must release the owned bytes before unload succeeds");
    Check(patch.TryRestore(Restore) && writes == 1 &&
        System.Runtime.InteropServices.Marshal.ReadByte(address) == 7, "retry restores owned bytes");
    Check(patch.TryRestore(Restore) && writes == 1, "repeated restore does not rewrite original bytes");
    System.Runtime.InteropServices.Marshal.WriteByte(address, 33);
    Check(patch.TryRestore(Restore) && writes == 1 &&
        System.Runtime.InteropServices.Marshal.ReadByte(address) == 33, "later owner's change survives cleanup");
}
finally { System.Runtime.InteropServices.Marshal.FreeHGlobal(address); }
Console.WriteLine("Legacy Star Eater patch ownership checks passed.");
PluginComponentOwnershipTests.Run();

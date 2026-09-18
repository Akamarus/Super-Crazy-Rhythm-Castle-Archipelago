using RhythmCastleAP;
static void Check(bool ok,string name) { if(!ok) throw new Exception(name); }
foreach(var entry in ExpandedCheckCatalog.Entries.Where(e => e.Level != null)) {
 CurrentPlayerSaveEnquiries.Success = false;
 Check(ExpandedCheckResults.ReadCompleted(entry.Level!,entry.Variant!) == false,"failed binary result is not completion");
 CurrentPlayerSaveEnquiries.Success = true;
 Check(ExpandedCheckResults.ReadCompleted(entry.Level!,entry.Variant!) == true,"saved successful binary result");
 Check(CurrentPlayerSaveEnquiries.Last == entry.Level+"|"+entry.Variant,"exact variant identity");
}
Check(LevelIdentifier.Constructions==6 && LevelVariantIdentifier.Constructions==6,"native identifiers cached for repeat reads");
string source = "LEVEL_09_COMBO_ABILITY_EARNED";
CurrentPlayerSaveEnquiries.Flag = false;
Check(ExpandedCheckResults.ReadFlag(source)==false,"uncollected source");
CurrentPlayerSaveEnquiries.Flag = true;
Check(ExpandedCheckResults.ReadFlag(source)==true,"live flag value is not cached");
Check(ExpandedCheckResults.ReadFlag("UNKNOWN")==null,"unknown marker fails closed");
Check(CurrentPlayerSaveEnquiries.FlagCalls==2,"no native lookup for unknown marker");
Check(ExpandedCheckResults.ReadFlag("PRISON_HUB_STAR_EATER_FED")==true,"Cell native enum recognized");
CurrentPlayerSaveEnquiries.Flag=false;
Check(ExpandedCheckResults.ReadFlag("PRISON_HUB_STAR_EATER_FED")==false,"Cell saved value read live");
int calls=CurrentPlayerSaveEnquiries.Calls;
Check(ExpandedCheckResults.ReadCompleted("Level_06","LevelVariant_Default") == null,"normal variant excluded");
Check(ExpandedCheckResults.ReadCompleted("Level_12","LevelVariant_DevilMode") == null,"unknown pairing excluded");
Check(CurrentPlayerSaveEnquiries.Calls==calls,"unmapped result never reads native save");
CurrentPlayerSaveEnquiries.Throw = true;
Check(ExpandedCheckResults.ReadCompleted("Level_06","LevelVariant_BeeMode") == null,"unreadable fails closed");
Console.WriteLine("Expanded native binary result reader: 29 assertions passed.");
public sealed class LevelIdentifier { public static int Constructions; public string Value; public LevelIdentifier(string value) {Constructions++;Value=value;} }
public sealed class LevelVariantIdentifier { public static int Constructions; public string Value; public LevelVariantIdentifier(string value) {Constructions++;Value=value;} }
public static class CurrentPlayerSaveEnquiries {
 public static bool Flag; public static int FlagCalls; public static bool GetProgressionFlagValue(eGameProgressionFlag flag) {FlagCalls++;return Flag;}
 public static bool Success,Throw; public static int Calls; public static string Last="";
 public static bool HasBinaryResultLevelVariantBeenCompleted(LevelIdentifier level,LevelVariantIdentifier variant) {
  Calls++; Last=level.Value+"|"+variant.Value; if(Throw)throw new InvalidOperationException();return Success;
 }
}
namespace RhythmCastleAP { internal static class ReflectionUtil {internal static System.Reflection.Assembly GameAssembly=>System.Reflection.Assembly.GetExecutingAssembly();} }

public enum eGameProgressionFlag { LEVEL_09_COMBO_ABILITY_EARNED = 109001, PRISON_HUB_STAR_EATER_FED = 823 }

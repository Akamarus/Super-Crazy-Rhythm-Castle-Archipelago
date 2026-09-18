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

using RhythmCastleAP;
using System.Reflection;
int checks=0;
void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
var value=new Fixture();
Check((int)ReflectionUtil.ReadMember(value,"Value")! == 1,"reads property");
value.Value=2; Check((int)ReflectionUtil.ReadMember(value,"Value")! == 2,"native values never cached");
Check((int)ReflectionUtil.ReadMember(value,"Field")! == 3,"reads field");
Check(ReflectionUtil.ReadMember(value,"Absent")==null,"missing member");
Check(ReflectionUtil.ReadMember(value,"Throwing")==null,"throwing getter safe fallback");
Check(ReflectionUtil.ReadMember(value,"Item")==null,"indexer ignored");
Check(ReflectionUtil.ReadMember(null,"Value")==null,"null object");
Check(ReflectionUtil.ExtractIdentifier("Level_01")=="Level_01","string identifier");
var assembly=typeof(Fixture).Assembly;
var types=ReflectionUtil.SafeGetTypes(assembly);
Check(types.Contains(typeof(Fixture)),"type catalog complete");
Check(ReferenceEquals(types,ReflectionUtil.SafeGetTypes(assembly)),"stable type catalog reuses metadata instead of allocating each call");
var probe=new HubReadinessProbePolicy();
Check(!probe.ShouldSearch("GameRoom_10",1,false),"no Hub6 searches during songs");
Check(probe.ShouldSearch("GameRoom_Hub6",1,false),"immediate probe on hub entry");
Check(!probe.ShouldSearch("GameRoom_Hub6",1.1,false),"missing object retries bounded");
Check(probe.ShouldSearch("GameRoom_Hub6",1.25,false),"missing object retried");
Check(!probe.ShouldSearch("GameRoom_Hub6",2,true),"live cached object avoids search");
Check(probe.ShouldSearch("GameRoom_Hub6",3,false),"destroyed object reacquired");
Check(!probe.ShouldSearch("other",3.1,false) && probe.ShouldSearch("GameRoom_Hub6",3.11,false),"re-entry immediate despite prior retry delay");
var reports=new List<string>();
ClientPerformance.Configure(false,reports.Add);
using(ClientPerformance.Measure("fixture")) {} ClientPerformance.Frame(31,"room");
Check(reports.Count==0,"profiling disabled by default has no reports");
ClientPerformance.Configure(true,reports.Add); ClientPerformance.Frame(0,"room");
for(int i=0;i<60;i++){using(ClientPerformance.Measure("fixture")) {} ClientPerformance.Frame(.5,"room");}
Check(reports.Count==1 && reports[0].Contains("fixture:calls=60"),"timings aggregate in a single bounded report");
ClientPerformance.Frame(.1,"new room");
Check(reports.Count==1,"short transition does not flood logs");
Console.WriteLine($"{checks} reflection checks passed");
class Fixture {public int Value{get;set;}=1;public int Field=3;public int Throwing=>throw new Exception();public int this[int x]=>x;}

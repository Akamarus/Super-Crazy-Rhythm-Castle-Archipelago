using System.Reflection;
var t=Assembly.GetExecutingAssembly().GetType("RhythmCastleAP.MovementTimingBuffer");
if(t==null){Console.WriteLine("FAIL: timing recorder missing");return 1;}
dynamic b=Activator.CreateInstance(t,4)!;
void Check(bool ok,string name){if(!ok)throw new Exception(name);}
b.Record(1L,10,0.003f);Check(b.Count==0,"inactive recorder");
b.Begin();b.Record(1L,10,0.003f);b.Record(1L,10,0.001f);b.Record(2L,10,float.NaN);b.Record(1L,11,0.006f);b.Record(1L,12,0.004f);
Check(b.Count==4 && b.Dropped==1,"bounded capacity");
Check(b.Get(1).Frame==10 && b.Get(1).Elapsed==0.001f,"same-frame calls retained exactly");
Check(float.IsNaN(b.Get(2).Elapsed),"invalid timing preserved as evidence");
b.Stop();b.Record(1L,13,1f);Check(b.Count==4 && b.Dropped==1,"stop prevents recording");
b.Begin();Check(b.Count==0 && b.Dropped==0,"new capture clears old data");
b.Record(9L,22,0f);Check(b.Get(0).Actor==9 && b.Get(0).Elapsed==0f,"zero retained");
Console.WriteLine("PASS: 7 timing recorder checks");return 0;

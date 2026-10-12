using System.Reflection;
using System.Text.RegularExpressions;
var t=Assembly.GetExecutingAssembly().GetType("RhythmCastleAP.PlayerMovementTimingPolicy");
if(t==null){Console.WriteLine("FAIL: player timing policy missing");return 1;}
dynamic p=Activator.CreateInstance(t)!;
void Check(bool v,string m){if(!v)throw new Exception(m);}
float applied;
Check(!p.Admit(1L,1,0.00001f,true,out applied),"tiny deferred");
Check(p.Admit(1L,2,0.012f,true,out applied)&&Math.Abs(applied-.01201f)<1e-7,"elapsed carried");
Check(p.Admit(1L,3,0f,false,out applied)&&applied==0,"pause immediate");
p.Admit(1L,4,.001f,true,out applied);
Check(p.Admit(1L,8,.006f,true,out applied)&&applied==.006f,"frame gap clears");
p.Admit(1L,9,.001f,true,out applied);p.Reset();
Check(p.Admit(1L,10,.006f,true,out applied)&&applied==.006f,"scene reset clears");
Check(p.Admit(1L,11,float.NaN,true,out applied)&&float.IsNaN(applied),"invalid untouched");
p.Admit(1L,12,.001f,true,out applied);
Check(p.Admit(2L,12,.006f,true,out applied)&&applied==.006f,"actors isolated");
foreach(var file in args){p.Reset();double input=0,output=0;int admitted=0;float last=0; foreach(Match m in Regex.Matches(File.ReadAllText(file),@"PLAYER_TICK actor=(\S+) frame=(\d+) elapsed=(\S+)")){float dt=float.Parse(m.Groups[3].Value,System.Globalization.CultureInfo.InvariantCulture);input+=dt;last=dt;if(p.Admit(1L,int.Parse(m.Groups[2].Value),dt,true,out applied)){output+=applied;admitted++;Check(applied>=.002f,"admitted minimum");}}Check(input-output>=-1e-5 && input-output<.00201,"capture elapsed conservation");Console.WriteLine($"Replay {Path.GetFileName(file)}: admitted={admitted}, residual={input-output:F8}s");}
Console.WriteLine("PASS: timing boundaries and capture replay");return 0;

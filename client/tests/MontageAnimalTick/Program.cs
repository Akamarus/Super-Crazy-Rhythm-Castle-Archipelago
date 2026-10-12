using RhythmCastleAP;
int checks=0;
void Check(bool ok,string why){if(!ok)throw new Exception(why);checks++;}
var policy=new MontageAnimalTickPolicy();
float delta;
Check(policy.Admit(1,1,10,.005f,true,out delta)&&delta==.005f,"first tick retained");
Check(!policy.Admit(1,1,11,.005f,true,out delta),"same physics step deferred");
Check(policy.Admit(2,1,11,.005f,true,out delta),"actors have independent clocks");
Check(policy.Admit(1,1.02,12,.005f,true,out delta)&&Math.Abs(delta-.01f)<1e-6,"elapsed time retained");
policy.Admit(1,1.02,13,.005f,true,out delta);
Check(policy.Admit(1,1.04,20,.005f,true,out delta)&&delta==.005f,"inactive actor drops stale time");
policy.Admit(1,1.04,21,.005f,true,out delta);
Check(policy.Admit(1,1.04,22,.005f,false,out delta)&&delta==.005f,"pause or room gate passes original");
Check(policy.Admit(1,1.04,23,.005f,true,out delta)&&delta==.005f,"resume does not replay stale time");
Check(policy.Admit(1,1.04,24,0,true,out delta)&&delta==0,"zero delta passes and clears clock");
Check(policy.Admit(1,1.04,25,float.NaN,true,out delta)&&float.IsNaN(delta),"nonfinite input passes through");
policy.Reset();
for(int i=1;i<=32;i++)policy.Admit(i,1,1,.005f,true,out delta);
Check(policy.Admit(33,1,1,.005f,true,out delta)&&policy.ActorCount==32,"actor cache bounded");
Check(policy.Admit(33,1.02,4,.005f,true,out delta)&&policy.ActorCount==32,"stale actor evicted at capacity");
Check(!policy.Admit(33,1.02,5,.005f,true,out delta),"replacement actor is paced");
policy.Reset();Check(policy.ActorCount==0,"boundary clears all actors");
foreach(int fps in new[]{30,50,60,120,144,165,240}){
 var p=new MontageAnimalTickPolicy();float supplied=0,executed=0;int calls=0;
 for(int frame=0;frame<fps*4;frame++){
  float dt=1f/fps;supplied+=dt;
  double tick=Math.Floor(frame*50d/fps)*.02;
  if(p.Admit(1,tick,frame,dt,true,out delta)){executed+=delta;calls++;}
 }
 Check(Math.Abs(supplied-executed)<.025f,$"elapsed time conserved at {fps}FPS");
 Check(calls<=Math.Min(fps*4,200),$"one animal tick per physics interval at {fps}FPS");
}
policy.Reset();policy.Admit(1,2,1,.005f,true,out delta);policy.Admit(1,2,2,.005f,true,out delta);
Check(policy.Admit(1,1,3,.005f,true,out delta)&&delta==.005f,"clock rewind drops old queue");
Console.WriteLine($"{checks} animal timing policy checks passed");
NativeSignatureChecks.Run();

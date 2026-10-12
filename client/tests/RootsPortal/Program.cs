using RhythmCastleAP;
int checks=0; void Check(bool v,string why){checks++;if(!v)throw new Exception(why);}
var renderer=new FakeRenderer(); int binds=0; bool? collected=false;
var policy=new RootsPortalPolicy();
void Tick(bool active=true,double seconds=1)=>policy.Tick(TimeSpan.FromSeconds(seconds),active,()=>collected,()=>{binds++;return renderer;});
Tick();Check(!renderer.Enabled,"incomplete source hides portal");
Tick(seconds:0.1);Check(binds==1,"cached binding avoids repeated lookup");
collected=true;Tick();Check(renderer.Enabled,"completion restores portal");
collected=false;Tick();Check(!renderer.Enabled,"incomplete source remains hidden");
Tick(false,0);Check(renderer.Enabled,"disable or room exit restores immediately");
renderer.Enabled=false;Tick();Tick(false);Check(!renderer.Enabled,"native hidden renderer is never enabled");
renderer.Enabled=true;collected=null;Tick();Check(renderer.Enabled,"unknown source leaves portal native");
collected=false;Tick();policy.Reset();Check(renderer.Enabled,"reset restores changed renderer");
var absent=new RootsPortalPolicy();int misses=0;
for(int i=0;i<100;i++) absent.Tick(TimeSpan.FromSeconds(1),true,()=>false,()=>{misses++;return null;});
Check(misses==30,"missing hierarchy has bounded retries");
absent.Reset();absent.Tick(TimeSpan.FromSeconds(1),true,()=>false,()=>{misses++;return renderer;});Check(misses==31&&!renderer.Enabled,"room reset permits new binding");
Console.WriteLine($"Roots portal: {checks} assertions passed.");
class FakeRenderer:IRootsPortalRenderer {public bool Alive=>true;public bool Enabled{get;set;}=true;}

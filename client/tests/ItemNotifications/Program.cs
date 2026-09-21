using RhythmCastleAP;
void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
NotificationReceipt R(string item, bool own=false) => new(item, "Alex", "Light Humor", own);
var feed = new ItemNotificationFeed();
feed.Connect(1, "seed:team:slot");
feed.Receive(1, 0, new[]{R("Old item")});
Check(feed.History.Length == 1, "initial history must be available to review");
feed.Advance(0);
Check(feed.Visible.Length == 0, "initial history must not spam popups");
feed.Receive(1, 1, new[]{R("Plant Pipes")});
feed.Advance(0);
Check(feed.Visible.Length == 1 && feed.Visible[0].Text.Contains("Received Plant Pipes from Alex"), "new received item must be visible");
feed.Connect(2, "seed:team:slot");
feed.Receive(2, 0, new[]{R("Old item"),R("Plant Pipes"),R("Car Battery")});
Check(feed.History.Length == 3, "reconnect must show only unseen receipts, including offline arrivals");
feed.Receive(1, 3, new[]{R("STALE")});
Check(feed.History.Length == 3, "old connection must not publish");
feed.Receive(2, 5, new[]{R("GAP")});
Check(feed.History.Length == 3, "history gap must not invent receipts");
feed.Receive(2, 3, new[]{R("Own reward",true)});
Check(feed.History[0].Text.Contains("Found Own reward"), "self reward should have one clear found message");
feed.Send(2, "location:123:receiver:2", "Weed Killer", "Sam", "Gecko");
feed.Send(2, "location:123:receiver:2", "Weed Killer", "Sam", "Gecko");
Check(feed.History.Count(x=>x.Text.Contains("Sent Weed Killer to Sam"))==1,"confirmed sends must deduplicate");
feed.Advance(7);
Check(feed.Visible.Length <= 3, "burst must not cover the screen");
feed.Connect(3,"different-seed");
Check(feed.History.Length==0 && feed.Visible.Length==0,"identity switch must clear notifications");
feed.Receive(3,0,Enumerable.Range(0,150).Select(i=>R("Item "+i)).ToArray());
Check(feed.History.Length==100 && feed.History[0].Text.Contains("Item 149"),"bounded history keeps latest receipts");
feed.Receive(3,150,new[]{R("New arrival")});feed.Advance(0);
Check(feed.Visible.Length==1,"new identity establishes its own receipt baseline");
feed.Advance(6.1);Check(feed.Visible.Length==0,"popup must expire without changing history");
Check(feed.History[0].Text.Contains("New arrival"),"expired popup stays in history");
feed.Reset();Check(feed.History.Length==0,"shutdown clears feed");
Console.WriteLine("PASS: item history, live receipts, replay deduplication, offline arrivals, self finds, sends, identity isolation, burst bounds, expiry.");

var burst = new ItemNotificationFeed();burst.Connect(1,"burst");burst.Receive(1,0,Array.Empty<NotificationReceipt>());
burst.Receive(1,0,Enumerable.Range(0,125).Select(i=>R("Reward "+i)).ToArray());
burst.Advance(0);Check(burst.Visible[0].Text.Contains("Reward 0"),"burst queue must retain its earliest unseen reward");
burst.Advance(20,0);Check(burst.Visible[0].Text.Contains("Reward 0"),"hidden popups must not expire");
burst.Advance(6.1,1);Check(burst.Visible[0].Text.Contains("Reward 1"),"only the card that fit on screen should expire");
var displayed = new List<string>{"Received Reward 0 from Alex"};
while(burst.Visible.Length>0){displayed.Add(burst.Visible[0].Text);burst.Advance(6.1,1);}
Check(displayed.SequenceEqual(Enumerable.Range(0,125).Select(i=>"Received Reward "+i+" from Alex")),"every reward in a burst must receive visible time in arrival order");
Check(burst.History.Length==100,"unseen popup retention must not unbound recent history");
AdapterTests.Run();

var sentReplay = new ItemNotificationFeed();
sentReplay.Connect(1, "perfection");
for (int i = 0; i < 316; i++) sentReplay.Send(1, "location:" + i, "Reward " + i, "Sam", "Source " + i);
sentReplay.Connect(2, "perfection");
for (int i = 0; i < 316; i++) sentReplay.Send(2, "location:" + i, "Reward " + i, "Sam", "Source " + i);
Check(sentReplay.OutstandingCount == 316, "replaying a full Perfection seed must not requeue sent rewards after 256 sends");
sentReplay.Connect(3, "different-seed");
sentReplay.Send(3, "location:0", "New reward", "Sam", "Source 0");
Check(sentReplay.OutstandingCount == 1, "send deduplication must reset for another authenticated identity");
Console.WriteLine("PASS: full-seed send replay deduplication and identity reset");
OverlayTests.Run();



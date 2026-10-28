using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using JouchoPet;
class DefenderCheck {
 const string Folder="qa/v0812/";const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
 static int checks;
 static void Check(bool pass,string label){if(!pass)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
 static object Get(object o,string n){return o.GetType().GetField(n,F).GetValue(o);}
 static void Set(object o,string n,object v){o.GetType().GetField(n,F).SetValue(o,v);}
 static void Call(object o,string n,params object[] args){o.GetType().GetMethod(n,F).Invoke(o,args);}
 static string Xml(int severity,string guid,int id=1116,long record=1){return "<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='Microsoft-Windows-Windows Defender'/><EventID>"+id+"</EventID><EventRecordID>"+record+"</EventRecordID><TimeCreated SystemTime='"+DateTime.UtcNow.ToString("o")+"'/><Channel>"+DefenderThreatMonitor.Channel+"</Channel></System><EventData><Data Name='Detection ID'>"+guid+"</Data><Data Name='Severity ID'>"+severity+"</Data><Data Name='Severity Name'>本地化名称</Data><Data Name='Path'>NeverOpenMe.exe</Data></EventData></Event>";}
 static DefenderThreat Threat(int severity,DateTime time){return new DefenderThreat(Guid.NewGuid().ToString(),severity,time);}
 static void Parser(){
  string key=Guid.NewGuid().ToString();
  for(int s=1;s<=4;s++){var threat=DefenderThreatMonitor.Parse(Xml(s,key));Check(threat!=null&&threat.Severity==s&&threat.Facing==(s<=2?-1:1),"native numeric severity selects direction / "+s);}
  Check(DefenderThreatMonitor.Parse(Xml(1,key,1006)).Key==DefenderThreatMonitor.Parse(Xml(1,key,1116,99)).Key,"legacy/current detection IDs deduplicate together");
  foreach(string xml in new[]{Xml(0,key),Xml(5,key),Xml(1,key,1117),Xml(1,key).Replace("Provider Name='Microsoft-Windows-Windows Defender'","Provider Name='Other'"),Xml(1,key).Replace(DefenderThreatMonitor.Channel,"Application"),Xml(1,key).Replace("Name='Severity ID'","Name='Other ID'"),"<broken>","<!DOCTYPE foo [<!ENTITY secret SYSTEM 'file:///never-read'>]><foo>&secret;</foo>"})Check(DefenderThreatMonitor.Parse(xml)==null,"invalid, unrelated or unknown event ignored");
  Check(DefenderThreatMonitor.Parse(Xml(2,"",1116,9))!=null,"missing detection GUID uses record plus timestamp");
 }
 static void Queue(){
  var q=new DefenderArcheryQueue();var now=DateTime.UtcNow;var small=Threat(1,now);var big=Threat(4,now);
  Check(q.Add(small,now)&&!q.Add(small,now)&&q.Count==1,"duplicate notification queued once");q.Add(big,now);
  Check(q.Take(now,false)==null&&q.Count==2,"busy character retains notifications");
  Check(q.Take(now,true)==big,"large threat takes priority");
  Check(q.Take(now.AddSeconds(1),true)==null,"burst does not restart the bow animation");
  Check(q.Take(now.AddSeconds(8),true)==small,"small threat fires later in its own direction");
  Check(!q.Add(Threat(1,now.AddMinutes(-6)),now)&&!q.Add(Threat(1,now.AddMinutes(2)),now),"stale and future timestamps rejected");
  q.Add(Threat(1,now),now);Check(q.Take(now.AddMinutes(6),true)==null,"delayed stale notification expires");
  for(int i=0;i<100;i++)q.Add(Threat(1,now),now);Check(q.Count==32,"burst queue remains bounded");q.Add(big=Threat(3,now),now);Check(q.Take(now.AddSeconds(20),true)==big,"large threat survives full queue");q.Clear();Check(q.Count==0,"disable clears pending shots");
 }
 sealed class Source:IDefenderThreatSource {
  public event Action<DefenderThreat> Detected;internal int Starts,Disposals;public string Status{get{return "Test source";}}
  public void Start(){Starts++;}public void Dispose(){Disposals++;}
  internal Action<DefenderThreat> Capture(){return Detected;}
  internal void Emit(DefenderThreat t){var action=Detected;if(action!=null)action(t);}
 }
 static void Runtime(){
  foreach(var form in new[]{CharacterForm.AnemoneI,CharacterForm.AnemoneII})using(var pet=new Pet(Path.GetFullPath("qa/v0821/runtime/defender-settings.txt")))try{
   Set(pet,"wandering",false);Set(pet,"sleeping",false);Set(pet,"defenderEnabled",true);
   string error;Check(pet.TryChangeForm(form,out error),"runtime form / "+form);
   var source=new Source();pet.ConnectDefender(source);Check(source.Starts==1,"source starts once");var shot=(ArcheryMotion)Get(pet,"archery");
   var small=Threat(1,DateTime.UtcNow);Task.Run(()=>source.Emit(small)).Wait();
   Set(pet,"menuOpen",true);pet.UpdateDefender(DateTime.UtcNow);Check(!shot.Busy,"worker callback never fires through an open menu");
   Set(pet,"menuOpen",false);Set(pet,"dragging",true);pet.UpdateDefender(DateTime.UtcNow);Check(!shot.Busy,"dragging defers shooting");Set(pet,"dragging",false);
   Set(pet,"sleeping",true);pet.UpdateDefender(DateTime.UtcNow);Check(!shot.Busy,"sleeping defers shooting");Set(pet,"sleeping",false);
   Check(pet.Think(),"test starts an existing daily action");pet.UpdateDefender(DateTime.UtcNow);Check(!shot.Busy,"existing daily action is not interrupted");pet.CancelDailyAction();
   Set(pet,"happyUntil",DateTime.MinValue);pet.UpdateDefender(DateTime.UtcNow);Check(shot.Pending&&shot.Facing==-1,"small threat reaches existing left archery request");
   source.Emit(small);Call(pet,"CancelArchery");pet.UpdateDefender(DateTime.UtcNow.AddSeconds(9));Check(!shot.Busy,"same detection does not replay after cancellation");
   source.Emit(Threat(4,DateTime.UtcNow));pet.UpdateDefender(DateTime.UtcNow.AddSeconds(10));Check(shot.Pending&&shot.Facing==1,"large threat reaches existing right archery request");
   var motion=(Motion)Get(pet,"motion");bool fired=false;int height=(int)Get(pet,"petHeight");
   for(int i=0;i<500&&!fired;i++){motion.Update(.02,shot.ApproachDirection(motion,true),height,shot.Pending,false);Call(pet,"UpdateArchery",.02);fired=Get(pet,"magicArrow")!=null;}
   Check(fired&&shot.Facing==1,"automatic request completes turn, draw and projectile release / "+form);
   var stale=source.Capture();Call(pet,"StopDefender");Call(pet,"CancelArchery");var replacement=new Source();pet.ConnectDefender(replacement);stale(Threat(4,DateTime.UtcNow));pet.UpdateDefender(DateTime.UtcNow.AddSeconds(20));Check(!shot.Busy&&source.Disposals==1,"callbacks from disposed source ignored after reconnect");
   Set(pet,"defenderEnabled",false);replacement.Emit(Threat(4,DateTime.UtcNow));pet.UpdateDefender(DateTime.UtcNow.AddSeconds(30));Check(!shot.Busy,"disabled linkage cannot trigger");
   pet.Dispose();Check(replacement.Disposals==1,"disposing pet releases subscription");
  }finally {typeof(Form).GetMethod("OnFormClosed",F).Invoke(pet,new object[]{new FormClosedEventArgs(CloseReason.None)});}
 }
 static object OldFrame(Assembly a,RenderFrame frame){var result=Activator.CreateInstance(a.GetType("JouchoPet.RenderFrame"));foreach(var field in typeof(RenderFrame).GetFields())if(field.FieldType.IsPrimitive||field.FieldType==typeof(string))result.GetType().GetField(field.Name).SetValue(result,field.GetValue(frame));return result;}
 static void Lashes(){
  var a=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));var ft=a.GetType("JouchoPet.CharacterForm");
  using(var picture=new Bitmap(960,600))using(var g=Graphics.FromImage(picture)){
   g.Clear(Color.FromArgb(46,51,61));
   foreach(var form in new[]{CharacterForm.AnemoneI,CharacterForm.AnemoneII}){
    object old=Activator.CreateInstance(a.GetType("JouchoPet.PetRenderer"),F,null,new[]{Enum.ToObject(ft,(int)form)},null);
    try{using(var current=new PetRenderer(form))foreach(int size in new[]{220,300,400}){current.Resize(size);Call(old,"Resize",size);
     foreach(int side in new[]{-1,1})foreach(double closure in new[]{0,.5,1.0}){
      var frame=new RenderFrame{MotionState="idle",Time=.7,Settle=1,Affection=.85,PatSide=side,ClosedEyes=closure};
      var before=(Bitmap)old.GetType().GetMethod("Draw").Invoke(old,new[]{OldFrame(a,frame)});var after=current.Draw(frame);var bp=PoseMesh.Read(before);var ap=PoseMesh.Read(after);int changed=0;
      for(int i=0;i<bp.Length;i++)if(bp[i]!=ap[i]){changed++;uint b=(uint)bp[i],n=(uint)ap[i];if((b>>24)!=(n>>24)||i/after.Width>28+size*.32)throw new Exception("Lash tint escaped face or changed alpha");for(int c=0;c<3;c++)if((n>>(c*8)&255)>(b>>(c*8)&255))throw new Exception("Lash tint lightened pixel");}
      Check(changed==0,"approved lash and head pose unchanged / "+form+" / "+size+" / "+side+" / "+closure);
      if(form==CharacterForm.AnemoneII&&size==300&&closure==1){int column=side<0?0:1;int row=0;foreach(var image in new[]{before,after}){g.DrawString((row==0?"0.8.11":"0.8.12")+(side<0?" / lean left":" / lean right"),SystemFonts.DefaultFont,Brushes.White,column*480+15,row*300+10);g.DrawImage(image,new Rectangle(column*480+40,row*300+35,400,250),new Rectangle(image.Width/2-64,44,128,80),GraphicsUnit.Pixel);row++;}}
     }
    }}finally{((IDisposable)old).Dispose();}
   }picture.Save(Folder+"lash-comparison.png",ImageFormat.Png);
  }
 }
 [STAThread]static void Main(string[] args){try{
  Directory.CreateDirectory(Folder);AnimationParameters.UseFixedValues();
  if(args.Length>0&&args[0]=="runtime"){Directory.CreateDirectory("qa/v0821/runtime");Parser();Queue();Runtime();Console.WriteLine("PASS "+checks+" Defender regression checks; synthetic events only.");return;}
  if(args.Length>0&&args[0]=="native-read"){using(var source=new DefenderThreatMonitor()){int events=0;source.Detected+=t=>Interlocked.Increment(ref events);source.Start();Thread.Sleep(1200);Console.WriteLine(source.Status);Check(source.Status.StartsWith("已连接"),"real Defender future-event subscription enabled");Console.WriteLine("New events during observation: "+events+". No threat files or event records created; no scans or settings changes.");}return;}
  Parser();Queue();Runtime();Lashes();Console.WriteLine("PASS "+checks+" Defender/lash checks; synthetic events and offline frames only.");
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}

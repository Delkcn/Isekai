using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using JouchoPet;
class HitFileCheck {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static int checks;
 sealed class Opener:IFileOpener {
  internal int Calls;internal string Path;internal Action Callback;internal bool Fail;
  public ProgramLaunchResult Open(string path){Calls++;Path=path;if(Callback!=null)Callback();if(Fail)throw new IOException("simulated association failure");return new ProgramLaunchResult(true,"");}
 }
 static void Check(bool value,string text){if(!value)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
 static object Get(Pet p,string name){return typeof(Pet).GetField(name,F).GetValue(p);}
 static void Set(Pet p,string name,object value){typeof(Pet).GetField(name,F).SetValue(p,value);}
 static void Call(Pet p,string name,params object[] args){typeof(Pet).GetMethod(name,F).Invoke(p,args);}
 static void Controller(){
  var op=new Opener();var action=new HitFileOpen(op);PointF point=new PointF(-400,200);PointF? current=point;
  using(var target=new DesktopFileTarget(System.IO.Path.GetFullPath("qa/v0810/含 空格 & 文件.txt"),point,path=>current)){
   Check(action.Hit()==null&&op.Calls==0,"no file target / preview does not open");
   action.Arm(target);Check(op.Calls==0,"selection alone does not open");
   action.Hit();action.Hit();Check(op.Calls==1&&op.Path==target.Path,"one hit opens exact selected path once");
   action.Arm(target);action.Cancel();action.Hit();Check(op.Calls==1,"cancel consumes pending target");
   foreach(var next in new PointF?[]{null,new PointF(-390,200)}){current=next;action.Arm(target);Check(!action.Hit().Success&&op.Calls==1,"missing/moved target cannot open");}
   current=point;op.Fail=true;action.Arm(target);Check(!action.Hit().Success&&action.Hit()==null&&op.Calls==2,"open exception reported once, no retry loop");
   op.Fail=false;op.Callback=()=>action.Hit();action.Arm(target);action.Hit();Check(op.Calls==3,"reentrant opener cannot duplicate activation");
  }
  Check(!new ShellFileOpener().Open("relative-missing.txt").Success,"shell adapter rejects missing/relative file without launching");
 }
 static void Runtime(){
  using(var pet=new Pet())try{
   ((NotifyIcon)Get(pet,"tray")).Visible=false;
   Set(pet,"settings",System.IO.Path.GetFullPath("qa/v0810/test-settings.txt"));Set(pet,"wandering",false);Set(pet,"sleeping",false);
   var op=new Opener();var beam=new HitFileOpen(op);var boltOpen=new HitFileOpen(op);
   Set(pet,"beamFileOpen",beam);Set(pet,"castFileOpen",boltOpen);
   var u=(UtilityMotion)Get(pet,"utility");var m=(Motion)Get(pet,"motion");
   PointF point=new PointF(pet.Left+20,pet.Top+20);PointF? position=point;
   Func<DesktopFileTarget> target=()=>new DesktopFileTarget(System.IO.Path.GetFullPath("qa/v0810/命中文件.txt"),point,path=>position);
   Action beginBeam=()=>{pet.CancelUtilityAction();m.FollowPush(0,0,1,300);Call(pet,"StartUtility",UtilityAction.Beam,point,1);u.Pending=false;var t=target();Set(pet,"beamFile",t);beam.Arm(t);};
   beginBeam();Call(pet,"UpdateUtility",.49);Check(op.Calls==0,"beam does not open during reach");
   Set(pet,"menuOpen",true);Call(pet,"UpdateUtility",.1);Set(pet,"menuOpen",false);Check(op.Calls==0,"paused menu cannot trigger hit");
   Call(pet,"UpdateUtility",double.NaN);Call(pet,"UpdateUtility",double.PositiveInfinity);Check(op.Calls==0&&u.Age==.49,"invalid time cannot trigger hit");
   Call(pet,"UpdateUtility",.02);Call(pet,"UpdateUtility",.5);Call(pet,"UpdateUtility",1.0);Check(op.Calls==1&&!u.Busy,"beam first contact opens once through recovery");
   beginBeam();Call(pet,"UpdateUtility",.49);pet.CancelUtilityAction();Call(pet,"UpdateUtility",2.0);Check(op.Calls==1,"cancelled beam cannot open later");
   beginBeam();position=new PointF(point.X+10,point.Y);Call(pet,"UpdateUtility",.6);Check(op.Calls==1&&!u.Busy,"moved beam target cancels before contact");position=point;
   beginBeam();Call(pet,"UpdateUtility",3.0);Check(op.Calls==2&&!u.Busy,"large timestep crosses beam hit once");
   Call(pet,"StartUtility",UtilityAction.Beam,point,1);u.Pending=false;Call(pet,"UpdateUtility",3.0);Check(op.Calls==2,"coordinate beam preview never opens");
   var casting=(CastingMotion)Get(pet,"casting");int events=0;pet.ProjectileHit+=(s,e)=>events++;
   Func<bool,MagicBolt> beginBolt=withFile=>{
    pet.CancelCast();casting.Id=Guid.NewGuid();var t=withFile?target():null;
    Set(pet,"castFileTarget",t);if(t!=null)boltOpen.Arm(t);
    var bolt=new MagicBolt(casting.Id,new PointF(point.X-300,point.Y),point,300,t==null?null:t.Path);
    Set(pet,"castBolt",bolt);bolt.ProjectileHit+=(s,e)=>Call(pet,"RecordProjectileHit",s,e);return bolt;
   };
   var first=beginBolt(true);first.Update(first.FlightDuration-.001);Check(op.Calls==2,"flying bolt does not open early");
   first.Update(.002);first.Update(3);Check(op.Calls==3&&events==1&&pet.LastProjectileHit.TargetFile==op.Path,"bolt impact opens once and preserves hit event");
   var cancelled=beginBolt(true);pet.CancelCast();cancelled.Update(3);Check(op.Calls==3&&events==1,"cancelled bolt cannot open");
   var removed=beginBolt(true);position=null;removed.Update(3);Check(op.Calls==3&&events==1,"deleted bolt target cannot open or report valid hit");position=point;
   var stale=beginBolt(true);var current=beginBolt(true);Call(pet,"RecordProjectileHit",stale,new ProjectileHitEventArgs(stale.Id,stale.Origin,stale.Target,stale.TargetFile));Check(op.Calls==3,"stale projectile cannot consume newer target");
   Set(pet,"castEffects",false);current.Update(3);Check(op.Calls==4&&events==2,"effects hidden still opens on physical hit");
   beginBolt(false).Update(3);Check(op.Calls==4&&events==3,"coordinate bolt preview reports hit but does not open");
  }finally{
   typeof(Form).GetMethod("OnFormClosed",F).Invoke(pet,new object[]{new FormClosedEventArgs(CloseReason.None)});
  }
 }
 [STAThread]static void Main(){try{Controller();Runtime();Console.WriteLine("PASS "+checks+" file-hit checks; all successful opens used a fake opener; no user files launched.");}catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}

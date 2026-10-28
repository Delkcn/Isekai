using System;using System.Collections.Generic;using System.Diagnostics;using System.Drawing;using System.IO;using System.Linq;using System.Reflection;using System.Threading;using System.Windows.Forms;using JouchoPet;
class TurnRateCheck {
 const string Folder="qa/v0820/";const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;static int checks;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static object OldRenderer(Assembly old,int size,CharacterForm form){var r=Activator.CreateInstance(old.GetType("JouchoPet.PetRenderer"),F,null,new[]{Enum.ToObject(old.GetType("JouchoPet.CharacterForm"),(int)form)},null);r.GetType().GetMethod("Resize").Invoke(r,new object[]{size});return r;}
 static Bitmap DrawOld(object r,RenderFrame f){var a=r.GetType().Assembly;var converted=Activator.CreateInstance(a.GetType("JouchoPet.RenderFrame"));foreach(var field in typeof(RenderFrame).GetFields())if(field.FieldType.IsPrimitive||field.FieldType==typeof(string))converted.GetType().GetField(field.Name).SetValue(converted,field.GetValue(f));return (Bitmap)r.GetType().GetMethod("Draw").Invoke(r,new[]{converted});}
 static void Compare(Assembly old){
  foreach(int size in new[]{220,300,400})foreach(var form in new[]{CharacterForm.AnemoneI,CharacterForm.AnemoneII}){object previous=OldRenderer(old,size,form);try{using(var current=new PetRenderer(form)){current.Resize(size);int n=0;
   foreach(bool light in new[]{false,true})foreach(int direction in new[]{-1,1})for(int i=0;i<=24;i++){double p=i/24.0;var f=new RenderFrame{MotionState="turn",Angle=direction*(2*p-1),LightTurn=light,TurnProgress=p,TurnFrom=-direction,SupportNear=direction<0,Settle=1};if(!PoseMesh.Read(DrawOld(previous,f)).SequenceEqual(PoseMesh.Read(current.Draw(f))))throw new Exception("Turn pixels changed");n++;}
   foreach(double angle in new[]{-1,-.00000011,-.0000001,-.00000009,0,.00000009,.0000001,.00000011,1})foreach(double progress in new[]{0,.4,1}){var f=new RenderFrame{MotionState="turn",Angle=angle,LightTurn=true,TurnProgress=progress,TurnFrom=0,Settle=1};if(!PoseMesh.Read(DrawOld(previous,f)).SequenceEqual(PoseMesh.Read(current.Draw(f))))throw new Exception("Endpoint/front threshold changed");n++;}
   foreach(double closure in new[]{0,.5,1}){var f=new RenderFrame{MotionState="idle",Time=.7,LookX=.1,ClosedEyes=closure,Affection=.8,PatSide=-1};if(!PoseMesh.Read(DrawOld(previous,f)).SequenceEqual(PoseMesh.Read(current.Draw(f))))throw new Exception("Post-turn face changed");n++;}
   Check(true,n+" complete pixel comparisons incl. normal/light/full turn, boundaries and post-turn face / "+form+" / "+size);
  }}finally{((IDisposable)previous).Dispose();}}
  foreach(string file in new[]{"src/Animation/Motion.cs","src/Animation/PivotTurn.cs","src/Rendering/SkeletalTurn.cs","src/Rendering/A2FrontArt.cs","src/Rendering/A2FrontCoatColor.cs","src/Rendering/A2FrontGarment.cs","src/Rendering/TurnRig.cs"})Check(File.ReadAllBytes(file).SequenceEqual(File.ReadAllBytes(Folder+"baseline/"+file)),"approved timing/material/geometry unchanged / "+file);
 }
 static void Pump(double seconds){using(var context=new ApplicationContext())using(var finish=new System.Windows.Forms.Timer()){finish.Interval=(int)(seconds*1000);finish.Tick+=(s,e)=>{finish.Stop();context.ExitThread();};finish.Start();Application.Run(context);}}
 static void Scheduler(){
  using(var owner=new Control()){owner.CreateControl();using(var timer=new FrameClock(owner)){Check(timer.TargetFramesPerSecond==60,"non-turn default target 60");timer.Turning=true;Check(timer.TargetFramesPerSecond==120,"turn target 120");timer.Turning=false;
   int count=0,depth=0,maxDepth=0;bool stall=false;var times=new List<double>();var watch=Stopwatch.StartNew();timer.Tick+=(s,e)=>{depth++;maxDepth=Math.Max(maxDepth,depth);count++;times.Add(watch.Elapsed.TotalMilliseconds);if(stall){stall=false;Thread.Sleep(70);Application.DoEvents();}depth--;};
   timer.Start();timer.Start();Pump(.8);timer.Stop();int stopped=count;Pump(.1);Check(count==stopped,"stop discards queued callbacks");double low=(times.Count-1)*1000/(times.Last()-times.First());Console.WriteLine("Clock only normal observed="+low.ToString("F1")+" fps");
   timer.Turning=true;times.Clear();Console.WriteLine("Owner handle="+owner.IsHandleCreated+" disposed="+owner.IsDisposed);var handle=owner.Handle;timer.Start();Pump(.8);timer.Stop();double high=(times.Count-1)*1000/(times.Last()-times.First());Console.WriteLine("Clock only turn observed="+high.ToString("F1")+" fps");Check(high>low*1.3,"higher target produces denser UI dispatch with no rendering load");
   var restartHandle=owner.Handle;timer.Start();stall=true;Pump(.25);timer.Stop();Check(maxDepth==1,"70ms stall and nested message pump never reenter rendering");
   timer.Turning=false;Check(timer.TargetFramesPerSecond==60,"exit turn returns to 60 without changing action duration");
  }}
  var stats=new FrameStatistics();for(int i=0;i<10;i++)stats.Record(i/120.0,3,true);Check(stats.Report().Contains("Turn cadence: 120.0"),"performance report separates observed turn cadence");
 }
 static void Timing(Assembly old){
  foreach(int direction in new[]{-1,1})foreach(bool updated in new[]{false,true})using(var owner=new Control()){
   owner.CreateControl();object previous=updated?null:OldRenderer(old,300,CharacterForm.AnemoneII);PetRenderer current=updated?new PetRenderer(CharacterForm.AnemoneII):null;object scheduler=updated?(object)new FrameClock(owner):Activator.CreateInstance(old.GetType("JouchoPet.FrameClock"),F,null,new object[]{owner},null);
   try{if(updated){current.Resize(300);((FrameClock)scheduler).Turning=true;}for(int i=0;i<24;i++){var f=new RenderFrame{MotionState="turn",Angle=direction*(.02+i*.04),Settle=1};if(updated)current.Draw(f);else DrawOld(previous,f);}
    var watch=Stopwatch.StartNew();var starts=new List<double>();var costs=new List<double>();EventHandler callback=(s,e)=>{double t=watch.Elapsed.TotalSeconds;var f=new RenderFrame{MotionState="turn",Angle=direction*(.02+.96*((t%1.28)/1.28)),Settle=1};starts.Add(watch.Elapsed.TotalMilliseconds);var cost=Stopwatch.StartNew();if(updated)current.Draw(f);else DrawOld(previous,f);costs.Add(cost.Elapsed.TotalMilliseconds);};
    scheduler.GetType().GetEvent("Tick").AddEventHandler(scheduler,callback);scheduler.GetType().GetMethod("Start").Invoke(scheduler,null);Pump(2.6);scheduler.GetType().GetMethod("Stop").Invoke(scheduler,null);
    double[] gaps=starts.Skip(1).Select((t,i)=>t-starts[i]).OrderBy(v=>v).ToArray();Console.WriteLine("UI clock + actual A2 Draw, 300px, "+(direction<0?"left":"right")+", "+(updated?"0.8.20":"0.8.19")+": frames="+starts.Count+", cadence="+(1000/gaps.Average()).ToString("F1")+" fps, interval p95="+gaps[(int)((gaps.Length-1)*.95)].ToString("F2")+" ms, mean Draw="+costs.Average().ToString("F2")+" ms. No window present / display sync.");
   }finally{((IDisposable)scheduler).Dispose();if(previous!=null)((IDisposable)previous).Dispose();if(current!=null)current.Dispose();}
  }
 }
 [STAThread]static void Main(string[] args){try{var old=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));if(args.Length>0&&args[0]=="timing"){Timing(old);return;}if(args.Length>0&&args[0]=="scheduler"){Scheduler();return;}Compare(old);Scheduler();Console.WriteLine("PASS "+checks+" turn rate checks.");}catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}



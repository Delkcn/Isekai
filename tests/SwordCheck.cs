using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;
using JouchoPet;
class SwordCheck {
 static int count;
 static void Quiet(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Check(bool ok,string text){Quiet(ok,text);count++;Console.WriteLine("PASS "+text);}
 static bool Same(object a,object b){return a is double?Math.Abs((double)a-(double)b)<1e-10:a.Equals(b);}
 static double Distance(PointF a,PointF b){return RigParts.Length(a,b);}
 [STAThread]static void Main(){try{AnimationParameters.UseFixedValues();ArmBaseline();Geometry();Timing();Runtime();Lifecycle();Console.WriteLine("All "+count+" skeletal slash checks passed.");}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void ArmBaseline(){var flags=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;var old=Assembly.LoadFile(Path.GetFullPath("qa/v0706/baseline/IsekaijouchoPet.exe"));var method=old.GetType("JouchoPet.SwordMotion").GetMethod("At",flags);foreach(int facing in new[]{-1,1}){
  for(int k=0;k<=226;k++){double t=k*.005;object prior=method.Invoke(null,new object[]{SwordMotion.PoseTime(t),facing});object current=SwordMotion.At(t,facing);foreach(var field in typeof(SwordPose).GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public))if(field.Name!="Opacity"&&field.Name!="Age")Quiet(Same(field.GetValue(current),prior.GetType().GetField(field.Name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(prior)),"0.7.5 arm/body motion changed / "+field.Name+" / "+t);}
  Check(true,"all non-visibility pose fields exactly match 0.7.5 at 200 Hz / "+facing);
 }}
 static void Geometry(){foreach(int size in new[]{220,300,400}){
  Size c=PetRenderer.CanvasSize(size);
  using(var side=new SideWalk(c.Width,c.Height,size))using(var rig=new SwordRig(c.Width,c.Height,size,side))foreach(int facing in new[]{-1,1}){
   var initial=rig.SkeletonPose(SwordMotion.At(0,facing));PointF last=PointF.Empty;double max=0,minUpper=999,maxUpper=-999;
   for(int i=0;i<=226;i++){
    var p=SwordMotion.At(i*.005,facing);var b=rig.Sample(p);var s=rig.SkeletonPose(p);
    Quiet(Math.Abs(Distance(b.Shoulder,b.Elbow)-b.UpperLength)<.001&&Math.Abs(Distance(b.Elbow,b.Wrist)-b.LowerLength)<.001,"arm bone length changed");
    Quiet(Math.Abs(Distance(b.Wrist,b.Grip)-size*.035)<.001&&Math.Abs(Distance(b.Grip,b.Tip)-size*.805*.8*.75/.96)<.001,"grip or rigid blade detached");
    double palmX=b.Grip.X-b.Wrist.X,palmY=b.Grip.Y-b.Wrist.Y;
    Quiet(facing*(palmX*Math.Sin(b.Angle)-palmY*Math.Cos(b.Angle))>size*.034,"weapon must retain the requested 0.7.5 wrist/palm binding");
    PointF sw=new PointF(647,239),sg=new PointF(993,239);var handPoints=new[]{sw,sg,new PointF(sg.X,sg.Y-100)};
    using(var matrix=PaintedSwordArt.HandFrame(b.Wrist,b.Grip,b.Angle,sw,sg))matrix.TransformPoints(handPoints);
    Quiet(Distance(handPoints[0],b.Wrist)<.001&&Distance(handPoints[1],b.Grip)<.001,"painted wrist or handle anchor detached");
    double hx=handPoints[2].X-handPoints[1].X,hy=handPoints[2].Y-handPoints[1].Y;
    Quiet(hx*Math.Cos(b.Angle)+hy*Math.Sin(b.Angle)>0&&Math.Abs(hx*Math.Sin(b.Angle)-hy*Math.Cos(b.Angle))<.001,"blade must exit thumb side of the fist");
    Quiet(s.ElbowAngle>=-105&&s.ElbowAngle<=.001&&s.WristAngle>=-25.001&&s.WristAngle<=30.001,"joint limit or elbow branch changed");minUpper=Math.Min(minUpper,s.UpperAngle);maxUpper=Math.Max(maxUpper,s.UpperAngle);
    foreach(var leg in new[]{s.Near,s.Far})Quiet(Math.Abs(Distance(leg.Hip,leg.Knee)-size*.125)<.001&&Math.Abs(Distance(leg.Knee,leg.Ankle)-size*.125)<.001,"leg length changed");
    Quiet(s.Near.Ankle==initial.Near.Ankle&&s.Far.Ankle==initial.Far.Ankle&&s.Near.Foot==initial.Near.Foot&&s.Far.Foot==initial.Far.Foot,"planted foot slid");
    if(i>0)max=Math.Max(max,Distance(last,b.Tip));last=b.Tip;
   }
   Check(true,"arm, palm, blade and leg lengths fixed / "+size+" / "+facing);
   Check(true,"painted hand anchors coincide and thumb points along blade at 200 Hz / "+size+" / "+facing);
   Check(maxUpper-minUpper<140,"bounded shoulder arc; no full-circle arm / "+size+" / "+facing);
   Check(true,"elbow and wrist limits plus planted-foot IK / "+size+" / "+facing);
   Check(max<size*.10,"continuous sword tip at 200 Hz / "+size+" / "+facing+" max="+max.ToString("F2"));
   foreach(double t in new[]{.15,.37,.47,.60,.63,.88}){var a=rig.Sample(SwordMotion.At(t-1e-6,facing));var b=rig.Sample(SwordMotion.At(t+1e-6,facing));Quiet(Distance(a.Wrist,b.Wrist)<.02&&Distance(a.Elbow,b.Elbow)<.02&&Distance(a.Tip,b.Tip)<.02,"joint discontinuity at stage boundary");}Check(true,"all stage joins continuous / "+size+" / "+facing);
   using(var neutral=(Bitmap)side.Draw(0,0,0,facing,1).Clone())foreach(double t in new[]{0.0,SwordMotion.Duration}){var frame=rig.Draw(SwordMotion.At(t,facing),false);for(int y=0;y<c.Height;y++)for(int x=0;x<c.Width;x++)Quiet(neutral.GetPixel(x,y)==frame.GetPixel(x+rig.Padding,y+rig.Padding),"standing endpoint changed");}Check(true,"exact standing entry and exit / "+size+" / "+facing);
   foreach(double t in new[]{.25,.35,.68,.76}){var clean=PoseMesh.Read(rig.Draw(SwordMotion.At(t,facing),false));var fx=PoseMesh.Read(rig.Draw(SwordMotion.At(t,facing),true));for(int k=0;k<fx.Length;k++)Quiet(clean[k]==fx[k],"effect outside its stage");}Check(true,"no effects between formation, strike and dispersion / "+size+" / "+facing);
   foreach(double t in new[]{.42,1.0}){var clean=PoseMesh.Read(rig.Draw(SwordMotion.At(t,facing),false));var fx=PoseMesh.Read(rig.Draw(SwordMotion.At(t,facing),true));int stars=0;for(int k=0;k<fx.Length;k++)if(clean[k]!=fx[k])stars++;Check(stars>20,"blue stars form/disperse sword / "+t+" / "+size+" / "+facing);}
   var raised=rig.Sample(SwordMotion.At(.37,facing));var cut=rig.Sample(SwordMotion.At(.63,facing));
   Check(facing*(raised.Grip.X-cut.Grip.X)>size*.03,"restored 0.7.5 grip sweep / "+size+" / "+facing);
   var noEffect=PoseMesh.Read(rig.Draw(SwordMotion.At(.56,facing),false));var effect=PoseMesh.Read(rig.Draw(SwordMotion.At(.56,facing),true));int changed=0;for(int k=0;k<effect.Length;k++)if(effect[k]!=noEffect[k])changed++;Check(changed>20,"short trail visible at real blade path / "+size+" / "+facing);
  }
 }}
 static Motion Ready(int facing){var m=new Motion();m.FollowPush(0,0,facing,300);return m;}
 static void Timing(){foreach(int fps in new[]{30,60,120})foreach(int facing in new[]{-1,1}){
  var action=new SwordMotion();int hits=0;double age=0;PointF target=new PointF(-120,450);Guid expected=Guid.Empty;
  action.HitFrame+=(sender,e)=>{hits++;age=action.Age;Quiet(e.TargetPosition==target&&e.FacingDirection==facing&&e.RequestId==expected,"hit payload differs from request");};
  action.SwordSlash(target,facing);expected=action.RequestId;action.Update(10);Check(hits==0&&action.Pending,"pending action cannot hit / "+fps+" / "+facing);
  Quiet(!action.Begin(Ready(-facing),true),"starts before facing correct");Quiet(!action.Begin(Ready(facing),false),"starts before ready");Quiet(action.Begin(Ready(facing),true),"fails standing entry");
  for(int i=0;i<fps*2;i++)action.Update(1.0/fps);Check(hits==1&&!action.Busy&&age>=SwordMotion.HitAt&&age<=SwordMotion.HitAt+1.0/fps+.000001,"one timed HitFrame and complete / "+fps+" / "+facing);
 }
 var skipped=new SwordMotion();int n=0;skipped.HitFrame+=(s,e)=>n++;skipped.Request(1);skipped.Begin(Ready(1),true);skipped.Update(10);skipped.Update(10);Check(n==1,"large dt neither skips nor duplicates HitFrame");
 skipped.Request(1);skipped.Begin(Ready(1),true);skipped.Update(.2);skipped.Cancel();skipped.Update(10);Check(n==1,"cancelled slash cannot emit hit");
 skipped.Request(1);skipped.Begin(Ready(1),true);skipped.Update(double.NaN);skipped.Update(-1);skipped.Update(0);Check(skipped.Age==0,"invalid and zero elapsed time do not advance");
 }
 sealed class FakeLauncher:IProgramLauncher {internal int Calls;internal bool Fail;public ProgramLaunchResult Launch(string p){Calls++;return new ProgramLaunchResult(!Fail,Fail?"test failure":"");}}
 static void Runtime(){var action=new SwordMotion();var tool=new FakeLauncher();using(var runtime=new SwordAgentRuntime(action,tool)){
  int results=0;bool success=false;runtime.LaunchCompleted+=(s,e)=>{results++;success=e.Success;};
  action.Request(1);action.Begin(Ready(1),true);action.Update(10);Check(tool.Calls==0,"ordinary action sends notification without launching");
  runtime.RequestLaunch("test.exe",new PointF(300,400),1);action.Begin(Ready(1),true);action.Update(.59);Check(tool.Calls==0,"runtime waits for hit frame");action.Update(.02);action.Update(10);Check(tool.Calls==1&&results==1&&success,"explicit runtime request launches exactly once via separate controller");
  tool.Fail=true;runtime.RequestLaunch("test.exe",PointF.Empty,-1);action.Begin(Ready(-1),true);action.Update(10);Check(tool.Calls==2&&results==2&&!success,"controller failure reported independently");
  runtime.RequestLaunch("test.exe",PointF.Empty,1);runtime.CancelPendingLaunch();action.Begin(Ready(1),true);action.Update(10);Check(tool.Calls==2,"cancelled runtime request does not launch");
  runtime.RequestLaunch("test.exe",PointF.Empty,1);action.Request(1);action.Begin(Ready(1),true);action.Update(10);Check(tool.Calls==2,"superseded animation cannot launch stale target");
 }
 Check(!new ProgramLaunchController().Launch("not-an-absolute-program").Success,"real launcher rejects invalid input without starting a process");
 }
 static void Lifecycle(){const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;var pet=(Pet)FormatterServices.GetUninitializedObject(typeof(Pet));GC.SuppressFinalize(pet);var action=new SwordMotion();action.Request(1);action.Begin(Ready(1),true);action.Update(.2);typeof(Pet).GetField("sword",flags).SetValue(pet,action);typeof(Pet).GetField("menuOpen",flags).SetValue(pet,true);typeof(Pet).GetMethod("UpdateSword",flags).Invoke(pet,new object[]{.05});Check(action.Age==.2,"menu freezes action clock");typeof(Pet).GetMethod("CancelSword",flags).Invoke(pet,null);Check(!action.Busy,"cancellation clears action");
  using(var menu=new ContextMenuStrip()){typeof(Pet).GetField("menu",flags).SetValue(pet,menu);typeof(Pet).GetMethod("AddSwordMenu",flags).Invoke(pet,null);Check(((ToolStripMenuItem)menu.Items[0]).DropDownItems.Count==3,"left/right actions and independent effect toggle available");}
 }
}

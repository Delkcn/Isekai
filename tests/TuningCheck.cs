using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Imaging;using System.IO;using System.Windows.Forms;using JouchoPet;
class TuningCheck {
 static int count;static string folder=Path.Combine("qa","v0638");
 static void Check(bool yes,string message){if(!yes)throw new Exception(message);count++;Console.WriteLine("PASS "+message);}
 static void Quiet(bool yes,string message){if(!yes)throw new Exception(message);}
 static Button ButtonNamed(Control parent,string text){foreach(Control c in parent.Controls){var b=c as Button;if(b!=null && b.Text==text)return b;var nested=ButtonNamed(c,text);if(nested!=null)return nested;}return null;}
 static TabControl Tabs(Control parent){foreach(Control c in parent.Controls){var t=c as TabControl;if(t!=null)return t;var nested=Tabs(c);if(nested!=null)return nested;}return null;}
 [STAThread] static void Main(){try {
  Directory.CreateDirectory(folder);AnimationParameters.Reset();var defaults=AnimationParameters.Snapshot();
  Check(defaults.Count==24 && RunGait.SpeedAt300==132 && AnimationParameters.Get("elbowBend")==defaults["elbowBend"],"approved defaults and exposed parameters");
  string userBackup=Path.Combine(folder,"baseline","user-animation-parameters.json");
  var userValues=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string,double>>(File.ReadAllText(userBackup));AnimationParameters.Load(userBackup);
  foreach(var pair in userValues)Quiet(AnimationParameters.Get(pair.Key)==pair.Value,"upgrade changed saved user parameter "+pair.Key);
  Check(AnimationParameters.Matches(userValues),"all 24 latest saved user values load unchanged");AnimationParameters.Reset();
  foreach(var spec in AnimationParameters.Specs)foreach(double invalid in new[]{spec.Min-1,spec.Max+1,double.NaN,double.PositiveInfinity}) {
   bool failed=false;try{AnimationParameters.Set(spec.Key,invalid);}catch(ArgumentOutOfRangeException){failed=true;}
   Quiet(failed && AnimationParameters.Matches(defaults),"invalid input changed live values");
  }
  Check(true,"range and non-finite inputs rejected without mutation");
  var bad=new Dictionary<string,double>(defaults);bad["runSpeed"]=150;bad["turnLift"]=1000;
  bool rejected=false;try{AnimationParameters.Apply(bad);}catch(InvalidDataException){rejected=true;}
  Check(rejected && AnimationParameters.Matches(defaults),"whole import validates before applying any value");
  string path=Path.Combine(folder,"test-settings.json");AnimationParameters.Set("runSpeed",150);AnimationParameters.Save(path);AnimationParameters.Reset();AnimationParameters.Load(path);
  Check(RunGait.SpeedAt300==150,"saved values restore from disk");
  AnimationParameters.Set("kneeBase",1.2);AnimationParameters.Save(path);AnimationParameters.Reset();AnimationParameters.Load(path);
  Check(AnimationParameters.Get("kneeBase")==1.2,"atomic replacement preserves latest values");
  AnimationParameters.Set("elbowBend",45);AnimationParameters.Save(path);AnimationParameters.Reset();AnimationParameters.Load(path);
  Check(AnimationParameters.Get("elbowBend")==45,"elbow setting persists and reloads");
  var legacy=AnimationParameters.Snapshot();legacy.Remove("elbowBend");AnimationParameters.Apply(legacy);
  Check(AnimationParameters.Get("elbowBend")==defaults["elbowBend"] && AnimationParameters.Get("kneeBase")==1.2,"old presets retain existing values and default new elbow setting");
  var prior=AnimationParameters.Snapshot();File.WriteAllText(Path.Combine(folder,"bad.json"),"{broken");rejected=false;
  try{AnimationParameters.Load(Path.Combine(folder,"bad.json"));}catch{rejected=true;}
  Check(rejected && AnimationParameters.Matches(prior),"malformed file leaves current values untouched");
  AnimationParameters.Apply(new Dictionary<string,double>{{"runSpeed",100},{"futureSetting",55}});
  Check(RunGait.SpeedAt300==100 && AnimationParameters.Get("turnLift")==defaults["turnLift"],"missing keys default and unknown future keys ignored");
  AnimationParameters.Reset();
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  string uiPath=Path.Combine(folder,"ui-settings.json");
  using(var window=new AnimationDebugWindow(uiPath)) {
   window.StartPosition=FormStartPosition.Manual;window.Location=new Point(-10000,-10000);window.ShowInTaskbar=false;
   window.Show();Application.DoEvents();window.SetEditor("kneeBase",1.5);
   Check(AnimationParameters.Get("kneeBase")==1.5 && window.Text.Contains("未保存"),"numeric control applies live parameter and dirty indicator");
   foreach(int mode in new[]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16}){window.SelectPreview(mode);for(int i=0;i<10;i++)window.StepPreview();}
   Check(true,"all seventeen preview modes render through the actual renderer");
   window.SelectPreview(0);window.SetEditor("kneeBase",.8);
   Check(!window.Text.Contains("未保存"),"returning to saved values clears dirty indicator");
   window.SetEditor("elbowBend",40);
   Check(AnimationParameters.Get("elbowBend")==40 && window.Text.Contains("未保存"),"elbow editor applies immediately");
   window.SetEditor("elbowBend",0);
   window.SetEditor("runSpeed",150);ButtonNamed(window,"保存参数").PerformClick();
   Check(File.Exists(uiPath) && !window.Text.Contains("未保存"),"save button writes current settings and establishes undo baseline");
   window.SetEditor("runSpeed",160);ButtonNamed(window,"撤销未保存").PerformClick();
   Check(RunGait.SpeedAt300==150,"undo button restores last explicitly saved values");
   ButtonNamed(window,"恢复默认").PerformClick();
   Check(RunGait.SpeedAt300==132 && window.Text.Contains("未保存"),"defaults button is a reversible unsaved edit");
   ButtonNamed(window,"保存参数").PerformClick();
   Tabs(window).SelectedIndex=3;window.SetEditor("waveAmplitude",12);for(int i=0;i<60;i++)window.StepPreview();
   foreach(var size in new[]{new Size(980,720),new Size(884,641)}) {
    window.ClientSize=size;window.PerformLayout();Application.DoEvents();using(var b=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(Path.Combine(folder,"debug-window-"+size.Width+".png"),ImageFormat.Png);Quiet(new FileInfo(Path.Combine(folder,"debug-window-"+size.Width+".png")).Length>30000,"window controls missing from rendered layout");}
   }
  }
  Check(true,"window layouts rendered; resources dispose cleanly");
  AnimationParameters.Reset();WaveParameters();Elbows();Bounds();AnimationParameters.Reset();
  Console.WriteLine("All "+count+" tuning checks passed.");
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}finally{AnimationParameters.Reset();}}
 static void WaveParameters(){foreach(int size in new[]{220,300,400}) {
  Size c=PetRenderer.CanvasSize(size);var sprites=SpriteAssets.LoadFront();try{using(var front=new SoftRig(sprites,c.Width,c.Height,size))using(var rig=new GreetingArmRig(PoseMesh.Read(front.Draw(0,0,0,0,0,0,0,new[]{1.0,0,0})),c.Width,c.Height,size)) {
   PointF e=rig.Elbow;int[] cap=PoseMesh.Read(rig.DiagnosticElbowCap);int opaque=0;
   for(int y=0;y<c.Height;y++)for(int x=0;x<c.Width;x++)if(Math.Pow(x-e.X,2)+Math.Pow(y-e.Y,2)<Math.Pow(size*.010,2)){opaque++;Quiet(((uint)cap[y*c.Width+x]>>24)>240,"elbow connection lacks texture coverage");}
   Check(opaque>10,"rounded elbow connection retains opaque original texture "+size);
   foreach(bool maximum in new[]{false,true}) {
    AnimationParameters.Reset();foreach(var spec in AnimationParameters.Specs)if(spec.Group=="挥手问好")AnimationParameters.Set(spec.Key,maximum?spec.Max:spec.Min);
    var rest=rig.Sample(0,0);double upper=Distance(rest.Shoulder,rest.Elbow),lower=Distance(rest.Elbow,rest.Wrist);
    foreach(double amount in new[]{0.0,.15,.5,.85,1})for(int i=0;i<24;i++) {
     double phase=i*Math.PI/12;var pose=rig.Sample(amount,phase);
     Quiet(pose.UpperAngle>=0 && pose.UpperAngle<=AnimationParameters.Get("waveUpper")+.001,"wave crossed inside resting upper arm");
     Quiet(Math.Abs(Distance(pose.Shoulder,pose.Elbow)-upper)<.001 && Math.Abs(Math.Sqrt(Math.Pow(Distance(pose.Elbow,pose.Wrist),2)+pose.ForearmDepth*pose.ForearmDepth)-lower)<.001,"wave altered spatial bone lengths");
     int[] pixels=rig.Draw(amount,phase);for(int y=0;y<c.Height;y++)for(int x=0;x<c.Width;x++)if(((uint)pixels[y*c.Width+x]>>24)>80)Quiet(x>0 && y>0 && x<c.Width-1 && y<c.Height-1,"wave extreme pose clipped");
    }
    Check(true,"wave parameter bounds and entry/exit geometry "+size+" / "+maximum);
   }
  }}finally{foreach(var b in sprites)b.Dispose();}
 }AnimationParameters.Reset();}
 static double Distance(PointF a,PointF b){return Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));}
 static void Elbows(){foreach(int size in new[]{220,300,400})for(int d=0;d<2;d++) {
  Size c=PetRenderer.CanvasSize(size);using(var body=CharacterArt.Normalized(d==0?"profile-left":"profile-right",c.Width,c.Height,size))using(var arm=new CarryArmRig(d,body,size)) {
   double upper=Distance(arm.Shoulder,arm.Elbow),lower=Distance(arm.Elbow,arm.Wrist);
   foreach(double bend in new[]{0.0,15,35,55,70})foreach(double amount in new[]{0.0,.25,.5,.75,1})for(int i=0;i<24;i++) {
    double phase=i/24.0;AnimationParameters.Set("elbowBend",0);RunArmPose zero=RunArmPose.At(phase);AnimationParameters.Set("elbowBend",bend);RunArmPose pose=RunArmPose.At(phase);
    Quiet(Math.Abs(pose.Lower-zero.Lower+bend)<1e-8 && pose.Upper==zero.Upper && pose.Wrist==zero.Wrist,"elbow control altered swing or inertia");
    PointF s=arm.SkinRunning(arm.Shoulder,phase,amount),e=arm.SkinRunning(arm.Elbow,phase,amount),w=arm.SkinRunning(arm.Wrist,phase,amount);
    Quiet(Distance(s,arm.Shoulder)<.001 && Math.Abs(Distance(s,e)-upper)<.001 && Math.Abs(Distance(e,w)-lower)<.001,"bending changed bone lengths");
    arm.DrawRunning(phase,amount);Quiet(arm.MinimumArea>0,"elbow interpolation folded arm");
   }
   Check(true,"elbow angles and entry/exit preserve bones, phase and mesh "+size+" / "+d);
  }
 }AnimationParameters.Reset();}
 static void Bounds(){
  foreach(bool maximum in new[]{false,true}) {
   foreach(var s in AnimationParameters.Specs)AnimationParameters.Set(s.Key,maximum?s.Max:s.Min);
   foreach(int size in new[]{220,300,400}) {
    Size c=PetRenderer.CanvasSize(size);
    using(var renderer=new PetRenderer()) {
     renderer.Resize(size);
     foreach(int facing in new[]{-1,1})for(int i=0;i<24;i++) {
      Bitmap b=renderer.Draw(new RenderFrame{MotionState="walk",Facing=facing,Speed=RunGait.SpeedAt300,Gait=i/24.0,RunBlend=1});
      var pixels=PoseMesh.Read(b);int n=0;
      for(int y=0;y<c.Height;y++)for(int x=0;x<c.Width;x++)if(((uint)pixels[y*c.Width+x]>>24)>30){n++;Quiet(x>0 && y>0 && x<c.Width-1 && y<c.Height-1,"extreme parameter run clipped");}
      Quiet(n>size*size*.10,"extreme parameter run lost artwork");
     }
    }
    for(int d=0;d<2;d++)using(var body=CharacterArt.Normalized(d==0?"profile-left":"profile-right",c.Width,c.Height,size))using(var arm=new CarryArmRig(d,body,size)) {
     for(int i=0;i<48;i++){arm.DrawRunning(i/48.0,1);Quiet(arm.MinimumArea>0,"extreme parameter arm mesh folded");}
    }
    var sprites=SpriteAssets.LoadFront();try{using(var front=new SoftRig(sprites,c.Width,c.Height,size))using(var side=new SideWalk(c.Width,c.Height,size))using(var turn=new TurnRig(c.Width,c.Height,size)) {
     Bitmap neutral=front.Draw(0,0,0,0,0,0,0,new[]{1.0,0,0});turn.Prepare(neutral,side);
     foreach(int from in new[]{-1,0,1})foreach(int to in new[]{-1,0,1})if(from!=to)for(int i=1;i<24;i++) {
      double p=i/24.0;Bitmap b=turn.DrawLight(from+(to-from)*PivotTurn.Progress(p),neutral,side,0,p,from,to<from);
      Quiet(turn.LastMinimumJacobian>0,"extreme turn mesh folded");
      int[] pixels=PoseMesh.Read(b);for(int y=0;y<c.Height;y++)for(int x=0;x<c.Width;x++)if(((uint)pixels[y*c.Width+x]>>24)>80)Quiet(x>0 && y>0 && x<c.Width-1 && y<c.Height-1,"extreme turn clipped");
     }
    }}finally{foreach(var b in sprites)b.Dispose();}
    Check(true,"parameter range renders without clipping or arm folds "+maximum+" / "+size);
   }
   var m=new Motion{SelectLightTurn=delegate{return true;}};bool done=false;
   for(int i=0;i<600;i++){m.Update(1.0/120,1,300,false,true);if(m.State=="walk" && m.Speed>1){done=true;break;}}
   Check(done,"configured turn duration hands off to run "+maximum);
  }
 }
}

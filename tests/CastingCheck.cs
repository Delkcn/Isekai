using System;using System.IO;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.Reflection;using JouchoPet;
class CastingCheck {
 const string Folder="qa/v0724/";static int checks;
 const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);checks++;}
 [STAThread] static void Main(string[] args){try{
  AnimationParameters.UseFixedValues();Directory.CreateDirectory(Folder);
  string mode=args.Length==0?"all":args[0];
  if(mode=="desktop"){DesktopRead();return;}
  if(mode=="approach"){Approach();return;}
  if(mode=="speed"){SpeedCheck();Approach();Console.WriteLine("Completed "+checks+" focused check groups.");return;}
  if(mode=="journey"){JourneyPreview();return;}
  if(mode=="arms"){Geometry();Recovery();Regression();}
  else if(mode!="preview"){Simulation();Geometry();Targeting();Integration();Approach();Regression();}
  if(mode=="all"||mode=="preview"){Preview();TargetPreview();HeadPreview();}
  Console.WriteLine("Completed "+checks+" focused check groups.");
 }catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void SpeedCheck(){
  bool faster=true,walk=true,feet=true;
  var old=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));var type=old.GetType("JouchoPet.Motion");
  old.GetType("JouchoPet.AnimationParameters").GetMethod("UseFixedValues",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
  foreach(int size in new[]{220,300,400})foreach(int direction in new[]{-1,1})foreach(bool running in new[]{false,true}){
   var m=new Motion();object prior=Activator.CreateInstance(type,true);var update=type.GetMethod("Update");
   for(int i=0;i<1200;i++){m.Update(1.0/120,direction,size,false,running);update.Invoke(prior,new object[]{1.0/120,direction,(double)size,false,running});}
   double speed=(double)type.GetField("Speed").GetValue(prior);
   if(running)faster&=Math.Abs(m.Speed/speed-1.5)<1e-8&&Math.Abs(m.Speed-direction*198.0*size/300)<.001;
   else walk&=Math.Abs(m.Speed-speed)<1e-8&&Math.Abs(m.Speed-direction*36)<.001;
   double travel=198.0*size/300/120,phase=.10;
   var a=RunGait.Sample(phase,size,1);var b=RunGait.Sample(phase+travel*RunGait.Contact(1)/RunGait.Stride(size,1),size,1);
   feet&=a.Planted&&b.Planted&&Math.Abs(travel+b.X-a.X)<1e-6;
  }
  Check(faster,"both directions / three sizes: running speed equals 150 percent of 0.7.23; 198px/s at 300px size");
  Check(walk&&feet,"walking remains 36px/s; run step phase follows travel and planted feet remain grounded");
 }
 static void Simulation(){
  bool hit=true,continuous=true,cancel=true,release=true,brake=true;
  foreach(int fps in new[]{30,60,120})foreach(int direction in new[]{-1,1}){
   var bolt=new MagicBolt(Guid.NewGuid(),new PointF(-850,-150),new PointF(direction*700,60),300);int n=0;ProjectileHitEventArgs record=null;
   bolt.ProjectileHit+=(s,e)=>{n++;record=e;};PointF previous=bolt.Position;double previousDistance=0;
   for(int i=0;i<fps*4;i++){bolt.Update(1.0/fps);double distance=RigParts.Length(bolt.Origin,bolt.Position);continuous&=distance>=previousDistance-.001&&RigParts.Length(previous,bolt.Position)<=300*MagicBolt.Speed/fps+.01;previous=bolt.Position;previousDistance=distance;}
   hit&=n==1&&bolt.Done&&record.RequestId==bolt.Id&&record.TargetPosition==bolt.Target&&bolt.Position==bolt.Target;
   var m=new Motion();m.FollowPush(0,0,direction,300);var action=new CastingMotion();action.Request(new PointF(direction*900,160),direction);release&=action.Begin(m,true,PointF.Empty);int releases=0;
   for(int i=0;i<fps*3;i++)if(action.Update(1.0/fps))releases++;
   release&=releases==1&&!action.Busy;
   action.Request(PointF.Empty,direction);action.Cancel();release&=!action.Begin(m,true,PointF.Empty)&&!action.Update(3);
  }
  foreach(double time in new[]{0,.10,.79}){
   var bolt=new MagicBolt(Guid.NewGuid(),PointF.Empty,new PointF(2000,0),300);int n=0;bolt.ProjectileHit+=(s,e)=>n++;bolt.Update(time);bolt.Cancel();bolt.Update(8);cancel&=n==0&&bolt.Done;
  }
  var large=new MagicBolt(Guid.NewGuid(),PointF.Empty,PointF.Empty,300);int once=0;large.ProjectileHit+=(s,e)=>once++;large.Update(double.NaN);large.Update(double.PositiveInfinity);large.Update(-1);large.Update(10);large.Update(10);hit&=once==1&&large.Done;
  foreach(int start in new[]{-1,0,1})foreach(int target in new[]{-1,1}){
   var m=new Motion();if(start!=0){m.FollowPush(0,100,start,300);m.RunBlend=1;}var action=new CastingMotion();action.Request(new PointF(target*500,100),target);double old=Math.Abs(m.Speed);bool began=false;
   for(int k=0;k<360;k++){
    if(action.Begin(m,true,PointF.Empty)){began=true;break;}
    m.Update(1.0/60,action.ApproachDirection(m,true),300,true,false);
    brake&=Math.Abs(m.Speed)<=old+.001;old=Math.Abs(m.Speed);
    if(action.Begin(m,true,PointF.Empty)){began=true;break;}
   }brake&=began&&m.Facing==target&&m.Speed==0;
  }
  Check(hit,"single ProjectileHit at 30/60/120fps, negative desktop coordinates, zero distance and large dt");
  Check(continuous,"flight position is continuous, monotone and bounded by speed; exact endpoint");
  Check(cancel&&release,"cancellation suppresses hit; one release; invalid time deltas ignored");
  Check(brake,"walking/running/idle enter through existing brake and turn without accelerating");
 }
 static void Geometry(){bool lengths=true,feet=true,anchors=true,continuous=true,ends=true,pushes=true;double jump=0,minPush=10,minBend=10;
  foreach(int size in new[]{220,300,400}){Size c=PetRenderer.CanvasSize(size);using(var side=new SideWalk(c.Width,c.Height,size))using(var rig=new CastingRig(c.Width,c.Height,size,side))foreach(int facing in new[]{-1,1})foreach(double aim in new[]{-15.0,0,15}){
   var rest=rig.Sample(CastingMotion.At(0,facing,aim));var prev=rest;double u=RigParts.Length(rest.Shoulder,rest.Elbow),l=RigParts.Length(rest.Elbow,rest.Wrist);
   for(int i=0;i<=(int)Math.Ceiling(CastingMotion.Duration/.005);i++){var p=CastingMotion.At(i*.005,facing,aim);var b=rig.Sample(p);PointF core=rig.Core(p);
    lengths&=Math.Abs(RigParts.Length(b.Shoulder,b.Elbow)-u)<.001&&Math.Abs(RigParts.Length(b.Elbow,b.Wrist)-l)<.001;
    feet&=b.Near.Foot==rest.Near.Foot&&b.Far.Foot==rest.Far.Foot&&b.Near.Ankle==rest.Near.Ankle&&b.Far.Ankle==rest.Far.Ankle;
    var left=rig.SampleArm(p,0);var right=rig.SampleArm(p,1);var center=RigParts.Mix(left.Grip,right.Grip,.5);
    anchors&=Math.Abs(RigParts.Length(core,center)-size*.0628)<.001&&(p.Core<=0||(core.X-center.X)*facing>=-.001);
    foreach(var arm in new[]{left,right}){lengths&=Math.Abs(RigParts.Length(arm.Shoulder,arm.Elbow)-arm.UpperLength)<.001&&Math.Abs(RigParts.Length(arm.Elbow,arm.Wrist)-arm.LowerLength)<.001;if(p.Body.Amount>.99){minBend=Math.Min(minBend,Math.Abs(arm.Bend));lengths&=arm.Bend*facing<=.0001;}}
    jump=Math.Max(jump,RigParts.Length(prev.Wrist,b.Wrist)/size);continuous&=RigParts.Length(prev.Wrist,b.Wrist)<size*.021;prev=b;
   }
   ends&=RigParts.Length(prev.Wrist,rest.Wrist)<.001&&RigParts.Length(prev.Shoulder,rest.Shoulder)<.001;
   for(int hand=0;hand<2;hand++){var before=rig.SampleArm(CastingMotion.At(CastingMotion.PushStart,facing,aim),hand);var after=rig.SampleArm(CastingMotion.At(CastingMotion.PushEnd,facing,aim),hand);minPush=Math.Min(minPush,(after.Wrist.X-before.Wrist.X)*facing/size);pushes&=(after.Wrist.X-before.Wrist.X)*facing>size*.022;}
   foreach(double t in new[]{0,.15,.4,.75,1.10,1.24,1.45,1.63})rig.Draw(CastingMotion.At(t,facing,aim),false);
  }}
  Console.WriteLine("Geometry: lengths="+lengths+" feet="+feet+" pushes="+pushes+" minPush="+minPush+" minBend="+(minBend*180/Math.PI));
  Console.WriteLine("Maximum wrist motion / 5ms = "+jump.ToString("F5")+" body heights");
  Check(lengths&&feet&&pushes,"both physical arms: fixed bone lengths, no elbow inversion, outward push and planted feet in both directions / three sizes / three elevations");
  Check(anchors&&continuous&&ends,"palm/core attachment, joint continuity and exact rest endpoints; no folded skin");
 }
 static void Recovery(){
  bool straight=true,relax=true,smooth=true;double worstEnd=0,maxStep=0;
  foreach(int size in new[]{220,300,400}){Size c=PetRenderer.CanvasSize(size);using(var side=new SideWalk(c.Width,c.Height,size))using(var rig=new CastingRig(c.Width,c.Height,size,side))foreach(int facing in new[]{-1,1})foreach(double aim in new[]{-15.0,0,15})for(int hand=0;hand<2;hand++){
   var end=rig.SampleArm(CastingMotion.At(CastingMotion.PushEnd,facing,aim),hand);double endAngle=Math.Abs(end.Bend)*180/Math.PI;worstEnd=Math.Max(worstEnd,endAngle);
   straight&=endAngle<.01&&Math.Abs(RigParts.Length(end.Shoulder,end.Wrist)-end.UpperLength-end.LowerLength)<.001;
   var middle=rig.SampleArm(CastingMotion.At(CastingMotion.PushEnd+CastingMotion.RecoverDuration*.5,facing,aim),hand);relax&=middle.Bend*facing< -30*Math.PI/180;
   var before=end;
   for(int i=1;i<=200;i++){var arm=rig.SampleArm(CastingMotion.At(CastingMotion.PushEnd+CastingMotion.RecoverDuration*i/200,facing,aim),hand);
    double step=Math.Abs(arm.Bend-before.Bend)*180/Math.PI;maxStep=Math.Max(maxStep,step);
    smooth&=step<2&&RigParts.Length(before.Wrist,arm.Wrist)<size*.012&&arm.Wrist.Y>=before.Wrist.Y-.015*size;
    before=arm;
   }
   var rest=rig.SampleArm(CastingMotion.At(0,facing,aim),hand);relax&=RigParts.Length(before.Wrist,rest.Wrist)<.001&&Math.Abs(before.Bend-rest.Bend)<1e-9;
  }}
  Console.WriteLine("Push-end elbow residual="+worstEnd.ToString("F6")+"deg; maximum recovery bend step / 5ms="+maxStep.ToString("F3")+"deg");
  Check(straight&&relax&&smooth,"both arms fully straight at push end, smoothly flex while lowering, then exactly return to idle across sizes and facings");
 }
 static void Revision(){
  bool timing=Math.Abs((.30/.65)/CastingMotion.LiftDuration-.70)<1e-9&&Math.Abs((.35/.50)/CastingMotion.RecoverDuration-.70)<1e-9&&Math.Abs(.70/CastingMotion.ChargeDuration-.70)<1e-9;
  bool trajectory=true,clear=true,wrist=true,particle=true;double maxY=0,minBack=100,minClear=100,maxWrist=0;
  foreach(int size in new[]{220,300,400}){Size c=PetRenderer.CanvasSize(size);using(var side=new SideWalk(c.Width,c.Height,size))using(var rig=new CastingRig(c.Width,c.Height,size,side))foreach(int facing in new[]{-1,1})foreach(double aim in new[]{-15.0,0,15}){
   for(int hand=0;hand<2;hand++){
    var raised=rig.SampleArm(CastingMotion.At(CastingMotion.ChargeStart,facing,aim),hand);var charged=rig.SampleArm(CastingMotion.At(CastingMotion.PushStart,facing,aim),hand);
    var extended=rig.SampleArm(CastingMotion.At(CastingMotion.PushEnd,facing,aim),hand);var released=rig.SampleArm(CastingMotion.At(CastingMotion.ReleaseAt,facing,aim),hand);trajectory&=Math.Abs(extended.Bend)*180/Math.PI<.01&&Math.Abs(released.Bend)*180/Math.PI<18&&(extended.Wrist.X-charged.Wrist.X)*facing>size*.08;
    double back=(raised.Grip.X-charged.Grip.X)*facing;minBack=Math.Min(minBack,back/size);trajectory&=back>size*.05;
    for(int i=0;i<=60;i++){
     double t=CastingMotion.PushStart+(CastingMotion.PushEnd-CastingMotion.PushStart)*i/60;var arm=rig.SampleArm(CastingMotion.At(t,facing,aim),hand);
     maxY=Math.Max(maxY,Math.Abs(arm.Grip.Y-charged.Grip.Y));trajectory&=Math.Abs(arm.Grip.Y-charged.Grip.Y)<.001&&Math.Abs(arm.Angle-charged.Angle)<1e-8;
     double lower=Math.Atan2(arm.Wrist.Y-arm.Elbow.Y,arm.Wrist.X-arm.Elbow.X);double delta=arm.Angle-lower;while(delta>Math.PI)delta-=2*Math.PI;while(delta<-Math.PI)delta+=2*Math.PI;
     maxWrist=Math.Max(maxWrist,Math.Abs(delta)*180/Math.PI);wrist&=Math.Abs(delta)<45*Math.PI/180;
    }
   }
   PointF startCore=rig.Core(CastingMotion.At(CastingMotion.ChargeStart,facing,aim)),endCore=rig.Core(CastingMotion.At(CastingMotion.PushStart,facing,aim));trajectory&=(startCore.X-endCore.X)*facing>size*.05;
   for(double t=CastingMotion.ChargeStart;t<CastingMotion.ReleaseAt;t+=.01){var p=CastingMotion.At(t,facing,aim);PointF core=rig.Core(p);
    for(int hand=0;hand<2;hand++){var arm=rig.SampleArm(p,hand);var tip=RigParts.Add(arm.Wrist,Math.Cos(arm.Angle)*size*.05734,Math.Sin(arm.Angle)*size*.05734);double gap=RigParts.Length(core,tip)-size*MagicBoltArt.Radius*(.16+.84*p.Core);minClear=Math.Min(minClear,gap);clear&=gap>0;}
   }
  }}
  for(int i=0;i<MagicBoltArt.ParticleCount;i++){double previous=100;for(int n=0;n<=20;n++){double q=n/20.0;double distance=RigParts.Length(PointF.Empty,MagicBoltArt.ParticlePoint(PointF.Empty,300,i,q));particle&=distance<=previous+.001;previous=distance;}particle&=previous<.001;}
  var bolt=new MagicBolt(Guid.NewGuid(),PointF.Empty,new PointF(660,0),300);bool speed=Math.Abs(bolt.FlightDuration-(660.0/(300*5.5))/.70)<1e-9;
  Console.WriteLine("Horizontal push Y error="+maxY.ToString("F5")+"px; retraction="+minBack.ToString("F4")+" body heights; fingertip clearance="+minClear.ToString("F2")+"px; wrist deflection="+maxWrist.ToString("F1")+"deg");
  Check(timing&&speed,"0.7.19 lift / charge / recovery timing and projectile speed preserved");
  Check(trajectory&&clear&&wrist&&particle,"larger retraction / push, full extension at push end; core and inward particles follow hands; horizontal palms and clear fingertips");
  bool orb=true,bounds=true;using(var image=new Bitmap(200,200)){using(var g=Graphics.FromImage(image)){g.SmoothingMode=SmoothingMode.AntiAlias;MagicBoltArt.Core(g,new PointF(100,100),300,1);}
   Color center=image.GetPixel(100,100);orb=center.A>245&&center.R>245&&center.G>245&&center.B>225;
   for(int r=1;r<34;r++){Color a=image.GetPixel(100+r,100),b=image.GetPixel(100-r,100);orb&=Math.Abs(a.A-b.A)<22&&a.R>=a.G&&a.G>=a.B;}
   image.Save(Folder+"orb-transparent.png",ImageFormat.Png);
  }
  foreach(double age in new[]{.01,.2,bolt.FlightDuration,bolt.FlightDuration+.10}){
   var b=new MagicBolt(Guid.NewGuid(),new PointF(-500,-100),new PointF(-200,-100),300);b.Update(Math.Min(age,b.FlightDuration+.15));Rectangle box=MagicBoltArt.Bounds(b);using(var image=new Bitmap(box.Width,box.Height)){using(var g=Graphics.FromImage(image)){g.TranslateTransform(-box.Left,-box.Top);MagicBoltArt.Draw(g,b);}for(int x=0;x<image.Width;x++)bounds&=image.GetPixel(x,0).A==0&&image.GetPixel(x,image.Height-1).A==0;for(int y=0;y<image.Height;y++)bounds&=image.GetPixel(0,y).A==0&&image.GetPixel(image.Width-1,y).A==0;}
  }
  Console.WriteLine("Light check: emissive="+orb+" bounds="+bounds);
  Check(orb&&bounds&&MagicBoltArt.Radius==.036*1.30&&MagicBoltArt.Brightness==1.5&&MagicBoltArt.TrailWidth==2&&MagicBoltArt.TrailBrightness==2,"emissive unshaded center, symmetric gold glow, 130% radius / 150% light / 200% trail and unclipped effect bounds");
 }
 static void ImpactReuse(){
  var old=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));var arrowType=old.GetType("JouchoPet.MagicArrow");var impact=old.GetType("JouchoPet.ArcheryArt").GetMethod("Impact",BindingFlags.Static|BindingFlags.NonPublic);
  bool same=true;using(var sheet=new Bitmap(1200,800))using(var sg=Graphics.FromImage(sheet)){
   sg.Clear(Color.FromArgb(35,43,56));int cell=0;
   foreach(double age in new[]{.05,.15,.25,.40,.60,.78}){
    using(var expected=new Bitmap(400,400))using(var actual=new Bitmap(400,400)){
     object arrow=Activator.CreateInstance(arrowType,Flags,null,new object[]{PointF.Empty,1,Rectangle.FromLTRB(-1,-1,0,0),300*.70},null);arrowType.GetMethod("Update",Flags).Invoke(arrow,new object[]{age});
     using(var g=Graphics.FromImage(expected)){g.SmoothingMode=SmoothingMode.AntiAlias;g.TranslateTransform(200,200);impact.Invoke(null,new[]{g,arrow});}
     using(var g=Graphics.FromImage(actual)){g.SmoothingMode=SmoothingMode.AntiAlias;MagicBoltArt.Impact(g,new PointF(200,200),300,age);}
     int[] a=PoseMesh.Read(expected),b=PoseMesh.Read(actual);for(int i=0;i<a.Length;i++)same&=a[i]==b[i];
     sg.DrawImageUnscaled(actual,cell%3*400,cell/3*400);sg.DrawString("Hit + "+age.ToString("F2")+"s / arrow radius x 70%",SystemFonts.DefaultFont,Brushes.White,cell%3*400+10,cell/3*400+12);cell++;
    }
   }sheet.Save(Folder+"impact-review.png",ImageFormat.Png);
  }
  Check(same&&MagicBolt.ImpactDuration==MagicArrow.ImpactDuration,"impact pixels match accepted arrow renderer at 70% size: golden stars and three 0.1s-spaced rings, full 0.85s lifetime");
 }
 static void FileTargets(){
  PointF? resolved=new PointF(-500,140);int reads=0;
  using(var target=new DesktopFileTarget("test-only.txt",resolved.Value,path=>{reads++;return resolved;})){
   bool valid=target.Valid();resolved=new PointF(-490,140);bool moved=!target.Valid();resolved=null;bool removed=!target.Valid();
   Check(valid&&moved&&removed&&reads==3,"read-only file identity lock validates position and cancels moved or disappeared files");
  }
  var old=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));var at=old.GetType("JouchoPet.CastingMotion").GetMethod("At",BindingFlags.Static|BindingFlags.NonPublic);bool half=true;
  for(int i=0;i<=20;i++){double u=i/20.0;double priorStart=(double)old.GetType("JouchoPet.CastingMotion").GetField("ChargeStart",BindingFlags.Static|BindingFlags.NonPublic).GetRawConstantValue();object prior=at.Invoke(null,new object[]{priorStart+(double)old.GetType("JouchoPet.CastingMotion").GetField("ChargeDuration",BindingFlags.Static|BindingFlags.NonPublic).GetRawConstantValue()*u,1,0.0});double q=(double)prior.GetType().GetField("Core",Flags).GetValue(prior);half&=Math.Abs(CastingMotion.At(CastingMotion.ChargeStart+CastingMotion.ChargeDuration*u,1).Core-q)<1e-9;}
  Check(half,"formation easing and duration preserved from 0.7.19");
  int events=0;string pathHit=null;var bolt=new MagicBolt(Guid.NewGuid(),new PointF(-500,40),new PointF(-100,40),300,"chosen-file.txt");bolt.ProjectileHit+=(s,e)=>{events++;pathHit=e.TargetFile;};bolt.Update(1);bolt.Update(1);
  Check(events==1&&pathHit=="chosen-file.txt","single hit preserves the chosen file identity without executing any file operation");
  using(var picker=new CastFilePicker(()=>new[]{@"C:\Preview\demo.txt",@"C:\Preview\app.lnk"})){
   picker.Show();System.Windows.Forms.Application.DoEvents();var list=(System.Windows.Forms.ListView)typeof(CastFilePicker).GetField("files",Flags).GetValue(picker);var button=(System.Windows.Forms.Button)typeof(CastFilePicker).GetField("accept",Flags).GetValue(picker);
   bool explicitChoice=picker.SelectedPath==null&&!button.Enabled;list.Items[0].Selected=true;list.Select();System.Windows.Forms.Application.DoEvents();explicitChoice&=button.Enabled&&button.Parent.ClientRectangle.Contains(button.Bounds);
   using(var image=new Bitmap(picker.Width,picker.Height)){picker.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Folder+"file-picker.png",ImageFormat.Png);}
   string choice=(string)list.Items[0].Tag;button.PerformClick();explicitChoice&=picker.SelectedPath==choice&&picker.DialogResult==System.Windows.Forms.DialogResult.OK;
   Check(explicitChoice,"file picker requires an explicit choice and returns the selected path; UI rendered using synthetic filenames only");
  }
  var art=old.GetType("JouchoPet.MagicBoltArt").GetMethod("Core",BindingFlags.Static|BindingFlags.NonPublic);
  using(var prior=new Bitmap(160,160))using(var current=new Bitmap(160,160)){
   using(var g=Graphics.FromImage(prior)){g.SmoothingMode=SmoothingMode.AntiAlias;art.Invoke(null,new object[]{g,new PointF(80,80),300.0,1.0,1.0});}
   using(var g=Graphics.FromImage(current)){g.SmoothingMode=SmoothingMode.AntiAlias;MagicBoltArt.Core(g,new PointF(80,80),300,1);}
   double oldLight=0,newLight=0;int oldArea=0,newArea=0;for(int y=0;y<160;y++)for(int x=0;x<160;x++){Color a=prior.GetPixel(x,y),b=current.GetPixel(x,y);oldLight+=a.A*(a.R+a.G+a.B);newLight+=b.A*(b.R+b.G+b.B);if(a.A>8)oldArea++;if(b.A>8)newArea++;}
   Check(newLight>=oldLight&&newArea==oldArea,"native rendered pale-gold core retains glow footprint and integrated light versus 0.7.18");
  }
 }
 [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool SetProcessDPIAware();
 static void DesktopRead(){
  SetProcessDPIAware();Console.WriteLine("Virtual desktop="+System.Windows.Forms.SystemInformation.VirtualScreen);
  bool valid=true;int files=0;using(var shell=new DesktopIcons())foreach(var item in shell.Read())if(File.Exists(item.Id)){
   files++;if(files==1){Rectangle bounds=shell.IconScreenRectangle(item);Point old=shell.ScreenPoint(item.Position);PointF estimated=new PointF(old.X+shell.IconSize*.5f,old.Y+shell.IconSize*.5f);
    using(var target=DesktopFileTarget.Open(item.Id)){valid&=target.Valid()&&target.Position==DesktopFileTarget.Center(bounds);Console.WriteLine("Icon rectangle="+bounds+"; old estimated center="+estimated+"; native center="+target.Position+"; delta="+(target.Position.X-estimated.X)+","+(target.Position.Y-estimated.Y));}
   }
  }
  Check(valid&&files>0,"live desktop native icon rectangle center validated without selection, move, cast or file modification");
 }
 static void Targeting(){
  bool centers=true;foreach(var r in new[]{new Rectangle(-1400,-220,48,48),new Rectangle(100,120,72,64),new Rectangle(13,27,97,83)}){PointF center=DesktopFileTarget.Center(r);centers&=center.X==(r.Left+r.Right)*.5f&&center.Y==(r.Top+r.Bottom)*.5f;}
  Check(centers,"target uses actual icon rectangle center including negative coordinates and non-square / odd-sized bounds");
  bool align=true,head=true,skin=true;double maxCross=0;
  foreach(int size in new[]{220,300,400}){Size c=PetRenderer.CanvasSize(size);using(var side=new SideWalk(c.Width,c.Height,size))using(var rig=new CastingRig(c.Width,c.Height,size,side))foreach(int direction in new[]{-1,1})foreach(double height in new[]{-.9,0,.9}){
   PointF shoulder=rig.Sample(CastingMotion.At(0,direction)).Shoulder,target=RigParts.Add(shoulder,direction*size*1.3,size*height);double aim;align&=rig.ResolveAim(target,direction,out aim);
   var start=rig.PoseForTarget(CastingMotion.PushStart,direction,target);var end=rig.PoseForTarget(CastingMotion.PushEnd,direction,target);var release=rig.PoseForTarget(CastingMotion.ReleaseAt,direction,target);PointF origin=rig.Core(release);
   PointF ray=new PointF(target.X-origin.X,target.Y-origin.Y);double length=RigParts.Length(origin,target);
   for(int hand=0;hand<2;hand++){
    var a=rig.SampleArm(start,hand);var b=rig.SampleArm(end,hand);double dx=b.Grip.X-a.Grip.X,dy=b.Grip.Y-a.Grip.Y;
    double error=Math.Abs(dx*ray.Y-dy*ray.X)/length;maxCross=Math.Max(maxCross,error);align&=error<.001&&(dx*ray.X+dy*ray.Y)>0&&Math.Abs(b.Bend)<.001;
   }
   double look=rig.LookAim(target,direction);head&=end.HeadTilt*direction*look>=0&&Math.Abs(end.HeadTilt)<=(CastingMotion.MaxHeadTilt+.001)*Math.PI/180;
   foreach(double t in new[]{0,CastingMotion.ChargeStart,CastingMotion.PushStart,CastingMotion.PushEnd,CastingMotion.PushEnd+.5,CastingMotion.Duration}){var p=rig.PoseForTarget(t,direction,target);rig.Draw(p,false);skin&=!double.IsNaN(p.HeadTilt);}
   var bolt=new MagicBolt(Guid.NewGuid(),origin,target,size);bolt.Update(bolt.FlightDuration);
   
   align&=bolt.Position==target;
  }}
  Console.WriteLine("Maximum push-to-target perpendicular error="+maxCross.ToString("F6")+"px");
  Console.WriteLine("Target alignment="+align+" head="+head+" skin="+skin);
  Check(align&&head&&skin,"both palm push paths and projectile align to real target at three elevations / three sizes; head tracks target; full extension and skin remain valid");
 }
 static void JourneyPreview(){
  var p=new Pet();int hits=0;var rows=new System.Text.StringBuilder();
  try{
   Set(p,"settings",Path.GetFullPath(Folder+"journey-settings-test.txt"));Set(p,"wandering",false);Set(p,"petHeight",300);Call(p,"ResizePet");
   var m=(Motion)Field(p,"motion");m.SelectLightTurn=()=>false;var cast=(CastingMotion)Field(p,"casting");var renderer=(PetRenderer)Field(p,"renderer");
   p.ProjectileHit+=(s,e)=>hits++;var area=System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
   foreach(int height in new[]{-1,1}){
    p.CancelCast();p.Location=new Point(area.Left+(area.Width-p.Width)/2,area.Top+area.Height/2-150);m.FollowPush(0,0,height,300);
    int startX=p.Left,top=p.Top,before=hits;PointF target=new PointF(p.Left+p.Width*.5f,p.Top+120+height*300);p.Cast(target);
    string name=height<0?"above":"below";var captures=new System.Collections.Generic.HashSet<string>();
    using(var atlas=new Bitmap(600*6,540*30,PixelFormat.Format32bppPArgb))using(var ag=Graphics.FromImage(atlas)){
     for(int i=0;i<180;i++){
      if(i>0)for(int sub=0;sub<4;sub++)ApproachStep(p,1.0/60);
      var bolt=(MagicBolt)Field(p,"castBolt");var approach=(CastingApproach)Field(p,"castApproach");
      string phase=cast.Active?(cast.Age<CastingMotion.ChargeStart?"lift":cast.Age<CastingMotion.PushStart?"charge":cast.Age<CastingMotion.PushEnd?"push":"recover"):
       cast.Pending?(approach.Moving?(approach.Braking?"brake":m.State=="turn"?"turn to run":"run"):"face target"):"idle";
      if(bolt!=null&&bolt.Hit)phase="hit";
      using(var image=new Bitmap(600,540,PixelFormat.Format32bppPArgb))using(var g=Graphics.FromImage(image)){
       g.Clear(Color.FromArgb(35,43,56));var state=g.Save();g.ScaleTransform(.64f,.64f);g.TranslateTransform(-startX+350,-top+300);
       if(cast.Active){var body=renderer.DrawCast(cast.Pose,true);g.DrawImageUnscaled(body,p.Left-renderer.CastPadding,p.Top-renderer.CastPadding);}
       else {var body=renderer.Draw(new RenderFrame{Time=i/15.0,Gait=m.Phase,Speed=m.Speed,MotionState=m.State,Facing=m.Facing,Angle=m.Angle,Settle=m.Settle,RunBlend=m.RunBlend,LightTurn=m.LightTurn,TurnProgress=m.TurnProgress,TurnFrom=m.TurnFrom,SupportNear=m.SupportNear});g.DrawImageUnscaled(body,p.Left,p.Top);}
       g.FillRectangle(Brushes.LightSteelBlue,target.X-12,target.Y-16,24,32);g.DrawString("demo.txt",SystemFonts.DefaultFont,Brushes.White,target.X-25,target.Y+20);
       if(bolt!=null)MagicBoltArt.Draw(g,bolt);g.Restore(state);
       g.DrawString(name+" / "+(i/15.0).ToString("F2")+"s / "+phase+" / hits="+(hits-before),SystemFonts.DefaultFont,Brushes.White,10,12);
       ag.DrawImageUnscaled(image,i%6*600,i/6*540);
       if((phase=="run"&&m.RunBlend>.8)||phase=="face target"||phase=="charge"||phase=="push"||phase=="hit")if(captures.Add(phase))image.Save(Folder+"journey-"+name+"-"+phase.Replace(' ','-')+".png",ImageFormat.Png);
      }
      rows.AppendLine(name+","+i+","+(i/15.0).ToString("F3")+","+phase+","+p.Left+","+m.Speed.ToString("F2")+","+m.RunBlend.ToString("F3")+","+(hits-before));
     }
     atlas.Save(Folder+"journey-"+name+".png",ImageFormat.Png);
    }
    Check(hits==before+1&&!Prop(p,"CastBusy"),"native journey preview "+name+": one request, run, stop, cast and one hit");
   }
  }finally{CloseTestPet(p);}
  File.WriteAllText(Folder+"JourneyFrames.csv","case,frame,time,phase,x,speed,runBlend,hits\n"+rows);
 }
 static bool Prop(Pet p,string name){return (bool)typeof(Pet).GetProperty(name,Flags).GetValue(p,null);}
 static void ApproachStep(Pet p,double dt){
  var m=(Motion)Field(p,"motion");var cast=(CastingMotion)Field(p,"casting");
  Call(p,"BeginCastIfReady");
  bool menu=(bool)Field(p,"menuOpen");
  int desired=(int)typeof(Pet).GetMethod("CastDesired",Flags).Invoke(p,new object[]{!menu});
  double travel=cast.Active||menu?0:m.Update(dt,desired,300,Prop(p,"CastBusy"),Prop(p,"CastRunApproach"));
  double remainder=(double)Field(p,"moveRemainder")+travel;int delta=(int)remainder;Set(p,"moveRemainder",remainder-delta);
  var area=System.Windows.Forms.Screen.FromControl(p).WorkingArea;
  int next=p.Left+delta;if(next<area.Left||next+p.Width>area.Right){m.Boundary();Set(p,"moveRemainder",0.0);}
  p.Left=Math.Max(area.Left,Math.Min(next,area.Right-p.Width));
  Set(p,"speed",m.Speed);Set(p,"gait",m.Phase);Call(p,"UpdateCast",dt);
 }
 static void CloseTestPet(Pet p){typeof(System.Windows.Forms.Form).GetMethod("OnFormClosed",Flags).Invoke(p,new object[]{new System.Windows.Forms.FormClosedEventArgs(System.Windows.Forms.CloseReason.None)});p.Dispose();}
 static void Approach(){
  var p=new Pet();bool completes=true,safety=true;int hits=0;bool sawRun=false;double maxRun=0;
  try{
   Set(p,"settings",Path.GetFullPath(Folder+"approach-settings-test.txt"));Set(p,"wandering",false);Set(p,"petHeight",300);Call(p,"ResizePet");
   var m=(Motion)Field(p,"motion");m.SelectLightTurn=()=>false;var cast=(CastingMotion)Field(p,"casting");var renderer=(PetRenderer)Field(p,"renderer");
   p.ProjectileHit+=(s,e)=>hits++;
   var area=System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
   int center=area.Left+(area.Width-p.Width)/2;
   foreach(int fps in new[]{30,60,120})foreach(int face in new[]{-1,1})foreach(int height in new[]{-1,1}){
    p.CancelCast();p.Location=new Point(center,area.Top+area.Height/2-150);m.FollowPush(0,0,face,300);
    var shoulder=renderer.CastBones(CastingMotion.At(0,face)).Shoulder;
    var target=new PointF(p.Left+p.Width*.5f,p.Top+shoulder.Y+height*300);int start=p.Left,before=hits;Guid request=p.Cast(target);bool began=false;int movedBeforeCast=0;
    for(int n=0;n<fps*20;n++){
     ApproachStep(p,1.0/fps);maxRun=Math.Max(maxRun,m.RunBlend);sawRun|=m.RunBlend>.7&&Math.Abs(m.Speed)>36;
     if(cast.Active){began=true;movedBeforeCast=Math.Abs(p.Left-start);safety&=m.Speed==0&&m.Settle==1&&m.Facing==cast.Facing;}
     if(hits>before&&!Prop(p,"CastBusy"))break;
    }
    bool pass=began&&movedBeforeCast>25&&hits==before+1&&p.LastProjectileHit.RequestId==request&&p.LastProjectileHit.TargetPosition==target&&!Prop(p,"CastBusy");completes&=pass;
    Console.WriteLine("Approach "+fps+"fps initialFacing="+face+" targetHeight="+height+" moved="+Math.Abs(p.Left-start)+" hit="+(hits-before)+" pass="+pass);
   }
   foreach(int height in new[]{-1,1}){
    p.CancelCast();p.Location=new Point(center,area.Top+area.Height/2-150);m.SelectLightTurn=()=>true;m.FollowPush(0,RunGait.SpeedAt300,height,300,true);
    var target=new PointF(p.Left+p.Width*.5f,p.Top+120+height*300);int before=hits;p.Cast(target);
    for(int n=0;n<1200&&!(hits>before&&!Prop(p,"CastBusy"));n++)ApproachStep(p,1.0/60);
    safety&=hits==before+1&&!Prop(p,"CastBusy");
   }
   m.SelectLightTurn=()=>false;
   // Both horizontal boundaries: use the available side of the current screen.
   foreach(bool edge in new[]{false,true}){
    p.CancelCast();p.Location=new Point(edge?area.Right-p.Width-4:area.Left+4,area.Top+area.Height/2-150);m.FollowPush(0,0,edge?1:-1,300);
    var target=new PointF(p.Left+p.Width*.5f,p.Top-220);int before=hits;p.Cast(target);
    for(int n=0;n<1200&&!(hits>before&&!Prop(p,"CastBusy"));n++){ApproachStep(p,1.0/60);safety&=p.Left>=area.Left&&p.Right<=area.Right;}
    safety&=hits==before+1;
   }
   p.CancelCast();p.Location=new Point(center,area.Top+area.Height/2-150);m.FollowPush(0,0,1,300);
   PointF selected=new PointF(p.Left+p.Width*.5f,p.Top-220);p.Cast(selected);Call(p,"BeginCastIfReady");int count=hits;
   Set(p,"menuOpen",true);int saved=p.Left;for(int n=0;n<60;n++)ApproachStep(p,1.0/60);safety&=p.Left==saved&&cast.Pending;Set(p,"menuOpen",false);
   Set(p,"castFileTarget",new DesktopFileTarget("qa-removed.txt",selected,path=>null));ApproachStep(p,.12);safety&=!cast.Busy&&!Prop(p,"CastRunApproach")&&hits==count;
   p.Cast(selected);Call(p,"BeginCastIfReady");p.CancelCast();safety&=!cast.Busy&&!Prop(p,"CastRunApproach");
   // Planner also supports negative desktop coordinates and fails cleanly when no position is usable.
   var plan=new CastingApproach();safety&=plan.Plan(-800,-1800,-400,300,1,(x,d)=>x< -1000&&d==1)&&plan.Destination< -1000;
   safety&=!plan.Plan(0,0,50,300,1,(x,d)=>false)&&!plan.Moving;
  }finally{CloseTestPet(p);}
  Console.WriteLine("Maximum approach run blend="+maxRun.ToString("F3"));
  Check(completes&&sawRun,"above/below files: run to a usable position, brake, face target, cast and hit once at 30/60/120fps without a second request");
  Check(safety,"approach respects screen edges, menu pause, target loss and cancellation; no motion during casting; negative coordinates and no-space plan covered");
 }
 static object Field(Pet p,string name){return typeof(Pet).GetField(name,Flags).GetValue(p);}
 static void Set(Pet p,string name,object value){typeof(Pet).GetField(name,Flags).SetValue(p,value);}
 static void Call(Pet p,string name,params object[] args){typeof(Pet).GetMethod(name,Flags).Invoke(p,args);}
 static void Integration(){
  var p=new Pet();bool safe=true;int hits=0;var m=(Motion)Field(p,"motion");var cast=(CastingMotion)Field(p,"casting");
  try {
   // Keep runtime checks and event logs within QA, never overwrite user settings.
   Set(p,"settings",Path.GetFullPath(Folder+"runtime-settings-test.txt"));Set(p,"wandering",false);p.Location=new Point(150,100);
   PointF target=new PointF(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Right-40,220);
   p.ProjectileHit+=(s,e)=>hits++;
   Action begin=delegate{m.FollowPush(0,0,1,300);p.Cast(target);Call(p,"BeginCastIfReady");if(!cast.Active)throw new Exception("UI cast failed to begin");};
   begin();Set(p,"menuOpen",true);Call(p,"UpdateCast",.5);safe&=cast.Age==0;Set(p,"menuOpen",false);
   Call(p,"UpdateCast",.5);p.Location=new Point(151,100);safe&=!cast.Busy&&hits==0;
   begin();Call(p,"UpdateCast",CastingMotion.ReleaseAt+.01);safe&=Field(p,"castBolt")!=null;
   IntPtr handle=p.Handle;Call(p,"RenderCast");
   var overlay=(System.Windows.Forms.Form)Field(p,"castFlightOverlay");
   safe&=overlay.Visible&&overlay.Bounds==MagicBoltArt.Bounds((MagicBolt)Field(p,"castBolt"));
   p.CancelCast();Call(p,"UpdateCast",5.0);safe&=hits==0&&!overlay.Visible;
   begin();Call(p,"UpdateCast",.5);m.Facing=-1;Call(p,"UpdateCast",.1);safe&=!cast.Busy;
   begin();Call(p,"UpdateCast",.4);Call(p,"StartArchery",1);safe&=!cast.Busy&&((ArcheryMotion)Field(p,"archery")).Busy;
   begin();safe&=!((ArcheryMotion)Field(p,"archery")).Busy;p.SwordSlash(target,1);safe&=!cast.Busy&&((SwordMotion)Field(p,"sword")).Busy;
   begin();safe&=!((SwordMotion)Field(p,"sword")).Busy;Set(p,"castEffects",false);
   for(int i=0;i<250;i++)Call(p,"UpdateCast",.02);
   safe&=hits==1&&!cast.Busy&&Field(p,"castBolt")==null&&p.LastProjectileHit!=null&&m.State=="hold";
   begin();Set(p,"castFileTarget",new DesktopFileTarget("qa-chosen.txt",target,path=>(PointF?)target));
   for(int i=0;i<250;i++)Call(p,"UpdateCast",.02);safe&=hits==2&&p.LastProjectileHit.TargetFile=="qa-chosen.txt";
   begin();Set(p,"castFileTarget",new DesktopFileTarget("qa-moved.txt",target,path=>(PointF?)new PointF(target.X+10,target.Y)));Call(p,"UpdateCast",.12);safe&=!cast.Busy&&hits==2;
   begin();PointF? fileLocation=target;Set(p,"castFileTarget",new DesktopFileTarget("qa-removed.txt",target,path=>fileLocation));Call(p,"UpdateCast",CastingMotion.ReleaseAt+.01);fileLocation=null;Call(p,"UpdateCast",2.0);safe&=Field(p,"castBolt")==null&&hits==2;
   begin();Set(p,"castDisplayLayout","display removed");Call(p,"UpdateCast",.02);safe&=!cast.Busy;
   begin();Guid first=cast.Id;EventHandler<ProjectileHitEventArgs> next=(s,e)=>p.Cast(target);p.ProjectileHit+=next;
   for(int i=0;i<240&&cast.Id==first;i++)Call(p,"UpdateCast",.02);p.ProjectileHit-=next;safe&=cast.Id!=first&&cast.Pending;
   p.CancelCast();
  } finally {
   typeof(System.Windows.Forms.Form).GetMethod("OnFormClosed",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,new object[]{new System.Windows.Forms.FormClosedEventArgs(System.Windows.Forms.CloseReason.None)});p.Dispose();
  }
  Check(safe,"real Pet coordination: menu pause, move/facing/display cancellation, sword/bow exclusion, effects-off hit, recovery and reentrant subscriber");
 }
 static object ConvertValue(object value,Type target){if(value==null||target.IsPrimitive||target==typeof(string))return value;object copy=Activator.CreateInstance(target);foreach(var f in value.GetType().GetFields(Flags))target.GetField(f.Name,Flags).SetValue(copy,ConvertValue(f.GetValue(value),target.GetField(f.Name,Flags).FieldType));return copy;}
 static void Regression(){
  var old=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));var type=old.GetType("JouchoPet.PetRenderer");object prior=Activator.CreateInstance(type,true);bool same=true;
  old.GetType("JouchoPet.AnimationParameters").GetMethod("UseFixedValues",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
  using(var current=new PetRenderer()){current.Resize(300);type.GetMethod("Resize").Invoke(prior,new object[]{300});try{
   foreach(string kind in new[]{"idle","sleep","walk","run","wave","pat","carry","leap","archery","turn"}){
    var f=new RenderFrame{Time=.4,Settle=1,MotionState="idle",Facing=0,Gait=.7};
    if(kind=="sleep"){f.RestAmount=.75;f.ClosedEyes=1;}
    if(kind=="walk"||kind=="run"||kind=="carry"){f.Facing=kind=="walk"?-1:1;f.Speed=36;f.MotionState="walk";f.RunBlend=kind=="run"?1:0;f.Carry=kind=="carry"?1:0;}
    if(kind=="wave"){f.Greeting=1;f.WaveBlend=1;f.WavePhase=.7;}
    if(kind=="pat"){f.Affection=1;f.ClosedEyes=1;}
    if(kind=="leap"){f.Facing=-1;f.Leaping=true;f.Leap=LeapMotion.At(.4,-1);}
    if(kind=="archery"){f.Archery=true;f.BowPose=ArcheryMotion.At(1.2,-1);}
    if(kind=="turn"){f.MotionState="turn";f.Angle=.4;}
    int[] a=PoseMesh.Read((Bitmap)type.GetMethod("Draw").Invoke(prior,new[]{ConvertValue(f,old.GetType("JouchoPet.RenderFrame"))})),b=PoseMesh.Read(current.Draw(f));for(int k=0;k<a.Length;k++)same&=a[k]==b[k];
   }
   foreach(int direction in new[]{-1,1})foreach(double t in new[]{.2,.42,.60,.99}){
    var p=SwordMotion.At(t,direction);int[] a=PoseMesh.Read((Bitmap)type.GetMethod("DrawSword",Flags).Invoke(prior,new[]{ConvertValue(p,old.GetType("JouchoPet.SwordPose")),(object)true})),b=PoseMesh.Read(current.DrawSword(p,true));for(int k=0;k<a.Length;k++)same&=a[k]==b[k];
   }
  }finally{((IDisposable)prior).Dispose();}}
  Check(same,"18 representative frozen frames including sleep, sword, bow, eyes, locomotion and carry are pixel-identical to 0.7.23");
 }
 static PointF PreviewTarget(int size,int direction){float margin=(float)(size*.80*MagicBoltArt.ImpactScale+10);return new PointF(direction>0?760-margin:margin,32+size*.46f);}
 static Bitmap Scene(CastingRig rig,int size,int direction,double t,bool effects,bool debug=false,double aim=0){
  var image=new Bitmap(760,500,PixelFormat.Format32bppPArgb);
  using(var g=Graphics.FromImage(image)){
   g.Clear(Color.FromArgb(35,43,56));
   Point origin=new Point(direction>0?70:760-70-PetRenderer.CanvasSize(size).Width,32);
   PointF target=PreviewTarget(size,direction);target.Y+=(float)(size*Math.Tan(aim*Math.PI/180));
   PointF localTarget=RigParts.Add(target,-origin.X,-origin.Y);var p=rig.PoseForTarget(Math.Min(t,CastingMotion.Duration),direction,localTarget);
   var character=rig.Draw(p,effects,debug);g.DrawImageUnscaled(character,origin.X-rig.Padding,origin.Y-rig.Padding);
   // A static example file icon; only the projectile/effect changes between frames.
   using(var paper=new SolidBrush(Color.FromArgb(205,221,233)))g.FillRectangle(paper,target.X-12,target.Y-16,24,32);
   using(var pen=new Pen(Color.FromArgb(73,110,145),1.4f)){g.DrawRectangle(pen,target.X-12,target.Y-16,24,32);g.DrawLine(pen,target.X-7,target.Y-3,target.X+7,target.Y-3);g.DrawLine(pen,target.X-7,target.Y+3,target.X+7,target.Y+3);}
   g.DrawString("demo.txt",SystemFonts.DefaultFont,Brushes.LightGray,target.X-23,target.Y+20);
   if(t>=CastingMotion.ReleaseAt){PointF local=rig.Core(rig.PoseForTarget(CastingMotion.ReleaseAt,direction,localTarget));var bolt=new MagicBolt(Guid.Empty,RigParts.Add(local,origin.X,origin.Y),target,size);bolt.Update(t-CastingMotion.ReleaseAt);if(effects)MagicBoltArt.Draw(g,bolt);}
   g.DrawString(size+"px / "+(direction<0?"Left":"Right")+" / "+t.ToString("F2")+"s"+(effects?"":" / effects OFF"),SystemFonts.DefaultFont,Brushes.White,12,12);
  }return image;
 }
 static void HeadPreview(){
  int size=300;Size c=PetRenderer.CanvasSize(size);
  using(var side=new SideWalk(c.Width,c.Height,size))using(var rig=new CastingRig(c.Width,c.Height,size,side))using(var sheet=new Bitmap(1600,1000))using(var g=Graphics.FromImage(sheet)){
   g.Clear(Color.FromArgb(35,43,56));
   for(int row=0;row<2;row++)for(int col=0;col<4;col++){
    int direction=col<2?1:-1;bool prior=col%2==0;
    PointF shoulder=rig.Sample(CastingMotion.At(0,direction)).Shoulder;
    PointF target=RigParts.Add(shoulder,direction*size*1.3,(row==0?-1:1)*size*.9);
    var p=rig.PoseForTarget(CastingMotion.PushEnd,direction,target);
    if(prior)p.HeadTilt*=.5;
    var b=rig.Draw(p,false);
    g.DrawImageUnscaled(b,col*400+(400-c.Width)/2-rig.Padding,row*500+30-rig.Padding);
    g.DrawString((prior?"Previous tilt":"New tilt")+" / "+(direction>0?"Right":"Left")+" / "+(row==0?"Up":"Down")+" / "+(Math.Abs(p.HeadTilt)*180/Math.PI).ToString("F1")+" deg",SystemFonts.DefaultFont,Brushes.White,col*400+10,row*500+10);
   }
   sheet.Save(Folder+"head-review.png",ImageFormat.Png);
  }
 }
 static void TargetPreview(){
  int size=300;Size c=PetRenderer.CanvasSize(size);using(var side=new SideWalk(c.Width,c.Height,size))using(var rig=new CastingRig(c.Width,c.Height,size,side))using(var sheet=new Bitmap(1520,1500))using(var g=Graphics.FromImage(sheet)){
   for(int row=0;row<3;row++)for(int col=0;col<2;col++)using(var frame=Scene(rig,size,col==0?1:-1,CastingMotion.PushEnd,true,false,(row-1)*25))g.DrawImageUnscaled(frame,col*760,row*500);
   sheet.Save(Folder+"target-review.png",ImageFormat.Png);
  }
 }
 static void Preview(){
  foreach(int size in new[]{220,300,400}){Size c=PetRenderer.CanvasSize(size);using(var side=new SideWalk(c.Width,c.Height,size))using(var rig=new CastingRig(c.Width,c.Height,size,side))foreach(int direction in new[]{-1,1}){
   string name="cast-"+size+"-"+(direction<0?"left":"right");
   Point origin=new Point(direction>0?70:760-70-c.Width,32);PointF target=PreviewTarget(size,direction);
   var travel=new MagicBolt(Guid.Empty,RigParts.Add(rig.Core(rig.PoseForTarget(CastingMotion.ReleaseAt,direction,RigParts.Add(target,-origin.X,-origin.Y))),origin.X,origin.Y),target,size);
   File.WriteAllText(Folder+name+"-meta.json","{\"hitAt\":"+(CastingMotion.ReleaseAt+travel.FlightDuration).ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"}");
   foreach(bool effects in new[]{false,true})using(var atlas=new Bitmap(760*8,500*15,PixelFormat.Format32bppPArgb))using(var g=Graphics.FromImage(atlas)){
    for(int i=0;i<120;i++)using(var frame=Scene(rig,size,direction,i/30.0,effects))g.DrawImageUnscaled(frame,i%8*760,i/8*500);
    atlas.Save(Folder+name+(effects?"-fx":"-clean")+".png",ImageFormat.Png);
   }
   if(size==300){
    using(var aimSheet=new Bitmap(1140,500))using(var ag=Graphics.FromImage(aimSheet))for(int i=0;i<3;i++){
     using(var frame=Scene(rig,size,direction,CastingMotion.PushStart,true,false,(i-1)*20)){ag.DrawImage(frame,new Rectangle(i*380,0,380,500),new Rectangle(direction>0?40:360,0,380,500),GraphicsUnit.Pixel);ag.FillRectangle(Brushes.DarkSlateGray,i*380,0,380,26);ag.DrawString("Aim "+((i-1)*20)+" deg / target-directed head",SystemFonts.DefaultFont,Brushes.White,i*380+8,5);}if(i==2)aimSheet.Save(Folder+name+"-aim.png",ImageFormat.Png);
    }
    double[] times={.15,CastingMotion.ChargeStart,CastingMotion.PushStart,CastingMotion.PushEnd,CastingMotion.PushEnd+CastingMotion.RecoverDuration*.5};foreach(bool debug in new[]{false,true})using(var sheet=new Bitmap(380*5,500))using(var g=Graphics.FromImage(sheet)){
    for(int i=0;i<5;i++)using(var frame=Scene(rig,size,direction,times[i],false,debug)){int x=direction>0?40:360;g.DrawImage(frame,new Rectangle(i*380,0,380,500),new Rectangle(x,0,380,500),GraphicsUnit.Pixel);g.FillRectangle(Brushes.DarkSlateGray,i*380,0,380,26);g.DrawString(new[]{"Prepare","Lift","Charge","Release","Recover"}[i]+" / "+times[i].ToString("F2")+"s",SystemFonts.DefaultFont,Brushes.White,i*380+8,5);}sheet.Save(Folder+name+(debug?"-bones":"-poses")+".png",ImageFormat.Png);
   }}
   Console.WriteLine("PREVIEW "+name+" effects on/off ready");
  }}
 }
}


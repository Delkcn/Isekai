using System;using System.IO;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using JouchoPet;
class ArcheryPoseCheck {
 static int count;static readonly string Folder=Path.Combine("qa","v0646");
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static void Quiet(bool ok,string name){if(!ok)throw new Exception(name);}
 [STAThread]static void Main(string[] args){try{AnimationParameters.UseFixedValues();if(args.Length>0&&args[0]=="--preview-only"){Portraits();return;}AimAndCadence();Joints();DrawContact();Lowering();Occlusion();Release();Portraits();Console.WriteLine("All "+count+" archery pose checks passed.");}catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
 static void AimAndCadence(){foreach(int facing in new[]{-1,1}){
  PointF axis=ArcheryMotion.Axis(facing);Check(Math.Abs(Math.Atan2(-axis.Y,Math.Abs(axis.X))*180/Math.PI-25)<.00001&&axis.X*facing>0&&Math.Abs(axis.X*axis.X+axis.Y*axis.Y-1)<.000001,"arrow and bow share a normalized 25-degree upward aim / "+facing);
  foreach(int size in new[]{220,300,400}){int width=PetRenderer.CanvasSize(size).Width;PointF grip=ArcheryMotion.Grip(width,size,facing);for(int i=0;i<=186;i++){var p=ArcheryMotion.At(.95+i*(ArcheryMotion.ReleaseAt-.95)/186,facing);PointF nock=ArcheryMotion.Nock(width,size,p);double dx=grip.X-nock.X,dy=grip.Y-nock.Y;Quiet(Math.Abs(dx*axis.Y-dy*axis.X)<.001,"arrow shaft drifts away from the painted bow grip");Quiet(dx*axis.X+dy*axis.Y>=ArcheryMotion.BowDepth(size)-.001,"nock moves forward beyond the resting string");}
   var rest=ArcheryMotion.At(.95,facing);PointF expected=RigParts.Add(grip,-axis.X*ArcheryMotion.BowDepth(size),-axis.Y*ArcheryMotion.BowDepth(size));Check(RigParts.Length(expected,ArcheryMotion.Nock(width,size,rest))<.001,"rest string, held arrow and drawn nock remain collinear through grip / "+size+" / "+facing);
  }
 }
 double previous=0;for(int i=0;i<=504;i++){double p=ArcheryMotion.DrawProgress(1.15+i/480.0);Quiet(p>=previous-1e-12&&p>=0&&p<=1,"draw reverses during continuous effort");previous=p;}Check(previous==1,"continuous draw advances monotonically and reaches full draw");
 Check(ArcheryMotion.DrawProgress(1.15)==0&&ArcheryMotion.DrawProgress(1.5)<ArcheryMotion.DrawProgress(1.565)&&ArcheryMotion.DrawProgress(1.565)<ArcheryMotion.DrawProgress(1.63)&&ArcheryMotion.DrawProgress(2.2)==1,"pull continues through the former pause interval");
 const double dt=.0001;foreach(double boundary in new[]{1.15,2.2}){double a=ArcheryMotion.DrawProgress(boundary-dt),b=ArcheryMotion.DrawProgress(boundary),c=ArcheryMotion.DrawProgress(boundary+dt);Quiet(Math.Abs((c-a)/(2*dt))<.00001&&Math.Abs((a+c-2*b)/(dt*dt))<.02,"draw cadence has a hard velocity or acceleration discontinuity at "+boundary);}Check(true,"continuous pull starts and finishes with zero velocity and acceleration");
 foreach(int facing in new[]{-1,1}){const int size=300;Size c=PetRenderer.CanvasSize(size);using(var sides=new SideWalk(c.Width,c.Height,size))using(var rig=new ArcheryRig(c.Width,c.Height,size,sides)){
   var a=rig.DiagnosticArm(ArcheryMotion.At(1.5,facing),false);var b=rig.DiagnosticArm(ArcheryMotion.At(1.63,facing),false);Check(RigParts.Length(a.Elbow,b.Elbow)>1&&RigParts.Length(a.Finger,b.Finger)>1,"drawing arm moves through former pause / "+facing);
   a=rig.DiagnosticArm(ArcheryMotion.At(2.2,facing),false);b=rig.DiagnosticArm(ArcheryMotion.At(ArcheryMotion.ReleaseAt,facing),false);Check(RigParts.Length(a.Elbow,b.Elbow)<.001&&RigParts.Length(a.Finger,b.Finger)<.001&&Math.Abs(ArcheryMotion.ReleaseAt-2.2-.30)<1e-12,"full draw holds aim for 300 milliseconds before the unchanged release / "+facing);
  }
 }
 }
 static void Joints(){foreach(int size in new[]{220,300,400}){Size c=PetRenderer.CanvasSize(size);using(var sides=new SideWalk(c.Width,c.Height,size))using(var rig=new ArcheryRig(c.Width,c.Height,size,sides))foreach(int facing in new[]{-1,1}){
  foreach(bool holder in new[]{true,false}){PointF prior=PointF.Empty,priorElbow=PointF.Empty;double max=0,min=1;int minFrame=0,maxFrame=0;for(int i=0;i<=708;i++){var p=ArcheryMotion.At(i/120.0,facing);var arm=rig.DiagnosticArm(p,holder);double u=RigParts.Length(arm.Shoulder,arm.Elbow),l=RigParts.Length(arm.Elbow,arm.Wrist);
   double h=RigParts.Length(arm.Wrist,arm.Finger);
   Quiet(Math.Abs(Math.Sqrt(u*u+arm.Depth*arm.Depth)-arm.UpperLength)<.001 && Math.Abs(Math.Sqrt(l*l+arm.LowerDepth*arm.LowerDepth)-arm.LowerLength)<.001 && Math.Abs(Math.Sqrt(h*h+arm.HandDepth*arm.HandDepth)-arm.HandLength)<.001,"bone stretches / "+size+" / "+facing+" / "+holder+" / "+i);
   double projection=Math.Min(u/arm.UpperLength,l/arm.LowerLength);if(projection<min){min=projection;minFrame=i;}if(i>0){double step=Math.Max(RigParts.Length(prior,arm.Wrist),RigParts.Length(priorElbow,arm.Elbow));if(step>max){max=step;maxFrame=i;}}prior=arm.Wrist;priorElbow=arm.Elbow;
  }Console.WriteLine("Minimum projection="+min.ToString("F4")+" at "+(minFrame/120.0).ToString("F3")+" max joint step="+max.ToString("F4")+" at "+(maxFrame/120.0).ToString("F3"));Check(min>.35 && max<size*.025,"spatial arm lengths fixed, no wrist jump or collapsed projection / "+size+" / "+facing+" / "+holder);}
  var full=ArcheryMotion.At(2.25,facing);var hold=rig.DiagnosticArm(full,true);var pull=rig.DiagnosticArm(full,false);
  Console.WriteLine("Wrist target gap="+RigParts.Length(hold.Wrist,ArcheryMotion.BowWrist(c.Width,size,facing)).ToString("F4")+"; extension="+(RigParts.Length(hold.Shoulder,hold.Wrist)/(hold.UpperLength+hold.LowerLength)).ToString("F4"));
  Check(RigParts.Length(hold.Wrist,ArcheryMotion.BowWrist(c.Width,size,facing))<.02 && RigParts.Length(hold.Shoulder,hold.Wrist)/(hold.UpperLength+hold.LowerLength)>.985,"bow arm extends fully to grip / "+size+" / "+facing);
  Console.WriteLine("Drawing shoulder="+pull.Shoulder+" elbow="+pull.Elbow+" wrist="+pull.Wrist);
  // Lowering a fixed-length arm reduces its horizontal projection, especially
  // in the far view. Require a substantial anatomical rearward component and
  // the precise requested lower height rather than the previous pose's x-span.
  Check(Math.Abs(RigParts.Length(pull.Finger,ArcheryMotion.FaceSide(c.Width,size,facing))-size*.025)<.02 && facing*(pull.Elbow.X-pull.Shoulder.X)<-pull.UpperLength*.40 && Math.Abs((pull.Elbow.Y-pull.Shoulder.Y)-size*.060)<.02,"finger anchors beside jaw; elbow remains behind and .060 height below shoulder / "+size+" / "+facing);
  foreach(double boundary in new[]{.15,.95,1.0,.40,.45,.95,1.15,1.4,1.5,1.63,2.2,2.5,2.55,2.64,2.77,3.1,3.15,4.65,4.85})foreach(bool holder in new[]{true,false}){
   var a=rig.DiagnosticArm(ArcheryMotion.At(boundary-1e-6,facing),holder);var b=rig.DiagnosticArm(ArcheryMotion.At(boundary+1e-6,facing),holder);Quiet(RigParts.Length(a.Elbow,b.Elbow)<.01 && RigParts.Length(a.Wrist,b.Wrist)<.01,"joint stage discontinuity / "+size+" / "+facing+" / "+holder+" / "+boundary+" elbow="+RigParts.Length(a.Elbow,b.Elbow)+" wrist="+RigParts.Length(a.Wrist,b.Wrist));
  }Check(true,"joint stages including release/lowering connect continuously / "+size+" / "+facing);
 }}}
 static double Bend(ArcheryRig.ArmPose a){double ux=a.Elbow.X-a.Shoulder.X,uy=a.Elbow.Y-a.Shoulder.Y,vx=a.Wrist.X-a.Elbow.X,vy=a.Wrist.Y-a.Elbow.Y;return Math.Acos(Math.Max(-1,Math.Min(1,(ux*vx+uy*vy+a.Depth*a.LowerDepth)/(a.UpperLength*a.LowerLength))));}
 static void Lowering(){foreach(int size in new[]{220,300,400}){Size c=PetRenderer.CanvasSize(size);using(var sides=new SideWalk(c.Width,c.Height,size))using(var rig=new ArcheryRig(c.Width,c.Height,size,sides))foreach(int facing in new[]{-1,1}){
  var previous=rig.DiagnosticArm(ArcheryMotion.At(3.15,facing),false);double previousBend=Bend(previous),maxFingerRise=0;
  for(int i=1;i<=180;i++){var arm=rig.DiagnosticArm(ArcheryMotion.At(3.15+i/120.0,facing),false);double bend=Bend(arm);Quiet(bend<=previousBend+.0001,"elbow folds backwards while lowering / "+facing+" / "+i);Quiet(arm.Elbow.Y>=previous.Elbow.Y-.001,"elbow rises instead of dropping / "+facing+" / "+i);maxFingerRise=Math.Max(maxFingerRise,previous.Finger.Y-arm.Finger.Y);previous=arm;previousBend=bend;}
  Check(maxFingerRise<size*.001,"elbow opens continuously as arm and hand drop, no reverse orbit / "+size+" / "+facing);
  var rest=rig.DiagnosticArm(ArcheryMotion.At(0,facing),false);Check(RigParts.Length(rest.Shoulder,previous.Shoulder)<.001&&RigParts.Length(rest.Elbow,previous.Elbow)<.001&&RigParts.Length(rest.Wrist,previous.Wrist)<.001&&RigParts.Length(rest.Finger,previous.Finger)<.001,"forward-kinematic lowering returns all joints to exact rest / "+size+" / "+facing);
 }}}
 static void DrawContact(){foreach(int size in new[]{220,300,400}){Size c=PetRenderer.CanvasSize(size);using(var sides=new SideWalk(c.Width,c.Height,size))using(var rig=new ArcheryRig(c.Width,c.Height,size,sides))foreach(int facing in new[]{-1,1}){
  var start=rig.DiagnosticArm(ArcheryMotion.At(1.15,facing),false);var finish=rig.DiagnosticArm(ArcheryMotion.At(2.2,facing),false);double previous=facing*start.Elbow.X;
  for(int i=0;i<=186;i++){double age=ArcheryMotion.BowAppearAt+i*(ArcheryMotion.ReleaseAt-ArcheryMotion.BowAppearAt)/186;var p=ArcheryMotion.At(age,facing);var arm=rig.DiagnosticArm(p,false);Quiet(RigParts.Length(arm.Finger,ArcheryMotion.Nock(c.Width,size,p))<.025,"painted fingers lose arrow nock during draw");if(age>=1.15){Quiet(facing*arm.Elbow.X<=previous+.001,"elbow moves forward while drawing");previous=facing*arm.Elbow.X;}}
  double retreat=facing*(start.Elbow.X-finish.Elbow.X),rise=Math.Abs(start.Elbow.Y-finish.Elbow.Y);
  Check(retreat>size*.10 && rise<retreat*.5,"shoulder drives rearward elbow on near-horizontal track / "+size+" / "+facing);
  Check(true,"painted fingertip stays on arrow tail through hook, draw and aim / "+size+" / "+facing);
  for(int i=0;i<=65;i++){var p=ArcheryMotion.At(ArcheryMotion.ReleaseAt+i*.01,facing);var arm=rig.DiagnosticArm(p,false);Quiet(RigParts.Length(arm.Finger,ArcheryMotion.DrawingHand(c.Width,size,p))<.025,"finger follow-through leaves intended contact curve");Quiet(p.Ready==1 && facing*(arm.Elbow.X-arm.Shoulder.X)<-arm.UpperLength*.40 && facing*(arm.Elbow.X-finish.Elbow.X)<.01,"elbow chases releasing string");}
  Check(true,"arms hold aim after release; drawing fingers follow back while string returns independently / "+size+" / "+facing);
  var hold=rig.DiagnosticArm(ArcheryMotion.At(2.25,facing),true);Check(RigParts.Length(hold.Finger,ArcheryMotion.Grip(c.Width,size,facing))<.025,"left hand contact stays on bow grip / "+size+" / "+facing);
 }}}
 static void Occlusion(){const int size=300;Size c=PetRenderer.CanvasSize(size);using(var sides=new SideWalk(c.Width,c.Height,size))using(var rig=new ArcheryRig(c.Width,c.Height,size,sides)){
  var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
  var stances=(ArcheryStance[])typeof(ArcheryRig).GetField("stance",flags).GetValue(rig);
  var arms=(JointArm[])typeof(ArcheryRig).GetField("arms",flags).GetValue(rig);
  foreach(int facing in new[]{-1,1}){int d=facing<0?0:1;long hidden=0;int maxBlendError=0;
   foreach(double age in new[]{.6,.95,.65,.95,1.65,2.25,2.77,3.6,4.3}){
    var pose=ArcheryMotion.At(age,facing);var far=rig.DiagnosticArm(pose,d==1);var near=rig.DiagnosticArm(pose,d==0);
    int[] body;using(var rendered=new Bitmap(c.Width,c.Height,PixelFormat.Format32bppPArgb)){using(var g=Graphics.FromImage(rendered)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImageUnscaled(stances[d].Draw(pose.Ready),0,0);}body=PoseMesh.Read(rendered);}
    using(var farImage=new Bitmap(c.Width,c.Height,PixelFormat.Format32bppPArgb))using(var nearImage=new Bitmap(c.Width,c.Height,PixelFormat.Format32bppPArgb)){
     using(var g=Graphics.FromImage(farImage)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;arms[1-d].DrawToFinger(g,far.Shoulder,far.Elbow,far.Wrist,far.Finger);}
     using(var g=Graphics.FromImage(nearImage)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;arms[d].DrawToFinger(g,near.Shoulder,near.Elbow,near.Wrist,near.Finger);}
     int[] fp=PoseMesh.Read(farImage),np=PoseMesh.Read(nearImage),actual=PoseMesh.Read(rig.Draw(pose));
     // Native GDI+ rounds each premultiplied layer separately. Permit at most
     // one channel level per draw while checking that the body dominates it.
     for(int i=0;i<body.Length;i++)if((uint)body[i]>>24>=240 && (uint)np[i]>>24==0 && (uint)fp[i]>>24>16){hidden++;uint b=(uint)body[i],f=(uint)fp[i],a=(uint)actual[i];for(int shift=0;shift<32;shift+=8){int expected=(int)(b>>shift&255)+(int)Math.Round((f>>shift&255)*(255-(b>>24))/255.0),error=Math.Abs((int)(a>>shift&255)-expected);maxBlendError=Math.Max(maxBlendError,error);Quiet(error<=8,"far arm painted through body/head at age "+age+" facing "+facing+" pixel="+(i%c.Width)+","+(i/c.Width)+" body="+b.ToString("X8")+" far="+f.ToString("X8")+" actual="+a.ToString("X8")+" shift="+shift+" expected="+expected);}}
    }
   }Check(hidden>20,"body/head fully occlude far "+(facing<0?"right":"left")+" arm across lifting, drawing and lowering ("+hidden+" pixels, maximum blend error "+maxBlendError+")");
  }
 }}
 static void Release(){foreach(int facing in new[]{-1,1}){int width=PetRenderer.CanvasSize(300).Width;var before=ArcheryMotion.At(ArcheryMotion.ReleaseAt-1e-7,facing);var after=ArcheryMotion.At(ArcheryMotion.ReleaseAt+1e-7,facing);
  Check(RigParts.Length(ArcheryMotion.DrawingHand(width,300,before),ArcheryMotion.DrawingHand(width,300,after))<.001,"string release does not jump hand / "+facing);
  var follow=ArcheryMotion.At(2.77,facing);PointF hand=ArcheryMotion.DrawingHand(width,300,follow),cheek=ArcheryMotion.FaceSide(width,300,facing),nock=ArcheryMotion.Nock(width,300,follow);
  Check((hand.X-cheek.X)*facing<0 && RigParts.Length(hand,nock)>15 && follow.Ready==1,"drawing hand follows behind cheek independently of springing string / "+facing);
  Check(ArcheryMotion.At(3.15,facing).Ready==1 && ArcheryMotion.At(4.65,facing).Ready==0,"gentle lowering takes 1.5 seconds after release follow-through / "+facing);
 }}
 static void Portraits(){using(var renderer=new PetRenderer()){renderer.Resize(400);foreach(int facing in new[]{-1,1}){double[] ages={.95,.95,2.25,2.77,3.6,4.3};using(var sheet=new Bitmap(650*3,600*2))using(var g=Graphics.FromImage(sheet)){
  g.Clear(Color.FromArgb(53,61,74));g.SmoothingMode=SmoothingMode.AntiAlias;for(int i=0;i<ages.Length;i++){Point origin=new Point(i%3*650+100,i/3*600+110);var pose=ArcheryMotion.At(ages[i],facing);if(facing<0)ArcheryArt.Bow(g,origin,PetRenderer.CanvasSize(400).Width,400,pose);g.DrawImageUnscaled(renderer.Draw(new RenderFrame{Archery=true,BowPose=pose}),origin);if(facing>0)ArcheryArt.Bow(g,origin,PetRenderer.CanvasSize(400).Width,400,pose);g.DrawString("Shot age "+ages[i].ToString("F2"),SystemFonts.DefaultFont,Brushes.White,i%3*650+20,i/3*600+20);}sheet.Save(Path.Combine(Folder,"archery-arms-"+(facing<0?"left":"right")+".png"),ImageFormat.Png);
 }}}Check(true,"full-size arm and bow contact sheets rendered");}
}


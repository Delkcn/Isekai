using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;using System.Text;using System.Globalization;using JouchoPet;
class LeapCheck {
 static int count;
 static void Check(bool yes,string message){if(!yes)throw new Exception(message);count++;Console.WriteLine("PASS "+message);}
 static void Quiet(bool yes,string message){if(!yes)throw new Exception(message);}
 static double Distance(PointF a,PointF b){return Math.Sqrt(Math.Pow(a.X-b.X,2)+Math.Pow(a.Y-b.Y,2));}
 static double Diff(Bitmap a,Bitmap b){int[] x=PoseMesh.Read(a),y=PoseMesh.Read(b);double sum=0;for(int i=0;i<x.Length;i++)for(int s=0;s<32;s+=8)sum+=Math.Abs(((uint)x[i]>>s&255)-(double)((uint)y[i]>>s&255));return sum/(x.Length*4);}
 static void Main(){try {
  foreach(int size in new[]{220,300,400})foreach(int direction in new[]{-1,1})foreach(int vertical in new[]{-1,1}) {
   Size canvas=PetRenderer.CanvasSize(size);var area=new Rectangle(-1920,-100,1920,1080);var m=new LeapMotion();m.Request(vertical,direction);
   string label=size+" / "+direction+" / "+vertical;
   Check(m.Begin(new Point(-1000,250),area,canvas,size),"begin "+label);
   PointF previous=m.Position;bool airborne=false,crouch=false,done=false;
   for(int i=0;i<400;i++) {
    double before=m.Age;PointF hold=m.Position;m.Update(0);Quiet(m.Age==before && m.Position==hold,"pause advanced leap");
    done=m.Update(1.0/120);LeapPose pose=m.Pose;
    Quiet(m.Position.X>=area.Left && m.Position.X+canvas.Width<=area.Right && m.Position.Y>=area.Top && m.Position.Y+canvas.Height<=area.Bottom,"leap left working area");
    Quiet(Distance(previous,m.Position)<size*.02,"trajectory position jump");previous=m.Position;
    crouch|=pose.Crouch>.3;airborne|=pose.Air>.9;
    if(m.Age<=m.Windup)Quiet(m.Position==m.Start,"windup moved window");
    if(done)break;
   }
   Check(done && m.Position==m.Target && !m.Busy,"lands exactly and completes "+label);
   Check(crouch && airborne && m.Pose.Crouch==0 && m.Pose.Air==0,"anticipation flight recovery "+label);
   Check(Math.Sign(m.Target.Y-m.Start.Y)==vertical,"requested vertical direction "+label);
   Check(Math.Sign(m.Target.X-m.Start.X)==direction && Math.Abs(m.Target.X-m.Start.X)>=size*.25,"landing advances in facing direction "+label);
   Check(Math.Abs(m.Target.X-m.Start.X)==Math.Round(size*(vertical<0?.45:.28)),"upward forward distance extended; downward distance retained "+label);
   m.Request(vertical,direction);m.Begin(new Point(-1000,250),area,canvas,size);m.Update(.7);PointF cancel=m.Position;m.Cancel();m.Update(1);
   Check(!m.Busy && m.Position==cancel,"cancel preserves current position "+label);
   foreach(double seam in new[]{m.Windup,m.Windup+m.Flight,m.Duration}) {
    LeapPose a=LeapMotion.At(seam-1e-7,vertical),b=LeapMotion.At(seam+1e-7,vertical);
    Quiet(Math.Abs(a.Crouch-b.Crouch)+Math.Abs(a.Air-b.Air)+Math.Abs(a.Reach-b.Reach)+Math.Abs(a.Hair-b.Hair)+Math.Abs(a.Petals-b.Petals)<.0001,"pose seam jump");
   }
   Check(true,"continuous pose at stage boundaries "+label);
  }
  foreach(int vertical in new[]{-1,1}) {
   var m=new LeapMotion();Size canvas=PetRenderer.CanvasSize(300);var area=new Rectangle(0,0,800,800);int edge=vertical<0?0:800-canvas.Height;
   m.Request(vertical,1);Check(!m.Begin(new Point(300,edge),area,canvas,300) && !m.Busy,"edge rejects zero-distance leap "+vertical);
   m.Request(vertical,1);Check(m.Begin(new Point(800-canvas.Width,edge-vertical*10),area,canvas,300),"short edge leap starts "+vertical);
   for(int i=0;i<250;i++){m.Update(.01);Quiet(m.Position.X+canvas.Width<=800 && m.Position.Y>=0 && m.Position.Y+canvas.Height<=800,"short leap escaped screen");}
   Check(m.Position.Y==edge,"short edge leap lands on boundary "+vertical);
  }
  foreach(double forward in new[]{28.0,75})foreach(int facing in new[]{-1,1}) {
   AnimationParameters.Set("leapUpForward",forward);var m=new LeapMotion();var area=new Rectangle(-1200,0,2400,1000);Size c=PetRenderer.CanvasSize(300);
   m.Request(-1,facing);m.Begin(new Point(-200,350),area,c,300);Point target=m.Target;
   Check(target.X-m.Start.X==facing*(int)Math.Round(300*forward/100),"configured up-jump distance "+forward+" / "+facing);
   AnimationParameters.Set("leapUpForward",forward==28?75:28);for(int i=0;i<250;i++)m.Update(.01);
   Check(m.Position==target,"changing distance does not retarget active jump "+forward+" / "+facing);
   m.Request(-1,facing);m.Begin(new Point(facing<0?area.Left+12:area.Right-c.Width-12,350),area,c,300);
   for(int i=0;i<250;i++){m.Update(.01);Quiet(m.Position.X>=area.Left && m.Position.X+c.Width<=area.Right,"long up-jump crossed horizontal screen edge");}
   Check(!m.Busy,"long up-jump completes at screen edge "+forward+" / "+facing);
  }
  AnimationParameters.Set("leapUpForward",45);
  HandoffChecks();ArmChecks();Preview();Console.WriteLine("All "+count+" leap/run-arm checks passed.");
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
 static void HandoffChecks(){foreach(bool running in new[]{false,true})foreach(int facing in new[]{-1,1}) {
  var motion=new Motion();if(running)for(int i=0;i<240;i++)motion.Update(1.0/60,facing,300,false,true);
  var leap=new LeapMotion();leap.Request(-1,facing);bool started=false;
  Check(leap.ApproachDirection(motion,false)==0 && !leap.CanBegin(motion,false),"sleep/greeting/menu blocks takeoff "+running+" / "+facing);
  for(int i=0;i<240;i++) {
   motion.Update(1.0/60,leap.ApproachDirection(motion,true),300,true,false);
   if(leap.CanBegin(motion,true)){started=leap.Begin(new Point(400,300),new Rectangle(0,0,1600,1200),PetRenderer.CanvasSize(300),300);break;}
  }
  Check(started && motion.Speed==0 && motion.Facing==facing && motion.Settle==1,"front or run reaches neutral side before leap "+running+" / "+facing);
  for(int i=0;i<180;i++)leap.Update(1.0/60);
  motion.State="hold";motion.Speed=0;motion.Settle=1;
  for(int i=0;i<120;i++)motion.Update(1.0/60,0,300);
  Check(!leap.Busy && motion.State=="idle" && motion.Facing==0,"landing returns toward user "+running+" / "+facing);
 }}
 static void ArmChecks(){foreach(int size in new[]{220,300,400})for(int d=0;d<2;d++) {
  Size c=PetRenderer.CanvasSize(size);using(var body=CharacterArt.Normalized(d==0?"profile-left":"profile-right",c.Width,c.Height,size))using(var rig=new CarryArmRig(d,body,size)) {
   Check(Diff(body,rig.DrawRunning(0,0))==0,"zero running arm restores original "+size+" / "+d);
   double upper=Distance(rig.Shoulder,rig.Elbow),lower=Distance(rig.Elbow,rig.Wrist),min=double.MaxValue,minX=double.MaxValue,maxX=double.MinValue;
   double covariance=0,maxWristFollow=0;
   for(int i=0;i<48;i++) {
    double phase=i/48.0;PointF s=rig.SkinRunning(rig.Shoulder,phase,1),e=rig.SkinRunning(rig.Elbow,phase,1),w=rig.SkinRunning(rig.Wrist,phase,1);
    Quiet(Distance(s,rig.Shoulder)<.001 && Math.Abs(Distance(s,e)-upper)<.001 && Math.Abs(Distance(e,w)-lower)<.001,"run arm changed bone length");
    double elbowAngle=Math.Acos(Math.Max(-1,Math.Min(1,((s.X-e.X)*(w.X-e.X)+(s.Y-e.Y)*(w.Y-e.Y))/(upper*lower))))*180/Math.PI;
    Quiet(elbowAngle>150,"running elbow stays relaxed without deliberate bending");
    PointF opposite=rig.SkinRunning(rig.Wrist,phase+.5,1);
    double footLead=RunGait.Sample(phase,1,1).X-RunGait.Sample(phase+.5,1,1).X;
    double handLead=(d==0?-1:1)*(w.X-opposite.X);
    PointF oppositeElbow=rig.SkinRunning(rig.Elbow,phase+.5,1);
    if(Math.Abs(footLead)>.001)Quiet(footLead*(d==0?-1:1)*(e.X-oppositeElbow.X)<0,"upper arm must oppose the same-side leg at every phase");
    PointF tip=new PointF(rig.Wrist.X,rig.Wrist.Y+size*.035f);
    maxWristFollow=Math.Max(maxWristFollow,Distance(rig.SkinRunning(tip,phase,1),rig.SkinRunning(tip,phase,1,false)));
    Quiet(Distance(w,rig.SkinRunning(rig.Wrist,phase,1,false))<.001,"wrist follow moved its pivot");
    Quiet(Distance(rig.SkinRunning(tip,phase,1),rig.SkinRunning(tip,phase+1,1))<.001,"inertia has a loop seam");
    RunArmPose pose=RunArmPose.At(phase);
    Quiet(Math.Abs(pose.Wrist)<=9 && Math.Abs(pose.Lower)<12,"inertial angles exceed subtle follow-through limits");
    covariance+=footLead*handLead;
    Bitmap b=rig.DrawRunning(phase,1);min=Math.Min(min,rig.MinimumArea);minX=Math.Min(minX,w.X);maxX=Math.Max(maxX,w.X);
    int[] pixels=PoseMesh.Read(b),plate=rig.DiagnosticBackplate,arm=rig.DiagnosticPosedArm;
    for(int p=0;p<pixels.Length;p++)if(((uint)arm[p]>>24)==0 && ((uint)plate[p]>>24)>=12)Quiet(pixels[p]==plate[p],"run arm distorted backplate");
   }
   Check(min>0 && maxX-minX>size*.08,"visible arm swing without folded mesh or torso deformation "+size+" / "+d);
   Check(covariance<0,"relaxed arm keeps contralateral step coordination "+size+" / "+d);
   Check(maxWristFollow>size*.002,"hand has visible independent inertial follow "+size+" / "+d);
  }
 }}
 static void Preview() {
  string folder=Path.Combine("qa","v0634","preview");Directory.CreateDirectory(folder);Size c=PetRenderer.CanvasSize(300);
  var json=new StringBuilder("window.leapClips=[");int clip=0;
  using(var side=new SideWalk(c.Width,c.Height,300))using(var atlas=new Bitmap(c.Width*12,c.Height*16,PixelFormat.Format32bppPArgb))using(var g=Graphics.FromImage(atlas))
  using(var contact=new Bitmap(c.Width*8,c.Height*4))using(var cg=Graphics.FromImage(contact)) {
   cg.Clear(Color.FromArgb(238,235,230));
   foreach(int vertical in new[]{-1,1})foreach(int facing in new[]{-1,1}) {
    var motion=new LeapMotion();motion.Request(vertical,facing);motion.Begin(new Point(400,300),new Rectangle(0,0,1600,1200),c,300);
    if(clip>0)json.Append(',');json.Append("{vertical:").Append(vertical).Append(",facing:").Append(facing).Append(",duration:").Append(motion.Duration.ToString(CultureInfo.InvariantCulture)).Append(",frames:[");
    using(Bitmap original=(Bitmap)side.Draw(0,0,0,facing,1).Clone()) {
     Check(Diff(original,side.DrawLeap(facing,LeapMotion.At(0,vertical)))==0,"leap starts at exact side pose "+vertical+" / "+facing);
     Check(Diff(original,side.DrawLeap(facing,LeapMotion.At(motion.Duration,vertical)))==0,"leap ends at exact side pose "+vertical+" / "+facing);
    }
    for(int i=0;i<48;i++) {
     if(i>0)motion.Update(motion.Duration/47);
     Bitmap frame=side.DrawLeap(facing,motion.Pose);int[] pixels=PoseMesh.Read(frame);int opaque=0;
     for(int y=0;y<c.Height;y++)for(int x=0;x<c.Width;x++)if(((uint)pixels[y*c.Width+x]>>24)>20){opaque++;Quiet(x>0 && y>0 && x<c.Width-1 && y<c.Height-1,"leap artwork clipped");}
     Quiet(opaque>10000,"leap lost artwork");int index=clip*48+i;
     g.DrawImageUnscaled(frame,(index%12)*c.Width,(index/12)*c.Height);
     if(i%6==0)cg.DrawImageUnscaled(frame,(i/6)*c.Width,clip*c.Height);
     if(i>0)json.Append(',');json.Append('[').Append((motion.Position.X-motion.Start.X).ToString("F3",CultureInfo.InvariantCulture)).Append(',').Append((motion.Position.Y-motion.Start.Y).ToString("F3",CultureInfo.InvariantCulture)).Append(']');
    }
    json.Append("]}");clip++;
   }
   atlas.Save(Path.Combine(folder,"leap-atlas.png"),ImageFormat.Png);contact.Save(Path.Combine(folder,"leap-contact.png"),ImageFormat.Png);
  }
  json.Append("]; ");File.WriteAllText(Path.Combine(folder,"frames.js"),json.ToString());
  Check(true,"192 rendered leap frames retain art within canvas");
 }
}

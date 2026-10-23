using System;using System.Drawing;using JouchoPet;
class WaveCheck {
 static int checks;
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
 static void ReturnTiming() {
  foreach(double dt in new[]{1.0/60,1.0/120}) {
   var wave=new GreetingMotion();var normal=new GreetingMotion();wave.Request(true);normal.Request();
   for(int i=0;i<(int)(1/dt);i++){wave.Update(dt,true);normal.Update(dt,true);}
   Check(Math.Abs(wave.Amount-normal.Amount)<1e-12 && wave.Phase==normal.Phase,"raising and waving cadence unchanged at "+(1/dt)+" fps");
   double threshold=wave.Amount*.1,waveTime=0,normalTime=0;
   wave.Cancel();normal.Cancel();
   while(normal.Amount>threshold){normal.Update(dt,true);normalTime+=dt;}
   while(wave.Amount>threshold){wave.Update(dt,true);waveTime+=dt;}
   Check(Math.Abs(waveTime/normalTime-1/.7)<.06,"90-percent return takes 1/0.7 as long: "+normalTime.ToString("F3")+" -> "+waveTime.ToString("F3")+" seconds");
   for(int i=0;i<(int)(2/dt);i++)wave.Update(dt,true);
   Check(!wave.Active && wave.Amount==0,"slower return still finishes and releases walking");
  }
 }
 static double Length(PointF a,PointF b){return Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));}
 static double Angle(PointF a,PointF joint,PointF b){return Math.Acos(Math.Max(-1,Math.Min(1,((a.X-joint.X)*(b.X-joint.X)+(a.Y-joint.Y)*(b.Y-joint.Y))/(Length(a,joint)*Length(b,joint)))))*180/Math.PI;}
 static void WaveGeometry(int[] original,Size canvas,int size) {
  using(var arm=new GreetingArmRig(original,canvas.Width,canvas.Height,size)) {
   var rest=arm.Sample(0,0);double min=180,max=0,drift=0,opening=0,minOpening=180,handMin=180,handMax=0,armMin=180,armMax=0,elbowLeft=double.MaxValue,elbowRight=double.MinValue;
   double restOpening=Math.Atan2(rest.Shoulder.X-rest.Elbow.X,rest.Elbow.Y-rest.Shoulder.Y)*180/Math.PI;
   for(int frame=0;frame<120;frame++) {
    var pose=arm.Sample(1,frame*Math.PI/60);
    elbowLeft=Math.Min(elbowLeft,pose.Elbow.X);elbowRight=Math.Max(elbowRight,pose.Elbow.X);
    double bend=Angle(pose.Shoulder,pose.Elbow,pose.Wrist);min=Math.Min(min,bend);max=Math.Max(max,bend);
    drift=Math.Max(drift,Math.Max(Math.Abs(Length(pose.Shoulder,pose.Elbow)-Length(rest.Shoulder,rest.Elbow)),Math.Max(Math.Abs(Length(pose.Elbow,pose.Wrist)-Length(rest.Elbow,rest.Wrist)),Math.Abs(Length(pose.Wrist,pose.Tip)-Length(rest.Wrist,rest.Tip)))));
    opening=Math.Max(opening,Math.Abs(Math.Atan2(pose.Shoulder.X-pose.Elbow.X,pose.Elbow.Y-pose.Shoulder.Y)*180/Math.PI));
    minOpening=Math.Min(minOpening,Math.Abs(Math.Atan2(pose.Shoulder.X-pose.Elbow.X,pose.Elbow.Y-pose.Shoulder.Y)*180/Math.PI));
    double hand=Angle(pose.Elbow,pose.Wrist,pose.Tip);handMin=Math.Min(handMin,hand);handMax=Math.Max(handMax,hand);
    armMin=Math.Min(armMin,bend);armMax=Math.Max(armMax,bend);
   }
   Check(min>15 && max<40,"forearm folds upward with upper arm near torso "+size+" / "+min.ToString("F1")+".."+max.ToString("F1"));
   Check(minOpening>=restOpening-.001 && opening<=restOpening+5.001,"wave stays outside original resting angle "+size);
   Check(opening-minOpening>4.9 && opening-minOpening<5.1,"upper arm adds five-degree outward swing "+size);
   Check(elbowRight-elbowLeft>size*.008,"visible elbow travel "+size+" / "+(elbowRight-elbowLeft).ToString("F2")+" px");
   bool outside=true;
   foreach(double amount in new[]{0.0,.1,.4,.7,1.0})for(int frame=0;frame<120;frame++) {
    var p=arm.Sample(amount,frame*Math.PI/60);double angle=Math.Atan2(p.Shoulder.X-p.Elbow.X,p.Elbow.Y-p.Shoulder.Y)*180/Math.PI;
    if(angle<restOpening-.001 || angle>restOpening+5.001)outside=false;
   }
   Check(outside,"entry and exit never retract inside resting arm "+size);
   Check(Length(arm.Sample(1,Math.PI*1.5+.65).Elbow,rest.Elbow)<.001,"innermost wave pose equals resting upper arm "+size);
   Check(drift<.001,"shoulder elbow wrist and hand retain fixed lengths "+size);
   Check(handMax-handMin>5 && handMax-handMin<15 && armMax-armMin>3 && armMax-armMin<5,"hand has small independent wave and subtler forearm follow-through "+size);
   var start=arm.Sample(1,0);var loop=arm.Sample(1,2*Math.PI);
   Check(Length(start.Tip,loop.Tip)<.001 && Length(start.Wrist,loop.Wrist)<.001,"wave loop has no endpoint jump "+size);
  }
 }
 static int Error(int[] a,int[] b,int width,Rectangle region) {
  int error=0;for(int y=region.Top;y<region.Bottom;y++)for(int x=region.Left;x<region.Right;x++)for(int s=0;s<32;s+=8)
   error=Math.Max(error,Math.Abs((int)((uint)a[y*width+x]>>s&255)-(int)((uint)b[y*width+x]>>s&255)));
  return error;
 }
 static void Main(){try {
  ReturnTiming();
  var motion=new GreetingMotion();motion.Request(true);
  for(int i=0;i<120;i++)motion.Update(1.0/60,false);
  Check(motion.Active && motion.Amount==0,"greeting waits for front-facing awake pose");
  double previous=0,largest=0;
  for(int i=0;i<100;i++){motion.Update(1.0/60,true);largest=Math.Max(largest,Math.Abs(motion.Amount-previous));previous=motion.Amount;}
  Check(motion.Amount>.99 && largest<.12,"arm and expression rise continuously");
  double before=motion.Amount,phase=motion.Phase;motion.Request(true);
  Check(motion.Amount==before && motion.Phase==phase,"repeated greeting preserves current pose");
  motion.Cancel();Check(motion.Amount==before,"cancel does not snap arm down");
  for(int i=0;i<120;i++)motion.Update(1.0/60,true);
  Check(!motion.Active && motion.Amount==0,"cancel settles and releases walking");
  motion.Request(true);for(int i=0;i<360;i++)motion.Update(1.0/60,true);
  Check(!motion.Active && motion.Amount==0,"greeting finishes without sticking");
  foreach(int size in new[]{220,300,400}) {
   Size c=PetRenderer.CanvasSize(size);var sprites=SpriteAssets.LoadFront();
   try{using(var rig=new SoftRig(sprites,c.Width,c.Height,size)) {
    double[] neutral={1,0,0};int[] original=PoseMesh.Read(rig.Draw(0,0,0,0,0,0,0,neutral));
    WaveGeometry(original,c,size);
    int[] smiling=PoseMesh.Read(rig.Draw(0,0,0,0,0,0,1,neutral,1,0,1));
    foreach(double p in new[]{0.0,Math.PI/2,Math.PI,Math.PI*1.5}) {
     int[] waved=PoseMesh.Read(rig.Draw(0,0,0,0,0,0,1,neutral,1,0,1,0,p,1));
     Rectangle face=new Rectangle((int)(c.Width*.5-size*.04),28,(int)(size*.19),(int)(size*.29));
     int faceError=Error(smiling,waved,c.Width,face);
     Check(faceError<=1,"greeting preserves smile/closed-eye face geometry "+size+" / "+p+" / error "+faceError);
     Rectangle otherArm=new Rectangle((int)(c.Width*.5+size*.02),(int)(28+size*.37),(int)(size*.15),(int)(size*.30));
     Check(Error(smiling,waved,c.Width,otherArm)<=1,"covered left arm stays unchanged "+size+" / "+p);
     Rectangle legs=new Rectangle(0,(int)(28+size*.73),c.Width,c.Height-(int)(28+size*.73));
     Check(Error(smiling,waved,c.Width,legs)<=1,"wave does not deform legs "+size+" / "+p);
    }
    Check(Error(original,PoseMesh.Read(rig.Draw(0,0,0,0,0,0,0,neutral)),c.Width,new Rectangle(0,0,c.Width,c.Height))==0,"neutral returns exactly after greeting "+size);
    int[] left=PoseMesh.Read(rig.Draw(0,0,0,0,0,0,1,neutral,1,-1,1));
    int[] right=PoseMesh.Read(rig.Draw(0,0,0,0,0,0,1,neutral,1,1,1));
    Check(Error(left,right,c.Width,new Rectangle(0,28,c.Width,(int)(size*.29)))>10,"pat tilts head toward the touched side "+size);
    Check(Error(left,right,c.Width,new Rectangle(0,(int)(28+size*.4),c.Width,c.Height-(int)(28+size*.4)))==0,"pat tilt leaves torso and legs unchanged "+size);
   }}finally{foreach(var b in sprites)b.Dispose();}
  }
  Console.WriteLine("All "+checks+" gesture checks passed.");
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}

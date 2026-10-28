using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace JouchoPet {
 sealed class CastingRig:IDisposable {
  readonly Bitmap[] neutral=new Bitmap[2],body=new Bitmap[2];
  readonly SlashSkeleton[] bones=new SlashSkeleton[2];
  readonly SlashBodyRig[] skin=new SlashBodyRig[2];
  readonly JointArm[] arms=new JointArm[2];readonly Bitmap palm,canvas,handLayer;
  internal struct ArmNodes {internal PointF Shoulder,Elbow,Wrist,Grip;internal double Angle,UpperLength,LowerLength,Bend;}
  readonly double size;internal readonly int Padding;
  internal CastingRig(int w,int h,double s,SideWalk sides,CharacterForm form=CharacterForm.AnemoneI){
   size=s;Padding=(int)Math.Ceiling(s*.3);
   canvas=new Bitmap(w+2*Padding,h+2*Padding,PixelFormat.Format32bppPArgb);
   handLayer=new Bitmap(canvas.Width,canvas.Height,PixelFormat.Format32bppPArgb);
   for(int d=0;d<2;d++){
    neutral[d]=(Bitmap)sides.Draw(0,0,0,d==0?-1:1,1).Clone();
    using(var cut=new CarryArmRig(d,neutral[d],s,form)){
     body[d]=RigParts.BitmapOf(cut.DiagnosticBackplate,w,h);
     var def=CharacterArt.Definition(d==0?"profile-left":"profile-right");
     arms[d]=new JointArm(cut.DiagnosticArm,w,h,s,cut.Shoulder,cut.Elbow,cut.Wrist,new PointF(cut.Wrist.X,(float)CharacterArt.Y(def,d==0?311:313,s)));
    }
   }
   for(int d=0;d<2;d++){bones[d]=new SlashSkeleton(w,s,d==0?-1:1,arms[1]);skin[d]=new SlashBodyRig(w,h,s,d==0?-1:1,body[d],bones[d],form);}
   using(var source=AssetStore.Image("v0618/greeting-palm.png")){
    int l=source.Width,t=source.Height,r=0,b=0;int[] pixels=PoseMesh.Read(source);
    for(int y=0;y<source.Height;y++)for(int x=0;x<source.Width;x++)if((uint)pixels[y*source.Width+x]>>24>32){l=Math.Min(l,x);r=Math.Max(r,x);t=Math.Min(t,y);b=Math.Max(b,y);}
    palm=source.Clone(Rectangle.FromLTRB(l,t,r+1,b+1),PixelFormat.Format32bppPArgb);
   }
  }
  internal SlashSkeleton.Pose Sample(CastPose p){var b=bones[p.Body.Facing<0?0:1].Sample(p.Body);var arm=SampleArm(p,1);b.Shoulder=arm.Shoulder;b.Elbow=arm.Elbow;b.Wrist=arm.Wrist;b.Grip=arm.Grip;b.HandAngle=arm.Angle;return b;}
  static double AngleMix(double a,double b,double t){double d=b-a;while(d>Math.PI)d-=Math.PI*2;while(d<-Math.PI)d+=Math.PI*2;return a+d*t;}
  internal ArmNodes SampleArm(CastPose p,int index){
   int facing=p.Body.Facing,d=facing<0?0:1;bool near=index==d;var arm=arms[index];double q=p.Body.Amount;
   var root=new PointF((float)(facing*p.Body.RootX*size),(float)(p.Body.RootY*size));
   PointF shoulder=RigParts.Add(arms[d].Shoulder,root.X+(near?0:-facing*size*.022),root.Y+(near?0:size*.004));
   double upper=RigParts.Length(arm.Shoulder,arm.Elbow),lower=RigParts.Length(arm.Elbow,arm.Wrist);
   double restUpper=Math.Atan2(arm.Elbow.Y-arm.Shoulder.Y,arm.Elbow.X-arm.Shoulder.X),restLower=Math.Atan2(arm.Wrist.Y-arm.Elbow.Y,arm.Wrist.X-arm.Elbow.X);
   // Solve charge/release in a local horizontal plane, then aim that plane
   // at the target. Solve only these
   // existing two-bone chains; lengths and original sleeve drawings stay fixed.
   double baseUpper=(near?p.Body.Upper:30)*Math.PI/180;
   double baseBend=(near?p.Body.Elbow:-80)*Math.PI/180;
   double x=upper*Math.Cos(baseUpper)+lower*Math.Cos(baseUpper+baseBend);
   double y=upper*Math.Sin(baseUpper)+lower*Math.Sin(baseUpper+baseBend);
   double reach=upper+lower;
   double maxX=Math.Sqrt(Math.Max(0,reach*reach-y*y));
   // Lift to the previous ready position, then pull farther back while
   // charging. The final push reaches full extension without stretching bones.
   double travel=Math.Max(0,maxX-x)+size*.025;
   x+=Math.Min(size*.026,Math.Max(0,maxX-x))*(1-p.Retract)-size*.025*p.Retract+travel*p.Push;
   double distance=Math.Sqrt(x*x+y*y);
   double solvedBend=-Math.Acos(Math.Max(-1,Math.Min(1,(distance*distance-upper*upper-lower*lower)/(2*upper*lower))));
   double solvedUpper=Math.Atan2(y,x)+Math.Acos(Math.Max(-1,Math.Min(1,(upper*upper+distance*distance-lower*lower)/(2*upper*distance))));
   double ua=AngleMix(restUpper,Math.Atan2(Math.Sin(solvedUpper),facing*Math.Cos(solvedUpper)),q);
   double bend=(restLower-restUpper)*(1-q)+facing*solvedBend*q;
   // After full extension, relax the elbow while the shoulder lowers. Counter
   // rotate the upper arm to keep the wrist from flicking upward as it folds.
   double settle=Math.Max(0,Math.Min(1,(p.Body.Age-CastingMotion.PushEnd)/CastingMotion.RecoverDuration));
   double fold=(near?42:38)*Math.PI/180*16*settle*settle*(1-settle)*(1-settle);
   ua+=facing*fold*lower/(upper+lower);bend-=facing*fold;
   PointF elbow=RigParts.Add(shoulder,Math.Cos(ua)*upper,Math.Sin(ua)*upper),wrist=RigParts.Add(elbow,Math.Cos(ua+bend)*lower,Math.Sin(ua+bend)*lower);
   // Palm orientation stays steady through the push, so the hand translates
   // with the ball instead of flicking upward as the forearm opens.
   double palmAngle=(near?-18:-34)*Math.PI/180;
   double handAngle=AngleMix(restLower,Math.Atan2(Math.Sin(palmAngle),facing*Math.Cos(palmAngle)),q);
   PointF grip=RigParts.Add(wrist,Math.Cos(handAngle)*size*.035,Math.Sin(handAngle)*size*.035);
   PointF pivot=RigParts.Add(arms[d].Shoulder,root.X,root.Y);double aimAngle=facing*p.Aim*Math.PI/180*q;
   return new ArmNodes{Shoulder=RigParts.Rotate(shoulder,pivot,aimAngle),Elbow=RigParts.Rotate(elbow,pivot,aimAngle),Wrist=RigParts.Rotate(wrist,pivot,aimAngle),Grip=RigParts.Rotate(grip,pivot,aimAngle),Angle=handAngle+aimAngle,UpperLength=upper,LowerLength=lower,Bend=bend};
  }
  internal PointF Core(CastPose p){PointF mid=RigParts.Mix(SampleArm(p,0).Grip,SampleArm(p,1).Grip,.5);double angle=p.Aim*Math.PI/180;return RigParts.Add(mid,p.Body.Facing*Math.Cos(angle)*size*.0628,Math.Sin(angle)*size*.0628);}
  internal bool ResolveAim(PointF target,int facing,out double aim){
   double lo=-CastingMotion.MaxAim,hi=CastingMotion.MaxAim;
   Func<double,double> cross=angle=>{PointF core=Core(CastingMotion.At(CastingMotion.ReleaseAt,facing,angle));double a=angle*Math.PI/180;return Math.Cos(a)*(target.Y-core.Y)-Math.Sin(a)*facing*(target.X-core.X);};
   if(cross(lo)<0||cross(hi)>0){aim=0;return false;}
   for(int i=0;i<40;i++){double mid=(lo+hi)*.5;if(cross(mid)>0)lo=mid;else hi=mid;}aim=(lo+hi)*.5;
   PointF origin=Core(CastingMotion.At(CastingMotion.ReleaseAt,facing,aim));return facing*(target.X-origin.X)>size*.035;
  }
  internal double LookAim(PointF target,int facing){PointF head=Sample(CastingMotion.At(0,facing)).Head;return Math.Atan2(target.Y-head.Y,Math.Max(1,facing*(target.X-head.X)))*180/Math.PI;}
  internal CastPose PoseForTarget(double t,int facing,PointF target){double aim;if(!ResolveAim(target,facing,out aim))throw new ArgumentException("Target is outside the natural casting range.");var p=CastingMotion.At(t,facing,aim);p.HeadTilt=CastingMotion.LookTilt(LookAim(target,facing),facing,p.Body.Amount);return p;}
  static void Alpha(Graphics g,Bitmap image,RectangleF rect,double alpha){
   if(alpha<=0)return;using(var attr=new ImageAttributes()){var matrix=new ColorMatrix();matrix.Matrix33=(float)Math.Min(1,alpha);attr.SetColorMatrix(matrix);g.DrawImage(image,new[]{new PointF(rect.Left,rect.Top),new PointF(rect.Right,rect.Top),new PointF(rect.Left,rect.Bottom)},new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel,attr);}
  }
  void Arm(Graphics g,CastPose p,int index){
   var b=SampleArm(p,index);var arm=arms[index];arm.DrawUpper(g,b.Shoulder,b.Elbow);arm.DrawLowerOnly(g,b.Elbow,b.Wrist);
   double open=LeapMotion.Ease(p.Body.Amount),angle=b.Angle;
   if(open<1){using(var hg=Graphics.FromImage(handLayer)){hg.Clear(Color.Transparent);hg.TranslateTransform(Padding,Padding);hg.InterpolationMode=InterpolationMode.HighQualityBicubic;arm.DrawHandOnly(hg,b.Wrist,RigParts.Add(b.Wrist,Math.Cos(angle)*RigParts.Length(arm.Wrist,arm.Tip),Math.Sin(angle)*RigParts.Length(arm.Wrist,arm.Tip)));}Alpha(g,handLayer,new RectangleF(-Padding,-Padding,handLayer.Width,handLayer.Height),1-open);}
   var state=g.Save();g.TranslateTransform(b.Wrist.X,b.Wrist.Y);g.RotateTransform((float)(angle*180/Math.PI+90));
   float ph=(float)(size*.061),pw=ph*palm.Width/palm.Height;
   Alpha(g,palm,new RectangleF(-pw*.47f,-ph*.94f,pw,ph),open);g.Restore(state);
  }
  internal Bitmap Draw(CastPose p,bool effects=true,bool debug=false){
   int d=p.Body.Facing<0?0:1;var b=Sample(p);
   using(var g=Graphics.FromImage(canvas)){
    g.Clear(Color.Transparent);g.TranslateTransform(Padding,Padding);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.SmoothingMode=SmoothingMode.AntiAlias;
    if(p.Body.Amount<=0&&p.Body.RootY==0)g.DrawImageUnscaled(neutral[d],0,0);
    else {Arm(g,p,1-d);skin[d].DrawAdjusted(g,p.Body,b,point=>AimHead(point,p,b));Arm(g,p,d);}
    if(effects){MagicBoltArt.Charge(g,Core(p),size,p.Core,p.Body.Age);MagicBoltArt.Release(g,Core(p),size,p.ReleaseRing);}
    if(debug)using(var pen=new Pen(Color.Cyan,1)){for(int i=0;i<2;i++){var arm=SampleArm(p,i);g.DrawLines(pen,new[]{arm.Shoulder,arm.Elbow,arm.Wrist,arm.Grip,Core(p)});foreach(var point in new[]{arm.Shoulder,arm.Elbow,arm.Wrist,arm.Grip,Core(p)})g.FillEllipse(Brushes.Orange,point.X-2,point.Y-2,4,4);}}
   }return canvas;
  }
  PointF AimHead(PointF point,CastPose p,SlashSkeleton.Pose b){
   // A restrained target-directed tilt around the neck. Face and crown rotate
   // rigidly together; the lower attachment blends back into the body mesh.
   PointF neck=new PointF(b.Head.X-b.Root.X,(float)(28+size*.37));
   // Integrate the blended rotation in small steps. One large weighted
   // rotation can invert triangles far from the neck, even in transparent
   // margins of the existing full-canvas mesh.
   for(int i=0;i<16;i++){
    // The long rear hair needs a broader falloff than the neck/torso; using
    // the neck's short blend there creates a sharp shelf at large nod angles.
    double rear=LeapMotion.Ease((-p.Body.Facing*(point.X-neck.X)/size-.02)/.12);
    double end=.49+.16*rear;
    double weight=LeapMotion.Ease((28+size*end-point.Y)/(size*(end-.37)));
    point=RigParts.Rotate(point,neck,p.HeadTilt*weight/16);
   }
   return point;
  }
  public void Dispose(){foreach(var item in skin)item.Dispose();foreach(var item in body)item.Dispose();foreach(var item in neutral)item.Dispose();foreach(var arm in arms)arm.Dispose();palm.Dispose();canvas.Dispose();handLayer.Dispose();}
 }
}

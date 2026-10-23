using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace JouchoPet {
 // Retained for comparison only; the live archer now uses SideArcheryRig.cs.
 sealed class LegacyFrontArcheryRig : IDisposable {
  readonly FrontParts parts;readonly SingleSkin skin;readonly Bitmap canvas;readonly int[] plate;readonly int width,height;readonly double size;
  internal double MinimumArea=1;
  internal LegacyFrontArcheryRig(int w,int h,double s){width=w;height=h;size=s;parts=new FrontParts(w,h,s);skin=new SingleSkin(w,h);canvas=new Bitmap(w,h,PixelFormat.Format32bppPArgb);plate=new int[w*h];}
  internal PointF Turn(PointF p,ArcheryPose pose){double x=(p.X-width*.5)/size,y=(p.Y-28)/size,head=1-LeapMotion.Ease((y-.27)/.12),angle=pose.Facing*pose.Yaw*.70*(1-.28*head);
   // Bounded depth slopes keep projected vertices ordered at either yaw.
   double depth=(.035+.045*head)*Math.Max(0,1-x*x/.10);
   double tail=LeapMotion.Ease((Math.Abs(x)-.17)/.20)*LeapMotion.Ease((y-.22)/.2);depth-=tail*.02;
   return new PointF((float)(width*.5+size*(x*Math.Cos(angle)+depth*Math.Sin(angle))),p.Y);}
  internal Bitmap Draw(Bitmap live,ArcheryPose pose){if(pose.Yaw==0 && pose.Ready==0)return live;Array.Copy(parts.StandingBody,plate,plate.Length);int[] input=PoseMesh.Read(live);for(int i=0;i<plate.Length;i++)if(parts.StandingSurface[i])plate[i]=input[i];
   using(var g=Graphics.FromImage(canvas)){g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImageUnscaled(skin.Draw(plate,p=>Turn(p,pose)),0,0);MinimumArea=skin.MinimumArea;
    // Draw the far arm first; each arm retains its own sleeve and skin.
    if(pose.Facing<0){Arm(g,parts.RightArm,false,pose);Arm(g,parts.LeftArm,true,pose);}else{Arm(g,parts.LeftArm,true,pose);Arm(g,parts.RightArm,false,pose);}
   }return canvas;
  }
  static PointF Elbow(PointF shoulder,PointF wrist,double upper,double lower){double distance=RigParts.Length(shoulder,wrist);if(distance<.01)return RigParts.Add(shoulder,0,upper);double along=(upper*upper-lower*lower+distance*distance)/(2*distance);along=Math.Max(0,Math.Min(distance,along));double lift=Math.Sqrt(Math.Max(0,upper*upper-along*along));double ux=(wrist.X-shoulder.X)/distance,uy=(wrist.Y-shoulder.Y)/distance;
   PointF a=RigParts.Add(shoulder,ux*along-uy*lift,uy*along+ux*lift),b=RigParts.Add(shoulder,ux*along+uy*lift,uy*along-ux*lift);return a.Y>=b.Y?a:b;}
  void Arm(Graphics g,JointArm arm,bool holder,ArcheryPose pose){PointF shoulder=Turn(arm.Shoulder,pose),neutralElbow=Turn(arm.Elbow,pose),neutralWrist=Turn(arm.Wrist,pose),axis=ArcheryMotion.Axis(pose.Facing);
   PointF grip=ArcheryMotion.Grip(width,size,pose.Facing),ready=new PointF((float)(width*.5+pose.Facing*size*.085),(float)(28+size*.43));
   PointF goal=holder?grip:RigParts.Mix(ready,ArcheryMotion.Nock(width,size,pose),pose.Reach);
   if(!holder)goal=RigParts.Add(goal,-axis.X*size*pose.Recoil,-axis.Y*size*pose.Recoil);
   PointF wrist=RigParts.Mix(neutralWrist,goal,pose.Ready),bent=Elbow(shoulder,goal,RigParts.Length(arm.Shoulder,arm.Elbow),RigParts.Length(arm.Elbow,arm.Wrist));
   PointF elbow=RigParts.Mix(neutralElbow,bent,pose.Ready);arm.Draw(g,shoulder,elbow,wrist,holder?-.18*pose.Facing*pose.Ready:0);
  }
  public void Dispose(){parts.Dispose();skin.Dispose();canvas.Dispose();}
 }
 static class ArcheryArt {
  static readonly Color WarmWhite=Color.FromArgb(255,249,236),WhiteGold=Color.FromArgb(255,236,190),BowGold=Color.FromArgb(255,230,139);
  internal const double ReleaseRippleDuration=.36,ConjureTail=.16;
  internal const double GlowGain=2;
  static Color Fade(Color color,double alpha){return Color.FromArgb((int)Math.Round(255*Math.Max(0,Math.Min(1,alpha))),color);}
  static PointF Across(PointF axis){return new PointF(-axis.Y,axis.X);}
  // A circular arc through the same two tips and fixed grip gives the limbs
  // even curvature and gently rounded ends without changing string depth.
  // At rest the full string sits one BowDepth behind it: twice the old curve's
  // actual foremost-point depth (.0306172839506173 - -.045), not its control points.
  internal static PointF BowCurve(PointF grip,PointF axis,double size,double t){return BowCurve(grip,axis,size,t,0);}
  internal static PointF BowCurve(PointF grip,PointF axis,double size,double t,double flex){double depth=ArcheryMotion.BowDepth(size),half=size*.43,radius=(half*half+depth*depth)/(2*depth),angle=(t-.5)*2*Math.Asin(half/radius),height=radius*Math.Sin(angle),u=height/half;PointF across=Across(axis);double x=radius*(Math.Cos(angle)-1)-size*.025*flex*u*u,y=height-size*.006*flex*u*Math.Abs(u);return RigParts.Add(grip,axis.X*x+across.X*y,axis.Y*x+across.Y*y);}
  internal static double BowFlex(ArcheryPose pose){double after=pose.Age-ArcheryMotion.ReleaseAt-.14;if(after<=0)return pose.Pull;if(after>=.23)return 0;double attack=1-Math.Exp(-50*after),envelope=attack*attack*Math.Exp(-14*after)*(1-LeapMotion.Ease((after-.16)/.07));return -.28*Math.Sin(after*Math.PI*2/.10)*envelope;}
  static void Particle(Graphics g,PointF point,double radius,double alpha,bool star){Particle(g,point,radius,alpha,star,WhiteGold);}
  internal static void Particle(Graphics g,PointF point,double radius,double alpha,bool star,Color tint){if(alpha<.001||radius<.02)return;using(var glow=new SolidBrush(Fade(tint,alpha*.16*GlowGain)))g.FillEllipse(glow,(float)(point.X-radius*2.5),(float)(point.Y-radius*2.5),(float)(radius*5),(float)(radius*5));using(var core=new SolidBrush(Fade(tint,alpha*GlowGain))){if(star){float r=(float)(radius*1.6);g.FillPolygon(core,new[]{new PointF(point.X,point.Y-r),new PointF(point.X+r*.55f,point.Y),new PointF(point.X,point.Y+r),new PointF(point.X-r*.55f,point.Y)});}else g.FillEllipse(core,(float)(point.X-radius),(float)(point.Y-radius),(float)(radius*2),(float)(radius*2));}}
  static double Cycle(double t){return t-Math.Floor(t);}
  internal static void BowParticles(Graphics g,PointF grip,PointF axis,double size,ArcheryPose pose){PointF across=Across(axis);double flex=BowFlex(pose);for(int i=0;i<72;i++){double life=Cycle(pose.Age*(.42+i%4*.07)+i*.61803398875),fade=Math.Pow(Math.Sin(Math.PI*life),1.5)*pose.Bow,position=(i%24+.5+Math.Sin(i*1.7)*.2)/24;PointF point=BowCurve(grip,axis,size,position,flex);double band=i/24-1,drift=(life-.5)*size*.08+band*size*.014,flutter=Math.Sin(pose.Age*1.8+i*2.1)*size*.018;point=RigParts.Add(point,axis.X*drift+across.X*flutter,axis.Y*drift+across.Y*flutter-size*.020*life);Particle(g,point,size*(.0018+i%4*.0006),fade*.82,i%4==0,BowGold);}}
  static bool Conjuring(ArcheryPose pose){return pose.Age>=ArcheryMotion.ConjureStart&&pose.Age<ArcheryMotion.BowAppearAt+ConjureTail;}
  internal static bool HasBowEffect(ArcheryPose pose){return pose.Bow>0||Conjuring(pose);}
  static PointF ConjurePoint(PointF grip,PointF axis,double size,ArcheryPose pose,int i){const int count=54;PointF across=Across(axis),target=BowCurve(grip,axis,size,(i+.5)/count);double angle=i*2.39996322972865,radial=Math.Sqrt((i+.5)/count),axial=Math.Cos(angle)*size*.23*radial-ArcheryMotion.BowDepth(size)*.5,lateral=Math.Sin(angle)*size*.5*radial;PointF start=RigParts.Add(grip,axis.X*axial+across.X*lateral,axis.Y*axial+across.Y*lateral);PointF point=RigParts.Mix(start,target,pose.Conjure);double curl=Math.Sin(Math.PI*pose.Conjure)*(1-pose.Conjure)*size*.035;return RigParts.Add(point,Math.Cos(angle+pose.Age*3)*curl,Math.Sin(angle+pose.Age*3)*curl);}
  static void ConjureStars(Graphics g,PointF grip,PointF axis,double size,ArcheryPose pose){if(!Conjuring(pose))return;double fade=LeapMotion.Ease((pose.Age-ArcheryMotion.ConjureStart)/.08)*(1-LeapMotion.Ease((pose.Age-ArcheryMotion.BowAppearAt)/ConjureTail));for(int i=0;i<54;i++){double twinkle=.5+.5*Math.Pow(Math.Sin(i*1.7+pose.Age*8),2),radius=size*(.0025+i%4*.0011)*(.8+.35*pose.Conjure);Particle(g,ConjurePoint(grip,axis,size,pose,i),radius,fade*twinkle,i%3!=0,BowGold);}}
  internal static void Bow(Graphics g,PointF origin,int canvasWidth,double size,ArcheryPose p){if(!HasBowEffect(p))return;PointF axis=ArcheryMotion.Axis(p.Facing),grip=RigParts.Add(ArcheryMotion.Grip(canvasWidth,size,p.Facing),origin.X,origin.Y);ConjureStars(g,grip,axis,size,p);if(p.Bow<=0)return;double flex=BowFlex(p);
   BowParticles(g,grip,axis,size,p);
   // Both surfaces retain their own painted material as the anchors flex.
   PointF nock=RigParts.Add(ArcheryMotion.Nock(canvasWidth,size,p),origin.X,origin.Y);
   PaintedBowArt.Draw(g,grip,axis,nock,size,flex,p.Bow);
   if(p.Arrow>0){double length=ArcheryMotion.ArrowLength(size)*p.Arrow;PointF tip=RigParts.Add(nock,axis.X*length,axis.Y*length);Arrow(g,tip,axis,length,size,false,p.Age);}
  }
  // Six turns maintain the pitch across the shortened two-arrow span:
  // arrowhead -> nock -> one more arrow length behind the nock.
  internal const double HelixLengthFactor=2,HelixRadiusFactor=.056,HelixOpacityFactor=1;
  internal static double HelixRadius(double size,double t){return size*HelixRadiusFactor*LeapMotion.Ease(t*6)*LeapMotion.Ease((1-t)*6);}
  internal static PointF HelixPoint(PointF tip,PointF axis,double length,double size,double age,double t){PointF across=Across(axis);double radius=HelixRadius(size,t),phase=t*Math.PI*12-age*17;double sideways=Math.Sin(phase)*radius,depth=Math.Cos(phase)*radius*.14;return RigParts.Add(tip,-axis.X*(length*HelixLengthFactor*t+depth)+across.X*sideways,-axis.Y*(length*HelixLengthFactor*t+depth)+across.Y*sideways);}
  internal static void Spiral(Graphics g,PointF tip,PointF axis,double length,double size,double age,bool back){const int sections=216;for(int i=0;i<sections;i++){double t=(i+.5)/sections,depth=Math.Cos(t*Math.PI*12-age*17);if((depth<0)!=back)continue;double alpha=(back?.25+.12*(depth+1):.43+.24*depth)*HelixOpacityFactor;using(var pen=new Pen(Fade(WarmWhite,alpha),(float)Math.Max(1.7,size*.009))){pen.StartCap=pen.EndCap=LineCap.Round;g.DrawLine(pen,HelixPoint(tip,axis,length,size,age,i/(double)sections),HelixPoint(tip,axis,length,size,age,(i+1.0)/sections));}}}
  static void ArrowParticles(Graphics g,PointF tip,PointF axis,double length,double size,bool spiral,double age){PointF across=Across(axis);int count=spiral?54:18;for(int i=0;i<count;i++){double life=Cycle(age*(.9+i%3*.11)+i*.61803398875),t=(i+.5)/count,fade=Math.Sin(Math.PI*life);PointF point=spiral?HelixPoint(tip,axis,length,size,age,t):RigParts.Add(tip,-axis.X*length*t,-axis.Y*length*t);double flutter=Math.Sin(i*2.7+age*3.1)*size*.012,drift=(life-.5)*size*.025;point=RigParts.Add(point,across.X*flutter-axis.X*drift,across.Y*flutter-axis.Y*drift);Particle(g,point,size*(.0017+i%4*.00045),fade*.7*Math.Min(1,length/(size*.2)),i%7==0);}}
  static void EllipticRipple(Graphics g,PointF center,PointF axis,double radius,double alpha,double size,Color color){PointF across=Across(axis);var points=new PointF[41];for(int i=0;i<points.Length;i++){double angle=i*Math.PI*2/(points.Length-1),forward=Math.Cos(angle)*radius*.42,side=Math.Sin(angle)*radius;points[i]=RigParts.Add(center,axis.X*forward+across.X*side,axis.Y*forward+across.Y*side);}using(var pen=new Pen(Fade(color,alpha),(float)Math.Max(.7,size*.0032)))g.DrawLines(pen,points);}
  // These smaller wavefronts travel with the helix, independently of the
  // one-off release burst. Staggered fades keep each pulse continuous.
  internal static void SpiralRipples(Graphics g,PointF tip,PointF axis,double length,double size,double age){for(int i=0;i<6;i++){double life=Cycle(age/.62+i/6.0),fade=Math.Pow(Math.Sin(Math.PI*life),1.5),radius=size*(.004+.032*(1-Math.Pow(1-life,2)));PointF center=HelixPoint(tip,axis,length,size,age,(i+.7)/6.0);EllipticRipple(g,center,axis,radius,fade*.30*HelixOpacityFactor,size,WarmWhite);EllipticRipple(g,center,axis,radius*.72,fade*.14*HelixOpacityFactor,size,WhiteGold);}}
  internal static void Arrow(Graphics g,PointF tip,PointF axis,double length,double size,bool spiral,double age){if(length<=0)return;if(spiral)Spiral(g,tip,axis,length,size,age,true);PaintedArrowArt.Draw(g,tip,axis,length);if(spiral){Spiral(g,tip,axis,length,size,age,false);SpiralRipples(g,tip,axis,length,size,age);}ArrowParticles(g,tip,axis,length,size,spiral,age);}
  static void ReleaseRipple(Graphics g,MagicArrow arrow){if(arrow.Age>=ReleaseRippleDuration)return;double t=arrow.Age/ReleaseRippleDuration,fade=Math.Pow(1-t,1.7),size=arrow.Size,radius=size*(.012+.16*(1-Math.Pow(1-t,2)));PointF point=arrow.Origin;using(var pen=new Pen(Fade(WarmWhite,fade*.9),(float)(size*(.007-.004*t))))g.DrawEllipse(pen,(float)(point.X-radius),(float)(point.Y-radius),(float)(radius*2),(float)(radius*2));double inner=radius*.73;using(var pen=new Pen(Fade(WhiteGold,fade*.5),(float)Math.Max(.7,size*.003)))g.DrawEllipse(pen,(float)(point.X-inner),(float)(point.Y-inner),(float)(inner*2),(float)(inner*2));for(int i=0;i<12;i++){double angle=i*Math.PI/6+.11,spread=radius*(1.03+i%3*.06);Particle(g,RigParts.Add(point,Math.Cos(angle)*spread,Math.Sin(angle)*spread),size*.0025,fade*.72,i%3==0);}}
  internal static void Trail(Graphics g,MagicArrow arrow){
   double end=Math.Min(arrow.Age,arrow.FlightTime),start=Math.Max(0,arrow.Age-MagicArrow.TrailHold-MagicArrow.TrailFade);
   if(end<=start)return;
   var state=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;
   int count=Math.Max(1,(int)Math.Ceiling((end-start)*arrow.Size*12.8/8));
   for(int i=0;i<count;i++){
    double t=start+(end-start)*i/count,t1=start+(end-start)*(i+1)/count,alpha=arrow.TrailOpacity((t+t1)*.5);
    PointF a=arrow.TrailPoint(t),b=arrow.TrailPoint(t1);
    using(var halo=new Pen(Fade(Color.FromArgb(255,220,135),alpha*.22),(float)Math.Max(2,arrow.Size*.018)))g.DrawLine(halo,a,b);
    using(var line=new Pen(Fade(Color.FromArgb(255,241,186),alpha*.88),(float)Math.Max(1.2,arrow.Size*.005)))g.DrawLine(line,a,b);
   }
   g.Restore(state);
  }
  internal static void Projectile(Graphics g,MagicArrow arrow){if(arrow.Done)return;Trail(g,arrow);ReleaseRipple(g,arrow);if(arrow.Age<arrow.FlightTime)Arrow(g,arrow.Tip,arrow.Direction,ArcheryMotion.ArrowLength(arrow.Size),arrow.Size,true,arrow.Age);else Impact(g,arrow);}
  internal static readonly Color ImpactGold=Color.FromArgb(255,205,70);
  internal static void Impact(Graphics g,MagicArrow arrow){
   if(arrow.ImpactAge<0 || arrow.Done)return;
   for(int ring=0;ring<MagicArrow.RingCount;ring++){
    double t=arrow.ProgressAt(ring),alpha=arrow.OpacityAt(ring),radius=arrow.RadiusAt(ring);
    if(t<=0 || alpha<=0)continue;
    float stroke=(float)(arrow.Size*.009*(1-t)+.4);
    using(var halo=new Pen(Fade(ImpactGold,alpha*.18),stroke*3))g.DrawEllipse(halo,(float)(arrow.Impact.X-radius),(float)(arrow.Impact.Y-radius),(float)(2*radius),(float)(2*radius));
    using(var pen=new Pen(Fade(ImpactGold,alpha),stroke))g.DrawEllipse(pen,(float)(arrow.Impact.X-radius),(float)(arrow.Impact.Y-radius),(float)(2*radius),(float)(2*radius));
    double fade=alpha*LeapMotion.Ease(t/.06);
    for(int i=0;i<24;i++){
     double angle=i*Math.PI*2/24+ring*.31,spread=radius*(.55+.4*(i%5)/4),twinkle=.65+.35*Math.Pow(Math.Sin(i*1.7+t*14),2);
     PointF point=RigParts.Add(arrow.Impact,Math.Cos(angle)*spread,Math.Sin(angle)*spread);
     Particle(g,point,arrow.Size*(.0035+i%3*.0013),fade*twinkle,true,ImpactGold);
    }
   }
  }
  static RectangleF Around(PointF point,double radius){return new RectangleF((float)(point.X-radius),(float)(point.Y-radius),(float)(radius*2),(float)(radius*2));}
  internal static RectangleF BowBounds(PointF origin,int width,double size,ArcheryPose pose){PointF grip=RigParts.Add(ArcheryMotion.Grip(width,size,pose.Facing),origin.X,origin.Y),axis=ArcheryMotion.Axis(pose.Facing);double flex=BowFlex(pose);RectangleF bounds=Around(grip,size*.09);for(int i=0;i<=12;i++)bounds=RectangleF.Union(bounds,Around(BowCurve(grip,axis,size,i/12.0,flex),size*.09));if(Conjuring(pose))for(int i=0;i<54;i++)bounds=RectangleF.Union(bounds,Around(ConjurePoint(grip,axis,size,pose,i),size*.025));PointF nock=RigParts.Add(ArcheryMotion.Nock(width,size,pose),origin.X,origin.Y);bounds=RectangleF.Union(bounds,Around(nock,size*.06));if(pose.Arrow>0){double length=ArcheryMotion.ArrowLength(size)*pose.Arrow;PointF tip=RigParts.Add(nock,axis.X*length,axis.Y*length);bounds=RectangleF.Union(bounds,Around(tip,size*.06));bounds=RectangleF.Union(bounds,PaintedArrowArt.Bounds(tip,axis,length));}return bounds;}
  internal static RectangleF ProjectileBounds(MagicArrow arrow){RectangleF bounds;if(arrow.Age<arrow.FlightTime){double length=ArcheryMotion.ArrowLength(arrow.Size);PointF tail=RigParts.Add(arrow.Tip,-arrow.Direction.X*length*HelixLengthFactor,-arrow.Direction.Y*length*HelixLengthFactor);bounds=RectangleF.Union(Around(arrow.Tip,arrow.Size*.125),Around(tail,arrow.Size*.125));bounds=RectangleF.Union(bounds,PaintedArrowArt.Bounds(arrow.Tip,arrow.Direction,length));}else bounds=Around(arrow.Impact,arrow.Size*.80);if(arrow.Age<ReleaseRippleDuration)bounds=RectangleF.Union(bounds,Around(arrow.Origin,arrow.Size*.21));double start=Math.Max(0,arrow.Age-MagicArrow.TrailHold-MagicArrow.TrailFade),end=Math.Min(arrow.Age,arrow.FlightTime);if(end>start){bounds=RectangleF.Union(bounds,Around(arrow.TrailPoint(start),arrow.Size*.02));bounds=RectangleF.Union(bounds,Around(arrow.TrailPoint(end),arrow.Size*.02));}return bounds;}
 }
}

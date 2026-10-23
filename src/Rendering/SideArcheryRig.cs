using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace JouchoPet {
 // Start and finish on the exact SideWalk surface used by TurnRig endpoints.
 // Arms and a small planted stance move independently; head/hair/torso stay fixed.
 sealed class ArcheryRig : IDisposable {
  internal struct ArmPose {internal PointF Shoulder,Elbow,Wrist,Finger;internal double Depth,LowerDepth,HandDepth,UpperLength,LowerLength,HandLength;}
  readonly Bitmap[] neutral=new Bitmap[2],body=new Bitmap[2];
  readonly JointArm[] arms=new JointArm[2];
  readonly ArcheryStance[] stance=new ArcheryStance[2];
  readonly Bitmap canvas;readonly int width;readonly double size;
  internal double MinimumArea=1;
  internal ArcheryRig(int w,int h,double s,SideWalk sides){width=w;size=s;canvas=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
   try{PaintedBowArt.Prepare();for(int d=0;d<2;d++){
    neutral[d]=(Bitmap)sides.Draw(0,0,0,d==0?-1:1,1).Clone();
    using(var cut=new CarryArmRig(d,neutral[d],s)){
     body[d]=RigParts.BitmapOf(cut.DiagnosticBackplate,w,h);
     stance[d]=new ArcheryStance(w,h,s,d==0?-1:1,body[d]);
     var definition=CharacterArt.Definition(d==0?"profile-left":"profile-right");
     PointF tip=new PointF(cut.Wrist.X,(float)CharacterArt.Y(definition,311,s));
     if(d==1)tip=new PointF((float)(w*.5+(1602-definition.Anchor)*CharacterArt.Scale(definition,s)),(float)CharacterArt.Y(definition,313,s));
     arms[d]=new JointArm(cut.DiagnosticArm,w,h,s,cut.Shoulder,cut.Elbow,cut.Wrist,tip);
    }
   }}catch{Dispose();throw;}
  }
  internal Bitmap Draw(ArcheryPose pose){int d=pose.Facing<0?0:1;if(pose.Ready<=0){MinimumArea=1;return neutral[d];}MinimumArea=double.MaxValue;
   JointArm near=arms[d],far=arms[1-d];
   PointF farShoulder=RigParts.Add(near.Shoulder,-pose.Facing*size*.022,size*.004);
   PointF farWrist=RigParts.Add(farShoulder,far.Wrist.X-far.Shoulder.X,far.Wrist.Y-far.Shoulder.Y);
   ArmPose farPose=SampleArm(far,d==1,farShoulder,farWrist,pose),nearPose=SampleArm(near,d==0,near.Shoulder,near.Wrist,pose);
   using(var g=Graphics.FromImage(canvas)){g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
    far.DrawToFinger(g,farPose.Shoulder,farPose.Elbow,farPose.Wrist,farPose.Finger);
    g.DrawImageUnscaled(stance[d].Draw(pose.Ready),0,0);
    // Profile-left: covered left arm is near. Profile-right: bare right arm
    // is near. The opposite arm stays behind the complete body/head surface.
    near.DrawToFinger(g,nearPose.Shoulder,nearPose.Elbow,nearPose.Wrist,nearPose.Finger);
   }return canvas;
  }
  // The holding arm keeps its original planar lift. The drawing arm solves
  // shoulder/elbow/finger contact in depth, with an almost level rearward elbow.
  ArmPose SampleArm(JointArm arm,bool holder,PointF shoulder,PointF neutralWrist,ArcheryPose pose){
   if(!holder)return DrawingArm(arm,shoulder,neutralWrist,pose);
   PointF goal=ArcheryMotion.BowWrist(width,size,pose.Facing);
   double upper=RigParts.Length(arm.Shoulder,arm.Elbow),lower=RigParts.Length(arm.Elbow,arm.Wrist);
   double startAngle=Math.Atan2(neutralWrist.Y-shoulder.Y,neutralWrist.X-shoulder.X),endAngle=Math.Atan2(goal.Y-shoulder.Y,goal.X-shoulder.X);
   double delta=endAngle-startAngle;while(delta>Math.PI)delta-=Math.PI*2;while(delta<-Math.PI)delta+=Math.PI*2;
   double angle=startAngle+delta*pose.Ready;
   double radius=RigParts.Length(shoulder,neutralWrist)*(1-pose.Ready)+RigParts.Length(shoulder,goal)*pose.Ready;
   radius=Math.Max(Math.Abs(upper-lower)+.01,Math.Min(upper+lower-.001,radius));
   PointF wrist=RigParts.Add(shoulder,Math.Cos(angle)*radius,Math.Sin(angle)*radius);
   double along=(upper*upper-lower*lower+radius*radius)/(2*radius),bend=Math.Sqrt(Math.Max(0,upper*upper-along*along));
   double side=pose.Facing,depth=0;
   PointF elbow=RigParts.Add(shoulder,Math.Cos(angle)*along-Math.Sin(angle)*bend*side,Math.Sin(angle)*along+Math.Cos(angle)*bend*side);
   MinimumArea=Math.Min(MinimumArea,Math.Min(RigParts.Length(shoulder,elbow)/upper,RigParts.Length(elbow,wrist)/lower));
   double restHand=Math.Atan2(arm.Tip.Y-arm.Wrist.Y,arm.Tip.X-arm.Wrist.X),aimHand=Math.Atan2(ArcheryMotion.Axis(pose.Facing).Y,ArcheryMotion.Axis(pose.Facing).X);
   double handDelta=aimHand-restHand;while(handDelta>Math.PI)handDelta-=2*Math.PI;while(handDelta<-Math.PI)handDelta+=2*Math.PI;
   double handAngle=restHand+handDelta*pose.Ready,handLength=RigParts.Length(arm.Wrist,arm.Tip);
   PointF finger=RigParts.Add(wrist,Math.Cos(handAngle)*handLength,Math.Sin(handAngle)*handLength);
   return new ArmPose{Shoulder=shoulder,Elbow=elbow,Wrist=wrist,Finger=finger,Depth=depth,UpperLength=upper,LowerLength=lower,HandLength=handLength};
  }
  ArmPose DrawingArm(JointArm arm,PointF restShoulder,PointF neutralWrist,ArcheryPose pose){
   if(pose.Lower>0)return LowerDrawingArm(arm,restShoulder,neutralWrist,pose);
   double lift=pose.Ready,upper=RigParts.Length(arm.Shoulder,arm.Elbow),lower=RigParts.Length(arm.Elbow,arm.Wrist),hand=RigParts.Length(arm.Wrist,arm.Tip);
   PointF neutralFinger=RigParts.Add(neutralWrist,arm.Tip.X-arm.Wrist.X,arm.Tip.Y-arm.Wrist.Y);
   PointF restElbow=RigParts.Add(restShoulder,arm.Elbow.X-arm.Shoulder.X,arm.Elbow.Y-arm.Shoulder.Y);
   if(lift<=1e-8)return new ArmPose{Shoulder=restShoulder,Elbow=restElbow,Wrist=neutralWrist,Finger=neutralFinger,UpperLength=upper,LowerLength=lower,HandLength=hand};
   PointF shoulder=RigParts.Add(restShoulder,-pose.Facing*size*(.008*pose.Draw+.003*pose.Release)*lift,0);
   var restPose=pose;restPose.Pull=0;
   PointF ready=RigParts.Add(ArcheryMotion.Nock(width,size,restPose),-pose.Facing*size*.018,size*.025);
   PointF goal=RigParts.Mix(ready,ArcheryMotion.DrawingHand(width,size,pose),pose.Reach);
   double start=Math.Atan2(neutralFinger.Y-restShoulder.Y,neutralFinger.X-restShoulder.X),end=Math.Atan2(goal.Y-shoulder.Y,goal.X-shoulder.X),delta=end-start;
   while(delta>Math.PI)delta-=2*Math.PI;while(delta<-Math.PI)delta+=2*Math.PI;
   double angle=start+delta*lift,radius=RigParts.Length(restShoulder,neutralFinger)*(1-lift)+RigParts.Length(shoulder,goal)*lift;
   PointF finger=RigParts.Add(shoulder,Math.Cos(angle)*radius,Math.Sin(angle)*radius);
   double fz=pose.Facing*size*.11*Math.Sqrt(lift);
   // Preserve the original small wrist angle at rest, then align the hooked
   // fingers with the forearm. This makes the visible fingertip the IK endpoint.
   double restBend=Math.Atan2(arm.Tip.Y-arm.Wrist.Y,arm.Tip.X-arm.Wrist.X)-Math.Atan2(arm.Wrist.Y-arm.Elbow.Y,arm.Wrist.X-arm.Elbow.X);
   double span=Math.Sqrt(lower*lower+hand*hand+2*lower*hand*Math.Cos(restBend*(1-lift)));
   double dx=finger.X-shoulder.X,dy=finger.Y-shoulder.Y,dz=fz,distance=Math.Sqrt(dx*dx+dy*dy+dz*dz);
   double ux=dx/distance,uy=dy/distance,uz=dz/distance;
   double reach=Math.Min(upper+span-.000001,Math.Max(Math.Abs(upper-span)+.000001,distance));
   if(Math.Abs(reach-distance)>.000001){distance=reach;finger=RigParts.Add(shoulder,ux*distance,uy*distance);fz=uz*distance;}
   double along=(upper*upper-span*span+distance*distance)/(2*distance),bend=Math.Sqrt(Math.Max(0,upper*upper-along*along));
   double cx=shoulder.X+ux*along,cy=shoulder.Y+uy*along,cz=uz*along;
   double vertical=Math.Sqrt(Math.Max(1e-12,1-uy*uy));
   double vx=-ux*uy/vertical,vy=vertical,vz=-uz*uy/vertical,qx=-uz/vertical,qz=ux/vertical;
   // Keep the drawing elbow visibly below the shoulder for an upward shot.
   // The hook starts only slightly lower, so the pull still travels backwards
   // almost horizontally while the registered fingertip stays on the string.
   double raisedY=size*(.060+.002*(1-LeapMotion.Ease((pose.Draw-.35)/.65)));
   double elbowLift=lift*(1-pose.Lower);
   double elbowY=shoulder.Y+(restElbow.Y-restShoulder.Y)*(1-elbowLift)+raisedY*elbowLift;
   double k=bend<1e-8?0:Math.Max(-1,Math.Min(1,(elbowY-cy)/(bend*vertical)));
   double outward=Math.Sqrt(Math.Max(0,1-k*k))*(qx*pose.Facing>0?-1:1);
   PointF elbow=new PointF((float)(cx+bend*(k*vx+outward*qx)),(float)(cy+bend*k*vy));
   double ez=cz+bend*(k*vz+outward*qz);
   // Locate the wrist on the same two-sphere chain; all three bone lengths
   // stay fixed even while perspective shortens the visible upper arm.
   double ex=finger.X-elbow.X,ey=finger.Y-elbow.Y,ed=fz-ez,actualSpan=Math.Sqrt(ex*ex+ey*ey+ed*ed);
   double wristBend=lower*hand*Math.Abs(Math.Sin(restBend*(1-lift)))/span;
   double wristAlong=Math.Sqrt(Math.Max(0,lower*lower-wristBend*wristBend));
   double projected=Math.Max(.000001,Math.Sqrt(ex*ex+ey*ey));
   double originalCross=(arm.Tip.X-arm.Elbow.X)*(arm.Wrist.Y-arm.Elbow.Y)-(arm.Tip.Y-arm.Elbow.Y)*(arm.Wrist.X-arm.Elbow.X);
   double wristSide=originalCross<0?-1:1;
   PointF wrist=RigParts.Add(elbow,ex/actualSpan*wristAlong-ey/projected*wristBend*wristSide,ey/actualSpan*wristAlong+ex/projected*wristBend*wristSide);
   double wz=ez+ed/actualSpan*wristAlong;
   MinimumArea=Math.Min(MinimumArea,Math.Min(RigParts.Length(shoulder,elbow)/upper,Math.Min(RigParts.Length(elbow,wrist)/lower,RigParts.Length(wrist,finger)/hand)));
   return new ArmPose{Shoulder=shoulder,Elbow=elbow,Wrist=wrist,Finger=finger,Depth=ez,LowerDepth=wz-ez,HandDepth=fz-wz,UpperLength=upper,LowerLength=lower,HandLength=hand};
  }
  struct Vector3 {
   internal double X,Y,Z;internal Vector3(double x,double y,double z){X=x;Y=y;Z=z;}
   internal Vector3 Plus(Vector3 v){return new Vector3(X+v.X,Y+v.Y,Z+v.Z);}
   internal Vector3 Times(double s){return new Vector3(X*s,Y*s,Z*s);}
   internal double Dot(Vector3 v){return X*v.X+Y*v.Y+Z*v.Z;}
   internal Vector3 Cross(Vector3 v){return new Vector3(Y*v.Z-Z*v.Y,Z*v.X-X*v.Z,X*v.Y-Y*v.X);}
   internal double Length {get{return Math.Sqrt(Dot(this));}}
   internal Vector3 Unit(){double length=Length;return length<1e-12?new Vector3(1,0,0):Times(1/length);}
  }
  static double Acos(double value){return Math.Acos(Math.Max(-1,Math.Min(1,value)));}
  static Vector3 Between(PointF a,PointF b,double z){return new Vector3(b.X-a.X,b.Y-a.Y,z);}
  static Vector3 Rotate3(Vector3 v,Vector3 axis,double angle){double c=Math.Cos(angle),s=Math.Sin(angle);return v.Times(c).Plus(axis.Cross(v).Times(s)).Plus(axis.Times(axis.Dot(v)*(1-c)));}
  static Vector3 Transport(Vector3 v,Vector3 from,Vector3 to){Vector3 axis=from.Cross(to);double length=axis.Length;if(length<1e-10)return v;return Rotate3(v,axis.Times(1/length),Acos(from.Dot(to)));}
  static Vector3 Slerp(Vector3 from,Vector3 to,double t){double angle=Acos(from.Dot(to));if(angle<1e-8)return from;double sine=Math.Sin(angle);return from.Times(Math.Sin((1-t)*angle)/sine).Plus(to.Times(Math.Sin(t*angle)/sine)).Unit();}
  ArmPose LowerDrawingArm(JointArm arm,PointF restShoulder,PointF neutralWrist,ArcheryPose pose){
   // Lower from a fixed follow-through pose, never by reversing the fingertip
   // orbit or switching the inverse-kinematics elbow branch.
   var held=ArcheryMotion.At(3.15,pose.Facing);held.Lower=0;held.Ready=1;
   ArmPose start=DrawingArm(arm,restShoulder,neutralWrist,held);
   double t=pose.Lower,elbowT=LeapMotion.Ease((t-.06)/.94),handT=LeapMotion.Ease((t-.10)/.90);
   Vector3 u0=Between(start.Shoulder,start.Elbow,start.Depth).Unit(),v0=Between(start.Elbow,start.Wrist,start.LowerDepth).Unit();
   Vector3 u1=Between(arm.Shoulder,arm.Elbow,0).Unit(),v1=Between(arm.Elbow,arm.Wrist,0).Unit(),h1=Between(arm.Wrist,arm.Tip,0).Unit();
   // Interpolate elevation monotonically. A great-circle interpolation can
   // dip past the resting elbow and then lift it again near the end.
   double elevation0=Math.Asin(Math.Max(-1,Math.Min(1,u0.Y))),elevation1=Math.Asin(Math.Max(-1,Math.Min(1,u1.Y)));
   Vector3 orbit=Slerp(u0,u1,t);
   double elevation=elevation0+(elevation1-elevation0)*t,yaw=Math.Atan2(orbit.Z,orbit.X);
   Vector3 u=new Vector3(Math.Cos(elevation)*Math.Cos(yaw),Math.Sin(elevation),Math.Cos(elevation)*Math.Sin(yaw));
   double bend0=Acos(u0.Dot(v0)),bend1=Acos(u1.Dot(v1));
   Vector3 pole0=v0.Plus(u0.Times(-Math.Cos(bend0))).Unit(),pole1=v1.Plus(u1.Times(-Math.Cos(bend1))).Unit();
   Vector3 endPole=Transport(pole0,u0,u1),pole=Transport(pole0,u0,u);
   double twist=Math.Atan2(u1.Dot(endPole.Cross(pole1)),endPole.Dot(pole1));
   pole=Rotate3(pole,u,twist*t);
   // The elbow opens monotonically. Independent world-space forearm rotation
   // can pass through a fully folded elbow and make the joint turn inside out.
   double bend=bend0+(bend1-bend0)*elbowT;
   Vector3 v=u.Times(Math.Cos(bend)).Plus(pole.Times(Math.Sin(bend))).Unit();
   double wristBend=Acos(v1.Dot(h1));Vector3 handPole=h1.Plus(v1.Times(-Math.Cos(wristBend))).Unit();handPole=Transport(handPole,v1,v);
   Vector3 h=v.Times(Math.Cos(wristBend*handT)).Plus(handPole.Times(Math.Sin(wristBend*handT))).Unit();
   PointF shoulder=RigParts.Mix(start.Shoulder,restShoulder,t);
   PointF elbow=RigParts.Add(shoulder,u.X*start.UpperLength,u.Y*start.UpperLength);
   PointF wrist=RigParts.Add(elbow,v.X*start.LowerLength,v.Y*start.LowerLength);
   PointF finger=RigParts.Add(wrist,h.X*start.HandLength,h.Y*start.HandLength);
   MinimumArea=Math.Min(MinimumArea,Math.Min(Math.Sqrt(u.X*u.X+u.Y*u.Y),Math.Min(Math.Sqrt(v.X*v.X+v.Y*v.Y),Math.Sqrt(h.X*h.X+h.Y*h.Y))));
   return new ArmPose{Shoulder=shoulder,Elbow=elbow,Wrist=wrist,Finger=finger,Depth=u.Z*start.UpperLength,LowerDepth=v.Z*start.LowerLength,HandDepth=h.Z*start.HandLength,UpperLength=start.UpperLength,LowerLength=start.LowerLength,HandLength=start.HandLength};
  }
  internal ArmPose DiagnosticArm(ArcheryPose pose,bool holder){int d=pose.Facing<0?0:1;bool isNear=holder==(d==0);JointArm near=arms[d],arm=isNear?near:arms[1-d];
   PointF shoulder=isNear?near.Shoulder:RigParts.Add(near.Shoulder,-pose.Facing*size*.022,size*.004);
   PointF wrist=isNear?near.Wrist:RigParts.Add(shoulder,arm.Wrist.X-arm.Shoulder.X,arm.Wrist.Y-arm.Shoulder.Y);
   return SampleArm(arm,holder,shoulder,wrist,pose);
  }
  public void Dispose(){foreach(var b in neutral)if(b!=null)b.Dispose();foreach(var b in body)if(b!=null)b.Dispose();foreach(var a in arms)if(a!=null)a.Dispose();foreach(var s in stance)if(s!=null)s.Dispose();canvas.Dispose();}
 }
}

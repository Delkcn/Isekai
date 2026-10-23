using System;
using System.Drawing;
namespace JouchoPet {
 // Small action-specific 2D hierarchy, using the existing registered body and
 // JointArm bind pivots. It does not replace the project's locomotion skeleton.
 sealed class SlashSkeleton {
  internal struct Leg {internal PointF Hip,Knee,Ankle,Foot;}
  internal struct Pose {
   internal PointF Root,Chest,Head,Shoulder,Elbow,Wrist,Grip;
   internal double HandAngle,UpperAngle,ElbowAngle,WristAngle;
   internal Leg Near,Far;
  }
  readonly double size,upperLength,lowerLength,restUpper,restBend;
  readonly int width,facing;
  readonly PointF shoulder;
  internal SlashSkeleton(int w,double s,int direction,JointArm arm){
   width=w;size=s;facing=direction;
   upperLength=RigParts.Length(arm.Shoulder,arm.Elbow);lowerLength=RigParts.Length(arm.Elbow,arm.Wrist);
   restUpper=Math.Atan2(arm.Elbow.Y-arm.Shoulder.Y,arm.Elbow.X-arm.Shoulder.X)*180/Math.PI;
   restBend=Math.Atan2(arm.Wrist.Y-arm.Elbow.Y,arm.Wrist.X-arm.Elbow.X)*180/Math.PI-restUpper;
   var def=CharacterArt.Definition("profile-left");double scale=CharacterArt.Scale(def,s);
   shoulder=facing>0?arm.Shoulder:new PointF((float)(w*.5+(171-def.Anchor)*scale+s*.022),(float)CharacterArt.Y(def,184,s));
  }
  static double Clamp(double v,double lo,double hi){return Math.Max(lo,Math.Min(hi,v));}
  PointF End(PointF p,double length,double degrees){double a=degrees*Math.PI/180;return RigParts.Add(p,facing*Math.Cos(a)*length,Math.Sin(a)*length);}
  internal PointF BindHip(int part){return new PointF((float)(width*.5+(part==0?-1:1)*size*.012),(float)(28+size*.64));}
  internal PointF BindKnee(int part){return RigParts.Add(BindHip(part),0,size*.125);}
  internal PointF BindAnkle(int part){return RigParts.Add(BindHip(part),0,size*.25);}
  internal PointF BindFoot(int part){return RigParts.Add(BindAnkle(part),0,size*.09);}
  internal Pose Sample(SwordPose p){
   var root=new PointF((float)(facing*p.RootX*size),(float)(p.RootY*size));
   double upper=restUpper+(Clamp(p.Upper,-20,125)-restUpper)*p.Amount;
   double bend=restBend+(Clamp(p.Elbow,-105,0)-restBend)*p.Amount;
   double wrist=Clamp(p.Wrist,-25,30)*p.Amount;
   PointF sh=RigParts.Add(shoulder,root.X,root.Y),el=End(sh,upperLength,upper),wr=End(el,lowerLength,upper+bend),gr=End(wr,size*.035,upper+bend+wrist);
   return new Pose{Root=root,Chest=new PointF((float)(width*.5+root.X),(float)(28+size*.43+root.Y)),Head=new PointF((float)(width*.5+root.X),(float)(28+size*.25+root.Y)),Shoulder=sh,Elbow=el,Wrist=wr,Grip=gr,HandAngle=(upper+bend+wrist)*Math.PI/180,UpperAngle=upper,ElbowAngle=bend,WristAngle=wrist,Far=SolveLeg(0,root),Near=SolveLeg(1,root)};
  }
  Leg SolveLeg(int part,PointF root){
   PointF hip=RigParts.Add(BindHip(part),root.X,root.Y),ankle=BindAnkle(part);
   double u=size*.125,l=size*.125,dx=ankle.X-hip.X,dy=ankle.Y-hip.Y,d=Math.Sqrt(dx*dx+dy*dy);
   if(d>u+l+.001)throw new InvalidOperationException("Planted leg target is outside its fixed bone lengths.");
   double a=(u*u-l*l+d*d)/(2*d),h=Math.Sqrt(Math.Max(0,u*u-a*a));
   PointF knee=RigParts.Add(hip,dx*a/d+facing*dy*h/d,dy*a/d-facing*dx*h/d);
   return new Leg{Hip=hip,Knee=knee,Ankle=ankle,Foot=BindFoot(part)};
  }
  internal PointF SkinBody(PointF point,SwordPose p){
   // Root translation is rigid. A tiny delayed hair/skirt bone rotation blends
   // at its attachment; no y scaling or per-scanline leg deformation is used.
   double rear=Math.Max(0,Math.Min(1,(-facing*(point.X-width*.5)-size*.04)/(size*.12)));
   double lower=Math.Max(0,Math.Min(1,(point.Y-28-size*.35)/(size*.24)));
   PointF pivot=new PointF(width*.5f,(float)(28+size*.27));
   PointF lag=RigParts.Rotate(point,pivot,facing*p.Lag*rear*lower);
   return RigParts.Add(lag,facing*p.RootX*size,p.RootY*size);
  }
 }
}

using System;
using System.Drawing;
namespace JouchoPet {
 // The free foot lifts and follows the ongoing turn, without a staged preparation.
 static class PivotTurn {
  public static double Progress(double t){t=Math.Max(0,Math.Min(1,t));return .5-.5*Math.Cos(Math.PI*t);}
  public static double Lift(double t){return LeapMotion.Ease(t/.14)*(1-LeapMotion.Ease((t-.88)/.12));}
  public static double Weight(double t){return LeapMotion.Ease(t/.10)*(1-LeapMotion.Ease((t-.90)/.10));}
  public static PointF Pose(PointF p,double hip,double ground,double near,double far,PointF support,PointF anchor,double progress,bool supportNear,double size) {
   double lift=Lift(progress),weight=Weight(progress),mid=(near+far)*.5;
   double side=LeapMotion.Ease((p.X-mid+size*.018)/(size*.036));
   double free=supportNear?side:1-side;
   double along=LeapMotion.Ease((p.Y-hip)/(ground-hip));
   // Keep trailing hair outside the legs out of the knee deformation; below
   // the hair tips the whole free boot is lifted, including its toe outline.
   double center=supportNear?far:near;
   double legMask=1-LeapMotion.Ease((Math.Abs(p.X-center)-size*.07)/(size*.055));
   legMask+=(1-legMask)*LeapMotion.Ease((p.Y-(ground-size*.13))/(size*.06));
   free*=legMask;
   double x=p.X+(anchor.X-support.X)*weight;
   double y=p.Y-size*(AnimationParameters.Get("turnLift")/100)*lift*along*free-size*.003*weight*(1-along);
   double pivotX=support.X+(anchor.X-support.X)*weight;
   double angle=(supportNear?-1:1)*AnimationParameters.Get("turnLean")*Math.PI/180*weight;
   double dx=x-pivotX,dy=y-ground;
   return new PointF((float)(pivotX+dx*Math.Cos(angle)-dy*Math.Sin(angle)),(float)(ground+dx*Math.Sin(angle)+dy*Math.Cos(angle)));
  }
 }
}

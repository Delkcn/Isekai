using System;
using System.Drawing;
namespace JouchoPet {
 // Gait and contact timing retained from 0.5.19; only artwork binding changed.
 sealed class SideWalk : IDisposable {
  readonly WalkArt art;
  public SideWalk(int w,int h,int petHeight){art=new WalkArt(w,h,petHeight);}
  public struct Foot { public double X,Lift; public bool Planted; }
  public const double Stance=0.70;
  public const double LegHeightRatio=0.34;
  public const double ToeOffAngleDegrees=25;
  public const double MaxForwardAngleDegrees=20;
  // Toe-off geometry: rearward hip-to-foot offset = leg height * tan(25 deg).
  // Derive travel and phase from this same distance, rather than tuning a stride.
  public static readonly double StrideRatio=LegHeightRatio*(Math.Tan(ToeOffAngleDegrees*Math.PI/180)+Math.Tan(MaxForwardAngleDegrees*Math.PI/180));
  public static readonly double LandingFraction=Math.Tan(MaxForwardAngleDegrees*Math.PI/180)/
   (Math.Tan(ToeOffAngleDegrees*Math.PI/180)+Math.Tan(MaxForwardAngleDegrees*Math.PI/180));
  public const double RearRecoveryBias=0.22;
  public static Foot Sample(double phase,double stride,double lift) {
   double p=phase-Math.Floor(phase);
   if(p<Stance) return new Foot { X=stride*(LandingFraction-p/Stance),Lift=0,Planted=true };
   double q=(p-Stance)/(1-Stance);
   // Hermite endpoint tangent continues the backward stance velocity before
   // the rear leg recovers. This deliberately delays the forward swing.
   double f=q*q*(3-2*q)-(1-Stance)/Stance*q*(1-q)*(1-2*q);
   // Keep the recovering foot behind the hip through mid-swing. This bump
   // vanishes with zero derivative at both ends, preserving planted speed
   // and a smooth touchdown without moving the entire gait backward.
   f-=RearRecoveryBias*16*q*q*(1-q)*(1-q);
   return new Foot { X=stride*(f+LandingFraction-1),Lift=lift*Math.Pow(Math.Sin(Math.PI*q),2),Planted=false };
  }
  public static Foot LimitForward(Foot foot,double hipHeight) {
   // Use the actual hip-to-raised-foot height: a fixed X limit alone permits
   // excessive forward angles when the foot is lifted off the ground.
   double maxX=Math.Max(0,hipHeight-foot.Lift)*Math.Tan(MaxForwardAngleDegrees*Math.PI/180);
   foot.X=Math.Min(foot.X,maxX);
   return foot;
  }
  public Bitmap Draw(double time,double phase,double strength,int facing,double settle,double carry=0,double effort=0,double vertical=0,double run=0,bool runningCarry=false) {
   return art.Draw(time,phase,strength,facing,settle,carry,effort,vertical,run,runningCarry);
  }
  public void Dispose(){art.Dispose();}
  public Bitmap DrawLeap(int facing,LeapPose pose,double carry=0,double effort=0,double vertical=0){return art.DrawLeap(facing,pose,carry,effort,vertical);}
 }
}

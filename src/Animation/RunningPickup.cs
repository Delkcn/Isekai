using System;using System.Drawing;
namespace JouchoPet {
 struct PickupSample {public PointF Pet;public Point Icon;public double Distance,Velocity,Grip;}
 // The same distance clock spans approach and pushing: pickup never restarts it.
 sealed class RunningPickup {
  readonly Point start,contact,iconStart,iconEnd;
  readonly CarryTravel clock;
  readonly double[] arc=new double[129];
  readonly double arcLength,reach;
  readonly int facing;
  public readonly int ApproachDistance;
  public double Duration {get{return clock.Duration;}}
  public RunningPickup(Point start,Point contact,Point iconStart,Point iconEnd,int size,double initialSpeed) {
   this.start=start;this.contact=contact;this.iconStart=iconStart;this.iconEnd=iconEnd;facing=iconEnd.X>=iconStart.X?1:-1;
   PointF prior=Curve(0);for(int i=1;i<arc.Length;i++){PointF p=Curve(i/128.0);arc[i]=arc[i-1]+Math.Sqrt(Math.Pow(p.X-prior.X,2)+Math.Pow(p.Y-prior.Y,2));prior=p;}
   arcLength=arc[128];ApproachDistance=(int)Math.Ceiling(arcLength);
   reach=Math.Min(ApproachDistance,Math.Max(size*.16,RunGait.SpeedAt300*size/300*.42));
   clock=new CarryTravel(Point.Empty,new Point(ApproachDistance+Math.Abs(iconEnd.X-iconStart.X),0),size,CarryStyle.Run,initialSpeed);
  }
  PointF Curve(double u){return new PointF((float)(start.X+(contact.X-start.X)*u),(float)(start.Y+(contact.Y-start.Y)*u*u*(3-2*u)));}
  public PickupSample At(double age) {
   CarrySample step=clock.At(age);var s=new PickupSample{Distance=step.Distance,Velocity=step.Velocity,Icon=iconStart};
   if(step.Distance<ApproachDistance && ApproachDistance>0) {
    double target=step.Distance/ApproachDistance*arcLength;int i=1;while(i<128 && arc[i]<target)i++;
    double u=(i-1+(arc[i]>arc[i-1]?(target-arc[i-1])/(arc[i]-arc[i-1]):0))/128;
    s.Pet=Curve(u);s.Grip=LeapMotion.Ease(1-(ApproachDistance-step.Distance)/Math.Max(1,reach));
   }else {
    double pushed=Math.Max(0,Math.Min(Math.Abs(iconEnd.X-iconStart.X),step.Distance-ApproachDistance));
    s.Pet=new PointF((float)(contact.X+facing*pushed),contact.Y);s.Icon=new Point(iconStart.X+facing*(int)Math.Round(pushed),iconStart.Y);s.Grip=1;
   }
   return s;
  }
 }
}

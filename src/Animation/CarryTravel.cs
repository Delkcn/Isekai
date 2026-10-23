using System;using System.Drawing;
namespace JouchoPet {
 enum CarryStyle { Walk,Run,Leap }
 struct CarrySample {
  public Point Position;
  public double Distance,Velocity;
  public LeapPose Leap;
 }
 // One timeline drives Explorer's accepted position and the carrying pose.
 // Parameters are captured on pickup so a live edit cannot move the endpoint.
 sealed class CarryTravel {
  public readonly Point Start,End;
  public readonly CarryStyle Style;
  public readonly int Vertical;
  public readonly double Duration;
  readonly double length,speed,ramp,windup,flight,startSpeed;
  public CarryTravel(Point start,Point end,int size,CarryStyle style,double initialSpeed=0) {
   Start=start;End=end;length=IconCarryPlan.Distance(start,end);Vertical=Math.Sign(end.Y-start.Y);
   Style=Vertical!=0?CarryStyle.Leap:style==CarryStyle.Run?CarryStyle.Run:CarryStyle.Walk;
   if(Style==CarryStyle.Leap){windup=Vertical<0?.38:.32;flight=Vertical<0?.95:.85;Duration=windup+flight+.62;}
   else if(Style==CarryStyle.Run){speed=RunGait.SpeedAt300*size/300;startSpeed=Math.Max(0,Math.Min(speed,initialSpeed));ramp=Math.Min(.22,2*length/(startSpeed+2*speed));Duration=length/speed+ramp*(1-startSpeed/(2*speed));}
   else Duration=IconCarryPlan.Duration(start,end);
  }
  static double Integral(double t){t=Math.Max(0,Math.Min(1,t));return t*t*t*t*(2.5+t*(-3+t));}
  public CarrySample At(double age) {
   double t=Math.Max(0,Math.Min(Duration,age)),distance=0,velocity=0;var sample=new CarrySample();
   if(Style==CarryStyle.Leap) {
    double u=Math.Max(0,Math.Min(1,(t-windup)/flight));distance=length*LeapMotion.Ease(u);velocity=length/flight*30*u*u*(1-u)*(1-u);sample.Leap=LeapMotion.At(t,Vertical);
   }else if(Style==CarryStyle.Run && length>0) {
    if(t<ramp){distance=startSpeed*t+(speed-startSpeed)*ramp*Integral(t/ramp);velocity=startSpeed+(speed-startSpeed)*LeapMotion.Ease(t/ramp);}
    else if(t>Duration-ramp){double remaining=Duration-t;distance=length-speed*ramp*Integral(remaining/ramp);velocity=speed*LeapMotion.Ease(remaining/ramp);}
    else{distance=(startSpeed+speed)*ramp*.5+speed*(t-ramp);velocity=speed;}
   }else {
    double u=Duration>0?t/Duration:1;distance=length*IconCarryPlan.Smooth(u);velocity=length/Duration*30*u*u*(1-u)*(1-u);
   }
   if(age<=0){distance=0;velocity=startSpeed;}if(age>=Duration){distance=length;velocity=0;}
   double fraction=length>0?distance/length:1;
   sample.Position=new Point(Start.X+(int)Math.Round((End.X-Start.X)*fraction),Start.Y+(int)Math.Round((End.Y-Start.Y)*fraction));
   sample.Distance=distance;sample.Velocity=velocity;return sample;
  }
 }
}

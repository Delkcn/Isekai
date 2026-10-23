using System;
namespace JouchoPet {
 // A short, springy run using the approved directional leg layers.
 static class RunGait {
  public const double Stance=.42;
  public static double SpeedAt300 {get{return AnimationParameters.Get("runSpeed");}}
  public static double StrideRatio {get{return AnimationParameters.Get("stride")/100;}}
  public static double Contact(double blend){return SideWalk.Stance+(Stance-SideWalk.Stance)*blend;}
  public static double Stride(double size,double blend){return size*(SideWalk.StrideRatio+(StrideRatio-SideWalk.StrideRatio)*blend);}
  public static SideWalk.Foot Sample(double phase,double size,double blend) {
   if(blend<=0)return SideWalk.LimitForward(SideWalk.Sample(phase,size*SideWalk.StrideRatio,size*.026),size*SideWalk.LegHeightRatio);
   double stance=Contact(blend),stride=Stride(size,blend),p=phase-Math.Floor(phase);
   if(p<stance)return new SideWalk.Foot{X=stride*(SideWalk.LandingFraction-p/stance),Planted=true};
   double q=(p-stance)/(1-stance);
   double f=q*q*(3-2*q)-(1-stance)/stance*q*(1-q)*(1-2*q);
   f-=(SideWalk.RearRecoveryBias*(1-blend)+.08*blend)*16*q*q*(1-q)*(1-q);
   var foot=new SideWalk.Foot{X=stride*(f+SideWalk.LandingFraction-1),Lift=size*(.026+(AnimationParameters.Get("footLift")/100-.026)*blend)*Math.Pow(Math.Sin(Math.PI*q),2)};
   return SideWalk.LimitForward(foot,size*SideWalk.LegHeightRatio);
  }
  public static double Rise(double phase,double size,double blend) {
   return size*(AnimationParameters.Get("bodyRise")/100)*blend*Math.Pow(Math.Sin(2*Math.PI*(phase-Contact(blend)*.5)),2);
  }
  // A small forward knee offset, with extra softness while bearing weight.
  // The renderer fades it to zero at hip and sole, preserving foot placement.
  public static double KneeFlex(double phase,double size,double blend) {
   double p=phase-Math.Floor(phase),stance=Contact(blend);
   double load=p<stance?Math.Pow(Math.Sin(Math.PI*p/stance),2):0;
   return size*blend*(AnimationParameters.Get("kneeBase")/100+AnimationParameters.Get("kneeLoad")/100*load);
  }
  // The visible arm belongs to the near leg's side. Positive means that leg
  // leads, so its arm must swing back while the opposite arm leads.
  public static double ArmSwing(double phase) {
   return (Sample(phase,1,1).X-Sample(phase+.5,1,1).X)/Stride(1,1);
  }
 }
}

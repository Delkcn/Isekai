using System;
namespace JouchoPet {
 static class PoseInterpolation {
  public static float Tangent(float incoming,float outgoing) {
   if(incoming*outgoing<=0)return 0;
   float average=(incoming+outgoing)*0.5f;
   return Math.Sign(average)*Math.Min(Math.Abs(average),2*Math.Min(Math.Abs(incoming),Math.Abs(outgoing)));
  }
  public static double Hermite(double displacement,double start,double end,double t) {
   double t2=t*t,t3=t2*t;
   return (3*t2-2*t3)*displacement+(t3-2*t2+t)*start+(t3-t2)*end;
  }
 }
}

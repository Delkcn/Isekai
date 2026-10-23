using System;
namespace JouchoPet {
 static class SmileRig {
  // Landmarks calibrated on rig/front.png, in the normalized source face.
  // Apply BEFORE the head deformation is sampled: face and mouth share one map.
  // A single closed lip curve; no mouth cavity, skin patch or second overlay.
  public static uint Apply(uint pixel,double x,double y,double width,double size,double amount) {
   amount=Math.Max(0,Math.Min(1,amount));if(amount==0)return pixel;
   // The face is asymmetric under the fringe: chin/lip center is right of canvas center.
   double cx=width*.5,cy=28+size*.2725;
   double radius=size*.012,u=(x-cx)/radius;
   if(Math.Abs(u)>1.2)return pixel;
   // Keep the middle of the lip fixed and lift only the corners a little more.
   double lip=cy+size*(.0022-.0048*u*u);
   double edge=Math.Max(0,Math.Min(1,(1.15-Math.Abs(u))*6));
   double cover=amount*.76*edge*Math.Max(0,Math.Min(1,1-Math.Abs(y-lip)/Math.Max(.65,size*.0022)));
   if(cover<=0)return pixel;
   uint alpha=pixel>>24;uint result=alpha<<24;
   int[] ink={68,74,125};
   for(int c=0;c<3;c++)result|=(uint)Math.Round(((pixel>>(8*c))&255)*(1-cover)+ink[c]*alpha/255.0*cover)<<(8*c);
   return result;
  }
 }
}

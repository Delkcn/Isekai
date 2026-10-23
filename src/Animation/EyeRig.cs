using System;
namespace JouchoPet {
 // Coordinates belong to the original front artwork. All eye ink, including
 // upper/outer eyelashes, travels with the lids; no replacement eyelid overlay.
 static class EyeRig {
  static readonly double[] Original={180,195,205,238,247,259};
  static readonly double[] Closed={180,218,228,228.15,237.15,259};
  static double Smooth(double x) {x=Math.Max(0,Math.Min(1,x));return x*x*(3-2*x);}
  public static double SourceY(double x,double y,double closure) {
   if(closure<=0 || y<=180 || y>=259 || x<=301 || x>=398)return y;
   double mask=Smooth((x-301)/8)*(1-Smooth((x-390)/8));
   double amount=Math.Max(0,Math.Min(1,closure))*mask;
   // Upper lash thickness is preserved. Only the eye aperture collapses;
   // the lower lash rises to meet the upper one. Surrounding skin stays pinned.
   for(int i=0;i<Original.Length-1;i++) {
    double a=Original[i]+amount*(Closed[i]-Original[i]);
    double b=Original[i+1]+amount*(Closed[i+1]-Original[i+1]);
    if(y<=b)return Original[i]+(y-a)*(Original[i+1]-Original[i])/(b-a);
   }
   return y;
  }
 }
}

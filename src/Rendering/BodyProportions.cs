using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace JouchoPet {
 // Walking is the fixed reference. Normalize only front/turn artwork by
 // anatomical landmarks, never by the transparent canvas or hair bounds alone.
 static class BodyProportions {
  public const double Neck=0.34,Hip=0.64,Sole=0.98;
  public static double MapY(double y,double neck,double hip,double sole) {
   if(y<=neck)return y/neck*Neck;
   if(y<=hip)return Neck+(y-neck)/(hip-neck)*(Hip-Neck);
   return Hip+(y-hip)/(sole-hip)*(Sole-Hip);
  }
  public static double SourceY(double normalized,double neck,double hip,double sole) {
   if(normalized<=Neck)return normalized/Neck*neck;
   if(normalized<=Hip)return neck+(normalized-Neck)/(Hip-Neck)*(hip-neck);
   return hip+(normalized-Hip)/(Sole-Hip)*(sole-hip);
  }
  public static double WidthScale(double normalized,double neck,double sole) {
   // Preserve the head's aspect ratio while reducing its oversized frontal art.
   double head=(Neck/neck)/(Sole/sole);
   double t=Math.Max(0,Math.Min(1,(normalized-Neck)/(Hip-Neck)));t=t*t*(3-2*t);
   return head+(1-head)*t;
  }
  public static double MapX(double x,double y,int width,double size,double neck,double hip,double sole,double anchor) {
   double n=MapY(y,neck,hip,sole);
   return width*0.5+(x-anchor)*size*Sole/sole*WidthScale(n,neck,sole);
  }
  public static void Draw(Graphics g,Bitmap src,int width,double size,double neck,double hip,double sole,double anchor) {
   g.InterpolationMode=InterpolationMode.HighQualityBicubic;
   double nominal=size*Sole/sole;
   int rows=(int)Math.Ceiling(size*MapY(src.Height,neck,hip,sole));
   for(int y=0;y<rows;y++) {
    double n=y/size,sy=SourceY(n,neck,hip,sole),ey=SourceY((y+1)/size,neck,hip,sole);
    if(sy>=src.Height)break;
    double sx=nominal*WidthScale(n,neck,sole);
    g.DrawImage(src,new RectangleF((float)(width*0.5-anchor*sx),28+y,(float)(src.Width*sx),1),
     new RectangleF(0,(float)Math.Max(0,sy),src.Width,(float)Math.Min(ey-sy,src.Height-sy)),GraphicsUnit.Pixel);
   }
  }
 }
}

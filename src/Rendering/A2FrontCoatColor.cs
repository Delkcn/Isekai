using System;
using System.Drawing;
namespace JouchoPet {
 // Material-only correction in the canonical front import. Keep registration,
 // opacity and all side paintings intact; the existing rigs reuse this material.
 static class A2FrontCoatColor {
  static PointF[] Points(params float[] xy){var p=new PointF[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new PointF(xy[i*2],xy[i*2+1]);return p;}
  static readonly PointF[][] Panels={
   Points(174,160,205,160,209,174,219,180,224,190,227,200,230,210,232,220,235,230,239,240,243,250,247,260,250,272,235,280,264,360,222,318,191,278,180,327,110,363,137,303,155,258,166,238,141,238,141,217,150,213,162,184,173,174),
   Points(110,363,155,339,156,354,159,395,141,390),
   Points(181,326,188,316,197,324,198,400,182,399),
   Points(221,324,264,360,239,378,221,386)
  };
  static bool Inside(PointF[] p,int x,int y){bool inside=false;for(int i=0,j=p.Length-1;i<p.Length;j=i++)if((p[i].Y>y)!=(p[j].Y>y)&&x<(p[j].X-p[i].X)*(y-p[i].Y)/(p[j].Y-p[i].Y)+p[i].X)inside=!inside;return inside;}
  static double Clamp(double t){return Math.Max(0,Math.Min(1,t));}
  internal static void Apply(int[] pixels,int width,int height){
   if(width!=380||height!=465)throw new ArgumentException("Front coat palette requires canonical A2 dimensions.");
   for(int y=160;y<400;y++)for(int x=109;x<265;x++){
    bool cloth=false;foreach(var panel in Panels)if(Inside(panel,x,y)){cloth=true;break;}if(!cloth)continue;
    int k=y*width+x;uint p=(uint)pixels[k];int a=(int)(p>>24),r=(int)(p>>16&255),g=(int)(p>>8&255),b=(int)(p&255);
    // Neutral cloth only: exclude skin, warm hair, cyan locks and dark trim.
    if(a<200)continue;
    double weight=Clamp((Math.Min(r,Math.Min(g,b))-125)/55.0)*Clamp((18-Math.Abs(r-g))/8.0)*Clamp((22-Math.Abs(r-b))/8.0)*Clamp((24-Math.Abs(g-b))/8.0);
    if(weight<=0)continue;
    // Opaque midtone samples: front 231/228/229, sides about 233/220/215.
    r=(int)Math.Round(r+2*weight);g=(int)Math.Round(g-8*weight);b=(int)Math.Round(b-14*weight);
    pixels[k]=(int)((p&0xff000000)|((uint)Math.Min(255,r)<<16)|((uint)Math.Max(0,g)<<8)|(uint)Math.Max(0,b));
   }
  }
 }
}

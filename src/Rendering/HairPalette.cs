using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet {
 // Only derived sleep inbetweens are matched to the approved first painting.
 // The proportion-corrected board and its walking/turning views bypass this correction.
 // This is a load-time color correction, not another exported frame sequence.
 static class HairPalette {
  static readonly double[][] references=new double[2][]; static readonly object sync=new object();
  static double Mask(uint p,double x,double y) {
   double a=p>>24;if(a<32)return 0;
   double r=((p>>16)&255)*255/a,g=((p>>8)&255)*255/a,b=(p&255)*255/a;
   if(y>.90 || (y>.17 && Math.Abs(x)<.20))return 0;
   if(r<75 || g<r*.65 || b<g*.70 || r<=g || g<=b)return 0;
   return Math.Min(1,Math.Max(0,(r-b-8)/12))*Math.Min(1,(g/r-.65)/.08);
  }
  static double[] Mean(int[] p,int w,int h,double size,double crown) {
   var sum=new double[4];
   for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
    uint v=(uint)p[y*w+x];double m=Mask(v,(x-w*.5)/size,(y-28)/size-crown);
    if(m==0)continue;double a=v>>24;
    for(int c=0;c<3;c++)sum[c]+=((v>>(8*c))&255)*255/a*m;
    sum[3]+=m;
   }
   for(int c=0;c<3;c++)sum[c]/=Math.Max(1,sum[3]);return sum;
  }
  public static void Match(Bitmap picture,double size,double crown,CharacterForm form=CharacterForm.AnemoneI) { CharacterForms.Validate(form);double[] reference;lock(sync){reference=references[(int)form];
   if(reference==null) {
    Size c=PetRenderer.CanvasSize(300);
    using(var first=CharacterArt.Normalized("profile-left",c.Width,c.Height,300,form))reference=Mean(PoseMesh.Read(first),c.Width,c.Height,300,0);
   }references[(int)form]=reference;}
   int w=picture.Width,h=picture.Height;int[] pixels=PoseMesh.Read(picture);
   double[] mean=Mean(pixels,w,h,size,crown);if(mean[3]<20)return;
   for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
    int k=y*w+x;uint p=(uint)pixels[k];double mask=Mask(p,(x-w*.5)/size,(y-28)/size-crown);if(mask==0)continue;
    uint a=p>>24,value=a<<24;
    for(int c=0;c<3;c++) {
     double shift=Math.Max(-40,Math.Min(40,reference[c]-mean[c]));
     double v=((p>>(8*c))&255)+shift*mask*a/255;
     value|=(uint)Math.Round(Math.Max(0,Math.Min(a,v)))<<(8*c);
    }
    pixels[k]=(int)value;
   }
   var data=picture.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
   try{for(int y=0;y<h;y++)Marshal.Copy(pixels,y*w,IntPtr.Add(data.Scan0,y*data.Stride),w);}finally{picture.UnlockBits(data);}
  }
 }
}

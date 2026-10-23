using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet
{
    // The turn atlas has long hair in the same rows as the legs. Normalize
    // ONLY the outer hair field; keep neck, torso and leg anchors unchanged.
    static class TurnHair
    {
        public const double Tip=0.79; // walking body layer height is 0.80
        static double Smooth(double x){x=Math.Max(0,Math.Min(1,x));return x*x*(3-2*x);}
        public static double Weight(double x,double center,double size) {
            return Smooth((Math.Abs(x-center)/size-0.11)/0.10);
        }
        public static double SourceY(double y,double weight,double sourceTip) {
            if(y<=0.34)return y;
            return y+(y-0.34)*weight*(sourceTip-Tip)/(Tip-0.34);
        }
        public static unsafe void Normalize(Bitmap picture,double size) {
            int w=picture.Width,h=picture.Height;int[] pixels=new int[w*h];
            Rectangle bounds=new Rectangle(0,0,w,h);
            BitmapData input=picture.LockBits(bounds,ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);
            try{Marshal.Copy(input.Scan0,pixels,0,pixels.Length);}finally{picture.UnlockBits(input);}
            int bottom=0;
            for(int y=(int)(28+size*.5);y<Math.Min(h,28+size*.95);y++) {
                int hits=0;
                for(int x=0;x<w;x++) {
                    if(Weight(x,w*.5,size)<.7)continue;
                    uint p=(uint)pixels[y*w+x];int a=(int)(p>>24),r=(int)((p>>16)&255),g=(int)((p>>8)&255),b=(int)(p&255);
                    if(a>220 && r>85 && r>g && g>b && r-b>12 && g>r*.62 && b>g*.65)hits++;
                }
                if(hits>=Math.Max(3,(int)(size*.015)))bottom=y;
            }
            double tip=Math.Max(Tip,Math.Min(.94,(bottom-28)/size));
            if(tip<=Tip+.005)return;
            BitmapData output=picture.LockBits(bounds,ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
            try {
                for(int y=0;y<h;y++) {
                    uint* row=(uint*)((byte*)output.Scan0+y*output.Stride);
                    for(int x=0;x<w;x++) {
                        double sy=28+size*SourceY((y-28)/size,Weight(x,w*.5,size),tip);
                        int iy=(int)Math.Floor(sy);double f=sy-iy;
                        if(iy<0 || iy>=h-1){row[x]=0;continue;}
                        uint a=(uint)pixels[iy*w+x],b=(uint)pixels[(iy+1)*w+x],value=0;
                        for(int shift=0;shift<32;shift+=8)value|=(uint)Math.Round(((a>>shift)&255)*(1-f)+((b>>shift)&255)*f)<<shift;
                        row[x]=value;
                    }
                }
            }finally{picture.UnlockBits(output);}
        }
    }
}

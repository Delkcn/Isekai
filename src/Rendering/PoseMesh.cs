using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet {
 // Registered 2D poses. Legacy mode selects a drawing at half-key;
 // sleep mode aligns scanline silhouettes and blends premultiplied samples.
 // Neither mode is a 3D skeletal model or an optical-flow solver.
 sealed class PoseMesh : IDisposable {
  internal sealed class Pose {public int[] Pixels;public PointF[] Points;public double[] Left,Right;}
  const int Columns=9;
  readonly int width,height;
  readonly Pose[] poses;
  readonly Bitmap canvas;
  readonly double[] rows;
  readonly bool continuous;
  public double MinimumArea=1;
  internal Pose[] DiagnosticKeys {get{return poses;}}
  public PoseMesh(Pose[] keys,int w,int h,bool continuousMorph=false) {
   if(keys==null || keys.Length<2)throw new ArgumentException("At least two poses are required");
   poses=keys;width=w;height=h;continuous=continuousMorph;rows=new double[keys[0].Points.Length/Columns];
   foreach(Pose p in keys)if(p.Points.Length!=rows.Length*Columns || p.Pixels.Length!=w*h)throw new ArgumentException("Inconsistent pose topology");
   canvas=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
   if(continuous)foreach(Pose p in poses)Contours(p);
  }
  internal static int[] Read(Bitmap bitmap) {
   int[] p=new int[bitmap.Width*bitmap.Height];
   var d=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);
   try{for(int y=0;y<bitmap.Height;y++)Marshal.Copy(IntPtr.Add(d.Scan0,y*d.Stride),p,y*bitmap.Width,bitmap.Width);}finally{bitmap.UnlockBits(d);}
   for(int i=0;i<p.Length;i++)if(((uint)p[i]>>24)<12)p[i]=0;
   return p;
  }
  public void UpdateFirst(Bitmap live){poses[0].Pixels=Read(live);if(continuous)Contours(poses[0]);}
  void Contours(Pose p) {
   p.Left=new double[height];p.Right=new double[height];
   for(int y=0;y<height;y++) {
    int l=width,r=-1;
    for(int x=0;x<width;x++)if(((uint)p.Pixels[y*width+x]>>24)>80){l=Math.Min(l,x);r=x;}
    p.Left[y]=r>=l?l:width*.5-1;p.Right[y]=r>=l?r:width*.5+1;
   }
   // Smooth scanline silhouettes so strands cannot introduce local creases.
   double[] left=(double[])p.Left.Clone(),right=(double[])p.Right.Clone();
   for(int y=0;y<height;y++) {
    double l=0,r=0,n=0;
    for(int j=Math.Max(0,y-3);j<=Math.Min(height-1,y+3);j++){l+=left[j];r+=right[j];n++;}
    p.Left[y]=l/n;p.Right[y]=r/n;
   }
  }
  double Edge(double[] values,double y) {
   y=Math.Max(0,Math.Min(height-1.001,y));int i=(int)y;return values[i]+(values[i+1]-values[i])*(y-i);
  }
  uint Sample(Pose p,double x,double y) {
   int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);if(ix<0||iy<0||ix>=width-1||iy>=height-1)return 0;
   if((p.Pixels[iy*width+ix]|p.Pixels[iy*width+ix+1]|p.Pixels[(iy+1)*width+ix]|p.Pixels[(iy+1)*width+ix+1])==0)return 0;
   double fx=x-ix,fy=y-iy;uint value=0;
   for(int c=0;c<32;c+=8) {
    double a=((uint)p.Pixels[iy*width+ix]>>c)&255,b=((uint)p.Pixels[iy*width+ix+1]>>c)&255;
    double d=((uint)p.Pixels[(iy+1)*width+ix]>>c)&255,e=((uint)p.Pixels[(iy+1)*width+ix+1]>>c)&255;
    value|=(uint)Math.Round((a+(b-a)*fx)*(1-fy)+(d+(e-d)*fx)*fy)<<c;
   }
   return value;
  }
  internal static Pose Register(Bitmap bitmap,double[] bands,double center,double eyeX,double eyeY) {
   var points=new PointF[bands.Length*Columns];
   for(int r=0;r<bands.Length;r++)for(int c=0;c<Columns;c++)points[r*Columns+c]=new PointF((float)(c*(bitmap.Width-1.0)/(Columns-1)),(float)bands[r]);
   return new Pose {Pixels=Read(bitmap),Points=points};
  }
  internal static double Coordinate(double before,double a,double b,double after,double t) {
   double delta=b-a,m0=PoseInterpolation.Tangent((float)(a-before),(float)delta),m1=PoseInterpolation.Tangent((float)delta,(float)(after-b));
   return a+PoseInterpolation.Hermite(delta,m0,m1,t);
  }
  public unsafe Bitmap Draw(double position) {
   position=Math.Max(0,Math.Min(poses.Length-1,position));
   int index=Math.Min(poses.Length-2,(int)position);double t=position-index;
   Pose a=poses[index],b=poses[index+1],prev=poses[Math.Max(0,index-1)],next=poses[Math.Min(poses.Length-1,index+2)],art=t<.5?a:b;
   MinimumArea=double.MaxValue;
   for(int r=0;r<rows.Length;r++) {
    int k=r*Columns;rows[r]=Coordinate(prev.Points[k].Y,a.Points[k].Y,b.Points[k].Y,next.Points[k].Y,t);
    if(r>0)MinimumArea=Math.Min(MinimumArea,(rows[r]-rows[r-1])*(width-1.0)/(Columns-1));
   }
   if(MinimumArea<=0)throw new InvalidOperationException("Animation landmark rows are out of order");
   var data=canvas.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
   try {
    if(t<.0000001 || t>.9999999) {
     for(int y=0;y<height;y++)Marshal.Copy(art.Pixels,y*width,IntPtr.Add(data.Scan0,y*data.Stride),width);
    }else if(continuous) {
     int band=0;
     for(int y=0;y<height;y++) {
      uint* output=(uint*)((byte*)data.Scan0+y*data.Stride);
      while(band<rows.Length-2 && y>rows[band+1])band++;
      double f=(y-rows[band])/(rows[band+1]-rows[band]);int k=band*Columns;
      double ay=a.Points[k].Y+f*(a.Points[k+Columns].Y-a.Points[k].Y),by=b.Points[k].Y+f*(b.Points[k+Columns].Y-b.Points[k].Y);
      double al=Edge(a.Left,ay),ar=Edge(a.Right,ay),bl=Edge(b.Left,by),br=Edge(b.Right,by);
      double py=prev.Points[k].Y+f*(prev.Points[k+Columns].Y-prev.Points[k].Y),ny=next.Points[k].Y+f*(next.Points[k+Columns].Y-next.Points[k].Y);
      double pl=Edge(prev.Left,py),pr=Edge(prev.Right,py),nl=Edge(next.Left,ny),nr=Edge(next.Right,ny);
      // One shared tangent at each key prevents the silhouette restarting its
      // easing curve eight times during a single sit-down or stand-up motion.
      double l=Coordinate(pl,al,bl,nl,t),r=Coordinate(pr,ar,br,nr,t),span=Math.Max(2,r-l);
      for(int x=0;x<width;x++) {
       double u=(x-l)/span;uint p=Sample(a,al+u*(ar-al),ay),q=Sample(b,bl+u*(br-bl),by);
       uint before=Sample(prev,pl+u*(pr-pl),py),after=Sample(next,nl+u*(nr-nl),ny),v=0;
       if(p==q && before==p && after==p){output[x]=p;continue;}
       for(int s=0;s<32;s+=8)v|=(uint)Math.Max(0,Math.Min(255,Math.Round(Coordinate((before>>s)&255,(p>>s)&255,(q>>s)&255,(after>>s)&255,t))))<<s;
       uint alpha=v>>24;for(int s=0;s<24;s+=8)if((v>>s&255)>alpha)v=(v&~(255u<<s))|(alpha<<s);
       output[x]=v;
      }
     }
    }else fixed(int* source=art.Pixels) {
     int band=0;
     for(int y=0;y<height;y++) {
      uint* output=(uint*)((byte*)data.Scan0+y*data.Stride);
      while(band<rows.Length-2 && y>rows[band+1])band++;
      double f=(y-rows[band])/(rows[band+1]-rows[band]);
      double sy=art.Points[band*Columns].Y+f*(art.Points[(band+1)*Columns].Y-art.Points[band*Columns].Y);
      int iy=(int)Math.Floor(sy),fraction=(int)((sy-iy)*256);
      if(iy<0 || iy>=height-1){for(int x=0;x<width;x++)output[x]=0;continue;}
      for(int x=0;x<width;x++) {
       uint p=(uint)source[iy*width+x],q=(uint)source[(iy+1)*width+x],value=0;
       for(int shift=0;shift<32;shift+=8)value|=(uint)((((p>>shift)&255)*(256-fraction)+((q>>shift)&255)*fraction)>>8)<<shift;
       output[x]=value;
      }
     }
    }
   }finally{canvas.UnlockBits(data);}
   return canvas;
  }
  public void Dispose(){canvas.Dispose();}
 }
}

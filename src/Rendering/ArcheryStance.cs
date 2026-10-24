using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet {
 // Archery-only stance. Reuse both complete painted leg layers from WalkArt;
 // never split a visible boot, mirror its costume or deform the upper body.
 sealed class ArcheryStance : IDisposable {
  static readonly PointF[][] LegBoundary={
   new[]{new PointF(143,311),new PointF(177,317),new PointF(199,358),new PointF(200,411),new PointF(210,435),new PointF(210,455),new PointF(103,455),new PointF(105,425),new PointF(133,407)},
   new[]{new PointF(1603,308),new PointF(1644,316),new PointF(1650,410),new PointF(1671,438),new PointF(1667,455),new PointF(1577,455),new PointF(1577,428),new PointF(1598,410)}
  };
  static readonly Rectangle[] LegCuts={new Rectangle(233,31,237,584),new Rectangle(752,30,241,584),new Rectangle(229,688,248,580),new Rectangle(744,691,244,578)};
  static readonly int[,] LegIndex={{2,0},{3,1}};
  readonly int width,height,direction;readonly double size,hip,ground;
  readonly Bitmap original,foreground,canvas,legCanvas;
  readonly Bitmap[] legs=new Bitmap[2];readonly int[][] legPixels=new int[2][];
  readonly int[] originalPixels;
  internal ArcheryStance(int w,int h,double s,int facing,Bitmap backplate) {
   width=w;height=h;size=s;direction=facing<=0?-1:1;hip=28+s*(.98-SideWalk.LegHeightRatio);ground=28+s*.98;
   original=(Bitmap)backplate.Clone();originalPixels=Read(original);
   canvas=new Bitmap(w,h,PixelFormat.Format32bppPArgb);legCanvas=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
   try {
    int d=direction<0?0:1;
    using(var atlas=AssetStore.Image("v064/walk-legs.png"))for(int part=0;part<2;part++)using(var painted=atlas.Clone(LegCuts[LegIndex[d,part]],PixelFormat.Format32bppArgb)) {
     legs[part]=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
     double legHeight=s*SideWalk.LegHeightRatio,legWidth=painted.Width*legHeight/painted.Height,pivot=d==0?.70:.30,center=w*.5+(part==0?-1:1)*s*.012;
     using(var g=Graphics.FromImage(legs[part])) {g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(painted,new RectangleF((float)(center-legWidth*pivot),(float)(ground-legHeight),(float)legWidth,(float)legHeight));}
     legPixels[part]=Read(legs[part]);
    }
    // Recover only the original foreground alpha, retaining the supplied coat
    // backplate (including its arm removal) and every original hair/skirt edge.
    using(var torso=Torso(d))using(var restLegs=new Bitmap(w,h,PixelFormat.Format32bppPArgb)) {
     using(var g=Graphics.FromImage(restLegs)){g.DrawImageUnscaled(legs[0],0,0);g.DrawImageUnscaled(legs[1],0,0);}
     int[] mask=Read(torso),below=Read(restLegs),pixels=(int[])originalPixels.Clone();
     for(int y=(int)Math.Floor(hip);y<h;y++)for(int x=0;x<w;x++) {
      int k=y*w+x;uint a=(uint)mask[k]>>24;if(((uint)below[k]>>24)==0)continue;
      uint result=a<<24;
      for(int shift=0;shift<24;shift+=8) {
       int channel=(int)((uint)originalPixels[k]>>shift&255)-(int)Math.Round(((uint)below[k]>>shift&255)*(255-a)/255.0);
       result|=(uint)Math.Max(0,Math.Min((int)a,channel))<<shift;
      }
      pixels[k]=(int)result;
     }
     foreground=RigParts.BitmapOf(pixels,w,h);
    }
   }catch{Dispose();throw;}
  }
  Bitmap Torso(int d) {
   using(var source=CharacterArt.Load(d==0?"profile-left":"profile-right"))using(var path=new GraphicsPath()) {
    var definition=(CharacterArt.Pose)source.Tag;path.AddPolygon(LegBoundary[d]);
    RectangleF legBounds=path.GetBounds();legBounds.Inflate(1,1);
    var pixels=Read(source);
    for(int y=0;y<source.Height;y++)for(int x=0;x<source.Width;x++) {
     int gx=x+definition.Cell.X,gy=y+definition.Cell.Y;
     bool boot=gy>395 && gx>=(d==0?95:1560) && gx<=(d==0?220:1675);
     if(boot||(legBounds.Contains(gx,gy)&&path.IsVisible(gx,gy)))pixels[y*source.Width+x]=0;
    }
    using(var isolated=RigParts.BitmapOf(pixels,source.Width,source.Height)){isolated.Tag=definition;return CharacterArt.Normalize(isolated,width,height,size);}
   }
  }
  internal Bitmap Draw(double amount) {
   amount=Math.Max(0,Math.Min(1,amount));if(amount==0)return original;
   using(var g=Graphics.FromImage(canvas)) {
    g.Clear(Color.Transparent);
    DrawLeg(g,0,amount);DrawLeg(g,1,amount);g.DrawImageUnscaled(foreground,0,0);
   }
   // Upper-body coverage is copied exactly, not resampled or repainted.
   var bits=canvas.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
   try{for(int y=0;y<Math.Min(height,(int)Math.Ceiling(hip));y++)Marshal.Copy(originalPixels,y*width,IntPtr.Add(bits.Scan0,y*bits.Stride),width);}finally{canvas.UnlockBits(bits);}
   return canvas;
  }
  double Shift(int part,double y,double amount) {
   // A single translation per scanline keeps the entire foot intact. The
   // planted boot is rigid; only the leg above its ankle eases from the hip.
   double t=Math.Max(0,Math.Min(1,(y-hip)/(ground-hip-size*.09)));t=t*t*(3-2*t);
   return direction*(part==0?-1:1)*size*.045*amount*t;
  }
  unsafe void DrawLeg(Graphics g,int part,double amount) {
   var bits=legCanvas.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);int[] pixels=legPixels[part];
   try{for(int y=0;y<height;y++) {
    uint* row=(uint*)((byte*)bits.Scan0+y*bits.Stride);double delta=Shift(part,y,amount);
    for(int x=0;x<width;x++) {
     double sx=x-delta;int left=(int)Math.Floor(sx);if(left<0||left>=width-1){row[x]=0;continue;}
     double f=sx-left;uint a=(uint)pixels[y*width+left],b=(uint)pixels[y*width+left+1],result=0;
     for(int shift=0;shift<32;shift+=8)result|=(uint)Math.Round((a>>shift&255)*(1-f)+(b>>shift&255)*f)<<shift;
     row[x]=result;
    }
   }}finally{legCanvas.UnlockBits(bits);}g.DrawImageUnscaled(legCanvas,0,0);
  }
  internal double DiagnosticFootShift(int part,double amount){return Shift(part,ground,amount);}
  internal Bitmap DiagnosticLeg(int part){return legs[part];}
  // Shared painted layer access; existing archery drawing remains unchanged.
  internal Bitmap ForegroundLayer {get{return foreground;}}
  static int[] Read(Bitmap bitmap) {
   var pixels=new int[bitmap.Width*bitmap.Height];var bits=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);
   try{for(int y=0;y<bitmap.Height;y++)Marshal.Copy(IntPtr.Add(bits.Scan0,y*bits.Stride),pixels,y*bitmap.Width,bitmap.Width);}finally{bitmap.UnlockBits(bits);}return pixels;
  }
  public void Dispose(){if(original!=null)original.Dispose();if(foreground!=null)foreground.Dispose();if(canvas!=null)canvas.Dispose();if(legCanvas!=null)legCanvas.Dispose();foreach(var leg in legs)if(leg!=null)leg.Dispose();}
 }
}

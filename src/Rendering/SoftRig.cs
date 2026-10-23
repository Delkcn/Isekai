using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace JouchoPet {
 // Continuous texture deformation. Independent influence fields keep the head,
 // outer hair, sleeves, coat and legs moving at different phases.
 // The artist's left/right design is never mirrored.
 sealed class SoftRig : IDisposable {
  readonly int width,height,count;
  readonly double size;
  readonly int[][] pixels=new int[3][];
  readonly float[] head,hair,arm,hem,side,rx,ry;
  readonly Bitmap canvas;
  readonly double eyeY,eyeX;
  readonly CoveredEyeRig coveredEye,interactionEye;
  GreetingArmRig greetingArm;
  static double Smooth(double a,double b,double x) { double v=Math.Max(0,Math.Min(1,(x-a)/(b-a))); return v*v*(3-2*v); }
  public SoftRig(Bitmap[] sprites,int w,int h,int petHeight) {
   width=w;height=h;count=w*h;size=petHeight;
   head=new float[count];hair=new float[count];arm=new float[count];hem=new float[count];side=new float[count];rx=new float[count];ry=new float[count];
   canvas=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
   int[] indices={0,1,3};
   for(int i=0;i<3;i++) using(Bitmap b=new Bitmap(w,h,PixelFormat.Format32bppPArgb)) {
    Bitmap src=sprites[indices[i]];
    float scale=(float)petHeight/src.Height;
    using(Graphics g=Graphics.FromImage(b)) {
     g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.HighQualityBicubic;
     using(Bitmap canonical=CharacterArt.Normalize(src,w,h,size))g.DrawImageUnscaled(canonical,0,0);
    }
    pixels[i]=new int[count];
    BitmapData data=b.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);
    try { Marshal.Copy(data.Scan0,pixels[i],0,count); } finally { b.UnlockBits(data); }
   }
   eyeX=BodyProportions.MapX(346,220,w,size,290,480,710,296);
   eyeY=28+size*BodyProportions.MapY(220,290,480,710);
   double detectedX=0,detectedY=0,eyeWeight=0;
   for(int y=(int)(28+size*.15);y<Math.Min(h,28+size*.29);y++)for(int x=(int)(w*.5-size*.12);x<Math.Min(w,w*.5+size*.20);x++) {
    uint p=(uint)pixels[0][y*w+x];int r=(int)((p>>16)&255),green=(int)((p>>8)&255),blue=(int)(p&255);
    if((p>>24)>180 && blue>r+10 && blue>green+3){double weight=blue-r;detectedX+=x*weight;detectedY+=y*weight;eyeWeight+=weight;}
   }
   if(eyeWeight>0){eyeX=detectedX/eyeWeight;eyeY=detectedY/eyeWeight;}
   coveredEye=new CoveredEyeRig(pixels[0],w,h,size);
   interactionEye=new CoveredEyeRig(pixels[0],w,h,size,true);
   for(int y=0;y<h;y++) for(int x=0;x<w;x++) {
    int k=y*w+x; double nx=(x-w*0.5)/size,ny=(y-28)/size,abs=Math.Abs(nx);
    rx[k]=(float)(x-w*0.5); ry[k]=(float)(y-(28+size*CharacterArt.HeadToNeck));
    head[k]=(float)(1-Smooth(0.28,0.38,ny));
    hair[k]=(float)(Smooth(0.18,0.30,abs)*Smooth(0.18,0.60,ny)*(1-Smooth(0.82,0.97,ny)));
    arm[k]=(float)(Smooth(0.34,0.47,ny)*(1-Smooth(0.63,0.73,ny))*Smooth(0.075,0.14,abs)*(1-Smooth(0.21,0.29,abs)));
    hem[k]=(float)(Smooth(0.52,0.68,ny)*(1-Smooth(0.81,0.89,ny))*(1-Smooth(0.24,0.32,abs)));
    side[k]=nx<0?-1:1;
   }
  }
  static unsafe uint Sample(int* src,int w,int h,double x,double y) {
   int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);
   if(ix<0 || iy<0 || ix>=w-1 || iy>=h-1) return 0;
   int fx=(int)((x-ix)*256),fy=(int)((y-iy)*256);
   int a=(256-fx)*(256-fy),b=fx*(256-fy),c=(256-fx)*fy,d=fx*fy;
   uint p=(uint)src[iy*w+ix],q=(uint)src[iy*w+ix+1],r=(uint)src[(iy+1)*w+ix],s=(uint)src[(iy+1)*w+ix+1];
   uint result=0;
   for(int shift=0;shift<32;shift+=8) {
    uint channel=(uint)((((p>>shift)&255)*a+((q>>shift)&255)*b+((r>>shift)&255)*c+((s>>shift)&255)*d)>>16);
    result|=channel<<shift;
   }
   return result;
  }
  public unsafe Bitmap Draw(double t,double gait,double walking,double lookX,double lookY,double lean,double blink,double[] weights,double affection=0,double patSide=1,double closedEyes=0,double greeting=0,double wavePhase=0,double waveAmount=0) {
   double sleep=weights[2],awake=1-sleep;
   double breath=Math.Sin(t*2.25)*size*0.004;
   double stride=Math.Sin(gait),counter=Math.Cos(gait);
   double hairL=Math.Sin(t*2.2-0.7)*size*0.010+walking*Math.Sin(gait-0.9)*size*0.010;
   double hairR=Math.Sin(t*2.2-1.4)*size*0.010+walking*Math.Sin(gait-1.3)*size*0.010;
   // Greeting now gives a gentle ~2.6-degree head tilt, with both arms at rest.
   double tilt=(lookX*0.019+Math.Sin(t*1.25)*0.007+lean*0.045+affection*patSide*0.075+Math.Max(0,Math.Min(1,greeting))*.045)*awake;
   double bob=walking*(1-Math.Cos(gait*2))*size*0.005;
   double sxHead=(lookX*0.008+affection*patSide*0.006)*size*awake;
   double syHead=(lookY*0.005-affection*0.010)*size*awake;
   int w0=(int)(weights[0]*256),w1=(int)(weights[1]*256),w2=256-w0-w1;
   int[] frontPixels=pixels[0];
   // Restrict the new aperture registration to affectionate interactions.
   // Ordinary blinking and the frozen rest transition keep their old mapping.
   CoveredEyeRig rightEye=closedEyes>0&&(affection>0||greeting>0||waveAmount>0)?interactionEye:coveredEye;
   if(waveAmount>0) {
    if(greetingArm==null)greetingArm=new GreetingArmRig(pixels[0],width,height,size);
    frontPixels=greetingArm.Draw(waveAmount,wavePhase);
   }
   BitmapData data=canvas.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
   try { fixed(int* p0=frontPixels,p1=pixels[1],p2=pixels[2]) {
    byte* output=(byte*)data.Scan0;
    for(int y=0;y<height;y++) {
     uint* row=(uint*)(output+y*data.Stride);
     for(int x=0;x<width;x++) {
      int k=y*width+x; double s=side[k];
      double dx=head[k]*(sxHead-ry[k]*tilt)+hair[k]*(s<0?hairL:hairR)*(0.35+awake*0.65);
      dx+=arm[k]*(-s*stride*walking*size*0.013+Math.Sin(t*1.8+s)*size*0.003)*awake;
      // Leg warping removed: walking is rendered with independent side-view layers.
      dx+=hem[k]*Math.Sin(gait-0.4)*walking*size*0.007;
      double dy=head[k]*(syHead+rx[k]*tilt)-breath*(0.2+head[k]*0.8)-bob;

      dy+=arm[k]*s*counter*walking*size*0.009;
      double sampleX=x-dx,sampleY=y-dy;
      // Move the entire original eye, including the lash line, in its own
      // artwork coordinates. Apply this after head motion so they stay attached.
      double unitEye=size*.0011;
      double sourceEyeX=346+(sampleX-eyeX)/unitEye;
      double sourceEyeY=220+(sampleY-eyeY)/unitEye;
      double closure=Math.Max(blink,closedEyes)*weights[0];
      sampleY=eyeY+(EyeRig.SourceY(sourceEyeX,sourceEyeY,closure)-220)*unitEye;
      // The approved drawing also exposes a small orange-red right iris under
      // the fringe. Close it in its own source coordinates, without a color swap.
      double lidY=sampleY;
      sampleY=rightEye.SourceY(sampleX,sampleY,closure);
      uint a=w0>0?Sample(p0,width,height,sampleX,sampleY):0;
      uint b=w1>0?Sample(p1,width,height,sampleX,sampleY):0;
      uint c=w2>0?Sample(p2,width,height,x-dx*0.3,y+breath*0.45):0;
      uint value=0;
      for(int shift=0;shift<32;shift+=8) value|=(uint)((((a>>shift)&255)*w0+((b>>shift)&255)*w1+((c>>shift)&255)*w2)>>8)<<shift;
      value=rightEye.Lashes(value,sampleX,lidY,closure);
      value=SmileRig.Apply(value,sampleX,sampleY,width,size,affection*awake);
      row[x]=value;
     }
    }
   }} finally { canvas.UnlockBits(data); }
   return canvas;
  }
  public void Dispose() { if(greetingArm!=null)greetingArm.Dispose();canvas.Dispose(); }
 }
}

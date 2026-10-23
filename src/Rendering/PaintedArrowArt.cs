using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet {
 // A single painted arrow retains its spearhead, ivory shaft, feather grain
 // and open nock. The nock contact, rather than the lowest tail pixel, is the
 // rear animation anchor; the fork therefore remains behind the string.
 static class PaintedArrowArt {
  internal static readonly PointF SourceTip=new PointF(511,31),SourceNock=new PointF(512,1463);
  static readonly object gate=new object();
  static int[][] levels;static int[] widths,heights;
  static Bitmap canvas;static int[] output;static int width,height;
  internal static int DecodeCount{get;private set;}
  internal static void Prepare(){lock(gate){if(levels!=null)return;using(Bitmap source=AssetStore.Image("v0643/ivory-arrow.png")){
   int w=source.Width,h=source.Height;var raw=new int[w*h];var bits=source.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   try{for(int y=0;y<h;y++)Marshal.Copy(IntPtr.Add(bits.Scan0,y*bits.Stride),raw,y*w,w);}finally{source.UnlockBits(bits);}
   for(int i=0;i<raw.Length;i++){uint p=(uint)raw[i],a=p>>24,value=a<<24;for(int c=0;c<24;c+=8)value|=(((p>>c)&255)*a+127)/255<<c;raw[i]=(int)value;}
   int count=1,m=Math.Min(w,h);while(m>4){count++;m=(m+1)/2;}var prepared=new int[count][];var ws=new int[count];var hs=new int[count];prepared[0]=raw;ws[0]=w;hs[0]=h;
   for(int k=1;k<count;k++){int pw=ws[k-1],ph=hs[k-1],nw=(pw+1)/2,nh=(ph+1)/2;var next=new int[nw*nh];for(int y=0;y<nh;y++)for(int x=0;x<nw;x++){int x0=x*2,y0=y*2,x1=Math.Min(pw-1,x0+1),y1=Math.Min(ph-1,y0+1);uint a=(uint)prepared[k-1][y0*pw+x0],b=(uint)prepared[k-1][y0*pw+x1],c=(uint)prepared[k-1][y1*pw+x0],d=(uint)prepared[k-1][y1*pw+x1],value=0;for(int shift=0;shift<32;shift+=8)value|=(((a>>shift&255)+(b>>shift&255)+(c>>shift&255)+(d>>shift&255)+2)/4)<<shift;next[y*nw+x]=(int)value;}prepared[k]=next;ws[k]=nw;hs[k]=nh;}
   widths=ws;heights=hs;levels=prepared;DecodeCount++;
  }}}
  static uint Sample(int[] pixels,int w,int h,double x,double y){if(x<0||y<0||x>w-1||y>h-1)return 0;int ix=(int)x,iy=(int)y,jx=Math.Min(w-1,ix+1),jy=Math.Min(h-1,iy+1);double fx=x-ix,fy=y-iy;uint a=(uint)pixels[iy*w+ix],b=(uint)pixels[iy*w+jx],c=(uint)pixels[jy*w+ix],d=(uint)pixels[jy*w+jx],value=0;for(int shift=0;shift<32;shift+=8){double u=a>>shift&255,v=b>>shift&255,r=c>>shift&255,s=d>>shift&255;value|=(uint)Math.Round((u+(v-u)*fx)*(1-fy)+(r+(s-r)*fx)*fy)<<shift;}return value;}
  static PointF Map(PointF tip,PointF axis,double scale,double x,double y){double behind=y-SourceTip.Y,center=SourceTip.X+behind*(SourceNock.X-SourceTip.X)/(SourceNock.Y-SourceTip.Y),lateral=x-center;return RigParts.Add(tip,scale*(-axis.X*behind-axis.Y*lateral),scale*(-axis.Y*behind+axis.X*lateral));}
  internal static RectangleF Bounds(PointF tip,PointF axis,double length){if(length<=0)return new RectangleF(tip,SizeF.Empty);Prepare();double scale=length/(SourceNock.Y-SourceTip.Y),l=double.MaxValue,t=double.MaxValue,r=double.MinValue,b=double.MinValue;foreach(PointF source in new[]{new PointF(0,0),new PointF(widths[0],0),new PointF(0,heights[0]),new PointF(widths[0],heights[0])}){PointF p=Map(tip,axis,scale,source.X,source.Y);l=Math.Min(l,p.X);r=Math.Max(r,p.X);t=Math.Min(t,p.Y);b=Math.Max(b,p.Y);}return RectangleF.FromLTRB((float)(l-2),(float)(t-2),(float)(r+2),(float)(b+2));}
  internal static void Draw(Graphics graphics,PointF tip,PointF axis,double length){if(length<=0)return;Prepare();lock(gate){double scale=length/(SourceNock.Y-SourceTip.Y);RectangleF bounds=Bounds(tip,axis,length);int left=(int)Math.Floor(bounds.Left),top=(int)Math.Floor(bounds.Top),activeWidth=(int)Math.Ceiling(bounds.Right)-left,activeHeight=(int)Math.Ceiling(bounds.Bottom)-top;
   if(canvas==null||width<activeWidth||height<activeHeight){if(canvas!=null)canvas.Dispose();width=Math.Max(width,(activeWidth+31)/32*32);height=Math.Max(height,(activeHeight+31)/32*32);canvas=new Bitmap(width,height,PixelFormat.Format32bppPArgb);output=new int[width*height];}Array.Clear(output,0,output.Length);
   int level=0;while(level+1<levels.Length&&scale*(1<<(level+1))<=1)level++;double divisor=1<<level,skew=(SourceNock.X-SourceTip.X)/(SourceNock.Y-SourceTip.Y);
   for(int y=0;y<activeHeight;y++)for(int x=0;x<activeWidth;x++){double dx=left+x+.5-tip.X,dy=top+y+.5-tip.Y,behind=-(dx*axis.X+dy*axis.Y)/scale,lateral=(-dx*axis.Y+dy*axis.X)/scale,sx=SourceTip.X+behind*skew+lateral,sy=SourceTip.Y+behind;output[y*width+x]=(int)Sample(levels[level],widths[level],heights[level],(sx+.5)/divisor-.5,(sy+.5)/divisor-.5);}
   var bits=canvas.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);try{for(int y=0;y<activeHeight;y++)Marshal.Copy(output,y*width,IntPtr.Add(bits.Scan0,y*bits.Stride),activeWidth);}finally{canvas.UnlockBits(bits);}graphics.DrawImage(canvas,new Rectangle(left,top,activeWidth,activeHeight),0,0,activeWidth,activeHeight,GraphicsUnit.Pixel);
  }}
 }
}

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet {
 // The two painted PNGs are registered once, then their actual cross-sections
 // are skinned to the bow and string anchors. No outline is reconstructed by
 // drawing a line: shading, braid, gold trim and silhouette come from the art.
 static class PaintedBowArt {
  sealed class Strip {
   internal readonly int Width,Height;
   internal readonly int[][] Pixels;
   internal readonly int[] Widths,Heights;
   internal Strip(string name) {
    using(Bitmap source=AssetStore.Image(name)) {
     int w=source.Width,h=source.Height;var raw=new int[w*h];
     var data=source.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
     try{for(int y=0;y<h;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),raw,y*w,w);}finally{source.UnlockBits(data);}
     var center=new double[h];int first=h,last=-1;double half=0;
     for(int y=0;y<h;y++) {double sum=0,weight=0;int lo=w,hi=-1;
      for(int x=0;x<w;x++){uint p=(uint)raw[y*w+x],a=p>>24;if(a==0){raw[y*w+x]=0;continue;}lo=Math.Min(lo,x);hi=x;double z=a*a;sum+=x*z;weight+=z;uint value=a<<24;for(int c=0;c<24;c+=8)value|=(((p>>c)&255)*a+127)/255<<c;raw[y*w+x]=(int)value;}
      if(weight>0){center[y]=sum/weight;first=Math.Min(first,y);last=y;half=Math.Max(half,Math.Max(center[y]-lo,hi-center[y]));}else center[y]=double.NaN;
     }
     if(last<=first)throw new InvalidOperationException("Empty painted bow resource: "+name);
     Width=2*(int)Math.Ceiling(half+2)+1;Height=last-first+1;var level=new int[Width*Height];
     for(int y=0;y<Height;y++){double cx=center[y+first];if(double.IsNaN(cx))continue;for(int x=0;x<Width;x++)level[y*Width+x]=(int)Sample(raw,w,h,cx+x-(Width-1)*.5,y+first);}
     int count=1,m=Math.Min(Width,Height);while(m>4){count++;m=(m+1)/2;}
     Pixels=new int[count][];Widths=new int[count];Heights=new int[count];Pixels[0]=level;Widths[0]=Width;Heights[0]=Height;
     for(int k=1;k<count;k++){int pw=Widths[k-1],ph=Heights[k-1],nw=(pw+1)/2,nh=(ph+1)/2;var next=new int[nw*nh];for(int y=0;y<nh;y++)for(int x=0;x<nw;x++){int x0=x*2,y0=y*2,x1=Math.Min(pw-1,x0+1),y1=Math.Min(ph-1,y0+1);uint a=(uint)Pixels[k-1][y0*pw+x0],b=(uint)Pixels[k-1][y0*pw+x1],c=(uint)Pixels[k-1][y1*pw+x0],d=(uint)Pixels[k-1][y1*pw+x1],v=0;for(int shift=0;shift<32;shift+=8)v|=(((a>>shift&255)+(b>>shift&255)+(c>>shift&255)+(d>>shift&255)+2)/4)<<shift;next[y*nw+x]=(int)v;}Pixels[k]=next;Widths[k]=nw;Heights[k]=nh;}
    }
   }
  }
  struct Vertex {internal double X,Y,U,V;internal Vertex(PointF p,double u,double v,int left,int top){X=p.X-left;Y=p.Y-top;U=u;V=v;}}
  static readonly object gate=new object();
  static Strip bow,stringArt;
  static Bitmap canvas;
  static int[] output;
  static int width,height,activeWidth,activeHeight;
  internal static int LastCanvasWidth{get{return activeWidth;}}
  internal static int LastCanvasHeight{get{return activeHeight;}}
  // Decode and register during renderer construction, before the first shot.
  // Publish the pair together so a failed second load cannot leave half a set.
  internal static void Prepare(){PaintedArrowArt.Prepare();lock(gate){if(bow!=null)return;var preparedBow=new Strip("v0642/ivory-bow.png");var preparedString=new Strip("v0642/ivory-bowstring.png");bow=preparedBow;stringArt=preparedString;}}
  static uint Sample(int[] pixels,int w,int h,double x,double y){if(x<0||y<0||x>w-1||y>h-1)return 0;int ix=(int)x,iy=(int)y,jx=Math.Min(w-1,ix+1),jy=Math.Min(h-1,iy+1);double fx=x-ix,fy=y-iy;uint a=(uint)pixels[iy*w+ix],b=(uint)pixels[iy*w+jx],c=(uint)pixels[jy*w+ix],d=(uint)pixels[jy*w+jx],value=0;for(int shift=0;shift<32;shift+=8){double u=(a>>shift)&255,v=(b>>shift)&255,r=(c>>shift)&255,s=(d>>shift)&255;value|=(uint)Math.Round((u+(v-u)*fx)*(1-fy)+(r+(s-r)*fx)*fy)<<shift;}return value;}
  static uint Over(uint source,uint destination){uint alpha=source>>24;if(alpha==255||destination==0)return source;if(alpha==0)return destination;uint v=0;for(int c=0;c<32;c+=8)v|=Math.Min(255u,((source>>c)&255)+(((destination>>c)&255)*(255-alpha)+127)/255)<<c;return v;}
  static double Edge(Vertex a,Vertex b,double x,double y){return (b.X-a.X)*(y-a.Y)-(b.Y-a.Y)*(x-a.X);}
  static bool Inclusive(Vertex a,Vertex b){return b.Y>a.Y||(b.Y==a.Y&&b.X<a.X);}
  static bool Inside(double e,bool inclusive){return e>0||(e==0&&inclusive);}
  static void Span(Vertex a,Vertex b,double y,ref double low,ref double high){if(y<Math.Min(a.Y,b.Y)||y>=Math.Max(a.Y,b.Y)||a.Y==b.Y)return;double x=a.X+(b.X-a.X)*(y-a.Y)/(b.Y-a.Y);low=Math.Min(low,x);high=Math.Max(high,x);}
  static void Triangle(Strip art,int level,Vertex a,Vertex b,Vertex c){double area=Edge(a,b,c.X,c.Y);if(Math.Abs(area)<.00000001)return;if(area<0){Vertex swap=b;b=c;c=swap;area=-area;}int x0=Math.Max(0,(int)Math.Floor(Math.Min(a.X,Math.Min(b.X,c.X)))),x1=Math.Min(activeWidth-1,(int)Math.Ceiling(Math.Max(a.X,Math.Max(b.X,c.X)))),y0=Math.Max(0,(int)Math.Floor(Math.Min(a.Y,Math.Min(b.Y,c.Y)))),y1=Math.Min(activeHeight-1,(int)Math.Ceiling(Math.Max(a.Y,Math.Max(b.Y,c.Y))));bool ab=Inclusive(a,b),bc=Inclusive(b,c),ca=Inclusive(c,a);double divisor=1<<level;
   // A string triangle can span hundreds of pixels diagonally while being
   // only two pixels wide. Scan its actual covered interval, not that large
   // mostly empty bounding box; the same rule avoids waste on curved strips.
   for(int y=y0;y<=y1;y++){double py=y+.5,low=double.MaxValue,high=double.MinValue;Span(a,b,py,ref low,ref high);Span(b,c,py,ref low,ref high);Span(c,a,py,ref low,ref high);if(high<low)continue;int first=Math.Max(x0,(int)Math.Ceiling(low-.5)),last=Math.Min(x1,(int)Math.Floor(high-.5));for(int x=first;x<=last;x++){double px=x+.5,e0=Edge(b,c,px,py),e1=Edge(c,a,px,py),e2=Edge(a,b,px,py);if(!Inside(e0,bc)||!Inside(e1,ca)||!Inside(e2,ab))continue;double u=(a.U*e0+b.U*e1+c.U*e2)/area,v=(a.V*e0+b.V*e1+c.V*e2)/area;uint p=Sample(art.Pixels[level],art.Widths[level],art.Heights[level],(u+.5)/divisor-.5,(v+.5)/divisor-.5);if(p!=0)output[y*width+x]=(int)Over(p,(uint)output[y*width+x]);}}
  }
  static int Level(Strip art,double scale){int level=0;while(level+1<art.Pixels.Length&&scale*(1<<(level+1))<=1)level++;return level;}
  static void Quad(Strip art,int level,PointF a,PointF b,PointF normal,double half,double v0,double v1,int left,int top){Vertex al=new Vertex(RigParts.Add(a,-normal.X*half,-normal.Y*half),0,v0,left,top),ar=new Vertex(RigParts.Add(a,normal.X*half,normal.Y*half),art.Width-1,v0,left,top),bl=new Vertex(RigParts.Add(b,-normal.X*half,-normal.Y*half),0,v1,left,top),br=new Vertex(RigParts.Add(b,normal.X*half,normal.Y*half),art.Width-1,v1,left,top);Triangle(art,level,al,ar,br);Triangle(art,level,al,br,bl);}
  static void Include(PointF p,double radius,ref double l,ref double t,ref double r,ref double b){l=Math.Min(l,p.X-radius);t=Math.Min(t,p.Y-radius);r=Math.Max(r,p.X+radius);b=Math.Max(b,p.Y+radius);}
  internal static void Draw(Graphics graphics,PointF grip,PointF axis,PointF nock,double size,double flex,double opacity){if(opacity<=0||size<=0)return;Prepare();lock(gate){
   double bowScale=size*.86/(bow.Height-1),bowHalf=(bow.Width-1)*.5*bowScale,stringScale=size*.86/(stringArt.Height-1)*.4,stringHalf=(stringArt.Width-1)*.5*stringScale;
   double l=double.MaxValue,t=double.MaxValue,r=double.MinValue,b=double.MinValue;for(int i=0;i<=32;i++)Include(ArcheryArt.BowCurve(grip,axis,size,i/32.0,flex),bowHalf+2,ref l,ref t,ref r,ref b);Include(nock,stringHalf+2,ref l,ref t,ref r,ref b);
   int left=(int)Math.Floor(l),top=(int)Math.Floor(t);activeWidth=(int)Math.Ceiling(r)-left;activeHeight=(int)Math.Ceiling(b)-top;
   if(canvas==null||width<activeWidth||height<activeHeight){if(canvas!=null)canvas.Dispose();width=Math.Max(width,(activeWidth+31)/32*32);height=Math.Max(height,(activeHeight+31)/32*32);canvas=new Bitmap(width,height,PixelFormat.Format32bppPArgb);output=new int[width*height];}
   Array.Clear(output,0,output.Length);
   // Across(axis) reverses its screen vertical direction for a left-facing
   // shot, so reverse only the longitudinal registration, not the artwork.
   bool right=axis.X>=0;PointF upper=ArcheryArt.BowCurve(grip,axis,size,right?0:1,flex),lower=ArcheryArt.BowCurve(grip,axis,size,right?1:0,flex);int stringLevel=Level(stringArt,stringScale);
   PointF delta=new PointF(nock.X-upper.X,nock.Y-upper.Y);double length=Math.Max(.001,RigParts.Length(upper,nock));PointF normal=new PointF((float)(delta.Y/length),(float)(-delta.X/length));if(normal.X*axis.X+normal.Y*axis.Y<0)normal=new PointF(-normal.X,-normal.Y);Quad(stringArt,stringLevel,upper,nock,normal,stringHalf,0,(stringArt.Height-1)*.5,left,top);
   delta=new PointF(lower.X-nock.X,lower.Y-nock.Y);length=Math.Max(.001,RigParts.Length(nock,lower));normal=new PointF((float)(delta.Y/length),(float)(-delta.X/length));if(normal.X*axis.X+normal.Y*axis.Y<0)normal=new PointF(-normal.X,-normal.Y);Quad(stringArt,stringLevel,nock,lower,normal,stringHalf,(stringArt.Height-1)*.5,stringArt.Height-1,left,top);
   int sections=Math.Max(32,(int)Math.Ceiling(size*.6)),bowLevel=Level(bow,bowScale);PointF before=upper;
   for(int i=1;i<=sections;i++){double f=i/(double)sections;PointF after=ArcheryArt.BowCurve(grip,axis,size,right?f:1-f,flex);Quad(bow,bowLevel,before,after,axis,bowHalf,(i-1.0)/sections*(bow.Height-1),f*(bow.Height-1),left,top);before=after;}
   if(opacity<1){uint factor=(uint)Math.Round(Math.Max(0,opacity)*65535);for(int y=0;y<activeHeight;y++)for(int x=0;x<activeWidth;x++){int k=y*width+x;uint p=(uint)output[k],value=0;for(int c=0;c<32;c+=8)value|=(((p>>c)&255)*factor+32767)/65535<<c;output[k]=(int)value;}}
   var bits=canvas.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);try{for(int y=0;y<activeHeight;y++)Marshal.Copy(output,y*width,IntPtr.Add(bits.Scan0,y*bits.Stride),activeWidth);}finally{canvas.UnlockBits(bits);}
   // Bounds are in the caller's world coordinates. Graphics owns the final
   // desktop-to-overlay translation; the temporary surface stays local.
   graphics.DrawImage(canvas,new Rectangle(left,top,activeWidth,activeHeight),0,0,activeWidth,activeHeight,GraphicsUnit.Pixel);
  }}
 }
}

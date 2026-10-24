using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet {
 // All layers retain one painted surface. Transform geometry, never dissolve
 // between poses. Alpha is only the artwork's original edge coverage.
 static class RigParts {
  internal static Bitmap BitmapOf(int[] p,int w,int h){var b=new Bitmap(w,h,PixelFormat.Format32bppPArgb);var d=b.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);try{for(int y=0;y<h;y++)Marshal.Copy(p,y*w,IntPtr.Add(d.Scan0,y*d.Stride),w);}finally{b.UnlockBits(d);}return b;}
  internal static PointF Mix(PointF a,PointF b,double t){return new PointF((float)(a.X+(b.X-a.X)*t),(float)(a.Y+(b.Y-a.Y)*t));}
  internal static PointF Add(PointF a,double x,double y){return new PointF((float)(a.X+x),(float)(a.Y+y));}
  internal static double Length(PointF a,PointF b){return Math.Sqrt(Math.Pow(b.X-a.X,2)+Math.Pow(b.Y-a.Y,2));}
  internal static PointF Rotate(PointF p,PointF pivot,double angle){double x=p.X-pivot.X,y=p.Y-pivot.Y;return new PointF((float)(pivot.X+x*Math.Cos(angle)-y*Math.Sin(angle)),(float)(pivot.Y+x*Math.Sin(angle)+y*Math.Cos(angle)));}
  internal static void Segment(Graphics g,RigPatch art,PointF a,PointF b,PointF targetA,PointF targetB,double thickness=1) {
   double length=Length(a,b);if(length<.01)return;double ux=(b.X-a.X)/length,uy=(b.Y-a.Y)/length,dx=(targetB.X-targetA.X)/length,dy=(targetB.Y-targetA.Y)/length;
   double projected=Math.Sqrt(dx*dx+dy*dy);if(projected<.002)return;
   double px=-dy/projected*thickness,py=dx/projected*thickness;
   double m11=dx*ux-px*uy,m21=dx*uy+px*ux,m12=dy*ux-py*uy,m22=dy*uy+py*ux;
   var saved=g.Save();using(var m=new Matrix((float)m11,(float)m12,(float)m21,(float)m22,(float)(targetA.X-m11*a.X-m21*a.Y),(float)(targetA.Y-m12*a.X-m22*a.Y))) {g.MultiplyTransform(m);g.DrawImageUnscaled(art.Image,art.Origin);}g.Restore(saved);
  }
 }
 // Trim only runtime transparent margins. Original PNG bytes stay untouched.
 sealed class RigPatch : IDisposable {
  internal readonly Bitmap Image;internal readonly Point Origin;
  internal RigPatch(int[] pixels,int w,int h){int left=w,top=h,right=-1,bottom=-1;for(int y=0;y<h;y++)for(int x=0;x<w;x++)if((uint)pixels[y*w+x]>>24>0){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
   if(right<left){left=top=right=bottom=0;}left=Math.Max(0,left-2);top=Math.Max(0,top-2);right=Math.Min(w-1,right+2);bottom=Math.Min(h-1,bottom+2);Origin=new Point(left,top);int width=right-left+1,height=bottom-top+1;var crop=new int[width*height];for(int y=0;y<height;y++)Array.Copy(pixels,(top+y)*w+left,crop,y*width,width);Image=RigParts.BitmapOf(crop,width,height);
  }
  public void Dispose(){Image.Dispose();}
 }
 sealed class JointArm : IDisposable {
  readonly RigPatch upper,lower,hand,joint;internal readonly PointF Shoulder,Elbow,Wrist,Tip;
  internal JointArm(int[] pixels,int w,int h,double size,PointF shoulder,PointF elbow,PointF wrist,PointF tip) {
   Shoulder=shoulder;Elbow=elbow;Wrist=wrist;Tip=tip;
   var a=new int[pixels.Length];var b=new int[pixels.Length];var c=new int[pixels.Length];var cap=new int[pixels.Length];
   for(int y=0;y<h;y++)for(int x=0;x<w;x++) {int k=y*w+x;if(y<=elbow.Y+size*.009)a[k]=pixels[k];if(y>=elbow.Y-size*.009 && y<=wrist.Y+size*.005)b[k]=pixels[k];if(y>=wrist.Y-size*.005)c[k]=pixels[k];
    double distance=Math.Sqrt(Math.Pow(x-elbow.X,2)+Math.Pow(y-elbow.Y,2)),coverage=Math.Max(0,Math.Min(1,size*.016+.5-distance));if(coverage>0){int sx=Math.Max(0,Math.Min(w-1,(int)elbow.X)),sy=Math.Max(0,Math.Min(h-1,(int)(elbow.Y-size*.012)));uint p=(uint)pixels[sy*w+sx];if(p>>24<80)p=(uint)pixels[Math.Max(0,Math.Min(h-1,(int)elbow.Y))*w+sx];uint value=0;for(int s=0;s<32;s+=8)value|=(uint)Math.Round((p>>s&255)*coverage)<<s;cap[k]=(int)value;}}
   upper=new RigPatch(a,w,h);lower=new RigPatch(b,w,h);hand=new RigPatch(c,w,h);joint=new RigPatch(cap,w,h);
  }
  internal void Draw(Graphics g,PointF shoulder,PointF elbow,PointF wrist,double handAngle=0) {
   DrawUpper(g,shoulder,elbow);
   DrawForearm(g,elbow,wrist,handAngle);
  }
  // Archery registers the painted fingertip to the nock, including depth
  // foreshortening. Other actions retain their original wrist-angle path.
  internal void DrawToFinger(Graphics g,PointF shoulder,PointF elbow,PointF wrist,PointF finger) {
   DrawUpper(g,shoulder,elbow);
   RigParts.Segment(g,joint,Elbow,Wrist,elbow,wrist);
   RigParts.Segment(g,lower,Elbow,Wrist,elbow,wrist);
   RigParts.Segment(g,hand,Wrist,Tip,wrist,finger);
  }
  internal void DrawLowerOnly(Graphics g,PointF elbow,PointF wrist){RigParts.Segment(g,joint,Elbow,Wrist,elbow,wrist);RigParts.Segment(g,lower,Elbow,Wrist,elbow,wrist);}
  internal void DrawHandOnly(Graphics g,PointF wrist,PointF finger){RigParts.Segment(g,hand,Wrist,Tip,wrist,finger);}
  internal void DrawUpper(Graphics g,PointF shoulder,PointF elbow) {
   RigParts.Segment(g,upper,Shoulder,Elbow,shoulder,elbow);
  }
  internal void DrawForearm(Graphics g,PointF elbow,PointF wrist,double handAngle=0) {
   RigParts.Segment(g,joint,Elbow,Wrist,elbow,wrist);
   RigParts.Segment(g,lower,Elbow,Wrist,elbow,wrist);
   double angle=Math.Atan2(wrist.Y-elbow.Y,wrist.X-elbow.X)-Math.Atan2(Wrist.Y-Elbow.Y,Wrist.X-Elbow.X)+handAngle;
   PointF finger=RigParts.Rotate(Tip,Wrist,angle);finger=RigParts.Add(finger,wrist.X-Wrist.X,wrist.Y-Wrist.Y);
   RigParts.Segment(g,hand,Wrist,Tip,wrist,finger);
  }
  public void Dispose(){upper.Dispose();lower.Dispose();hand.Dispose();joint.Dispose();}
 }
 // A single-material regular mesh used by the torso/hair skin. Each output
 // pixel samples exactly one source painting, at an animated UV coordinate.
 sealed class SingleSkin : IDisposable {
  readonly int width,height;readonly PointF[] uv,target;readonly int[] output;readonly Bitmap canvas;
  const int Columns=17,Rows=29;internal double MinimumArea=1;
  internal SingleSkin(int w,int h){width=w;height=h;uv=new PointF[Columns*Rows];target=new PointF[uv.Length];output=new int[w*h];canvas=new Bitmap(w,h,PixelFormat.Format32bppPArgb);for(int r=0;r<Rows;r++)for(int c=0;c<Columns;c++)uv[r*Columns+c]=new PointF(c*(w-1f)/(Columns-1),r*(h-1f)/(Rows-1));}
  internal Bitmap Draw(int[] texture,Func<PointF,PointF> skin){for(int i=0;i<uv.Length;i++)target[i]=skin(uv[i]);Array.Clear(output,0,output.Length);MinimumArea=double.MaxValue;for(int r=0;r<Rows-1;r++)for(int c=0;c<Columns-1;c++){int k=r*Columns+c;Triangle(texture,k,k+1,k+Columns);Triangle(texture,k+1,k+Columns+1,k+Columns);}var d=canvas.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);try{for(int y=0;y<height;y++)Marshal.Copy(output,y*width,IntPtr.Add(d.Scan0,y*d.Stride),width);}finally{canvas.UnlockBits(d);}return canvas;}
  void Triangle(int[] texture,int i,int j,int k){PointF a=target[i],b=target[j],c=target[k];double area=(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);MinimumArea=Math.Min(MinimumArea,area);if(area<=0)throw new InvalidOperationException("Single skin folded");int x0=Math.Max(0,(int)Math.Floor(Math.Min(a.X,Math.Min(b.X,c.X)))),x1=Math.Min(width-1,(int)Math.Ceiling(Math.Max(a.X,Math.Max(b.X,c.X)))),y0=Math.Max(0,(int)Math.Floor(Math.Min(a.Y,Math.Min(b.Y,c.Y)))),y1=Math.Min(height-1,(int)Math.Ceiling(Math.Max(a.Y,Math.Max(b.Y,c.Y))));for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){double v=((x-a.X)*(c.Y-a.Y)-(y-a.Y)*(c.X-a.X))/area,w=((b.X-a.X)*(y-a.Y)-(b.Y-a.Y)*(x-a.X))/area,u=1-v-w;if(u<-.00001||v<-.00001||w<-.00001)continue;output[y*width+x]=(int)Sample(texture,uv[i].X*u+uv[j].X*v+uv[k].X*w,uv[i].Y*u+uv[j].Y*v+uv[k].Y*w);}}
  uint Sample(int[] pixels,double x,double y){int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);if(ix<0||iy<0||ix>=width-1||iy>=height-1)return 0;uint a=(uint)pixels[iy*width+ix],b=(uint)pixels[iy*width+ix+1],c=(uint)pixels[(iy+1)*width+ix],d=(uint)pixels[(iy+1)*width+ix+1];if((a|b|c|d)==0)return 0;double fx=x-ix,fy=y-iy;uint result=0;for(int s=0;s<32;s+=8){double top=(a>>s&255)*(1-fx)+(b>>s&255)*fx,bottom=(c>>s&255)*(1-fx)+(d>>s&255)*fx;result|=(uint)Math.Round(top*(1-fy)+bottom*fy)<<s;}return result;}
  public void Dispose(){canvas.Dispose();}
 }
}

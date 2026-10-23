using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet {
 // A carry-only arm layer. The coat backplate never enters the arm skinning map.
 // Both sleeves are taken from their own original profile, without mirroring.
 sealed class CarryArmRig : IDisposable {
  const int Columns=7,Rows=25;
  static readonly PointF[][] Outline={
   new[]{new PointF(169,171),new PointF(181,176),new PointF(190,186),new PointF(193,205),new PointF(194,228),new PointF(196,250),new PointF(198,272),new PointF(198,284),new PointF(191,292),new PointF(189,301),new PointF(181,307),new PointF(175,312),new PointF(169,306),new PointF(167,296),new PointF(166,288),new PointF(161,280),new PointF(161,266),new PointF(163,248),new PointF(162,229),new PointF(163,209),new PointF(162,193),new PointF(163,182)},
   new[]{new PointF(1608,172),new PointF(1622,175),new PointF(1631,191),new PointF(1630,210),new PointF(1629,225),new PointF(1622,236),new PointF(1619,253),new PointF(1616,265),new PointF(1616,284),new PointF(1611,294),new PointF(1611,304),new PointF(1603,313),new PointF(1597,315),new PointF(1587,305),new PointF(1586,291),new PointF(1584,278),new PointF(1586,264),new PointF(1591,244),new PointF(1590,234),new PointF(1587,220),new PointF(1594,200),new PointF(1597,184)}
  };
  readonly int width,height,facing;readonly double size;
  readonly int[] originalBody,plate,arm,posed,output;
  readonly PointF shoulder,elbow,wrist;readonly PointF[] uv,target;
  readonly Bitmap canvas;
  public double MinimumArea {get;private set;}
  internal PointF Shoulder {get{return shoulder;}}
  internal PointF Elbow {get{return elbow;}}
  internal PointF Wrist {get{return wrist;}}
  internal int[] DiagnosticBackplate {get{return plate;}}
  internal int[] DiagnosticArm {get{return arm;}}
  internal int[] DiagnosticPosedArm {get{return posed;}}
  public CarryArmRig(int direction,Bitmap body,double petSize) {
   width=body.Width;height=body.Height;size=petSize;facing=direction==0?-1:1;
   originalBody=PoseMesh.Read(body);plate=(int[])originalBody.Clone();arm=new int[plate.Length];posed=new int[plate.Length];output=new int[plate.Length];
   canvas=new Bitmap(width,height,PixelFormat.Format32bppPArgb);
   var pose=CharacterArt.Definition(direction==0?"profile-left":"profile-right");double scale=CharacterArt.Scale(pose,size);
   Func<double,double,PointF> map=(x,y)=>new PointF((float)(width*.5+(x-pose.Anchor)*scale),(float)CharacterArt.Y(pose,y,size));
   shoulder=map(direction==0?171:1618,184);elbow=map(direction==0?178:1604,246);wrist=map(direction==0?178:1597,301);
   var polygon=new PointF[Outline[direction].Length];for(int i=0;i<polygon.Length;i++)polygon[i]=map(Outline[direction][i].X,Outline[direction][i].Y);
   using(var patch=AssetStore.Image("v0611/underarm-backplate.png"))using(var path=new GraphicsPath()) {
    path.AddPolygon(polygon);
    for(int y=0;y<height;y++)for(int x=0;x<width;x++) {
     if(!path.IsVisible(x+.5f,y+.5f))continue;
     int k=y*width+x;uint original=(uint)plate[k];arm[k]=(int)original;
     double sourceX=pose.Anchor+(x-width*.5)/scale;
     double neck=CharacterArt.Top(pose)+CharacterArt.HeadToNeck,n=(y-28)/size;
     double sourceY=n<=neck?pose.Crown+(n-CharacterArt.Top(pose))/CharacterArt.HeadToNeck*(pose.Neck-pose.Crown):pose.Neck+(n-neck)/(.98-neck)*(pose.Sole-pose.Neck);
     int px=Math.Max(0,Math.Min(299,(int)Math.Round(direction*150+sourceX-(direction==0?70:1510))));
     int py=Math.Max(0,Math.Min(239,(int)Math.Round(sourceY-150)));Color color=patch.GetPixel(px,py);uint a=original>>24;
     // Scanline normalization can leave interior alpha at 254. Preserve its
     // coverage instead of treating every non-255 pixel as empty space.
     plate[k]=(int)((a<<24)|((uint)(color.R*a/255)<<16)|((uint)(color.G*a/255)<<8)|(uint)(color.B*a/255));
    }
    RectangleF bounds=path.GetBounds();bounds.Inflate(2,2);uv=new PointF[Columns*Rows];target=new PointF[uv.Length];
    for(int r=0;r<Rows;r++)for(int c=0;c<Columns;c++)uv[r*Columns+c]=new PointF(bounds.Left+c*bounds.Width/(Columns-1),bounds.Top+r*bounds.Height/(Rows-1));
   }
  }
  static PointF Rotate(PointF p,PointF pivot,double angle){double x=p.X-pivot.X,y=p.Y-pivot.Y;return new PointF((float)(pivot.X+x*Math.Cos(angle)-y*Math.Sin(angle)),(float)(pivot.Y+x*Math.Sin(angle)+y*Math.Cos(angle)));}
  static double Smooth(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
  internal PointF Skin(PointF p,double carry,double effort,double vertical=0) {
   carry=Math.Max(0,Math.Min(1,carry));effort=Math.Max(0,Math.Min(1,effort));
   vertical=Math.Max(-1,Math.Min(1,vertical));
   double upper=-facing*(18*carry+8*effort-vertical*16*carry)*Math.PI/180,lower=-facing*(20*carry+14*effort)*Math.PI/180;
   return SkinAngles(p,upper,lower);
  }
  PointF SkinAngles(PointF p,double upper,double lower,double jointWidth=.08) {
   PointF upperPoint=Rotate(p,shoulder,upper),movedElbow=Rotate(elbow,shoulder,upper),lowerPoint=Rotate(p,elbow,upper+lower);
   lowerPoint.X+=movedElbow.X-elbow.X;lowerPoint.Y+=movedElbow.Y-elbow.Y;
   double blend=jointWidth==.08?Smooth((p.Y-elbow.Y+size*.04)/(size*.08)):Smooth((p.Y-elbow.Y+size*jointWidth*.5)/(size*jointWidth));
   return new PointF((float)(upperPoint.X+(lowerPoint.X-upperPoint.X)*blend),(float)(upperPoint.Y+(lowerPoint.Y-upperPoint.Y)*blend));
  }
  internal Bitmap Draw(double carry,double effort,double vertical=0) {
   for(int i=0;i<uv.Length;i++)target[i]=Skin(uv[i],carry,effort,vertical);
   return Render(carry==0 && effort==0,1);
  }
  internal PointF SkinRunning(PointF p,double phase,double amount,bool followWrist=true) {
   return SkinRunningPose(p,RunArmPose.At(phase),amount,followWrist);
  }
  PointF SkinRunningPose(PointF p,RunArmPose pose,double amount,bool followWrist) {
   double upper=facing*pose.Upper*amount*Math.PI/180,lower=facing*pose.Lower*amount*Math.PI/180;
   // Spread large elbow rotations across more of the sleeve. The old width is
   // retained for the default relaxed arm; shoulders and wrists stay anchored.
   double jointWidth=.08+.12*Smooth((Math.Abs(pose.Lower*amount)-20)/45);
   PointF mapped=SkinAngles(p,upper,lower,jointWidth);
   if(!followWrist || p.Y<wrist.Y-size*.012)return mapped;
   PointF pivot=SkinAngles(wrist,upper,lower,jointWidth);
   PointF hand=Rotate(mapped,pivot,facing*pose.Wrist*amount*Math.PI/180);
   double blend=Smooth((p.Y-wrist.Y+size*.012)/(size*.035));
   return new PointF((float)(mapped.X+(hand.X-mapped.X)*blend),(float)(mapped.Y+(hand.Y-mapped.Y)*blend));
  }
  internal Bitmap DrawRunning(double phase,double amount) {
   amount=Math.Max(0,Math.Min(1,amount));
   RunArmPose pose=RunArmPose.At(phase);
   for(int i=0;i<uv.Length;i++)target[i]=SkinRunningPose(uv[i],pose,amount,true);
   return Render(amount==0,Smooth(amount/.18));
  }
  internal Bitmap DrawPickup(double phase,double running,double grip,double effort) {
   if(grip<=0)return DrawRunning(phase,running);
   if(grip>=1)return Draw(1,effort);
   double blend=LeapMotion.Ease(grip);RunArmPose pose=RunArmPose.At(phase);
   double upper=facing*(pose.Upper*running*(1-blend)-(18+8*effort)*blend)*Math.PI/180;
   double lower=facing*(pose.Lower*running*(1-blend)-(20+14*effort)*blend)*Math.PI/180;
   double jointWidth=.08+.12*Smooth((Math.Abs(lower*180/Math.PI)-20)/45)*(1-blend);
   PointF wristPivot=SkinAngles(wrist,upper,lower,jointWidth);
   for(int i=0;i<uv.Length;i++) {
    PointF p=SkinAngles(uv[i],upper,lower,jointWidth);
    if(uv[i].Y>=wrist.Y-size*.012) {
     PointF moved=Rotate(p,wristPivot,facing*pose.Wrist*running*(1-blend)*Math.PI/180);
     double weight=Smooth((uv[i].Y-wrist.Y+size*.012)/(size*.035));p=new PointF((float)(p.X+(moved.X-p.X)*weight),(float)(p.Y+(moved.Y-p.Y)*weight));
    }
    target[i]=p;
   }
   return Render(false,Smooth(Math.Max(running,grip)/.18));
  }
  internal Bitmap DrawLeap(double crouch,double air,double reach) {
   double upper=facing*(12*crouch-15*air-8*reach)*Math.PI/180;
   double lower=-facing*(8*crouch+18*air+8*reach)*Math.PI/180;
   for(int i=0;i<uv.Length;i++)target[i]=SkinAngles(uv[i],upper,lower);
   double amount=Math.Max(crouch,Math.Max(air,reach));
   return Render(amount==0,Smooth(amount/.18));
  }
  Bitmap Render(bool neutral,double reveal) {
   Array.Clear(posed,0,posed.Length);MinimumArea=double.MaxValue;
   for(int r=0;r<Rows-1;r++)for(int c=0;c<Columns-1;c++){int i=r*Columns+c;Triangle(i,i+1,i+Columns);Triangle(i+1,i+Columns+1,i+Columns);}
   for(int i=0;i<output.Length;i++) {
    uint foreground=(uint)posed[i],background=(uint)plate[i],inverse=255-(foreground>>24),pixel=0;
    for(int shift=0;shift<32;shift+=8)pixel|=Math.Min(255,((foreground>>shift)&255)+(((background>>shift)&255)*inverse+127)/255)<<shift;
    if(reveal<1) {
     uint original=(uint)originalBody[i],mixed=0;
     for(int shift=0;shift<32;shift+=8)mixed|=(uint)Math.Round(((original>>shift)&255)*(1-reveal)+((pixel>>shift)&255)*reveal)<<shift;
     pixel=mixed;
    }
    output[i]=neutral?originalBody[i]:(int)pixel;
   }
   var data=canvas.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
   try{for(int y=0;y<height;y++)Marshal.Copy(output,y*width,IntPtr.Add(data.Scan0,y*data.Stride),width);}finally{canvas.UnlockBits(data);}return canvas;
  }
  void Triangle(int i,int j,int k) {
   PointF a=target[i],b=target[j],c=target[k];double area=(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);MinimumArea=Math.Min(MinimumArea,area);
   if(area<=0)throw new InvalidOperationException("Carry arm mesh folded");
   int x0=Math.Max(0,(int)Math.Floor(Math.Min(a.X,Math.Min(b.X,c.X)))),x1=Math.Min(width-1,(int)Math.Ceiling(Math.Max(a.X,Math.Max(b.X,c.X))));
   int y0=Math.Max(0,(int)Math.Floor(Math.Min(a.Y,Math.Min(b.Y,c.Y)))),y1=Math.Min(height-1,(int)Math.Ceiling(Math.Max(a.Y,Math.Max(b.Y,c.Y))));
   for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++) {
    double v=((x-a.X)*(c.Y-a.Y)-(y-a.Y)*(c.X-a.X))/area,w=((b.X-a.X)*(y-a.Y)-(b.Y-a.Y)*(x-a.X))/area,u=1-v-w;
    if(u<-.00001||v<-.00001||w<-.00001)continue;posed[y*width+x]=(int)Sample(uv[i].X*u+uv[j].X*v+uv[k].X*w,uv[i].Y*u+uv[j].Y*v+uv[k].Y*w);
   }
  }
  uint Sample(double x,double y){int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);if(ix<0||iy<0||ix>=width-1||iy>=height-1)return 0;double fx=x-ix,fy=y-iy;uint result=0;for(int shift=0;shift<32;shift+=8){double a=((uint)arm[iy*width+ix]>>shift)&255,b=((uint)arm[iy*width+ix+1]>>shift)&255,c=((uint)arm[(iy+1)*width+ix]>>shift)&255,d=((uint)arm[(iy+1)*width+ix+1]>>shift)&255;result|=(uint)Math.Round((a+(b-a)*fx)*(1-fy)+(c+(d-c)*fx)*fy)<<shift;}return result;}
  public void Dispose(){canvas.Dispose();}
 }
}

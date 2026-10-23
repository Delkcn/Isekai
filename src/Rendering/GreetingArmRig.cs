using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet {
 // Only the anatomical right arm (viewer's left) moves. Three rigid segments
 // retain the approved sleeve, cuff and hand; the covered left arm is untouched.
 sealed class GreetingArmRig : IDisposable {
  readonly Bitmap plate,upper,lower,hand,palm,canvas,elbowCap;
  readonly double unit;
  readonly PointF shoulder,elbow,wrist,tip;
  readonly int width,height;
  internal PointF Shoulder {get{return shoulder;}}
  internal PointF Elbow {get{return elbow;}}
  internal Bitmap DiagnosticElbowCap {get{return elbowCap;}}
  public GreetingArmRig(int[] original,int w,int h,double size) {
   width=w;height=h;unit=size;canvas=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
   var pose=CharacterArt.Definition("front");double scale=CharacterArt.Scale(pose,size);
   Func<double,double,PointF> map=(x,y)=>new PointF((float)(w*.5+(x-190)*scale),(float)CharacterArt.Y(pose,y,size));
   shoulder=map(160,207);elbow=map(145,258);wrist=map(134,292);tip=map(130,308);
   PointF[] edge={map(158,193),map(170,204),map(166,221),map(159,237),map(152,262),map(148,272),map(147,288),map(139,296),map(136,309),map(128,318),map(122,307),map(123,292),map(124,283),map(128,266),map(137,244),map(140,233),map(137,224),map(143,214),map(151,208)};
   int[] background=(int[])original.Clone(),up=new int[original.Length],down=new int[original.Length],palm=new int[original.Length];
   using(var generated=AssetStore.Image("v0616/greeting-underlay.png"))
   using(var source=new Bitmap(380,465,PixelFormat.Format32bppArgb)) {
    using(var g=Graphics.FromImage(source)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(generated,new Rectangle(0,0,380,465));}
    source.Tag=pose;
    using(var normalized=CharacterArt.Normalize(source,w,h,size))using(var path=new GraphicsPath())
    using(var mask=new Bitmap(w,h,PixelFormat.Format32bppPArgb)) {
     int[] fill=PoseMesh.Read(normalized);path.AddPolygon(edge);
     using(var g=Graphics.FromImage(mask))using(var pen=new Pen(Color.White,(float)(6*scale))) {
      g.SmoothingMode=SmoothingMode.AntiAlias;g.FillPath(Brushes.White,path);g.DrawPath(pen,path);
     }
     int[] coverage=PoseMesh.Read(mask);
     for(int k=0;k<background.Length;k++) {
      uint amount=(uint)coverage[k]>>24,value=0;
      for(int shift=0;shift<32;shift+=8)value|=((((uint)fill[k]>>shift&255)*amount+((uint)original[k]>>shift&255)*(255-amount)+127)/255)<<shift;
      background[k]=(int)value;
     }
     for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(path.IsVisible(x+.5f,y+.5f)) {
      int k=y*w+x;
      if(y<=elbow.Y+size*.008)up[k]=original[k];
      if(y>=elbow.Y-size*.008 && y<=wrist.Y+size*.004)down[k]=original[k];
      if(y>=wrist.Y-size*.004)palm[k]=original[k];
     }
    }
   }
   plate=BitmapOf(background);upper=BitmapOf(up);lower=BitmapOf(down);hand=BitmapOf(palm);
   // The two flat cuts expose the outside of the elbow when folded upward.
   // Add a rounded overlap between the two segments using original elbow skin.
   // This is a joint surface in the rig; no character asset is replaced.
   int[] joint=new int[original.Length];double radius=size*.017;
   for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
    double dx=x-elbow.X,dy=y-elbow.Y,d=Math.Sqrt(dx*dx+dy*dy);
    double alpha=Math.Max(0,Math.Min(1,radius+.5-d));if(alpha==0)continue;
    int sx=Math.Max(0,Math.Min(w-1,(int)Math.Round(elbow.X+dx*.5-size*.004))),sy=Math.Max(0,Math.Min(h-1,(int)Math.Round(elbow.Y-size*.007+dy*.25)));
    uint pixel=(uint)original[sy*w+sx],value=0;for(int shift=0;shift<32;shift+=8)value|=(uint)Math.Round((pixel>>shift&255)*alpha)<<shift;joint[y*w+x]=(int)value;
   }
   elbowCap=BitmapOf(joint);
   using(var image=AssetStore.Image("v0618/greeting-palm.png")) {
    // Register the transparent hand by its alpha bounds, retaining its original pixels.
    int left=image.Width,top=image.Height,right=0,bottom=0;
    var data=PoseMesh.Read(image);
    for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(((uint)data[y*image.Width+x]>>24)>32){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);}
    if(right<=left || bottom<=top)throw new InvalidOperationException("Greeting palm is empty.");
    this.palm=image.Clone(Rectangle.FromLTRB(left,top,right+1,bottom+1),PixelFormat.Format32bppPArgb);
   }
  }
  Bitmap BitmapOf(int[] pixels) {
   var b=new Bitmap(width,height,PixelFormat.Format32bppPArgb);var d=b.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
   try{for(int y=0;y<height;y++)Marshal.Copy(pixels,y*width,IntPtr.Add(d.Scan0,y*d.Stride),width);}finally{b.UnlockBits(d);}return b;
  }
  internal static PointF Rotate(PointF p,PointF pivot,double degrees) {
   double a=degrees*Math.PI/180,x=p.X-pivot.X,y=p.Y-pivot.Y;
   return new PointF((float)(pivot.X+x*Math.Cos(a)-y*Math.Sin(a)),(float)(pivot.Y+x*Math.Sin(a)+y*Math.Cos(a)));
  }
  internal struct ArmPose {
   public PointF Shoulder,Elbow,Wrist,Tip;
   public double UpperAngle,ForearmAngle,HandAngle,ForearmDepth;
  }
  internal ArmPose Sample(double amount,double phase) {
   amount=Math.Max(0,Math.Min(1,amount));
   // The original resting arm is the inner limit, not the vertical body axis.
   // Add only the configured outward range; never retract inside the resting arm.
   double opening=AnimationParameters.Get("waveUpper");
   double a=opening*.5*(1+Math.Sin(phase-.65))*amount;
   double b=(AnimationParameters.Get("waveElbow")+AnimationParameters.Get("waveForearm")*Math.Sin(phase-.35))*amount;
   double c=(AnimationParameters.Get("waveWristAngle")+AnimationParameters.Get("waveAmplitude")*Math.Sin(phase))*amount;
   PointF movedElbow=Rotate(elbow,shoulder,a);
   // Lift toward the viewer, not along a wide circle beside the torso. The
   // projected forearm shortens around mid-lift; its full length lives in 3D.
   // The fully raised pose still honors the user's existing angle settings.
   double fullB=AnimationParameters.Get("waveElbow")+AnimationParameters.Get("waveForearm")*Math.Sin(phase-.35);
   PointF final=Rotate(wrist,elbow,fullB),flat=Rotate(wrist,elbow,b);
   double q=LeapMotion.Ease(amount),dx=(wrist.X-elbow.X)*(1-q)+(final.X-elbow.X)*q,dy=flat.Y-elbow.Y;
   double length2=Math.Pow(wrist.X-elbow.X,2)+Math.Pow(wrist.Y-elbow.Y,2);
   // Numerical and extreme-angle safety: stay on the original bone's sphere.
   double projected2=dx*dx+dy*dy;if(projected2>length2){double scale=Math.Sqrt(length2/projected2);dx*=scale;dy*=scale;}
   PointF forearm=Rotate(new PointF((float)(elbow.X+dx),(float)(elbow.Y+dy)),elbow,a);
   PointF movedWrist=new PointF(movedElbow.X+forearm.X-elbow.X,movedElbow.Y+forearm.Y-elbow.Y);
   PointF fingers=Rotate(tip,wrist,a+b+c);
   return new ArmPose{Shoulder=shoulder,Elbow=movedElbow,Wrist=movedWrist,
    Tip=new PointF(movedWrist.X+fingers.X-wrist.X,movedWrist.Y+fingers.Y-wrist.Y),
    UpperAngle=a,ForearmAngle=a+b,HandAngle=a+b+c,ForearmDepth=Math.Sqrt(Math.Max(0,length2-dx*dx-dy*dy))};
  }
  void DrawForearm(Graphics g,ArmPose pose) {
   double ox=wrist.X-elbow.X,oy=wrist.Y-elbow.Y,length=Math.Sqrt(ox*ox+oy*oy),ux=ox/length,uy=oy/length;
   double vx=(pose.Wrist.X-pose.Elbow.X)/length,vy=(pose.Wrist.Y-pose.Elbow.Y)/length;
   double projected=Math.Sqrt(vx*vx+vy*vy),px,py;
   if(projected>.04){px=vy/projected;py=-vx/projected;}else{double a=pose.ForearmAngle*Math.PI/180;px=uy*Math.Cos(a)+ux*Math.Sin(a);py=uy*Math.Sin(a)-ux*Math.Cos(a);}
   // A modest perspective width cue keeps the forward-facing short forearm legible.
   double widthScale=1+.10*pose.ForearmDepth/length;px*=widthScale;py*=widthScale;
   double m11=px*uy+vx*ux,m21=-px*ux+vx*uy,m12=py*uy+vy*ux,m22=-py*ux+vy*uy;
   using(var matrix=new Matrix((float)m11,(float)m12,(float)m21,(float)m22,(float)(pose.Elbow.X-m11*elbow.X-m21*elbow.Y),(float)(pose.Elbow.Y-m12*elbow.X-m22*elbow.Y))) {
    var saved=g.Save();g.Transform=matrix;g.DrawImageUnscaled(lower,0,0);g.Restore(saved);
   }
  }
  public int[] Draw(double amount,double phase) {
   ArmPose pose=Sample(amount,phase);
   using(var g=Graphics.FromImage(canvas)) {
    g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(plate,0,0);g.CompositingMode=CompositingMode.SourceOver;
    g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    var saved=g.Save();g.TranslateTransform(pose.Shoulder.X,pose.Shoulder.Y);g.RotateTransform((float)pose.UpperAngle);g.TranslateTransform(-shoulder.X,-shoulder.Y);g.DrawImageUnscaled(upper,0,0);g.Restore(saved);
    // The upper cut also includes opaque hair beside the skin. Drawing the
    // joint behind that cut hid its outer half. Join after the upper segment,
    // before the foreground forearm, with coverage matching the actual width.
    saved=g.Save();g.TranslateTransform(pose.Elbow.X,pose.Elbow.Y);g.RotateTransform((float)pose.UpperAngle);g.TranslateTransform(-elbow.X,-elbow.Y);
    double jointAmount=Math.Max(0,Math.Min(1,amount/.25));DrawAlpha(g,elbowCap,new RectangleF(0,0,width,height),jointAmount*jointAmount*(3-2*jointAmount));g.Restore(saved);
    DrawForearm(g,pose);
    double palmAmount=Math.Max(0,Math.Min(1,(amount-.20)/.50));palmAmount=palmAmount*palmAmount*(3-2*palmAmount);
    saved=g.Save();g.TranslateTransform(pose.Wrist.X,pose.Wrist.Y);g.RotateTransform((float)pose.HandAngle);g.TranslateTransform(-wrist.X,-wrist.Y);
    DrawAlpha(g,hand,new RectangleF(0,0,width,height),1-palmAmount);g.Restore(saved);
    // The generated palm points up; rotate it from the actual wrist-to-fingertip axis.
    saved=g.Save();g.TranslateTransform(pose.Wrist.X,pose.Wrist.Y);
    g.RotateTransform((float)(Math.Atan2(pose.Tip.Y-pose.Wrist.Y,pose.Tip.X-pose.Wrist.X)*180/Math.PI+90));
    float handHeight=(float)(unit*.080),handWidth=handHeight*palm.Width/palm.Height;
    DrawAlpha(g,palm,new RectangleF(-handWidth*.47f,-handHeight*.94f,handWidth,handHeight),palmAmount);g.Restore(saved);
   }
   // Preserve even faint original hair-edge alpha outside the moving arm.
   int[] result=new int[width*height];var bits=canvas.LockBits(new Rectangle(0,0,width,height),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);
   try{for(int y=0;y<height;y++)Marshal.Copy(IntPtr.Add(bits.Scan0,y*bits.Stride),result,y*width,width);}finally{canvas.UnlockBits(bits);}
   return result;
  }
  static void DrawAlpha(Graphics g,Bitmap image,RectangleF destination,double alpha) {
   if(alpha<=0)return;
   using(var attributes=new ImageAttributes()) {
    var matrix=new ColorMatrix();matrix.Matrix33=(float)alpha;attributes.SetColorMatrix(matrix);
    g.DrawImage(image,new[]{new PointF(destination.Left,destination.Top),new PointF(destination.Right,destination.Top),new PointF(destination.Left,destination.Bottom)},new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel,attributes);
   }
  }
  public void Dispose(){plate.Dispose();upper.Dispose();lower.Dispose();hand.Dispose();palm.Dispose();canvas.Dispose();elbowCap.Dispose();}
 }
}

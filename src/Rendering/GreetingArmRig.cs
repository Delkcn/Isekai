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
  PaintedSwordArt fistArt;
  internal PointF Shoulder {get{return shoulder;}}
  internal PointF Elbow {get{return elbow;}}
  internal Bitmap DiagnosticElbowCap {get{return elbowCap;}}
  public GreetingArmRig(int[] original,int w,int h,double size,CharacterForm form=CharacterForm.AnemoneI) {
   width=w;height=h;unit=size;canvas=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
   var pose=CharacterArt.Definition("front");double scale=CharacterArt.Scale(pose,size);
   Func<double,double,PointF> map=(x,y)=>new PointF((float)(w*.5+(x-190)*scale),(float)CharacterArt.Y(pose,y,size));
   shoulder=map(160,207);elbow=map(145,258);wrist=map(134,292);tip=map(130,308);
   PointF[] edge={map(158,193),map(170,204),map(166,221),map(159,237),map(152,262),map(148,272),map(147,288),map(139,296),map(136,309),map(128,318),map(122,307),map(123,292),map(124,283),map(128,266),map(137,244),map(140,233),map(137,224),map(143,214),map(151,208)};
   int[] background=(int[])original.Clone(),up=new int[original.Length],down=new int[original.Length],palm=new int[original.Length];
   using(var generated=AssetStore.Image("v0616/greeting-underlay.png",form))
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
   public double UpperAngle,ForearmAngle,HandAngle,ForearmDepth,UpperDepth;
  }
  internal Bitmap HairBackground {get{return plate;}}
  internal PointF HairContact(double stroke) {
   var p=CharacterArt.Definition("front");double s=CharacterArt.Scale(p,unit);
   return new PointF((float)(width*.5+(150+2*stroke-190)*s),(float)CharacterArt.Y(p,104+5*stroke,unit));
  }
  // Upper arm points toward the viewer; elbow sits beneath the wrist.
  // The original bone length is retained in depth throughout scalp contact.
  internal ArmPose HairSample(double lift,double stroke,Func<PointF,PointF> headMap,double breath) {
   PointF root=RigParts.Add(shoulder,0,-breath*.2),contact=headMap(HairContact(stroke));
   double handLength=RigParts.Length(wrist,tip),u=RigParts.Length(shoulder,elbow),l=RigParts.Length(elbow,wrist);
   double handTarget=-Math.PI*.5;
   PointF goal=RigParts.Add(contact,-Math.Cos(handTarget)*handLength,-Math.Sin(handTarget)*handLength),bend=RigParts.Add(goal,0,l);
   double a0=Math.Atan2(elbow.Y-shoulder.Y,elbow.X-shoulder.X),b0=Math.Atan2(wrist.Y-elbow.Y,wrist.X-elbow.X),c0=Math.Atan2(tip.Y-wrist.Y,tip.X-wrist.X);
   double a=Math.Atan2(bend.Y-root.Y,bend.X-root.X);while(a<a0)a+=2*Math.PI;
   double b=-Math.PI*.5,c=handTarget;
   // Follow the inward fold across the chest, not the outward angle wrap.
   // Lift the upper arm mostly into depth before the forearm finishes folding.
   double projected=u+(RigParts.Length(root,bend)-u)*DailyMotion.Ease(Math.Min(1,lift*1.8));
   a=a0+(a-a0)*lift;b=b0+(b-b0)*lift;c=c0+(c-c0)*lift;
   PointF e=RigParts.Add(root,projected*Math.Cos(a),projected*Math.Sin(a)),w=RigParts.Add(e,l*Math.Cos(b),l*Math.Sin(b)),f=RigParts.Add(w,handLength*Math.Cos(c),handLength*Math.Sin(c));
   return new ArmPose{Shoulder=root,Elbow=e,Wrist=w,Tip=f,UpperAngle=(a-a0)*180/Math.PI,ForearmAngle=(b-b0)*180/Math.PI,HandAngle=(c-c0)*180/Math.PI,UpperDepth=Math.Sqrt(Math.Max(0,u*u-projected*projected))};
  }
  internal ArmPose SigilSample(double amount,double breath){
   double u=RigParts.Length(shoulder,elbow),l=RigParts.Length(elbow,wrist),h=RigParts.Length(wrist,tip);
   double a0=Math.Atan2(elbow.Y-shoulder.Y,elbow.X-shoulder.X),b0=Math.Atan2(wrist.Y-elbow.Y,wrist.X-elbow.X),c0=Math.Atan2(tip.Y-wrist.Y,tip.X-wrist.X);
   // Preserve the accepted held fist anchor. Keep the upper-arm direction at rest,
   // using existing projection depth and a small wrist bend instead of shoulder rotation.
   double gx=u*Math.Cos(.90)+(l+h*.65)*Math.Cos(-1.90),gy=u*Math.Sin(.90)+(l+h*.65)*Math.Sin(-1.90);
   double wristBend=-.25,vx=l+h*.65*Math.Cos(wristBend),vy=h*.65*Math.Sin(wristBend),reach=Math.Sqrt(vx*vx+vy*vy);
   double dot=gx*Math.Cos(a0)+gy*Math.Sin(a0),cross=gx*Math.Sin(a0)-gy*Math.Cos(a0);
   double heldProjection=dot+Math.Sqrt(Math.Max(0,reach*reach-cross*cross));
   double ex=heldProjection*Math.Cos(a0),ey=heldProjection*Math.Sin(a0),heldB=Math.Atan2(gy-ey,gx-ex)-Math.Atan2(vy,vx);
   double projected=u+(heldProjection-u)*amount,b=b0+(heldB-b0)*amount,c=c0+(heldB+wristBend-c0)*amount;
   PointF root=RigParts.Add(shoulder,0,-breath*.2),e=RigParts.Add(root,projected*Math.Cos(a0),projected*Math.Sin(a0)),w=RigParts.Add(e,l*Math.Cos(b),l*Math.Sin(b));
   return new ArmPose{Shoulder=root,Elbow=e,Wrist=w,Tip=RigParts.Add(w,h*Math.Cos(c),h*Math.Sin(c)),UpperAngle=0,ForearmAngle=(b-b0)*180/Math.PI,HandAngle=(c-c0)*180/Math.PI,UpperDepth=Math.Sqrt(Math.Max(0,u*u-projected*projected))};
  }
  internal void DrawHairArm(Graphics g,ArmPose p,double lift,double fist=0,double openHandScale=1) {
   g.InterpolationMode=InterpolationMode.HighQualityBicubic;
   double length=RigParts.Length(shoulder,elbow),ux=(elbow.X-shoulder.X)/length,uy=(elbow.Y-shoulder.Y)/length;
   double vx=(p.Elbow.X-p.Shoulder.X)/length,vy=(p.Elbow.Y-p.Shoulder.Y)/length,n=Math.Sqrt(vx*vx+vy*vy);
   double px=-vy/n,py=vx/n,m11=vx*ux-px*uy,m12=vy*ux-py*uy,m21=vx*uy+px*ux,m22=vy*uy+py*ux;
   var saved=g.Save();
   using(var matrix=new Matrix((float)m11,(float)m12,(float)m21,(float)m22,(float)(p.Shoulder.X-m11*shoulder.X-m21*shoulder.Y),(float)(p.Shoulder.Y-m12*shoulder.X-m22*shoulder.Y))) {g.Transform=matrix;g.DrawImageUnscaled(upper,0,0);}
   g.Restore(saved);
   saved=g.Save();g.TranslateTransform(p.Elbow.X,p.Elbow.Y);g.RotateTransform((float)p.UpperAngle);g.TranslateTransform(-elbow.X,-elbow.Y);DrawAlpha(g,elbowCap,new RectangleF(0,0,width,height),Math.Min(1,lift*4));g.Restore(saved);
   DrawForearm(g,p);
   saved=g.Save();g.TranslateTransform(p.Wrist.X,p.Wrist.Y);g.RotateTransform((float)p.HandAngle);g.ScaleTransform((float)openHandScale,(float)openHandScale);g.TranslateTransform(-wrist.X,-wrist.Y);if(fist==0)g.DrawImageUnscaled(hand,0,0);else DrawAlpha(g,hand,new RectangleF(0,0,width,height),1-fist);g.Restore(saved);
   if(fist>0){if(fistArt==null)fistArt=new PaintedSwordArt();PointF grip=RigParts.Mix(p.Wrist,p.Tip,.65);double angle=Math.Atan2(p.Tip.Y-p.Wrist.Y,p.Tip.X-p.Wrist.X)+Math.PI/2;fistArt.DrawHand(g,p.Wrist,grip,angle,unit,true,fist);}
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
  public void Dispose(){if(fistArt!=null)fistArt.Dispose();plate.Dispose();upper.Dispose();lower.Dispose();hand.Dispose();palm.Dispose();canvas.Dispose();elbowCap.Dispose();}
 }
}

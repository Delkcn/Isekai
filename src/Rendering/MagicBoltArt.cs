using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace JouchoPet {
 static class MagicBoltArt {
  internal const double Radius=.036*1.30,Brightness=1.50,TrailWidth=2,TrailBrightness=2;
  internal const int ParticleCount=24;
  internal const double ParticleLife=.48,ImpactScale=.70;
  static int A(double alpha){return (int)(Math.Max(0,Math.Min(1,alpha))*255);}
  static void Disc(Graphics g,PointF p,double r,Color color){if(r<=0)return;using(var brush=new SolidBrush(color))g.FillEllipse(brush,(float)(p.X-r),(float)(p.Y-r),(float)(r*2),(float)(r*2));}
  static void Ring(Graphics g,PointF p,double r,double width,double alpha){if(r<=0)return;using(var pen=new Pen(Color.FromArgb(A(alpha),255,240,174),(float)Math.Max(.7,width)))g.DrawEllipse(pen,(float)(p.X-r),(float)(p.Y-r),(float)(2*r),(float)(2*r));}
  internal static void Core(Graphics g,PointF p,double size,double amount,double opacity=1){
   double r=size*Radius*amount;if(r<=0||opacity<=0)return;
   // Symmetric emissive light: no offset highlight, dark surface or hard rings.
   float halo=(float)(r*2.6);double light=opacity*Brightness;
   using(var path=new GraphicsPath()){
    path.AddEllipse(p.X-halo,p.Y-halo,halo*2,halo*2);
    using(var glow=new PathGradientBrush(path)){
     glow.CenterPoint=p;
     // GDI+ path blends run from the boundary (0) toward the center (1).
     glow.InterpolationColors=new ColorBlend{Positions=new[]{0f,.23f,.48f,.62f,.77f,1f},Colors=new[]{
      Color.FromArgb(0,255,232,163),Color.FromArgb(A(.15*light),255,232,163),
      Color.FromArgb(A(.40*light),255,240,186),Color.FromArgb(A(.70*light),255,249,216),
      Color.FromArgb(A(.85*light),255,255,239),Color.FromArgb(A(.92*light),255,255,244)}};
     g.FillPath(glow,path);
    }
   }
  }
  internal static double ParticleProgress(int index,double age){return (age-CastingMotion.ChargeStart-index*(CastingMotion.ReleaseAt-CastingMotion.ChargeStart-ParticleLife)/(ParticleCount-1))/ParticleLife;}
  internal static PointF ParticlePoint(PointF center,double size,int index,double progress){double angle=index*2.39996,r=size*.15*(1-LeapMotion.Ease(progress));return RigParts.Add(center,Math.Cos(angle)*r,Math.Sin(angle)*r);}
  internal static void Charge(Graphics g,PointF p,double size,double q,double age){
   if(q<=0)return;Core(g,p,size,.16+.84*q,q);
   for(int i=0;i<ParticleCount;i++){
    double progress=ParticleProgress(i,age);if(progress<=0||progress>=1)continue;
    PointF at=ParticlePoint(p,size,i,progress),tail=ParticlePoint(p,size,i,Math.Max(0,progress-.16));
    double alpha=Math.Sin(Math.PI*progress)*Math.Min(1,q*10);
    using(var pen=new Pen(Color.FromArgb(A(.9*alpha),255,240,187),(float)Math.Max(.9,size*.0035)))g.DrawLine(pen,tail,at);
    Disc(g,at,size*.011,Color.FromArgb(A(.24*alpha),255,231,157));
    Disc(g,at,size*.0055,Color.FromArgb(A(alpha),255,255,236));
   }
  }
  internal static void Release(Graphics g,PointF p,double size,double q){if(q>=0&&q<1)Ring(g,p,size*(.02+.034*q),size*.002,.45*(1-q));}
  internal static Rectangle Bounds(MagicBolt b){PointF a=b.PositionAt(Math.Max(0,b.Age-.13)),z=b.Position;double pad=b.Size*(b.Hit?.80*ImpactScale:Radius*2.6)+3;return Rectangle.FromLTRB((int)Math.Floor(Math.Min(a.X,z.X)-pad),(int)Math.Floor(Math.Min(a.Y,z.Y)-pad),(int)Math.Ceiling(Math.Max(a.X,z.X)+pad),(int)Math.Ceiling(Math.Max(a.Y,z.Y)+pad));}
  internal static void Draw(Graphics g,MagicBolt b){
   if(b.Done)return;g.SmoothingMode=SmoothingMode.AntiAlias;
   PointF tail=b.PositionAt(Math.Max(0,b.Age-.10)),head=b.Position;
   double strength=Math.Max(0,1-b.ImpactAge/.10);
   if(RigParts.Length(tail,head)>.01&&strength>0){
    using(var glow=new LinearGradientBrush(tail,head,Color.FromArgb(0,255,220,135),Color.FromArgb(A(.15*TrailBrightness*strength),255,220,135)))
    using(var pen=new Pen(glow,(float)(b.Size*.018*TrailWidth)))g.DrawLine(pen,tail,head);
    using(var light=new LinearGradientBrush(tail,head,Color.FromArgb(0,255,248,210),Color.FromArgb(A(.58*TrailBrightness*strength),255,248,210)))
    using(var pen=new Pen(light,(float)(b.Size*.005*TrailWidth)))g.DrawLine(pen,tail,head);
   }
   if(!b.Hit)Core(g,b.Position,b.Size,1);
   else Impact(g,b.Target,b.Size,b.ImpactAge);
  }
  internal static void Impact(Graphics g,PointF target,double size,double age){
   // Zero-distance arrow is an adapter to the accepted renderer, not a second
   // projectile. It cannot trigger events and never changes archery behavior.
   var effect=new MagicArrow(PointF.Empty,1,Rectangle.FromLTRB(-1,-1,0,0),size*ImpactScale);
   effect.Update(age);
   var state=g.Save();g.TranslateTransform(target.X,target.Y);
   ArcheryArt.Impact(g,effect);g.Restore(state);
  }
 }
}

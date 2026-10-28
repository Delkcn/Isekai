using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace JouchoPet {
 sealed class SwordRig:IDisposable {
  internal struct Bones {internal PointF Shoulder,Elbow,Wrist,Grip,Tip;internal double Angle,UpperLength,LowerLength;}
  readonly Bitmap[] neutral=new Bitmap[2],body=new Bitmap[2];
  readonly SlashSkeleton[] skeleton=new SlashSkeleton[2];
  readonly SlashBodyRig[] skin=new SlashBodyRig[2];
  readonly JointArm arm;
  readonly PaintedSwordArt art=new PaintedSwordArt();
  readonly Bitmap canvas;
  readonly double size;readonly int pad;
  internal int Padding{get{return pad;}}
  internal SwordRig(int w,int h,double s,SideWalk sides,CharacterForm form=CharacterForm.AnemoneI){
   size=s;pad=(int)Math.Ceiling(s);canvas=new Bitmap(w+2*pad,h+2*pad,PixelFormat.Format32bppPArgb);
   for(int d=0;d<2;d++){
    neutral[d]=(Bitmap)sides.Draw(0,0,0,d==0?-1:1,1).Clone();
    using(var cut=new CarryArmRig(d,neutral[d],s,form)){
     body[d]=d==0?(Bitmap)neutral[d].Clone():RigParts.BitmapOf(cut.DiagnosticBackplate,w,h);
     if(d==1){var def=CharacterArt.Definition("profile-right");PointF tip=new PointF(cut.Wrist.X,(float)CharacterArt.Y(def,313,s));arm=new JointArm(cut.DiagnosticArm,w,h,s,cut.Shoulder,cut.Elbow,cut.Wrist,tip);}
    }
   }
   for(int d=0;d<2;d++){skeleton[d]=new SlashSkeleton(w,s,d==0?-1:1,arm);skin[d]=new SlashBodyRig(w,h,s,d==0?-1:1,body[d],skeleton[d],form);}
  }
  internal SlashSkeleton.Pose SkeletonPose(SwordPose p){return skeleton[p.Facing<0?0:1].Sample(p);}
  internal PointF[] Blade(SwordPose p){var b=Sample(p);return art.Blade(b.Grip,b.Angle,size);}
  internal Bones Sample(SwordPose p){
   var b=SkeletonPose(p);double angle=Math.Atan2(Math.Sin(b.HandAngle+Math.PI/2),p.Facing*Math.Cos(b.HandAngle+Math.PI/2));
   return new Bones{Shoulder=b.Shoulder,Elbow=b.Elbow,Wrist=b.Wrist,Grip=b.Grip,Tip=art.Tip(b.Grip,angle,size),Angle=angle,UpperLength=RigParts.Length(arm.Shoulder,arm.Elbow),LowerLength=RigParts.Length(arm.Elbow,arm.Wrist)};
  }
  void Arm(Graphics g,Bones b,SwordPose p){arm.DrawUpper(g,b.Shoulder,b.Elbow);arm.DrawLowerOnly(g,b.Elbow,b.Wrist);art.DrawSword(g,b.Grip,b.Angle,size,p.Opacity);art.DrawHand(g,b.Wrist,b.Grip,b.Angle,size,p.Facing<0,1);}
  void Stars(Graphics g,SwordPose p,Bones b){
   double revealAge=p.Age-SwordMotion.RevealAt;
   if(revealAge<0)return;
   bool forming=revealAge<SwordMotion.RevealDuration+.04;if(!forming&&p.Age<=SwordMotion.FadeStart)return;
   double progress=LeapMotion.Ease(forming?revealAge/SwordMotion.RevealDuration:(p.Age-SwordMotion.FadeStart)/.25),spread=forming?1-progress:progress;
   double fade=forming?LeapMotion.Ease(revealAge/.008)*(1-LeapMotion.Ease((revealAge-SwordMotion.RevealDuration)/.04)):Math.Sin(Math.PI*progress);
   var state=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;
   for(int i=0;i<38;i++){
    double along=(i+.5)/38,angle=i*2.39996322972865;
    PointF point=RigParts.Mix(b.Grip,b.Tip,along);
    point=RigParts.Add(point,Math.Cos(angle+p.Age*3)*size*.13*spread,Math.Sin(angle+p.Age*3)*size*.13*spread);
    ArcheryArt.Particle(g,point,size*(.0028+i%4*.0009),fade*(.45+.25*Math.Pow(Math.Sin(i+p.Age*12),2)),true,Color.FromArgb(135,215,255));
   }
   g.Restore(state);
  }
  void Wind(Graphics g,SwordPose p){
   if(p.Age<=SwordMotion.StrikeStart||p.Age>=SwordMotion.StrikeEnd)return;
   double strength=p.Opacity*Math.Sin(Math.PI*(p.Age-SwordMotion.StrikeStart)/(SwordMotion.StrikeEnd-SwordMotion.StrikeStart));
   const int n=18;var edge=new PointF[n];double start=Math.Max(SwordMotion.StrikeStart,p.Age-.07);
   for(int i=0;i<n;i++)edge[i]=Sample(SwordMotion.At(start+(p.Age-start)*i/(n-1),p.Facing)).Tip;
   var state=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;
   using(var glow=new Pen(Color.FromArgb((int)(65*strength),30,160,255),(float)(size*.014)))g.DrawLines(glow,edge);
   using(var core=new Pen(Color.FromArgb((int)(190*strength),145,230,255),(float)(size*.004)))g.DrawLines(core,edge);
   g.Restore(state);
  }
  static void Chain(Graphics g,Pen pen,params PointF[] points){g.DrawLines(pen,points);foreach(var p in points)g.FillEllipse(Brushes.Orange,p.X-2,p.Y-2,4,4);}
  void Debug(Graphics g,SwordPose p){var b=SkeletonPose(p);var weapon=Sample(p);using(var pen=new Pen(Color.Cyan,1.2f)){
   Chain(g,pen,b.Head,b.Chest,b.Shoulder,b.Elbow,b.Wrist,b.Grip,weapon.Tip);
   Chain(g,pen,b.Chest,b.Near.Hip,b.Near.Knee,b.Near.Ankle,b.Near.Foot);
   Chain(g,pen,b.Chest,b.Far.Hip,b.Far.Knee,b.Far.Ankle,b.Far.Foot);
  }}
  internal Bitmap Draw(SwordPose p,bool effects=true,bool debug=false){
   int d=p.Facing<0?0:1;
   using(var g=Graphics.FromImage(canvas)){
    g.Clear(Color.Transparent);g.TranslateTransform(pad,pad);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
    if(p.Amount<=0){g.DrawImageUnscaled(neutral[d],0,0);if(debug)Debug(g,p);return canvas;}
    Bones b=Sample(p);
    if(d==0){if(effects)Wind(g,p);Arm(g,b,p);if(effects)Stars(g,p,b);}
    skin[d].Draw(g,p,SkeletonPose(p));
    if(d==1){if(effects)Wind(g,p);Arm(g,b,p);if(effects)Stars(g,p,b);}
    if(debug)Debug(g,p);
   }
   return canvas;
  }
  public void Dispose(){foreach(var s in skin)s.Dispose();art.Dispose();arm.Dispose();foreach(var b in neutral)b.Dispose();foreach(var b in body)b.Dispose();canvas.Dispose();}
 }
}

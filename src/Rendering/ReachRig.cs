using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;
namespace JouchoPet {
 // Reuse the original profile sleeve cuts and JointArm; each side retains its own art.
 sealed class ReachRig:IDisposable {
  readonly Bitmap[] body=new Bitmap[2],neutral=new Bitmap[2];readonly JointArm[] arm=new JointArm[2],scoutArm=new JointArm[2];
  readonly CharacterForm form; readonly Bitmap canvas;internal readonly int Padding;readonly double size;
  readonly SingleSkin[] scoutSkin=new SingleSkin[2];readonly int[][] scoutBody=new int[2][];
  internal struct Nodes {internal PointF Shoulder,Elbow,Wrist,Tip;internal double UpperDepth;}
  internal ReachRig(int w,int h,double s,SideWalk side,CharacterForm form=CharacterForm.AnemoneI){this.form=form;size=s;Padding=(int)Math.Ceiling(s*.25);canvas=new Bitmap(w+2*Padding,h+2*Padding,PixelFormat.Format32bppPArgb);
   for(int d=0;d<2;d++){neutral[d]=(Bitmap)side.Draw(0,0,0,d==0?-1:1,1).Clone();using(var cut=new CarryArmRig(d,neutral[d],s,form)){body[d]=RigParts.BitmapOf(cut.DiagnosticBackplate,w,h);var def=CharacterArt.Definition(d==0?"profile-left":"profile-right");arm[d]=new JointArm(cut.DiagnosticArm,w,h,s,cut.Shoulder,cut.Elbow,cut.Wrist,new PointF(cut.Wrist.X,(float)CharacterArt.Y(def,d==0?311:313,s)));}}
  }
  // The short-sleeve cut includes neighboring coat pixels. Keep the painted
  // skin and a subpixel outline; do not recolor the sleeve or glove.
  internal static int[] LookoutPixels(int[] source,int w,int h,double size,int facing){var clean=(int[])source.Clone();if(facing<0)return clean;var def=CharacterArt.Definition("profile-right");
   int top=(int)Math.Ceiling(CharacterArt.Y(def,236,size)),bottom=(int)Math.Floor(CharacterArt.Y(def,267,size));
   for(int y=top;y<=bottom;y++){int left=w,right=-1;for(int x=0;x<w;x++){uint p=(uint)source[y*w+x];int a=(int)(p>>24),r=(int)(p>>16&255),g=(int)(p>>8&255),b=(int)(p&255);if(a>160&&r>g*1.10&&g>80&&g>b*.90){left=Math.Min(left,x);right=x;}}
    if(right<left)continue;for(int x=0;x<w;x++){double coverage=Math.Max(0,Math.Min(1,Math.Min(x-left+1.5,right-x+1.5)));uint p=(uint)source[y*w+x],v=0;for(int shift=0;shift<32;shift+=8)v|=(uint)Math.Round((p>>shift&255)*coverage)<<shift;clean[y*w+x]=(int)v;}}
   return clean;
  }
  static double MixAngle(double a,double b,double t){double delta=b-a;while(delta>Math.PI)delta-=2*Math.PI;while(delta< -Math.PI)delta+=2*Math.PI;return a+delta*t;}
  internal Nodes Sample(int facing,double amount,PointF target){var a=arm[facing<0?0:1];double u=RigParts.Length(a.Shoulder,a.Elbow),l=RigParts.Length(a.Elbow,a.Wrist),h=RigParts.Length(a.Wrist,a.Tip);
   double aim=Math.Atan2(target.Y-a.Shoulder.Y,Math.Max(size*.10,facing*(target.X-a.Shoulder.X)));aim=Math.Max(-.85,Math.Min(.70,aim));
   double upper=Math.Atan2(Math.Sin(aim+.25),facing*Math.Cos(aim+.25)),lower=Math.Atan2(Math.Sin(aim-.22),facing*Math.Cos(aim-.22));
   upper=MixAngle(Math.Atan2(a.Elbow.Y-a.Shoulder.Y,a.Elbow.X-a.Shoulder.X),upper,amount);lower=MixAngle(Math.Atan2(a.Wrist.Y-a.Elbow.Y,a.Wrist.X-a.Elbow.X),lower,amount);
   var e=RigParts.Add(a.Shoulder,u*Math.Cos(upper),u*Math.Sin(upper));var w=RigParts.Add(e,l*Math.Cos(lower),l*Math.Sin(lower));
   double hand=MixAngle(Math.Atan2(a.Tip.Y-a.Wrist.Y,a.Tip.X-a.Wrist.X),Math.Atan2(target.Y-w.Y,target.X-w.X),amount);
   // Limit wrist deviation; the beam still runs exactly from this finger to the target.
   double delta=hand-lower;while(delta>Math.PI)delta-=2*Math.PI;while(delta< -Math.PI)delta+=2*Math.PI;hand=lower+Math.Max(-.55,Math.Min(.55,delta));
   return new Nodes{Shoulder=a.Shoulder,Elbow=e,Wrist=w,Tip=RigParts.Add(w,h*Math.Cos(hand),h*Math.Sin(hand))};
  }
  internal Bitmap Draw(int facing,double amount,PointF target){int d=facing<0?0:1;using(var g=Graphics.FromImage(canvas)){g.Clear(Color.Transparent);g.TranslateTransform(Padding,Padding);g.InterpolationMode=InterpolationMode.HighQualityBicubic;
   if(amount<=0)g.DrawImageUnscaled(neutral[d],0,0);else{var n=Sample(facing,amount,target);g.DrawImageUnscaled(body[d],0,0);arm[d].DrawToFinger(g,n.Shoulder,n.Elbow,n.Wrist,n.Tip);}}
   return canvas;
  }
  internal Nodes SampleLookout(int facing,double q){var a=arm[facing<0?0:1];double u=RigParts.Length(a.Shoulder,a.Elbow),l=RigParts.Length(a.Elbow,a.Wrist),h=RigParts.Length(a.Wrist,a.Tip);
   // 30 degrees between bone directions gives a 150-degree interior elbow angle.
   double up=-65*Math.PI/180,lo=-35*Math.PI/180;
   // Work in facing-local angles: raise through the front, never wrap behind the shoulder.
   double restUp=Math.Atan2(a.Elbow.Y-a.Shoulder.Y,facing*(a.Elbow.X-a.Shoulder.X));
   double restLo=Math.Atan2(a.Wrist.Y-a.Elbow.Y,facing*(a.Wrist.X-a.Elbow.X));
   up=restUp+(up-restUp)*q;
   // Fold the elbow early so the hand clears the torso during the lift.
   lo=restLo+(lo-restLo)*DailyMotion.Ease(Math.Min(1,q*1.25));
   up=Math.Atan2(Math.Sin(up),facing*Math.Cos(up));lo=Math.Atan2(Math.Sin(lo),facing*Math.Cos(lo));
   double hand=MixAngle(Math.Atan2(a.Tip.Y-a.Wrist.Y,a.Tip.X-a.Wrist.X),Math.Atan2(-.20,facing),q);
   double projected=u*(1-.18*DailyMotion.Ease(q));
   PointF root=RigParts.Add(a.Shoulder,0,-size*.012*q),e=RigParts.Add(root,projected*Math.Cos(up),projected*Math.Sin(up)),w=RigParts.Add(e,l*Math.Cos(lo),l*Math.Sin(lo));
   return new Nodes{Shoulder=root,Elbow=e,Wrist=w,Tip=RigParts.Add(w,h*Math.Cos(hand),h*Math.Sin(hand)),UpperDepth=Math.Sqrt(u*u-projected*projected)};
  }
  internal Bitmap DrawLookout(int facing,double q){int d=facing<0?0:1;using(var g=Graphics.FromImage(canvas)){g.Clear(Color.Transparent);g.TranslateTransform(Padding,Padding);g.InterpolationMode=InterpolationMode.HighQualityBicubic;
   if(q<=0)g.DrawImageUnscaled(neutral[d],0,0);else{if(scoutSkin[d]==null){scoutSkin[d]=new SingleSkin(body[d].Width,body[d].Height);scoutBody[d]=PoseMesh.Read(body[d]);}
    g.DrawImageUnscaled(scoutSkin[d].Draw(scoutBody[d],p=>RigParts.Add(p,0,-size*.012*q*(1-DailyMotion.Ease((p.Y-28-size*.90)/(size*.08))))),0,0);
    if(scoutArm[d]==null)using(var cut=new CarryArmRig(d,neutral[d],size,form)){var a=arm[d];scoutArm[d]=new JointArm(LookoutPixels(cut.DiagnosticArm,body[d].Width,body[d].Height,size,facing),body[d].Width,body[d].Height,size,a.Shoulder,a.Elbow,a.Wrist,a.Tip);}
    var n=SampleLookout(facing,q);scoutArm[d].DrawToFinger(g,n.Shoulder,n.Elbow,n.Wrist,n.Tip);}}
   return canvas;
  }
  public void Dispose(){canvas.Dispose();for(int i=0;i<2;i++){if(scoutSkin[i]!=null)scoutSkin[i].Dispose();if(scoutArm[i]!=null)scoutArm[i].Dispose();body[i].Dispose();neutral[i].Dispose();arm[i].Dispose();}}
 }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace JouchoPet {
 // One front drawing split into independently skinned body, arms and legs.
 // The foreground covered sleeve physically occludes the lower face.
 sealed class HugSleepRig : IDisposable {
  readonly FrontParts parts;readonly SingleSkin body;readonly Bitmap canvas;readonly int[] plate;
  readonly int width,height;readonly double size;
  readonly double armRegistration;
  internal double MinimumArea=1;
  internal HugSleepRig(int w,int h,double s){width=w;height=h;size=s;parts=new FrontParts(w,h,s);body=new SingleSkin(w,h);canvas=new Bitmap(w,h,PixelFormat.Format32bppPArgb);plate=new int[w*h];armRegistration=RegisterOcclusion();}
  double RegisterOcclusion(){
   // Register the actual sleeve edge, not its bone centre: projected painted
   // thickness varies slightly with raster size. The face itself is unchanged.
   using(var g=Graphics.FromImage(canvas)){g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;DrawArm(g,parts.RightArm,false,1,1);DrawArm(g,parts.LeftArm,true,1,1);}
   int[] mask=PoseMesh.Read(canvas);double chin=CharacterArt.Y(CharacterArt.Definition("front"),155,size)+size*.30,faceHeight=chin-(28+size*.51),target=chin-faceHeight*.25;
   int left=(int)(width*.5-size*.04),right=(int)(width*.5+size*.05);
   for(int y=(int)(chin-faceHeight*.6);y<chin+size*.02;y++){int covered=0;for(int x=left;x<right;x++)if((uint)mask[y*width+x]>>24>200)covered++;if(covered>=(right-left)*.65)return target-y;}
   return 0;
  }
  static double Ease(double t){return LeapMotion.Ease(t);}
  internal PointF BodyPoint(PointF p,double amount) {
   double root=.30*size*amount,neck=28+size*.30,floor=28+size*.98;
   double y=p.Y<=neck?p.Y+root:neck+root+(p.Y-neck)*(floor-neck-root)/(floor-neck);
   // Hair settles around the folded body; the head keeps its own scale.
   double outer=Ease((Math.Abs(p.X-width*.5)/size-.15)/.18),lower=Ease((p.Y-neck)/(size*.5));
   double x=p.X+(p.X-width*.5)*.035*amount*outer*lower;
   return new PointF((float)x,(float)y);
  }
  internal PointF Knee(int leg,double p){return RigParts.Mix(parts.Knee[leg],new PointF((float)(width*.5+(leg==0?-1:1)*size*.031),(float)(28+size*.625)),p);}
  internal double FaceBottom(double p){return 28+size*(.30+.30*p);}
  internal Bitmap Draw(Bitmap live,double amount) {
   double p=Math.Max(0,Math.Min(1,amount));if(p<=0)return live;
   Array.Copy(parts.Plate,plate,plate.Length);
   if(live!=null){int[] pixels=PoseMesh.Read(live);for(int i=0;i<plate.Length;i++)if(parts.BodySurface[i])plate[i]=pixels[i];}
   double tuck=Ease(p),hug=Ease((p-.16)/.80);
   PointF[] hips=new PointF[2],knees=new PointF[2],ankles=new PointF[2];
   for(int n=0;n<2;n++){hips[n]=RigParts.Mix(parts.Hip[n],new PointF((float)(width*.5+(n==0?-1:1)*size*.035),(float)(28+size*.86)),tuck);knees[n]=Knee(n,tuck);ankles[n]=RigParts.Mix(parts.Ankle[n],new PointF((float)(width*.5+(n==0?-1:1)*size*.036),parts.Ankle[n].Y),tuck);}
   using(var g=Graphics.FromImage(canvas)){g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
    for(int n=0;n<2;n++)RigParts.Segment(g,parts.Thigh[n],parts.Hip[n],parts.Knee[n],hips[n],knees[n]);
    g.DrawImageUnscaled(body.Draw(plate,q=>BodyPoint(q,p)),0,0);MinimumArea=body.MinimumArea;
    for(int n=0;n<2;n++)RigParts.Segment(g,parts.Shin[n],parts.Knee[n],parts.Ankle[n],knees[n],ankles[n]);
    DrawArm(g,parts.RightArm,false,p,hug);DrawArm(g,parts.LeftArm,true,p,hug);
   }return canvas;
  }
  void DrawArm(Graphics g,JointArm arm,bool covered,double p,double hug) {
   PointF shoulder=BodyPoint(arm.Shoulder,p),elbow=BodyPoint(arm.Elbow,p),wrist=BodyPoint(arm.Wrist,p);
   // The covered forearm plus its hand spans the lower quarter of the face.
   double y=FaceBottom(p)-size*.014+armRegistration;
   PointF finalElbow=new PointF((float)(width*.5+size*(covered?.106:-.088)),(float)y),finalWrist=new PointF((float)(width*.5+size*(covered?-.021:.018)),(float)y);
   elbow=RigParts.Mix(elbow,finalElbow,hug);wrist=RigParts.Mix(wrist,finalWrist,hug);
   arm.Draw(g,shoulder,elbow,wrist);
  }
  public void Dispose(){parts.Dispose();body.Dispose();canvas.Dispose();}
 }
}

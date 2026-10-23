using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace JouchoPet {
 // Anatomical left/right are separate cuts, retaining asymmetric clothing.
 sealed class FrontParts : IDisposable {
  internal readonly int Width,Height;internal readonly double Size;
  internal readonly int[] Plate;internal readonly bool[] Head,BodySurface,StandingSurface;
  internal readonly int[] StandingBody;
  internal readonly JointArm RightArm,LeftArm;
  internal readonly RigPatch[] Thigh=new RigPatch[2],Shin=new RigPatch[2];
  internal readonly PointF[] Hip=new PointF[2],Knee=new PointF[2],Ankle=new PointF[2];
  internal PointF Map(double x,double y){var p=CharacterArt.Definition("front");return new PointF((float)(Width*.5+(x-190)*CharacterArt.Scale(p,Size)),(float)CharacterArt.Y(p,y,Size));}
  internal FrontParts(int w,int h,double size){Width=w;Height=h;Size=size;using(var front=CharacterArt.Normalized("front",w,h,size)){Plate=PoseMesh.Read(front);}Head=new bool[Plate.Length];
   int[][] arms={new int[Plate.Length],new int[Plate.Length]};
   PointF[][] polygons={
    new[]{Map(158,193),Map(170,204),Map(166,221),Map(159,237),Map(152,262),Map(148,272),Map(147,288),Map(139,296),Map(136,309),Map(128,318),Map(122,307),Map(123,292),Map(124,283),Map(128,266),Map(137,244),Map(140,233),Map(137,224),Map(143,214),Map(151,208)},
    new[]{Map(215,189),Map(225,193),Map(232,217),Map(239,244),Map(249,274),Map(253,291),Map(255,301),Map(251,311),Map(244,319),Map(236,317),Map(233,301),Map(230,278),Map(225,251),Map(217,230),Map(212,211)}};
   int[] original=(int[])Plate.Clone(),underlay;
   using(var raw=AssetStore.Image("v0616/greeting-underlay.png"))using(var source=new Bitmap(380,465,PixelFormat.Format32bppArgb)){using(var g=Graphics.FromImage(source)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(raw,new Rectangle(0,0,380,465));}source.Tag=CharacterArt.Definition("front");using(var b=CharacterArt.Normalize(source,w,h,size))underlay=PoseMesh.Read(b);}
   for(int arm=0;arm<2;arm++)using(var path=new GraphicsPath()){path.AddPolygon(polygons[arm]);for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(path.IsVisible(x+.5f,y+.5f)){int k=y*w+x;arms[arm][k]=original[k];if(arm==0)Plate[k]=underlay[k];else {
    double sourceY=159+(y-28-size*.30)/(size*.68)*303;
    double coatEdge=Map(215+(sourceY-190)*.19,sourceY).X;
    int sx=Math.Max(0,Math.Min(w-1,(int)(x<coatEdge?w*.5+size*.075:x+size*.075)));
    Plate[k]=original[y*w+sx];
   }}}
   RightArm=new JointArm(arms[0],w,h,size,Map(160,207),Map(145,258),Map(134,292),Map(130,308));
   LeftArm=new JointArm(arms[1],w,h,size,Map(222,201),Map(235,250),Map(243,292),Map(243,307));
   StandingBody=(int[])Plate.Clone();
   StandingSurface=new bool[Plate.Length];for(int i=0;i<Plate.Length;i++)StandingSurface[i]=Plate[i]==original[i];
   PointF[][] legPolygons={new[]{Map(151,311),Map(185,311),Map(185,466),Map(137,466),Map(147,392)},new[]{Map(185,311),Map(224,311),Map(224,466),Map(181,466)}};
   for(int n=0;n<2;n++)using(var path=new GraphicsPath()){path.AddPolygon(legPolygons[n]);Hip[n]=Map(n==0?171:207,300);Knee[n]=Map(n==0?171:207,321);Ankle[n]=Map(n==0?171:207,448);var upper=new int[Plate.Length];var lower=new int[Plate.Length];for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(path.IsVisible(x+.5f,y+.5f)){int k=y*w+x;if(y<=Knee[n].Y+size*.008)upper[k]=original[k];if(y>=Knee[n].Y-size*.008)lower[k]=original[k];Plate[k]=0;}Thigh[n]=new RigPatch(upper,w,h);Shin[n]=new RigPatch(lower,w,h);}
   BodySurface=new bool[Plate.Length];for(int i=0;i<Plate.Length;i++)BodySurface[i]=Plate[i]==original[i];
   for(int y=0;y<h;y++)for(int x=0;x<w;x++)Head[y*w+x]=y<28+size*.31;
  }
  public void Dispose(){RightArm.Dispose();LeftArm.Dispose();foreach(var b in Thigh)b.Dispose();foreach(var b in Shin)b.Dispose();}
 }
}

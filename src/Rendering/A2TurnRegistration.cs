using System;
using System.Drawing;
namespace JouchoPet {
 // UV landmarks for A2's approved front painting and the original four side views.
 // These only describe where features are painted; the existing skeleton/time curve is shared.
 static class A2TurnRegistration {
  const int Columns=13;
  static float X(double value,int width,double size){return (float)(width*.5+(value-173.5)*size/300);}
  static float Y(double value,double size){return (float)(28+(value-28)*size/300);}
  static PointF Mouth(int view,int width,double size){
   if(view==2)return A2FrontArt.Point(675,945,width,size);
   string[] names={"profile-left","left","front","right","profile-right"};
   double[] sourceX={132.5,467.5,0,1300.5,1658.5};var pose=CharacterArt.Definition(names[view]);
   return new PointF((float)(width*.5+(sourceX[view]-pose.Anchor)*CharacterArt.Scale(pose,size)),(float)CharacterArt.Y(pose,148.5,size));
  }
  static void Features(PointF[] points,int row,double near,double center,double far,int width,double size){
   int k=row*Columns;double[] xs={near,(near+center)*.5,center,(center+far)*.5,far};
   for(int c=4;c<=8;c++)points[k+c].X=X(xs[c-4],width,size);
   points[k+3].X=Math.Min(points[k+3].X,points[k+4].X-(float)(size*.012));
   points[k+9].X=Math.Max(points[k+9].X,points[k+8].X+(float)(size*.012));
  }
  internal static void Apply(PointF[] points,int view,int width,double size){
   if(view==2){
    // White flower centers and visible irises in the 0.8.8 approved head.
    Features(points,3,132.5,173.5,214.8,width,size);
    for(int c=0;c<Columns;c++)points[3*Columns+c].Y=Y(63.4,size);
    Features(points,6,160.9,175.1,189.2,width,size);
    for(int c=3;c<=9;c++){double mix=Math.Max(0,Math.Min(1,(c-4)/4.0));points[6*Columns+c].Y=Y(99.2+(95.5-99.2)*mix,size);}
   }
   // The mouth must have its own correspondence. Averaging the eye/neck anchors
   // placed side mouths 6-11 px off their front counterpart during the blend.
   var mouth=Mouth(view,width,size);double center=173.5+(mouth.X-width*.5)*300/size;
   int row=7;double near=173.5+(points[row*Columns+4].X-width*.5)*300/size,far=173.5+(points[row*Columns+8].X-width*.5)*300/size;
   Features(points,row,near,center,far,width,size);
   for(int c=0;c<Columns;c++)points[row*Columns+c].Y=mouth.Y;
  }
 }
}

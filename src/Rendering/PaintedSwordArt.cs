using System;using System.Drawing;using System.Drawing.Imaging;using System.Drawing.Drawing2D;
namespace JouchoPet {
 sealed class PaintedSwordArt:IDisposable {
  readonly Bitmap sword=AssetStore.Image("v0700/blue-silver-sword.png"),hands=AssetStore.Image("v0700/right-grip-hands.png");
  internal PointF SourceGrip{get{return new PointF(sword.Width*.5f,sword.Height*.815f);}}
  internal double Scale(double size){return size*.80*.75/(sword.Height*.96);}
  internal PointF Tip(PointF grip,double angle,double size){double distance=sword.Height*.805*Scale(size);return RigParts.Add(grip,Math.Cos(angle)*distance,Math.Sin(angle)*distance);}
  internal void DrawSword(Graphics g,PointF grip,double angle,double size,double opacity){var state=g.Save();g.TranslateTransform(grip.X,grip.Y);g.RotateTransform((float)(angle*180/Math.PI+90));double scale=Scale(size);g.ScaleTransform((float)scale,(float)scale);using(var attr=new ImageAttributes()){var m=new ColorMatrix();m.Matrix33=(float)opacity;attr.SetColorMatrix(m);g.DrawImage(sword,new Rectangle(-(int)SourceGrip.X,-(int)SourceGrip.Y,sword.Width,sword.Height),0,0,sword.Width,sword.Height,GraphicsUnit.Pixel,attr);}g.Restore(state);}
  // In both authored right-hand views the thumb/handle exit is source -Y.
  // Bind that axis to the blade, rather than independently rotating a fist
  // whose thumb ended up pointing toward the pommel. Use the other surface
  // when the forearm is rolled; the character and its costume are not flipped.
  internal static Matrix HandFrame(PointF wrist,PointF grip,double bladeAngle,PointF sourceWrist,PointF sourceGrip){
   double scale=RigParts.Length(wrist,grip)/(sourceGrip.X-sourceWrist.X),length=RigParts.Length(wrist,grip);
   double xx=(grip.X-wrist.X)/length*scale,xy=(grip.Y-wrist.Y)/length*scale;
   double yx=-Math.Cos(bladeAngle)*scale,yy=-Math.Sin(bladeAngle)*scale;
   return new Matrix((float)xx,(float)xy,(float)yx,(float)yy,(float)(wrist.X-xx*sourceWrist.X-yx*sourceWrist.Y),(float)(wrist.Y-xy*sourceWrist.X-yy*sourceWrist.Y));
  }
  internal void DrawHand(Graphics g,PointF wrist,PointF grip,double bladeAngle,double size,bool far,double opacity){
   bool palm=!far;float sy=palm?hands.Height*.73f:hands.Height*.27f;
   PointF sourceWrist=new PointF(hands.Width*.365f,sy),sourceGrip=new PointF(hands.Width*.56f,sy);
   var state=g.Save();using(var frame=HandFrame(wrist,grip,bladeAngle,sourceWrist,sourceGrip))g.MultiplyTransform(frame);
   using(var attr=new ImageAttributes()){var color=new ColorMatrix();color.Matrix33=(float)opacity;attr.SetColorMatrix(color);Rectangle source=new Rectangle(0,palm?hands.Height/2:0,hands.Width,hands.Height/2);g.DrawImage(hands,source,source.X,source.Y,source.Width,source.Height,GraphicsUnit.Pixel,attr);}
   g.Restore(state);
  }
  public void Dispose(){sword.Dispose();hands.Dispose();}
 }
}

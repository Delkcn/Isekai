using System.Drawing;
using System.Drawing.Drawing2D;
namespace JouchoPet {
 // Neutral lip registration in atlas coordinates, before all shared head/turn
 // transforms. Like SmileRig, this is a small runtime facial ink layer.
 static class A2SideMouth {
  internal static bool Landmark(string name,out PointF center){
   switch(name){
    case "profile-left":center=new PointF(132.5f,148.5f);return true;
    case "left":center=new PointF(467.5f,148.5f);return true;
    case "right":center=new PointF(1300.5f,148.5f);return true;
    case "profile-right":center=new PointF(1658.5f,148.5f);return true;
    default:center=PointF.Empty;return false;
   }
  }
  internal static void Apply(Bitmap image,CharacterArt.Pose pose){
   PointF c;if(!Landmark(pose.Name,out c))return;c.X-=pose.Cell.X;c.Y-=pose.Cell.Y;
   using(var g=Graphics.FromImage(image))using(var pen=new Pen(Color.FromArgb(220,145,82,79),1.15f)){
    g.SmoothingMode=SmoothingMode.AntiAlias;pen.StartCap=pen.EndCap=LineCap.Round;
    g.DrawBezier(pen,c.X-2.7f,c.Y,c.X-1,c.Y+.45f,c.X+1,c.Y+.45f,c.X+2.7f,c.Y);
   }
  }
 }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
namespace JouchoPet {
 static class ArmAssets {
  public static Bitmap Load(string original,string edited,Rectangle[] regions) {
   string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets");
    using(Bitmap source=AssetStore.Image(original))
    using(Bitmap patch=AssetStore.Image(edited)) {
    Bitmap result=source.Clone(new Rectangle(0,0,source.Width,source.Height),PixelFormat.Format32bppArgb);
    using(Graphics g=Graphics.FromImage(result)) {
     // Replace only arm neighborhoods; preserve original face/eye coordinates,
     // lower legs, pose layout and all pixels outside the selected rectangles.
     g.CompositingMode=CompositingMode.SourceCopy;
     g.InterpolationMode=InterpolationMode.HighQualityBicubic;
     foreach(Rectangle r in regions) {
      RectangleF src=new RectangleF(r.X*(float)patch.Width/source.Width,r.Y*(float)patch.Height/source.Height,
       r.Width*(float)patch.Width/source.Width,r.Height*(float)patch.Height/source.Height);
      g.DrawImage(patch,r,src,GraphicsUnit.Pixel);
     }
    }
    return result;
   }
  }
 }
}

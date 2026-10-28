using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet {
 // Small local translation of the original eye ink, including its lash silhouette.
 // Applied once on import so every rig shares the same eye placement.
 static class EyePlacement {
  internal static double Rise(double size){return size*.006;}
  static RectangleF[] Regions(string pose){switch(pose){
   case "front":return new[]{RectangleF.FromLTRB(850,119,880,140),RectangleF.FromLTRB(897,108,939,139)};
   case "profile-left":return new[]{RectangleF.FromLTRB(129,107,169,140)};
   case "left":return new[]{RectangleF.FromLTRB(464,108,504,140)};
   case "right":return new[]{RectangleF.FromLTRB(1258,112,1296,141),RectangleF.FromLTRB(1299,113,1325,139)};
   case "profile-right":return new[]{RectangleF.FromLTRB(1626,115,1665,141)};
   case "pat":return new[]{RectangleF.FromLTRB(1573,568,1592,582),RectangleF.FromLTRB(1603,550,1640,580)};
   default:return new RectangleF[0]; // Frozen sleep/crouch artwork keeps its registration.
  }}
  static double Feather(double distance,double width){double t=Math.Max(0,Math.Min(1,distance/width));return t*t*(3-2*t);}
  internal static void Apply(Bitmap image,CharacterArt.Pose pose,double size){var regions=Regions(pose.Name);if(regions.Length==0)return;
   int w=image.Width,h=image.Height;double scale=CharacterArt.Scale(pose,size),rise=Rise(size),feather=scale*4;var source=PoseMesh.Read(image);var output=(int[])source.Clone();
   foreach(var region in regions){double left=w*.5+(region.Left-pose.Anchor)*scale,right=w*.5+(region.Right-pose.Anchor)*scale;
    double top=CharacterArt.Y(pose,region.Top,size)-rise,bottom=CharacterArt.Y(pose,region.Bottom,size)-rise;
    for(int y=Math.Max(0,(int)(top-feather));y<Math.Min(h-1,Math.Ceiling(bottom+feather));y++)for(int x=Math.Max(0,(int)(left-feather));x<Math.Min(w,Math.Ceiling(right+feather));x++){
     double mask=Feather(x-left+feather,feather)*Feather(right+feather-x,feather)*Feather(y-top+feather,feather)*Feather(bottom+feather-y,feather);
     if(mask==0)continue;double sy=y+rise*mask;int row=(int)sy;double f=sy-row;uint a=(uint)source[row*w+x],b=(uint)source[(row+1)*w+x],value=0;
     for(int shift=0;shift<32;shift+=8)value|=(uint)Math.Round((a>>shift&255)*(1-f)+(b>>shift&255)*f)<<shift;output[y*w+x]=(int)value;
    }
   }
   var data=image.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);try{for(int y=0;y<h;y++)Marshal.Copy(output,y*w,IntPtr.Add(data.Scan0,y*data.Stride),w);}finally{image.UnlockBits(data);}
  }
 }
}

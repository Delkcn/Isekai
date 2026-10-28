using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet {
 // A small, feathered source-coordinate correction like EyePlacement. Only the
 // profile nose contour moves; eyes, mouth and chin stay outside its influence.
 static class A2SideNose {
  internal static void Apply(Bitmap image,CharacterArt.Pose pose){
   PointF center;if(pose.Name=="profile-left")center=new PointF(122,134);else if(pose.Name=="profile-right")center=new PointF(1669,134);else return;
   center.X-=pose.Cell.X;center.Y-=pose.Cell.Y;int w=image.Width,h=image.Height;var source=new int[w*h];
   var read=image.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   try{for(int y=0;y<h;y++)Marshal.Copy(IntPtr.Add(read.Scan0,y*read.Stride),source,y*w,w);}finally{image.UnlockBits(read);}var result=(int[])source.Clone();
   for(int y=Math.Max(0,(int)center.Y-8);y<Math.Min(h-2,center.Y+8);y++)for(int x=Math.Max(0,(int)center.X-9);x<Math.Min(w,center.X+9);x++){
    double dx=(x-center.X)/9,dy=(y-center.Y)/8,r=dx*dx+dy*dy;if(r>=1)continue;
    double weight=(1-r)*(1-r),sy=y+2.2*weight;int row=(int)sy;double f=sy-row;uint a=(uint)source[row*w+x],b=(uint)source[(row+1)*w+x];double alpha=(a>>24)*(1-f)+(b>>24)*f;uint pixel=(uint)Math.Round(alpha)<<24;
    if(alpha>0)for(int shift=0;shift<24;shift+=8)pixel|=(uint)Math.Round(((a>>shift&255)*(a>>24)*(1-f)+(b>>shift&255)*(b>>24)*f)/alpha)<<shift;result[y*w+x]=(int)pixel;
   }
   var data=image.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
   try{for(int y=0;y<h;y++)Marshal.Copy(result,y*w,IntPtr.Add(data.Scan0,y*data.Stride),w);}finally{image.UnlockBits(data);}
  }
 }
}

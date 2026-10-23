using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet {
 static class SleepArt {
  static readonly int[] Left={34,461,894,1330,17,463,894,1327};
  static readonly int[] Right={458,893,1330,1770,461,893,1326,1770};
  static readonly double[] Crown={8,17,52,97,492,495,504,518};
  static readonly double[] Neck={204,214,251,303,699,704,711,737};
  static readonly double[] Sole={484,484,484,484,875,871,873,873};
  static readonly double[] Anchor={245,684,1110,1549,249,690,1105,1555};
  static readonly double[] Top={0,.03,.09,.20,.28,.30,.31,.31};
  public static PoseMesh.Pose Key(Bitmap sheet,int index,int w,int h,double size) {
   int top=index<4?0:487;var bounds=new Rectangle(Left[index],top,Right[index]-Left[index],index<4?487:400);
   using(Bitmap source=sheet.Clone(bounds,PixelFormat.Format32bppArgb))using(var output=new Bitmap(w,h,PixelFormat.Format32bppPArgb)) {
    // Isolated export specks must not become silhouette landmarks in PoseMesh.
    // Reuse the standing-art cutout rule and retain the soft two-pixel fringe.
    var pixels=new int[source.Width*source.Height];
    var data=source.LockBits(new Rectangle(0,0,source.Width,source.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
    try {
     for(int y=0;y<source.Height;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),pixels,y*source.Width,source.Width);
     CharacterArt.KeepMainIsland(pixels,source.Width,source.Height);
     for(int y=0;y<source.Height;y++)Marshal.Copy(pixels,y*source.Width,IntPtr.Add(data.Scan0,y*data.Stride),source.Width);
    }finally{source.UnlockBits(data);}
    double head=CharacterArt.HeadToNeck,neck=Top[index]+head,scale=size*head/(Neck[index]-Crown[index]);
    using(Graphics g=Graphics.FromImage(output)) {
     g.InterpolationMode=InterpolationMode.HighQualityBicubic;
     for(int y=0;y<h;y++) {
      double a=SourceY((y-28)/size,index,neck),b=SourceY((y-27)/size,index,neck);
      if(a<top || a>=Sole[index]+1)continue;
      g.DrawImage(source,new RectangleF((float)(w*.5+(bounds.X-Anchor[index])*scale),y,(float)(bounds.Width*scale),1),new RectangleF(0,(float)(a-top),bounds.Width,(float)Math.Min(b-a,Sole[index]+1-a)),GraphicsUnit.Pixel);
     }
    }
    HairPalette.Match(output,size,Top[index]);
    HairLength.ApplySleep(output,index,size,Top[index],Neck[index],Crown[index],Sole[index],Anchor[index]);
    double[] rows=new double[13],headRows={-.06,.08,.38,.56,.78,1};
    for(int i=0;i<6;i++)rows[i]=28+size*(Top[index]+headRows[i]*head);
    for(int i=6;i<13;i++)rows[i]=28+size*(neck+(1.04-neck)*(i-5)/7);
    return PoseMesh.Register(output,rows,w*.5,w*.5,28+size*(Top[index]+.3));
   }
  }
  static double SourceY(double n,int i,double neck) {
   return n<=neck?Crown[i]+(n-Top[i])/CharacterArt.HeadToNeck*(Neck[i]-Crown[i]):Neck[i]+(n-neck)/(.98-neck)*(Sole[i]-Neck[i]);
  }
 }
}

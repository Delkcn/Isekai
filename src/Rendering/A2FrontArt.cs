using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace JouchoPet {
 // Import the approved front-only painting into the existing atlas registration.
 // Side/turn paintings continue to come from the unchanged 0.8.0 atlas.
 static class A2FrontArt {
  internal const string Resource="v0802-a2/front-waist-hair.png";
  internal const string HeadResource="v0808-a2/front-head.png";
  const double Crown=20,Neck=425,Sole=1320,Anchor=573;
  // The approved head stays unmodified on disk. Registration and eye placement
  // are part of the same runtime import as the existing front/body registration.
  const double FaceCrown=65,FaceNeck=1020,FaceAnchor=675;
  internal const double EyeLift=1.5;
  static readonly double FaceScale=142.0/(FaceNeck-FaceCrown);
  static readonly double HeadScale=142.0/(Neck-Crown);
  static double AtlasY(double y){return y<=Neck?17+(y-Crown)*HeadScale:159+(y-Neck)*303/(Sole-Neck);}
  static double FaceX(double x){return 190+(x-FaceAnchor)*FaceScale;}
  static double FaceY(double y){return 17+(y-FaceCrown)*FaceScale;}
  internal static PointF Point(double x,double y,int width,double size){
   var p=CharacterArt.Definition("front");return new PointF((float)(width*.5+(FaceX(x)-190)*CharacterArt.Scale(p,size)),(float)CharacterArt.Y(p,FaceY(y),size));
  }
  internal static PointF EyePoint(double x,double y,int width,double size){return Point(x,y-EyeLift/FaceScale,width,size);}
  internal static PointF ExposedEye(int width,double size){return EyePoint(842,791,width,size);}
  static double Feather(double d,double w){double t=Math.Max(0,Math.Min(1,d/w));return t*t*(3-2*t);}
  static void RaiseEyes(int[] source,int width,int height){
   var original=(int[])source.Clone();double drop=-EyeLift,feather=2;
   foreach(var area in new[]{RectangleF.FromLTRB(490,798,582,864),RectangleF.FromLTRB(744,710,946,856)}){
    double left=FaceX(area.Left),right=FaceX(area.Right);
    double top=FaceY(area.Top)+drop,bottom=FaceY(area.Bottom)+drop;
    for(int y=(int)(top-feather);y<bottom+feather;y++)for(int x=(int)(left-feather);x<right+feather;x++){
     double weight=Feather(x-left+feather,feather)*Feather(right+feather-x,feather)*Feather(y-top+feather,feather)*Feather(bottom+feather-y,feather);
     if(weight==0)continue;double sy=y-drop*weight;int row=(int)Math.Floor(sy);double f=sy-row;uint a=(uint)original[row*width+x],b=(uint)original[(row+1)*width+x],pixel=0;
     for(int shift=0;shift<32;shift+=8)pixel|=(uint)Math.Round((a>>shift&255)*(1-f)+(b>>shift&255)*f)<<shift;source[y*width+x]=(int)pixel;
    }
   }
  }
  static uint Mix(uint a,uint b,double weight){uint pixel=0;for(int shift=0;shift<32;shift+=8)pixel|=(uint)Math.Round((a>>shift&255)*(1-weight)+(b>>shift&255)*weight)<<shift;return pixel;}
  static void ImportHead(int[] body){
   using(var source=AssetStore.Image(HeadResource))using(var head=new Bitmap(380,465,PixelFormat.Format32bppPArgb)){
    using(var g=Graphics.FromImage(head)){
     g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.CompositingMode=CompositingMode.SourceCopy;
     g.DrawImage(source,new RectangleF((float)FaceX(0),(float)FaceY(0),(float)(1355*FaceScale),(float)(1161*FaceScale)));
    }
    var pixels=PoseMesh.Read(head);RaiseEyes(pixels,380,465);
    // Replace the old silhouette as well as its face. Blend only below the jaw,
    // across the collar and side locks; the waist-length hair/body remain intact.
    for(int y=0;y<180;y++)for(int x=0;x<380;x++){
     double weight=1-Feather(y-164,16);int i=y*380+x;body[i]=(int)Mix((uint)body[i],(uint)pixels[i],weight);
    }
   }
  }
  internal static Bitmap Load(){
   using(var source=AssetStore.Image(Resource)){
    var result=new Bitmap(380,465,PixelFormat.Format32bppArgb);
    using(var g=Graphics.FromImage(result)){
     g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.CompositingMode=CompositingMode.SourceCopy;
     for(int y=0;y<463;y++){
      double a=y<=159?Crown+(y-17)/HeadScale:Neck+(y-159)*(Sole-Neck)/303;
      double b=y+1<=159?Crown+(y+1-17)/HeadScale:Neck+(y+1-159)*(Sole-Neck)/303;
      if(a<0||a>=1388)continue;
      g.DrawImage(source,new RectangleF((float)(190-Anchor*HeadScale),y,(float)(1133*HeadScale),1),new RectangleF(0,(float)(a*source.Height/1388),source.Width,(float)((b-a)*source.Height/1388)),GraphicsUnit.Pixel);
     }
    }
    // Same island cleanup used by all atlas poses; remove detached generation specks.
    var data=PoseMesh.Read(result);A2CoatLength.Apply(data,380,465);A2FrontGarment.Apply(data,380,465);ImportHead(data);A2FrontCoatColor.Apply(data,380,465);CharacterArt.KeepMainIsland(data,380,465);
    var bits=result.LockBits(new Rectangle(0,0,380,465),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
    try{for(int y=0;y<465;y++)System.Runtime.InteropServices.Marshal.Copy(data,y*380,IntPtr.Add(bits.Scan0,y*bits.Stride),380);}finally{result.UnlockBits(bits);}
    // The approved painting already ends at the lower waist; do not shorten it twice.
    result.Tag=new CharacterArt.Pose("front",700,0,380,465,890,17,159,462,155){PreserveFaceRegistration=true,PreserveHairLength=true};return result;
   }
  }
  internal static CoveredEyeRig CoveredEye(int width,double size){
   double[] xs={492,508,525,541,557,572,582},hi={855,849,834,818,805,802,808},lo={860,859,855,849,839,825,811};
   var upper=new PointF[xs.Length];var lower=new PointF[xs.Length];
   for(int i=0;i<xs.Length;i++){upper[i]=EyePoint(xs[i],hi[i],width,size);lower[i]=EyePoint(xs[i],lo[i],width,size);}
   return new CoveredEyeRig(width,size,upper,lower);
  }
 }
}

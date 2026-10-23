using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
namespace JouchoPet {
 // Authored in-betweens are the pose source. Runtime geometry only stabilizes
 // registration; it never invents body correspondence with optical flow.
 static class DrawnFrames {
  public static List<Bitmap> Load(string[] sheets,int[] first,int[] last) {
   var result=new List<Bitmap>();
   var rectangles=new Dictionary<string,Rectangle>();
   foreach(string line in AssetStore.Lines("v0519/frames.csv")) {
    if(string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))continue;
    string[] v=line.Split(',');if(v.Length!=6)throw new IOException("Invalid frame metadata");
    rectangles.Add(v[0]+":"+v[1],new Rectangle(int.Parse(v[2]),int.Parse(v[3]),int.Parse(v[4]),int.Parse(v[5])));
   }
   try {
    for(int s=0;s<sheets.Length;s++)using(Bitmap atlas=AssetStore.Image("v0519/"+sheets[s])) {
     for(int f=first[s];f<=last[s];f++) {
      Rectangle rect;if(!rectangles.TryGetValue(sheets[s]+":"+f,out rect))throw new IOException("Missing frame rectangle: "+sheets[s]+" / "+f);
      Bitmap cell=atlas.Clone(rect,PixelFormat.Format32bppArgb);
      // Fixed artist landmarks, not a dark-pixel detector: gloves cross the
      // collar in seated poses and otherwise produce sudden scale changes.
      double neck=sheets[s]=="turn-a.png"?127:sheets[s]=="turn-b.png"?125:sheets[s]=="turn-c.png"?125:sheets[s]=="turn-d.png"?125:sheets[s]=="sleep-a.png"?140:sheets[s]=="sleep-b.png"?150:158;
      cell.Tag=neck;result.Add(cell);
     }
    }
    return result;
   }catch{foreach(Bitmap b in result)b.Dispose();throw;}
  }
  static double Anchor(Bitmap b) {
   int left=b.Width,right=0;
   for(int y=(int)(b.Height*.90);y<b.Height;y++)for(int x=0;x<b.Width;x++) {
    Color c=b.GetPixel(x,y);if(c.A>180 && c.R>c.G*1.5 && c.R>c.B*1.5){left=Math.Min(left,x);right=Math.Max(right,x);}
   }
   return right>left?(left+right)*.5:b.Width*.5;
  }
  public static Bitmap Normalize(Bitmap source,int w,int h,double size,double rest) {
   if(source.Tag is CharacterArt.Pose)return CharacterArt.Normalize(source,w,h,size);
   var registration=source.Tag as AssetStore.Registration;
   double anchor=Anchor(source),neck=registration!=null?registration.Neck:source.Tag is double?(double)source.Tag:source.Height*.43,sole=source.Height-1;
   double crown=rest<0?0:.30*Smooth(rest),targetNeck=crown+.34+(rest<0?0:.06*Smooth(rest));
   double headScale=size*(targetNeck-crown)/neck;
   Bitmap result=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
   using(Graphics g=Graphics.FromImage(result)) {
    g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    for(int y=0;y<h;y++) {
     double n=(y-28)/size,nn=(y-27)/size;
     double sy=n<=targetNeck?(n-crown)/(targetNeck-crown)*neck:neck+(n-targetNeck)/(.98-targetNeck)*(sole-neck);
     double ey=nn<=targetNeck?(nn-crown)/(targetNeck-crown)*neck:neck+(nn-targetNeck)/(.98-targetNeck)*(sole-neck);
     if(sy<0 || sy>=source.Height)continue;
     // Keep head aspect ratio and a consistent physical scale through crouch.
     // New 600px turn paintings have explicit import registration. Their source
     // silhouette is slimmed without stretching the existing front/walk artwork.
     double sx=headScale*(registration!=null?registration.Width:1);
     g.DrawImage(source,new RectangleF((float)(w*.5-anchor*sx),y,(float)(source.Width*sx),1),new RectangleF(0,(float)sy,source.Width,(float)Math.Min(ey-sy,source.Height-sy)),GraphicsUnit.Pixel);
    }
   }
   if(rest<0)TurnHair.Normalize(result,size);
   HairPalette.Match(result,size,crown);
   return result;
  }
  public static double Smooth(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
  public static PoseMesh.Pose Register(Bitmap picture,double size,double rest) {
   double top=rest<0?0:.30*Smooth(rest),neck=top+.34+(rest<0?0:.06*Smooth(rest));
   double[] rows=new double[13];
   double[] head={-.06,.08,.38,.56,.78,1};
   for(int i=0;i<6;i++)rows[i]=28+size*(top+head[i]*(neck-top));
   for(int i=6;i<13;i++)rows[i]=28+size*(neck+(1.04-neck)*(i-5)/7);
   return PoseMesh.Register(picture,rows,picture.Width*.5,picture.Width*.5+size*.055,28+size*(top+.56*(neck-top)));
  }
 }
}

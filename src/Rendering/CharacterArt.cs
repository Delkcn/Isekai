using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace JouchoPet {
 // Import coordinates belong to the approved v064 board, never to generated frames.
 // Uniform head scaling plus registered body height keeps all views at 3-4 heads.
 static class CharacterArt {
  public const string Resource="v0610/anemone-i-unified-v10-eyes.png";
  public const double SourceHeight=452;
  public const double HeadToNeck=.30;
  public sealed class Pose {
   public string Name; public Rectangle Cell; public double Anchor,Crown,Neck,Sole;
   public double Chin;
   public Pose(string name,int x,int y,int w,int h,double anchor,double crown,double neck,double sole,double chin=-1) {
    Name=name;Cell=new Rectangle(x,y,w,h);Anchor=anchor;Crown=crown;Neck=neck;Sole=sole;Chin=chin<0?neck-6:chin;
   }
  }
  static readonly Pose[] Poses={
   new Pose("profile-left",0,0,390,465,160,17,161,451),
   new Pose("left",365,0,340,465,495,18,161,452),
   new Pose("front",700,0,380,465,890,17,159,462,155),
   new Pose("right",1070,0,320,465,1268,17,162,462),
   new Pose("profile-right",1390,0,384,465,1620,16,161,452),
   new Pose("push-left",0,460,396,427,202,466,610,858),
   new Pose("push-right",380,465,370,422,555,472,616,859),
   new Pose("crouch",720,475,360,412,917,506,662,854),
   new Pose("sleep",1070,510,372,377,1272,542,693,855),
   new Pose("pat",1432,455,342,432,1613,460,606,870)
  };
  static readonly object Sync=new object();
  static int[] atlas;static int atlasWidth,atlasHeight;
  static void Ensure() {
   lock(Sync)if(atlas==null)using(Bitmap b=AssetStore.Image(Resource)) {
    if(b.Width!=1774 || b.Height!=887)throw new InvalidOperationException("The character atlas must retain its original 1774 x 887 dimensions.");
    atlasWidth=b.Width;atlasHeight=b.Height;var p=new int[atlasWidth*atlasHeight];
    var d=b.LockBits(new Rectangle(0,0,b.Width,b.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
    try{for(int y=0;y<b.Height;y++)Marshal.Copy(IntPtr.Add(d.Scan0,y*d.Stride),p,y*b.Width,b.Width);}finally{b.UnlockBits(d);}atlas=p;
   }
  }
  public static Pose Definition(string name) {foreach(Pose p in Poses)if(p.Name==name)return p;throw new ArgumentException("Unknown character pose: "+name);}
  static bool Inside(string name,int x,int y) {
   double firstEdge=y<240?365:y<340?365+(y-240)*.23:388;
   double pushEdge=y<670?390:390-(y-670)*.025;
   double secondEdge=y<660?750:y<750?750-(y-660)/3.0:720;
   switch(name) {
    case "profile-left":return x<firstEdge;
    case "left":return x>=firstEdge && x<701;
    case "front":return x>=701 && x<1077;
    case "right":return x>=1077 && x<1390;
    case "push-left":return x<pushEdge;
    case "push-right":return x>=pushEdge && x<secondEdge;
    case "crouch":return x>=secondEdge && x<1076;
    case "sleep":return x>=(y<800?1086:1076) && x<1434;
    case "pat":return x>=1434;
    default:return true;
   }
  }
  public static Bitmap Load(string name) {
   Ensure();Pose pose=Definition(name);Rectangle r=pose.Cell;var pixels=new int[r.Width*r.Height];
   for(int y=0;y<r.Height;y++)for(int x=0;x<r.Width;x++)if(Inside(name,x+r.X,y+r.Y) && (r.Y!=0 || y<=pose.Sole))pixels[y*r.Width+x]=atlas[(y+r.Y)*atlasWidth+x+r.X];
   KeepMainIsland(pixels,r.Width,r.Height);
   Bitmap result=new Bitmap(r.Width,r.Height,PixelFormat.Format32bppArgb);var data=result.LockBits(new Rectangle(0,0,r.Width,r.Height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
   try{for(int y=0;y<r.Height;y++)Marshal.Copy(pixels,y*r.Width,IntPtr.Add(data.Scan0,y*data.Stride),r.Width);}finally{result.UnlockBits(data);}result.Tag=pose;return result;
  }
  internal static void KeepMainIsland(int[] pixels,int width,int height) {
   // Remove neighboring islands left by the nonuniform gutters, without repainting.
   bool[] keep=new bool[pixels.Length];int[] queue=new int[pixels.Length];int best=0;bool[] visited=new bool[pixels.Length];
   for(int seed=0;seed<pixels.Length;seed++) {
    if(visited[seed] || ((uint)pixels[seed]>>24)<32)continue;
    int n=1,read=0;queue[0]=seed;visited[seed]=true;
    while(read<n){int k=queue[read++],x=k%width;
     for(int dir=0;dir<4;dir++){int next=dir==0?k-width:dir==1?k+width:dir==2?k-1:k+1;
      if(next<0 || next>=pixels.Length || dir==2 && x==0 || dir==3 && x==width-1)continue;
      if(!visited[next] && ((uint)pixels[next]>>24)>=32){visited[next]=true;queue[n++]=next;}}}
    if(n>best){Array.Clear(keep,0,keep.Length);for(int j=0;j<n;j++)keep[queue[j]]=true;best=n;}
   }
   // Preserve the original soft alpha in a two-pixel fringe of the main island.
   var expanded=(bool[])keep.Clone();
   for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(keep[y*width+x])
    for(int yy=Math.Max(0,y-2);yy<=Math.Min(height-1,y+2);yy++)for(int xx=Math.Max(0,x-2);xx<=Math.Min(width-1,x+2);xx++)expanded[yy*width+xx]=true;
   for(int k=0;k<pixels.Length;k++)if(!expanded[k])pixels[k]=0;
  }
  public static double Scale(Pose p,double size){return size*HeadToNeck/(p.Neck-p.Crown);}
  public static double Top(Pose p){return p.Name=="sleep"?.31:p.Name=="crouch"?.28:p.Name.StartsWith("push-")?.13:0;}
  public static double Y(Pose p,double sourceY,double size) {
   double top=Top(p),neck=top+HeadToNeck;
   return 28+size*(sourceY<=p.Neck?top+(sourceY-p.Crown)*HeadToNeck/(p.Neck-p.Crown):neck+(sourceY-p.Neck)*(.98-neck)/(p.Sole-p.Neck));
  }
  static double SourceY(Pose p,double n) {
   double top=Top(p),neck=top+HeadToNeck;
   return n<=neck?p.Crown+(n-top)/HeadToNeck*(p.Neck-p.Crown):p.Neck+(n-neck)/(.98-neck)*(p.Sole-p.Neck);
  }
  public static Bitmap Normalize(Bitmap source,int w,int h,double size) {
   Pose p=(Pose)source.Tag;double scale=Scale(p,size);var result=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
   using(Graphics g=Graphics.FromImage(result)) {
    g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.CompositingMode=CompositingMode.SourceCopy;
    for(int y=0;y<h;y++) {
     double a=SourceY(p,(y-28)/size)-p.Cell.Y,b=SourceY(p,(y-27)/size)-p.Cell.Y;
     if(a<0 || a>=source.Height)continue;
     g.DrawImage(source,new RectangleF((float)(w*.5+(p.Cell.X-p.Anchor)*scale),y,(float)(source.Width*scale),1),new RectangleF(0,(float)a,source.Width,(float)Math.Min(b-a,source.Height-a)),GraphicsUnit.Pixel);
    }
   }HairLength.Apply(result,p,size);return result;
  }
  public static Bitmap Normalized(string name,int w,int h,double size){using(Bitmap b=Load(name))return Normalize(b,w,h,size);}
  // Ordered anatomical rows for the existing rest and carry interpolation engine.
  public static PoseMesh.Pose Register(Bitmap bitmap,string name,double size) {
   Pose p=Definition(name);double top=Y(p,p.Crown,size),neck=Y(p,p.Neck,size),sole=28+size*.98;
   double[] rows=new double[13];double[] head={-.06,.08,.38,.56,.78,1};
   for(int i=0;i<6;i++)rows[i]=top+head[i]*(neck-top);
   for(int i=6;i<13;i++)rows[i]=neck+(sole+size*.06-neck)*(i-5)/7;
   return PoseMesh.Register(bitmap,rows,bitmap.Width*.5,bitmap.Width*.5,top+(neck-top)*.7);
  }
 }
}

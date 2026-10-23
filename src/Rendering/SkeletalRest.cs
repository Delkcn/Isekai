using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet {
 // 2D skinning plus two same-atlas painted correctives, not sprite playback.
 // No old sleep sheet, generated in-between files, or frame-index timing.
 sealed class SkeletalRest : IDisposable {
  const int Columns=13;
  readonly int width,height;readonly double size;
  readonly RestSkeleton skeleton=new RestSkeleton();
  readonly RestSkeleton.Vertex[] vertices;
  readonly PointF[][] uv=new PointF[3][],correction=new PointF[3][];
  readonly int[][] textures=new int[3][];
  readonly PointF[] target;readonly int[] output;readonly Bitmap canvas;
  internal readonly PoseMesh.Pose[] DiagnosticKeys=new PoseMesh.Pose[3];
  public double MinimumArea=1;
  // Anatomical registration measured on normalized 300px drawings. Hair
  // contour fitting is confined to outer columns, away from hands and face.
  static readonly double[][] Y={
   new double[]{10,28,46,60,78,95,108,118,143,166,193,212,237,270,305,322,340},
   new double[]{94,112,130,144,164,183,195,202,215,235,263,286,298,306,315,323,340}};
  static readonly double[][] Near={
   new double[]{150,150,139,131,145,159,158,160,151,147,139,136,158,158,158,158,158},
   new double[]{137,137,124,117,130,144,146,153,147,145,145,141,139,137,151,151,151}};
  static readonly double[][] Far={
   new double[]{195,195,209,217,202,191,190,190,201,207,209,210,184,184,184,184,184},
   new double[]{182,182,195,199,186,175,176,180,180,190,196,202,209,207,180,180,180}};
  public SkeletalRest(int w,int h,int petSize,Bitmap standing) {
   width=w;height=h;size=petSize;canvas=new Bitmap(w,h,PixelFormat.Format32bppPArgb);output=new int[w*h];
   string[] names={"front","crouch","crouch"};
   for(int i=0;i<3;i++)using(Bitmap art=i==0?(Bitmap)standing.Clone():CharacterArt.Normalized(names[i],w,h,size)) {
    textures[i]=PoseMesh.Read(art);uv[i]=Fit(Math.Min(i,1),textures[i]);DiagnosticKeys[i]=CharacterArt.Register(art,names[i],size);
   }
   textures[2]=textures[1];uv[2]=uv[1];
   vertices=new RestSkeleton.Vertex[uv[0].Length];target=new PointF[vertices.Length];
   for(int v=0;v<vertices.Length;v++)vertices[v]=skeleton.Bind(uv[0][v],width,size);
   for(int i=0;i<3;i++) {
    correction[i]=new PointF[vertices.Length];
    for(int v=0;v<vertices.Length;v++){PointF p=skeleton.Skin(vertices[v],i*.5,width,size),goal=i==2?Settle(uv[i][v]):uv[i][v];correction[i][v]=new PointF(goal.X-p.X,goal.Y-p.Y);}
   }
  }
  PointF Settle(PointF p) {
   // A seated neck joint, preserving head scale. Arms/legs retain the SAME
   // texture throughout settling and steady sleep: no late material swap.
   double ny=(p.Y-28)/size,head=1-IconCarryPlan.Smooth((ny-.55)/.12);
   double x=p.X-(width*.5-size*.055),y=p.Y-(28+size*.58),angle=-.065;
   double rx=x*Math.Cos(angle)-y*Math.Sin(angle),ry=x*Math.Sin(angle)+y*Math.Cos(angle);
   return new PointF((float)(p.X+(rx-x)*head),(float)(p.Y+(ry-y+size*.012)*head));
  }
  PointF[] Fit(int view,int[] pixels) {
   var result=new PointF[Y[0].Length*Columns];
   for(int row=0;row<Y[0].Length;row++) {
    double y=28+(Y[view][row]-28)*size/300,near=width*.5+(Near[view][row]-173.5)*size/300,far=width*.5+(Far[view][row]-173.5)*size/300;
    int lo=width,hi=-1;
    for(int yy=Math.Max(0,(int)y-2);yy<=Math.Min(height-1,(int)y+2);yy++)for(int x=0;x<width;x++)if(((uint)pixels[yy*width+x]>>24)>32){lo=Math.Min(lo,x);hi=Math.Max(hi,x);}
    if(row>=8){near=width*.5+((view==0?136:141)-173.5)*size/300;far=width*.5+((view==0?210:202)-173.5)*size/300;}
    double innerLeft=near-size*.025,innerRight=far+size*.025,left=Math.Min(lo-2,innerLeft-size*.02),right=Math.Max(hi+2,innerRight+size*.02);
    double[] xs={0,left,(left+innerLeft)/2,innerLeft,near,Lerp(near,far,.25),(near+far)/2,Lerp(near,far,.75),far,innerRight,(innerRight+right)/2,right,width-1};
    if(row>=8){xs[5]=width*.5+(157-173.5)*size/300;xs[6]=width*.5+((view==0?175:166)-173.5)*size/300;xs[7]=width*.5+((view==0?184:173)-173.5)*size/300;}
    for(int c=0;c<Columns;c++) {
     double cy=y;
     if(view==1)cy-=(xs[c]-(width*.5-size*.04))*.18*(1-IconCarryPlan.Smooth((Y[view][row]-195)/90));
     // Exposed right forearm ends lower in the seated art; register its cuff
     // and fingers independently of the knees and the coat hem.
     if(view==1 && row>=9 && row<=13 && c>=3 && c<=5) {
      double[] armY={236,268,292,302,308};double weight=c==4?1:.55;
      cy+=((armY[row-9]-Y[view][row])*size/300)*weight;
     }
     if(view==1 && row>=8 && row<=13 && c>=5 && c<=7) {
      double[] legY={210,215,220,226,235,282};double weight=c==5?.15:c==6?.75:1;
      cy+=(legY[row-8]-Y[view][row])*size/300*weight;
     }
     result[row*Columns+c]=new PointF((float)xs[c],(float)cy);
    }
   }
   return result;
  }
  static double Lerp(double a,double b,double t){return a+(b-a)*t;}
  static double Curve(double a,double b,double c,double p) {
   double m=PoseInterpolation.Tangent((float)(b-a),(float)(c-b));
   return p<=.5?a+PoseInterpolation.Hermite(b-a,b-a,m,p*2):b+PoseInterpolation.Hermite(c-b,m,c-b,(p-.5)*2);
  }
  public Bitmap Draw(Bitmap standing,double amount) {
   if(amount<.5)textures[0]=PoseMesh.Read(standing);
   for(int v=0;v<vertices.Length;v++) {
    PointF p=skeleton.Skin(vertices[v],amount,width,size);
    target[v]=new PointF((float)(p.X+Curve(correction[0][v].X,correction[1][v].X,correction[2][v].X,amount)),(float)(p.Y+Curve(correction[0][v].Y,correction[1][v].Y,correction[2][v].Y,amount)));
   }
   int a=amount<.5?0:1,b=a+1;double blend=IconCarryPlan.Smooth(amount*2-a);
   Array.Clear(output,0,output.Length);MinimumArea=double.MaxValue;
   for(int r=0;r<Y[0].Length-1;r++)for(int c=0;c<Columns-1;c++){int k=r*Columns+c;Triangle(k,k+1,k+Columns,a,b,blend);Triangle(k+1,k+Columns+1,k+Columns,a,b,blend);}
   var data=canvas.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
   try{for(int y=0;y<height;y++)Marshal.Copy(output,y*width,IntPtr.Add(data.Scan0,y*data.Stride),width);}finally{canvas.UnlockBits(data);}return canvas;
  }
  void Triangle(int i,int j,int k,int from,int to,double blend) {
   PointF a=target[i],b=target[j],c=target[k];double area=(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);MinimumArea=Math.Min(MinimumArea,area);
   if(area<=0)throw new InvalidOperationException("Sleep mesh folded: "+i+","+j+","+k+" area="+area+" blend="+blend);
   int x0=Math.Max(0,(int)Math.Floor(Math.Min(a.X,Math.Min(b.X,c.X)))),x1=Math.Min(width-1,(int)Math.Ceiling(Math.Max(a.X,Math.Max(b.X,c.X))));
   int y0=Math.Max(0,(int)Math.Floor(Math.Min(a.Y,Math.Min(b.Y,c.Y)))),y1=Math.Min(height-1,(int)Math.Ceiling(Math.Max(a.Y,Math.Max(b.Y,c.Y))));
   for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++) {
    double v=((x-a.X)*(c.Y-a.Y)-(y-a.Y)*(c.X-a.X))/area,w=((b.X-a.X)*(y-a.Y)-(b.Y-a.Y)*(x-a.X))/area,u=1-v-w;
    if(u<-.00001||v<-.00001||w<-.00001)continue;
    uint p=Sample(textures[from],uv[from][i].X*u+uv[from][j].X*v+uv[from][k].X*w,uv[from][i].Y*u+uv[from][j].Y*v+uv[from][k].Y*w);
    if(from==1){output[y*width+x]=(int)p;continue;} // same seated skin, no redundant sampling/blending
    uint q=Sample(textures[to],uv[to][i].X*u+uv[to][j].X*v+uv[to][k].X*w,uv[to][i].Y*u+uv[to][j].Y*v+uv[to][k].Y*w),value=0;
    for(int s=0;s<32;s+=8)value|=(uint)Math.Round(Lerp(p>>s&255,q>>s&255,blend))<<s;
    output[y*width+x]=(int)value;
   }
  }
  uint Sample(int[] pixels,double x,double y) {
   int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);if(ix<0||iy<0||ix>=width-1||iy>=height-1)return 0;
   uint a=(uint)pixels[iy*width+ix],b=(uint)pixels[iy*width+ix+1],c=(uint)pixels[(iy+1)*width+ix],d=(uint)pixels[(iy+1)*width+ix+1];
   if((a|b|c|d)==0)return 0;
   double fx=x-ix,fy=y-iy;uint result=0;
   for(int s=0;s<32;s+=8)result|=(uint)Math.Round(Lerp(Lerp(a>>s&255,b>>s&255,fx),Lerp(c>>s&255,d>>s&255,fx),fy))<<s;return result;
  }
  public void Dispose(){canvas.Dispose();}
 }
}

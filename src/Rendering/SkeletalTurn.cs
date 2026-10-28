using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet {
 // Runtime linear blend skinning, sparse view correctives and registered
 // materials. Five textures regardless of duration/frame rate; no frame cache.
 sealed class SkeletalTurn : IDisposable {
  const int Columns=13;
  static readonly double[] Bands={-.065,0,.04,.105,.14,.18,.225,.255,.30,.34,.39,.45,.51,.57,.63,.70,.75,.84,.94,.98,1.04};
  readonly int width,height;readonly double size;readonly CharacterForm form;
  readonly CharacterSkeleton skeleton=new CharacterSkeleton();
  readonly CharacterSkeleton.Vertex[] vertices;
  readonly PointF[][] uv=new PointF[5][],correction=new PointF[5][];
  readonly PointF[][] proportions=new PointF[5][];
  readonly int[][] textures=new int[5][];
  readonly PointF[] target;readonly Bitmap canvas;readonly int[] output;
  public double MinimumArea=1;
  public int MaterialCount {get{return textures.Length;}}
  public SkeletalTurn(Bitmap[] views,int w,int h,double petSize,CharacterForm form=CharacterForm.AnemoneI) {
   this.form=form;width=w;height=h;size=petSize;canvas=new Bitmap(w,h,PixelFormat.Format32bppPArgb);output=new int[w*h];
   for(int i=0;i<5;i++){textures[i]=PoseMesh.Read(views[i]);uv[i]=Fit(i,textures[i]);}
   // Texture coordinates describe each independent painting. They must not also
   // dictate anatomy independently of calibration. The v064 paintings are already
   // registered at import; do not widen them again using the old corrective scale.
   for(int i=0;i<5;i++)proportions[i]=(PointF[])uv[i].Clone();
   vertices=new CharacterSkeleton.Vertex[uv[2].Length];target=new PointF[vertices.Length];
   for(int v=0;v<vertices.Length;v++)vertices[v]=skeleton.Bind(uv[2][v],w,size);
   for(int i=0;i<5;i++) {
    correction[i]=new PointF[vertices.Length];double yaw=(i-2)*Math.PI/4;
    for(int v=0;v<vertices.Length;v++){PointF p=skeleton.Skin(vertices[v],yaw,w,size);correction[i][v]=new PointF(proportions[i][v].X-p.X,proportions[i][v].Y-p.Y);}
   }
  }
  // Corresponding landmarks measured on the approved normalized views at 300px.
  // Columns 4/8 follow anatomical right/left features, including the hidden eye.
  // Flower, iris, cuff and hand registration avoids two drifting copies during blending.
  static readonly double[] ControlRows={0,.105,.225,.30,.45,.63,.75,.94,.98};
  static readonly double[][] FeatureY={
   new double[]{28,55,95,118,162,216,250,310,322},
   new double[]{28,55,95,118,162,216,250,310,322},
   new double[]{28,60,95,118,164,216,250,310,322},
   new double[]{28,58,95,118,160,216,250,310,322},
   new double[]{28,55,95,118,162,216,250,310,322}};
  static readonly double[][] NearX={
   new double[]{155,135,148,160,157,156,166,165,165},
   new double[]{155,135,148,160,157,156,166,165,165},
   new double[]{150,130,153,160,147,136,160,160,160},
   new double[]{155,153,180,174,159,147,168,165,165},
   new double[]{155,166,188,180,166,160,174,174,174}};
  static readonly double[][] FarX={
   new double[]{198,195,166,182,189,185,181,181,181},
   new double[]{198,193,166,182,189,188,181,181,181},
   new double[]{195,217,191,190,207,210,184,184,184},
   new double[]{198,219,203,192,199,204,188,185,185},
   new double[]{198,219,206,193,198,199,191,190,190}};
  static double Feature(double[] values,double n) {
   int k=0;while(k<ControlRows.Length-2 && n>ControlRows[k+1])k++;
   return Lerp(values[k],values[k+1],(n-ControlRows[k])/(ControlRows[k+1]-ControlRows[k]));
  }
  PointF[] Fit(int view,int[] pixels) {
   var points=new PointF[Bands.Length*Columns];
   for(int row=0;row<Bands.Length;row++) {
    double n=Bands[row],y=28+(Feature(FeatureY[view],n)-28)*size/300;
    double near=width*.5+(Feature(NearX[view],n)-173.5)*size/300,far=width*.5+(Feature(FarX[view],n)-173.5)*size/300;
    double left=near-size*.04,right=far+size*.04;int lo=width,hi=-1;
    for(int yy=Math.Max(0,(int)y-2);yy<=Math.Min(height-1,(int)y+2);yy++)for(int x=0;x<width;x++)if(((uint)pixels[yy*width+x]>>24)>32){lo=Math.Min(lo,x);hi=Math.Max(hi,x);}
    if(hi>lo){left=Math.Min(left,lo-2);right=Math.Max(right,hi+2);}
    double innerLeft=near-size*.025,innerRight=far+size*.025;
    left=Math.Min(left,innerLeft-size*.008);right=Math.Max(right,innerRight+size*.008);
    double[] xs={0,left,Lerp(left,innerLeft,.5),innerLeft,near,Lerp(near,far,.25),Lerp(near,far,.5),Lerp(near,far,.75),far,innerRight,Lerp(innerRight,right,.5),right,width-1};
    for(int c=1;c<Columns;c++)xs[c]=Math.Max(xs[c],xs[c-1]+.15);
    for(int c=0;c<Columns;c++)points[row*Columns+c]=new PointF((float)xs[c],(float)y);
   }
   if(form==CharacterForm.AnemoneII)A2TurnRegistration.Apply(points,view,width,size);
   return points;
  }
  static double Lerp(double a,double b,double t){return a+(b-a)*t;}
  public Bitmap Draw(double angle,double pivotProgress=-1,double fromAngle=0,bool supportNear=false) {
   angle=Math.Max(-1,Math.Min(1,angle));double position=(angle+1)*2;int a=Math.Min(3,(int)position),b=a+1;double t=position-a,yaw=angle*Math.PI*.5;
   for(int v=0;v<vertices.Length;v++) {
    PointF p=skeleton.Skin(vertices[v],yaw,width,size),ca=correction[a][v],cb=correction[b][v],prev=correction[Math.Max(0,a-1)][v],next=correction[Math.Min(4,b+1)][v];
    // A2's independent front head has different flower/eye heights. Linear Y
    // blending reverses velocity abruptly at the front key; use the existing
    // bounded Hermite interpolation for those head correctives as well as X.
    double cy=form==CharacterForm.AnemoneII&&v<9*Columns?PoseMesh.Coordinate(prev.Y,ca.Y,cb.Y,next.Y,t):Lerp(ca.Y,cb.Y,t);
    target[v]=new PointF((float)(p.X+PoseMesh.Coordinate(prev.X,ca.X,cb.X,next.X,t)),(float)(p.Y+cy));
   }
   if(form==CharacterForm.AnemoneII)for(int r=0;r<Bands.Length;r++){
    // Independent Hermite tracks can let an intermediate outer column pass
    // its silhouette neighbour. Constrain that column continuously, rather
    // than snapping the entire row (including the shoulder outline) to linear.
    KeepOuterColumns(r*Columns+4,-1,4);KeepOuterColumns(r*Columns+8,1,4);
   }
   for(int r=0;r<Bands.Length;r++)for(int c=1;c<Columns;c++) {
    int k=r*Columns+c;if(target[k].X<=target[k-1].X+.1f) {
     for(int j=0;j<Columns;j++){int q=r*Columns+j;target[q]=new PointF((float)Lerp(proportions[a][q].X,proportions[b][q].X,t),(float)Lerp(proportions[a][q].Y,proportions[b][q].Y,t));}break;
    }
   }
   if(pivotProgress>0 && pivotProgress<1) {
    int row=19*Columns,foot=supportNear?4:8;
    PointF support=target[row+foot],anchor=FootAt(fromAngle,foot);
    for(int r=0;r<Bands.Length;r++) {
     double near=target[r*Columns+4].X,far=target[r*Columns+8].X;
     for(int c=0;c<Columns;c++){int k=r*Columns+c;target[k]=PivotTurn.Pose(target[k],28+size*.64,28+size*.98,near,far,support,anchor,pivotProgress,supportNear,size);}
    }
   }
   Array.Clear(output,0,output.Length);MinimumArea=double.MaxValue;
   // Both paintings are sampled through the same anatomical mesh. A hard
   // material swap at t=.5 made the right turn jump at angles .25 and .75.
   double blend=t*t*t*(t*(t*6-15)+10);
   double firstWeight=1-blend,secondWeight=blend,lastWeight=0;
   if(form==CharacterForm.AnemoneII&&angle>0){
    // The independent A2 front and right paintings differ more than A1's.
    // Restarting a quintic texture fade at the oblique key freezes appearance
    // halfway through a front-to-right turn. Carry all three registered views
    // along one curve, rounding only the oblique junction. At that junction
    // the oblique view remains 92% dominant to avoid a three-image ghost.
    a=2;b=3;RightWeights(angle,out firstWeight,out secondWeight,out lastWeight);
   }
   for(int r=0;r<Bands.Length-1;r++)for(int c=0;c<Columns-1;c++){int k=r*Columns+c;Triangle(k,k+1,k+Columns,a,b,firstWeight,secondWeight,lastWeight);Triangle(k+1,k+Columns+1,k+Columns,a,b,firstWeight,secondWeight,lastWeight);}
   var data=canvas.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
   try{for(int y=0;y<height;y++)Marshal.Copy(output,y*width,IntPtr.Add(data.Scan0,y*data.Stride),width);}finally{canvas.UnlockBits(data);}
   return canvas;
  }
  void KeepOuterColumns(int anchor,int direction,int count){
   double original=target[anchor].X;
   for(int step=1;step<=count;step++){
    int k=anchor+step*direction;double next=target[k].X,gap=(next-original)*direction;
    // Fit's minimum spacing is .15 px; this guard becomes active below .14
    // and keeps .12 px, safely above the legacy fallback's .10 px threshold.
    target[k].X=(float)(target[k-direction].X+direction*SoftMaximum(gap,.12,.02));original=next;
   }
  }
  static double SoftMaximum(double x,double floor,double band){
   double d=x-floor;if(d>=band)return x;if(d<=-band)return floor;
   return floor+(d+band)*(d+band)/(4*band);
  }
  internal static void RightWeights(double angle,out double front,out double oblique,out double side){
   double q=Math.Max(0,Math.Min(1,angle));const double edge=.12;
   if(q<edge)q=q*q*(2*edge-q)/(edge*edge);
   else if(q>1-edge){double tail=1-q;q=1-tail*tail*(2*edge-tail)/(edge*edge);}
   front=SoftMaximum(1-2*q,0,.16);side=SoftMaximum(2*q-1,0,.16);oblique=1-front-side;
  }
  PointF FootAt(double angle,int column) {
   double position=(Math.Max(-1,Math.Min(1,angle))+1)*2;int a=Math.Min(3,(int)position),b=a+1;double t=position-a;
   int v=19*Columns+column;PointF p=skeleton.Skin(vertices[v],angle*Math.PI*.5,width,size),ca=correction[a][v],cb=correction[b][v],prev=correction[Math.Max(0,a-1)][v],next=correction[Math.Min(4,b+1)][v];
   return new PointF((float)(p.X+PoseMesh.Coordinate(prev.X,ca.X,cb.X,next.X,t)),(float)(p.Y+Lerp(ca.Y,cb.Y,t)));
  }
  void Triangle(int i,int j,int k,int a,int b,double firstWeight,double secondWeight,double lastWeight) {
   PointF p=target[i],q=target[j],r=target[k];double area=(q.X-p.X)*(r.Y-p.Y)-(q.Y-p.Y)*(r.X-p.X);MinimumArea=Math.Min(MinimumArea,area);
   if(area<=0)throw new InvalidOperationException("Inverted skeletal surface");
   int x0=Math.Max(0,(int)Math.Floor(Math.Min(p.X,Math.Min(q.X,r.X)))),x1=Math.Min(width-1,(int)Math.Ceiling(Math.Max(p.X,Math.Max(q.X,r.X))));
   int y0=Math.Max(0,(int)Math.Floor(Math.Min(p.Y,Math.Min(q.Y,r.Y)))),y1=Math.Min(height-1,(int)Math.Ceiling(Math.Max(p.Y,Math.Max(q.Y,r.Y))));
   for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++) {
    double v=((x-p.X)*(r.Y-p.Y)-(y-p.Y)*(r.X-p.X))/area,w=((q.X-p.X)*(y-p.Y)-(q.Y-p.Y)*(x-p.X))/area,u=1-v-w;
    if(u<-.00001 || v<-.00001 || w<-.00001)continue;
    uint first=firstWeight<=0?0:Sample(textures[a],uv[a][i].X*u+uv[a][j].X*v+uv[a][k].X*w,uv[a][i].Y*u+uv[a][j].Y*v+uv[a][k].Y*w);
    uint second=secondWeight<=0?0:Sample(textures[b],uv[b][i].X*u+uv[b][j].X*v+uv[b][k].X*w,uv[b][i].Y*u+uv[b][j].Y*v+uv[b][k].Y*w);
    uint last=lastWeight<=0?0:Sample(textures[4],uv[4][i].X*u+uv[4][j].X*v+uv[4][k].X*w,uv[4][i].Y*u+uv[4][j].Y*v+uv[4][k].Y*w);
    if(first==second&&lastWeight<=0){output[y*width+x]=(int)first;continue;}
    uint pixel=0;
    for(int shift=0;shift<32;shift+=8)pixel|=(uint)Math.Round(((first>>shift)&255)*firstWeight+((second>>shift)&255)*secondWeight+((last>>shift)&255)*lastWeight)<<shift;
    output[y*width+x]=(int)pixel;
   }
  }
  uint Sample(int[] pixels,double x,double y) {
   int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);if(ix<0 || iy<0 || ix>=width-1 || iy>=height-1)return 0;
   double fx=x-ix,fy=y-iy;uint a=(uint)pixels[iy*width+ix],b=(uint)pixels[iy*width+ix+1],c=(uint)pixels[(iy+1)*width+ix],d=(uint)pixels[(iy+1)*width+ix+1],result=0;
   if((a|b|c|d)==0)return 0;
   for(int shift=0;shift<32;shift+=8)result|=(uint)Math.Round((((a>>shift)&255)*(1-fx)+((b>>shift)&255)*fx)*(1-fy)+(((c>>shift)&255)*(1-fx)+((d>>shift)&255)*fx)*fy)<<shift;
   return result;
  }
  public void DrawSkeleton(Graphics g,double angle){skeleton.DrawDebug(g,angle*Math.PI*.5,width,size);}
  public void Dispose(){canvas.Dispose();}
 }
}

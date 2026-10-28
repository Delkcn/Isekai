using System;
namespace JouchoPet {
 // A2's separate front painting has a shorter skirt than the shared A1 atlas.
 // Register only the lower coat; preserve the legs as a foreground layer.
 static class A2CoatLength {
  static double Lerp(double y,double y0,double y1,double a,double b){return a+(b-a)*Math.Max(0,Math.Min(1,(y-y0)/(y1-y0)));}
  static bool Leg(int x,double y){
   // Traced in the 380 x 465 canonical import, not in screen coordinates.
   // A color threshold would mistake the old dark hem for a boot edge.
   double left=y<330?Lerp(y,306,330,178,155):Lerp(y,330,410,155,160);
   double right=Lerp(y,306,410,184,180);
   bool near=x>=left&&x<=right;
   left=Lerp(y,306,410,188,199);right=Lerp(y,306,410,223,214);
   return near||(x>=left&&x<=right);
  }
  static bool FrontFlap(int x,double y){
   return x<185?y<350+(x-110)*(305-350.0)/(178-110):y<350+(x-264)*(306-350.0)/(220-264);
  }
  static double SourceY(double y,int panel){
   // Root stays below the hands. Front tips and rear lining have independent
   // anchors: extending the entire body would also stretch the knee and laces.
   double root=306,oldTip=350,newTip=panel==0?362:panel==1?363:360;
   double oldHem=panel==0?382:panel==1?377:380,newHem=panel==0?394:panel==1?400:386;
   return y<=newTip?root+(y-root)*(oldTip-root)/(newTip-root):oldTip+(y-newTip)*(oldHem-oldTip)/(newHem-newTip);
  }
  internal static void Apply(int[] pixels,int width,int height){
   if(width!=380||height!=465||pixels.Length!=width*height)throw new ArgumentException("A2 coat registration requires the canonical front import.");
   var original=(int[])pixels.Clone();var coat=(int[])pixels.Clone();
   for(int y=306;y<410;y++)for(int x=90;x<280;x++)if(Leg(x,y)){
    // Underlap the lining behind unchanged legs before its vertical warp.
    coat[y*width+x]=0;
    for(int d=1;d<=12;d++){
     int l=x-d,r=x+d;
     if(l>=90&&!Leg(l,y)&&original[y*width+l]!=0){coat[y*width+x]=original[y*width+l];break;}
     if(r<280&&!Leg(r,y)&&original[y*width+r]!=0){coat[y*width+x]=original[y*width+r];break;}
    }
   }
   for(int y=306;y<410;y++)for(int x=90;x<280;x++){
    double sy=SourceY(y,x<184?0:x<195?1:2);
    // Front flaps may cover more of the leg; rear lining stays behind it.
    if(Leg(x,y)&&!FrontFlap(x,sy))continue;
    int k=y*width+x,iy=(int)Math.Floor(sy);double f=sy-iy;
    uint a=(uint)coat[iy*width+x],b=(uint)coat[(iy+1)*width+x],value=0;
    for(int shift=0;shift<32;shift+=8)value|=(uint)Math.Round(((a>>shift)&255)*(1-f)+((b>>shift)&255)*f)<<shift;
    pixels[k]=(int)value;
   }
  }
 }
}

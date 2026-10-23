using System;
namespace JouchoPet {
 // Close only the visible iris beneath the fringe. The actual hair boundary is
 // different in every column, so a rectangular eye warp would flatten the bangs.
 sealed class CoveredEyeRig {
  readonly double[] top,bottom;
  readonly double feather;
  readonly bool complete;
  readonly double canvasWidth,petSize;
  public CoveredEyeRig(int[] pixels,int width,int height,double size,bool completeAperture=false) {
   top=new double[width];bottom=new double[width];feather=Math.Max(1.5,size*.009);
   complete=completeAperture;canvasWidth=width;petSize=size;
   for(int x=0;x<width;x++)top[x]=bottom[x]=-1;
   if(completeAperture){
    // Registered v0610 front aperture: iris, white and lower lash together.
    // The upper contour follows the fringe; leave all pixels above it fixed.
    double[] upper={133,132,130.5,129,127.5,125.5,124.5,123.5,123.5,123.5,124,125,128};
    double[] lower={134,135,135,135,135,134.5,134,134,133.5,133,132,131,130};
    // The outer corner is a separate warm-red stroke at x852..860, not hair.
    // Close that aperture as well, preserving the fringe between both regions.
    double[] cornerUpper={125,124,124,124.5,125,127,130,134,136};
    double[] cornerLower={129,132,135,137,138,138,138,138,138};
    var face=CharacterArt.Definition("front");double scale=CharacterArt.Scale(face,size);
    for(int x=0;x<width;x++){
     double local=face.Anchor+(x-width*.5)/scale-864;
     double[] hi=upper,lo=lower;
     if(local<0){local+=12;hi=cornerUpper;lo=cornerLower;}
     if(local<0||local>=hi.Length-1)continue;
     int i=(int)local;double f=local-i;
     top[x]=CharacterArt.Y(face,hi[i]+f*(hi[i+1]-hi[i]),size);
     bottom[x]=CharacterArt.Y(face,lo[i]+f*(lo[i+1]-lo[i]),size);
    }
    return;
   }
   int left=Math.Max(0,(int)(width*.5-size*.11)),right=Math.Min(width-1,(int)(width*.5-size*.020));
   int first=Math.Max(0,(int)(28+size*.20)),last=Math.Min(height-1,(int)(28+size*.29));
   bool[] mask=new bool[pixels.Length],seen=new bool[pixels.Length],best=new bool[pixels.Length];int[] queue=new int[pixels.Length];int largest=0;
   for(int y=first;y<=last;y++)for(int x=left;x<=right;x++) {
    uint p=(uint)pixels[y*width+x];double a=p>>24;if(a<180)continue;
    double r=((p>>16)&255)*255/a,g=((p>>8)&255)*255/a,b=(p&255)*255/a;
    // The v067 iris is pale rose red. Its blue channel exceeds green, unlike
    // the warm skin and fringe. Keep the existing per-column closure mapping.
    mask[y*width+x]=r>145 && r-g>25 && r-b>15 && b-g>3;
   }
   // Isolate the iris from isolated red/brown fringe accents in the same ROI.
   for(int y=first;y<=last;y++)for(int x=left;x<=right;x++) {
    int seed=y*width+x;if(!mask[seed]||seen[seed])continue;
    int count=1,read=0;queue[0]=seed;seen[seed]=true;
    while(read<count){int k=queue[read++],xx=k%width,yy=k/width;
     for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){
      int nx=xx+dx,ny=yy+dy;if(nx<left||nx>right||ny<first||ny>last)continue;int n=ny*width+nx;
      if(mask[n]&&!seen[n]){seen[n]=true;queue[count++]=n;}
     }
    }
    if(count>largest){Array.Clear(best,0,best.Length);for(int i=0;i<count;i++)best[queue[i]]=true;largest=count;}
   }
   if(largest<3)return;
   for(int x=left;x<=right;x++) {
    int lo=height,hi=-1;
    for(int y=first;y<=last;y++)if(best[y*width+x]){lo=Math.Min(lo,y);hi=Math.Max(hi,y);}
    if(hi>=lo){top[x]=lo-.55;bottom[x]=hi+1.1;}
   }
  }
  public uint Lashes(uint pixel,double x,double y,double closure) {
   if(!complete||closure<=.55)return pixel;
   var face=CharacterArt.Definition("front");double scale=CharacterArt.Scale(face,petSize);
   double sx=face.Anchor+(x-canvasWidth*.5)/scale;
   if(sx<866||sx>875)return pixel;
   double sy=face.Crown+(y-28)/scale,u=(sx-870.5)/4.5;
   // A short, tapered dark-brown lid and a tiny outer lash, clipped to the
   // visible aperture. Coordinates precede head motion and the closure warp.
   double lid=127.6+.7*u*u;
   double coverage=Math.Max(0,1-Math.Abs(sy-lid)/.85)*Math.Max(0,1-Math.Pow(Math.Abs(u),4));
   double lash=128.1+(sx-867)*.7;
   if(sx>=866.5&&sx<=868.2)coverage=Math.Max(coverage,Math.Max(0,1-Math.Abs(sy-lash)/.65)*.8);
   int column=(int)Math.Round(x);
   if(column<0||column>=top.Length||top[column]<0||y<top[column]+.25)return pixel;
   coverage*=Math.Min(1,(closure-.55)/.45)*.90;
   uint alpha=pixel>>24,result=alpha<<24;int[] ink={39,38,49};
   for(int c=0;c<3;c++)result|=(uint)Math.Round(((pixel>>(c*8))&255)*(1-coverage)+ink[c]*alpha/255.0*coverage)<<(c*8);
   return result;
  }
  public double SourceY(double x,double y,double closure) {
   if(closure<=0||x<0||x>=top.Length-1)return y;
   int i=(int)x;double fraction=x-i;
   // Use the occupied column at the edge rather than bleeding into the hair.
   int column=fraction<.5?i:i+1;
   if(top[column]<0)return y;
   double upper=top[column],lower=bottom[column];
   if(top[i]>=0&&top[i+1]>=0){upper=top[i]+fraction*(top[i+1]-top[i]);lower=bottom[i]+fraction*(bottom[i+1]-bottom[i]);}
   double end=lower+feather;if(y<=upper||y>=end)return y;
   double closed=lower+(upper+.12-lower)*Math.Min(1,closure);
   if(y<closed)return upper+(y-upper)*(lower-upper)/(closed-upper);
   return lower+(y-closed)*feather/(end-closed);
  }
 }
}

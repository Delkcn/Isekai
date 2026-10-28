using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet {
 // Register lower coat panels across all four A2 side paintings. Coordinates
 // are local to the atlas cells; head, hands, legs and the shared rig stay fixed.
 static class A2SideCoatLength {
  static bool Inside(PointF[] p,double x,double y){bool b=false;for(int i=0,j=p.Length-1;i<p.Length;j=i++)if((p[i].Y>y)!=(p[j].Y>y)&&x<(p[j].X-p[i].X)*(y-p[i].Y)/(p[j].Y-p[i].Y)+p[i].X)b=!b;return b;}
  static PointF[] Points(params float[] values){var p=new PointF[values.Length/2];for(int i=0;i<p.Length;i++)p[i]=new PointF(values[i*2],values[i*2+1]);return p;}
  static double Mix(double a,double b,double t){return a+(b-a)*Math.Max(0,Math.Min(1,t));}
  static double Source(double y,double tip,double hem,double newTip,double newHem){return y<=newTip?310+(y-310)*(tip-310)/(newTip-310):tip+(y-newTip)*(hem-tip)/(newHem-newTip);}
  static bool Flap(string name,double x,double y){
   if(name=="left")x+=30;
   if(name=="left"||name=="profile-left")return x<145?y<349+(x-93)*(320-349.0)/40:y<355+(x-238)*(315-355.0)/(-78);
   if(name=="right")return x<210?y<372+(x-123)*(324-372.0)/82:y<350+(x-265)*(310-350.0)/(-21);
   return x<240?y<375+(x-154)*(332-375.0)/79:y<340+(x-302)*(318-340.0)/(-35);
  }
  static double Influence(string name,double x){
   if(name=="left")x+=30;
   if(name=="left"||name=="profile-left")return x<165?Mix(1,0,(x-93)/42):Mix(0,1,(x-199)/39);
   if(name=="right")return x<210?Mix(1,0,(x-123)/39):Mix(0,1,(x-242)/23);
   return x<235?Mix(1,0,(x-154)/44):Mix(0,1,(x-267)/35);
  }
  internal static void Apply(Bitmap picture,CharacterArt.Pose pose,double size){
   int width=picture.Width,height=picture.Height;var pixels=PoseMesh.Read(picture);
   double scaleX=CharacterArt.Scale(pose,size),scaleY=size*.68/(pose.Sole-pose.Neck);
   Func<double,double> X=x=>(x-width*.5)/scaleX+pose.Anchor-pose.Cell.X;
   Func<double,double> Y=y=>(y-28-size*.30)/scaleY+pose.Neck;
   Func<double,double> ScreenY=y=>28+size*.30+(y-pose.Neck)*scaleY;
   int start=Math.Max(1,(int)Math.Ceiling(ScreenY(310))),end=Math.Min(height-2,(int)Math.Ceiling(ScreenY(410)));
   bool left=pose.Name=="left"||pose.Name=="profile-left";
   bool right=pose.Name=="right"||pose.Name=="profile-right";
   if(!left&&!right)return;
   double offset=left?(pose.Name=="left"?-30:0):0;
   PointF[] bounds,legs;
   if(left){bounds=Points(121,310,223,310,242,355,212,378,188,385,134,384,90,350);legs=Points(136,310,170,315,190,346,194,412,139,412,137,351,129,330);}
   else if(pose.Name=="right"){bounds=Points(146,310,253,310,269,353,240,376,214,395,159,403,119,373);legs=Points(205,324,243,310,239,343,227,402,218,412,159,412,160,352);}
   else{bounds=Points(177,310,289,310,307,342,279,366,250,383,187,398,150,377);legs=Points(233,332,267,318,260,347,247,410,196,415,196,354);}
   if(offset!=0)for(int i=0;i<bounds.Length;i++)bounds[i].X+=(float)offset;
   if(offset!=0)for(int i=0;i<legs.Length;i++)legs[i].X+=(float)offset;
   foreach(var polygon in new[]{bounds,legs})for(int i=0;i<polygon.Length;i++)polygon[i]=new PointF((float)(width*.5+(polygon[i].X-pose.Anchor+pose.Cell.X)*scaleX),(float)ScreenY(polygon[i].Y));
   var original=(int[])pixels.Clone();var core=new bool[pixels.Length];var mask=new bool[pixels.Length];
   for(int y=start;y<end;y++)for(int x=0;x<width;x++){
    uint p=(uint)original[y*width+x];int r=(int)(p>>16&255),g=(int)(p>>8&255),b=(int)(p&255);
    if((p>>24)>80&&Inside(bounds,x,y)&&(!Inside(legs,x,y)||Flap(pose.Name,X(x),Y(y)))&&r-g<35&&r-b<35)core[y*width+x]=true;
   }
   for(int y=start;y<end;y++)for(int x=1;x<width-1;x++)if(core[y*width+x])for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(y+dy>=start&&(!Inside(legs,x+dx,y+dy)||Flap(pose.Name,X(x+dx),Y(y+dy))))mask[(y+dy)*width+x+dx]=true;
   // Retain the surrounding hair/background when a raised hem exposes it.
   for(int y=start;y<end;y++)for(int x=1;x<width-1;x++)if(mask[y*width+x]&&Influence(pose.Name,X(x))>0){
    int replacement=0;bool found=false;
    for(int d=1;d<Math.Ceiling(70*scaleX)&&!found;d++)foreach(int direction in new[]{0,-1,1}){
     int xx=x+direction*d,yy=y+(direction==0?d:0);if(xx<0||xx>=width||yy>=height||mask[yy*width+xx]||Inside(legs,xx,yy))continue;
     uint q=(uint)original[yy*width+xx];int r=(int)(q>>16&255),b=(int)(q&255);
     if((q>>24)>20&&r-b<20)continue;
     replacement=original[yy*width+xx];found=true;break;
    }
    pixels[y*width+x]=replacement;
   }
   double targetTip=pose.Neck+((pose.Name=="profile-right"?362:pose.Name=="profile-left"?371:365)-159)*(pose.Sole-pose.Neck)/303;
   double targetHem=pose.Neck+((left?400:394)-159)*(pose.Sole-pose.Neck)/303;
   for(int y=start;y<end;y++)for(int x=1;x<width-1;x++){
    double tip,hem,cx=X(x),cy=Y(y);
    if(left){tip=Mix(349,355,(cx-offset-130)/60);hem=380;}
    else if(pose.Name=="right"){tip=Mix(372,350,(cx-175)/50);hem=Mix(397,374,(cx-175)/50);}
    else{tip=Mix(375,340,(cx-216)/48);hem=Mix(394,365,(cx-216)/48);}
    double influence=Influence(pose.Name,X(x));if(left&&cy>355)influence=Math.Max(influence,Math.Min(1,(cy-355)/25.0));if(influence==0)continue;double sy=ScreenY(Mix(cy,Source(cy,tip,hem,targetTip,targetHem),influence));int iy=(int)Math.Floor(sy);if(iy<start||iy>=height-1){pixels[y*width+x]=original[y*width+x];continue;}double f=sy-iy;
    int k0=iy*width+x,k1=k0+width;double a=mask[k0]?1-f:0,b=mask[k1]?f:0;if(a+b<.0001)continue;
    // Keep the exposed leg in front of the lining. The extended front edge can
    // cover the narrow area immediately below its previous position.
    if(Inside(legs,x,y)&&!Flap(pose.Name,cx,Y(sy)))continue;
    uint p0=(uint)original[k0],p1=(uint)original[k1],basePixel=(uint)pixels[y*width+x],value=0;
    for(int s=0;s<32;s+=8)value|=(uint)Math.Round((p0>>s&255)*a+(p1>>s&255)*b+(basePixel>>s&255)*(1-a-b))<<s;
    pixels[y*width+x]=(int)value;
   }
   var bits=picture.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
   try{for(int y=0;y<height;y++)Marshal.Copy(pixels,y*width,IntPtr.Add(bits.Scan0,y*bits.Stride),width);}finally{picture.UnlockBits(bits);}
  }
 }
}

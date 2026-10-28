using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet {
 // Rebind the approved profiles to the existing gait. No mirrored costume and no
 // old walking texture, iris recoloring, hair recoloring, or generated frame cache.
 sealed class WalkArt : IDisposable {
  readonly Bitmap[,] layers=new Bitmap[2,3];
  readonly int[][] bodies=new int[2][];
  readonly int[,][] legPixels=new int[2,2][];
  readonly CarryArmRig[] carryArms=new CarryArmRig[2];
  readonly Bitmap canvas,warped,runLeg;
  readonly CharacterForm form; readonly int width,height;readonly double size;
  static readonly PointF[][] LegBoundary={
   new[]{new PointF(143,311),new PointF(177,317),new PointF(199,358),new PointF(200,411),new PointF(210,435),new PointF(210,455),new PointF(103,455),new PointF(105,425),new PointF(133,407)},
   // Follow the exposed thigh/boot edge below the diagonal front flap. The old
   // 1603,308 -> 1644,316 edge cut a triangle out of the coat in both forms.
   new[]{new PointF(1603,347),new PointF(1620,334),new PointF(1644,321),new PointF(1650,410),new PointF(1671,438),new PointF(1667,455),new PointF(1577,455),new PointF(1577,428),new PointF(1598,410)}
  };
  public WalkArt(int w,int h,int petHeight,CharacterForm form=CharacterForm.AnemoneI) { this.form=form;
   width=w;height=h;size=petHeight;canvas=new Bitmap(w,h,PixelFormat.Format32bppPArgb);warped=new Bitmap(w,h,PixelFormat.Format32bppPArgb);runLeg=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
   try {
    for(int d=0;d<2;d++)using(Bitmap source=CharacterArt.Load(d==0?"profile-left":"profile-right",form))using(var path=new GraphicsPath()) {
     var pose=(CharacterArt.Pose)source.Tag;path.AddPolygon(LegBoundary[d]);
     RectangleF legBounds=path.GetBounds();legBounds.Inflate(1,1);
     int sw=source.Width,sh=source.Height;int[] all=new int[sw*sh];var data=source.LockBits(new Rectangle(0,0,sw,sh),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
     try{for(int y=0;y<sh;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),all,y*sw,sw);}finally{source.UnlockBits(data);}
     var pixels=new[]{new int[all.Length],new int[all.Length],new int[all.Length]};
     for(int y=0;y<sh;y++)for(int x=0;x<sw;x++) {
      int gx=x+pose.Cell.X,gy=y+pose.Cell.Y,part=0;
      bool lowerBoot=gy>395 && gx>=(d==0?95:1560) && gx<=(d==0?220:1675);
      if(lowerBoot||(legBounds.Contains(gx,gy)&&path.IsVisible(gx,gy)))part=(d==0?gx<176-(gy-350)*.10:gx>=1621)?2:1;
      pixels[part][y*sw+x]=all[y*sw+x];
     }
     for(int part=0;part<1;part++)using(var isolated=new Bitmap(sw,sh,PixelFormat.Format32bppArgb)) {
      var bits=isolated.LockBits(new Rectangle(0,0,sw,sh),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
      try{for(int y=0;y<sh;y++)Marshal.Copy(pixels[part],y*sw,IntPtr.Add(bits.Scan0,y*bits.Stride),sw);}finally{isolated.UnlockBits(bits);}
      isolated.Tag=pose;layers[d,part]=CharacterArt.Normalize(isolated,w,h,size);
     }
     bodies[d]=PoseMesh.Read(layers[d,0]);
    }
    // Occluded leg surfaces cannot be recovered by slicing an overlapping pose.
    // Use complete direction-specific painted layers; the gait remains unchanged.
    using(Bitmap legs=AssetStore.Image("v064/walk-legs.png",form)) {
     Rectangle[] cuts={new Rectangle(233,31,237,584),new Rectangle(752,30,241,584),new Rectangle(229,688,248,580),new Rectangle(744,691,244,578)};
     int[,] index={{2,0},{3,1}};
     for(int d=0;d<2;d++)for(int p=1;p<=2;p++)using(Bitmap leg=legs.Clone(cuts[index[d,p-1]],PixelFormat.Format32bppArgb)) {
      layers[d,p]=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
      double lh=size*SideWalk.LegHeightRatio,lw=leg.Width*lh/leg.Height,pivot=d==0?.70:.30;
      double center=w*.5+(p==1?-1:1)*size*.012;
      using(Graphics g=Graphics.FromImage(layers[d,p])){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(leg,new RectangleF((float)(center-lw*pivot),(float)(28+size*.98-lh),(float)lw,(float)lh));}
      legPixels[d,p-1]=PoseMesh.Read(layers[d,p]);
     }
    }
    // Prepare both sleeves during resize/loading, not on the first running frame.
    // The direction-specific backplate extraction is too expensive for a frame tick.
    for(int d=0;d<2;d++)ArmRig(d);
   }catch{Dispose();throw;}
  }
  public Bitmap Draw(double time,double phase,double strength,int facing,double settle,double carry,double effort,double vertical=0,double run=0,bool runningCarry=false) {
   int d=facing<0?0:1;double amount=1-Math.Max(0,Math.Min(1,settle));
   carry=Math.Max(0,Math.Min(1,carry));effort=Math.Max(0,Math.Min(1,effort));
   bool holding=carry>0 || effort>0;
   run=holding && !runningCarry?0:Math.Max(0,Math.Min(1,run));
   double stride=size*SideWalk.StrideRatio,ground=28+size*.98,hip=ground-size*SideWalk.LegHeightRatio;
   SideWalk.Foot near=SideWalk.LimitForward(SideWalk.Sample(phase,stride,size*.026),size*SideWalk.LegHeightRatio);
   SideWalk.Foot far=SideWalk.LimitForward(SideWalk.Sample(phase+.5,stride,size*.026),size*SideWalk.LegHeightRatio);
   double grip=runningCarry?LeapMotion.Ease(carry):0;
   double rise=RunGait.Rise(phase,size,run)*amount*(1-grip);
   if(run>0){near=RunGait.Sample(phase,size,run);far=RunGait.Sample(phase+.5,size,run);}
   using(Graphics g=Graphics.FromImage(canvas)) {
    g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    if(run>0 && amount>0) {
     DrawRunningLeg(g,legPixels[d,0],far,facing,amount,hip,ground,rise,run,RunGait.KneeFlex(phase+.5,size,run)*amount);
     DrawRunningLeg(g,legPixels[d,1],near,facing,amount,hip,ground,rise,run,RunGait.KneeFlex(phase,size,run)*amount);
    }else {
     DrawLeg(g,layers[d,1],far,facing,amount,hip,ground);
     DrawLeg(g,layers[d,2],near,facing,amount,hip,ground);
    }
    Bitmap body;
    if(runningCarry)body=ArmRig(d).DrawPickup(phase,run*strength*amount,carry,effort);
    else if(!holding && run*strength*amount>0)body=ArmRig(d).DrawRunning(phase,run*strength*amount);
    else body=Arms(d,phase,strength*amount,facing,carry,effort,vertical);
    GraphicsState state=g.Save();
    // Ordinary walking adds no torso tilt; carrying keeps its existing effort pose.
    double pivotY=28+size*.48,angle=facing*(carry*3+effort*5)*(1-Math.Abs(vertical)*.75);
    if(runningCarry)angle*=grip;
    pivotY+=(hip-pivotY)*run*amount*(1-grip);angle+=facing*AnimationParameters.Get("runLean")*run*strength*amount*(1-grip);
    g.TranslateTransform(width*.5f,(float)(pivotY-rise+(runningCarry?0:size*.006*effort*strength*amount*(1-Math.Cos(phase*4*Math.PI)))));
    g.RotateTransform((float)angle);g.TranslateTransform(-width*.5f,(float)-pivotY);g.DrawImageUnscaled(body,0,0);g.Restore(state);
   }return canvas;
  }
  CarryArmRig ArmRig(int d) {
   if(carryArms[d]==null)carryArms[d]=new CarryArmRig(d,layers[d,0],size,form);
   return carryArms[d];
  }
  public Bitmap DrawLeap(int facing,LeapPose pose,double carry=0,double effort=0,double vertical=0) {
   if(pose.Crouch==0 && pose.Air==0 && pose.Reach==0 && pose.Hair==0 && pose.Petals==0)return Draw(0,0,0,facing,1,carry,effort,vertical);
   int d=facing<0?0:1;
   double ground=28+size*.98,hip=ground-size*SideWalk.LegHeightRatio;
   double rise=-size*.035*pose.Crouch;
   var near=new SideWalk.Foot{X=size*(.035*pose.Reach-.018*pose.Air),Lift=size*(.065*pose.Air+.018*pose.Reach)};
   var far=new SideWalk.Foot{X=-size*(.022*pose.Reach+.035*pose.Air),Lift=size*.085*pose.Air};
   using(Graphics g=Graphics.FromImage(canvas)) {
    g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    if(form==CharacterForm.AnemoneII)LeapFeathers.Draw(g,width,size,ground,pose);
    else DrawPetals(g,ground,pose);
    DrawRunningLeg(g,legPixels[d,0],far,facing,1,hip,ground,rise,1,size*.04*pose.Crouch);
    DrawRunningLeg(g,legPixels[d,1],near,facing,1,hip,ground,rise,1,size*.04*pose.Crouch);
    bool holding=carry>0 || effort>0;
    Bitmap body=holding?ArmRig(d).Draw(carry,effort,vertical):ArmRig(d).DrawLeap(pose.Crouch,pose.Air,pose.Reach);
    if(Math.Abs(pose.Hair)>.00001)body=LeapHair(body,facing,pose.Hair);
    GraphicsState state=g.Save();
    double pivot=holding?28+size*.48:hip;
    g.TranslateTransform(width*.5f,(float)(pivot-rise));
    g.RotateTransform((float)(holding?facing*(carry*3+effort*5)*(1-Math.Abs(vertical)*.75):facing*(3*pose.Crouch-2*pose.Air)));
    g.TranslateTransform(-width*.5f,(float)-pivot);g.DrawImageUnscaled(body,0,0);g.Restore(state);
   }
   return canvas;
  }
  void DrawPetals(Graphics g,double ground,LeapPose pose) {
   if(pose.Petals<.001)return;
   g.SmoothingMode=SmoothingMode.AntiAlias;
   double spread=size*(.07+.12*pose.Flare),alpha=pose.Petals;
   using(var pen=new Pen(Color.FromArgb((int)(65*alpha),178,89,102),1))
    g.DrawEllipse(pen,(float)(width*.5-spread),(float)(ground-2),(float)(2*spread),(float)(size*.025));
   for(int i=0;i<7;i++) {
    double theta=i*2*Math.PI/7+pose.Time*1.6;
    double x=width*.5+Math.Cos(theta)*spread,y=ground-4-Math.Sin(theta)*size*.018-pose.Flare*size*.025;
    var saved=g.Save();g.TranslateTransform((float)x,(float)y);g.RotateTransform((float)(theta*180/Math.PI+pose.Flare*65));
    float r=(float)(size*(i%2==0?.013:.009));
    using(var path=new GraphicsPath())using(var brush=new SolidBrush(Color.FromArgb((int)(170*alpha),i%2==0?182:220,i%2==0?84:153,i%2==0?103:158))) {
     path.AddBezier(0,-r,r*1.1f,-r*.2f,r*.65f,r*.7f,0,r);
     path.AddBezier(0,r,-r*.7f,r*.25f,-r*.8f,-r*.45f,0,-r);path.CloseFigure();g.FillPath(brush,path);
    }
    g.Restore(saved);
   }
  }
  unsafe Bitmap LeapHair(Bitmap body,int facing,double lag) {
   int[] pixels=PoseMesh.Read(body);
   var bits=warped.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
   try {for(int y=0;y<height;y++) {
    uint* row=(uint*)((byte*)bits.Scan0+y*bits.Stride);
    for(int x=0;x<width;x++) {
     double rear=-facing*(x-width*.5);
     double mask=Smooth((rear-size*.025)/(size*.10))*Smooth((y-28-size*.34)/(size*.24));
     row[x]=mask==0?(uint)pixels[y*width+x]:Sample(pixels,x+facing*size*.006*lag*mask,y-size*.012*lag*mask);
    }
   }}finally{warped.UnlockBits(bits);}
   return warped;
  }
  // Continuous knee bend, fixed hip attachment and a ground-locked planted foot.
  // Inverse sampling avoids seams between separately rotated upper/lower strips.
  unsafe void DrawRunningLeg(Graphics g,int[] pixels,SideWalk.Foot foot,int facing,double amount,double hip,double ground,double rise,double run,double knee=0) {
   double span=ground-hip,dy=-foot.Lift*amount+rise,scale=1+dy/span;
   var bits=runLeg.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
   try {
    for(int y=0;y<height;y++) {
     uint* row=(uint*)((byte*)bits.Scan0+y*bits.Stride);
     double sy=(y+rise+hip*dy/span)/scale,t=(sy-hip)/span;
     double bend=Math.Sin(Math.PI*Math.Max(0,Math.Min(1,t)))*(foot.Lift*.55*run*amount+knee);
     double dx=facing*(foot.X*amount*t+bend);
     for(int x=0;x<width;x++)row[x]=sy<hip-2 || sy>ground+3?0:Sample(pixels,x-dx,sy);
    }
   }finally{runLeg.UnlockBits(bits);}
   g.DrawImageUnscaled(runLeg,0,0);
  }
  void DrawLeg(Graphics g,Bitmap layer,SideWalk.Foot foot,int facing,double amount,double hip,double ground) {
   double dx=facing*foot.X*amount,dy=-foot.Lift*amount,span=ground-hip;
   PointF[] dest={new PointF((float)(-hip/span*dx),(float)(-hip/span*dy)),new PointF((float)(width-hip/span*dx),(float)(-hip/span*dy)),new PointF((float)((height-hip)/span*dx),(float)(height+(height-hip)/span*dy))};
   g.DrawImage(layer,dest,new Rectangle(0,0,width,height),GraphicsUnit.Pixel);
  }
  static double Smooth(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
  unsafe Bitmap Arms(int d,double phase,double walking,int facing,double carry,double effort,double vertical) {
   if(carry>0 || effort>0) {
    if(carryArms[d]==null)carryArms[d]=new CarryArmRig(d,layers[d,0],size,form);
    return carryArms[d].Draw(carry,effort,vertical);
   }
   if(walking<.001 && carry<.001 && effort<.001)return layers[d,0];
   var pose=CharacterArt.Definition(d==0?"profile-left":"profile-right");double scale=CharacterArt.Scale(pose,size);
   double shoulderX=width*.5+((d==0?155:1626)-pose.Anchor)*scale;
   double wristX=width*.5+((d==0?177:1597)-pose.Anchor)*scale;
   double top=CharacterArt.Y(pose,190,size),wristY=CharacterArt.Y(pose,296,size),bottom=wristY+size*.05;
   // This legacy field is now used only for the unchanged small walking swing.
   int[] original=bodies[d];var bits=warped.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
   try {
    for(int y=0;y<height;y++) {
     uint* row=(uint*)((byte*)bits.Scan0+y*bits.Stride);
     for(int x=0;x<width;x++) {
      double v=(y-top)/(wristY-top),center=shoulderX+(wristX-shoulderX)*Math.Min(1,Math.Max(0,v));
      double radius=size*.06,u=(x-center)/(radius),mask=Math.Abs(u)<1?(1-u*u)*(1-u*u):0;
      double along=Smooth(v)*(1-Smooth((y-wristY)/(bottom-wristY)));
      double swing=-facing*Math.Cos(phase*2*Math.PI)*walking*size*.013;
      double sx=x-swing*along*mask,sy=y;
      row[x]=Sample(original,sx,sy);
     }
    }
   }finally{warped.UnlockBits(bits);}return warped;
  }
  uint Sample(int[] p,double x,double y) {
   int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);if(ix<0||iy<0||ix>=width-1||iy>=height-1)return 0;
   double fx=x-ix,fy=y-iy;uint v=0;
   for(int s=0;s<32;s+=8) {double a=((uint)p[iy*width+ix]>>s)&255,b=((uint)p[iy*width+ix+1]>>s)&255,c=((uint)p[(iy+1)*width+ix]>>s)&255,e=((uint)p[(iy+1)*width+ix+1]>>s)&255;v|=(uint)Math.Round((a+(b-a)*fx)*(1-fy)+(c+(e-c)*fx)*fy)<<s;}return v;
  }
  public void Dispose(){canvas.Dispose();warped.Dispose();runLeg.Dispose();foreach(Bitmap b in layers)if(b!=null)b.Dispose();foreach(var rig in carryArms)if(rig!=null)rig.Dispose();}
 }
}

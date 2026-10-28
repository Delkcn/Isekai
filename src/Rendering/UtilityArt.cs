using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;
namespace JouchoPet {
 static class UtilityArt {
  internal static float BeamWidth(double size){return (float)(3*Math.Max(1,size*.004));}
  internal static void Spark(Graphics g,PointF p,double radius,double alpha){
   using(var glow=new SolidBrush(Color.FromArgb((int)(alpha*45),145,211,255)))using(var core=new SolidBrush(Color.FromArgb((int)(alpha*230),190,232,255))){float r=(float)radius;g.FillEllipse(glow,p.X-r*3,p.Y-r*3,r*6,r*6);g.FillEllipse(core,p.X-r,p.Y-r,r*2,r*2);}
  }
  internal static void Beam(Graphics g,PointF from,PointF to,double alpha,double size,double age=.60){if(alpha<=0)return;g.SmoothingMode=SmoothingMode.AntiAlias;
   using(var halo=new Pen(Color.FromArgb((int)(alpha*55),135,206,255),(float)(size*.051)))using(var line=new Pen(Color.FromArgb((int)(alpha*240),164,218,255),BeamWidth(size))){halo.StartCap=halo.EndCap=line.StartCap=line.EndCap=LineCap.Round;g.DrawLine(halo,from,to);g.DrawLine(line,from,to);}
   double length=RigParts.Length(from,to),nx=length>0?-(to.Y-from.Y)/length:0,ny=length>0?(to.X-from.X)/length:1;
   for(int i=0;i<26;i++){double f=(i+.5)/26.0,phase=age*5+i*2.399,offset=Math.Sin(phase)*size*.022;PointF p=RigParts.Add(RigParts.Mix(from,to,f),nx*offset,ny*offset);Spark(g,p,size*.003*(.7+.3*Math.Sin(i)),alpha*(.55+.35*Math.Sin(phase)*Math.Sin(phase)));}
  }
  internal static void Burst(Graphics g,PointF center,double age,double size){if(age<0||age>=.45)return;double t=age/.45,a=(1-t)*(1-t);g.SmoothingMode=SmoothingMode.AntiAlias;
   for(int i=0;i<22;i++){double angle=i*2.399963,speed=.6+.4*((i*.618)%1),radius=size*(.012+.15*(1-Math.Pow(1-t,2)))*speed;PointF p=RigParts.Add(center,Math.Cos(angle)*radius,Math.Sin(angle)*radius);Spark(g,p,size*.004*(1-.4*t),a);}
  }
  static double SparkNoise(int seed){unchecked{uint n=(uint)seed+0x9e3779b9u;n=(n^(n>>16))*0x85ebca6bu;n=(n^(n>>13))*0xc2b2ae35u;return (n^(n>>16))/(double)uint.MaxValue;}}
  internal static void BeamImpact(Graphics g,PointF center,double age,double size){if(age<0||age>=.95)return;g.SmoothingMode=SmoothingMode.AntiAlias;
   // Stable per-particle randomness avoids frame-to-frame flicker. Small irregular
   // bursts have independent speeds, lifetimes and streak lengths, not a radial ring.
   for(int batch=0;batch<24;batch++)for(int i=0;i<3;i++){int seed=batch*31+i*7;double t=age-(batch+SparkNoise(seed)*.85)/24*.60,life=.12+.20*SparkNoise(seed+101);if(t<0||t>=life)continue;
    double angle=SparkNoise(batch+911)*Math.PI*2+(SparkNoise(seed+202)-.5)*1.3,speed=size*(.45+1.45*SparkNoise(seed+303)),vx=Math.Cos(angle)*speed,vy=Math.Sin(angle)*speed;
    double tail=Math.Max(0,t-(.008+.035*SparkNoise(seed+404))),alpha=(.75+.25*SparkNoise(seed+505))*(1-DailyMotion.Ease((t/life-.35)/.65));
    PointF end=RigParts.Add(center,vx*t,vy*t+size*1.3*t*t),start=RigParts.Add(center,vx*tail,vy*tail+size*1.3*tail*tail);
    double width=size*(.0025+.003*SparkNoise(seed+606));using(var glow=new Pen(Color.FromArgb((int)(alpha*150),145,218,255),(float)(width*3)))using(var core=new Pen(Color.FromArgb((int)(alpha*255),245,252,255),(float)Math.Max(.8,width))){glow.StartCap=glow.EndCap=core.StartCap=core.EndCap=LineCap.Round;g.DrawLine(glow,start,end);g.DrawLine(core,start,end);}}
   if(age<.60){double pulse=.75+.25*Math.Sin(age*125)*Math.Sin(age*79);Spark(g,center,size*.014,pulse);}
  }
  internal static PointF[] TetherEnds(PointF center,double iconHeight){return new[]{RigParts.Add(center,0,-iconHeight*.42),center,RigParts.Add(center,0,iconHeight*.42)};}
  internal static void Tethers(Graphics g,PointF from,PointF center,double iconHeight,double alpha,double size,double age){foreach(var end in TetherEnds(center,iconHeight))Beam(g,from,end,alpha*.70,size,age);Burst(g,center,age-.45,size*.65);}
  internal static void Teleport(Graphics g,Bitmap body,double t,double size){double opacity=UtilityMotion.Opacity(t);using(var attr=new ImageAttributes()){var m=new ColorMatrix();m.Matrix33=(float)opacity;attr.SetColorMatrix(m);g.DrawImage(body,new Rectangle(0,0,body.Width,body.Height),0,0,body.Width,body.Height,GraphicsUnit.Pixel,attr);}
   // Fourteen sparse sparks disperse from the silhouette, then gather in reverse.
   double phase=t<.65?Math.Min(1,t/.55):Math.Max(0,Math.Min(1,1-(t-.70)/.60)),alpha=Math.Max(0,Math.Sin(Math.PI*phase));g.SmoothingMode=SmoothingMode.AntiAlias;
   for(int i=0;i<14;i++){double seed=(i*.61803398875)%1,y=.1+.80*((i*.381966)%1),x=Math.Sin(i*2.4)*.11;double spread=DailyMotion.Ease(phase);float px=(float)(body.Width*.5+size*(x+(seed-.5)*.19*spread)),py=(float)(28+size*(y-.07*spread));float r=(float)(size*.004*(.7+seed));
    using(var glow=new SolidBrush(Color.FromArgb((int)(alpha*45),188,224,255)))using(var core=new SolidBrush(Color.FromArgb((int)(alpha*240),245,247,226)))using(var ray=new Pen(Color.FromArgb((int)(alpha*160),219,240,255),1)){g.FillEllipse(glow,px-r*3,py-r*3,r*6,r*6);g.FillEllipse(core,px-r,py-r,r*2,r*2);if(i%3==0){g.DrawLine(ray,px-r*2,py,px+r*2,py);g.DrawLine(ray,px,py-r*2,px,py+r*2);}}}
  }
 }
}


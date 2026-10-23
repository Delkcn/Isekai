using System;
using System.Drawing;
namespace JouchoPet {
 // Hierarchical yaw bones and normalized skin weights. Sparse view correctives
 // preserve painted contours; this is a 2.5D model, not a watertight 3D model.
 sealed class CharacterSkeleton {
  internal struct V3 {public double X,Y,Z;public V3(double x,double y,double z){X=x;Y=y;Z=z;}}
  internal sealed class Bone {
   public string Name;public int Parent;public V3 Pivot;public double Response;
   public Bone(string n,int p,double x,double y,double z,double r){Name=n;Parent=p;Pivot=new V3(x,y,z);Response=r;}
  }
  internal sealed class Vertex {public V3 Bind;public double[] Weights;}
  internal readonly Bone[] Bones={
   new Bone("root",-1,0,.64,0,0),new Bone("chest",0,0,.43,0,.012),new Bone("head",1,0,.34,0,.045),
   new Bone("hair-left",2,-.15,.20,-.10,-.045),new Bone("hair-right",2,.15,.20,-.10,-.045),
   new Bone("arm-left",1,-.11,.39,0,-.015),new Bone("arm-right",1,.11,.39,0,-.015),
   new Bone("leg-left",0,-.055,.64,0,0),new Bone("leg-right",0,.055,.64,0,0)};
  static double Smooth(double a,double b,double x){x=Math.Max(0,Math.Min(1,(x-a)/(b-a)));return x*x*(3-2*x);}
  public Vertex Bind(PointF point,int width,double size) {
   double x=(point.X-width*.5)/size,y=(point.Y-28)/size,side=Math.Abs(x);var weights=new double[Bones.Length];
   double head=1-Smooth(.28,.39,y),hair=Smooth(.12,.24,side)*Smooth(.18,.35,y)*(1-Smooth(.76,.87,y));
   double arms=Smooth(.075,.13,side)*(1-Smooth(.18,.25,side))*Smooth(.36,.44,y)*(1-Smooth(.61,.69,y))*(1-hair);
   weights[2]=head*(1-hair);weights[x<0?3:4]=hair;weights[x<0?5:6]=arms*(1-head);weights[x<0?7:8]=Smooth(.65,.78,y)*(1-hair);
   double total=0;for(int i=2;i<weights.Length;i++)total+=weights[i];
   if(total>1)for(int i=2;i<weights.Length;i++)weights[i]/=total;else weights[1]=1-total;
   return new Vertex{Bind=new V3(x,y,head*.09*Math.Sqrt(Math.Max(0,1-x*x/.05))-hair*.13),Weights=weights};
  }
  V3 Transform(V3 p,int bone,double yaw) {
   Bone b=Bones[bone];double angle=bone==0?yaw:b.Response*Math.Sin(2*yaw),x=p.X-b.Pivot.X,z=p.Z-b.Pivot.Z,c=Math.Cos(angle),s=Math.Sin(angle);
   p=new V3(b.Pivot.X+c*x+s*z,p.Y,b.Pivot.Z-s*x+c*z);return b.Parent<0?p:Transform(p,b.Parent,yaw);
  }
  public PointF Skin(Vertex vertex,double yaw,int width,double size) {
   double x=0,y=0;for(int b=0;b<Bones.Length;b++)if(vertex.Weights[b]>0){V3 p=Transform(vertex.Bind,b,yaw);x+=p.X*vertex.Weights[b];y+=p.Y*vertex.Weights[b];}
   return new PointF((float)(width*.5+x*size),(float)(28+y*size));
  }
  public void DrawDebug(Graphics g,double yaw,int width,double size) {
   using(var pen=new Pen(Color.FromArgb(210,25,210,225),1.3f))foreach(Bone bone in Bones) {
    V3 p=bone.Parent<0?bone.Pivot:Transform(bone.Pivot,bone.Parent,yaw);float x=(float)(width*.5+p.X*size),y=(float)(28+p.Y*size);g.FillEllipse(Brushes.OrangeRed,x-2,y-2,4,4);
    if(bone.Parent>=0){Bone parent=Bones[bone.Parent];V3 q=parent.Parent<0?parent.Pivot:Transform(parent.Pivot,parent.Parent,yaw);g.DrawLine(pen,x,y,(float)(width*.5+q.X*size),(float)(28+q.Y*size));}
   }
  }
 }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace JouchoPet {
 // A2 appearance only: consume the existing leap envelope and clock. Keeping
 // this within each form renderer also makes mid-leap costume fades continuous.
 static class LeapFeathers {
  internal static void Draw(Graphics g,int width,double size,double ground,LeapPose pose){
   if(pose.Petals<.001)return;
   double alpha=Math.Min(1,pose.Petals),spread=size*(.075+.12*pose.Flare);
   var state=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;
   try{
    for(int i=0;i<7;i++){
     double theta=i*2*Math.PI/7+pose.Time*1.6;
     double x=width*.5+Math.Cos(theta)*spread;
     double y=ground-size*(.035+.018*Math.Sin(theta)+pose.Flare*.035);
     var saved=g.Save();
     try{
      g.TranslateTransform((float)x,(float)y);
      g.RotateTransform((float)(Math.Cos(theta)*48+Math.Sin(pose.Time*3+i)*18));
      // Narrow, asymmetric vane, pointed tip, split barbs, and a protruding
      // curved quill distinguish the feather from the old rounded petals.
      float length=(float)(size*(i%2==0?.064:.050));
      g.ScaleTransform(length,length);
      using(var vane=new GraphicsPath()){
       vane.AddBezier(0,-.56f,-.11f,-.45f,-.17f,-.34f,-.19f,-.22f);
       vane.AddLines(new[]{new PointF(-.19f,-.22f),new PointF(-.08f,-.25f),new PointF(-.21f,-.13f),new PointF(-.22f,-.01f),new PointF(-.08f,-.08f),new PointF(-.20f,.08f),new PointF(-.16f,.19f),new PointF(-.06f,.12f),new PointF(-.10f,.27f),new PointF(-.03f,.32f),new PointF(.08f,.24f),new PointF(.02f,.12f),new PointF(.14f,.14f),new PointF(.17f,.02f),new PointF(.04f,-.07f),new PointF(.18f,-.06f),new PointF(.17f,-.20f),new PointF(.06f,-.25f),new PointF(.14f,-.28f)});
       vane.AddBezier(.14f,-.28f,.11f,-.41f,.06f,-.49f,0,-.56f);vane.CloseFigure();
       foreach(float halo in new[]{.30f,.18f,.08f})using(var glow=new Pen(Color.FromArgb((int)(alpha*(halo>.2?12:halo>.1?23:40)),154,220,255),halo)){glow.LineJoin=LineJoin.Round;g.DrawPath(glow,vane);}
       using(var fill=new LinearGradientBrush(new PointF(-.24f,0),new PointF(.22f,0),Color.FromArgb((int)(225*alpha),189,228,250),Color.FromArgb((int)(250*alpha),255,255,248)))g.FillPath(fill,vane);
       using(var barbs=new Pen(Color.FromArgb((int)(195*alpha),102,166,199),.022f)){
        for(int j=0;j<7;j++){float yb=-.31f+j*.085f,reach=.17f*(float)Math.Sin((j+1)*Math.PI/9);g.DrawLine(barbs,-.025f,yb,-reach,yb-.09f);g.DrawLine(barbs,-.025f,yb,reach*.78f,yb-.105f);}
       }
       using(var quill=new Pen(Color.FromArgb((int)(255*alpha),253,255,255),.032f)){quill.StartCap=quill.EndCap=LineCap.Round;g.DrawBezier(quill,0,-.52f,-.065f,-.12f,-.04f,.23f,.045f,.58f);}
      }
     }finally{g.Restore(saved);}
    }
   }finally{g.Restore(state);}
  }
 }
}

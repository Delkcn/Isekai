using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;using JouchoPet;
class PivotCheck {
 static int count;
 static void Check(bool yes,string label){if(!yes)throw new Exception(label);count++;Console.WriteLine("PASS "+label);}
 static void Quiet(bool yes,string label){if(!yes)throw new Exception(label);}
 static double Diff(Bitmap a,Bitmap b){int[] x=PoseMesh.Read(a),y=PoseMesh.Read(b);double sum=0;for(int i=0;i<x.Length;i++)for(int s=0;s<32;s+=8)sum+=Math.Abs(((uint)x[i]>>s&255)-(double)((uint)y[i]>>s&255));return sum/(x.Length*4);}
 static void Main(){try {
  foreach(bool light in new[]{false,true})foreach(int from in new[]{-1,0,1})foreach(int to in new[]{-1,0,1})if(from!=to) {
   int choices=0;var m=new Motion{State=from==0?"idle":"hold",Facing=from,Angle=from,SelectLightTurn=delegate{choices++;return light;}};
   double previous=0;bool started=false,finished=false;
   for(int i=0;i<180;i++) {
    string before=m.State;double travel=m.Update(1.0/120,to,300);
    if(m.State=="turn"){started=true;Quiet(travel==0 && m.LightTurn==light,"turn moved or changed style mid-animation");Quiet(m.TurnProgress>=previous,"progress reversed");previous=m.TurnProgress;}
    if(before=="turn" && m.State!="turn"){finished=true;break;}
   }
   Check(started && finished && choices==1 && m.Facing==to && m.Settle==1,"style chosen once; exact handoff "+light+" / "+from+" / "+to);
  }
  var random=new Random(626);int singles=0,classics=0;var mixed=new Motion{SelectLightTurn=delegate{return random.Next(2)==0;}};
  for(int i=0;i<40;i++){mixed.Reset();mixed.Update(.01,1,300);if(mixed.LightTurn)singles++;else classics++;}
  Check(singles>0 && classics>0,"random selector reaches both styles");
  for(int i=1;i<=100;i++)Quiet(PivotTurn.Progress(i/100.0)>PivotTurn.Progress((i-1)/100.0),"light turn must rotate continuously without staging holds");
  Check(PivotTurn.Progress(0)==0 && PivotTurn.Progress(1)==1,"continuous light turn retains exact endpoints");
  Check(PivotTurn.Lift(.2)>.99 && PivotTurn.Lift(.85)>.99,"free foot remains lifted through the rotation");
  foreach(int size in new[]{220,300,400})Render(size);
  Console.WriteLine("All "+count+" pivot turn checks passed.");
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
 static void Render(int size) {
  Size c=PetRenderer.CanvasSize(size);var sprites=SpriteAssets.LoadFront();string folder=Path.Combine("qa","v0629","preview");Directory.CreateDirectory(folder);
  try {using(var front=new SoftRig(sprites,c.Width,c.Height,size))using(var side=new SideWalk(c.Width,c.Height,size))using(var turn=new TurnRig(c.Width,c.Height,size)) {
   Bitmap neutral=front.Draw(0,0,0,0,0,0,0,new[]{1.0,0,0});turn.Prepare(neutral,side);
   foreach(int from in new[]{-1,0,1})foreach(int to in new[]{-1,0,1})if(from!=to) {
    using(Bitmap a=(Bitmap)turn.Draw(from,neutral,side,0).Clone())Check(Diff(a,turn.DrawLight(from,neutral,side,0,0,from,to<from))==0,"start matches classic "+size+" / "+from+" / "+to);
    using(Bitmap a=(Bitmap)turn.Draw(to,neutral,side,0).Clone())Check(Diff(a,turn.DrawLight(to,neutral,side,0,1,from,to<from))==0,"end matches classic "+size+" / "+from+" / "+to);
    double minimum=double.MaxValue;
    for(int i=1;i<60;i++) {
     double progress=i/60.0,angle=from+(to-from)*PivotTurn.Progress(progress);
     Bitmap b=turn.DrawLight(angle,neutral,side,0,progress,from,to<from);minimum=Math.Min(minimum,turn.LastMinimumJacobian);
     int[] pixels=PoseMesh.Read(b);for(int y=0;y<c.Height;y++)for(int x=0;x<c.Width;x++)if(((uint)pixels[y*c.Width+x]>>24)>80)Quiet(x>0 && y>0 && x<c.Width-1 && y<c.Height-1,"turn clipped artwork");
    }
    Check(minimum>0,"59 single-leg poses have positive mesh area "+size+" / "+from+" / "+to);
   }
   foreach(bool near in new[]{false,true}) {
    PointF support=new PointF(near?160:184,(float)(28+size*.98)),anchor=new PointF(support.X+7,support.Y);
    PointF p=PivotTurn.Pose(support,28+size*.64,28+size*.98,160,184,support,anchor,.5,near,size);
    Check(Math.Abs(p.X-anchor.X)<.001 && Math.Abs(p.Y-anchor.Y)<.001,"support foot stays pinned "+size+" / "+near);
   }
   if(size==300) {
    using(var atlas=new Bitmap(c.Width*12,c.Height*12,PixelFormat.Format32bppPArgb))using(var g=Graphics.FromImage(atlas))
    using(var contact=new Bitmap(c.Width*8,c.Height*3))using(var cg=Graphics.FromImage(contact)) {
     cg.Clear(Color.FromArgb(238,235,230));
     for(int row=0;row<3;row++)for(int i=0;i<48;i++) {
      double p=i/47.0;int from=row==2?1:-1,to=row==2?-1:1;
      double angle=from+(to-from)*(row==0?LeapMotion.Ease(p):PivotTurn.Progress(p));
      Bitmap frame=row==0?turn.Draw(angle,neutral,side,0):turn.DrawLight(angle,neutral,side,0,p,from,to<from);
      int index=row*48+i;g.DrawImageUnscaled(frame,index%12*c.Width,index/12*c.Height);
      if(i%6==0)cg.DrawImageUnscaled(frame,i/6*c.Width,row*c.Height);
     }
     atlas.Save(Path.Combine(folder,"turn-atlas.png"),ImageFormat.Png);contact.Save(Path.Combine(folder,"turn-contact.png"),ImageFormat.Png);
    }
   }
  }}finally{foreach(Bitmap b in sprites)b.Dispose();}
 }
}

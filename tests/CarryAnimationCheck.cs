using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using JouchoPet;
class CarryAnimationCheck {
 static int checks;
 static void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static int Changed(Bitmap a,Bitmap b,int firstY) {
  int[] x=PoseMesh.Read(a),y=PoseMesh.Read(b);int count=0;
  for(int i=firstY*a.Width;i<x.Length;i++)if(x[i]!=y[i])count++;return count;
 }
 static int BootChanges(Bitmap a,Bitmap b) {
  int[] x=PoseMesh.Read(a),y=PoseMesh.Read(b);int changed=0;
  for(int row=310;row<324;row++)for(int col=(int)(a.Width*.5-45);col<(int)(a.Width*.5+45);col++) {
   int k=row*a.Width+col;uint p=(uint)x[k];double red=(p>>16)&255,green=(p>>8)&255;
   if((p>>24)>200 && red>green*1.7 && red>80 && x[k]!=y[k])changed++;
  }
  return changed;
 }
 static int Breadth(Bitmap b) {
  int[] p=PoseMesh.Read(b);int sum=0;
  for(int y=148;y<248;y++){int l=b.Width,r=-1;for(int x=0;x<b.Width;x++)if(((uint)p[y*b.Width+x]>>24)>128){l=Math.Min(l,x);r=x;}if(r>=l)sum+=r-l+1;}return sum;
 }
 static double Distance(PointF a,PointF b){return Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));}
 static void ArmIsolationChecks() {
  foreach(int size in new[]{220,300,400})for(int d=0;d<2;d++) {
   Size c=PetRenderer.CanvasSize(size);
   using(var body=CharacterArt.Normalized(d==0?"profile-left":"profile-right",c.Width,c.Height,size))using(var arm=new CarryArmRig(d,body,size)) {
    int[] original=PoseMesh.Read(body),plate=arm.DiagnosticBackplate,cutout=arm.DiagnosticArm;
    int escaped=0;for(int i=0;i<original.Length;i++){if(cutout[i]==0 && plate[i]!=original[i])escaped++;if(((uint)plate[i]>>24)!=((uint)original[i]>>24))throw new Exception("Backplate lost original coverage");}
    Check(escaped==0,"backplate leaves coat outside the removed arm untouched "+size+" / "+d);
    Check(Changed(body,arm.Draw(0,0),0)==0,"zero carry reconstructs the exact original body "+size+" / "+d);
    double upper=Distance(arm.Shoulder,arm.Elbow),lower=Distance(arm.Elbow,arm.Wrist),minimum=double.MaxValue;
    foreach(double vertical in new[]{-1.0,0,1})foreach(double reach in new[]{0.0,.25,.5,.75,1})foreach(double effort in new[]{0.0,.25,.5,.75,1}) {
     Bitmap frame=arm.Draw(reach,effort,vertical);minimum=Math.Min(minimum,arm.MinimumArea);
     PointF shoulder=arm.Skin(arm.Shoulder,reach,effort,vertical),elbow=arm.Skin(arm.Elbow,reach,effort,vertical),wrist=arm.Skin(arm.Wrist,reach,effort,vertical);
     if(Distance(shoulder,arm.Shoulder)>.001 || Math.Abs(Distance(shoulder,elbow)-upper)>.001 || Math.Abs(Distance(elbow,wrist)-lower)>.001)throw new Exception("Carry changed shoulder position or bone length");
     int[] drawn=PoseMesh.Read(frame),foreground=arm.DiagnosticPosedArm;
     for(int i=0;i<drawn.Length;i++)if(((uint)foreground[i]>>24)==0 && ((uint)plate[i]>>24)>=12 && drawn[i]!=plate[i])throw new Exception("Uncovered torso was deformed by arm motion");
    }
    Check(minimum>0,"arm stays unfolded; shoulder and bone lengths remain fixed through 75 horizontal/vertical poses "+size+" / "+d);
    Check(arm.Skin(arm.Wrist,1,1,-1).Y<arm.Skin(arm.Wrist,1,1,0).Y && arm.Skin(arm.Wrist,1,1,1).Y>arm.Skin(arm.Wrist,1,1,0).Y,"up/down pushes have distinct hand heights "+size+" / "+d);
    double forward=(arm.Skin(arm.Wrist,1,1).X-arm.Wrist.X)*(d==0?-1:1);
    Check(forward>size*.05,"hand reaches forward independently of torso "+size+" / "+d);
   }
  }
 }
 static void Main() {
  try {
   ArmIsolationChecks();
   Size c=PetRenderer.CanvasSize(300);
   // v063 art-width assertions are historical; the unified import is tested by ArtCheck.
   using(var side=new SideWalk(c.Width,c.Height,300)) {
    foreach(int facing in new[]{-1,1}) {
     using(var neutral=(Bitmap)side.Draw(0,0,0,facing,1,0,0).Clone())
     using(var reached=(Bitmap)side.Draw(0,0,0,facing,1,1,1).Clone()) {
      Check(Changed(neutral,reached,0)>1500,"visible carry upper-body pose "+facing);
      Check(BootChanges(neutral,reached)==0,"carry effort leaves planted boot soles unchanged "+facing);
      Check(Changed(neutral,side.Draw(0,0,0,facing,1,0,0),0)==0,"release returns to exact neutral "+facing);
     }
    }
    using(var sheet=new Bitmap(2880,18*174,PixelFormat.Format32bppPArgb))using(Graphics g=Graphics.FromImage(sheet)) {
     g.Clear(Color.Transparent);
     for(int direction=0;direction<2;direction++) {
      int facing=direction==0?-1:1;var motion=new Motion{State="hold",Facing=facing,Angle=facing,Settle=1};double effort=0;
      for(int i=0;i<180;i++) {
       bool pushing=i>=40 && i<140;double carry=i<40?IconCarryPlan.Smooth(i/39.0):i>=140?1-IconCarryPlan.Smooth((i-140)/39.0):1;
       double target=carry*(pushing?1:.65);effort+=(target-effort)*(1-Math.Exp(-8.0/60));
       motion.Update(1.0/60,pushing?facing:0,300,true);
       Bitmap b=side.Draw(i/60.0,motion.Phase,Math.Min(1,Math.Abs(motion.Speed)/36),facing,motion.Settle,carry,effort);
       g.DrawImage(b,new Rectangle(i%10*288+direction*144,i/10*174,144,174));
      }
     }
     using(var stream=new MemoryStream()) {
      sheet.Save(stream,ImageFormat.Png);
      File.WriteAllText("Carry-Preview.html","<!doctype html><meta charset='utf-8'><title>搬运姿态预览</title><style>body{font:18px system-ui;text-align:center;background:#ddd}canvas{width:864px;height:522px}button{font:inherit;margin:12px}</style><p>左右方向：伸手 → 前倾推动 → 放手回正。实际 C# 渲染采样；此预览不移动桌面图标。</p><button onclick='play=!play'>播放/暂停</button><select id='speed'><option value='1'>正常速度</option><option value='.25'>¼ 速度</option></select><input id='seek' type='range' min='0' max='179' value='0'><br><canvas id='v' width='288' height='174'></canvas><script>let im=new Image(),ctx=v.getContext('2d'),play=true,f=0,last=0;im.src='data:image/png;base64,"+Convert.ToBase64String(stream.ToArray())+"';seek.oninput=()=>{play=false;f=+seek.value};function tick(t){if(play&&last)f=(f+(t-last)*.06*Number(speed.value))%180;last=t;let i=Math.floor(f);ctx.clearRect(0,0,288,174);if(im.complete)ctx.drawImage(im,i%10*288,Math.floor(i/10)*174,288,174,0,0,288,174);seek.value=i;requestAnimationFrame(tick)}im.onload=()=>requestAnimationFrame(tick)</script>");
     }
    }
   }
   Console.WriteLine("All "+checks+" carry animation/art checks passed.");
  }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}
 }
}

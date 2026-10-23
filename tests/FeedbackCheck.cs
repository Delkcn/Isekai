using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using JouchoPet;
class FeedbackCheck {
 static double Difference(Bitmap a,Bitmap b) {
  int[] x=PoseMesh.Read(a),y=PoseMesh.Read(b);double sum=0;
  for(int i=0;i<x.Length;i++)for(int c=0;c<32;c+=8)sum+=Math.Abs(((uint)x[i]>>c&255)-(double)((uint)y[i]>>c&255));
  return sum/(x.Length*4);
 }
 static void Require(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
 static void Main() {
  try {
   const int size=300;Size c=PetRenderer.CanvasSize(size);Bitmap[] sprites=SpriteAssets.LoadFront();
   try {using(var front=new SoftRig(sprites,c.Width,c.Height,size)) {
    Bitmap standing=front.Draw(0,0,0,0,0,0,0,new[]{1.0,0,0});
    using(var sides=new SideWalk(c.Width,c.Height,size))using(var turn=new TurnRig(c.Width,c.Height,size))
    using(var palette=new Bitmap(c.Width*5,c.Height))using(Graphics pg=Graphics.FromImage(palette)) {
     pg.Clear(Color.FromArgb(235,235,235));
     for(int i=0;i<5;i++)pg.DrawImageUnscaled(turn.Draw(-1+i*.5,standing,sides,0),i*c.Width,0);
     palette.Save("hair-directions.png",ImageFormat.Png);
    }
    using(var rest=new RestRig(sprites,c.Width,c.Height,size,standing)) {
     int count=rest.DiagnosticKeys.Length;double worst=0,oldWorst=0;
     using(var old=new PoseMesh(rest.DiagnosticKeys,c.Width,c.Height)) {
      for(int i=0;i<count-1;i++) {
       double p=(i+.5)/(count-1);
       using(Bitmap a=(Bitmap)rest.Draw(standing,p-1e-6).Clone())worst=Math.Max(worst,Difference(a,rest.Draw(standing,p+1e-6)));
       using(Bitmap a=(Bitmap)old.Draw(i+.5-1e-6).Clone())oldWorst=Math.Max(oldWorst,Difference(a,old.Draw(i+.5+1e-6)));
      }
     }
     Console.WriteLine("Sleep half-key discontinuity, old="+oldWorst.ToString("F4")+", new="+worst.ToString("F4"));
     // Half a channel level allows 8-bit rounding; old hard swaps exceed 27 levels.
     Require(worst<.5,"all "+(count-1)+" sleep material boundaries are continuous");
     double keys=0;
     for(int i=1;i<count-1;i++)using(Bitmap a=(Bitmap)rest.Draw(standing,(i-1e-6)/(count-1)).Clone())keys=Math.Max(keys,Difference(a,rest.Draw(standing,(i+1e-6)/(count-1))));
     Console.WriteLine("Sleep key boundary max="+keys.ToString("F4"));Require(keys<.5,"sleep key boundaries are continuous");
     using(var sheet=new Bitmap(10*144,36*174,PixelFormat.Format32bppPArgb))using(Graphics g=Graphics.FromImage(sheet))
     using(var contact=new Bitmap(8*216,2*261))using(Graphics cg=Graphics.FromImage(contact)) {
      g.Clear(Color.Transparent);cg.Clear(Color.FromArgb(235,235,235));var motion=new RestMotion();motion.RequestSleep(true);double total=0,max=0;
      for(int i=0;i<360;i++) {
       if(i==180)motion.RequestSleep(false);motion.Update(1.0/60,true);
       var timer=Stopwatch.StartNew();Bitmap b=rest.Draw(standing,motion.Sleep);timer.Stop();total+=timer.Elapsed.TotalMilliseconds;max=Math.Max(max,timer.Elapsed.TotalMilliseconds);
       g.DrawImage(b,new Rectangle(i%10*144,i/10*174,144,174));
       if(i%23==0){int j=i/23;cg.DrawImage(b,new Rectangle(j%8*216,j/8*261,216,261));}
       RequireQuiet(rest.LastMinimumJacobian>0,"rest folded");
      }
      Console.WriteLine("Sleep draw mean ms="+(total/360).ToString("F2")+", max="+max.ToString("F2"));
      contact.Save("sleep-contact.png",ImageFormat.Png);
      using(var stream=new MemoryStream()) {
       sheet.Save(stream,ImageFormat.Png);
       File.WriteAllText("Sleep-Preview.html","<!doctype html><meta charset='utf-8'><title>0.6.35 睡眠离屏采样</title><style>body{font:18px system-ui;background:#ddd;text-align:center}canvas{height:522px}button{font:inherit;margin:12px}</style><p>实际 C# 渲染：3 秒入睡、3 秒起身，60 帧/秒。仅预览文件包含采样图片。</p><button onclick='play=!play'>播放/暂停</button><input id='seek' type='range' min='0' max='359' value='0'><br><canvas id='v' width='144' height='174'></canvas><script>let im=new Image(),ctx=v.getContext('2d'),play=true,f=0,last=0;im.src='data:image/png;base64,"+Convert.ToBase64String(stream.ToArray())+"';seek.oninput=()=>{play=false;f=+seek.value};function tick(t){if(play&&last)f=(f+(t-last)*.06)%360;last=t;let i=Math.floor(f);ctx.clearRect(0,0,144,174);if(im.complete)ctx.drawImage(im,i%10*144,Math.floor(i/10)*174,144,174,0,0,144,174);seek.value=i;requestAnimationFrame(tick)}im.onload=()=>requestAnimationFrame(tick)</script>");
      }
     }
    }
   }}finally{foreach(Bitmap b in sprites)b.Dispose();}
  }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}
 }
 static void RequireQuiet(bool ok,string name){if(!ok)throw new Exception(name);}
}

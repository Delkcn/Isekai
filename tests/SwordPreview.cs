using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using JouchoPet;
class SwordPreview {
 static void Main(){
  AnimationParameters.UseFixedValues();Directory.CreateDirectory("qa/v0711");Size c=PetRenderer.CanvasSize(300);
  using(var side=new SideWalk(c.Width,c.Height,300))using(var rig=new SwordRig(c.Width,c.Height,300,side))foreach(int facing in new[]{-1,1}){
   string name="sword-"+(facing<0?"left":"right");
   using(var strike=new Bitmap(500*5,640))using(var sg=Graphics.FromImage(strike)){
    sg.Clear(Color.FromArgb(45,50,62));sg.InterpolationMode=InterpolationMode.HighQualityBicubic;
    for(int i=0;i<5;i++){double t=.47+.04*i;var frame=rig.Draw(SwordMotion.At(t,facing),false);sg.DrawImage(frame,new RectangleF(i*500,40,500,500*650f/550),new RectangleF(rig.Padding-100,rig.Padding-140,550,650),GraphicsUnit.Pixel);sg.DrawString("Restored arm sweep "+t.ToString("F2")+"s",SystemFonts.DefaultFont,Brushes.White,i*500+10,15);}
    strike.Save("qa/v0711/"+name+"-strike-phases.png",ImageFormat.Png);
   }
   for(int mode=0;mode<3;mode++)using(var atlas=new Bitmap(420*9,500*8))using(var g=Graphics.FromImage(atlas)){
    g.Clear(Color.FromArgb(45,50,62));g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    for(int i=0;i<69;i++){var frame=rig.Draw(SwordMotion.At(Math.Min(SwordMotion.Duration,i/60.0),facing),mode==1,mode==2);g.DrawImage(frame,new RectangleF(i%9*420,i/9*500,420,420*650f/550),new RectangleF(rig.Padding-100,rig.Padding-140,550,650),GraphicsUnit.Pixel);}
    atlas.Save("qa/v0711/"+name+(mode==0?"-clean":mode==2?"-bones":"")+".png",ImageFormat.Png);
   }
   double[] times={.12,.42,.60,.78,1.05};string[] labels={"1 Prepare","2 Hold / conjure","3 Strike / HitFrame","4 Follow-through","5 Return"};
   using(var close=new Bitmap(400*5,400))using(var cg=Graphics.FromImage(close)){
    cg.Clear(Color.FromArgb(45,50,62));cg.InterpolationMode=InterpolationMode.NearestNeighbor;
    for(int i=0;i<5;i++){var pose=SwordMotion.At(times[i],facing);var grip=rig.Sample(pose).Grip;var frame=rig.Draw(pose,false);cg.DrawImage(frame,new Rectangle(i*400,25,400,375),new RectangleF(grip.X+rig.Padding-40,grip.Y+rig.Padding-37.5f,80,75),GraphicsUnit.Pixel);cg.DrawString(labels[i],SystemFonts.DefaultFont,Brushes.White,i*400+5,5);}
    close.Save("qa/v0711/"+name+"-grip-closeup.png",ImageFormat.Png);
   }
   foreach(bool debug in new[]{false,true})using(var sheet=new Bitmap(500*5,640))using(var g=Graphics.FromImage(sheet)){
    g.Clear(Color.FromArgb(45,50,62));g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    for(int i=0;i<5;i++){
     var frame=rig.Draw(SwordMotion.At(times[i],facing),false,debug);
     g.DrawImage(frame,new RectangleF(i*500,40,500,500*650f/550),new RectangleF(rig.Padding-100,rig.Padding-140,550,650),GraphicsUnit.Pixel);
     g.DrawString(labels[i]+"  "+times[i].ToString("F2")+"s",SystemFonts.DefaultFont,Brushes.White,i*500+10,15);
     if(!debug)using(var pose=new Bitmap(550,650))using(var pg=Graphics.FromImage(pose)){pg.Clear(Color.FromArgb(45,50,62));pg.DrawImageUnscaled(frame,100-rig.Padding,140-rig.Padding);pose.Save("qa/v0711/"+name+"-pose-"+(i+1)+".png",ImageFormat.Png);}
    }
    sheet.Save("qa/v0711/"+name+"-keyposes"+(debug?"-bones":"")+".png",ImageFormat.Png);
   }
   string html="<!doctype html><meta charset='utf-8'><title>0.7.11 · 0.7.5 动作 · 最高点停顿 0.1 秒显剑</title><style>body{background:#2d323e;color:white;font:17px system-ui;text-align:center}canvas{height:77vh}button,input,select{margin:8px}</style><p>0.7.11 · "+(facing<0?"左向":"右向")+" · 预备 / 起剑 / 斩击 / 收势 / 复位</p><button id='toggle'>暂停 / 播放</button><select id='mode'><option value='0'>无特效</option><option value='2'>骨骼与握持锚点</option><option value='1'>星芒与斩击轨迹</option></select><select id='speed'><option value='1'>正常速度</option><option value='.25'>四分之一速度</option></select><input id='seek' type='range' min='0' max='68' value='0'><span id='stamp'></span><br><canvas id='v' width='420' height='500'></canvas><script>const images=['-clean','','-bones'].map(s=>{let im=new Image();im.src='"+name+"'+s+'.png';return im}),ctx=v.getContext('2d');let f=0,last=0,play=true;toggle.onclick=()=>play=!play;seek.oninput=()=>{play=false;f=+seek.value};function tick(t){if(last&&play)f=(f+(t-last)*.06*+speed.value)%69;last=t;let n=Math.floor(f),im=images[+mode.value];if(im.complete&&im.naturalWidth)ctx.drawImage(im,n%9*420,Math.floor(n/9)*500,420,500,0,0,420,500);seek.value=n;stamp.textContent=Math.min(1.13,n/60).toFixed(2)+'s';requestAnimationFrame(tick)}requestAnimationFrame(tick);</script>";
   File.WriteAllText("qa/v0711/"+name+".html",html);
  }
  Console.WriteLine("Final visual pass: clean, bone overlay, effects and five poses generated for both directions.");
 }
}

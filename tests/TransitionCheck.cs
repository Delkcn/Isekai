using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;using JouchoPet;
class TransitionCheck {
 static int checks;static readonly string Folder=Path.Combine("qa","v0632");
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
 static double Length(PointF a,PointF b){return Math.Sqrt(Math.Pow(a.X-b.X,2)+Math.Pow(a.Y-b.Y,2));}
 static void Quiet(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Advance(Motion m,double seconds,int direction,bool run,double dt){for(double t=0;t<seconds;t+=dt)m.Update(dt,direction,300,false,run);}
 static void Main(){try {
  Directory.CreateDirectory(Folder);AnimationParameters.Load(Path.Combine(Folder,"baseline","user-animation-parameters.json"));
  foreach(bool light in new[]{false,true})foreach(double dt in new[]{1.0/30,1.0/60,1.0/120})foreach(int direction in new[]{-1,1}) {
   var m=new Motion{SelectLightTurn=delegate{return light;}};Advance(m,3,direction,true,dt);
   bool gather=false,turn=false,moving=false;double maxStep=0;
   for(double t=0;t<3;t+=dt) {
    double distance=m.Update(dt,0,300,false,false);maxStep=Math.Max(maxStep,Math.Abs(distance));
    Quiet(m.State!="brake" && m.State!="settle","run stop inserts ordinary stationary braking");
    gather|=m.State=="rungather";turn|=m.RunningTurn;moving|=m.RunningTurn && Math.Abs(distance)>.001;
   }
   Check(gather && turn && moving && m.State=="idle" && m.Speed==0 && maxStep<5,"coasting run stop reaches front / "+light+" / "+dt+" / "+direction);
   m.Reset();m.Update(dt,direction,300,false,true);Advance(m,3,-direction,true,dt);
   Check(m.State=="walk" && m.Facing==-direction && m.Speed*-direction>120,"redirect during entry finishes in requested direction");
   Advance(m,.05,0,false,dt);Advance(m,2,-direction,true,dt);
   Check(m.State=="walk" && m.Facing==-direction && m.RunBlend==1,"cancel gathering resumes run without stale turn flags");
   for(int i=0;i<300;i++)m.Update(dt,0,300,true,false);
   Check(m.State=="hold" && m.Speed==0 && !m.RunningTurn,"carry/leap hold still stops safely");
   m.Reset();Advance(m,3,direction,false,dt);bool stationary=false;
   for(int i=0;i<300;i++){double travel=m.Update(dt,-direction,300);if(m.State=="turn"){stationary=true;Quiet(travel==0 && !m.RunningTurn,"ordinary turn gained run drift");}}
   Check(stationary,"ordinary walking retains stationary turn");
  }
  Size c=PetRenderer.CanvasSize(300);var sprites=SpriteAssets.LoadFront();
  try{using(var front=new SoftRig(sprites,c.Width,c.Height,300))using(var arm=new GreetingArmRig(PoseMesh.Read(front.Draw(0,0,0,0,0,0,0,new[]{1.0,0,0})),c.Width,c.Height,300)) {
   for(int p=0;p<12;p++) {
    double phase=p*Math.PI/6;var first=arm.Sample(0,phase);var last=arm.Sample(1,phase);double length=Length(first.Elbow,first.Wrist),maxDepth=0;
    for(int i=0;i<=120;i++) {
     var pose=arm.Sample(i/120.0,phase);maxDepth=Math.Max(maxDepth,pose.ForearmDepth);
     Quiet(Math.Abs(Math.Sqrt(Math.Pow(Length(pose.Elbow,pose.Wrist),2)+pose.ForearmDepth*pose.ForearmDepth)-length)<.001,"front lift stretched forearm");
     PointF local=GreetingArmRig.Rotate(pose.Wrist,pose.Elbow,-pose.UpperAngle);
     PointF end=GreetingArmRig.Rotate(last.Wrist,last.Elbow,-last.UpperAngle);
     double dx=local.X-pose.Elbow.X,start=first.Wrist.X-first.Elbow.X,finish=end.X-last.Elbow.X;
     Quiet(dx>=Math.Min(start,finish)-.001 && dx<=Math.Max(start,finish)+.001,"front lift sweeps beyond endpoint horizontal range");
    }
    Check(maxDepth>length*.7 && first.ForearmDepth<.02 && last.ForearmDepth<.02,"forearm lifts in depth and restores approved raised pose / phase "+p);
   }
  }}finally{foreach(var b in sprites)b.Dispose();}
  // Uniform motion through a key must retain the shared derivative, not ease to zero.
  double e=1e-5,left=(PoseMesh.Coordinate(0,10,20,30,1)-PoseMesh.Coordinate(0,10,20,30,1-e))/e;
  double right=(PoseMesh.Coordinate(10,20,30,40,e)-PoseMesh.Coordinate(10,20,30,40,0))/e;
  Check(Math.Abs(left-right)<.001 && left>9.9,"monotone silhouette and color interpolation keep velocity across keys");
  foreach(double[] values in new[]{new[]{0.0,20,40,100},new[]{255.0,200,10,0},new[]{0.0,255,255,0}})for(int i=0;i<=100;i++) {
   double v=PoseMesh.Coordinate(values[0],values[1],values[2],values[3],i/100.0);
   Quiet(v>=Math.Min(values[1],values[2])-1e-6 && v<=Math.Max(values[1],values[2])+1e-6,"sleep interpolation overshoots colors");
  }
  Check(true,"sleep interpolation stays between source channel values");
  Preview();Console.WriteLine("All "+checks+" transition checks passed.");
 }catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}finally{AnimationParameters.Reset();}}
 static void Preview() {
  using(var renderer=new PetRenderer()) {
   renderer.Resize(300);Size c=PetRenderer.CanvasSize(300);
   using(var sheet=new Bitmap(c.Width*8,c.Height))using(var g=Graphics.FromImage(sheet)) {
    g.Clear(Color.FromArgb(235,231,225));
    for(int i=0;i<8;i++)g.DrawImageUnscaled(renderer.Draw(new RenderFrame{MotionState="idle",Greeting=i/7.0,WaveBlend=1}),i*c.Width,0);
    sheet.Save(Path.Combine(Folder,"wave-front-lift.png"),ImageFormat.Png);
   }
   foreach(string name in new[]{"wave","run","sleep"}) {
    int count=name=="wave"?144:195,w=name=="run"?450:216,h=name=="run"?325:261;
    using(var atlas=new Bitmap(w*10,h*((count+9)/10),PixelFormat.Format32bppPArgb))using(var g=Graphics.FromImage(atlas)) {
     for(int i=0;i<count;i++) {
      double t=i/30.0,distance=0;RenderFrame f=name=="wave"?AnimationDebugWindow.WavePreview(t):name=="sleep"?AnimationDebugWindow.SleepPreview(t):AnimationDebugWindow.RunPreview(t,1,out distance);
      Bitmap b=renderer.Draw(f);int x=i%10*w,y=i/10*h;
      if(name=="run")g.DrawImage(b,new RectangleF(x+(float)(275+distance)*.5f,y+80,c.Width*.5f,c.Height*.5f));
      else g.DrawImage(b,new Rectangle(x,y,w,h));
     }
     atlas.Save(Path.Combine(Folder,name+"-frames.png"),ImageFormat.Png);
     File.WriteAllText(Path.Combine(Folder,name+"-preview.html"),"<!doctype html><meta charset='utf-8'><title>0.6.32 "+name+"</title><style>body{font:18px system-ui;text-align:center;background:#ebe7e1}canvas{max-width:95vw;height:522px}button,input{margin:12px}</style><p>0.6.32 实际渲染 · 使用你当前的参数</p><button onclick='play=!play'>播放 / 暂停</button><select id='speed'><option value='.25'>0.25 倍</option><option value='.5'>0.5 倍</option><option value='1' selected>1 倍</option></select><input id='seek' type='range' min='0' max='"+(count-1)+"' value='0'><br><canvas id='view' width='"+w+"' height='"+h+"'></canvas><script>let im=new Image(),ctx=view.getContext('2d'),play=true,f=0,last=0;im.src='"+name+"-frames.png';seek.oninput=()=>{play=false;f=+seek.value};function tick(t){if(play&&last)f=(f+(t-last)*.03*+speed.value)%"+count+";last=t;let i=Math.floor(f);ctx.clearRect(0,0,view.width,view.height);ctx.drawImage(im,i%10*"+w+",Math.floor(i/10)*"+h+","+w+","+h+",0,0,"+w+","+h+");seek.value=i;requestAnimationFrame(tick)}im.onload=()=>requestAnimationFrame(tick)</script>");
    }
   }
  }
 }
}

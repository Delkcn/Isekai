using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using JouchoPet;
class ArtCheck {
 static int count;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Console.WriteLine("PASS "+message);}
 static double Difference(Bitmap a,Bitmap b){int[] x=PoseMesh.Read(a),y=PoseMesh.Read(b);double sum=0;for(int i=0;i<x.Length;i++)for(int s=0;s<32;s+=8)sum+=Math.Abs(((uint)x[i]>>s&255)-(double)((uint)y[i]>>s&255));return sum/(x.Length*4);}
 static void Main(string[] args) {try{if(args.Length>0 && args[0]=="--smoke")Smoke();else Run();Console.WriteLine("All "+count+" unified artwork checks passed.");}catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
 static void Smoke() {
  Check(!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets")),"pack-only directory has no source assets");
  using(var renderer=new PetRenderer()) {
   renderer.Resize(300);
   foreach(double turn in new[]{-1.0,-.75,-.5,-.25,0,.25,.5,.75,1})Check(PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="turn",Angle=turn})).Length>0,"pack-only turn "+turn);
   foreach(int sign in new[]{-1,1})foreach(double progress in new[]{.1,.3,.5,.75,.95})Check(PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="turn",LightTurn=true,TurnProgress=progress,TurnFrom=-sign,SupportNear=sign<0,Angle=-sign+2*sign*PivotTurn.Progress(progress)})).Length>0,"pack-only single-leg turn "+sign+" / "+progress);
   foreach(double rest in new[]{0.0,.1,.25,.5,.75,.9,1})Check(PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="idle",RestAmount=rest})).Length>0,"pack-only rest "+rest);
   foreach(int facing in new[]{-1,1})foreach(int vertical in new[]{-1,0,1})Check(PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="walk",Facing=facing,Speed=36,Gait=.25,Carry=1,CarryEffort=1,CarryVertical=vertical})).Length>0,"pack-only carry "+facing+" / vertical "+vertical);
   foreach(int facing in new[]{-1,1})foreach(double phase in new[]{0.0,.23,.5,.73})Check(PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="walk",Facing=facing,Speed=132,Gait=phase,RunBlend=1})).Length>0,"pack-only run "+facing+" / "+phase);
   foreach(int facing in new[]{-1,1})foreach(double phase in new[]{0.0,.25,.5,.75})Check(PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="walk",Facing=facing,Speed=132,Gait=phase,RunBlend=1,RunningCarry=true,Carry=1,CarryEffort=.65})).Length>0,"pack-only running carry "+facing+" / "+phase);
   foreach(int facing in new[]{-1,1})foreach(int vertical in new[]{-1,1})foreach(double age in new[]{.3,.8,1.6})Check(PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="hold",Facing=facing,Leaping=true,Leap=LeapMotion.At(age,vertical),Carry=1,CarryEffort=.65,CarryVertical=vertical})).Length>0,"pack-only leaping carry "+facing+" / "+vertical+" / "+age);
   foreach(int facing in new[]{-1,1})foreach(int vertical in new[]{-1,1})foreach(double age in new[]{.2,.75,1.5})Check(PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="hold",Facing=facing,Leaping=true,Leap=LeapMotion.At(age,vertical)})).Length>0,"pack-only leap "+facing+" / "+vertical+" / "+age);
   foreach(double phase in new[]{0.0,Math.PI})Check(PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="idle",Greeting=1,WavePhase=phase})).Length>0,"pack-only greeting "+phase);
   foreach(double phase in new[]{0.0,Math.PI})Check(PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="idle",Greeting=1,WavePhase=phase,WaveBlend=1})).Length>0,"pack-only wave "+phase);
   foreach(int side in new[]{-1,1})Check(PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="idle",Affection=1,ClosedEyes=1,PatSide=side})).Length>0,"pack-only head tilt "+side);
  }
 }
 static int EyePixels(Bitmap b,Rectangle roi,bool blue) {
  int n=0;for(int y=roi.Top;y<roi.Bottom;y++)for(int x=roi.Left;x<roi.Right;x++){
   Color c=b.GetPixel(x,y);if(c.A<180)continue;
   if(blue ? c.B>c.R+12 && c.G>c.R+8 : c.R>145 && c.R-c.G>25 && c.R-c.B>15 && c.B-c.G>3)n++;
  }return n;
 }
 static void EyeColorChecks() {
  using(var art=AssetStore.Image(CharacterArt.Resource)) {
   Check(EyePixels(art,new Rectangle(907,118,17,14),true)>100,"frontal anatomical left iris stays blue");
   Check(EyePixels(art,new Rectangle(857,134,23,7),false)==0,"frontal right iris no longer hangs below the left eye");
   Check(EyePixels(art,new Rectangle(1308,126,6,7),true)>12,"right-turn far left iris is blue, not a second red eye");
   Check(EyePixels(art,new Rectangle(632,588,5,5),true)>8,"right-push far left iris is blue");
   Check(EyePixels(art,new Rectangle(1270,123,18,14),false)>10,"right-turn near right iris remains pale red");
   Check(EyePixels(art,new Rectangle(597,577,18,14),false)>10,"right-push near right iris remains pale red");
  }
 }
 static void Run() {
  string root=AppDomain.CurrentDomain.BaseDirectory;
  using(var sha=SHA256.Create())Check(Convert.ToBase64String(sha.ComputeHash(AssetStore.Read(CharacterArt.Resource)))==Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(Path.Combine(root,"assets",CharacterArt.Resource)))),"pack preserves approved PNG byte for byte");
  EyeColorChecks();
  Size c=PetRenderer.CanvasSize(300);
  string[] names={"profile-left","left","front","right","profile-right","push-left","push-right","crouch","sleep","pat"};
  using(var sheet=new Bitmap(c.Width*5,(c.Height+25)*2))using(Graphics g=Graphics.FromImage(sheet)) {
   g.Clear(Color.FromArgb(235,235,235));
   for(int i=0;i<names.Length;i++)using(Bitmap source=CharacterArt.Load(names[i]))using(Bitmap image=CharacterArt.Normalize(source,c.Width,c.Height,300)) {
    int visible=0,edge=0;int[] p=PoseMesh.Read(image);
    for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(((uint)p[y*image.Width+x]>>24)>80){visible++;if(x<2||y<2||x>=image.Width-2||y>=image.Height-2)edge++;}
    Check(visible>5000,"nonempty pose "+names[i]);Check(edge==0,"pose fits canvas "+names[i]);
    g.DrawImageUnscaled(image,(i%5)*c.Width,(i/5)*(c.Height+25));g.DrawString(names[i],SystemFonts.DefaultFont,Brushes.Black,(i%5)*c.Width+8,(i/5)*(c.Height+25)+c.Height);
    if(names[i]=="front")source.Save("front-source.png",ImageFormat.Png);
   }sheet.Save("approved-art-import.png",ImageFormat.Png);
  }
  foreach(int size in new[]{220,300,400}) {
   Size canvas=PetRenderer.CanvasSize(size);var sprites=SpriteAssets.LoadFront();
   try{using(var front=new SoftRig(sprites,canvas.Width,canvas.Height,size))using(var side=new SideWalk(canvas.Width,canvas.Height,size))using(var turn=new TurnRig(canvas.Width,canvas.Height,size)) {
    Bitmap neutral=front.Draw(0,0,0,0,0,0,0,new[]{1.0,0,0});turn.Prepare(neutral,side);
    foreach(int facing in new[]{-1,1})using(Bitmap endpoint=(Bitmap)turn.Draw(facing,neutral,side,0).Clone()) {
     Check(Difference(endpoint,side.Draw(0,0,0,facing,1))==0,"turn/walk endpoint identity "+size+" "+facing);
     double error=Difference(endpoint,turn.Draw(facing*(1-1e-6),neutral,side,0));Console.WriteLine("endpoint mean error "+error.ToString("F4"));Check(error<.5,"turn converges to profile "+size+" "+facing);
    }
    for(int i=0;i<=160;i++){turn.Draw(-1+i/80.0,neutral,side,0);CheckQuiet(turn.LastMinimumJacobian>0,"turn mesh inverted");}
    Check(true,"161 turn angles have positive triangles at "+size);
    using(var rest=new RestRig(sprites,canvas.Width,canvas.Height,size,neutral)) {
     for(int i=0;i<=80;i++){rest.Draw(neutral,i/80.0);CheckQuiet(rest.LastMinimumJacobian>0,"sleep mesh inverted");}
     using(Bitmap a=(Bitmap)rest.Draw(neutral,.5-1e-6).Clone())Check(Difference(a,rest.Draw(neutral,.5+1e-6))<.1,"crouch key continuous at "+size);
    }
   }}finally{foreach(Bitmap b in sprites)b.Dispose();}
  }
  using(var renderer=new PetRenderer())using(var sheet=new Bitmap(8*174,5*174))using(Graphics g=Graphics.FromImage(sheet)) {
   renderer.Resize(300);g.Clear(Color.FromArgb(235,235,235));
   for(int row=0;row<5;row++)for(int col=0;col<8;col++) {
    double t=col/7.0;var f=new RenderFrame{Time=0,MotionState="idle"};
    if(row==0){f.MotionState="turn";f.Angle=-1+2*t;}
    if(row==1||row==2){f.MotionState="walk";f.Facing=row==1?-1:1;f.Speed=36;f.Gait=t;f.Settle=0;}
    if(row==3)f.RestAmount=t;
    if(row==4){f.Affection=t;f.ClosedEyes=t;f.PatSide=1;}
    g.DrawImage(renderer.Draw(f),new Rectangle(col*174,row*174,174,174));
   }sheet.Save("runtime-poses.png",ImageFormat.Png);
  }
  ExportProportions();ExportPreview();
 }
 static void ExportProportions() {
  const int size=300;Size canvas=PetRenderer.CanvasSize(size);
  string[] names={"profile-left","left","front","right","profile-right"};
  using(var renderer=new PetRenderer())using(var sheet=new Bitmap(canvas.Width*5,canvas.Height+60))using(Graphics g=Graphics.FromImage(sheet))using(var line=new Pen(Color.FromArgb(150,40,130,180)))using(var floor=new Pen(Color.FromArgb(190,160,100,30))) {
   renderer.Resize(size);g.Clear(Color.FromArgb(245,245,245));
   for(int i=0;i<5;i++) {
    Bitmap picture=renderer.Draw(new RenderFrame{MotionState="turn",Angle=-1+i*.5});
    var pose=CharacterArt.Definition(names[i]);double crown=CharacterArt.Y(pose,pose.Crown,size),chin=CharacterArt.Y(pose,pose.Chin,size),head=chin-crown;
    int[] pixels=PoseMesh.Read(picture);int top=picture.Height,bottom=0;
    for(int y=0;y<picture.Height;y++)for(int x=0;x<picture.Width;x++)if(((uint)pixels[y*picture.Width+x]>>24)>200){top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
    double ratio=(bottom-top+1)/head;
    Check(ratio>=3 && ratio<=4,"rendered crown/sole bounds are 3-4 heads: "+names[i]+" = "+ratio.ToString("F2"));
    g.DrawImageUnscaled(picture,i*canvas.Width,0);
    for(int k=0;k<=3;k++){float y=(float)(crown+k*head);g.DrawLine(line,i*canvas.Width+25,y,(i+1)*canvas.Width-25,y);g.DrawString(k==0?"Crown":k+" head",SystemFonts.DefaultFont,Brushes.SteelBlue,i*canvas.Width+27,y+2);}
    g.DrawLine(floor,i*canvas.Width+25,bottom,(i+1)*canvas.Width-25,bottom);
    g.DrawString(names[i]+"\n"+ratio.ToString("F2")+" heads (crown to chin unit)",SystemFonts.DefaultFont,Brushes.Black,i*canvas.Width+25,canvas.Height+5);
   }
   sheet.Save("proportion-guide.png",ImageFormat.Png);
  }
 }
 static void ExportPreview() {
  string[] kinds={"turn","walk-left","walk-right","carry-left","carry-right","sleep","pat"};
  var items=new System.Collections.Generic.Dictionary<string,object>();
  using(var renderer=new PetRenderer()) {
   renderer.Resize(300);
   foreach(string kind in kinds) {
    int frames=kind=="sleep"?360:kind=="pat"?240:120;var motion=new Motion();var rest=new RestMotion();
    if(kind=="sleep")rest.RequestSleep(true);if(kind=="pat")rest.RequestPat(1);
    using(var sheet=new Bitmap(10*174,((frames+9)/10)*174,PixelFormat.Format32bppPArgb))using(Graphics g=Graphics.FromImage(sheet))using(var bytes=new MemoryStream()) {
     g.Clear(Color.Transparent);
     for(int i=0;i<frames;i++) {
      var f=new RenderFrame{Time=i/60.0,MotionState="idle",PatSide=1};
      if(kind=="turn"){f.MotionState="turn";f.Angle=-1+2*i/(double)(frames-1);}
      if(kind.StartsWith("walk")){f.MotionState="walk";f.Facing=kind.EndsWith("left")?-1:1;f.Speed=36;f.Gait=i/60.0*36/(300*SideWalk.StrideRatio);}
      if(kind.StartsWith("carry")) {
       int facing=kind.EndsWith("left")?-1:1;bool push=i>=30 && i<90;
       if(i==0)motion=new Motion{State="hold",Facing=facing,Angle=facing,Settle=1};motion.Update(1.0/60,push?facing:0,300,true);
       double reach=i<30?IconCarryPlan.Smooth(i/29.0):i>=90?1-IconCarryPlan.Smooth((i-90)/29.0):1;
       f.MotionState=motion.State;f.Facing=facing;f.Speed=motion.Speed;f.Gait=motion.Phase;f.Settle=motion.Settle;f.Carry=reach;f.CarryEffort=reach*(push?1:.65);
      }
      if(kind=="sleep"||kind=="pat") {
       if(kind=="sleep"&&i==180)rest.RequestSleep(false);rest.Update(1.0/60,true);
       f.RestAmount=rest.Sleep;f.Affection=rest.Affection;f.ClosedEyes=rest.Eyes;f.Nod=rest.Nod;
      }
      g.DrawImage(renderer.Draw(f),new Rectangle(i%10*174,i/10*174,174,174));
     }
     sheet.Save(bytes,ImageFormat.Png);items[kind]=new {count=frames,image="data:image/png;base64,"+Convert.ToBase64String(bytes.ToArray())};
    }
   }
  }
  var json=new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=int.MaxValue};
  File.WriteAllText("Animation-Preview.html","<!doctype html><meta charset='utf-8'><title>人物动画预览</title><style>body{font:18px system-ui;text-align:center;background:#e5e5e5;color:#222}canvas{width:480px;height:480px}button,select{font:inherit;margin:10px}input{width:360px}</style><p>新版人物 · 实际 C# 渲染采样</p><select id='mode'><option value='turn'>转身</option><option value='walk-left'>向左走</option><option value='walk-right'>向右走</option><option value='carry-left'>向左搬运姿态</option><option value='carry-right'>向右搬运姿态</option><option value='sleep'>入睡与起立</option><option value='pat'>摸头与微笑</option></select><button id='toggle'>暂停</button><select id='speed'><option value='1'>正常速度</option><option value='.25'>四分之一速度</option></select><br><canvas id='view' width='174' height='174'></canvas><br><input id='seek' type='range' min='0' max='119' value='0'><p>预览不移动真实桌面图标。</p><script>const data="+json.Serialize(items)+";let play=true,f=0,last=0,im=new Image(),ctx=view.getContext('2d');function change(){f=0;seek.max=data[mode.value].count-1;im.src=data[mode.value].image}mode.onchange=change;toggle.onclick=()=>{play=!play;toggle.textContent=play?'暂停':'播放'};seek.oninput=()=>{play=false;f=+seek.value;toggle.textContent='播放'};function tick(t){if(play&&last)f=(f+(t-last)*.06*Number(speed.value))%data[mode.value].count;last=t;let i=Math.floor(f);ctx.clearRect(0,0,174,174);if(im.complete&&im.naturalWidth)ctx.drawImage(im,i%10*174,Math.floor(i/10)*174,174,174,0,0,174,174);seek.value=i;requestAnimationFrame(tick)}change();requestAnimationFrame(tick)</script>");
 }
 static void CheckQuiet(bool ok,string message){if(!ok)throw new Exception(message);}
}

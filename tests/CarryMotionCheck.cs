using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;using System.Windows.Forms;using JouchoPet;
class CarryMotionCheck {
 static int checks;static readonly string Folder=Path.Combine("qa","v0636");
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static void Quiet(bool ok,string name){if(!ok)throw new Exception(name);}
 [STAThread] static void Main(){try {
  AnimationParameters.Load(Path.Combine(Folder,"baseline","user-animation-parameters.json"));
  Paths();NativePositions();Lifecycle();Rendering();Elbow();Console.WriteLine("All "+checks+" new carry/elbow checks passed.");
 }catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}finally{AnimationParameters.Reset();}}
 static void Paths(){foreach(int size in new[]{220,300,400})foreach(Point axis in new[]{new Point(-1,0),new Point(1,0),new Point(0,-1),new Point(0,1)})foreach(CarryStyle style in new[]{CarryStyle.Walk,CarryStyle.Run}) {
  var plan=new CarryTravel(new Point(300,300),new Point(300+axis.X*120,300+axis.Y*120),size,style);
  foreach(double dt in new[]{1.0/30,1.0/60,.05}) {
   var motion=new Motion();Point previous=plan.Start;double oldDistance=0,peak=0;int moved=0;bool crouched=false,air=false,landed=false;
   for(int i=0;i<=Math.Ceiling(plan.Duration/dt);i++) {
    double age=Math.Min(plan.Duration,i*dt);var s=plan.At(age);peak=Math.Max(peak,s.Velocity);
    Quiet(s.Distance>=oldDistance-1e-8 && s.Distance<=120+1e-8,"carry reverses or overshoots");
    double maxSpeed=plan.Style==CarryStyle.Run?RunGait.SpeedAt300*size/300:plan.Style==CarryStyle.Leap?120/(axis.Y<0?.95:.85)*1.875:45;
    Quiet(IconCarryPlan.Distance(previous,s.Position)<=Math.Ceiling(maxSpeed*dt)+1,"icon jumps beyond frame travel limit");
    Quiet(axis.X==0?s.Position.X==300:s.Position.Y==300,"icon leaves planned clear corridor");
    if(s.Position!=previous)moved++;
    motion.FollowPush(s.Distance-oldDistance,s.Velocity,axis.X==0?1:axis.X,size,plan.Style==CarryStyle.Run);
    if(plan.Style==CarryStyle.Leap){crouched|=age<.3 && s.Leap.Crouch>.1;air|=s.Leap.Air>.8;landed|=age>plan.Duration-.62 && s.Leap.Crouch>.5;}
    previous=s.Position;oldDistance=s.Distance;
   }
   Quiet(previous==plan.End && moved>10,"move is endpoint-only or incomplete");
   double blend=plan.Style==CarryStyle.Run?1:0;
   Quiet(Math.Abs(motion.Phase-120*RunGait.Contact(blend)/RunGait.Stride(size,blend))<1e-7,"carry gait lost distance synchronization");
   if(plan.Style==CarryStyle.Run)Quiet(Math.Abs(peak-RunGait.SpeedAt300*size/300)<1e-7,"run carry fails to attain configured running speed");
   if(plan.Style==CarryStyle.Leap)Quiet(crouched && air && landed,"vertical carrying omits preparation flight or landing");
   Check(true,"continuous accepted-position path, gait and full action / "+size+" / "+axis+" / "+plan.Style+" / "+dt);
  }
  var before=plan.At(plan.Duration*.4);double speed=AnimationParameters.Get("runSpeed");AnimationParameters.Set("runSpeed",60);
  var after=plan.At(plan.Duration*.4);Check(before.Position==after.Position && before.Velocity==after.Velocity,"in-flight path survives live speed edit");AnimationParameters.Set("runSpeed",speed);
  Check(plan.At(-1).Position==plan.Start && plan.At(plan.Duration+1).Position==plan.End && plan.At(plan.Duration).Velocity==0,"exact endpoints and stopped landing");
  if(plan.Style==CarryStyle.Leap)Check(plan.At(plan.Duration-.61).Position==plan.End && plan.At(plan.Duration-.3).Leap.Crouch>0,"icon stays at endpoint during landing before release");
 }}
 sealed class QuietForm:Form {protected override bool ShowWithoutActivation{get{return true;}}}
 static void NativePositions(){using(var host=new QuietForm{ShowInTaskbar=false,Opacity=0,StartPosition=FormStartPosition.Manual,Location=new Point(-10000,-10000),ClientSize=new Size(700,700)})using(var list=new ListView{Dock=DockStyle.Fill,View=View.LargeIcon,AutoArrange=false}) {
  host.Controls.Add(list);list.Items.Add("isolated-test-item");host.Show();Application.DoEvents();
  foreach(Point axis in new[]{new Point(-1,0),new Point(1,0),new Point(0,-1),new Point(0,1)})using(var native=new DesktopIconView(list.Handle)) {
   native.SetGrid(true);Quiet(DesktopIconView.GridEnabled(list.Handle),"test view grid setup failed");
   try {
    native.SetGrid(false);native.Move(0,native.Read(0),new Point(300,300));
    var plan=new CarryTravel(new Point(300,300),new Point(300+axis.X*120,300+axis.Y*120),300,CarryStyle.Run);Point prior=plan.Start;int steps=0;
    for(double t=0;t<plan.Duration+.02;t+=.02){Point target=plan.At(t).Position;if(target==prior)continue;Quiet(native.Move(0,prior,target)==target,"native list view deferred intermediate move");prior=target;steps++;}
    Check(prior==plan.End && steps>20,"native icon view accepts intermediate positions / "+axis+" / "+steps);
    native.Move(0,prior,new Point(299,299));bool rejected=false;try{native.Move(0,prior,plan.End);}catch(InvalidOperationException){rejected=true;}
    Check(rejected,"external position change stops movement");
   }finally{native.SetGrid(true);}
   Check(DesktopIconView.GridEnabled(list.Handle),"isolated view grid flag restored after action");
  }
 }}
 static int Changes(int[] a,int[] b,int begin,int end){int n=0;for(int i=begin;i<end;i++)if(a[i]!=b[i])n++;return n;}
 static void Lifecycle(){
  const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public;
  foreach(CarryStyle style in new[]{CarryStyle.Walk,CarryStyle.Run,CarryStyle.Leap}) {
   var pet=(Pet)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Pet));GC.SuppressFinalize(pet);
   Action<string,object> set=(name,value)=>typeof(Pet).GetField(name,flags).SetValue(pet,value);
   Func<string,object> get=name=>typeof(Pet).GetField(name,flags).GetValue(pet);
   var motion=new Motion();var plan=new CarryTravel(Point.Empty,style==CarryStyle.Leap?new Point(0,-120):new Point(120,0),300,style);
   using(var status=new ToolStripMenuItem()) {
    set("carryPhase","push");set("carryTravel",plan);set("carryAge",plan.Duration*.4);set("carryFacing",1);set("petHeight",300);set("motion",motion);set("rest",new RestMotion());set("carryLayout",IconPositions.Layout());set("menuOpen",true);set("carryStatus",status);set("iconStart",Point.Empty);set("iconEnd",plan.End);set("iconLast",plan.At(plan.Duration*.4).Position);
    typeof(Pet).GetMethod("FollowCarrySample",flags).Invoke(pet,new object[]{plan.At(plan.Duration*.4)});
    Check(style==CarryStyle.Leap?motion.State=="hold" && motion.RunBlend==0:motion.RunBlend==(style==CarryStyle.Run?1:0),"application applies selected gait while preserving leap pose / "+style);
    double age=(double)get("carryAge"),phase=motion.Phase;object position=get("iconLast");
    typeof(Pet).GetMethod("CarryDesired",flags).Invoke(pet,new object[]{.05});typeof(Pet).GetMethod("CarryAfterMove",flags).Invoke(pet,new object[]{.05});
    Check((double)get("carryAge")==age && motion.Phase==phase && position.Equals(get("iconLast")),"menu freezes icon path, gait and leap clock together / "+style);
    typeof(Pet).GetMethod("CancelCarry",flags).Invoke(pet,new object[]{"test interrupt"});
    Check((string)get("carryPhase")=="" && get("carryTravel")==null && motion.RunBlend==0 && motion.Speed==0 && position.Equals(get("iconLast")),"cancel clears carrying action and retains last accepted position / "+style);
   }
  }
 }
 static void Rendering(){foreach(int size in new[]{220,300,400})foreach(int facing in new[]{-1,1}) {
  Size c=PetRenderer.CanvasSize(size);using(var side=new SideWalk(c.Width,c.Height,size)) {
   int[] first=PoseMesh.Read(side.Draw(0,0,1,facing,0,1,.65,0,1,true));
   int[] next=PoseMesh.Read(side.Draw(0,.25,1,facing,0,1,.65,0,1,true));int hip=(int)(28+size*.62);
   Check(Changes(first,next,0,hip*c.Width)==0 && Changes(first,next,hip*c.Width,first.Length)>size,"running carry holds upper body still while legs run / "+size+" / "+facing);
   var held=PoseMesh.Read(side.Draw(0,0,0,facing,1,1,.65));var start=PoseMesh.Read(side.Draw(0,0,0,facing,1,1,.65,0,1,true));
   Check(Changes(held,start,0,held.Length)==0,"run pickup and release retain held pose exactly");
   foreach(int vertical in new[]{-1,1}) {
    held=PoseMesh.Read(side.Draw(0,0,0,facing,1,1,.65,vertical));
    start=PoseMesh.Read(side.DrawLeap(facing,LeapMotion.At(0,vertical),1,.65,vertical));
    var end=PoseMesh.Read(side.DrawLeap(facing,LeapMotion.At(5,vertical),1,.65,vertical));
    Check(Changes(held,start,0,held.Length)==0 && Changes(held,end,0,held.Length)==0,"leap pickup and release retain grip / "+vertical);
    int n=0;for(int i=0;i<24;i++) {
     int[] pixels=PoseMesh.Read(side.DrawLeap(facing,LeapMotion.At(i*2.0/24,vertical),1,.65,vertical));
     for(int y=0;y<c.Height;y++)for(int x=0;x<c.Width;x++)if(((uint)pixels[y*c.Width+x]>>24)>80){n++;Quiet(x>0 && y>0 && x<c.Width-1 && y<c.Height-1,"carry leap clips artwork");}
    }
    Check(n>size*size,"carrying leap retains textured artwork throughout action");
   }
  }
 }}
 static void Elbow(){foreach(int size in new[]{220,300,400}) {
  Size c=PetRenderer.CanvasSize(size);var sprites=SpriteAssets.LoadFront();try{using(var front=new SoftRig(sprites,c.Width,c.Height,size))using(var arm=new GreetingArmRig(PoseMesh.Read(front.Draw(0,0,0,0,0,0,0,new[]{1.0,0,0})),c.Width,c.Height,size)) {
   int minimum=100;for(int i=0;i<24;i++){
    double phase=i*Math.PI/12;var p=arm.Sample(1,phase);int[] pixels=arm.Draw(1,phase);int skin=0;
    for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++){
     int ix=(int)Math.Round(p.Elbow.X)+x,iy=(int)Math.Round(p.Elbow.Y+size*.011)+y;uint v=(uint)pixels[iy*c.Width+ix];double r=(v>>16)&255,g=(v>>8)&255,b=v&255;
     if((v>>24)>240 && r>220 && r-g>18 && g-b<40)skin++;
    }
    minimum=Math.Min(minimum,skin);
   }
   Check(minimum>=6,"visible outer elbow retains skin rather than background hair / "+size+" / "+minimum);
  }}finally{foreach(var b in sprites)b.Dispose();}
 }}
 static void Preview(){Size c=PetRenderer.CanvasSize(300);using(var renderer=new PetRenderer())using(var contact=new Bitmap(450*6,325*4))using(var cg=Graphics.FromImage(contact)) {
  renderer.Resize(300);cg.Clear(Color.FromArgb(235,231,225));
  for(int mode=0;mode<4;mode++) {
   int vertical=mode==2?-1:mode==3?1:0;var travel=new CarryTravel(Point.Empty,new Point(vertical==0?120:0,vertical*120),300,mode==1?CarryStyle.Run:CarryStyle.Walk);
   double duration=travel.Duration+1.7;int frames=(int)Math.Ceiling(duration*30);
   using(var atlas=new Bitmap(450*10,325*((frames+9)/10),PixelFormat.Format32bppPArgb))using(var g=Graphics.FromImage(atlas))using(var stage=new Bitmap(900,650))using(var sg=Graphics.FromImage(stage)) {
    for(int i=0;i<frames;i++) {
     double t=i/30.0,amount=t<.65?IconCarryPlan.Smooth(t/.65):t<travel.Duration+.65?1:1-IconCarryPlan.Smooth((t-travel.Duration-.65)/.65);
     var s=travel.At(t-.65);var m=new Motion();m.FollowPush(s.Distance,s.Velocity,1,300,mode==1);
     var f=new RenderFrame{MotionState="hold",Facing=1,Carry=amount,CarryEffort=.65*amount,CarryVertical=vertical,RunBlend=m.RunBlend,RunningCarry=mode==1,Speed=m.Speed,Gait=m.Phase,Settle=m.Settle,Leaping=vertical!=0 && t>=.65 && t<=.65+travel.Duration,Leap=s.Leap};
     sg.Clear(Color.FromArgb(235,231,225));sg.DrawImageUnscaled(renderer.Draw(f),275+s.Position.X,180+s.Position.Y);
     int ix=275+s.Position.X+c.Width/2+60-16,iy=312+s.Position.Y;
     sg.FillRectangle(Brushes.AliceBlue,ix,iy,32,40);sg.DrawRectangle(Pens.SteelBlue,ix,iy,32,40);sg.DrawLine(Pens.SteelBlue,ix+7,iy+14,ix+25,iy+14);
     g.DrawImage(stage,new Rectangle(i%10*450,i/10*325,450,325));
    }
    atlas.Save(Path.Combine(Folder,"carry-"+mode+"-frames.png"),ImageFormat.Png);
    for(int j=0;j<6;j++){int frame=Math.Min(frames-1,(int)Math.Round((.65+travel.Duration*j/5)*30));cg.DrawImage(atlas,new Rectangle(j*450,mode*325,450,325),new Rectangle(frame%10*450,frame/10*325,450,325),GraphicsUnit.Pixel);}
    File.WriteAllText(Path.Combine(Folder,"carry-"+mode+"-preview.html"),"<!doctype html><meta charset='utf-8'><title>搬运动作预览</title><style>body{text-align:center;background:#ebe7e1;font:18px system-ui}canvas{height:650px;max-width:95vw}input,button,select{margin:10px}</style><p>0.6.33 实际人物渲染 · 示例图标 · "+new[]{"走动推动","跑动推动","抱住向上轻跃","抱住向下轻落"}[mode]+"</p><button onclick='play=!play'>播放 / 暂停</button><select id='speed'><option value='.25'>0.25 倍</option><option value='.5'>0.5 倍</option><option value='1' selected>1 倍</option></select><input id='seek' type='range' min='0' max='"+(frames-1)+"' value='0'><br><canvas id='view' width='450' height='325'></canvas><script>let im=new Image(),ctx=view.getContext('2d'),play=true,f=0,last=0;im.src='carry-"+mode+"-frames.png';seek.oninput=()=>{play=false;f=+seek.value};function tick(t){if(play&&last)f=(f+(t-last)*.03*+speed.value)%"+frames+";last=t;let i=Math.floor(f);ctx.clearRect(0,0,450,325);ctx.drawImage(im,i%10*450,Math.floor(i/10)*325,450,325,0,0,450,325);seek.value=i;requestAnimationFrame(tick)}im.onload=()=>requestAnimationFrame(tick)</script>");
   }
  }
  contact.Save(Path.Combine(Folder,"carry-contact.png"),ImageFormat.Png);
 }}
}

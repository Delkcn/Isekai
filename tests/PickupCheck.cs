using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;using System.Reflection;using System.Runtime.Serialization;using System.Windows.Forms;using JouchoPet;
class PickupCheck {
 static int checks;static string folder=Path.Combine("qa","v0638");
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static void Quiet(bool ok,string name){if(!ok)throw new Exception(name);}
 static double Diff(int[] a,int[] b){double sum=0;for(int i=0;i<a.Length;i++)for(int s=0;s<32;s+=8)sum+=Math.Abs((int)((uint)a[i]>>s&255)-(int)((uint)b[i]>>s&255));return sum/(a.Length*4);}
 [STAThread] static void Main(){try{AnimationParameters.Load(Path.Combine(folder,"baseline","user-animation-parameters.json"));Paths();Rendering();PauseCancel();Preview();Console.WriteLine("All "+checks+" pickup/end-pose checks passed.");}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}finally{AnimationParameters.Reset();}}
 static void Paths(){foreach(int size in new[]{220,300,400})foreach(int facing in new[]{-1,1})foreach(int dy in new[]{-150,0,150})foreach(int lead in new[]{8,60,210}) {
  Point start=new Point(400,300),contact=new Point(400+facing*lead,300+dy),icon=new Point(700,600),end=new Point(700+facing*120,600);
  var plan=new RunningPickup(start,contact,icon,end,size,RunGait.SpeedAt300*size/300*.22);
  var motion=new Motion{Phase=2.31};double previous=0,grip=0;int steps=0;Point lastIcon=icon;
  for(int i=0;i<=Math.Ceiling(plan.Duration*60);i++) {
   var sample=plan.At(Math.Min(plan.Duration,i/60.0));
   Quiet(sample.Distance>=previous && sample.Grip>=grip,"sequence rewinds");
   Quiet(sample.Pet.X>=Math.Min(start.X,contact.X+facing*120)-.01 && sample.Pet.X<=Math.Max(start.X,contact.X+facing*120)+.01 && sample.Pet.Y>=Math.Min(start.Y,contact.Y)-.01 && sample.Pet.Y<=Math.Max(start.Y,contact.Y)+.01,"approach escapes bounded route");
   if(sample.Distance<plan.ApproachDistance)Quiet(sample.Icon==icon,"icon moves before pickup");
   if(sample.Icon!=lastIcon){steps++;Quiet(IconCarryPlan.Distance(lastIcon,sample.Icon)<=Math.Ceiling(RunGait.SpeedAt300*size/300/60)+1,"icon jumps at pickup");}
   motion.FollowPush(sample.Distance-previous,sample.Velocity,facing,size,true);
   if(sample.Grip>.01 && sample.Distance<plan.ApproachDistance+40)Quiet(sample.Velocity>10 && motion.RunBlend==1 && motion.Settle<.01,"feet stop during pickup");
   previous=sample.Distance;grip=sample.Grip;lastIcon=sample.Icon;
  }
  double low=0,high=plan.Duration;for(int i=0;i<50;i++){double t=(low+high)/2;if(plan.At(t).Distance<plan.ApproachDistance)low=t;else high=t;}
  var before=plan.At(high-.0001);var after=plan.At(high+.0001);
  Quiet(Math.Abs(before.Velocity-after.Velocity)<.25 && before.Velocity>10 && before.Grip>.999 && after.Grip==1,"pickup resets speed or hands");
  Quiet(Math.Abs(motion.Phase-2.31-(plan.ApproachDistance+120)*RunGait.Contact(1)/RunGait.Stride(size,1))<1e-7,"pickup resets accumulated step phase");
  Check(lastIcon==end && steps>15 && motion.Speed==0,"run-in and push share speed, gait, grip and accepted icon path / "+size+" / "+facing+" / "+dy+" / "+lead);
 }
 foreach(double initial in new[]{0.0,30,132}) {
  var plan=new CarryTravel(Point.Empty,new Point(120,0),300,CarryStyle.Run,initial);
  Check(Math.Abs(plan.At(0).Velocity-initial)<1e-8 && plan.At(plan.Duration).Position.X==120 && plan.At(plan.Duration).Velocity==0,"run trajectory preserves incoming speed and final stop / "+initial);
 }}
 static void Rendering(){foreach(int size in new[]{220,300,400}) {
  Size c=PetRenderer.CanvasSize(size);var sprites=SpriteAssets.LoadFront();try{using(var front=new SoftRig(sprites,c.Width,c.Height,size)) {
   Bitmap standing=front.Draw(0,0,0,0,0,0,0,new[]{1.0,0,0});using(var rest=new RestRig(sprites,c.Width,c.Height,size,standing)) {
    Check(rest.DiagnosticKeys.Length==9 && Diff(rest.DiagnosticKeys[7].Pixels,rest.DiagnosticKeys[8].Pixels)==0,"v0.6.34 final transition and sleep share the approved drawing / "+size);
    int[] final=PoseMesh.Read(rest.Draw(standing,1));
    int[] left=PoseMesh.Read(rest.Draw(standing,1-1e-6));Check(Diff(left,final)<.25,"terminal sleep bone pose has no boundary jump / "+size);
   }
   using(var arm=new GreetingArmRig(PoseMesh.Read(standing),c.Width,c.Height,size)) {
    int maxWidth=0;int[] cap=PoseMesh.Read(arm.DiagnosticElbowCap);for(int y=0;y<c.Height;y++){int n=0;for(int x=0;x<c.Width;x++)if(((uint)cap[y*c.Width+x]>>24)>128)n++;maxWidth=Math.Max(maxWidth,n);}
    Check(maxWidth<=Math.Ceiling(size*.037) && maxWidth>=size*.026,"elbow connection matches narrow arm width / "+size+" / "+maxWidth);
   }
  }}finally{foreach(var b in sprites)b.Dispose();}
  foreach(int d in new[]{0,1})using(var body=CharacterArt.Normalized(d==0?"profile-left":"profile-right",c.Width,c.Height,size))using(var arm=new CarryArmRig(d,body,size)) {
   for(int i=0;i<16;i++) {
    double phase=i/16.0;int[] running=PoseMesh.Read(arm.DrawRunning(phase,1));Quiet(Diff(running,PoseMesh.Read(arm.DrawPickup(phase,1,0,0)))==0,"pickup begins with different running arms");
    foreach(double grip in new[]{.001,.1,.25,.5,.75,.999}){arm.DrawPickup(phase,1,grip,.65*grip);Quiet(arm.MinimumArea>0,"pickup arm mesh folds");}
    int[] held=PoseMesh.Read(arm.Draw(1,.65));Quiet(Diff(held,PoseMesh.Read(arm.DrawPickup(phase,1,1,.65)))==0,"full grip still swings while pushing");
   }
   Check(true,"running arms blend into fixed grip without mesh folds / "+size+" / "+d);
  }
 }}
 static void PauseCancel(){const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;var pet=(Pet)FormatterServices.GetUninitializedObject(typeof(Pet));GC.SuppressFinalize(pet);
  Action<string,object> set=(name,value)=>typeof(Pet).GetField(name,flags).SetValue(pet,value);Func<string,object> get=name=>typeof(Pet).GetField(name,flags).GetValue(pet);
  var motion=new Motion();motion.FollowPush(10,132,1,300,true);var plan=AnimationDebugWindow.PickupPreviewPlan(1);
  using(var status=new ToolStripMenuItem()) {
   set("carryPhase","runpickup");set("carryPickup",plan);set("carryAge",.3);set("carryFacing",1);set("motion",motion);set("petHeight",300);set("rest",new RestMotion());set("carryLayout",IconPositions.Layout());set("menuOpen",true);set("carryStatus",status);set("iconStart",new Point(100,100));set("iconLast",new Point(100,100));set("iconEnd",new Point(220,100));
   double phase=motion.Phase;typeof(Pet).GetMethod("CarryDesired",flags).Invoke(pet,new object[]{.05});typeof(Pet).GetMethod("CarryAfterMove",flags).Invoke(pet,new object[]{.05});
   Check((double)get("carryAge")==.3 && motion.Phase==phase,"menu freezes new pickup sequence before shell access");
   typeof(Pet).GetMethod("CancelCarry",flags).Invoke(pet,new object[]{"test"});
   Check(get("carryPickup")==null && (string)get("carryPhase")=="" && motion.Speed==0 && motion.RunBlend==0 && ((Point)get("iconLast")).X==100,"cancel clears pickup and keeps confirmed icon position");
  }
 }
 static void Preview(){var plan=AnimationDebugWindow.PickupPreviewPlan(1);int count=(int)Math.Ceiling((plan.Duration+1.05)*30);
  using(var renderer=new PetRenderer())using(var stage=new Bitmap(900,650))using(var sg=Graphics.FromImage(stage))using(var atlas=new Bitmap(450*10,325*((count+9)/10),PixelFormat.Format32bppPArgb))using(var ag=Graphics.FromImage(atlas))using(var contact=new Bitmap(450*6,325*2))using(var cg=Graphics.FromImage(contact)) {
   renderer.Resize(300);cg.Clear(Color.FromArgb(235,231,225));
   for(int i=0;i<count;i++) {
    Point pet,icon;var f=AnimationDebugWindow.PickupPreview(i/30.0,1,out pet,out icon);sg.Clear(Color.FromArgb(235,231,225));sg.DrawImageUnscaled(renderer.Draw(f),pet);
    sg.FillRectangle(Brushes.AliceBlue,icon.X,icon.Y,32,40);sg.DrawRectangle(Pens.SteelBlue,icon.X,icon.Y,32,40);sg.DrawLine(Pens.SteelBlue,icon.X+7,icon.Y+14,icon.X+25,icon.Y+14);
    ag.DrawImage(stage,new Rectangle(i%10*450,i/10*325,450,325));
   }
   atlas.Save(Path.Combine(folder,"pickup-frames.png"),ImageFormat.Png);
   for(int j=0;j<12;j++){int i=(int)Math.Round((count-1)*j/11.0);cg.DrawImage(atlas,new Rectangle(j%6*450,j/6*325,450,325),new Rectangle(i%10*450,i/10*325,450,325),GraphicsUnit.Pixel);}contact.Save(Path.Combine(folder,"pickup-contact.png"),ImageFormat.Png);
   File.WriteAllText(Path.Combine(folder,"pickup-preview.html"),"<!doctype html><meta charset='utf-8'><title>0.6.34 连贯跑动接取</title><style>body{text-align:center;background:#ebe7e1;font:18px system-ui}canvas{height:650px;max-width:95vw}input,button,select{margin:10px}</style><p>跑着接近 → 途中伸手 → 连贯推动 · 实际人物渲染与示例图标</p><button onclick='play=!play'>播放 / 暂停</button><select id='speed'><option value='.25'>0.25 倍</option><option value='.5'>0.5 倍</option><option value='1' selected>1 倍</option></select><input id='seek' type='range' min='0' max='"+(count-1)+"' value='0'><br><canvas id='view' width='450' height='325'></canvas><script>let im=new Image(),ctx=view.getContext('2d'),play=true,f=0,last=0;im.src='pickup-frames.png';seek.oninput=()=>{play=false;f=+seek.value};function tick(t){if(play&&last)f=(f+(t-last)*.03*+speed.value)%"+count+";last=t;let i=Math.floor(f);ctx.clearRect(0,0,450,325);ctx.drawImage(im,i%10*450,Math.floor(i/10)*325,450,325,0,0,450,325);seek.value=i;requestAnimationFrame(tick)}im.onload=()=>requestAnimationFrame(tick)</script>");
  }
 }
}

using System;using System.IO;using System.Drawing;using System.Drawing.Imaging;using System.Reflection;using JouchoPet;
class CorrectionCheck {
 static int count;static readonly string Folder=Path.Combine("qa","v0643");
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static void Quiet(bool ok,string name){if(!ok)throw new Exception(name);}
 static double Diff(int[] a,int[] b){double sum=0;for(int i=0;i<a.Length;i++)for(int s=0;s<32;s+=8)sum+=Math.Abs((int)((uint)a[i]>>s&255)-(int)((uint)b[i]>>s&255));return sum/(a.Length*4);}
 [STAThread]static void Main(string[] args){try{if(args.Length>0 && args[0]=="--preview-only"){Previews();return;}AnimationParameters.Load(Path.Combine(Folder,"baseline","animation-parameters.json"));SleepRollback();Sequence();Rendering();Previews();Console.WriteLine("All "+count+" rollback/archery checks passed.");}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void SleepRollback(){
  var old=Assembly.LoadFile(Path.GetFullPath("dist/IsekaijouchoPet-v0.6.34-preview/IsekaijouchoPet.exe"));Type rt=old.GetType("JouchoPet.PetRenderer"),ft=old.GetType("JouchoPet.RenderFrame"),tuning=old.GetType("JouchoPet.AnimationTuning+Rest");
  Check(AnimationTuning.Rest.SleepRate==(double)tuning.GetField("SleepRate").GetRawConstantValue(),"sleep/wake rate exactly matches released 0.6.34");
  foreach(int size in new[]{220,300,400})using(var renderer=new PetRenderer()){
   renderer.Resize(size);object before=Activator.CreateInstance(rt,true);try{rt.GetMethod("Resize").Invoke(before,new object[]{size});
    for(int i=0;i<=65;i++){RenderFrame frame=AnimationDebugWindow.SleepPreview(i*.1);object input=Activator.CreateInstance(ft);foreach(var field in typeof(RenderFrame).GetFields()){var target=ft.GetField(field.Name);if(target!=null && (field.FieldType.IsPrimitive || field.FieldType==typeof(string)))target.SetValue(input,field.GetValue(frame));}
     int[] expected=PoseMesh.Read((Bitmap)rt.GetMethod("Draw").Invoke(before,new[]{input})),actual=PoseMesh.Read(renderer.Draw(frame));Quiet(Diff(expected,actual)==0,"sleep differs from released 0.6.34 / "+size+" / "+i);
    }
    Check(true,"66 full sleep/wake frames pixel-identical to released 0.6.34 / "+size);
   }finally{((IDisposable)before).Dispose();}
  }
 }
 static void Sequence(){foreach(bool light in new[]{false,true})foreach(int start in new[]{-1,0,1})foreach(int facing in new[]{-1,1})foreach(double speed in new[]{0.0,132}) {
  if(start==0 && speed>0)continue;var motion=new Motion{SelectLightTurn=delegate{return light;}};if(start!=0)motion.FollowPush(0,speed,start,300,speed>0);
  var shot=new ArcheryMotion();shot.Request(facing);int release=0;bool began=false,sawTurn=false;double first=-1;
  for(int i=0;i<1500;i++){double dt=1.0/120;if(!shot.Active)motion.Update(dt,shot.ApproachDirection(motion,true),300,shot.Pending);
   if(shot.Pending){Quiet(shot.Age==0 && shot.Pose.Ready==0 && shot.Pose.Bow==0,"arms/bow moved before turn completion");if(motion.State=="turn")sawTurn=true;}
   if(shot.Begin(motion,true)){began=true;first=i*dt;Quiet(motion.State!="turn" && motion.Facing==facing && motion.Angle==facing && motion.Speed==0 && motion.Settle==1,"begin before side endpoint");Quiet(shot.Age==0,"begin skipped endpoint frame");}
   else{bool active=shot.Active;if(shot.Update(dt))release++;if(active&&!shot.Active)motion.FollowPush(0,0,facing,300);}
   if(began&&!shot.Busy && motion.State=="idle")break;
  }
  Check(began && release==1 && (start==facing||sawTurn) && motion.State=="idle" && first>=0,"complete original turn before shot, then ordinary return / "+light+" / "+start+" / "+facing+" / "+speed);
 }
 var pending=new ArcheryMotion();pending.Request(1);var side=new Motion{Facing=1,State="hold"};Check(!pending.Begin(side,false) && pending.Age==0,"rest/greeting gate blocks launch");side.State="turn";Check(!pending.Begin(side,true),"target-facing field alone cannot bypass turn completion");side.State="hold";side.Speed=1;Check(!pending.Begin(side,true),"must finish deceleration before posing");
 }
 static void Rendering(){foreach(int size in new[]{220,300,400})using(var renderer=new PetRenderer()){
  renderer.Resize(size);foreach(int facing in new[]{-1,1}){
   var normal=new RenderFrame{MotionState="hold",Facing=facing,Settle=1};int[] endpoint=PoseMesh.Read(renderer.Draw(normal));
   foreach(double age in new[]{0.0,.15,ArcheryMotion.Duration}){int[] shot=PoseMesh.Read(renderer.Draw(new RenderFrame{Archery=true,BowPose=ArcheryMotion.At(age,facing)}));Check(Diff(endpoint,shot)==0,"side endpoint identical at shot start/end / "+size+" / "+facing+" / "+age);}
   double max=0;int[] last=null;for(int i=0;i<=(int)Math.Ceiling(ArcheryMotion.Duration*60);i++){int[] pixels=PoseMesh.Read(renderer.Draw(new RenderFrame{Archery=true,BowPose=ArcheryMotion.At(i/60.0,facing)}));if(last!=null)max=Math.Max(max,Diff(last,pixels));Quiet(renderer.LastArcheryJacobian>0,"arm collapsed");last=pixels;}
   Console.WriteLine("60 Hz maximum channel delta / "+size+" / "+facing+": "+max.ToString("F3"));Check(max<3,"continuous side arms through shot / "+size+" / "+facing);
   foreach(double boundary in new[]{.15,.95,1.0,1.45,1.5,2.0,2.2,2.45,2.55,2.68,3.15,3.55,3.6,3.69,3.82,4.15,4.2,5.7,5.9}){int[] a=PoseMesh.Read(renderer.Draw(new RenderFrame{Archery=true,BowPose=ArcheryMotion.At(boundary-1e-6,facing)})),b=PoseMesh.Read(renderer.Draw(new RenderFrame{Archery=true,BowPose=ArcheryMotion.At(boundary+1e-6,facing)}));Quiet(Diff(a,b)<.35,"pose boundary pop / "+boundary+" / "+Diff(a,b));}
   Check(true,"side arm stage boundaries continuous / "+size+" / "+facing);
  }
 }
 foreach(int facing in new[]{-1,1}){bool turned=false,shot=false;for(int i=0;i<780;i++){double fire;var frame=AnimationDebugWindow.BowPreview(i/100.0,facing,out fire);if(frame.MotionState=="turn" && !shot){turned=true;Quiet(!frame.Archery && frame.BowPose.Bow==0,"preview overlaps turning with bow");}if(frame.Archery){Quiet(turned && frame.Facing==facing && frame.MotionState!="turn","preview starts before turn");shot=true;}}Check(turned&&shot,"preview uses same ordered motion controller / "+facing);}
 }
 static void Previews(){using(var renderer=new PetRenderer()){renderer.Resize(300);
  using(var sleep=new Bitmap(347*6,348*2))using(var g=Graphics.FromImage(sleep)){g.Clear(Color.FromArgb(235,231,225));double[] times={0,.15,.3,.45,.7,1,1.5,2.8,3.3,3.7,4.2,6};for(int i=0;i<times.Length;i++){g.DrawImageUnscaled(renderer.Draw(AnimationDebugWindow.SleepPreview(times[i])),i%6*347,i/6*348);g.DrawString(times[i].ToString("F2"),SystemFonts.DefaultFont,Brushes.Black,i%6*347+5,i/6*348+5);}sleep.Save(Path.Combine(Folder,"sleep-restored.png"),ImageFormat.Png);}
  using(var stage=new Bitmap(900,650))foreach(int facing in new[]{-1,1})using(var atlas=new Bitmap(360*15,260*32))using(var ag=Graphics.FromImage(atlas))using(var contact=new Bitmap(450*4,325*3))using(var cg=Graphics.FromImage(contact)){
   ag.Clear(Color.FromArgb(65,72,82));cg.Clear(Color.FromArgb(65,72,82));for(int i=0;i<468;i++){using(var g=Graphics.FromImage(stage)){g.Clear(Color.FromArgb(65,72,82));try{AnimationDebugWindow.DrawBowPreview(g,renderer,i/60.0,facing);}catch(Exception ex){throw new Exception("Preview frame "+i+" / facing "+facing+" / time "+(i/60.0),ex);}}ag.DrawImage(stage,new Rectangle(i%15*360,i/15*260,360,260));}
   double[] times={0,.3,.8,1.6,2.4,3.0,3.8,4.22,4.4,5.5,6.6,7.6};for(int i=0;i<times.Length;i++){using(var g=Graphics.FromImage(stage)){g.Clear(Color.FromArgb(65,72,82));AnimationDebugWindow.DrawBowPreview(g,renderer,times[i],facing);g.DrawString(times[i].ToString("F2")+" s",SystemFonts.DefaultFont,Brushes.White,20,20);}cg.DrawImage(stage,new Rectangle(i%4*450,i/4*325,450,325));}
   string name="archery-"+(facing<0?"left":"right");atlas.Save(Path.Combine(Folder,name+"-60fps.png"),ImageFormat.Png);contact.Save(Path.Combine(Folder,name+"-contact.png"),ImageFormat.Png);
   File.WriteAllText(Path.Combine(Folder,name+".html"),"<!doctype html><meta charset='utf-8'><title>0.6.43 转身完成后射箭</title><style>body{text-align:center;background:#414852;color:white;font:18px system-ui}canvas{width:900px;max-width:100%}button,input,select{margin:12px}</style><p>完整转身 → 侧身就绪 → 抬臂凝弓 → 射箭 → 收手 → 回正</p><button onclick='play=!play'>播放 / 暂停</button><select id='speed'><option value='1'>正常速度</option><option value='.5'>半速</option><option value='.25'>四分之一速度</option></select><input id='seek' type='range' min='0' max='467'><br><canvas id='view' width='360' height='260'></canvas><script>let im=new Image(),ctx=view.getContext('2d'),play=true,f=0,last=0;im.src='"+name+"-60fps.png';seek.oninput=()=>{play=false;f=+seek.value};function tick(t){if(play&&last)f=(f+(t-last)*.06*+speed.value)%468;last=t;let i=Math.floor(f);ctx.clearRect(0,0,360,260);ctx.drawImage(im,i%15*360,Math.floor(i/15)*260,360,260,0,0,360,260);seek.value=i;requestAnimationFrame(tick)}im.onload=()=>requestAnimationFrame(tick)</script>");
  }
 }Check(true,"restored sleep and full ordered shot previews rendered");}
}

using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;
using JouchoPet;
class SleepGridCheck {
 static int count;static readonly string Folder=Path.Combine("qa","v0638");
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;Console.WriteLine("PASS "+text);}
 static void Quiet(bool ok,string text){if(!ok)throw new Exception(text);}
 static double Diff(int[] a,int[] b){double sum=0;for(int i=0;i<a.Length;i++)for(int s=0;s<32;s+=8)sum+=Math.Abs((int)((uint)a[i]>>s&255)-(int)((uint)b[i]>>s&255));return sum/(a.Length*4);}
 [STAThread] static void Main(){try{AnimationParameters.Load(Path.Combine(Folder,"baseline","user-animation-parameters.json"));Sleep();Grid();Menu();Preview();Console.WriteLine("All "+count+" sleep/grid checks passed.");}catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
 static void Sleep(){foreach(int size in new[]{220,300,400}) {
  Size c=PetRenderer.CanvasSize(size);var sprites=SpriteAssets.LoadFront();
  try{using(var front=new SoftRig(sprites,c.Width,c.Height,size)) {
   Bitmap standing=front.Draw(0,0,0,0,0,0,0,new[]{1.0,0,0});
   using(var rig=new RestRig(sprites,c.Width,c.Height,size,standing)) {
    Check(rig.DiagnosticKeys.Length==9 && Diff(rig.DiagnosticKeys[7].Pixels,rig.DiagnosticKeys[8].Pixels)==0,"v0.6.34 sleep keys and final folded-arm drawing restored / "+size);
    Check(object.ReferenceEquals(standing,rig.Draw(standing,0)),"awake returns live original / "+size);
    var watch=Stopwatch.StartNew();int[] final=null;double minimum=double.MaxValue,maxDelta=0;int[] previous=null;
    for(int i=0;i<=240;i++) {
     int[] pixels=PoseMesh.Read(rig.Draw(standing,i/240.0));if(previous!=null)maxDelta=Math.Max(maxDelta,Diff(previous,pixels));previous=pixels;final=pixels;
     Quiet(rig.LastMinimumJacobian>0,"folded sleep mesh / "+size+" / "+i);if(i>0)minimum=Math.Min(minimum,rig.LastMinimumJacobian);
     foreach(int value in pixels){uint p=(uint)value;Quiet((p>>16&255)<=(p>>24) && (p>>8&255)<=(p>>24) && (p&255)<=(p>>24),"invalid premultiplied alpha");}
    }
    watch.Stop();Console.WriteLine("Dense sweep "+size+": min area="+minimum.ToString("F4")+", max channel delta="+maxDelta.ToString("F3")+", with pixel checks ms/frame="+(watch.Elapsed.TotalMilliseconds/241).ToString("F2"));
    Check(maxDelta<3,"no hard texture switch in 241 samples / "+size);
    foreach(double boundary in new[]{0.0,.25,.5,.75,1.0}) {
     int[] a=PoseMesh.Read(rig.Draw(standing,Math.Max(0,boundary-1e-6))),b=PoseMesh.Read(rig.Draw(standing,Math.Min(1,boundary+1e-6)));
     Check(Diff(a,b)<.3,"continuous pose boundary / "+size+" / "+boundary);
    }
    foreach(double p in new[]{.1,.33,.49,.65,.9}){int[] a=PoseMesh.Read(rig.Draw(standing,p));rig.Draw(standing,.99);Quiet(Diff(a,PoseMesh.Read(rig.Draw(standing,p)))==0,"reverse seek changes pose");}
    Check(Diff(final,PoseMesh.Read(rig.Draw(standing,1)))==0,"settled pose stable after interruption / "+size);
   }
  }}finally{foreach(var b in sprites)b.Dispose();}
 }
 foreach(int fps in new[]{30,60,120}){var r=new RestMotion();r.RequestSleep(true);for(int i=0;i<fps;i++)r.Update(1.0/fps,true);double rate=AnimationTuning.Rest.SleepRate,expected=1-(1+rate)*Math.Exp(-rate);Check(Math.Abs(r.Sleep-expected)<1e-10,"time-based sleep at "+fps+" Hz");double before=r.Sleep;r.RequestSleep(false);Check(r.Sleep==before,"wake command retains pose / "+fps);for(int i=0;i<fps*5;i++)r.Update(1.0/fps,true);Check(!r.BlocksWalking,"interrupted sleep recovers / "+fps);}
 }
 static void Grid(){foreach(int spacing in new[]{72,96,140,180})foreach(int cells in new[]{1,2,3,4,5,6})foreach(int direction in new[]{-1,1}) {
  int distance=IconCarryPlan.PushDistance(spacing,false,true,cells);Quiet(distance==spacing*cells && distance==IconCarryPlan.PushDistance(spacing,true,true,cells),"cell semantics depend on snap");
  Point start=new Point(2000,500),end=new Point(start.X+direction*distance,start.Y);var plan=new RunningPickup(new Point(1000,300),new Point(1000+direction*160,320),start,end,300,70);
  Point last=start;int moves=0;double previous=0;
  for(int i=0;i<=Math.Ceiling(plan.Duration*60);i++){var s=plan.At(Math.Min(i/60.0,plan.Duration));Quiet(s.Distance>=previous,"run rewinds");if(last!=s.Icon){moves++;Quiet(IconCarryPlan.Distance(last,s.Icon)<=Math.Ceiling(RunGait.SpeedAt300/60)+1,"multi-cell jump");}last=s.Icon;previous=s.Distance;}
  Check(last==end && moves>15,"continuous exact-cell route / "+spacing+" / "+cells+" / "+direction);
  var obstacle=new List<DesktopIcons.Item>{new DesktopIcons.Item{Id="other",Position=new Point(start.X+direction*distance/2,500)}};
  Quiet(!IconCarryPlan.Clear(start,end,"target",obstacle,new Point(spacing,100)),"long path misses middle obstruction");
 }
 foreach(int invalid in new[]{0,7,-1}){bool rejected=false;try{IconCarryPlan.PushDistance(96,true,true,invalid);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"invalid grid count rejected / "+invalid);}
 Check(IconCarryPlan.PushDistance(180,false,false,6)==140,"walking distance unchanged");
 }
 static void Menu(){const BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;var pet=(Pet)FormatterServices.GetUninitializedObject(typeof(Pet));GC.SuppressFinalize(pet);Action<string,object> set=(n,v)=>typeof(Pet).GetField(n,flags).SetValue(pet,v);Func<string,object> get=n=>typeof(Pet).GetField(n,flags).GetValue(pet);
  using(var menu=new ContextMenuStrip()){set("menu",menu);set("runPushCells",1);set("carryPhase","");set("carryEnabled",true);set("leap",new LeapMotion());typeof(Pet).GetMethod("AddCarryMenu",flags).Invoke(pet,null);
   ToolStripMenuItem cells=null,run=null;foreach(ToolStripItem item in menu.Items){if(item.Text.StartsWith("跑动推动距离"))cells=(ToolStripMenuItem)item;if(item.Text=="按所选格数小跑推动")run=(ToolStripMenuItem)item;}
   Check(cells!=null && run!=null && cells.DropDownItems.Count==6,"grid selector and explicit running commands present");
   ((ToolStripMenuItem)cells.DropDownItems[3]).PerformClick();int checkedItems=0;foreach(ToolStripMenuItem item in cells.DropDownItems)if(item.Checked)checkedItems++;
   Check((int)get("runPushCells")==4 && checkedItems==1 && cells.Text.Contains("4 格"),"menu selects exactly four cells");
   ((ToolStripMenuItem)run.DropDownItems[0]).PerformClick();Check((bool)get("requestedRun") && ((Point)get("requestedPush")).X==-1,"explicit left command forces running");
   var snapshot=new RunningPickup(Point.Empty,new Point(160,0),Point.Empty,new Point(384,0),300,0);((ToolStripMenuItem)cells.DropDownItems[1]).PerformClick();Check(snapshot.At(snapshot.Duration).Icon.X==384 && (int)get("runPushCells")==2,"changing selection leaves active route endpoint unchanged");
   typeof(Pet).GetMethod("CancelCarry",flags).Invoke(pet,new object[]{"test"});Check(!(bool)get("requestedRun") && (Point)get("requestedPush")==Point.Empty,"cancel clears pending forced request");
  }
 }
 static void Preview(){const int frames=390,tw=174,th=174;using(var renderer=new PetRenderer())using(var atlas=new Bitmap(tw*15,th*26))using(var ag=Graphics.FromImage(atlas))using(var contact=new Bitmap(347*6,348*2))using(var cg=Graphics.FromImage(contact)){
  renderer.Resize(300);ag.Clear(Color.FromArgb(235,231,225));cg.Clear(Color.FromArgb(235,231,225));
  double sum=0,max=0;for(int i=0;i<frames;i++){RenderFrame frame=AnimationDebugWindow.SleepPreview(i/60.0);var clock=Stopwatch.StartNew();Bitmap b=renderer.Draw(frame);clock.Stop();sum+=clock.Elapsed.TotalMilliseconds;max=Math.Max(max,clock.Elapsed.TotalMilliseconds);ag.DrawImage(b,new Rectangle(i%15*tw,i/15*th,tw,th));}
  Console.WriteLine("Full sleep/wake renderer 300px mean="+(sum/frames).ToString("F2")+" ms; max="+max.ToString("F2")+" ms (offscreen, no desktop FPS claim)");
  double[] times={0,.15,.3,.45,.6,.9,1.4,2.5,3.3,3.6,4,5.5};for(int i=0;i<times.Length;i++){cg.DrawImageUnscaled(renderer.Draw(AnimationDebugWindow.SleepPreview(times[i])),i%6*347,i/6*348);cg.DrawString(times[i].ToString("F2")+" s",SystemFonts.DefaultFont,Brushes.Black,i%6*347+6,i/6*348+6);}
  atlas.Save(Path.Combine(Folder,"sleep-60fps.png"),ImageFormat.Png);contact.Save(Path.Combine(Folder,"sleep-actual-size.png"),ImageFormat.Png);
  File.WriteAllText(Path.Combine(Folder,"sleep-preview.html"),"<!doctype html><meta charset='utf-8'><title>0.6.37 · 恢复 0.6.34 睡眠</title><style>body{text-align:center;background:#ebe7e1;font:18px system-ui}canvas{width:347px;height:348px}button,input,select{margin:12px}</style><p>实际人物渲染 · 0.6.34 入睡 → 休息 → 起身 · 60 帧采样</p><button onclick='play=!play'>播放 / 暂停</button><select id='speed'><option value='1'>正常速度</option><option value='.5'>半速</option><option value='.25'>四分之一速度</option></select><input id='seek' type='range' min='0' max='389' value='0'><br><canvas id='view' width='174' height='174'></canvas><script>let im=new Image(),ctx=view.getContext('2d'),play=true,f=0,last=0;im.src='sleep-60fps.png';seek.oninput=()=>{play=false;f=+seek.value};function tick(t){if(play&&last)f=(f+(t-last)*.06*+speed.value)%390;last=t;let i=Math.floor(f);ctx.clearRect(0,0,174,174);ctx.drawImage(im,i%15*174,Math.floor(i/15)*174,174,174,0,0,174,174);seek.value=i;requestAnimationFrame(tick)}im.onload=()=>requestAnimationFrame(tick)</script>");
 }}
}

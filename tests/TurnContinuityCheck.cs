using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Linq;
using JouchoPet;
class TurnContinuityCheck {
 const string Folder="qa/v0816/";const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static int checks;
 static void Check(bool pass,string label){if(!pass)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
 static object Model(object renderer){var turn=renderer.GetType().GetField("turn",F).GetValue(renderer);return turn.GetType().GetField("model",F).GetValue(turn);}
 static Bitmap DrawOld(object renderer,RenderFrame frame){var a=renderer.GetType().Assembly;var converted=Activator.CreateInstance(a.GetType("JouchoPet.RenderFrame"));foreach(var field in typeof(RenderFrame).GetFields())if(field.FieldType.IsPrimitive||field.FieldType==typeof(string))converted.GetType().GetField(field.Name).SetValue(converted,field.GetValue(frame));return (Bitmap)renderer.GetType().GetMethod("Draw").Invoke(renderer,new[]{converted});}
 static PointF[] Targets(object model,double angle){model.GetType().GetMethod("Draw").Invoke(model,new object[]{angle,-1.0,0.0,false});return (PointF[])((PointF[])model.GetType().GetField("target",F).GetValue(model)).Clone();}
 static double VelocityBreak(object model){double worst=0;const double step=.001;foreach(double key in new[]{-.5,0,.5}){var before=Targets(model,key-step);var at=Targets(model,key);var after=Targets(model,key+step);for(int i=0;i<9*13;i++)worst=Math.Max(worst,Math.Abs((after[i].Y-at[i].Y)-(at[i].Y-before[i].Y))/step);}return worst;}
 static double MeshJump(object model){
  PointF[] previous=null,older=null;double worst=0;
  for(int i=0;i<=400;i++){
   var current=Targets(model,-1+i*.005);
   if(older!=null)for(int v=0;v<current.Length;v++){double x=current[v].X-2*previous[v].X+older[v].X,y=current[v].Y-2*previous[v].Y+older[v].Y;worst=Math.Max(worst,Math.Sqrt(x*x+y*y));}
   older=previous;previous=current;
  }return worst;
 }
 static bool HeadEqual(Bitmap a,Bitmap b,int size){return PoseMesh.Read(a).Take((int)(28+size*.42)*a.Width).SequenceEqual(PoseMesh.Read(b).Take((int)(28+size*.42)*b.Width));}
 static void Verify(){
  for(int i=0;i<=1000;i++){double f,o,s;SkeletalTurn.RightWeights(i/1000.0,out f,out o,out s);if(f<0||o<-.00000001||s<0||Math.Abs(f+o+s-1)>.00000001)throw new Exception("right material weights out of range");}
  Check(true,"right-view weights nonnegative and normalized across 1001 angles");
  foreach(double key in new[]{0,.12,.42,.5,.58,.88,1}){
   const double h=.0001;double f0,o0,s0,f1,o1,s1,f2,o2,s2;
   SkeletalTurn.RightWeights(key-h,out f0,out o0,out s0);SkeletalTurn.RightWeights(key,out f1,out o1,out s1);SkeletalTurn.RightWeights(key+h,out f2,out o2,out s2);
   Check(Math.Abs((f2-f1)-(f1-f0))/h<.02&&Math.Abs((s2-s1)-(s1-s0))/h<.02,"continuous material velocity at "+key);
   if(key==.5)Check(Math.Abs(f2-f0)/(2*h)>.9&&Math.Abs(s2-s0)/(2*h)>.9,"material transition does not stop at right oblique key");
  }
  var baseline=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));
  foreach(int size in new[]{220,300,400}){
   object old=Activator.CreateInstance(baseline.GetType("JouchoPet.PetRenderer"),F,null,new[]{Enum.ToObject(baseline.GetType("JouchoPet.CharacterForm"),1)},null);
   try{old.GetType().GetMethod("Resize").Invoke(old,new object[]{size});using(var now=new PetRenderer(CharacterForm.AnemoneII)){now.Resize(size);var model=Model(now);double before=VelocityBreak(Model(old)),after=VelocityBreak(model);
    Check(after<.6&&after<=before+.001,"previous head continuity retained / "+size+" / before="+before.ToString("F3")+" after="+after.ToString("F3"));
    double oldJump=MeshJump(Model(old)),newJump=MeshJump(model);Check(newJump<.3,"smooth mesh trajectory after coat silhouette registration / "+size+" / before="+oldJump.ToString("F4")+" after="+newJump.ToString("F4"));
    foreach(double angle in new[]{-1.0,0,1}){var frame=new RenderFrame{MotionState="turn",Angle=angle,Settle=1};Check(HeadEqual(DrawOld(old,frame),now.Draw(frame),size),"approved head at each endpoint identical / "+size+" / "+angle);}
    foreach(bool light in new[]{false,true})foreach(int from in new[]{-1,0,1})foreach(int to in new[]{-1,0,1})if(from!=to){double minimum=Double.MaxValue;
     for(int i=0;i<=32;i++){double p=i/32.0,angle=from+(to-from)*(light?PivotTurn.Progress(p):LeapMotion.Ease(p));var f=new RenderFrame{MotionState="turn",Angle=angle,Settle=1,LightTurn=light,TurnProgress=p,TurnFrom=from,SupportNear=to<from};var a=PoseMesh.Read(DrawOld(old,f));var b=PoseMesh.Read(now.Draw(f));int w=PetRenderer.CanvasSize(size).Width;
      for(int k=0;k<Math.Min(a.Length,(int)(28+size*.42)*w);k++)if(a[k]!=b[k])throw new Exception("turn head pixels changed / "+size+" / "+light+" / "+from+" / "+to+" / "+p);
      double area=(double)typeof(TurnRig).GetField("LastMinimumJacobian").GetValue(typeof(PetRenderer).GetField("turn",F).GetValue(now));minimum=Math.Min(minimum,area);
      for(int k=0;k<b.Length;k++)if(((uint)b[k]>>24)>80&&(k%w==0||k%w==w-1||k/w==0||k/w==b.Length/w-1))throw new Exception("turn artwork clipped");
     }Check(minimum>0,"head pixels identical, positive mesh, no clipping / "+size+" / "+light+" / "+from+" -> "+to);
    }
    Check(((SkeletalTurn)model).MaterialCount==5,"still five shared-skeleton materials / "+size);
   }}finally{((IDisposable)old).Dispose();}
  }
  Console.WriteLine("PASS "+checks+" turn continuity checks.");
 }
 static void Preview(){
  Directory.CreateDirectory(Folder+"turn-frames");var baseline=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));object old=Activator.CreateInstance(baseline.GetType("JouchoPet.PetRenderer"),F,null,new[]{Enum.ToObject(baseline.GetType("JouchoPet.CharacterForm"),1)},null);
  try{old.GetType().GetMethod("Resize").Invoke(old,new object[]{300});using(var a1=new PetRenderer())using(var a2=new PetRenderer(CharacterForm.AnemoneII)){a1.Resize(300);a2.Resize(300);
   foreach(bool front in new[]{false,true})foreach(bool light in new[]{false,true})foreach(int direction in new[]{-1,1})for(int i=0;i<48;i++){
    double p=i/47.0,from=front?0:-direction;var frame=new RenderFrame{MotionState="turn",Angle=from+(direction-from)*(light?PivotTurn.Progress(p):LeapMotion.Ease(p)),LightTurn=light,TurnProgress=p,TurnFrom=from,SupportNear=direction<0,Settle=1};
    using(var image=new Bitmap(960,400))using(var g=Graphics.FromImage(image)){g.Clear(Color.FromArgb(46,51,61));int col=0;foreach(var b in new[]{a1.Draw(frame),DrawOld(old,frame),a2.Draw(frame)}){
     g.DrawString(col==0?"A1":col==1?"A2 / 0.8.15":"A2 / 0.8.16",SystemFonts.DefaultFont,Brushes.White,col*320+12,12);g.DrawImageUnscaled(b,col*320+160-b.Width/2,45);col++;
    }image.Save(Folder+"turn-frames/"+(front?"front-":"")+(light?"pivot":"classic")+(direction<0?"-left-":"-right-")+i.ToString("00")+".png",ImageFormat.Png);}
   }
  }}finally{((IDisposable)old).Dispose();}
 }
 static double Diff(Bitmap a,Bitmap b,int y0,int y1){var x=PoseMesh.Read(a);var y=PoseMesh.Read(b);double sum=0;for(int i=y0*a.Width;i<Math.Min(x.Length,y1*a.Width);i++)for(int c=0;c<4;c++)sum+=Math.Abs(((uint)x[i]>>(c*8)&255)- (double)((uint)y[i]>>(c*8)&255));return sum/(a.Width*(y1-y0)*4);}
 static void Timing(){
  var baseline=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));
  foreach(int size in new[]{220,300,400}){
   object old=Activator.CreateInstance(baseline.GetType("JouchoPet.PetRenderer"),F,null,new[]{Enum.ToObject(baseline.GetType("JouchoPet.CharacterForm"),1)},null);
   try{old.GetType().GetMethod("Resize").Invoke(old,new object[]{size});using(var now=new PetRenderer(CharacterForm.AnemoneII))using(var a1=new PetRenderer()){
    now.Resize(size);a1.Resize(size);var times=new[]{new System.Collections.Generic.List<double>(),new System.Collections.Generic.List<double>(),new System.Collections.Generic.List<double>()};
    for(int pass=0;pass<4;pass++)for(int i=1;i<48;i++){
     var frame=new RenderFrame{MotionState="turn",Angle=LeapMotion.Ease(i/48.0),Settle=1};var clock=Stopwatch.StartNew();a1.Draw(frame);double a=clock.Elapsed.TotalMilliseconds;clock.Restart();DrawOld(old,frame);double b=clock.Elapsed.TotalMilliseconds;clock.Restart();now.Draw(frame);double c=clock.Elapsed.TotalMilliseconds;
     if(pass>0){times[0].Add(a);times[1].Add(b);times[2].Add(c);}
    }
    for(int k=0;k<3;k++){var sorted=times[k].OrderBy(t=>t).ToArray();Console.WriteLine(size+"px "+new[]{"A1","A2 baseline","A2 revised"}[k]+" mean="+sorted.Average().ToString("F3")+"ms p95="+sorted[(int)(sorted.Length*.95)].ToString("F3")+"ms >16.67ms="+sorted.Count(t=>t>16.67)+"/"+sorted.Length);}
   }}finally{((IDisposable)old).Dispose();}
  }
  Console.WriteLine("Interleaved warmed offline Draw timings, not desktop presentation FPS.");
 }
 static void Curves(){
  foreach(var form in new[]{CharacterForm.AnemoneI,CharacterForm.AnemoneII})using(var renderer=new PetRenderer(form)){
   renderer.Resize(300);Console.WriteLine(form+" angle flower eyeNear eyeFar mouth neck hairLeft hairRight");
   for(int i=0;i<=20;i++){double angle=i/20.0;var p=Targets(Model(renderer),angle);Console.WriteLine(angle.ToString("F2")+" "+String.Join(" ",new[]{3*13+4,6*13+4,6*13+8,7*13+6,8*13+6,5*13+1,5*13+11}.Select(v=>p[v].X.ToString("F2"))));}
  }
 }
 static void Handoff(){
  var baseline=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));
  foreach(int size in new[]{220,300,400}){
   object old=Activator.CreateInstance(baseline.GetType("JouchoPet.PetRenderer"),F,null,new[]{Enum.ToObject(baseline.GetType("JouchoPet.CharacterForm"),1)},null);
   try{old.GetType().GetMethod("Resize").Invoke(old,new object[]{size});using(var now=new PetRenderer(CharacterForm.AnemoneII)){
    now.Resize(size);
    foreach(bool light in new[]{false,true})foreach(double progress in new[]{0,.5,1})foreach(double angle in new[]{-1.0,-1e-8,0,1e-8,.999999e-7,1e-7,1.00001e-7,.5,.9999999,1}){
     var frame=new RenderFrame{MotionState="turn",Angle=angle,LightTurn=light,TurnProgress=progress,TurnFrom=0,Settle=1};
     if(!PoseMesh.Read(DrawOld(old,frame)).SequenceEqual(PoseMesh.Read(now.Draw(frame))))throw new Exception("front tolerance/turn handoff changed / "+size+" / "+angle);
    }Check(true,"all 60 endpoint/tolerance/light-turn combinations identical / "+size);
    foreach(double closure in new[]{0,.5,1}){
     var frame=new RenderFrame{MotionState="idle",Time=.7,LookX=.1,ClosedEyes=closure,Affection=.8,PatSide=-1};
     if(!PoseMesh.Read(DrawOld(old,frame)).SequenceEqual(PoseMesh.Read(now.Draw(frame))))throw new Exception("front interaction after right turn changed");
    }Check(true,"idle/head-pet redraw after right turn identical / "+size);
   }}finally{((IDisposable)old).Dispose();}
  }Console.WriteLine("PASS "+checks+" handoff checks.");
 }
 static void Scan(){
  foreach(var form in new[]{CharacterForm.AnemoneI,CharacterForm.AnemoneII})using(var renderer=new PetRenderer(form)){
   renderer.Resize(300);var model=Model(renderer);var lines=new System.Collections.Generic.List<string>();lines.Add("angle,meshAcceleration,vertex,headDelta,bodyDelta,ms");PointF[] prev=null,prev2=null;Bitmap prior=null;
   var skeleton=(CharacterSkeleton)model.GetType().GetField("skeleton",F).GetValue(model);var vertices=(CharacterSkeleton.Vertex[])model.GetType().GetField("vertices",F).GetValue(model);var correction=(PointF[][])model.GetType().GetField("correction",F).GetValue(model);
   foreach(double angle in new[]{-.69,-.685,-.68,-.565,-.56,-.555,.25,.5,.75}){
    int a=Math.Min(3,(int)((angle+1)*2)),b=a+1;double t=(angle+1)*2-a;var actual=Targets(model,angle);double[] raw=new double[vertices.Length];
    for(int v=0;v<vertices.Length;v++){var p=skeleton.Skin(vertices[v],angle*Math.PI*.5,PetRenderer.CanvasSize(300).Width,300);raw[v]=p.X+PoseMesh.Coordinate(correction[Math.Max(0,a-1)][v].X,correction[a][v].X,correction[b][v].X,correction[Math.Min(4,b+1)][v].X,t);}
    for(int r=0;r<21;r++)for(int c=1;c<13;c++)if(raw[r*13+c]-raw[r*13+c-1]<=.1){Console.WriteLine(form+" fallback at "+angle+" row "+r+" col "+c+" gap "+(raw[r*13+c]-raw[r*13+c-1]).ToString("F3")+" outline displacement "+(actual[r*13+1].X-raw[r*13+1]).ToString("F3")+" raw="+String.Join("/",raw.Skip(r*13).Take(5).Select(v=>v.ToString("F2"))));break;}
   }
   for(int i=0;i<=400;i++){
    double angle=-1+i*.005;var timer=Stopwatch.StartNew();var points=Targets(model,angle);double max=0;int at=0;
    if(prev2!=null)for(int v=0;v<points.Length;v++){double x=points[v].X-2*prev[v].X+prev2[v].X,y=points[v].Y-2*prev[v].Y+prev2[v].Y,d=Math.Sqrt(x*x+y*y);if(d>max){max=d;at=v;}}
    using(var frame=(Bitmap)renderer.Draw(new RenderFrame{MotionState="turn",Angle=angle,Settle=1}).Clone()){
     lines.Add(angle.ToString("F4")+","+max.ToString("F6")+","+at+","+(prior==null?0:Diff(prior,frame,25,125)).ToString("F5")+","+(prior==null?0:Diff(prior,frame,125,330)).ToString("F5")+","+timer.ElapsedMilliseconds);
     if(prior!=null)prior.Dispose();prior=(Bitmap)frame.Clone();
    }prev2=prev;prev=points;
   }if(prior!=null)prior.Dispose();File.WriteAllLines(Folder+"scan-"+form+".csv",lines);
   Console.WriteLine(form);foreach(var line in lines.Skip(1).OrderByDescending(s=>Double.Parse(s.Split(',')[1])).Take(12))Console.WriteLine(line);
  }
 }
 [STAThread]static void Main(string[] args){try{
  Directory.CreateDirectory(Folder);AnimationParameters.UseFixedValues();string tag=args.Length>0?args[0]:"before";
  if(tag=="verify"){Verify();return;}if(tag=="preview"){Preview();return;}if(tag=="scan"){Scan();return;}if(tag=="timing"){Timing();return;}if(tag=="curves"){Curves();return;}if(tag=="handoff"){Handoff();return;}
  using(var sheet=new Bitmap(1500,640))using(var g=Graphics.FromImage(sheet)){
   g.Clear(Color.FromArgb(46,51,61));int row=0;
   foreach(var form in new[]{CharacterForm.AnemoneI,CharacterForm.AnemoneII})using(var renderer=new PetRenderer(form)){
    renderer.Resize(300);var turn=typeof(PetRenderer).GetField("turn",F).GetValue(renderer);var model=turn.GetType().GetField("model",F).GetValue(turn);var textures=(int[][])model.GetType().GetField("textures",F).GetValue(model);
    var uv=(PointF[][])model.GetType().GetField("uv",F).GetValue(model);
    using(var materials=new Bitmap(1200,430))using(var mg=Graphics.FromImage(materials)){
     mg.Clear(Color.FromArgb(232,229,222));for(int k=0;k<5;k++)using(var image=new Bitmap(PetRenderer.CanvasSize(300).Width,PetRenderer.CanvasSize(300).Height,PixelFormat.Format32bppPArgb)){
      var bits=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);System.Runtime.InteropServices.Marshal.Copy(textures[k],0,bits.Scan0,textures[k].Length);image.UnlockBits(bits);
      mg.DrawString(form+" / "+k,SystemFonts.DefaultFont,Brushes.Black,k*240+10,10);mg.DrawImage(image,new Rectangle(k*240+10,40,220,193),new Rectangle(image.Width/2-65,23,130,114),GraphicsUnit.Pixel);mg.DrawImage(image,new Rectangle(k*240+50,235,140,180));
      if(tag=="landmarks")foreach(int band in new[]{3,6,8})foreach(int col in new[]{4,8}){var p=uv[k][band*13+col];float px=k*240+10+(p.X-(image.Width/2-65))*220/130,py=40+(p.Y-23)*193/114;mg.FillEllipse(band==3?Brushes.Red:band==6?Brushes.Blue:Brushes.Green,px-2,py-2,4,4);}
     }materials.Save(Folder+tag+"-"+form+"-materials.png",ImageFormat.Png);
    }
    Bitmap previous=null;double total=0,max=0,at=0;var timer=Stopwatch.StartNew();
    for(int i=0;i<=200;i++){
     double angle=-1+i*.01;using(var frame=(Bitmap)renderer.Draw(new RenderFrame{MotionState="turn",Angle=angle,Settle=1,Time=0}).Clone()){
      if(previous!=null){double delta=Diff(previous,frame,25,125);if(delta>max){max=delta;at=angle;}total+=delta;previous.Dispose();}previous=(Bitmap)frame.Clone();
      if(i%20==0&&i<=180){int col=i/20;g.DrawString(form+" "+angle.ToString("F2"),SystemFonts.DefaultFont,Brushes.White,col*150+5,row*320+8);g.DrawImage(frame,new Rectangle(col*150,row*320+30,150,130),new Rectangle(frame.Width/2-60,27,120,104),GraphicsUnit.Pixel);g.DrawImage(frame,new Rectangle(col*150+15,row*320+165,120,150));}
     }
    }
    if(previous!=null)previous.Dispose();Console.WriteLine(form+" mean head delta="+(total/200).ToString("F4")+" max="+max.ToString("F4")+" at="+at+" elapsed(ms)="+timer.ElapsedMilliseconds);row++;
   }sheet.Save(Folder+tag+"-turn-strip.png",ImageFormat.Png);
  }
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}

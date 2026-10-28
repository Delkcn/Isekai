using System;using System.IO;using System.Linq;using System.Reflection;using System.Drawing;using System.Drawing.Imaging;using System.Windows.Forms;using JouchoPet;
class DailyCheck {
 const string Folder="qa/v0728/";
 static readonly BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static int checks;
 static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 [STAThread] static void Main(string[] args){try{AnimationParameters.UseFixedValues();Directory.CreateDirectory(Folder);if(args.Length>0&&args[0]=="live"){Live();return;}if(args.Length>0&&args[0]=="preview"){Preview();Entries();return;}Timelines();Bones();Faces();Revision();Regression();Integration();Console.WriteLine("All "+checks+" assertions passed.");}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void Timelines(){
  foreach(int fps in new[]{30,60,120})foreach(DailyAction kind in new[]{DailyAction.Wink,DailyAction.Thinking,DailyAction.Hair}){
   var m=new DailyMotion();Check(m.Request(kind)&&!m.Request(kind),kind+" accepts one request / "+fps);
   m.Update(.1,false);Check(m.Pending&&m.Age==0,"waits for front Idle");m.Update(1.0/fps,true);
   for(int k=0;k<fps*4;k++)m.Update(1.0/fps,true);Check(!m.Busy&&m.Pose.Lift==0&&m.Pose.Wink==0,"finite one-shot returns neutral");
   foreach(double age in new[]{.03,.15,.35,.6,1.1}){m.Request(kind);m.Update(.01,true);m.Update(age,true);var before=m.Pose;m.Cancel();Check(Math.Abs(before.Lift-m.Pose.Lift)<1e-9&&Math.Abs(before.Wink-m.Pose.Wink)<1e-9,"cancel preserves initial pose");for(int i=0;i<fps;i++)m.Update(1.0/fps,false);Check(!m.Busy,"cancel ends without stale state");}
  }
  var thinking=new DailyMotion();thinking.Request(DailyAction.Thinking,5);thinking.Update(.01,true);thinking.Update(4,true);Check(thinking.Active&&thinking.Pose.Think==1,"caller controls thought hold");thinking.Cancel();thinking.Update(.3,true);Check(!thinking.Busy,"long thought cancels");
  foreach(int size in new[]{220,300,400})foreach(DailyAction kind in new[]{DailyAction.Wink,DailyAction.Thinking,DailyAction.Hair})foreach(int facing in new[]{-1,1}){var motion=new Motion();motion.FollowPush(0,0,facing,size);var action=new DailyMotion();action.Request(kind);bool began=false;for(int k=0;k<240;k++){motion.Update(1.0/60,0,size);action.Update(1.0/60,motion.State=="idle"&&motion.Facing==0);began|=action.Active;}Check(began&&!action.Busy&&motion.Facing==0,"existing turn / "+kind+" / "+size+" / "+facing);}
 }
 static void Bones(){foreach(int size in new[]{220,300,400}){var c=PetRenderer.CanvasSize(size);using(var art=CharacterArt.Normalized("front",c.Width,c.Height,size))using(var rig=new GreetingArmRig(PoseMesh.Read(art),c.Width,c.Height,size)){
  var rest=rig.HairSample(0,0,p=>p,0);bool lengths=true,continuous=true,contact=true;var previous=rest;
  for(int k=0;k<=350;k++){double t=k*.005;var d=DailyMotion.At(DailyAction.Hair,t);var p=rig.HairSample(d.Lift,d.Stroke,x=>x,0);lengths&=Math.Abs(Math.Sqrt(Math.Pow(RigParts.Length(p.Shoulder,p.Elbow),2)+p.UpperDepth*p.UpperDepth)-RigParts.Length(rest.Shoulder,rest.Elbow))<.001&&Math.Abs(RigParts.Length(p.Elbow,p.Wrist)-RigParts.Length(rest.Elbow,rest.Wrist))<.001;if(RigParts.Length(previous.Tip,p.Tip)>=size*.04)Console.WriteLine("Tip step "+t+": "+RigParts.Length(previous.Tip,p.Tip));continuous&=RigParts.Length(previous.Tip,p.Tip)<size*.04;if(t>=.90&&t<=1.35)contact&=RigParts.Length(p.Tip,rig.HairContact(d.Stroke))<.01;previous=p;}
  Console.WriteLine("Hair geometry / "+size+": lengths="+lengths+", continuous="+continuous+", contact="+contact);
  Check(lengths&&continuous&&contact,"fixed arm lengths, continuous tip and bang contact / "+size);
 }}}
 static void Faces(){foreach(int size in new[]{220,300,400})using(var r=new PetRenderer()){
  r.Resize(size);var f=new RenderFrame{MotionState="idle",Time=.7};var a=PoseMesh.Read(r.Draw(f));f.Daily=DailyMotion.At(DailyAction.Wink,.64);var b=PoseMesh.Read(r.Draw(f));int changed=0;double minX=1,maxX=-1,minY=1,maxY=-1;bool local=true;Size c=PetRenderer.CanvasSize(size);
  for(int k=0;k<a.Length;k++)if(a[k]!=b[k]){changed++;double x=(k%c.Width-c.Width*.5)/size,y=(k/c.Width-28)/(double)size;minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);local&=x>0&&x<.15&&y>.12&&y<.29;}
  Console.WriteLine("Wink bounds "+size+": "+minX+","+maxX+","+minY+","+maxY+" / changed="+changed);Check(changed>3&&local,"wink changes only anatomical left eye; right eye/hair untouched / "+size);
  f.Daily=new DailyPose();Check(a.SequenceEqual(PoseMesh.Read(r.Draw(f))),"wink restores exact neutral pixels / "+size);
  f.Daily=DailyMotion.At(DailyAction.Thinking,.8);var t=PoseMesh.Read(r.Draw(f));f.Time=1.4;Check(!t.SequenceEqual(PoseMesh.Read(r.Draw(f))),"thought retains time-driven breathing / "+size);f.Time=.7;f.Blink=1;Check(!t.SequenceEqual(PoseMesh.Read(r.Draw(f))),"thought retains natural blinking / "+size);
 }}
 static void Revision(){
  var previousAssembly=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));var previousMotion=previousAssembly.GetType("JouchoPet.DailyMotion");var previousKind=previousAssembly.GetType("JouchoPet.DailyAction");var sample=previousMotion.GetMethod("At",Flags);
  Func<string,double,string,double> oldValue=(kind,t,field)=>{object pose=sample.Invoke(null,new[]{Enum.Parse(previousKind,kind),(object)t,1.5});return (double)pose.GetType().GetField(field,Flags).GetValue(pose);};
  bool halfSpeed=true,halfLift=true;for(int k=0;k<=148;k++){double t=k*.01;halfSpeed&=Math.Abs(DailyMotion.At(DailyAction.Wink,t).Wink-oldValue("Wink",t*.5,"Wink"))<1e-9;}
  for(int k=0;k<=70;k++){double elapsed=k*.01;halfLift&=Math.Abs(DailyMotion.At(DailyAction.Hair,.2+elapsed).Lift-oldValue("Hair",.2+elapsed*.5,"Lift"))<1e-9;}
  Check(halfSpeed&&DailyMotion.Duration(DailyAction.Wink)==1.48,"Wink at exactly 50 percent of v0.7.27 speed: 1.48 s");
  Check(halfLift&&DailyMotion.Duration(DailyAction.Hair)==1.75,"hair raise at exactly 50 percent speed; brush and lower durations unchanged");
  foreach(int size in new[]{220,300,400}){
   Size canvas=PetRenderer.CanvasSize(size);PointF pivot=new PointF(canvas.Width*.5f,(float)(28+size*.30)),a=new PointF(pivot.X-(float)(size*.1),pivot.Y-(float)(size*.1)),b=new PointF(pivot.X+(float)(size*.1),pivot.Y-(float)(size*.1)),c=new PointF(a.X,a.Y-(float)(size*.1));
   bool rigid=true;foreach(double angle in new[]{-.14,-.07,.07,.14}){PointF ra=SoftRig.DailyHeadSource(a,canvas.Width,size,angle),rb=SoftRig.DailyHeadSource(b,canvas.Width,size,angle),rc=SoftRig.DailyHeadSource(c,canvas.Width,size,angle);rigid&=Math.Abs(RigParts.Length(a,b)-RigParts.Length(ra,rb))<.001&&Math.Abs(RigParts.Length(a,c)-RigParts.Length(ra,rc))<.001&&Math.Abs(RigParts.Length(a,pivot)-RigParts.Length(ra,pivot))<.001&&RigParts.Length(pivot,SoftRig.DailyHeadSource(pivot,canvas.Width,size,angle))<.001;}
   Check(rigid,"rigid head rotation preserves face distances and neck pivot / "+size);
  }
  var oldAssembly=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));var oldType=oldAssembly.GetType("JouchoPet.GreetingArmRig");
  foreach(int size in new[]{220,300,400}){Size c=PetRenderer.CanvasSize(size);using(var art=CharacterArt.Normalized("front",c.Width,c.Height,size))using(var rig=new GreetingArmRig(PoseMesh.Read(art),c.Width,c.Height,size)){
   bool vertical=true,reachable=true;for(int k=0;k<=20;k++){var pose=rig.HairSample(1,k/20.0,x=>RigParts.Rotate(x,new PointF(c.Width*.5f,(float)(28+size*.30)),-.018),size*.004);vertical&=Math.Abs(pose.Elbow.X-pose.Wrist.X)<.001&&pose.Wrist.Y<pose.Elbow.Y;reachable&=RigParts.Length(pose.Shoulder,pose.Elbow)<RigParts.Length(rig.Shoulder,rig.Elbow)*.96;}
   Check(vertical&&reachable,"bang contact has vertical forearm and reachable forward upper arm / "+size);
   object oldRig=Activator.CreateInstance(oldType,Flags,null,new object[]{PoseMesh.Read(art),c.Width,c.Height,(double)size},null);double currentReach=0,oldReach=0;
   try{for(int i=0;i<=100;i++){double lift=i/100.0;var p=rig.HairSample(lift,0,x=>x,0);object previous=oldType.GetMethod("HairSample",Flags).Invoke(oldRig,new object[]{lift,0.0,(Func<PointF,PointF>)(x=>x),0.0});var elbow=(PointF)previous.GetType().GetField("Elbow",Flags).GetValue(previous);currentReach=Math.Max(currentReach,p.Shoulder.X-p.Elbow.X);oldReach=Math.Max(oldReach,p.Shoulder.X-elbow.X);}
    Console.WriteLine("Elbow outward reach / "+size+": "+oldReach.ToString("F2")+" -> "+currentReach.ToString("F2")+" px");var raised=rig.HairSample(1,0,x=>x,0);object previousContact=oldType.GetMethod("HairSample",Flags).Invoke(oldRig,new object[]{1.0,0.0,(Func<PointF,PointF>)(x=>x),0.0});var previousTip=(PointF)previousContact.GetType().GetField("Tip",Flags).GetValue(previousContact);
    Check(raised.Tip.Y<previousTip.Y-size*.04&&raised.Wrist.Y<previousTip.Y,"whole palm raised above previous lower bang contact / "+size);
   }finally{((IDisposable)oldRig).Dispose();}
  }}
 }
 static object ConvertValue(object value,Type target){if(value==null||target.IsPrimitive||target==typeof(string))return value;object o=Activator.CreateInstance(target);foreach(var field in target.GetFields(Flags)){var source=value.GetType().GetField(field.Name,Flags);if(source!=null)field.SetValue(o,ConvertValue(source.GetValue(value),field.FieldType));}return o;}
 static void Regression(){var old=Assembly.LoadFile(Path.GetFullPath("qa/v0725/baseline/IsekaijouchoPet.exe"));var type=old.GetType("JouchoPet.PetRenderer");object prior=Activator.CreateInstance(type,true);using(var now=new PetRenderer()){now.Resize(300);type.GetMethod("Resize").Invoke(prior,new object[]{300});try{
  var frames=new[]{new RenderFrame{MotionState="idle"},new RenderFrame{MotionState="idle",Blink=1},new RenderFrame{MotionState="idle",Affection=1,ClosedEyes=1,PatSide=1},new RenderFrame{MotionState="idle",Greeting=1,WaveBlend=1,WavePhase=.7},new RenderFrame{MotionState="idle",RestAmount=.4,ClosedEyes=1},new RenderFrame{MotionState="idle",RestAmount=1,ClosedEyes=1},new RenderFrame{MotionState="turn",Angle=-.5},new RenderFrame{MotionState="turn",Angle=.5},new RenderFrame{MotionState="walk",Facing=-1,Speed=-36,Gait=.7},new RenderFrame{MotionState="walk",Facing=1,Speed=36,Gait=.7},new RenderFrame{MotionState="walk",Facing=-1,Speed=-198,Gait=.7,RunBlend=1},new RenderFrame{MotionState="walk",Facing=1,Speed=198,Gait=.7,RunBlend=1},new RenderFrame{MotionState="hold",Facing=-1,Carry=1,CarryEffort=.8},new RenderFrame{MotionState="hold",Facing=1,Carry=1,CarryEffort=.8}};
  frames=frames.Concat(new[]{new RenderFrame{MotionState="hold",Facing=-1,Leaping=true,Leap=LeapMotion.At(.5,-1)},new RenderFrame{MotionState="hold",Facing=1,Leaping=true,Leap=LeapMotion.At(.5,1)},new RenderFrame{Archery=true,BowPose=ArcheryMotion.At(2,-1)},new RenderFrame{Archery=true,BowPose=ArcheryMotion.At(2,1)}}).ToArray();
  foreach(var f in frames){var a=PoseMesh.Read((Bitmap)type.GetMethod("Draw").Invoke(prior,new[]{ConvertValue(f,old.GetType("JouchoPet.RenderFrame"))}));Check(a.SequenceEqual(PoseMesh.Read(now.Draw(f))),"v0.7.24 exact pixels: "+f.MotionState+" / "+f.Facing+" / rest "+f.RestAmount);}
  foreach(int dir in new[]{-1,1}){var p=SwordMotion.At(.6,dir);var method=type.GetMethod("DrawSword",Flags);var a=PoseMesh.Read((Bitmap)method.Invoke(prior,new[]{ConvertValue(p,old.GetType("JouchoPet.SwordPose")),(object)true}));Check(a.SequenceEqual(PoseMesh.Read(now.DrawSword(p,true))),"sword exact pixels / "+dir);}
  foreach(int dir in new[]{-1,1}){var p=CastingMotion.At(1.9,dir,25);var method=type.GetMethod("DrawCast",Flags);var a=PoseMesh.Read((Bitmap)method.Invoke(prior,new[]{ConvertValue(p,old.GetType("JouchoPet.CastPose")),(object)true}));Check(a.SequenceEqual(PoseMesh.Read(now.DrawCast(p,true))),"casting exact pixels / "+dir);}
 }finally{((IDisposable)prior).Dispose();}}}
 static void Integration(){using(var pet=new Pet()){
  typeof(Pet).GetField("settings",Flags).SetValue(pet,Path.GetFullPath(Folder+"test-settings.txt"));typeof(Pet).GetField("wandering",Flags).SetValue(pet,false);
  var motion=(Motion)typeof(Pet).GetField("motion",Flags).GetValue(pet);var d=(DailyMotion)typeof(Pet).GetField("daily",Flags).GetValue(pet);motion.Reset();Check(pet.Wink(),"Pet semantic Wink entry");typeof(Pet).GetMethod("UpdateDaily",Flags).Invoke(pet,new object[]{.01});Check(d.Active,"Pet begins at idle");
  typeof(Pet).GetField("dragging",Flags).SetValue(pet,true);typeof(Pet).GetMethod("UpdateDaily",Flags).Invoke(pet,new object[]{.25});Check(!d.Busy&&!pet.AdjustHair(),"drag cancels and rejects new daily action");typeof(Pet).GetField("dragging",Flags).SetValue(pet,false);
  foreach(string higher in new[]{"sleeping","leap","archery","sword","casting"}){
   motion.Reset();typeof(Pet).GetField("walking",Flags).SetValue(pet,false);Check(pet.AdjustHair(),"hair ready before "+higher);d.Update(.01,true);d.Update(1.1,true);
   if(higher=="sleeping")typeof(Pet).GetField("sleeping",Flags).SetValue(pet,true);
   if(higher=="leap")((LeapMotion)typeof(Pet).GetField("leap",Flags).GetValue(pet)).Request(1,1);
   if(higher=="archery")((ArcheryMotion)typeof(Pet).GetField("archery",Flags).GetValue(pet)).Request(1);
   if(higher=="sword")((SwordMotion)typeof(Pet).GetField("sword",Flags).GetValue(pet)).Request(1);
   if(higher=="casting")((CastingMotion)typeof(Pet).GetField("casting",Flags).GetValue(pet)).Request(new PointF(100,100),1);
   typeof(Pet).GetMethod("UpdateDaily",Flags).Invoke(pet,new object[]{.1});Check(d.Active&&d.Pose.Lift>0&&d.Pose.Lift<1,"smooth hand return before "+higher);
   typeof(Pet).GetMethod("UpdateDaily",Flags).Invoke(pet,new object[]{.15});Check(!d.Busy,"higher priority clears daily state: "+higher);
   typeof(Pet).GetField("sleeping",Flags).SetValue(pet,false);foreach(string field in new[]{"leap","archery","sword","casting"}){var action=typeof(Pet).GetField(field,Flags).GetValue(pet);action.GetType().GetMethod("Cancel",Flags).Invoke(action,null);}
  }
  Check(pet.Think(3),"Pet Think duration entry");d.Update(.01,true);typeof(Pet).GetMethod("StartWalk",Flags).Invoke(pet,new object[]{1,true});typeof(Pet).GetMethod("UpdateDaily",Flags).Invoke(pet,new object[]{.25});Check(!d.Busy,"run request cancels daily action");
 }}
 static void Preview(){foreach(int size in new[]{220,300,400})using(var r=new PetRenderer()){r.Resize(size);Size c=PetRenderer.CanvasSize(size);foreach(DailyAction kind in new[]{DailyAction.Wink,DailyAction.Thinking,DailyAction.Hair}){
  double[] times=kind==DailyAction.Wink?new[]{0,.20,.64,1.16,1.48}:kind==DailyAction.Thinking?new[]{0,.15,.7,1.95,2.1}:new[]{0,.55,.90,1.20,1.55,1.75};
  using(var sheet=new Bitmap(c.Width*times.Length,c.Height+28))using(var g=Graphics.FromImage(sheet)){g.Clear(Color.FromArgb(42,47,57));for(int i=0;i<times.Length;i++){var f=new RenderFrame{MotionState="idle",Time=times[i],Daily=DailyMotion.At(kind,times[i])};g.DrawImageUnscaled(r.Draw(f),i*c.Width,28);g.DrawString(kind+" "+times[i].ToString("F2")+"s",SystemFonts.DefaultFont,Brushes.White,i*c.Width+8,5);}sheet.Save(Folder+kind+"-"+size+".png");}
  if(size==300){int n=(int)Math.Ceiling((DailyMotion.Duration(kind)+.3)*24),cols=10;using(var atlas=new Bitmap(c.Width*cols,c.Height*((n+cols-1)/cols)))using(var g=Graphics.FromImage(atlas)){g.Clear(Color.Transparent);for(int i=0;i<n;i++){var f=new RenderFrame{MotionState="idle",Time=i/24.0,Daily=DailyMotion.At(kind,i/24.0)};if(kind==DailyAction.Thinking&&i>=22&&i<27)f.Blink=Math.Sin((i-22)/5.0*Math.PI);g.DrawImageUnscaled(r.Draw(f),i%cols*c.Width,i/cols*c.Height);}atlas.Save(Folder+kind+"-atlas.png");File.WriteAllText(Folder+kind+"-atlas.txt",c.Width+","+c.Height+","+n);}}
 }}}
 static void Entries(){foreach(int size in new[]{220,300,400})using(var r=new PetRenderer()){r.Resize(size);Size c=PetRenderer.CanvasSize(size);foreach(int dir in new[]{-1,1})using(var sheet=new Bitmap(c.Width*5,(c.Height+26)*3))using(var g=Graphics.FromImage(sheet)){
  g.Clear(Color.FromArgb(42,47,57));int row=0;foreach(DailyAction kind in new[]{DailyAction.Wink,DailyAction.Thinking,DailyAction.Hair}){
   var m=new Motion();m.FollowPush(0,0,dir,size);var d=new DailyMotion();d.Request(kind);int col=0;bool turn=false,started=false,peak=false;
   for(int k=0;k<260;k++){bool capture=k==0;if(!turn&&m.State=="turn"&&m.TurnProgress>=.45){turn=true;capture=true;}if(!started&&d.Active){started=true;capture=true;}double peakTime=kind==DailyAction.Wink?.52:kind==DailyAction.Hair?1.1:.8;if(!peak&&d.Active&&d.Age>=peakTime){peak=true;capture=true;}if(started&&!d.Busy){capture=true;}
    if(capture&&col<5){var f=new RenderFrame{MotionState=m.State,Facing=m.Facing,Angle=m.Angle,Settle=m.Settle,Time=k/60.0,Daily=d.Active?d.Pose:new DailyPose()};g.DrawImageUnscaled(r.Draw(f),col*c.Width,row*(c.Height+26)+26);g.DrawString(kind+" "+(k/60.0).ToString("F2")+"s",SystemFonts.DefaultFont,Brushes.White,col*c.Width+8,row*(c.Height+26)+4);col++;}
    if(started&&!d.Busy)break;
    if(!d.Active)m.Update(1.0/60,0,size);d.Update(1.0/60,m.State=="idle"&&m.Facing==0);
   }row++;
  }sheet.Save(Folder+"entry-"+(dir<0?"left":"right")+"-"+size+".png");
 }}}

 // Test-only visible host: ticks the real Pet controller and displays its actual
 // SoftRig canvas. It neither changes production window styles nor adds an editor.
 static void Live(){
  Application.EnableVisualStyles();using(var pet=new Pet())using(var host=new Form{Text="日常动作验收 v0.7.28",Width=1100,Height=650,StartPosition=FormStartPosition.CenterScreen,BackColor=Color.FromArgb(42,47,57)}){
   typeof(Pet).GetField("settings",Flags).SetValue(pet,Path.GetFullPath(Folder+"live-settings.txt"));typeof(Pet).GetField("wandering",Flags).SetValue(pet,false);
   typeof(Pet).GetField("petHeight",Flags).SetValue(pet,300);typeof(Pet).GetMethod("ResizePet",Flags).Invoke(pet,null);var handle=pet.Handle;
   var menu=(ContextMenuStrip)typeof(Pet).GetField("menu",Flags).GetValue(pet);
   var controls=new FlowLayoutPanel{Dock=DockStyle.Top,Height=42};host.Controls.Add(controls);
   var live=new PictureBox{Left=20,Top=70,Width=400,Height=460,SizeMode=PictureBoxSizeMode.Zoom};host.Controls.Add(live);
   var phase=new Label{Left=20,Top=540,Width=1000,ForeColor=Color.White};host.Controls.Add(phase);
   var strip=new PictureBox{Left=430,Top=70,Width=620,Height=460,SizeMode=PictureBoxSizeMode.Zoom};host.Controls.Add(strip);
   var daily=(DailyMotion)typeof(Pet).GetField("daily",Flags).GetValue(pet);DailyAction previous=DailyAction.None;int captured=0;Bitmap sheet=null;double[] marks=null;
   Action<string,Action> button=(name,action)=>{var b=new Button{Text=name,AutoSize=true};b.Click+=(sender,e)=>action();controls.Controls.Add(b);};
   button("Wink",()=>pet.Wink());button("Thinking",()=>pet.Think());button("Hair",()=>pet.AdjustHair());button("Cancel",()=>pet.CancelDailyAction());
   button("Menu",()=>menu.Show(host,new Point(10,45)));
   var sizes=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=80};sizes.Items.AddRange(new object[]{220,300,400});sizes.SelectedItem=300;sizes.SelectedIndexChanged+=(sender,e)=>{typeof(Pet).GetField("petHeight",Flags).SetValue(pet,(int)sizes.SelectedItem);typeof(Pet).GetMethod("ResizePet",Flags).Invoke(pet,null);};controls.Controls.Add(sizes);
   using(var timer=new Timer{Interval=16}){
    timer.Tick+=(sender,e)=>{
     typeof(Pet).GetMethod("Animate",Flags).Invoke(pet,new object[]{null,EventArgs.Empty});
     var renderer=typeof(Pet).GetField("renderer",Flags).GetValue(pet);var front=typeof(PetRenderer).GetField("front",Flags).GetValue(renderer);var bitmap=(Bitmap)typeof(SoftRig).GetField("canvas",Flags).GetValue(front);
     var prior=live.Image;live.Image=(Bitmap)bitmap.Clone();if(prior!=null)prior.Dispose();
     if(daily.Active&&daily.Kind!=previous){previous=daily.Kind;captured=0;marks=previous==DailyAction.Wink?new[]{0.0,.40,.88,1.20}:previous==DailyAction.Hair?new[]{0.0,.55,1.05,1.5}:new[]{0.0,.3,1.0,1.9};if(sheet!=null)sheet.Dispose();sheet=new Bitmap(bitmap.Width*2,bitmap.Height*2);using(var g=Graphics.FromImage(sheet))g.Clear(host.BackColor);}
     if(daily.Active&&captured<4&&daily.Age>=marks[captured]){using(var g=Graphics.FromImage(sheet))g.DrawImageUnscaled(bitmap,captured%2*bitmap.Width,captured/2*bitmap.Height);captured++;var old=strip.Image;strip.Image=(Bitmap)sheet.Clone();if(old!=null)old.Dispose();sheet.Save(Folder+"live-"+previous+"-"+sizes.SelectedItem+".png");}
     phase.Text=daily.Busy?daily.Kind+" / "+daily.Age.ToString("F2")+"s":"Idle — 使用真实 Pet 控制器、帧时钟输入及渲染结果";
     if(!daily.Busy)previous=DailyAction.None;
    };host.Shown+=(sender,e)=>timer.Start();host.FormClosed+=(sender,e)=>timer.Stop();Application.Run(host);
   }
   if(live.Image!=null)live.Image.Dispose();if(strip.Image!=null)strip.Image.Dispose();if(sheet!=null)sheet.Dispose();
  }
 }

}

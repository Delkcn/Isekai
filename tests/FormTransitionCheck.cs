using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;using System.Linq;using System.Reflection;using System.Diagnostics;using System.Threading;using System.Collections.Generic;using JouchoPet;
class FormTransitionCheck {
 const string Folder="qa/v0821/";const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;static int checks,frames;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static void Ready(FormTransition t,double now){var wait=Stopwatch.StartNew();while(t.Loading){t.Advance(now);if(wait.Elapsed.TotalSeconds>30)throw new Exception("preload timeout");Thread.Sleep(2);}}
 static PetRenderer Create(CharacterForm f,int size){var r=new PetRenderer(f);r.Resize(size);return r;}
 static PetRenderer[] Forms(FormTransition t){return (PetRenderer[])typeof(FormTransition).GetField("forms",F).GetValue(t);}
 static int[] Raw(Bitmap b){var p=new int[b.Width*b.Height];var data=b.LockBits(new Rectangle(Point.Empty,b.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);try{for(int y=0;y<b.Height;y++)System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),p,y*b.Width,b.Width);}finally{b.UnlockBits(data);}return p;} static void EqualBlend(int[] a,int[] b,int[] actual,double mix){int weight=(int)Math.Round(mix*65536),inverse=65536-weight;for(int i=0;i<a.Length;i++)for(int shift=0;shift<32;shift+=8){int expected=(int)((((uint)a[i]>>shift&255)*inverse+((uint)b[i]>>shift&255)*weight+32768)>>16);if(expected!=((uint)actual[i]>>shift&255))throw new Exception("Live-pose blend mismatch / "+i+" / "+shift);}}
 static readonly string[] Kinds={"idle","pat","greet","wave","walk","run","turn","pivot","sleep","carry","runcarry","leapup","leapdown","carryleap","wink","think","hair","sigil","cross","archery","sword","cast","lookout","beam","levitate","teleport"};
 static RenderFrame Frame(string kind,double t,int direction){
  var f=new RenderFrame{MotionState="idle",Time=t*2,Settle=1};
  if(kind=="pat"){f.Affection=PatResponse.Strength(t*2);f.ClosedEyes=PatResponse.EyeClosure(t*2);f.Nod=PatResponse.Nod(t*2);f.PatSide=direction;}
  if(kind=="greet"||kind=="wave"){f.Greeting=Math.Sin(Math.PI*t);f.WaveBlend=kind=="wave"?1:0;f.WavePhase=t;}
  if(kind=="walk"||kind=="run"||kind=="carry"||kind=="runcarry"){f.MotionState="walk";f.Facing=direction;f.Speed=36;f.Gait=t;f.Settle=0;f.RunBlend=kind=="run"||kind=="runcarry"?1:0;f.RunningCarry=kind=="runcarry";f.Carry=kind=="carry"||kind=="runcarry"?1:0;f.CarryEffort=f.Carry;}
  if(kind=="turn"||kind=="pivot"){f.MotionState="turn";f.Angle=direction*(2*t-1);f.LightTurn=kind=="pivot";f.TurnProgress=t;f.TurnFrom=-direction;f.SupportNear=direction<0;}
  if(kind=="sleep"){f.RestAmount=t;f.ClosedEyes=t;}
  if(kind.StartsWith("leap")||kind=="carryleap"){f.Facing=direction;f.Leaping=true;f.Leap=LeapMotion.At(t*1.95,kind=="leapdown"?1:-1);f.Carry=kind=="carryleap"?1:0;f.CarryEffort=f.Carry;}
  if(kind=="archery"){f.Archery=true;f.BowPose=ArcheryMotion.At(t*4.85,direction);}
  if(kind=="wink")f.Daily=DailyMotion.At(DailyAction.Wink,t*1.48);
  if(kind=="think")f.Daily=DailyMotion.At(DailyAction.Thinking,t*2.5);
  if(kind=="hair")f.Daily=DailyMotion.At(DailyAction.Hair,t*2.05);
  if(kind=="sigil"||kind=="cross"){f.Daily=DailyMotion.At(DailyAction.Sigil,t*2);f.Daily.SigilSeed=71;f.Daily.CrossOnly=kind=="cross";}
  return f;
 }
 static Bitmap Render(PetRenderer renderer,string kind,double t,int direction,int size){
  if(kind=="sword")return renderer.DrawSword(SwordMotion.At(t*SwordMotion.Duration,direction),true);
  if(kind=="cast")return renderer.DrawCast(CastingMotion.At(t*CastingMotion.Duration,direction,.18),true);
  if(kind=="lookout")return renderer.Reach.DrawLookout(direction,Math.Sin(Math.PI*t));
  if(kind=="beam"||kind=="levitate")return renderer.Reach.Draw(direction,Math.Sin(Math.PI*t),new PointF(PetRenderer.CanvasSize(size).Width*.5f+direction*size*2,size*.4f));
  return renderer.Draw(Frame(kind,t,direction));
 }

 static void Visuals(){
  using(var t=new FormTransition(Create(CharacterForm.AnemoneI,220),220)){
   Ready(t,0);var pair=Forms(t);t.Request(CharacterForm.AnemoneII,1);t.Advance(1+FormTransition.Duration/2);
   Check(Math.Abs(t.Mix-.5)<1e-10,"450ms transition is halfway at 225ms");
   foreach(string kind in Kinds){foreach(int direction in new[]{-1,1})foreach(double phase in new[]{.2,.5,.8}){
    var a=Raw(Render(pair[0],kind,phase,direction,220));var b=Raw(Render(pair[1],kind,phase,direction,220));var c=Raw(t.Draw(r=>Render(r,kind,phase,direction,220)));EqualBlend(a,b,c,t.Mix);frames++;
   }Check(true,"both forms use same live pose and premultiplied blend / "+kind);}
   var first=PoseMesh.Read(t.Draw(r=>Render(r,"walk",.15,1,220)));var next=PoseMesh.Read(t.Draw(r=>Render(r,"walk",.55,1,220)));Check(!first.SequenceEqual(next),"pose advances inside fade instead of blending frozen snapshots");
   double mix=t.Mix;t.Request(CharacterForm.AnemoneI,1.225);Check(Math.Abs(t.Mix-mix)<1e-10,"reverse keeps current blend without a jump");t.Advance(1.3);Check(t.Mix<mix,"reverse moves continuously back toward A1");mix=t.Mix;t.Request(CharacterForm.AnemoneII,1.3);Check(Math.Abs(t.Mix-mix)<1e-10,"rapid retarget preserves continuity");t.Advance(2);Check(t.Mix==1&&t.Current.Form==CharacterForm.AnemoneII,"finish adopts A2 without replacing action state");
   foreach(var f in new[]{CharacterForm.AnemoneI,CharacterForm.AnemoneII}){t.Request(f,3+(int)f);t.Advance(3.5+(int)f);Check(PoseMesh.Read(t.Draw(r=>Render(r,"pat",.4,-1,220))).SequenceEqual(PoseMesh.Read(Render(pair[(int)f],"pat",.4,-1,220))),"fade endpoint matches original renderer exactly / "+f);}
   foreach(int direction in new[]{-1,1}){for(int i=0;i<12;i++){var p=SwordMotion.At(i/12.0,direction);if(!pair[0].SwordBlade(p).SequenceEqual(pair[1].SwordBlade(p)))throw new Exception("weapon geometry differs across forms");var cast=CastingMotion.At(i/12.0*CastingMotion.Duration,direction,.1);if(pair[0].CastCore(cast)!=pair[1].CastCore(cast))throw new Exception("cast origin differs across forms");}Check(true,"shared sword hit geometry and projectile origins / "+direction);}
   t.Request(CharacterForm.AnemoneI,5);t.Advance(5.2);mix=t.Mix;t.Resize(300);Check(t.Mix==mix&&t.Draw(r=>r.Draw(Frame("turn",.3,1))).Size==PetRenderer.CanvasSize(300),"resize preserves blend and equal canvas dimensions");
   var sigil=Frame("sigil",.4,1);t.Draw(r=>r.Draw(sigil));var p0=pair[0].SigilAnchor;var p1=pair[1].SigilAnchor;var p2=t.RenderedSigilAnchor;Check(Math.Abs(p2.X-(p0.X+(p1.X-p0.X)*t.Mix))<.001,"sigil overlay follows blended live wrist");
  }
 }
 sealed class Files:IDesktopAttackFiles {
  internal List<DesktopAttackFile> Items=new List<DesktopAttackFile>();internal int Opens,Reads;
  public List<DesktopAttackFile> Read(){Reads++;return Items;}public List<DesktopAttackFile> ReadForSelection(){return Items;}public int Select(List<DesktopAttackFile> f){return f.Count;}public ProgramLaunchResult Open(DesktopAttackFile f){Opens++;return new ProgramLaunchResult(true,"");}
 }
 static void Actions(){using(var t=new FormTransition(Create(CharacterForm.AnemoneI,220),220)){
  Ready(t,0);var motion=new Motion();var reference=new Motion();var planted=new Motion{Facing=1,State="hold",Settle=1};
  var sword=new SwordMotion();sword.Request(1);sword.Begin(planted,true);int hits=0;sword.HitFrame+=(s,e)=>hits++;Guid swordId=sword.RequestId;
  var casting=new CastingMotion();Guid castId=casting.Request(new PointF(400,200),1);casting.Begin(planted,true,new PointF(180,150));int releases=0;
  var bow=new ArcheryMotion();bow.Request(1);bow.Begin(planted,true);int arrows=0;
  var bolt=new MagicBolt(Guid.NewGuid(),new PointF(100,100),new PointF(400,100),220,"mock-file");int boltHits=0;bolt.ProjectileHit+=(s,e)=>boltHits++;
  var files=new Files();var strike=new SwordFileStrike();strike.Begin();var blade=t.Current.SwordBlade(SwordMotion.At(.55,1));var center=RigParts.Mix(RigParts.Mix(blade[0],blade[4],.5),blade[2],.55);files.Items.Add(new DesktopAttackFile("mock",new Rectangle((int)center.X-4,(int)center.Y-4,8,8)));
  var idling=PoseMesh.Read(t.Draw(r=>r.Draw(Frame("idle",0,1))));int movingImages=0;double travel=0,expectedTravel=0;
  for(int i=0;i<720;i++){double now=i/120.0,dt=1/120.0;if(i==12||i==55||i==83||i==200||i==270)t.Request(t.Requested==CharacterForm.AnemoneI?CharacterForm.AnemoneII:CharacterForm.AnemoneI,now);t.Advance(now);
   travel+=motion.Update(dt,now<3?1:-1,220);expectedTravel+=reference.Update(dt,now<3?1:-1,220);if(motion.State!=reference.State||motion.Angle!=reference.Angle||motion.Phase!=reference.Phase||motion.Speed!=reference.Speed||travel!=expectedTravel)throw new Exception("Switch altered locomotion timeline");
   double previous=sword.Age;sword.Update(dt);strike.Update(files,age=>t.Current.SwordBlade(SwordMotion.At(age,1)),previous,sword.Age);if(casting.Update(dt))releases++;if(bow.Update(dt))arrows++;bolt.Update(dt);
   if(sword.RequestId!=swordId||casting.Id!=castId)throw new Exception("Action restarted");
   if(i%16==0){var f=new RenderFrame{Time=now,MotionState=motion.State,Facing=motion.Facing,Angle=motion.Angle,Gait=motion.Phase,Speed=motion.Speed,Settle=motion.Settle};if(!idling.SequenceEqual(PoseMesh.Read(t.Draw(r=>r.Draw(f)))))movingImages++;}
  }
  Check(travel==expectedTravel&&movingImages>30,"720 ticks: walking, turning and movement stay on uninterrupted reference timeline");Check(hits==1&&sword.Age==SwordMotion.Duration,"sword keeps request ID, age and exactly one hit");Check(releases==1&&casting.Age==CastingMotion.Duration,"casting keeps request ID, duration and exactly one release");Check(arrows==1&&bow.Age==ArcheryMotion.Duration,"archery continues to one release and natural completion");Check(boltHits==1&&!bolt.Cancelled,"in-flight magic projectile keeps one impact during repeated switching");Check(files.Reads==1&&files.Opens==1,"mock sword file target read/open occurs once despite repeated switching");
 }}
 static void Loading(){
  using(var gate=new ManualResetEventSlim(false)){PetRenderer result=null;var t=new FormTransition(Create(CharacterForm.AnemoneI,220),220,(form,size)=>{gate.Wait();result=Create(form,size);return result;});try{
   var watch=Stopwatch.StartNew();t.Request(CharacterForm.AnemoneII,0);Check(watch.ElapsedMilliseconds<100&&t.Loading&&t.Mix==0,"request while asset load is pending returns without blocking");
   var a=PoseMesh.Read(t.Draw(r=>Render(r,"walk",.1,1,220)));var b=PoseMesh.Read(t.Draw(r=>Render(r,"walk",.5,1,220)));Check(!a.SequenceEqual(b),"A1 action advances while A2 prepares");
   t.Request(CharacterForm.AnemoneI,.1);t.Resize(300);gate.Set();Ready(t,.2);Check(t.Requested==CharacterForm.AnemoneI&&t.Mix==0&&Forms(t)[1].Draw(Frame("idle",0,1)).Size==PetRenderer.CanvasSize(300),"pending request can cancel; stale-size preload is rebuilt");
  }finally{gate.Set();t.Dispose();}Check(result!=null&&!result.Ready,"prepared renderers released on disposal");}
  using(var t=new FormTransition(Create(CharacterForm.AnemoneI,220),220,(form,size)=>{throw new IOException("missing A2 fixture");})){t.Request(CharacterForm.AnemoneII,0);Ready(t,.1);Check(t.Current.Form==CharacterForm.AnemoneI&&t.Mix==0&&t.TakeError()!=null,"load failure retains live A1 and reports failure");Check(t.Draw(r=>Render(r,"walk",.3,1,220))!=null,"load failure does not block current action rendering");}
  using(var gate=new ManualResetEventSlim(false))using(var done=new ManualResetEventSlim(false)){PetRenderer result=null;var t=new FormTransition(Create(CharacterForm.AnemoneI,220),220,(f,s)=>{gate.Wait();result=Create(f,s);done.Set();return result;});t.Dispose();gate.Set();if(!done.Wait(30000))throw new Exception("dispose preparation timed out");var watch=Stopwatch.StartNew();while(result.Ready&&watch.ElapsedMilliseconds<30000)Thread.Sleep(2);Check(!result.Ready,"closing during preload releases late result without UI callback");}
 }
 static void Preview(){Directory.CreateDirectory(Folder+"frames");using(var t=new FormTransition(Create(CharacterForm.AnemoneI,300),300)){
  Ready(t,0);foreach(string kind in new[]{"walk","turn","pat","sword","cast","archery"}){t.Request(CharacterForm.AnemoneI,0);t.Advance(1);t.Request(CharacterForm.AnemoneII,2);
   using(var strip=new Bitmap(1500,420))using(var sg=Graphics.FromImage(strip)){sg.Clear(Color.FromArgb(47,52,61));for(int i=0;i<=30;i++){double age=i/30.0*.8;t.Advance(2+age);double phase=kind=="sword"?.28+age/SwordMotion.Duration:kind=="cast"?.45+age/CastingMotion.Duration:kind=="archery"?.35+age/ArcheryMotion.Duration:.1+age*.8;var b=t.Draw(r=>Render(r,kind,Math.Min(.95,phase),1,300));int previewWidth=kind=="sword"||kind=="cast"?640:440;using(var pic=new Bitmap(previewWidth,420))using(var g=Graphics.FromImage(pic)){g.Clear(Color.FromArgb(47,52,61));int padding=kind=="sword"?t.Current.SwordPadding:kind=="cast"?t.Current.CastPadding:0;double scale=.85;g.DrawImage(b,new RectangleF((float)(previewWidth*.5-b.Width*scale/2),(float)(370-(28+300*.98+padding)*scale),(float)(b.Width*scale),(float)(b.Height*scale)));g.DrawString(kind+" / A2 "+t.Mix.ToString("P0"),SystemFonts.DefaultFont,Brushes.White,15,12);pic.Save(Folder+"frames/"+kind+"-"+i.ToString("00")+".png",ImageFormat.Png);if(i%6==0&&i<30)sg.DrawImage(pic,new Rectangle(i/6*300,0,300,410));}}
    strip.Save(Folder+kind+"-transition.png",ImageFormat.Png);
   }
  }
 }}
 static object Get(Pet p,string name){return typeof(Pet).GetField(name,F).GetValue(p);}static void Set(Pet p,string name,object value){typeof(Pet).GetField(name,F).SetValue(p,value);}static object Call(Pet p,string name,params object[] args){return typeof(Pet).GetMethod(name,F).Invoke(p,args);}
 static string Snapshot(object value,int depth=0){if(value==null)return "null";var type=value.GetType();if(type.IsPrimitive||type.IsEnum||value is string||value is Guid||value is DateTime||value is PointF)return value.ToString();if(value is Delegate||depth>3)return type.Name;return String.Join(";",type.GetFields(F).Where(f=>!f.IsStatic).OrderBy(f=>f.Name).Select(f=>f.Name+"="+Snapshot(f.GetValue(value),depth+1)));}
 static string ActionSnapshot(Pet p){return String.Join("|",new[]{"motion","rest","greeting","leap","archery","sword","casting","daily","utility","walking","running","sleeping","dragging","carryPhase","carryAge","carryAmount","carryEffort","previousTime","elapsed","gait","speed"}.Select(n=>n+"="+Snapshot(Get(p,n))));}
 static void Runtime(){
  string settings=Path.GetFullPath(Folder+"runtime/settings.txt");Directory.CreateDirectory(Path.GetDirectoryName(settings));
  using(var pet=new Pet(settings))try{
   Check(!pet.Visible&&!((System.Windows.Forms.NotifyIcon)Get(pet,"tray")).Visible,"real Pet handlers tested without desktop window or tray icon");
   Call(pet,"PrepareForms");var t=(FormTransition)Get(pet,"formTransition");Ready(t,((Stopwatch)Get(pet,"clock")).Elapsed.TotalSeconds);var motion=(Motion)Get(pet,"motion");var rest=(RestMotion)Get(pet,"rest");var greeting=(GreetingMotion)Get(pet,"greeting");
   foreach(string state in new[]{"walk","turn","sleep","pat","greet","carry","sword","cast","utility","drag"}){
    if(state=="walk"){motion.State="walk";motion.Speed=36;motion.Facing=1;Set(pet,"walking",true);}if(state=="turn"){motion.State="turn";motion.Angle=.35;}
    if(state=="sleep"){Set(pet,"sleeping",true);rest.RequestSleep(true);rest.Update(.25,true);}if(state=="pat")rest.RequestPat(-1);if(state=="greet")greeting.Request(true);
    if(state=="carry"){Set(pet,"carryPhase","approach");Set(pet,"carryAge",.8);Set(pet,"carryAmount",.5);}if(state=="sword"){var sword=(SwordMotion)Get(pet,"sword");sword.Request(1);sword.Active=true;sword.Age=.52;}
    if(state=="cast"){var cast=(CastingMotion)Get(pet,"casting");cast.Request(new PointF(300,200),1);cast.Active=true;cast.Age=1.2;}if(state=="utility"){var utility=(UtilityMotion)Get(pet,"utility");utility.Kind=UtilityAction.Beam;utility.Age=.7;}if(state=="drag")Set(pet,"dragging",true);
    string before=ActionSnapshot(pet),error;Check(pet.FormSwitchReady&&pet.TryChangeForm(pet.CurrentForm==CharacterForm.AnemoneI?CharacterForm.AnemoneII:CharacterForm.AnemoneI,out error),"real switch entry accepts busy state / "+state);Check(before==ActionSnapshot(pet),"real switch entry preserves every action state and clock / "+state);
   }
   var menu=(System.Windows.Forms.ContextMenuStrip)Get(pet,"menu");typeof(System.Windows.Forms.ToolStripDropDown).GetMethod("OnOpening",F).Invoke(menu,new object[]{new System.ComponentModel.CancelEventArgs()});Check(!(bool)Get(pet,"menuOpen")&&((System.Windows.Forms.ToolStripItem)Get(pet,"characterTitle")).Enabled,"opening actual menu leaves action clock running and switch enabled");
   string snapshot=ActionSnapshot(pet);((System.Windows.Forms.ToolStripItem)Get(pet,"characterTitle")).PerformClick();Check(snapshot==ActionSnapshot(pet),"top menu click switches without cancelling busy actions");
   Set(pet,"carryPhase","");Set(pet,"dragging",false);((UtilityMotion)Get(pet,"utility")).Kind=UtilityAction.None;
  }finally{typeof(System.Windows.Forms.Form).GetMethod("OnFormClosed",F).Invoke(pet,new object[]{new System.Windows.Forms.FormClosedEventArgs(System.Windows.Forms.CloseReason.None)});}
  using(var pet=new Pet(settings))try{
   Set(pet,"wandering",false);var files=new Files();Set(pet,"desktopAttackFiles",files);var motion=(Motion)Get(pet,"motion");motion.FollowPush(0,0,1,300);var renderer=(PetRenderer)Get(pet,"renderer");var blade=renderer.SwordBlade(SwordMotion.At(.55,1));var center=RigParts.Mix(RigParts.Mix(blade[0],blade[4],.5),blade[2],.55);files.Items.Add(new DesktopAttackFile("mock-runtime",new Rectangle((int)(center.X+pet.Left)-4,(int)(center.Y+pet.Top)-4,8,8)));
   Call(pet,"PrepareForms");var t=(FormTransition)Get(pet,"formTransition");Ready(t,0);pet.SwordSlash(PointF.Empty,1);int hits=0;pet.HitFrame+=(s,e)=>hits++;Call(pet,"UpdateSword",.01);Call(pet,"UpdateSword",.46);var sword=(SwordMotion)Get(pet,"sword");Guid id=sword.RequestId;string error;Check(pet.TryChangeForm(CharacterForm.AnemoneII,out error),"A1 to A2 accepted during actual pre-strike phase");double now=((Stopwatch)Get(pet,"clock")).Elapsed.TotalSeconds;typeof(FormTransition).GetField("started",F).SetValue(t,now-.225);Call(pet,"UpdateFormTransition",now);
   Check(t.Mix>0&&t.Mix<1&&sword.Age==.46&&sword.RequestId==id,"actual sword remains mid-action while form is halfway blended");Call(pet,"UpdateSword",.2);Call(pet,"UpdateSword",1.0);Check(hits==1&&files.Opens==1&&files.Reads==1,"actual Pet sword path emits one hit and one mock file open during transition");
  }finally{typeof(System.Windows.Forms.Form).GetMethod("OnFormClosed",F).Invoke(pet,new object[]{new System.Windows.Forms.FormClosedEventArgs(System.Windows.Forms.CloseReason.None)});}
 }
 [STAThread]static void Main(string[] args){try{AnimationParameters.UseFixedValues();if(args.Length>0&&args[0]=="preview"){Preview();return;}if(args.Length>0&&args[0]=="runtime"){Runtime();return;}Visuals();Actions();Loading();Runtime();Console.WriteLine("PASS "+checks+" transition checks; "+frames+" live-pose blends. No real file launch/process action.");}catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}




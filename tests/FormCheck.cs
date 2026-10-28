using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using JouchoPet;
class FormCheck {
 const string Folder="qa/v0816/";
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static int checks,frames; static Assembly baseline;
 static readonly string[] Kinds={"idle","pat","greet","wave","walk","run","turn","pivot","sleep","carry","runcarry","leapup","leapdown","carryleap","wink","think","hair","sigil","cross","archery","sword","cast","lookout","beam","levitate","teleport"};
 static void Check(bool b,string label){if(!b)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
 static object Call(object o,string name,params object[] args){return o.GetType().GetMethod(name,F).Invoke(o,args);}
 static object ConvertValue(object value,Type type){
  if(value==null||type.IsInstanceOfType(value))return value;
  if(type.IsEnum)return Enum.ToObject(type,Convert.ToInt32(value));
  var result=Activator.CreateInstance(type);
  foreach(var field in type.GetFields(F)){if(field.IsStatic)continue;var source=value.GetType().GetField(field.Name,F);if(source!=null)field.SetValue(result,ConvertValue(source.GetValue(value),field.FieldType));}return result;
 }
 static object OldCall(object o,string name,params object[] values){var method=o.GetType().GetMethod(name,F);var parameters=method.GetParameters();var args=new object[values.Length];for(int i=0;i<values.Length;i++)args[i]=ConvertValue(values[i],parameters[i].ParameterType);return method.Invoke(o,args);}
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
 static Bitmap OldRender(object renderer,string kind,double t,int direction,int size){
  if(kind=="sword")return (Bitmap)OldCall(renderer,"DrawSword",SwordMotion.At(t*SwordMotion.Duration,direction),true);
  if(kind=="cast")return (Bitmap)OldCall(renderer,"DrawCast",CastingMotion.At(t*CastingMotion.Duration,direction,.18),true);
  if(kind=="lookout"||kind=="beam"||kind=="levitate"){
   var reach=renderer.GetType().GetProperty("Reach",F).GetValue(renderer,null);
   return kind=="lookout"?(Bitmap)OldCall(reach,"DrawLookout",direction,Math.Sin(Math.PI*t)):(Bitmap)OldCall(reach,"Draw",direction,Math.Sin(Math.PI*t),new PointF(PetRenderer.CanvasSize(size).Width*.5f+direction*size*2,size*.4f));
  }
  return (Bitmap)OldCall(renderer,"Draw",Frame(kind,t,direction));
 }
 static void Resources(){
  CharacterForms.CheckResources(CharacterForm.AnemoneII);
  foreach(string path in Directory.GetFiles(Folder+"baseline/assets","*",SearchOption.AllDirectories)){
   string relative=path.Substring((Folder+"baseline/").Length);
   if(!File.ReadAllBytes(path).SequenceEqual(File.ReadAllBytes(relative)))throw new Exception("A1 asset changed: "+relative);
  }Check(true,"every existing asset is byte-identical to 0.8.13");
  Check(File.ReadAllBytes("src/Configuration/AnimationParameters.cs").SequenceEqual(File.ReadAllBytes(Folder+"baseline/src/Configuration/AnimationParameters.cs")),"fixed animation parameters unchanged");
  foreach(string path in Directory.GetFiles(Folder+"baseline/src/Animation","*.cs")){
   Check(File.ReadAllBytes(path).SequenceEqual(File.ReadAllBytes("src/Animation/"+Path.GetFileName(path))),"unchanged animation implementation / "+Path.GetFileName(path));
  }
  Check(CharacterForms.Resource("v0700/right-grip-hands.png",CharacterForm.AnemoneII)=="v0700/right-grip-hands.png","hands shared");
 }
 static void Regression(){
  baseline=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));
  baseline.GetType("JouchoPet.AnimationParameters").GetMethod("UseFixedValues",F).Invoke(null,null);
  foreach(int size in new[]{220,300,400}){
   object old=Activator.CreateInstance(baseline.GetType("JouchoPet.PetRenderer"),true);
   try{Call(old,"Resize",size);using(var a1=new PetRenderer())using(var a2=new PetRenderer(CharacterForm.AnemoneII)){
    a1.Resize(size);a2.Resize(size);
    var eyeA=typeof(PetRenderer).GetField("front",F).GetValue(a1);var eyeB=typeof(PetRenderer).GetField("front",F).GetValue(a2);
    var anchor=A2FrontArt.ExposedEye(PetRenderer.CanvasSize(size).Width,size);Check(Math.Abs((double)typeof(SoftRig).GetField("eyeX",F).GetValue(eyeB)-anchor.X)<.00001&&Math.Abs((double)typeof(SoftRig).GetField("eyeY",F).GetValue(eyeB)-anchor.Y)<.00001,"A2 approved eye registration / "+size);
    foreach(string kind in Kinds){
     for(int d=-1;d<=1;d+=2)for(int k=0;k<=8;k++){
      double t=k/8.0;int[] before=PoseMesh.Read(OldRender(old,kind,t,d,size)),now=PoseMesh.Read(Render(a1,kind,t,d,size));
      if(!before.SequenceEqual(now))throw new Exception("A1 regression / "+kind+" / "+size+" / "+d+" / "+k);
      int[] other=PoseMesh.Read(Render(a2,kind,t,d,size));
      if(other.Length!=now.Length||!other.Any(p=>((uint)p>>24)>100))throw new Exception("A2 empty or wrong canvas / "+kind);
      // Interleave forms so lazy layers/caches cannot silently depend on whichever loaded last.
      if(!now.SequenceEqual(PoseMesh.Read(Render(a1,kind,t,d,size))))throw new Exception("A2 contaminated A1 / "+kind);
      frames++;
     }Check(true,"A1 identical + A2 renders / "+kind+" / "+size+" / both directions, nine phases");
    }
    for(int d=-1;d<=1;d+=2)for(int k=0;k<=20;k++){
     double t=k/20.0;var x=a1.CastBones(CastingMotion.At(t*CastingMotion.Duration,d,.2));var y=a2.CastBones(CastingMotion.At(t*CastingMotion.Duration,d,.2));
     if(x.Shoulder!=y.Shoulder||x.Elbow!=y.Elbow||x.Wrist!=y.Wrist||x.Grip!=y.Grip)throw new Exception("form changed cast bones");
     var p=a1.Reach.SampleLookout(d,t);var q=a2.Reach.SampleLookout(d,t);
     if(p.Shoulder!=q.Shoulder||p.Elbow!=q.Elbow||p.Wrist!=q.Wrist||p.Tip!=q.Tip)throw new Exception("form changed lookout bones");
    }Check(true,"A1/A2 identical cast and lookout joints / "+size);
   }}finally{((IDisposable)old).Dispose();}
  }
 }
 static void Switching(){
  PetRenderer r=new PetRenderer();try{r.Resize(300);var frame=Frame("idle",.4,1);int[] a1=PoseMesh.Read(r.Draw(frame));string error;
   for(int i=0;i<3;i++){
    Check(PetRenderer.TryReplace(ref r,CharacterForm.AnemoneII,300,out error)&&r.Form==CharacterForm.AnemoneII,"switch to A2 / "+i);
    Check(!a1.SequenceEqual(PoseMesh.Read(r.Draw(frame))),"A2 has different appearance / "+i);
    foreach(int size in new[]{220,400,300}){r.Resize(size);Render(r,"wave",.5,1,size);Render(r,"lookout",.5,-1,size);Render(r,"lookout",.5,1,size);Render(r,"sleep",.5,1,size);}
    Check(PetRenderer.TryReplace(ref r,CharacterForm.AnemoneI,300,out error)&&a1.SequenceEqual(PoseMesh.Read(r.Draw(frame))),"switch back restores A1 exactly after resizing / "+i);
   }
   PetRenderer saved=r;Check(!PetRenderer.TryReplace(ref r,(CharacterForm)99,300,out error)&&r==saved&&r.Ready&&a1.SequenceEqual(PoseMesh.Read(r.Draw(frame))),"invalid form leaves old renderer alive");
   Check(PetRenderer.TryReplace(ref r,CharacterForm.AnemoneI,300,out error)&&r==saved,"same-form switch is a no-op");
  }finally{r.Dispose();}
 }
 static void MissingResource(){
  PetRenderer r=new PetRenderer();try{r.Resize(300);var saved=r;var frame=Frame("idle",.4,1);var pixels=PoseMesh.Read(r.Draw(frame));string error;
   Check(!PetRenderer.TryReplace(ref r,CharacterForm.AnemoneII,300,out error)&&r==saved&&r.Ready&&pixels.SequenceEqual(PoseMesh.Read(r.Draw(frame))),"missing A2 resource preserves complete A1 renderer");
  }finally{r.Dispose();}
 }
 static void SideRegression(){
  var oldAssembly=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));
  var formType=oldAssembly.GetType("JouchoPet.CharacterForm");
  object old=Activator.CreateInstance(oldAssembly.GetType("JouchoPet.PetRenderer"),F,null,new[]{Enum.ToObject(formType,1)},null);
  try{Call(old,"Resize",300);using(var now=new PetRenderer(CharacterForm.AnemoneII)){now.Resize(300);
   foreach(string kind in new[]{"walk","run","carry","runcarry","leapup","leapdown","archery","sword","cast","lookout"}){
    for(int d=-1;d<=1;d+=2)for(int k=0;k<=8;k++){
     var before=OldRender(old,kind,k/8.0,d,300);var after=Render(now,kind,k/8.0,d,300);var a=PoseMesh.Read(before);var b=PoseMesh.Read(after);
     int padding=kind=="cast"?now.CastPadding:kind=="sword"?now.SwordPadding:kind=="lookout"?now.Reach.Padding:0,changed=0;
     for(int i=0;i<a.Length;i++)if(a[i]!=b[i]){changed++;if(i/after.Width<padding+28+300*.38)throw new Exception("A2 head or upper-body pixels changed / "+kind);}
     if(changed!=0)throw new Exception("Side output changed during front-only edit / "+kind);
    }
    Check(true,"A2 entire side action remains identical / "+kind);
   }
  }}finally{((IDisposable)old).Dispose();}
 }
 static void Preview(){
  Directory.CreateDirectory(Folder+"frames");
  using(var a1=new PetRenderer())using(var a2=new PetRenderer(CharacterForm.AnemoneII)){
   a1.Resize(300);a2.Resize(300);
   using(var comparison=new Bitmap(1080,660))using(var g=Graphics.FromImage(comparison)){
    g.Clear(Color.FromArgb(232,229,222));int i=0;foreach(var r in new[]{a1,a2}){
     var b=r.Draw(Frame("idle",.25,1));g.DrawImageUnscaled(b,i*540+270-b.Width/2,260);g.DrawString(r.Form==CharacterForm.AnemoneI?"A1 / 0.7.47 baseline":"A2 / corrected face",SystemFonts.DefaultFont,Brushes.Black,i*540+25,20);
     var p=CharacterArt.Definition("front");double scale=CharacterArt.Scale(p,300);var face=new RectangleF((float)(b.Width*.5-85*scale),28,(float)(170*scale),(float)(300*.31));
     g.DrawImage(b,new Rectangle(i*540+170,40,200,(int)Math.Round(200*face.Height/face.Width)),face,GraphicsUnit.Pixel);i++;
    }comparison.Save(Folder+"face-comparison.png",ImageFormat.Png);
   }
   foreach(string kind in Kinds)foreach(int d in new[]{-1,1}){
    string name=kind+(d<0?"-left":"-right");
    for(int k=0;k<12;k++){
     double t=k/11.0;using(var scene=new Bitmap(840,460))using(var g=Graphics.FromImage(scene)){
      g.Clear(Color.FromArgb(46,51,61));int column=0;
      foreach(var r in new[]{a1,a2}){
       var b=Render(r,kind,t,d,300);double zoom=.80;int extra=kind=="sword"?r.SwordPadding:kind=="cast"?r.CastPadding:(kind=="lookout"||kind=="beam"||kind=="levitate")?r.Reach.Padding:0;
       float x=(float)(column*420+210-(b.Width/2.0)*zoom),y=(float)(410-(28+300*.98+extra)*zoom);
       var state=g.Save();g.TranslateTransform(x,y);g.ScaleTransform((float)zoom,(float)zoom);
       if(kind=="teleport")UtilityArt.Teleport(g,b,t*1.3,300);else g.DrawImageUnscaled(b,0,0);
       if(kind=="sigil"||kind=="cross"){var pose=Frame(kind,t,d).Daily;MiSigilArt.Draw(g,r.SigilAnchor,360,300,pose,r.SigilFace);}
       if(kind=="archery")ArcheryArt.Bow(g,PointF.Empty,b.Width,300,ArcheryMotion.At(t*4.85,d));
       g.Restore(state);g.DrawString(column==0?"A1":"A2",SystemFonts.DefaultFont,Brushes.White,column*420+20,15);column++;
      }
      g.DrawString(name+" / "+k,SystemFonts.DefaultFont,Brushes.White,350,435);scene.Save(Folder+"frames/"+name+"-"+k.ToString("00")+".png",ImageFormat.Png);
     }
    }
   }
  }
 }
 static void EyeRegression(){
  var old=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));
  using(var before=(Bitmap)old.GetType("JouchoPet.A2FrontArt").GetMethod("Load",F).Invoke(null,null))using(var after=A2FrontArt.Load()){
   var a=PoseMesh.Read(before);var b=PoseMesh.Read(after);int changed=0;
   for(int i=0;i<a.Length;i++)if(a[i]!=b[i]){changed++;if(i/before.Width<306||i/before.Width>=410||i%before.Width<90||i%before.Width>=280)throw new Exception("A2 coat edit escaped lower garment region");}
   Check(changed==0,"approved A2 front including its coat is byte-identical to 0.8.13");
   foreach(int size in new[]{220,300,400})foreach(var location in new[]{new PointF(842,791),new PointF(541,830)}){var original=A2FrontArt.Point(location.X,location.Y,400,size);var moved=A2FrontArt.EyePoint(location.X,location.Y,400,size);Check(moved.X==original.X&&moved.Y<original.Y&&original.Y-moved.Y<size*.004,"small equal upward eye shift preserves spacing / "+size+" / "+location.X);}
  }
  // Validate the inverse maps by looking up the destination of each source lid.
  Func<Func<double,double>,double,double> destination=(map,source)=>{double lo=160,hi=270;for(int k=0;k<50;k++){double mid=(lo+hi)/2;if(map(mid)<source)lo=mid;else hi=mid;}return (lo+hi)/2;};
  double upper=destination(y=>EyeRig.SourceY(346,y,1,4),205),lower=destination(y=>EyeRig.SourceY(346,y,1,4),238);
  Check(upper-205>3*(238-lower)&&upper>228,"exposed eye: upper lid moves more, meeting line lower");
  var covered=new CoveredEyeRig(100,300,new[]{new PointF(10,200),new PointF(20,200)},new[]{new PointF(10,220),new PointF(20,220)});
  upper=destination(y=>covered.SourceY(15,y,1,.8),200);lower=destination(y=>covered.SourceY(15,y,1,.8),220);
  Check(upper-200>3*(220-lower)&&lower>upper&&upper>215,"covered eye: upper/lower travel approximately 80/20, no inversion");
  foreach(double closure in new[]{0,.25,.5,.75,1.0}){double last=Double.NegativeInfinity;for(double y=190;y<=230;y+=.1){double value=covered.SourceY(15,y,closure,.8);if(value<last||Double.IsNaN(value))throw new Exception("Lid mapping folded");last=value;}}Check(true,"partial-to-full closure remains monotonic");
  var formType=old.GetType("JouchoPet.CharacterForm");object previous=Activator.CreateInstance(old.GetType("JouchoPet.PetRenderer"),F,null,new[]{Enum.ToObject(formType,1)},null);
  try{Call(previous,"Resize",300);using(var current=new PetRenderer(CharacterForm.AnemoneII)){current.Resize(300);
   foreach(string action in new[]{"idle","wink","greet","wave","pat"}){
    var before=OldRender(previous,action,.35,1,300);var after=Render(current,action,.35,1,300);var a=PoseMesh.Read(before);var b=PoseMesh.Read(after);
    for(int i=0;i<after.Width*190;i++)if(a[i]!=b[i])throw new Exception("Front head or upper body changed / "+action);
    Check(true,"approved head and upper-body action identical / "+action);
   }
   using(var picture=new Bitmap(1050,640))using(var g=Graphics.FromImage(picture)){
    g.Clear(Color.FromArgb(46,51,61));int col=0;
    foreach(double phase in new[]{0,.08,.35}){int row=0;foreach(bool newer in new[]{false,true}){
     var bitmap=newer?Render(current,"pat",phase,1,300):OldRender(previous,"pat",phase,1,300);
     g.DrawString((newer?"0.8.14":"0.8.13")+" / head pet "+phase,SystemFonts.DefaultFont,Brushes.White,col*350+12,row*320+10);
     g.DrawImage(bitmap,new Rectangle(col*350+10,row*320+40,330,270),new Rectangle(bitmap.Width/2-55,35,110,90),GraphicsUnit.Pixel);row++;
    }col++;}picture.Save(Folder+"pat-comparison.png",ImageFormat.Png);
   }
  }}finally{((IDisposable)previous).Dispose();}
  using(var picture=new Bitmap(1000,540))using(var g=Graphics.FromImage(picture)){
   g.Clear(Color.FromArgb(46,51,61));int col=0;var art=old.GetType("JouchoPet.CharacterArt");
   foreach(string name in new[]{"profile-left","left","right","profile-right"}){var pose=CharacterArt.Definition(name);int row=0;
    foreach(bool newer in new[]{false,true})using(var bitmap=newer?CharacterArt.Load(name,CharacterForm.AnemoneII):(Bitmap)art.GetMethod("Load").Invoke(null,new[]{(object)name,Enum.ToObject(formType,1)})){
     g.DrawString((newer?"0.8.14":"0.8.13")+" / "+name,SystemFonts.DefaultFont,Brushes.White,col*250+10,row*270+10);
     g.DrawImage(bitmap,new Rectangle(col*250+12,row*270+32,225,225),new Rectangle((int)(pose.Anchor-pose.Cell.X)-50,80,100,100),GraphicsUnit.Pixel);row++;
    }col++;
   }picture.Save(Folder+"mouth-comparison.png",ImageFormat.Png);
  }
 }
 [STAThread]static void Main(string[] args){try{
  AnimationParameters.UseFixedValues();Directory.CreateDirectory(Folder);
  if(args.Length>0&&args[0]=="garment"){Regression();Switching();SideRegression();Console.WriteLine("PASS "+checks+" garment regression checks; "+frames+" A1 comparisons.");return;}
  if(args.Length>0&&args[0]=="missing"){MissingResource();return;}
  if(args.Length>0&&args[0]=="preview"){Preview();return;}
  if(args.Length>0&&args[0]=="eyes"){EyeRegression();return;}
  Resources();EyeRegression();Regression();Switching();SideRegression();Console.WriteLine("PASS "+checks+" checks; "+frames+" baseline frame comparisons plus A2/interleaved renders. Offline only.");
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}

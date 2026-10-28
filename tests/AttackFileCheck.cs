using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using JouchoPet;
class AttackFileCheck {
 const string Folder="qa/v0810/";const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 static int checks;
 sealed class Files:IDesktopAttackFiles {
  internal List<DesktopAttackFile> Items=new List<DesktopAttackFile>(),Selected;
  internal int Reads,SelectionReads,Selections,Opens;internal string Opened;internal bool Fail;
  public List<DesktopAttackFile> Read(){Reads++;return new List<DesktopAttackFile>(Items);}
  public List<DesktopAttackFile> ReadForSelection(){SelectionReads++;return new List<DesktopAttackFile>(Items);}
  public int Select(List<DesktopAttackFile> files){Selections++;Selected=files;return files.Count;}
  public ProgramLaunchResult Open(DesktopAttackFile file){Opens++;Opened=file.Path;return new ProgramLaunchResult(!Fail,Fail?"fixture removed":"");}
 }
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
 static object Get(Pet p,string name){return typeof(Pet).GetField(name,F).GetValue(p);}
 static void Set(Pet p,string name,object value){typeof(Pet).GetField(name,F).SetValue(p,value);}
 static void Call(Pet p,string name,params object[] args){typeof(Pet).GetMethod(name,F).Invoke(p,args);}
 static DesktopAttackFile Icon(string name,PointF p,int size=12){return new DesktopAttackFile(name,new Rectangle((int)Math.Round(p.X-size*.5),(int)Math.Round(p.Y-size*.5),size,size));}
 static PointF Middle(PointF[] blade){return RigParts.Mix(RigParts.Mix(blade[0],blade[4],.5),blade[2],.55);}
 static PointF[] Offset(PointF[] a,Point p){return a.Select(x=>RigParts.Add(x,p.X,p.Y)).ToArray();}
 static void Geometry(){
  foreach(int size in new[]{220,300,400})foreach(var origin in new[]{new PointF(450,320),new PointF(-800,-300)}){
   var icons=new List<DesktopAttackFile>();foreach(var d in new[]{new PointF(80,0),new PointF(-80,0),new PointF(0,80),new PointF(0,-80)})icons.Add(Icon("hit"+icons.Count,RigParts.Add(origin,d.X,d.Y)));
   icons.Add(Icon("diagonal",RigParts.Add(origin,80,80)));icons.Add(Icon("far",RigParts.Add(origin,400,0)));
   var hit=AttackGeometry.CrossFiles(icons,origin,300,size);Check(hit.Count==4&&hit.All(x=>x.Path.StartsWith("hit")),"cross covers four arms, excludes bounding-box corners / "+size+" / "+origin);
   var edge=new DesktopAttackFile("edge",new Rectangle((int)origin.X+60,(int)(origin.Y+MiSigilArt.ArmWidth(size))-1,12,12));
   Check(AttackGeometry.CrossFiles(new List<DesktopAttackFile>{edge},origin,300,size).Count==1,"partial icon overlap counts even if center is outside / "+size);
  }
  using(var renderer=new PetRenderer()){renderer.Resize(300);
   foreach(int direction in new[]{-1,1}){
    Func<double,PointF[]> blade=t=>renderer.SwordBlade(SwordMotion.At(t,direction));
    var icon=Icon("blade",Middle(blade(.55)),5);var outside=Icon("outside",new PointF(-2000,-2000));
    Check(AttackGeometry.SwordFile(new List<DesktopAttackFile>{outside},blade,.1,1.0)==null,"empty sword range stays empty / "+direction);
    Check(AttackGeometry.SwordFile(new List<DesktopAttackFile>{icon},blade,.1,1.0)==icon,"swept blade catches target across large timestep / "+direction);
    Check(AttackGeometry.SwordFile(new List<DesktopAttackFile>{icon},blade,0,.45)==null&&AttackGeometry.SwordFile(new List<DesktopAttackFile>{icon},blade,.7,1.1)==null,"reveal/recovery cannot hit / "+direction);
    var first=Icon("first",Middle(blade(.49)),4);var last=Icon("last",Middle(blade(.61)),4);
    string expected=null;
    foreach(int fps in new[]{30,60,120}){
     var files=new Files();files.Items.AddRange(new[]{last,first,icon,outside});var strike=new SwordFileStrike();strike.Begin();
     for(int i=0;i<fps*2;i++)strike.Update(files,blade,i/(double)fps,(i+1)/(double)fps);
     if(expected==null)expected=files.Opened;Check(files.Opens==1&&files.Reads==1&&files.Opened==expected,"one earliest file, same result across fps / "+direction+" / "+fps);
    }
    var fake=new Files();fake.Items.Add(icon);fake.Fail=true;var cancelled=new SwordFileStrike();cancelled.Begin();cancelled.Cancel();cancelled.Update(fake,blade,0,1);Check(fake.Opens==0&&fake.Reads==0,"cancelled strike does not query or open / "+direction);
    cancelled.Begin();cancelled.Update(fake,blade,0,1);cancelled.Update(fake,blade,.4,.7);Check(fake.Opens==1,"failed open is consumed once / "+direction);
   }
  }
 }
 static void Nose(){
  var old=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));var art=old.GetType("JouchoPet.CharacterArt");var form=Enum.ToObject(old.GetType("JouchoPet.CharacterForm"),1);
  foreach(string name in new[]{"profile-left","profile-right","left","right"})using(var a=(Bitmap)art.GetMethod("Load").Invoke(null,new[]{(object)name,form}))using(var b=CharacterArt.Load(name,CharacterForm.AnemoneII)){
   int changed=0;var pose=CharacterArt.Definition(name);int cx=(name=="profile-right"?1656:1298)-(int)pose.Cell.X;
   for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++)if(a.GetPixel(x,y)!=b.GetPixel(x,y)){
    changed++;if(!name.EndsWith("right")||Math.Abs(x-cx)>10||Math.Abs(y-149)>4)throw new Exception("Mouth edit escaped registered region: "+name);
   }
   Check(changed==0,"side artwork and mouth remain identical / "+name);
  }
 }
 static void Runtime(){using(var pet=new Pet())try{
  ((NotifyIcon)Get(pet,"tray")).Visible=false;Set(pet,"settings",Path.GetFullPath(Folder+"attack-test-settings.txt"));Set(pet,"wandering",false);Set(pet,"sleeping",false);
  var files=new Files();Set(pet,"desktopAttackFiles",files);var m=(Motion)Get(pet,"motion");var daily=(DailyMotion)Get(pet,"daily");
  foreach(var form in new[]{CharacterForm.AnemoneI,CharacterForm.AnemoneII}){
   pet.CancelDailyAction();daily.Reset();Call(pet,"CancelSword");m.Reset();string error;Check(pet.TryChangeForm(form,out error),"runtime form / "+form);
   var renderer=(PetRenderer)Get(pet,"renderer");var center=RigParts.Add(renderer.SampleSigilAnchor(0),pet.Left,pet.Top);
   files.Items.Clear();files.Items.Add(Icon("cross-a",RigParts.Add(center,50,0)));files.Items.Add(Icon("cross-b",RigParts.Add(center,0,-50)));files.Items.Add(Icon("corner",RigParts.Add(center,60,60)));
   int before=files.Selections,selectionReads=files.SelectionReads,weaponReads=files.Reads;Check(pet.CrossSigil(),"cross request accepted / "+form);Call(pet,"UpdateDaily",.01);Call(pet,"UpdateDaily",.67);Check(files.Selections==before,"no selection before full expansion / "+form);
   Set(pet,"menuOpen",true);Call(pet,"UpdateDaily",.05);Set(pet,"menuOpen",false);Check(files.Selections==before,"paused cross cannot select / "+form);
   Call(pet,"UpdateDaily",.02);Call(pet,"UpdateDaily",.3);Check(files.Selections==before+1&&files.Selected.Count==2,"cross selects all covered files exactly once / "+form);
   Check(files.SelectionReads==selectionReads+1&&files.Reads==weaponReads,"cross uses selection bounds and bypasses weapon visibility filter / "+form);
   daily.Reset();m.Reset();pet.CrossSigil();Call(pet,"UpdateDaily",.01);Call(pet,"UpdateDaily",.4);pet.CancelDailyAction();Call(pet,"UpdateDaily",1.0);Check(files.Selections==before+1,"cancelled cross cannot select / "+form);
   daily.Reset();m.Reset();pet.MiSigilPreview();Call(pet,"UpdateDaily",.01);Call(pet,"UpdateDaily",.9);Check(files.Selections==before+1,"eight-ray sigil has no new selection effect / "+form);
   daily.Reset();m.Reset();pet.CrossSigil();Call(pet,"UpdateDaily",.01);Set(pet,"sigilLayout","changed");Call(pet,"UpdateDaily",.9);Check(files.Selections==before+1,"display change cancels cross selection / "+form);daily.Reset();
   foreach(int direction in new[]{-1,1}){
    files.Items.Clear();var blade=Offset(renderer.SwordBlade(SwordMotion.At(.55,direction)),pet.Location);files.Items.Add(Icon("sword-hit",Middle(blade),8));int opens=files.Opens;
    m.FollowPush(0,0,direction,300);pet.SwordSlash(new PointF(-9999,-9999),direction);Set(pet,"swordEffects",false);Call(pet,"UpdateSword",.01);Call(pet,"UpdateSword",.46);Check(files.Opens==opens,"sword reveal does not open / "+form+" / "+direction);
    Call(pet,"UpdateSword",.2);Call(pet,"UpdateSword",1.0);Check(files.Opens==opens+1&&files.Opened=="sword-hit","sword uses blade range, not requested point or effects / "+form+" / "+direction);
    files.Items.Clear();m.FollowPush(0,0,direction,300);pet.SwordSlash(PointF.Empty,direction);Call(pet,"UpdateSword",.01);Call(pet,"UpdateSword",2.0);Check(files.Opens==opens+1&&!((SwordMotion)Get(pet,"sword")).Busy,"empty sword range completes normal swing / "+form+" / "+direction);
    files.Items.Add(Icon("cancel",Middle(blade),8));m.FollowPush(0,0,direction,300);pet.SwordSlash(PointF.Empty,direction);Call(pet,"UpdateSword",.01);Call(pet,"CancelSword");Call(pet,"UpdateSword",1.0);Check(files.Opens==opens+1,"cancelled sword cannot open / "+form+" / "+direction);
   }
  }
 }finally{typeof(Form).GetMethod("OnFormClosed",F).Invoke(pet,new object[]{new FormClosedEventArgs(CloseReason.None)});}}
 [STAThread]static void Main(){try{AnimationParameters.UseFixedValues();Directory.CreateDirectory(Folder);Geometry();Nose();Runtime();Console.WriteLine("PASS "+checks+" attack/nose checks; fake selection and open backends only.");}catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}

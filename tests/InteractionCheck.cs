using System;using System.IO;using System.Drawing;using System.Drawing.Imaging;using System.Drawing.Drawing2D;using System.Reflection;using JouchoPet;
class InteractionCheck {
 static readonly string Folder="qa/v0714/";
 static readonly BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static int count;
 static void Check(bool value,string name){if(!value)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static void Main(string[] args){try{AnimationParameters.UseFixedValues();Directory.CreateDirectory(Folder);Eyes();EyeChecks();Console.WriteLine("All "+count+" selected checks passed.");}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static Assembly Baseline(){return Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));}
 static void Sword(){var old=Baseline();var at=old.GetType("JouchoPet.SwordMotion").GetMethod("At",Flags);bool same=true,held=true,attached=true;
  Size c=PetRenderer.CanvasSize(300);using(var side=new SideWalk(c.Width,c.Height,300))using(var rig=new SwordRig(c.Width,c.Height,300,side))foreach(int face in new[]{-1,1}){
   var peak=rig.Sample(SwordMotion.At(.37,face));
   for(int k=0;k<=226;k++){double t=k*.005;var p=SwordMotion.At(t,face);object oldPose=at.Invoke(null,new object[]{t,face});
    foreach(var f in typeof(SwordPose).GetFields(Flags))if(f.Name!="Age"&&f.Name!="Opacity"){
     object a=f.GetValue(p),b=oldPose.GetType().GetField(f.Name,Flags).GetValue(oldPose);
     same&=a is double?Math.Abs((double)a-(double)b)<1e-10:a.Equals(b);
    }
    var bones=rig.Sample(p);attached&=Math.Abs(RigParts.Length(bones.Wrist,bones.Grip)-10.5)<.001;
    if(t>=.37&&t<=.47)held&=RigParts.Length(peak.Grip,bones.Grip)<.0001&&RigParts.Length(peak.Tip,bones.Tip)<.0001;
   }
   using(var sheet=new Bitmap(500*5,640))using(var g=Graphics.FromImage(sheet)){
    g.Clear(Color.FromArgb(45,50,62));g.InterpolationMode=InterpolationMode.HighQualityBicubic;double[] times={.37,.42,.47,.56,.99};
    for(int i=0;i<times.Length;i++){var b=rig.Draw(SwordMotion.At(times[i],face),true);g.DrawImage(b,new RectangleF(i*500,40,500,500*650f/550),new RectangleF(rig.Padding-100,rig.Padding-140,550,650),GraphicsUnit.Pixel);g.DrawString(times[i].ToString("F2")+"s",SystemFonts.DefaultFont,Brushes.White,i*500+10,15);}
    sheet.Save(Folder+"sword-"+(face<0?"left":"right")+"-hold.png",ImageFormat.Png);
   }
  }
  Check(same&&attached,"0.7.5 pose curves, blade binding and grip preserved under the new timeline");
  Check(held&&SwordMotion.At(.37,1).Opacity==0&&Math.Abs(SwordMotion.At(.42,1).Opacity-.5)<1e-8&&SwordMotion.At(.47,1).Opacity>.99999,"highest pose held for 0.10s while sword opacity advances from zero to full");
  bool hits=true;foreach(int fps in new[]{30,60,120}){var action=new SwordMotion();int n=0;double hitAge=0;action.HitFrame+=(s,e)=>{n++;hitAge=action.Age;};var m=new Motion();m.FollowPush(0,0,1,300);action.Request(1);hits&=action.Begin(m,true);for(int k=0;k<fps*2;k++)action.Update(1.0/fps);hits&=n==1&&hitAge>=.60&&hitAge<=.60+1.0/fps+.00001&&!action.Busy;}
  Check(hits&&Math.Abs(SwordMotion.Duration-1.13)<1e-8,"HitFrame shifted to 0.60s once at 30/60/120fps; animation ends at 1.13s");
 }
 static bool Rose(int value){uint p=(uint)value;int r=(int)(p>>16&255),g=(int)(p>>8&255),b=(int)(p&255);return (p>>24)>180&&r>145&&r-g>25&&r-b>55&&r-g>55&&g<150&&b<125;}
 static object OldFrame(Assembly old,RenderFrame frame){var t=old.GetType("JouchoPet.RenderFrame");object value=Activator.CreateInstance(t);foreach(var f in typeof(RenderFrame).GetFields())if(f.FieldType.IsPrimitive||f.FieldType==typeof(string))t.GetField(f.Name).SetValue(value,f.GetValue(frame));return value;}
 static void EyeChecks(){var old=Baseline();var type=old.GetType("JouchoPet.PetRenderer");bool eyes=true,local=true,unchanged=true,map=true;
  foreach(int size in new[]{220,300,400})using(var renderer=new PetRenderer()){
   Size c=PetRenderer.CanvasSize(size);renderer.Resize(size);object prior=Activator.CreateInstance(type,true);type.GetMethod("Resize").Invoke(prior,new object[]{size});
   try{
    foreach(bool wave in new[]{false,true}){
     var f=new RenderFrame{MotionState="idle",Time=.2,Settle=1,Affection=wave?0:1,ClosedEyes=wave?0:1,PatSide=1,Greeting=wave?1:0,WaveBlend=wave?1:0,WavePhase=.7};
     int[] a=HairLength.ReadRaw((Bitmap)type.GetMethod("Draw").Invoke(prior,new[]{OldFrame(old,f)})),b=HairLength.ReadRaw(renderer.Draw(f));int opened=0,closed=0,changed=0;
     for(int k=0;k<a.Length;k++)if(a[k]!=b[k]){changed++;if((a[k]&255)>(b[k]&255)+10)opened++;if(Rose(b[k])&&!Rose(a[k]))closed++;double x=k%c.Width-c.Width*.5,y=k/c.Width-28;local&=x>-size*.12&&x<size*.02&&y>size*.16&&y<size*.30;}
     Console.WriteLine("Eye / "+size+" / "+(wave?"wave":"pat")+" darkened lash pixels="+opened+" new="+closed+" changed="+changed);eyes&=opened>0&&closed<=Math.Ceiling(opened*.25)&&changed>0;
    }
    if(size==300)foreach(var f in new[]{new RenderFrame{MotionState="idle"},new RenderFrame{MotionState="idle",Blink=1},new RenderFrame{MotionState="idle",RestAmount=.35,ClosedEyes=1},new RenderFrame{MotionState="idle",RestAmount=1,ClosedEyes=1}}){var a=HairLength.ReadRaw((Bitmap)type.GetMethod("Draw").Invoke(prior,new[]{OldFrame(old,f)}));var b=HairLength.ReadRaw(renderer.Draw(f));for(int k=0;k<a.Length;k++)unchanged&=a[k]==b[k];}
   }finally{((IDisposable)prior).Dispose();}
   using(var art=CharacterArt.Normalized("front",c.Width,c.Height,size)){var eye=new CoveredEyeRig(HairLength.ReadRaw(art),c.Width,c.Height,size,true);foreach(double amount in new[]{0.0,.25,.5,.75,1.0})for(double x=c.Width*.5-size*.1;x<c.Width*.5;x+=.5){double last=0;for(double y=28+size*.15;y<28+size*.30;y+=.2){double source=eye.SourceY(x,y,amount);map&=source>=last;if(amount==0||y<28+size*.20)map&=source==y;last=source;}}}
  }
  Check(eyes&&local,"pat and wave thicken dark lashes without new red pixels at 220/300/400; changes confined to that eye");
  Check(map,"eye closure is monotone, neutral is exact, upper fringe stays fixed");
  Check(unchanged,"idle, ordinary blink and two frozen rest samples are pixel-identical to 0.7.13");
 }
 static void Eyes(){const int size=400;Size c=PetRenderer.CanvasSize(size);using(var renderer=new PetRenderer()){
  renderer.Resize(size);
  using(var sheet=new Bitmap(1600,650))using(var g=Graphics.FromImage(sheet)){
   g.Clear(Color.FromArgb(45,50,62));g.InterpolationMode=InterpolationMode.NearestNeighbor;
   for(int i=0;i<4;i++){
    var f=new RenderFrame{MotionState="idle",Time=0,Settle=1};
    if(i==1){f.ClosedEyes=1;}if(i==2){f.Affection=1;f.ClosedEyes=1;f.PatSide=1;}if(i==3){f.Greeting=1;f.WaveBlend=1;f.WavePhase=.7;}
    var b=renderer.Draw(f);b.Save(Folder+"face-"+i+".png");
    g.DrawImage(b,new Rectangle(i*400,25,400,300),new RectangleF((float)(c.Width*.5-size*.15),28+(float)size*.12f,(float)size*.30f,(float)size*.225f),GraphicsUnit.Pixel);
    g.DrawImage(b,new RectangleF(i*400+50,330,300,300f*c.Height/c.Width));g.DrawString(new[]{"Idle","Blink","Pat","Wave"}[i],SystemFonts.DefaultFont,Brushes.White,i*400+5,5);
   }sheet.Save(Folder+"eye-preview.png",ImageFormat.Png);
  }
 }}
}

using System;using System.IO;using System.Drawing;using System.Drawing.Imaging;using System.Drawing.Drawing2D;using System.Reflection;using JouchoPet;
class RevisionCheck {
 static int count;static readonly Color Background=Color.FromArgb(45,50,62);
 static void Check(bool v,string message){if(!v)throw new Exception(message);count++;Console.WriteLine("PASS "+message);}
 static void Main(string[] args){try{AnimationParameters.UseFixedValues();if(args.Length>0&&args[0]=="quick"){Focused();}else{Directory.CreateDirectory("qa/v0705");Directory.CreateDirectory("qa/v0707");if(args.Length==0){Hair();Transparency();Entry();}Arrow();}Console.WriteLine("All "+count+" selected revision checks passed.");}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static bool Same(object a,object b){return a is double?Math.Abs((double)a-(double)b)<1e-10:a.Equals(b);}
 static void Focused(){
  Directory.CreateDirectory("qa/v0711");
  var old=Assembly.LoadFile(Path.GetFullPath("qa/v0706/baseline/IsekaijouchoPet.exe"));const BindingFlags flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  Size c=PetRenderer.CanvasSize(300);Type sideType=old.GetType("JouchoPet.SideWalk"),rigType=old.GetType("JouchoPet.SwordRig");object oldSide=Activator.CreateInstance(sideType,flags,null,new object[]{c.Width,c.Height,300},null);object oldRig=Activator.CreateInstance(rigType,flags,null,new object[]{c.Width,c.Height,300.0,oldSide},null);var oldAt=old.GetType("JouchoPet.SwordMotion").GetMethod("At",flags);
  bool arms=true,angle=true,grip=true,effects=true,hidden=true,motion=true;double[] times={.12,.42,.60,.78,1.05};
  try{using(var side=new SideWalk(c.Width,c.Height,300))using(var rig=new SwordRig(c.Width,c.Height,300,side))foreach(int facing in new[]{-1,1}){
   for(int k=0;k<=226;k++){double t=k*.005;object prior=oldAt.Invoke(null,new object[]{SwordMotion.PoseTime(t),facing});var current=SwordMotion.At(t,facing);
    foreach(var field in typeof(SwordPose).GetFields(flags))if(field.Name!="Opacity"&&field.Name!="Age")motion&=Same(field.GetValue(current),prior.GetType().GetField(field.Name,flags).GetValue(prior));
   }
   foreach(double t in new[]{.075,.15,.35,.369}){var p=SwordMotion.At(t,facing);hidden&=p.Opacity==0;var a=HairLength.ReadRaw(rig.Draw(p,false));var b=HairLength.ReadRaw(rig.Draw(p,true));for(int k=0;k<a.Length;k++)hidden&=a[k]==b[k];}
   hidden&=SwordMotion.At(.37,facing).Opacity==0&&SwordMotion.At(.38,facing).Opacity>0&&SwordMotion.At(.47,facing).Opacity>.9999;
   string name="sword-"+(facing<0?"left":"right");
   using(var sheet=new Bitmap(500*5,640))using(var g=Graphics.FromImage(sheet))using(var close=new Bitmap(400*5,400))using(var cg=Graphics.FromImage(close)){
    g.Clear(Background);cg.Clear(Background);g.InterpolationMode=InterpolationMode.HighQualityBicubic;cg.InterpolationMode=InterpolationMode.NearestNeighbor;
    for(int i=0;i<times.Length;i++){var p=SwordMotion.At(times[i],facing);var b=rig.Sample(p);object prior=rigType.GetMethod("Sample",flags).Invoke(oldRig,new[]{oldAt.Invoke(null,new object[]{SwordMotion.PoseTime(times[i]),facing})});
     foreach(string field in new[]{"Shoulder","Elbow","Wrist","Grip","Tip"})arms&=RigParts.Length((PointF)typeof(SwordRig.Bones).GetField(field,flags).GetValue(b),(PointF)prior.GetType().GetField(field,flags).GetValue(prior))<.0001;
     angle&=Math.Abs(b.Angle-(double)prior.GetType().GetField("Angle",flags).GetValue(prior))<1e-10;
     object oldPose=oldAt.Invoke(null,new object[]{SwordMotion.PoseTime(times[i]),facing});oldPose.GetType().GetField("Opacity",flags).SetValue(oldPose,p.Opacity);
     var oldPixels=HairLength.ReadRaw((Bitmap)rigType.GetMethod("Draw",flags).Invoke(oldRig,new object[]{oldPose,false,false}));var newPixels=HairLength.ReadRaw(rig.Draw(p,false));
     for(int k=0;k<oldPixels.Length;k++)angle&=oldPixels[k]==newPixels[k];
     grip&=Math.Abs(RigParts.Length(b.Wrist,b.Grip)-300*.035)<.001&&Math.Abs(RigParts.Length(b.Grip,b.Tip)-300*.805*.8*.75/.96)<.001;
     var frame=rig.Draw(p,false);g.DrawImage(frame,new RectangleF(i*500,40,500,500*650f/550),new RectangleF(rig.Padding-100,rig.Padding-140,550,650),GraphicsUnit.Pixel);g.DrawString(times[i].ToString("F2")+"s",SystemFonts.DefaultFont,Brushes.White,i*500+10,15);
     cg.DrawImage(frame,new Rectangle(i*400,25,400,375),new RectangleF(b.Grip.X+rig.Padding-40,b.Grip.Y+rig.Padding-37.5f,80,75),GraphicsUnit.Pixel);cg.DrawString(times[i].ToString("F2")+"s",SystemFonts.DefaultFont,Brushes.White,i*400+5,5);
    }sheet.Save("qa/v0711/"+name+"-keyposes.png",ImageFormat.Png);close.Save("qa/v0711/"+name+"-grip.png",ImageFormat.Png);
   }
   using(var sheet=new Bitmap(500*3,640))using(var g=Graphics.FromImage(sheet)){
    g.Clear(Background);g.InterpolationMode=InterpolationMode.HighQualityBicubic;double[] ages={.42,.56,1.0};
    for(int i=0;i<ages.Length;i++){var p=SwordMotion.At(ages[i],facing);var frame=rig.Draw(p,true);g.DrawImage(frame,new RectangleF(i*500,40,500,500*650f/550),new RectangleF(rig.Padding-100,rig.Padding-140,550,650),GraphicsUnit.Pixel);g.DrawString((i==0?"Forming stars":i==1?"Blade-tip trail":"Dispersing stars")+" "+ages[i].ToString("F3")+"s",SystemFonts.DefaultFont,Brushes.White,i*500+10,15);}
    sheet.Save("qa/v0711/"+name+"-effects.png",ImageFormat.Png);
   }
   var strike=SwordMotion.At(.56,facing);var clean=HairLength.ReadRaw(rig.Draw(strike,false));var effectFrame=rig.Draw(strike,true);var fx=HairLength.ReadRaw(effectFrame);int changed=0;
   var trail=new PointF[18];for(int k=0;k<18;k++)trail[k]=rig.Sample(SwordMotion.At(.49+.07*k/17,facing)).Tip;
   for(int k=0;k<fx.Length;k++)if(fx[k]!=clean[k]){changed++;double x=k%effectFrame.Width-rig.Padding,y=k/effectFrame.Width-rig.Padding,min=double.MaxValue;
    for(int j=1;j<trail.Length;j++){double dx=trail[j].X-trail[j-1].X,dy=trail[j].Y-trail[j-1].Y,den=dx*dx+dy*dy,t=den<1e-10?0:Math.Max(0,Math.Min(1,((x-trail[j-1].X)*dx+(y-trail[j-1].Y)*dy)/den));double ex=x-trail[j-1].X-t*dx,ey=y-trail[j-1].Y-t*dy;min=Math.Min(min,Math.Sqrt(ex*ex+ey*ey));}effects&=min<4.5;
   }effects&=changed>10;
  }}finally{((IDisposable)oldRig).Dispose();((IDisposable)oldSide).Dispose();}
  Check(motion,"all non-visibility pose fields match 0.7.5 at 200 Hz in both directions");
  Check(hidden,"no sword or effects before raised pose; reveal starts at 0.37s and completes at 0.47s");
  Check(arms,"shoulder/elbow/wrist/grip/tip exactly match 0.7.5 in ten key poses");
  Check(angle,"blade angles and full clean renders match 0.7.5 except requested sword visibility");
  Check(grip,"hand and rigid blade stay attached in both directions");
  Check(effects,"rendered blue trail follows restored 0.7.5 sword-tip arc in both directions");
  using(var sheet=new Bitmap(1350,600*3))using(var g=Graphics.FromImage(sheet)){g.Clear(Background);g.SmoothingMode=SmoothingMode.AntiAlias;double[] ages={.13,.4,.7};for(int i=0;i<3;i++){var a=Shot(1);a.Update(ages[i]);var state=g.Save();g.TranslateTransform(0,600*i);ArcheryArt.Projectile(g,a);g.Restore(state);g.DrawString(ages[i].ToString("F2")+"s",SystemFonts.DefaultFont,Brushes.White,20,600*i+20);}sheet.Save("qa/v0711/arrow-trail.png",ImageFormat.Png);}
 }
 static void Checker(Graphics g,Rectangle r){using(var brush=new SolidBrush(Color.FromArgb(186,194,205))){g.FillRectangle(Brushes.White,r);for(int y=r.Y;y<r.Bottom;y+=12)for(int x=r.X;x<r.Right;x+=12)if(((x-r.X)/12+(y-r.Y)/12)%2==0)g.FillRectangle(brush,x,y,12,12);}}
 static void Transparency(){
  string[] names={"profile-left","left","front","right","profile-right","push-left","push-right","crouch","sleep","pat"};Size c=PetRenderer.CanvasSize(400);
  using(var sheet=new Bitmap(c.Width*3,c.Height*names.Length))using(var g=Graphics.FromImage(sheet)){
   for(int i=0;i<names.Length;i++)using(var b=CharacterArt.Normalized(names[i],c.Width,c.Height,400)){
    b.Save("qa/v0705/hair-transparent-"+names[i]+".png",ImageFormat.Png);
    var pixels=HairLength.ReadRaw(b);foreach(int pixel in pixels)if(((uint)pixel>>24)==0&&pixel!=0)throw new Exception("nonzero colour at alpha zero / "+names[i]);
    Check(true,"zero-alpha pixels contain no matte / "+names[i]);
    for(int j=0;j<3;j++){Rectangle cell=new Rectangle(j*c.Width,i*c.Height,c.Width,c.Height);if(j==0)Checker(g,cell);else g.FillRectangle(j==1?Brushes.White:Brushes.Black,cell);g.DrawImageUnscaled(b,cell.X,cell.Y);g.DrawString(names[i],SystemFonts.DefaultFont,j==2?Brushes.White:Brushes.Black,cell.X+3,cell.Y+3);}
   }sheet.Save("qa/v0705/transparency-backgrounds.png",ImageFormat.Png);
  }
  string[] probes={"front","left","profile-right","push-left","sleep","crouch","pat"};
  PointF[] points={new PointF(973,380),new PointF(555,390),new PointF(1540,390),new PointF(200,800),new PointF(1340,840),new PointF(950,827),new PointF(1664,811)};
  for(int i=0;i<probes.Length;i++){var p=CharacterArt.Definition(probes[i]);int x=(int)Math.Round(c.Width*.5+(points[i].X-p.Anchor)*CharacterArt.Scale(p,400)),y=(int)Math.Round(CharacterArt.Y(p,points[i].Y,400));
   HairLength.Enabled=false;using(var old=CharacterArt.Normalized(probes[i],c.Width,c.Height,400)){HairLength.Enabled=true;using(var b=CharacterArt.Normalized(probes[i],c.Width,c.Height,400))Check(old.GetPixel(x,y).A>180&&b.GetPixel(x,y).A==0,"former lower hair is truly transparent / "+probes[i]);}
  }
 }
 static void Hair(){
  string[] poses={"profile-left","left","front","right","profile-right","push-left","push-right","crouch","sleep","pat"};
  Size c=PetRenderer.CanvasSize(300);
  using(var sheet=new Bitmap(c.Width*5,c.Height*4))using(var g=Graphics.FromImage(sheet)){
   g.Clear(Background);
   for(int i=0;i<poses.Length;i++){
    HairLength.Enabled=false;using(var before=CharacterArt.Normalized(poses[i],c.Width,c.Height,300)){
     HairLength.Enabled=true;using(var after=CharacterArt.Normalized(poses[i],c.Width,c.Height,300)){
      int[] a=HairLength.ReadRaw(before),b=HairLength.ReadRaw(after);var def=CharacterArt.Definition(poses[i]);int neck=(int)Math.Floor(28+300*(CharacterArt.Top(def)+.3));int changed=0;
      for(int y=0;y<c.Height;y++)for(int x=0;x<c.Width;x++){int k=y*c.Width+x;if(a[k]!=b[k]){if(y<neck)throw new Exception("head changed "+poses[i]);changed++;}}
      Check(changed>50,"head exact; rear hair shortened / "+poses[i]);
      g.DrawImageUnscaled(before,i%5*c.Width,i/5*2*c.Height);g.DrawImageUnscaled(after,i%5*c.Width,(i/5*2+1)*c.Height);
      g.DrawString(poses[i]+" BEFORE",SystemFonts.DefaultFont,Brushes.White,i%5*c.Width+5,i/5*2*c.Height+3);
      g.DrawString(poses[i]+" AFTER",SystemFonts.DefaultFont,Brushes.White,i%5*c.Width+5,(i/5*2+1)*c.Height+3);
     }
    }
   }
   sheet.Save("qa/v0705/hair-all-views.png",ImageFormat.Png);
  }
  using(var renderer=new PetRenderer()){
   renderer.Resize(300);
   using(var sheet=new Bitmap(c.Width*6,c.Height*3))using(var g=Graphics.FromImage(sheet)){
    g.Clear(Background);
    for(int i=0;i<9;i++){var f=new RenderFrame{MotionState="idle",Settle=1,RestAmount=i/8.0,ClosedEyes=i/8.0};var b=renderer.Draw(f);g.DrawImageUnscaled(b,i%6*c.Width,i/6*c.Height);g.DrawString("sleep "+i+"/8",SystemFonts.DefaultFont,Brushes.White,i%6*c.Width+4,i/6*c.Height+4);}
    for(int i=0;i<9;i++){var f=new RenderFrame{Time=.4,MotionState="walk",Facing=i%2==0?-1:1,Settle=0,Speed=36,Gait=.7};string title;
     if(i<2){title="run";f.RunBlend=1;}else if(i<4){title="carry";f.Carry=1;}else if(i<6){title="bow";f.Archery=true;f.BowPose=ArcheryMotion.At(2.3,f.Facing);}else if(i<8){title="leap";f.Leaping=true;f.Leap=LeapMotion.At(.6,f.Facing);}else{title="wave";f.MotionState="idle";f.Facing=0;f.Greeting=1;f.WaveBlend=1;f.WavePhase=.4;}
     int j=i+9;g.DrawImageUnscaled(renderer.Draw(f),j%6*c.Width,j/6*c.Height);g.DrawString(title,SystemFonts.DefaultFont,Brushes.White,j%6*c.Width+4,j/6*c.Height+4);
    }
    sheet.Save("qa/v0705/hair-actions.png",ImageFormat.Png);
   }
   var field=typeof(PetRenderer).GetField("swordsman",BindingFlags.Instance|BindingFlags.NonPublic);Check(field.GetValue(renderer)!=null,"sword rig is prepared before first slash frame");
  }
 }
 static void Entry(){foreach(int facing in new[]{-1,1}){
  var m=new Motion();m.FollowPush(0,0,facing,300);m.State="walk";var s=new SwordMotion();s.Request(facing);
  Check(s.Begin(m,true),"planted walking endpoint enters before brake/settle / "+facing);
  foreach(bool light in new[]{false,true}){
   m.Reset();m.SelectLightTurn=()=>light;s.Request(facing);bool entered=false;
   for(int i=0;i<180&&!entered;i++){if(s.Begin(m,true)){entered=true;break;}m.Update(1.0/60,s.ApproachDirection(m,true),300,true);if(m.Facing==facing&&m.State=="walk"){Check(s.Begin(m,true),"turn endpoint enters same tick / "+facing+" / "+light);entered=true;}}
   Check(entered,"front-to-side entry completes / "+facing+" / "+light);
  }
 }}
 static MagicArrow Shot(int facing){return new MagicArrow(new PointF(facing>0?250:1100,430),facing,new Rectangle(0,0,1350,600),300);}
 static void Arrow(){foreach(int facing in new[]{-1,1}){
  var arrow=Shot(facing);Check(Math.Abs(arrow.FlightTime-RigParts.Length(arrow.Origin,arrow.Impact)/(300*12.8))<.000001,"arrow speed preserved / "+facing);
  Check(arrow.TrailPoint(0)==arrow.Origin&&arrow.TrailPoint(arrow.FlightTime)==arrow.Impact,"trail begins at launch tip and ends at impact / "+facing);
  var mid=Shot(facing);mid.Update(mid.FlightTime*.45);Check(RigParts.Length(mid.TrailPoint(mid.Age),mid.Tip)<.0001,"live trail endpoint coincides with arrow tip / "+facing);
  using(var b=new Bitmap(1350,600))using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);ArcheryArt.Trail(g,mid);PointF probe=RigParts.Add(mid.Tip,-mid.Direction.X*2,-mid.Direction.Y*2);Check(b.GetPixel((int)Math.Round(probe.X),(int)Math.Round(probe.Y)).A>80,"visible trail reaches arrowhead instead of stopping at nock / "+facing);}
  arrow.Update(MagicArrow.TrailHold);Check(arrow.TrailOpacity(0)==1,"trail holds for configured interval / "+facing);arrow.Update(MagicArrow.TrailFade*.5);Check(arrow.TrailOpacity(0)>.4&&arrow.TrailOpacity(0)<.6,"trail fades at configured midpoint / "+facing);arrow.Update(10);Check(arrow.Done&&Math.Abs(arrow.TrailOpacity(arrow.FlightTime))<1e-9,"last trail sample fully fades / "+facing);
  foreach(double t in new[]{.03,.13,.5,1.1,1.3,1.6}){
   arrow=Shot(facing);arrow.Update(t);RectangleF bounds=ArcheryArt.ProjectileBounds(arrow);
   using(var b=new Bitmap(1600,900))using(var g=Graphics.FromImage(b)){
    g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.TranslateTransform(100,100);ArcheryArt.Projectile(g,arrow);
    int[] pixels=PoseMesh.Read(b);for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)if((uint)pixels[y*b.Width+x]>>24>8&&!bounds.Contains(x-100,y-100))throw new Exception("trail/effect clipped by bounds");
   }
  }Check(true,"flight and fading tail fit overlay bounds / "+facing);
 }
 using(var sheet=new Bitmap(1350,600*4))using(var g=Graphics.FromImage(sheet)){
  g.Clear(Background);g.SmoothingMode=SmoothingMode.AntiAlias;double[] ages={.13,.7,1.15,1.4};
  for(int i=0;i<ages.Length;i++){var a=Shot(1);a.Update(ages[i]);var state=g.Save();g.TranslateTransform(0,i*600);ArcheryArt.Projectile(g,a);g.Restore(state);g.DrawString("arrow age "+ages[i].ToString("F2")+" s",SystemFonts.DefaultFont,Brushes.White,20,i*600+20);}
  sheet.Save("qa/v0707/arrow-trail.png",ImageFormat.Png);
 }
 }
}

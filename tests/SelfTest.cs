using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JouchoPet;
class SelfTest {
 static int count;
 static void Assert(bool v,string label){if(!v)throw new Exception("FAIL: "+label);Console.WriteLine("PASS: "+label);count++;}
 static Dictionary<string,object> Args(string s){return Json.Object(s);}
 static void Main(){
  string testRoot=Path.Combine(Path.GetTempPath(),"joucho-test-"+Guid.NewGuid().ToString("N"));
  string root=Path.Combine(testRoot,"files");Directory.CreateDirectory(root);
  // This test executable must never use the person's real settings/backup folder.
  // The runtime configuration class remains unchanged; isolate its field here only.
  typeof(ApiSettings).GetField("AppDir").SetValue(null,Path.Combine(testRoot,"app-state"));
  try {
   bool allow=false;var f=new FileTools(root,delegate(string a,string b,string c){return allow;});
   Assert(f.Execute("list_files",Args("{}" )).Contains("error"),"disabled tools reject access");f.Enabled=true;
   foreach(string p in new[]{"../outside.txt","C:\\outside.txt","a.txt:secret","CON.txt","foo.\\a.txt"}){bool rejected=false;try{f.Resolve(p);}catch(Exception){rejected=true;}Assert(rejected,"blocked path "+p);}
   Assert(f.Execute("write_text",Args("{\"path\":\"note.txt\",\"content\":\"hello\",\"mode\":\"create\"}")).Contains("cancelled")&&!File.Exists(Path.Combine(root,"note.txt")),"cancel creates nothing");allow=true;
   Assert(f.Execute("write_text",Args("{\"path\":\"note.txt\",\"content\":\"hello\",\"mode\":\"create\"}")).Contains("true"),"create text");
   Assert(File.ReadAllText(Path.Combine(root,"note.txt"))=="hello","created content exact");
   Assert(f.Execute("read_text",Args("{\"path\":\"note.txt\"}")).Contains("hello"),"approved read");
   string replaced=f.Execute("write_text",Args("{\"path\":\"note.txt\",\"content\":\"new\",\"mode\":\"replace\"}"));
   Assert(replaced.Contains("true"),"replacement succeeds before undo: "+replaced);f.Undo();Assert(File.ReadAllText(Path.Combine(root,"note.txt"))=="hello","replace undo restores original");
   f.Execute("move_file",Args("{\"source\":\"note.txt\",\"destination\":\"renamed.txt\"}"));Assert(File.Exists(Path.Combine(root,"renamed.txt")),"rename executed");f.Undo();Assert(File.Exists(Path.Combine(root,"note.txt")),"rename undo");
   f.Execute("write_text",Args("{\"path\":\"note.txt\",\"content\":\"new\",\"mode\":\"replace\"}"));File.WriteAllText(Path.Combine(root,"note.txt"),"external");f.Undo();Assert(File.ReadAllText(Path.Combine(root,"note.txt"))=="external","undo preserves external edits");
   var raced=new FileTools(root,delegate(string a,string b,string c){File.WriteAllText(Path.Combine(root,"note.txt"),"changed during review");return true;});raced.Enabled=true;
   Assert(raced.Execute("write_text",Args("{\"path\":\"note.txt\",\"content\":\"AI\",\"mode\":\"replace\"}")).Contains("error"),"changes during review block overwrite");
   var snap=new FileTools(root,delegate(string a,string b,string c){File.WriteAllText(Path.Combine(root,"note.txt"),"later data");return true;});snap.Enabled=true;
   string read=snap.Execute("read_text",Args("{\"path\":\"note.txt\"}"));Assert(read.Contains("changed during review")&&!read.Contains("later data"),"read sends reviewed snapshot");
   Assert(ApiClient.CompletionUri("https://example.com/v1/").AbsoluteUri=="https://example.com/v1/chat/completions","base URL normalized");
   bool insecure=false;try{ApiClient.CompletionUri("http://example.com/v1");}catch(ArgumentException){insecure=true;}Assert(insecure,"remote HTTP blocked");
   Assert(FileTools.Schemas().Length==5,"five tool schemas serialize");Json.Encode(FileTools.Schemas());
   Assert(PatResponse.Strength(-1)==0 && PatResponse.Strength(3.2)==0,"pat response returns to neutral");
   Assert(PatResponse.Strength(1)>0.99,"pat acknowledgement holds briefly");
   Assert(PatResponse.EyeClosure(-1)==0 && PatResponse.EyeClosure(3.2)==0,"closed eyes return to neutral");
   Assert(PatResponse.EyeClosure(0.5)==1 && PatResponse.EyeClosure(2.05)==1,"eyes fully closed during held pat");
   Assert(PatResponse.EyeClosure(0.3)>0 && PatResponse.EyeClosure(0.3)<1,"eyes close gradually");
   Assert(PatResponse.EyeClosure(2.4)>0 && PatResponse.EyeClosure(2.4)<1,"eyes reopen gradually");
   Assert(Math.Abs(EyeRig.SourceY(346,222,1)-199)<1e-9,"upper eyelashes travel with closing lid");
   Assert(Math.Abs(EyeRig.SourceY(346,228,1)-205)<1e-9,"upper lash edge reaches closed position");
   bool eyeMonotone=true;
   foreach(double closure in new[]{0.0,0.25,0.5,0.75,1.0}) {
    double last=EyeRig.SourceY(346,180,closure);
    for(double yy=180.1;yy<259;yy+=0.1) {
     double mapped=EyeRig.SourceY(346,yy,closure);eyeMonotone &= mapped>=last;last=mapped;
    }
   }
   Assert(eyeMonotone,"eyelid mapping never folds or duplicates eyelashes");
   Assert(EyeRig.SourceY(280,220,1)==220 && EyeRig.SourceY(346,170,1)==170,"fringe and eyebrow outside eye remain fixed");
   double epsilon=0.000001,shared=PoseInterpolation.Tangent(4,6);
   double outgoing=(PoseInterpolation.Hermite(4,2,shared,1)-PoseInterpolation.Hermite(4,2,shared,1-epsilon))/epsilon;
   double incoming=PoseInterpolation.Hermite(6,shared,7,epsilon)/epsilon;
   Assert(Math.Abs(outgoing-incoming)<0.0001,"adjacent turn intervals share velocity");
   Assert(PoseInterpolation.Tangent(-4,6)==0,"turn tangent avoids overshooting a reversal");
   double stride=300*SideWalk.StrideRatio,mid=SideWalk.Stance+(1-SideWalk.Stance)*0.5;
   foreach(double height in new[]{220.0,300.0,400.0}) {
    double step=height*SideWalk.StrideRatio;
    var vertical=SideWalk.Sample(SideWalk.Stance*SideWalk.LandingFraction,step,8);
    Assert(vertical.Planted && vertical.Lift==0,"vertical leg remains planted "+height);
    var beforeLift=SideWalk.Sample(SideWalk.Stance-1e-8,step,8);
    double angle=Math.Atan2(-beforeLift.X,height*SideWalk.LegHeightRatio)*180/Math.PI;
    Assert(beforeLift.Planted && Math.Abs(angle-SideWalk.ToeOffAngleDegrees)<0.00001,"rear toe-off at 25 degrees "+height);
    Assert(!SideWalk.Sample(SideWalk.Stance+1e-5,step,8).Planted,"swing follows angular threshold "+height);
   }
   for(int i=0;i<=10000;i++) {
    var limited=SideWalk.LimitForward(SideWalk.Sample(i/10000.0,stride,300*0.026),300*SideWalk.LegHeightRatio);
    double forward=Math.Atan2(limited.X,300*SideWalk.LegHeightRatio-limited.Lift)*180/Math.PI;
    if(forward>20.000001)throw new Exception("Forward leg angle exceeded 20 degrees");
   }
   Assert(true,"forward swing stays within 20 degrees including raised foot");
   var recovery=SideWalk.Sample(mid,stride,8);
   Assert(Math.Abs(recovery.X-stride*(SideWalk.LandingFraction-0.5-SideWalk.RearRecoveryBias))<1e-8 && recovery.Lift>7.9,"recovering foot stays aft at peak lift");
   double eps=1e-7;
   foreach(double seam in new[]{SideWalk.Stance,1.0}) {
    var a=SideWalk.Sample(seam-eps,stride,8);var b=SideWalk.Sample(seam+eps,stride,8);
    Assert(Math.Abs(a.X-b.X)<0.001 && Math.Abs(a.Lift-b.Lift)<0.001,"gait position continuous at "+seam);
    double left=(SideWalk.Sample(seam-eps,stride,8).X-SideWalk.Sample(seam-2*eps,stride,8).X)/eps;
    double right=(SideWalk.Sample(seam+2*eps,stride,8).X-SideWalk.Sample(seam+eps,stride,8).X)/eps;
    Assert(Math.Abs(left-right)<0.01,"gait velocity continuous at "+seam);
   }
   Assert(Math.Abs(SideWalk.Sample(SideWalk.Stance*SideWalk.LandingFraction,stride,8).X)<1e-8,"stance anchor unchanged");
   Assert(Math.Abs(BodyProportions.MapY(290,290,480,710)-0.34)<1e-12,"front neck matches walking reference");
   Assert(Math.Abs(BodyProportions.MapY(480,290,480,710)-0.64)<1e-12,"front hip matches walking reference");
   Assert(Math.Abs(BodyProportions.MapY(710,290,480,710)-0.98)<1e-12,"front soles match walking reference");
   bool mappingInvertible=true;
   for(int y=0;y<=724;y++) {
    double n=BodyProportions.MapY(y,290,480,710);
    mappingInvertible &= Math.Abs(BodyProportions.SourceY(n,290,480,710)-y)<1e-9;
   }
   Assert(mappingInvertible,"landmark mapping invertible across full image");
   IconCarryChecks();
   RestTransitions();
   MockApi().GetAwaiter().GetResult();
   Console.WriteLine("All "+count+" checks passed. This does not test the desktop UI or a real provider.");
  } catch(Exception ex){Console.Error.WriteLine(ex);Environment.ExitCode=1;} finally {
   string temp=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
   string target=Path.GetFullPath(testRoot);
   if(target.StartsWith(temp,StringComparison.OrdinalIgnoreCase) && Path.GetFileName(target).StartsWith("joucho-test-") && Directory.Exists(target))Directory.Delete(target,true);
  }
 }
 static void IconCarryChecks() {
  var items=new List<DesktopIcons.Item>{new DesktopIcons.Item{Id="chosen",Position=new System.Drawing.Point(10,10)},new DesktopIcons.Item{Id="other",Position=new System.Drawing.Point(300,10)}};
  Assert(IconCarryPlan.Clear(new System.Drawing.Point(10,10),new System.Drawing.Point(100,10),"chosen",items,new System.Drawing.Point(80,80)),"push path ignores chosen item and allows free corridor");
  Assert(!IconCarryPlan.Clear(new System.Drawing.Point(10,10),new System.Drawing.Point(260,10),"chosen",items,new System.Drawing.Point(80,80)),"push path rejects occupied destination");
  Assert(!IconCarryPlan.Clear(new System.Drawing.Point(400,10),new System.Drawing.Point(100,10),"chosen",items,new System.Drawing.Point(80,80)),"left push detects obstacle between endpoints");
  foreach(int direction in new[]{-1,1}) {
   var m=new Motion();for(int i=0;i<240;i++)m.Update(1.0/60,direction,300,true);
   for(int i=0;i<120;i++)m.Update(1.0/60,0,300,true);
   Assert(m.State=="hold" && m.Facing==direction && m.Speed==0,"push reaches stable side-facing hold "+direction);
   for(int i=0;i<90;i++)m.Update(1.0/60,direction,300,true);
   Assert(m.State=="walk" && m.Speed*direction>30,"push resumes original gait "+direction);
   for(int i=0;i<240;i++)m.Update(1.0/60,0,300,false);
   Assert(m.State=="idle" && m.Facing==0,"cancel or finish returns to neutral "+direction);
  }
  Assert(IconCarryPlan.Smooth(0)==0 && IconCarryPlan.Smooth(1)==1 && Math.Abs(IconCarryPlan.Smooth(0.5)-0.5)<1e-12,"reach/release animation has exact endpoints");
 }
 static void RestTransitions() {
  var rest=new RestMotion();
  rest.RequestSleep(true);
  for(int i=0;i<100;i++)rest.Update(0.01,false);
  Assert(rest.Sleep==0,"sleep waits for front-facing settlement");
  for(int i=0;i<500;i++)rest.Update(0.01,true); // slower continuous sit/nod transition
  Assert(rest.Sleep>0.999 && rest.Eyes>0.999,"standing reaches closed-eye sleep");
  double before=rest.Sleep;
  rest.RequestPat(-1);
  Assert(rest.Sleep==before,"sleep-to-pat command does not reset visible posture");
  rest.Update(0.01,true);
  Assert(rest.Sleep>0.99 && rest.Affection==0,"sleep-to-pat starts with gradual rising");
  bool acknowledged=false;
  for(int i=0;i<900;i++) { rest.Update(0.01,true);acknowledged |= rest.Affection>0.8; }
  Assert(acknowledged && !rest.BlocksWalking,"sleep-to-pat completes acknowledgement and neutral recovery");
  rest.RequestPat(1);
  for(int i=0;i<100;i++)rest.Update(0.01,true);
  before=rest.Affection;double eye=rest.Eyes;
  rest.RequestPat(-1);
  Assert(rest.Affection==before && rest.Eyes==eye,"repeated pat preserves expression at interruption");
  rest.RequestSleep(true);
  for(int i=0;i<40;i++)rest.Update(0.01,true);
  before=rest.Sleep;rest.RequestSleep(false);
  Assert(rest.Sleep==before,"reversing sleep preserves current height");
  for(int i=0;i<800;i++)rest.Update(0.01,true);
  Assert(!rest.BlocksWalking,"interrupted sleep releases walking after recovery");
  for(int pose=0;pose<=100;pose++) {
   double t=pose/100.0,top=.30*DrawnFrames.Smooth(t),neck=top+.34+.06*DrawnFrames.Smooth(t);
   Assert(top>=0 && neck>top && neck<.98,"rest landmarks keep head/neck/floor ordering");
  }
  Assert(PoseMesh.Coordinate(0,0,1,1,0)==0 && PoseMesh.Coordinate(0,0,1,1,1)==1,"continuous pose registration has exact endpoints");
  SingleDrawingCheck();
  Assert(TurnHair.Weight(150,150,300)==0,"turn hair normalization leaves body center fixed");
  Assert(TurnHair.SourceY(.34,1,.88)==.34,"turn hair neck anchor fixed");
  Assert(Math.Abs(TurnHair.SourceY(TurnHair.Tip,1,.88)-.88)<1e-12,"outer hair tip matches walking length");

 }
 static void SingleDrawingCheck() {
  using(var red=new System.Drawing.Bitmap(24,24))using(var blue=new System.Drawing.Bitmap(24,24)) {
   using(var g=System.Drawing.Graphics.FromImage(red))g.Clear(System.Drawing.Color.Red);
   using(var g=System.Drawing.Graphics.FromImage(blue))g.Clear(System.Drawing.Color.Blue);
   double[] rows={0,12,23};
   var a=PoseMesh.Register(red,rows,12,12,5);var b=PoseMesh.Register(blue,rows,12,12,5);
   using(var rig=new PoseMesh(new[]{a,b},24,24)) {
    var p=rig.Draw(.49).GetPixel(12,12);Assert(p.R>250 && p.B==0,"first drawing stays opaque before handoff");
    p=rig.Draw(.51).GetPixel(12,12);Assert(p.B>250 && p.R==0,"second drawing is selected without purple crossfade");
   }
  }
 }
 static async Task MockApi(){
  var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port;
  try {
   Task server=Task.Run(async delegate {
    using(var socket=await listener.AcceptTcpClientAsync())using(var stream=socket.GetStream()){
     var bytes=new List<byte>();int state=0;byte[] one=new byte[1];
     while(state<4){if(await stream.ReadAsync(one,0,1)==0)throw new IOException("request ended");bytes.Add(one[0]);byte expected=(byte)(state%2==0?'\r':'\n');state=one[0]==expected?state+1:0;}
     string header=Encoding.ASCII.GetString(bytes.ToArray());int length=0;foreach(string line in header.Split('\n'))if(line.StartsWith("Content-Length:",StringComparison.OrdinalIgnoreCase))length=int.Parse(line.Substring(15).Trim());
     byte[] data=new byte[length];int at=0;while(at<length){int n=await stream.ReadAsync(data,at,length-at);if(n==0)throw new IOException();at+=n;}
     var body=Json.Object(Encoding.UTF8.GetString(data));Assert(header.Contains("POST /v1/chat/completions"),"mock request route");Assert(Json.Text(body,"model")=="test-model"&&Json.Array(body,"messages").Length==1,"mock request payload");
     string reply="{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"mock OK\"}}]}";byte[] response=Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: "+Encoding.UTF8.GetByteCount(reply)+"\r\nConnection: close\r\n\r\n"+reply);await stream.WriteAsync(response,0,response.Length);
    }
   });
   var result=await ApiClient.Complete(new ApiSettings {Endpoint="http://127.0.0.1:"+port+"/v1",Model="test-model"},new List<Dictionary<string,object>>{new Dictionary<string,object>{{"role","user"},{"content","test"}}},null,CancellationToken.None);
   await server;Assert(Json.Text(result,"content")=="mock OK","mock response parsed");
  }finally{listener.Stop();}
 }
}

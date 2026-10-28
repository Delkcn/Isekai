using System;
using System.IO;
using System.Linq;
using JouchoPet;
class SelectionCheck {
 [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool SetProcessDPIAware();
 [STAThread]static void Main(string[] args){try{
  SetProcessDPIAware();
  using(var shell=new DesktopIcons()){
   var items=shell.Read();int files=0,folders=0,bounds=0,visible=0,errors=0;
   Console.WriteLine("Desktop items="+items.Count+" list="+DesktopIconView.Find(shell.Window));
   foreach(var item in items){if(Directory.Exists(item.Id))folders++;if(!File.Exists(item.Id))continue;files++;
    try{var rect=shell.IconScreenRectangle(item);bounds++;if(DesktopIcons.Unobscured(System.Drawing.Point.Round(DesktopFileTarget.Center(rect)),IntPtr.Zero))visible++;}
    catch(Exception ex){errors++;if(errors<3)Console.WriteLine("Bounds error: "+ex.Message);}
   }
   Console.WriteLine("Files="+files+" folders="+folders+" readable bounds="+bounds+" unobscured="+visible+" errors="+errors);
   if(args.Length>0&&args[0]=="select-restore"){
    var before=items.Select(i=>shell.IsSelected(i)).ToArray();var chosen=items.Where(i=>File.Exists(i.Id)).Take(2).ToList();
    if(chosen.Count==0)throw new Exception("No desktop file available for native selection check.");
    try{
     shell.SelectFiles(chosen);if(chosen.Any(i=>!shell.IsSelected(i)))throw new Exception("Native selection not set.");Console.WriteLine("PASS native Shell selection read-back / "+chosen.Count+" files");
     var backend=new DesktopAttackFiles(IntPtr.Zero);var candidates=backend.ReadForSelection();
     var target=candidates.First(i=>i.Path==chosen[0].Id);var icon=shell.IconScreenRectangle(chosen[0]);
     if(!target.Bounds.Contains(icon))throw new Exception("Selection rectangle must include icon.");
     Console.WriteLine("PASS full desktop item bounds / candidates="+candidates.Count);
     var hits=AttackGeometry.CrossFiles(candidates,DesktopFileTarget.Center(target.Bounds),60,300);
     int selected=backend.Select(hits);if(selected==0||!shell.IsSelected(chosen[0]))throw new Exception("Cross selection did not persist.");
     Console.WriteLine("PASS cross geometry -> native backend -> selection read-back / "+selected+" files");
     if(target.Bounds.Bottom>icon.Bottom+1){
      var labelPoint=new System.Drawing.PointF(icon.Left+icon.Width*.5f,(icon.Bottom+target.Bounds.Bottom)*.5f);
      if(AttackGeometry.CrossFiles(new System.Collections.Generic.List<DesktopAttackFile>{target},labelPoint,2,300).Count!=1)throw new Exception("Filename-only overlap was excluded.");
      Console.WriteLine("PASS filename-only overlap counts");
     }
    }
    finally{for(int i=0;i<items.Count;i++)shell.RestoreSelected(items[i],before[i]);if(items.Where((v,i)=>shell.IsSelected(v)!=before[i]).Any())throw new Exception("Selection restoration failed.");Console.WriteLine("PASS original desktop selection restored");}
   }
  }
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
namespace JouchoPet {
 sealed class DesktopAttackFile {
  internal readonly string Path;internal readonly Rectangle Bounds;
  internal DesktopAttackFile(string path,Rectangle bounds){Path=path;Bounds=bounds;}
 }
 interface IDesktopAttackFiles {
  List<DesktopAttackFile> Read();
  List<DesktopAttackFile> ReadForSelection();
  int Select(List<DesktopAttackFile> files);
  ProgramLaunchResult Open(DesktopAttackFile file);
 }
 sealed class DesktopAttackFiles:IDesktopAttackFiles {
  readonly IntPtr pet;
  internal DesktopAttackFiles(IntPtr pet){this.pet=pet;}
  bool Visible(Rectangle r){return DesktopIcons.Unobscured(Point.Round(DesktopFileTarget.Center(r)),pet);}
  public List<DesktopAttackFile> Read(){
   using(var shell=new DesktopIcons()){
    var result=new List<DesktopAttackFile>();
    foreach(var item in shell.Read())if(File.Exists(item.Id)){
     var bounds=shell.IconScreenRectangle(item);if(Visible(bounds))result.Add(new DesktopAttackFile(item.Id,bounds));
    }return result;
   }
  }
  public List<DesktopAttackFile> ReadForSelection(){
   using(var shell=new DesktopIcons()){
    var result=new List<DesktopAttackFile>();
    foreach(var item in shell.Read())if(File.Exists(item.Id))
     result.Add(new DesktopAttackFile(item.Id,shell.SelectionScreenRectangle(item)));
    return result;
   }
  }
  public int Select(List<DesktopAttackFile> files){
   if(files.Count==0)return 0;
   using(var shell=new DesktopIcons()){
    var valid=new List<DesktopIcons.Item>();
    foreach(var item in shell.Read()){
     var expected=files.FirstOrDefault(f=>String.Equals(f.Path,item.Id,StringComparison.OrdinalIgnoreCase));
     if(expected==null||!File.Exists(item.Id))continue;
     var bounds=shell.SelectionScreenRectangle(item);if(bounds==expected.Bounds)valid.Add(item);
    }
    shell.SelectFiles(valid);return valid.Count;
   }
  }
  public ProgramLaunchResult Open(DesktopAttackFile file){
   try{
    using(var target=DesktopFileTarget.Open(file.Path)){
     if(target.Position!=DesktopFileTarget.Center(file.Bounds)||!Visible(file.Bounds))return new ProgramLaunchResult(false,"目标图标位置已改变或被窗口遮挡。");
     var opener=new HitFileOpen(new ShellFileOpener());opener.Arm(target);return opener.Hit();
    }
   }catch(Exception ex){return new ProgramLaunchResult(false,ex.Message);}
  }
 }
}

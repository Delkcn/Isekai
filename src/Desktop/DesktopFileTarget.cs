using System;using System.Drawing;using System.IO;
namespace JouchoPet {
 // A read-only identity/position lease. Never calls the desktop move or select APIs.
 sealed class DesktopFileTarget:IDisposable {
  internal readonly string Path;internal readonly PointF Position;
  readonly Func<string,PointF?> locate;IDisposable owner;
  internal DesktopFileTarget(string path,PointF position,Func<string,PointF?> resolver,IDisposable resource=null){Path=path;Position=position;locate=resolver;owner=resource;}
  internal bool Valid(){PointF? current=locate(Path);return current.HasValue&&RigParts.Length(current.Value,Position)<.5;}
  internal static PointF Center(Rectangle rect){if(rect.Width<=0||rect.Height<=0)throw new ArgumentException("Invalid icon bounds.");return new PointF(rect.Left+rect.Width*.5f,rect.Top+rect.Height*.5f);}
  internal static DesktopFileTarget Open(string path){
   if(string.IsNullOrWhiteSpace(path)||!File.Exists(path))throw new ArgumentException("所选文件已不存在。请重新选择桌面文件。");
   var shell=new DesktopIcons();
   try{
    Func<string,PointF?> resolver=id=>{
     if(!File.Exists(id))return null;
     foreach(var item in shell.Read())if(string.Equals(item.Id,id,StringComparison.OrdinalIgnoreCase)){
      return Center(shell.IconScreenRectangle(item));
     }return null;
    };
    PointF? position=resolver(path);if(!position.HasValue)throw new InvalidOperationException("未找到此文件的桌面图标。请选择桌面上的文件或快捷方式。");
    return new DesktopFileTarget(path,position.Value,resolver,shell);
   }catch{shell.Dispose();throw;}
  }
  public void Dispose(){var resource=owner;owner=null;if(resource!=null)resource.Dispose();}
 }
}

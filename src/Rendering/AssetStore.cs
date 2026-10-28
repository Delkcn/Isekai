using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace JouchoPet {
 // Editable loose resources take priority. Packs remain a legacy fallback.
 static class AssetStore {
  internal sealed class Registration {
   public readonly double Neck,Width;
   public Registration(double neck,double width){Neck=neck;Width=width;}
  }
  public static byte[] Read(string name) {
   string root=AppDomain.CurrentDomain.BaseDirectory;
   string loose=Path.Combine(root,"assets",name);
   if(File.Exists(loose))return File.ReadAllBytes(loose);
   string pack=Path.Combine(root,"character.pack");
   if(File.Exists(pack))using(var zip=ZipFile.OpenRead(pack)) {
    var entry=zip.GetEntry(name.Replace('\\','/'));
    if(entry==null)throw new FileNotFoundException("Character resource missing: "+name);
    using(var input=entry.Open())using(var output=new MemoryStream()){input.CopyTo(output);return output.ToArray();}
   }
   return File.ReadAllBytes(Path.Combine(root,"assets",name));
  }
  public static Bitmap Image(string name,CharacterForm form=CharacterForm.AnemoneI) { name=CharacterForms.Resource(name,form);
   using(var stream=new MemoryStream(Read(name)))using(var source=new Bitmap(stream)) {
    var result=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppArgb);
    var rect=new Rectangle(0,0,source.Width,source.Height);
    var input=source.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
    try {
     var output=result.LockBits(rect,ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
     try {var row=new byte[source.Width*4];for(int y=0;y<source.Height;y++){Marshal.Copy(IntPtr.Add(input.Scan0,y*input.Stride),row,0,row.Length);Marshal.Copy(row,0,IntPtr.Add(output.Scan0,y*output.Stride),row.Length);}}
     finally{result.UnlockBits(output);}
    }catch{result.Dispose();throw;}finally{source.UnlockBits(input);}
    return result;
   }
  }
  public static string[] Lines(string name){return Encoding.UTF8.GetString(Read(name)).Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries);}
  public static Bitmap TurnView(string name,CharacterForm form=CharacterForm.AnemoneI) {
   return CharacterArt.Load(name,form);
  }
 }
}

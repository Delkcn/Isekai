using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
class PackAssets {
 static void Main(string[] args) {
  string root=Path.GetFullPath(args[0]),name="v0610/anemone-i-unified-v10-eyes.png";
  string source=Path.Combine(root,"assets",name),pack=Path.Combine(root,"character.pack"),temp=pack+"."+Guid.NewGuid().ToString("N")+".tmp";
  using(var b=new Bitmap(source))if(b.Width!=1774 || b.Height!=887)throw new InvalidDataException("Approved atlas dimensions changed");
  try {
   using(var zip=ZipFile.Open(temp,ZipArchiveMode.Create)) {
    // Preserve the full PNG bytes; no downsampling, requantization or palette conversion.
    zip.CreateEntryFromFile(source,name,CompressionLevel.NoCompression);
    foreach(string support in new[]{"v064/walk-legs.png","v067/sleep-keys-eye.png","v0611/underarm-backplate.png","v0616/greeting-underlay.png","v0618/greeting-palm.png","v0642/ivory-bow.png","v0642/ivory-bowstring.png","v0643/ivory-arrow.png","v0700/blue-silver-sword.png","v0700/right-grip-hands.png"})zip.CreateEntryFromFile(Path.Combine(root,"assets",support),support,CompressionLevel.NoCompression);
    using(var writer=new StreamWriter(zip.CreateEntry("version.txt").Open()))writer.Write("0.7.0-preview");
   }
   if(File.Exists(pack))File.Replace(temp,pack,null);else File.Move(temp,pack);
  }finally{if(File.Exists(temp))File.Delete(temp);}
  Console.WriteLine("Packed approved full-resolution atlas: "+new FileInfo(pack).Length+" bytes. Source and historical assets retained.");
 }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
namespace JouchoPet {
 // Compact, click-through layered window: the arrow can leave the pet canvas.
 sealed class MagicOverlay : Form {
  Bitmap canvas,petSurface;
  internal Bitmap DiagnosticCanvas {get{return canvas;}}
  internal Rectangle EffectBounds {get;private set;}
  protected override bool ShowWithoutActivation {get{return true;}}
  protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x00080000|0x00000080|0x08000000|0x00000020;return p;}}
  protected override void WndProc(ref Message m){if(m.Msg==0x84){m.Result=new IntPtr(-1);return;}if(m.Msg==0x21){m.Result=new IntPtr(3);return;}base.WndProc(ref m);}
  internal MagicOverlay(){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;Text="情緒 · 白弓";}
  static Rectangle Cover(RectangleF bounds){int left=(int)Math.Floor(bounds.Left),top=(int)Math.Floor(bounds.Top);return Rectangle.FromLTRB(left,top,(int)Math.Ceiling(bounds.Right),(int)Math.Ceiling(bounds.Bottom));}
  internal Bitmap PaintEffect(Point pet,int petWidth,double size,ArcheryPose? pose,MagicArrow arrow,bool topmost,Bitmap character=null) {
   Rectangle bounds=Rectangle.Empty;
   if(pose.HasValue && ArcheryArt.HasBowEffect(pose.Value))bounds=Cover(ArcheryArt.BowBounds(pet,petWidth,size,pose.Value));
   if(arrow!=null && !arrow.Done){Rectangle r=Cover(ArcheryArt.ProjectileBounds(arrow));bounds=bounds.IsEmpty?r:Rectangle.Union(bounds,r);}
   if(bounds.IsEmpty){Hide();return character;}
   if(character!=null)bounds=Rectangle.Union(bounds,new Rectangle(pet,character.Size));
   bounds.Inflate(3,3);if(canvas==null || canvas.Width!=bounds.Width || canvas.Height!=bounds.Height){if(canvas!=null)canvas.Dispose();canvas=new Bitmap(bounds.Width,bounds.Height,PixelFormat.Format32bppPArgb);}
   using(var g=Graphics.FromImage(canvas)){g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.TranslateTransform(-bounds.Left,-bounds.Top);if(pose.HasValue)ArcheryArt.Bow(g,pet,petWidth,size,pose.Value);if(arrow!=null)ArcheryArt.Projectile(g,arrow);}
   if(character!=null){
    // Composite once, then split into non-overlapping windows. Even if Windows
    // raises the effect window, it contains no pixels over the pet rectangle.
    if(petSurface==null || petSurface.Size!=character.Size){if(petSurface!=null)petSurface.Dispose();petSurface=new Bitmap(character.Width,character.Height,PixelFormat.Format32bppPArgb);}
    Point offset=new Point(pet.X-bounds.Left,pet.Y-bounds.Top);
    using(var g=Graphics.FromImage(canvas))g.DrawImageUnscaled(character,offset);
    using(var g=Graphics.FromImage(petSurface)){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(canvas,-offset.X,-offset.Y);}
    using(var g=Graphics.FromImage(canvas)){g.CompositingMode=CompositingMode.SourceCopy;using(var clear=new SolidBrush(Color.Transparent))g.FillRectangle(clear,new Rectangle(offset,character.Size));}
   }
   EffectBounds=bounds;
   // Upload before showing so a reused HWND never flashes its previous frame.
   TopMost=topmost;Native.Present(Handle,canvas,bounds.Left,bounds.Top);if(!Visible)Show();
   return character==null?null:petSurface;
  }
  protected override void Dispose(bool disposing){if(disposing){if(canvas!=null){canvas.Dispose();canvas=null;}if(petSurface!=null){petSurface.Dispose();petSurface=null;}}base.Dispose(disposing);}
 }
}

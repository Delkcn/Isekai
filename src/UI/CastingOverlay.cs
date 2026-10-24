using System;using System.Drawing;using System.Drawing.Imaging;using System.Windows.Forms;
namespace JouchoPet {
 // Compact independent flight window. All bounds are signed screen coordinates.
 sealed class CastingOverlay:Form {
  Bitmap canvas;
  protected override bool ShowWithoutActivation{get{return true;}}
  protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x00080000|0x00000080|0x08000000|0x00000020;return p;}}
  protected override void WndProc(ref Message m){if(m.Msg==0x84){m.Result=new IntPtr(-1);return;}if(m.Msg==0x21){m.Result=new IntPtr(3);return;}base.WndProc(ref m);}
  internal CastingOverlay(){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;AutoScaleMode=AutoScaleMode.None;Text="情緒 · 魔法弹";}
  internal void PresentBolt(MagicBolt bolt,bool topmost){
   if(bolt==null||bolt.Done){Hide();return;}Rectangle r=MagicBoltArt.Bounds(bolt);
   if(canvas==null||canvas.Size!=r.Size){if(canvas!=null)canvas.Dispose();canvas=new Bitmap(r.Width,r.Height,PixelFormat.Format32bppPArgb);}
   using(var g=Graphics.FromImage(canvas)){g.Clear(Color.Transparent);g.TranslateTransform(-r.Left,-r.Top);MagicBoltArt.Draw(g,bolt);}
   TopMost=topmost;Native.Present(Handle,canvas,r.Left,r.Top);if(!Visible)Show();
  }
  protected override void Dispose(bool disposing){if(disposing&&canvas!=null)canvas.Dispose();base.Dispose(disposing);}
 }
}

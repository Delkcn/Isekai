using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.Windows.Forms;
namespace JouchoPet {
 sealed class SwordOverlay:Form {
  Bitmap outside,inside;
  protected override bool ShowWithoutActivation{get{return true;}}
  protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x00080000|0x00000080|0x08000000|0x00000020;return p;}}
  protected override void WndProc(ref Message m){if(m.Msg==0x84){m.Result=new IntPtr(-1);return;}base.WndProc(ref m);}
  internal SwordOverlay(){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;}
  internal Bitmap Present(Bitmap full,Point pet,Size petSize,int pad,bool topmost){if(outside==null||outside.Size!=full.Size){if(outside!=null)outside.Dispose();outside=new Bitmap(full.Width,full.Height,PixelFormat.Format32bppPArgb);}if(inside==null||inside.Size!=petSize){if(inside!=null)inside.Dispose();inside=new Bitmap(petSize.Width,petSize.Height,PixelFormat.Format32bppPArgb);}using(var g=Graphics.FromImage(inside)){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(full,-pad,-pad);}using(var g=Graphics.FromImage(outside)){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(full,0,0);using(var clear=new SolidBrush(Color.Transparent))g.FillRectangle(clear,new Rectangle(new Point(pad,pad),petSize));}TopMost=topmost;Native.Present(Handle,outside,pet.X-pad,pet.Y-pad);if(!Visible)Show();return inside;}
  protected override void Dispose(bool disposing){if(disposing){if(inside!=null)inside.Dispose();if(outside!=null)outside.Dispose();}base.Dispose(disposing);}
 }
}

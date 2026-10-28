using System;using System.Drawing;using System.Drawing.Imaging;using System.Windows.Forms;
namespace JouchoPet {
 sealed class MiSigilOverlay:Form {
  Bitmap canvas;
  internal MiSigilOverlay(){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;}
  protected override bool ShowWithoutActivation{get{return true;}}
  protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x00080000|0x00000080|0x08000000|0x00000020;return p;}}
  protected override void WndProc(ref Message m){if(m.Msg==0x84){m.Result=new IntPtr(-1);return;}base.WndProc(ref m);}
  internal void Present(PointF center,double span,double size,DailyPose pose,bool topmost,RectangleF face){
   if(pose.Glow<=0&&pose.SigilAlpha<=0){Hide();return;}
   int pad=(int)Math.Ceiling(span/2+MiSigilArt.ArmWidth(size)+size*.10);int edge=pad*2+2;
   if(canvas==null||canvas.Width!=edge){if(canvas!=null)canvas.Dispose();canvas=new Bitmap(edge,edge,PixelFormat.Format32bppPArgb);}
   int left=(int)Math.Floor(center.X)-pad,top=(int)Math.Floor(center.Y)-pad;
   face.Offset(-left,-top);
   using(var g=Graphics.FromImage(canvas)){g.Clear(Color.Transparent);MiSigilArt.Draw(g,new PointF(center.X-left,center.Y-top),span,size,pose,face);}
   TopMost=topmost;Native.Present(Handle,canvas,left,top);if(!Visible)Show();
  }
  protected override void Dispose(bool disposing){if(disposing&&canvas!=null)canvas.Dispose();base.Dispose(disposing);}
 }
}

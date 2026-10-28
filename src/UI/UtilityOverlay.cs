using System;using System.Drawing;using System.Drawing.Imaging;using System.Windows.Forms;
namespace JouchoPet {
 sealed class UtilityOverlay:Form {
  Bitmap canvas;
  internal UtilityOverlay(){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;}
  protected override bool ShowWithoutActivation{get{return true;}}
  protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x00080000|0x00000080|0x08000000|0x00000020;return p;}}
  protected override void WndProc(ref Message m){if(m.Msg==0x84){m.Result=new IntPtr(-1);return;}base.WndProc(ref m);}
  internal void PresentBeam(PointF from,PointF to,double alpha,double size,bool topmost,double age=.60,bool tether=false,double iconHeight=0){
   double burstAge=age-(tether?.45:.50);if(alpha<=0&&(burstAge<0||burstAge>=(tether?.45:.95))){Hide();return;}
   float margin=(float)(size*(tether?.20:.72)+iconHeight*.5);var bounds=Rectangle.Ceiling(RectangleF.FromLTRB(Math.Min(from.X,to.X)-margin,Math.Min(from.Y,to.Y)-margin,Math.Max(from.X,to.X)+margin,Math.Max(from.Y,to.Y)+margin));
   if(canvas==null||canvas.Size!=bounds.Size){if(canvas!=null)canvas.Dispose();canvas=new Bitmap(bounds.Width,bounds.Height,PixelFormat.Format32bppPArgb);}
   using(var g=Graphics.FromImage(canvas)){g.Clear(Color.Transparent);g.TranslateTransform(-bounds.Left,-bounds.Top);if(tether)UtilityArt.Tethers(g,from,to,iconHeight,alpha,size,age);else{UtilityArt.Beam(g,from,to,alpha,size,age);UtilityArt.BeamImpact(g,to,burstAge,size);}}
   TopMost=topmost;Native.Present(Handle,canvas,bounds.Left,bounds.Top);if(!Visible)Show();}
  protected override void Dispose(bool disposing){if(disposing&&canvas!=null)canvas.Dispose();base.Dispose(disposing);}
 }
}

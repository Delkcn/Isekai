using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
namespace JouchoPet {
 sealed class SpeechBubble : Form {
  readonly Timer timer=new Timer{Interval=33};
  readonly Stopwatch clock=Stopwatch.StartNew();
  readonly Font body=new Font("Microsoft YaHei UI",15,FontStyle.Regular,GraphicsUnit.Pixel),caption=new Font("Microsoft YaHei UI",12,FontStyle.Regular,GraphicsUnit.Pixel);
  SpeechContent content;double previous;bool tailLeft=true;string lastText;int lastPage=-1;bool lastComplete;
  public bool Conversation {get;private set;}
  public event Action OpenConversation;
  internal SpeechContent Content {get{return content;}}
  protected override bool ShowWithoutActivation {get{return true;}}
  protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x00080000|0x00000080|0x08000000;return p;}}
  protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=new IntPtr(3);return;}base.WndProc(ref m);}
  public SpeechBubble() {
   Text="情緒 · 对话泡泡";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;ClientSize=new Size(304,222);
   timer.Tick+=delegate {
    double now=clock.Elapsed.TotalSeconds,dt=Math.Min(.15,now-previous);previous=now;
    if(content==null)return;
    content.Update(dt,Bounds.Contains(Cursor.Position));
    if(content.Closed){Dismiss();return;}
    if(lastText!=content.VisibleText || lastPage!=content.Page || lastComplete!=content.Complete)PaintBubble();
   };
   MouseDown+=delegate(object sender,MouseEventArgs e) {
    if(e.Button==MouseButtons.Right || e.X>=264 && e.Y<44){Dismiss();return;}
    if(e.Button!=MouseButtons.Left || content==null)return;
    if(e.Y>=178 && e.X>=206){var open=OpenConversation;if(open!=null)open();return;}
    content.Advance();if(content.Closed)Dismiss();else PaintBubble();
   };
  }
  internal SpeechContent PrepareText(string text) {
   using(var image=new Bitmap(1,1))using(var g=Graphics.FromImage(image))using(var format=(StringFormat)StringFormat.GenericTypographic.Clone())
    return new SpeechContent(text,s=>g.MeasureString(s,body,int.MaxValue,format).Width<=248,5);
  }
  public void Speak(string text,bool conversation,Rectangle pet,Rectangle work,bool topmost) {
   if(string.IsNullOrWhiteSpace(text))return;
   content=PrepareText(text);Conversation=conversation;lastText=null;lastPage=-1;previous=clock.Elapsed.TotalSeconds;
   Follow(pet,work,topmost);if(!Visible)Show();PaintBubble();timer.Start();
  }
  public void Follow(Rectangle pet,Rectangle work,bool topmost) {
   if(TopMost!=topmost)TopMost=topmost;
   bool side;Rectangle next=SpeechContent.Place(pet,work,Size,out side);
   if(Location!=next.Location)Location=next.Location;
   if(tailLeft!=side){tailLeft=side;if(Visible && content!=null)PaintBubble();}
  }
  public void Dismiss(){timer.Stop();Hide();content=null;Conversation=false;lastText=null;}
  void PaintBubble() {
   if(content==null || !IsHandleCreated)return;
   using(var image=RenderCard(content,tailLeft,body,caption))Native.Present(Handle,image,Left,Top);
   lastText=content.VisibleText;lastPage=content.Page;lastComplete=content.Complete;
  }
  static GraphicsPath Round(RectangleF r,float radius) {
   var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
  }
  internal static Bitmap RenderCard(SpeechContent state,bool left,Font body,Font caption) {
   var image=new Bitmap(304,222,PixelFormat.Format32bppPArgb);
   using(var g=Graphics.FromImage(image)) {
    g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
    using(var shadow=Round(new RectangleF(13,12,280,199),18))using(var b=new SolidBrush(Color.FromArgb(36,44,29,35)))g.FillPath(b,shadow);
    using(var shape=Round(new RectangleF(10,7,280,198),18))using(var paper=new SolidBrush(Color.FromArgb(250,255,250,242)))using(var edge=new Pen(Color.FromArgb(210,211,184,178),1)) {
     g.FillPath(paper,shape);g.DrawPath(edge,shape);
     PointF[] tail=left?new[]{new PointF(11,129),new PointF(0,146),new PointF(11,144)}:new[]{new PointF(289,129),new PointF(302,146),new PointF(289,144)};
     g.FillPolygon(paper,tail);g.DrawLine(edge,tail[0],tail[1]);g.DrawLine(edge,tail[1],tail[2]);
    }
    using(var red=new SolidBrush(Color.FromArgb(161,63,75)))using(var ink=new SolidBrush(Color.FromArgb(71,52,57)))using(var muted=new SolidBrush(Color.FromArgb(148,114,118)))using(var line=new Pen(Color.FromArgb(231,211,204))) {
     for(int petal=0;petal<5;petal++){double a=petal*Math.PI*2/5;g.FillEllipse(red,32+(float)Math.Cos(a)*5-5,29+(float)Math.Sin(a)*5-5,10,10);}
     g.FillEllipse(Brushes.Ivory,28,26,9,5);g.FillEllipse(ink,31,26,3,5);
     g.DrawString("情緒",caption,ink,49,20);using(var small=new Font(FontFamily.GenericSansSerif,9,FontStyle.Regular,GraphicsUnit.Pixel))g.DrawString("ANEMONE I",small,muted,91,23);
     g.DrawLine(line,27,46,273,46);g.DrawString("×",caption,muted,268,17);
     using(var format=(StringFormat)StringFormat.GenericTypographic.Clone()) {
      string[] lines=state.VisibleText.Split('\n');for(int i=0;i<lines.Length;i++)g.DrawString(lines[i],body,ink,new PointF(27,59+i*23),format);
     }
     g.DrawLine(line,27,181,273,181);
     string hint=!state.Complete?"点击展开这句话":state.Page+1<state.Pages.Count?"点击继续  ·  "+(state.Page+1)+" / "+state.Pages.Count:"点击收起";
     g.DrawString(hint,caption,muted,27,188);g.DrawString("对话 ↗",caption,red,221,188);
    }
   }
   return image;
  }
  protected override void Dispose(bool disposing){if(disposing){timer.Stop();timer.Dispose();body.Dispose();caption.Dispose();}base.Dispose(disposing);}
 }
}

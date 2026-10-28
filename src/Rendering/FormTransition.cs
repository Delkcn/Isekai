using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
namespace JouchoPet {
 // Own two appearance renderers, but no action timelines or gameplay effects.
 // Every blended image is drawn twice from the same live pose, never a snapshot.
 sealed class FormTransition : IDisposable {
  internal const double Duration=.45;
  readonly PetRenderer[] forms=new PetRenderer[2];
  readonly Func<CharacterForm,int,PetRenderer> build;
  Task<PetRenderer> pending;CharacterForm pendingForm,committed;int height,pendingHeight;
  bool disposed,fading;double from,started,span;Bitmap canvas;
  internal double Mix {get;private set;}
  internal CharacterForm Requested {get;private set;}
  internal PetRenderer Current {get{return forms[(int)committed];}}
  internal bool Loading {get{return pending!=null;}}
  internal bool Fading {get{return fading;}}
  internal string Error {get;private set;}
  internal FormTransition(PetRenderer initial,int size,Func<CharacterForm,int,PetRenderer> factory=null){
   height=size;forms[(int)initial.Form]=initial;committed=Requested=initial.Form;Mix=(int)initial.Form;build=factory??Prepare;
   BeginPrepare(initial.Form==CharacterForm.AnemoneI?CharacterForm.AnemoneII:CharacterForm.AnemoneI);
  }
  static PetRenderer Prepare(CharacterForm form,int size){
   PetRenderer r=null;try{r=new PetRenderer(form);r.Resize(size);
    r.Draw(new RenderFrame{MotionState="idle",Greeting=1,WaveBlend=1,WavePhase=.5});
    r.Reach.DrawLookout(-1,1);r.Reach.DrawLookout(1,1);return r;
   }catch{if(r!=null)r.Dispose();throw;}
  }
  void BeginPrepare(CharacterForm form){if(pending!=null||forms[(int)form]!=null)return;pendingForm=form;pendingHeight=height;int size=height;pending=Task.Factory.StartNew(()=>build(form,size),CancellationToken.None,TaskCreationOptions.None,TaskScheduler.Default);}
  void BeginFade(double now){from=Mix;started=now;span=Duration*Math.Abs((int)Requested-from);fading=span>0;}
  internal void Request(CharacterForm form,double now){
   if(disposed)throw new ObjectDisposedException("FormTransition");CharacterForms.Validate(form);Advance(now);Error=null;
   if(form==Requested)return;Requested=form;
   if(forms[(int)form]==null){BeginPrepare(form);return;}BeginFade(now);
  }
  internal void Advance(double now){
   if(disposed)return;
   if(pending!=null&&pending.IsCompleted){var task=pending;pending=null;
    if(task.IsFaulted){string message=task.Exception.GetBaseException().Message;if(Requested==pendingForm){Error=message;Requested=committed;}}
    else if(task.IsCanceled){if(Requested==pendingForm){Error="衣装准备已取消。";Requested=committed;}}
    else if(pendingHeight!=height){task.Result.Dispose();BeginPrepare(pendingForm);}
    else forms[(int)pendingForm]=task.Result;
   }
   if(!fading&&Mix!=(int)Requested&&forms[(int)Requested]!=null)BeginFade(now);
   if(fading){double t=Math.Max(0,Math.Min(1,(now-started)/span));Mix=from+((int)Requested-from)*t*t*(3-2*t);if(t>=1){Mix=(int)Requested;committed=Requested;fading=false;}}
  }
  internal string TakeError(){string error=Error;Error=null;return error;}
  internal void Resize(int size){height=size;foreach(var r in forms)if(r!=null)r.Resize(size);}
  internal Bitmap Draw(Func<PetRenderer,Bitmap> draw){
   if(Mix<=0)return draw(forms[0]);if(Mix>=1)return draw(forms[1]);
   Bitmap a=draw(forms[0]),b=draw(forms[1]);return Blend(a,b,Mix);
  }
  unsafe Bitmap Blend(Bitmap a,Bitmap b,double mix){
   if(a.Size!=b.Size)throw new InvalidOperationException("Form canvases must share the same rig size.");
   if(canvas==null||canvas.Size!=a.Size){if(canvas!=null)canvas.Dispose();canvas=new Bitmap(a.Width,a.Height,PixelFormat.Format32bppPArgb);}
   var rect=new Rectangle(Point.Empty,a.Size);BitmapData pa=null,pb=null,pc=null;int weight=(int)Math.Round(mix*65536),inverse=65536-weight;
   try{pa=a.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);pb=b.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);pc=canvas.LockBits(rect,ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
    for(int y=0;y<a.Height;y++){byte* x=(byte*)pa.Scan0+y*pa.Stride,z=(byte*)pb.Scan0+y*pb.Stride,o=(byte*)pc.Scan0+y*pc.Stride;for(int i=0;i<a.Width*4;i++)o[i]=(byte)((x[i]*inverse+z[i]*weight+32768)>>16);}
   }finally{if(pc!=null)canvas.UnlockBits(pc);if(pb!=null)b.UnlockBits(pb);if(pa!=null)a.UnlockBits(pa);}return canvas;
  }
  internal PointF SigilAnchor(double time){var a=forms[0];var b=forms[1];if(Mix<=0)return a.SampleSigilAnchor(time);if(Mix>=1)return b.SampleSigilAnchor(time);PointF p=a.SampleSigilAnchor(time),q=b.SampleSigilAnchor(time);return new PointF((float)(p.X+(q.X-p.X)*Mix),(float)(p.Y+(q.Y-p.Y)*Mix));}
  internal PointF RenderedSigilAnchor {get{if(Mix<=0)return forms[0].SigilAnchor;if(Mix>=1)return forms[1].SigilAnchor;var a=forms[0].SigilAnchor;var b=forms[1].SigilAnchor;return new PointF((float)(a.X+(b.X-a.X)*Mix),(float)(a.Y+(b.Y-a.Y)*Mix));}}
  internal RectangleF SigilFace {get{if(Mix<=0)return forms[0].SigilFace;if(Mix>=1)return forms[1].SigilFace;var a=forms[0].SigilFace;var b=forms[1].SigilFace;return new RectangleF((float)(a.X+(b.X-a.X)*Mix),(float)(a.Y+(b.Y-a.Y)*Mix),(float)(a.Width+(b.Width-a.Width)*Mix),(float)(a.Height+(b.Height-a.Height)*Mix));}}
  public void Dispose(){if(disposed)return;disposed=true;foreach(var r in forms)if(r!=null)r.Dispose();if(canvas!=null)canvas.Dispose();if(pending!=null){pending.ContinueWith(t=>{if(t.Status==TaskStatus.RanToCompletion)t.Result.Dispose();else if(t.IsFaulted){var observed=t.Exception;}},TaskScheduler.Default);pending=null;}}
 }
}

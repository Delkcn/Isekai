using System;using System.Drawing;using System.Windows.Forms;
namespace JouchoPet {
 sealed partial class Pet {
  MiSigilOverlay sigilOverlay;Point sigilOrigin;string sigilLayout;double sigilSpan;bool crossSigil;
  bool crossFilesSelected;IDesktopAttackFiles desktopAttackFiles;
  IDesktopAttackFiles AttackFiles{get{return desktopAttackFiles??(desktopAttackFiles=new DesktopAttackFiles(Handle));}}
  public bool MiSigil(){if(emergencyRunning||!RequestSigil(false))return false;emergencyPending=true;return true;}
  public bool MiSigilPreview(){return RequestSigil(false);}
  public bool CrossSigil(){return RequestSigil(true);}
  bool RequestSigil(bool cross){if(magicArrow!=null||!RequestDaily(DailyAction.Sigil))return false;crossSigil=cross;crossFilesSelected=false;CancelEmergency();sigilOrigin=Location;sigilLayout=DisplayLayout();sigilSpan=MiSigilArt.Span(Screen.FromControl(this).Bounds.Size);if(bubble!=null)bubble.Dismiss();return true;}
  void SelectCrossFiles(double dt){
   if(!crossSigil||crossFilesSelected||menuOpen||!SigilVisible||!CastingMotion.ValidDelta(dt)||daily.Age>=.68||daily.Age+dt<.68)return;
   crossFilesSelected=true;
   try{
    var center=RigParts.Add(FormSigilAnchor(elapsed),Left,Top);
    var files=AttackFiles;int count=files.Select(AttackGeometry.CrossFiles(files.ReadForSelection(),center,sigilSpan,petHeight));
    CrossSelectionMessage(count>0?"十字法阵已选中 "+count+" 个文件":"十字法阵没有覆盖到桌面文件");
   }catch(Exception ex){CrossSelectionMessage("十字法阵选中失败："+ex.Message);}
  }
  void CrossSelectionMessage(string message){UtilityMessage(message);if(Visible)Say(message);}
  void GuardSigil(){if(daily.Kind!=DailyAction.Sigil)return;if(daily.Pending)sigilOrigin=Location;else if(Location!=sigilOrigin||DisplayLayout()!=sigilLayout){daily.Cancel();CancelEmergency();}}
  bool SigilVisible{get{return daily.Active&&daily.Kind==DailyAction.Sigil&&!daily.Cancelling;}}
  void RenderSigil(){if(!SigilVisible)return;if(sigilOverlay==null)sigilOverlay=new MiSigilOverlay();var p=formTransition==null?renderer.SigilAnchor:formTransition.RenderedSigilAnchor;var face=formTransition==null?renderer.SigilFace:formTransition.SigilFace;face.Offset(Left,Top);var pose=daily.Pose;pose.CrossOnly=crossSigil;sigilOverlay.Present(new PointF(p.X+Left,p.Y+Top),sigilSpan,petHeight,pose,TopMost,face);}
 }
}

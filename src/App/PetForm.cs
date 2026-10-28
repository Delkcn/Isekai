using System;
using System.Drawing;
using System.Windows.Forms;
namespace JouchoPet {
 sealed partial class Pet {
  ToolStripItem characterTitle;
  ToolStripMenuItem formMenu,a1FormItem,a2FormItem;
  FormTransition formTransition;
  internal CharacterForm CurrentForm {get{return formTransition==null?renderer.Form:formTransition.Requested;}}
  internal bool FormSwitchReady {
   get{return !IsDisposed&&renderer!=null&&renderer.Ready;}
  }
  void PrepareForms(){if(formTransition==null)formTransition=new FormTransition(renderer,petHeight);}
  void UpdateFormTransition(double now){
   if(formTransition==null)return;var previous=CurrentForm;bool loading=formTransition.Loading,fading=formTransition.Fading;
   formTransition.Advance(now);renderer=formTransition.Current;
   string error=formTransition.TakeError();if(error!=null)Say("衣装切换未完成：\n"+error);
   if(previous!=CurrentForm||loading!=formTransition.Loading||fading!=formTransition.Fading){Text="Isekaijoucho - "+CharacterForms.Name(CurrentForm);UpdateFormMenu();}
  }
  Bitmap DrawForm(Func<PetRenderer,Bitmap> draw){return formTransition==null?draw(renderer):formTransition.Draw(draw);}
  PointF FormSigilAnchor(double time){return formTransition==null?renderer.SampleSigilAnchor(time):formTransition.SigilAnchor(time);}
  internal bool TryChangeForm(CharacterForm form,out string error){
   if(InvokeRequired)throw new InvalidOperationException("Form changes must run on the desktop UI thread.");
   error=null;if(!FormSwitchReady){error="角色尚未准备完成。";return false;}
   try{PrepareForms();formTransition.Request(form,clock.Elapsed.TotalSeconds);}catch(Exception ex){error=ex.Message;return false;}
   // Appearance does not cancel/restart an action or reset the animation clock.
   Text="Isekaijoucho - "+CharacterForms.Name(form);UpdateFormMenu();return true;
  }
  void SelectForm(CharacterForm form){string error;if(!TryChangeForm(form,out error))Say("衣装切换未完成：\n"+error);}
  void AddFormMenu(){
   characterTitle.Enabled=true;
   characterTitle.ToolTipText="动作中也可渐变切换；Ctrl + 右键直接切换。";
   characterTitle.Click+=delegate{SelectForm(CurrentForm==CharacterForm.AnemoneI?CharacterForm.AnemoneII:CharacterForm.AnemoneI);};
   formMenu=new ToolStripMenuItem("衣装调试（内部）"){Visible=false};
   a1FormItem=new ToolStripMenuItem("Anemone I · 默认",null,delegate{SelectForm(CharacterForm.AnemoneI);});
   a2FormItem=new ToolStripMenuItem("Anemone II · 隐藏",null,delegate{SelectForm(CharacterForm.AnemoneII);});
   formMenu.DropDownItems.Add(a1FormItem);formMenu.DropDownItems.Add(a2FormItem);
   formMenu.DropDownItems.Add(new ToolStripMenuItem("随时渐变切换；当前动作继续（Ctrl + 右键）"){Enabled=false});menu.Items.Add(formMenu);
  }
  void UpdateFormMenu(){
   if(formMenu==null)return;
   formMenu.Visible=(Control.ModifierKeys&Keys.Shift)!=0;
   a1FormItem.Checked=CurrentForm==CharacterForm.AnemoneI;a2FormItem.Checked=CurrentForm==CharacterForm.AnemoneII;
   a1FormItem.Enabled=a1FormItem.Checked||FormSwitchReady;a2FormItem.Enabled=a2FormItem.Checked||FormSwitchReady;
   characterTitle.Text=CurrentForm==CharacterForm.AnemoneI?"切换到 Anemone II（A2）":"Anemone II（A2） · 切回 A1";
   if(formTransition!=null&&formTransition.Loading&&CurrentForm!=renderer.Form)characterTitle.Text="正在准备 "+CharacterForms.Name(CurrentForm)+" · 点击取消";
   characterTitle.Enabled=FormSwitchReady;
  }
 }
}

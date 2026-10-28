using System;using System.Drawing;using System.IO;using System.Windows.Forms;
namespace JouchoPet {
 // Explicit destination selection; clicks are consumed, never forwarded to desktop apps.
 sealed class UtilityPointPicker:Form {
  internal Point Selected;
  internal UtilityPointPicker(string instruction){Text=instruction;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;Bounds=SystemInformation.VirtualScreen;BackColor=Color.FromArgb(30,42,58);Opacity=.25;Cursor=Cursors.Cross;KeyPreview=true;
   var label=new Label{AutoSize=true,Text=instruction+"  ·  Esc / 右键取消",ForeColor=Color.White,BackColor=Color.Black,Font=new Font("Microsoft YaHei",16),Location=new Point(Screen.PrimaryScreen.WorkingArea.Left-Left+30,Screen.PrimaryScreen.WorkingArea.Top-Top+30)};Controls.Add(label);
   MouseDown+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left){Selected=PointToScreen(e.Location);DialogResult=DialogResult.OK;}else DialogResult=DialogResult.Cancel;Close();};
   KeyDown+=delegate(object s,KeyEventArgs e){if(e.KeyCode==Keys.Escape){DialogResult=DialogResult.Cancel;Close();}};
  }
 }
 sealed class UtilityIconPicker:Form {
  internal string SelectedId;readonly ListBox list=new ListBox();
  internal UtilityIconPicker(DesktopIcons.Item[] icons){Text="选择悬浮搬运的桌面图标";StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(560,380);MinimizeBox=MaximizeBox=false;FormBorderStyle=FormBorderStyle.FixedDialog;
   var label=new Label{Text="只改变桌面图标位置，不修改文件。随后点击落点。\n可从原有“恢复图标位置”菜单撤销；桌面须关闭自动排列。",Dock=DockStyle.Top,Height=50};list.Dock=DockStyle.Fill;list.HorizontalScrollbar=true;
   foreach(var icon in icons)list.Items.Add(icon.Id);var ok=new Button{Text="选择落点",Dock=DockStyle.Bottom,Height=36};ok.Click+=delegate{if(list.SelectedIndex<0)return;SelectedId=icons[list.SelectedIndex].Id;DialogResult=DialogResult.OK;Close();};Controls.Add(list);Controls.Add(label);Controls.Add(ok);AcceptButton=ok;}
 }
}

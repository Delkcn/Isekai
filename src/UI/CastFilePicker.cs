using System;using System.Drawing;using System.IO;using System.Windows.Forms;
namespace JouchoPet {
 sealed class CastFilePicker:Form {
  readonly ListView files=new ListView();readonly Label status=new Label();readonly Button accept=new Button();
  readonly Func<string[]> readFiles;
  internal string SelectedPath{get;private set;}
  internal CastFilePicker(Func<string[]> source=null,string title="选择施法目标",string instruction="选择一个桌面文件，法弹命中图标后将使用默认应用打开它。",string acceptText="锁定并施法"){
   readFiles=source??ReadDesktopFiles;
   Text=title;ClientSize=new Size(700,410);MinimumSize=new Size(560,340);StartPosition=FormStartPosition.CenterScreen;ShowInTaskbar=false;TopMost=true;
   Font=new Font("Microsoft YaHei UI",9);AutoScaleMode=AutoScaleMode.Dpi;
   var hint=new Label{Text=instruction,Dock=DockStyle.Top,Height=48,Padding=new Padding(12)};
   files.Dock=DockStyle.Fill;files.View=View.Details;files.FullRowSelect=true;files.MultiSelect=false;files.HideSelection=false;files.Columns.Add("文件",210);files.Columns.Add("位置",430);
   var footer=new Panel{Dock=DockStyle.Bottom,Width=ClientSize.Width,Height=75};status.SetBounds(12,5,670,25);status.Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right;
   accept.Text=acceptText;accept.Enabled=false;accept.SetBounds(555,35,130,30);accept.Anchor=AnchorStyles.Right|AnchorStyles.Bottom;
   var cancel=new Button{Text="取消",DialogResult=DialogResult.Cancel};cancel.SetBounds(440,35,100,30);cancel.Anchor=AnchorStyles.Right|AnchorStyles.Bottom;
   var refresh=new Button{Text="刷新列表"};refresh.SetBounds(12,35,110,30);refresh.Click+=delegate{Reload();};
   files.SelectedIndexChanged+=delegate{accept.Enabled=files.SelectedItems.Count==1;};accept.Click+=delegate{if(files.SelectedItems.Count==1){SelectedPath=(string)files.SelectedItems[0].Tag;DialogResult=DialogResult.OK;Close();}};
   files.DoubleClick+=delegate{if(accept.Enabled)accept.PerformClick();};
   footer.Controls.AddRange(new Control[]{status,refresh,cancel,accept});Controls.Add(files);Controls.Add(footer);Controls.Add(hint);AcceptButton=accept;CancelButton=cancel;Shown+=delegate{Reload();};
  }
  static string[] ReadDesktopFiles(){var paths=new System.Collections.Generic.List<string>();using(var shell=new DesktopIcons())foreach(var item in shell.Read())if(File.Exists(item.Id))paths.Add(item.Id);return paths.ToArray();}
  void Reload(){files.Items.Clear();accept.Enabled=false;
   try{foreach(var path in readFiles()){var row=new ListViewItem(new[]{System.IO.Path.GetFileName(path),path});row.Tag=path;files.Items.Add(row);}files.Sorting=SortOrder.Ascending;status.Text=files.Items.Count==0?"没有可选文件。请将文件或快捷方式放到桌面后刷新。":"请选择目标；施法时请让桌面图标保持可见。";}
   catch(Exception ex){status.Text="无法读取桌面文件："+ex.Message;}
  }
 }
}

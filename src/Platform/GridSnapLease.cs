using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
namespace JouchoPet {
 // Persist the original grid flag before changing it. Only this flag is touched.
 // Normal completion/cancel/exit restores immediately; a killed app recovers on
 // its next launch after verifying Explorer's process and window identity.
 sealed class GridSnapLease : IDisposable {
  static readonly string Journal=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"IsekaijouchoPet","grid-snap-lease.txt");
  readonly DesktopIconView view;
  bool owned;
  public GridSnapLease(DesktopIconView target) {
   view=target;
   Recover();
   if(File.Exists(Journal))throw new InvalidOperationException("另一项推动尚未恢复网格设置，请先结束该操作。");
   if((view.ExtendedStyle&0x80000)==0)return;
   uint pid;GetWindowThreadProcessId(view.Window,out pid);
   using(var remote=Process.GetProcessById((int)pid))using(var owner=Process.GetCurrentProcess()) {
    Directory.CreateDirectory(Path.GetDirectoryName(Journal));
    File.WriteAllLines(Journal,new[]{view.Window.ToInt64().ToString(),pid.ToString(),remote.StartTime.ToUniversalTime().Ticks.ToString(),owner.Id.ToString(),owner.StartTime.ToUniversalTime().Ticks.ToString()});
   }
   owned=true;
   try{view.SetGrid(false);if((view.ExtendedStyle&0x80000)!=0)throw new InvalidOperationException("无法临时关闭网格吸附，推动已暂停。");}
   catch{Dispose();throw;}
  }
  public void Dispose() {
   if(!owned)return;
   view.SetGrid(true);
   if((view.ExtendedStyle&0x80000)==0)throw new InvalidOperationException("网格吸附恢复失败，已保留恢复记录。请重新启动桌宠重试。");
   if(File.Exists(Journal))File.Delete(Journal);owned=false;
  }
  static bool SameProcess(int pid,long start) {
   try{using(var p=Process.GetProcessById(pid))return !p.HasExited && p.StartTime.ToUniversalTime().Ticks==start;}catch(ArgumentException){return false;}
  }
  public static void Recover() {
   if(!File.Exists(Journal))return;
   string[] fields=File.ReadAllLines(Journal);
   if(fields.Length!=5)throw new InvalidDataException("网格设置恢复记录异常，请保留 grid-snap-lease.txt 以便检查。");
   IntPtr window=new IntPtr(long.Parse(fields[0]));uint pid;GetWindowThreadProcessId(window,out pid);
   if(SameProcess(int.Parse(fields[3]),long.Parse(fields[4])))return;
   var name=new StringBuilder(64);GetClassName(window,name,name.Capacity);
   if(pid==uint.Parse(fields[1]) && SameProcess((int)pid,long.Parse(fields[2])) && name.ToString()=="SysListView32") {
    using(var view=new DesktopIconView(window)) {
     view.SetGrid(true);if((view.ExtendedStyle&0x80000)==0)throw new InvalidOperationException("无法恢复上次推动前的网格设置。");
    }
   }
   File.Delete(Journal);
  }
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr hwnd,StringBuilder name,int count);
 }
}

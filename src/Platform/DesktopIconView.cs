using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
namespace JouchoPet {
 // Move the displayed list-view item itself, not a deferred Shell layout request.
 // The only remote memory accessed is our own eight-byte POINT allocation.
 sealed class DesktopIconView : IDisposable {
  readonly IntPtr window;
  IntPtr process,point;
  bool uncertain;
  GridSnapLease grid;
  public IntPtr Window {get{return window;}}
  public static bool GridEnabled(IntPtr window) {
   if(window==IntPtr.Zero)return false;
   IntPtr value;
   if(SendMessageTimeout(window,0x1037,IntPtr.Zero,IntPtr.Zero,3,500,out value)==IntPtr.Zero)throw new InvalidOperationException("无法读取桌面网格设置。");
   return (value.ToInt64()&0x80000)!=0;
  }
  public static IntPtr Find(IntPtr parent) {
   IntPtr found=IntPtr.Zero;
   EnumChildWindows(parent,delegate(IntPtr child,IntPtr unused) {
    var name=new StringBuilder(64);GetClassName(child,name,name.Capacity);
    if(name.ToString()=="SysListView32"){found=child;return false;}return true;
   },IntPtr.Zero);
   return found;
  }
  public DesktopIconView(IntPtr handle) {
   window=handle;if(handle==IntPtr.Zero)throw new InvalidOperationException("未找到桌面图标显示控件。");
   uint pid;GetWindowThreadProcessId(window,out pid);
   process=OpenProcess(0x38,false,pid);
   if(process==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error(),"无法访问桌面图标位置缓冲区。");
   point=VirtualAllocEx(process,IntPtr.Zero,new UIntPtr(8),0x3000,4);
   if(point==IntPtr.Zero){Dispose();throw new Win32Exception(Marshal.GetLastWin32Error());}
  }
  IntPtr Send(uint message,int index,IntPtr parameter) {
   IntPtr result;
   if(SendMessageTimeout(window,message,new IntPtr(index),parameter,3,500,out result)==IntPtr.Zero) {
    // A timed-out receiver might still use the POINT; do not free it underneath
    // Explorer. At most eight bytes remain until that process exits.
    uncertain=true;throw new InvalidOperationException("桌面图标控件响应超时，推动已暂停。");
   }
   return result;
  }
  public int Count {get{return Send(0x1004,0,IntPtr.Zero).ToInt32();}}
  public long ExtendedStyle {get{return Send(0x1037,0,IntPtr.Zero).ToInt64();}}
  internal void SetGrid(bool enabled){Send(0x1036,0x80000,new IntPtr(enabled?0x80000:0));}
  public void BeginSmooth(){if(grid==null)grid=new GridSnapLease(this);}
  public Point Read(int index) {
   if(Send(0x1010,index,point)==IntPtr.Zero)throw new InvalidOperationException("无法读取图标的显示位置。");
   byte[] bytes=new byte[8];UIntPtr read;
   if(!ReadProcessMemory(process,point,bytes,new UIntPtr(8),out read) || read.ToUInt64()!=8)throw new Win32Exception(Marshal.GetLastWin32Error());
   return new Point(BitConverter.ToInt32(bytes,0),BitConverter.ToInt32(bytes,4));
  }
  public Point Move(int index,Point expected,Point target) {
   if(Read(index)!=expected)throw new InvalidOperationException("图标显示位置被外部修改，推动已暂停。");
   byte[] bytes=new byte[8];Array.Copy(BitConverter.GetBytes(target.X),0,bytes,0,4);Array.Copy(BitConverter.GetBytes(target.Y),0,bytes,4,4);UIntPtr written;
   if(!WriteProcessMemory(process,point,bytes,new UIntPtr(8),out written) || written.ToUInt64()!=8)throw new Win32Exception(Marshal.GetLastWin32Error());
   Send(0x1031,index,point); // LVM_SETITEMPOSITION32: signed 32-bit desktop coordinates
   return Read(index);
  }
  public void Dispose() {
   try{if(grid!=null){grid.Dispose();grid=null;}}
   finally {
    if(point!=IntPtr.Zero && process!=IntPtr.Zero && !uncertain)VirtualFreeEx(process,point,UIntPtr.Zero,0x8000);
    if(process!=IntPtr.Zero)CloseHandle(process);process=point=IntPtr.Zero;
   }
  }
  delegate bool EnumProc(IntPtr window,IntPtr data);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent,EnumProc callback,IntPtr data);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr hwnd,StringBuilder name,int count);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
  [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SendMessageTimeout(IntPtr hwnd,uint message,IntPtr w,IntPtr l,uint flags,uint timeout,out IntPtr result);
  [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,uint pid);
  [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr process,IntPtr address,UIntPtr size,uint allocation,uint protection);
  [DllImport("kernel32.dll",SetLastError=true)] static extern bool VirtualFreeEx(IntPtr process,IntPtr address,UIntPtr size,uint type);
  [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr process,IntPtr address,byte[] buffer,UIntPtr size,out UIntPtr read);
  [DllImport("kernel32.dll",SetLastError=true)] static extern bool WriteProcessMemory(IntPtr process,IntPtr address,byte[] buffer,UIntPtr size,out UIntPtr written);
  [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
 }
}

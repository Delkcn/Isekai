using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;

namespace JouchoPet
{
    // Shell identities plus native view coordinates; no filesystem item is opened,
    // renamed, moved or deleted by this class.
    sealed class DesktopIcons : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)] internal struct XY { public int X,Y; public XY(Point p){X=p.X;Y=p.Y;} public Point Point { get{return new Point(X,Y);} } }
        internal sealed class Item { public string Id; public int Index; public Point Position; }
        object windows, desktop, browserObject, viewObject, folderObject;
        FolderView view;
        DesktopIconView visibleView;
        IntPtr parent, hwnd;
        public Point Spacing { get; private set; }
        public int IconSize { get; private set; }
        public IntPtr Window { get { return hwnd; } }
        public string MovementBackend {get{return DesktopIconView.Find(hwnd)!=IntPtr.Zero?"图标显示控件（逐步定位）":"Shell 兼容定位";}}
        public bool GridAligned {get{return DesktopIconView.GridEnabled(DesktopIconView.Find(hwnd));}}
        static void Check(int hr) { Marshal.ThrowExceptionForHR(hr); }
        public DesktopIcons()
        {
            string stage="查找桌面";
            try {
                windows=Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39")));
                object[] args={0,0,8,0,1}; // SWC_DESKTOP, SWFO_NEEDDISPATCH
                ParameterModifier modifier=new ParameterModifier(5);modifier[3]=true;
                desktop=windows.GetType().InvokeMember("FindWindowSW",BindingFlags.InvokeMethod,null,windows,args,new ParameterModifier[]{modifier},null,null);
                if(desktop==null)throw new InvalidOperationException("未找到 Windows 桌面。请确认资源管理器正在运行。");
                Guid service=new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837"),iid=new Guid("000214E2-0000-0000-C000-000000000046");
                Check(((ServiceProvider)desktop).QueryService(ref service,ref iid,out browserObject));
                stage="IShellView";
                ShellViewWindow activeView;
                Check(((ShellBrowser)browserObject).QueryActiveShellView(out activeView));
                viewObject=activeView;
                view=(FolderView)viewObject;
                stage="IShellView.GetWindow";
                Check(activeView.GetWindow(out hwnd));
                if(hwnd==IntPtr.Zero)throw new InvalidOperationException("桌面视图没有有效窗口句柄。");
                stage="桌面文件夹标识";
                Guid persist=new Guid("1AC3D9F0-175C-11D1-95BE-00609797EA4F");
                Check(view.GetFolder(ref persist,out folderObject));
                Check(((PersistFolder2)folderObject).GetCurFolder(out parent));
                XY spacing;Check(view.GetSpacing(out spacing));
                Spacing=new Point(Math.Max(64,spacing.X),Math.Max(64,spacing.Y));
                uint mode;int iconSize;Check(((FolderView2)viewObject).GetViewModeAndIconSize(out mode,out iconSize));IconSize=Math.Max(16,iconSize);
                // Enumeration is read-only and also works with auto-arrange enabled.
                // Only movement requires manual arrangement.
            } catch(Exception ex) { Dispose();throw new InvalidOperationException("初始化桌面图标接口失败（"+stage+"）："+ex.Message,ex); }
        }
        public void EnsureManual()
        {
            int hr=view.GetAutoArrange();Check(hr);
            if(hr==0)throw new InvalidOperationException("请先在桌面右键 → 查看中关闭“自动排列图标”，再开启图标搬运。程序不会修改此设置。");
        }
        string Identity(IntPtr child)
        {
            IntPtr absolute=ILCombine(parent,child),name=IntPtr.Zero;
            if(absolute==IntPtr.Zero)throw new OutOfMemoryException();
            try { Check(SHGetNameFromIDList(absolute,0x80028000,out name));return Marshal.PtrToStringUni(name); }
            finally { if(name!=IntPtr.Zero)Marshal.FreeCoTaskMem(name);Marshal.FreeCoTaskMem(absolute); }
        }
        public List<Item> Read()
        {
            int count;Check(view.ItemCount(2,out count)); // SVGIO_ALLVIEW
            var result=new List<Item>();
            for(int i=0;i<count;i++) {
                IntPtr pidl=IntPtr.Zero;
                try { Check(view.Item(i,out pidl));XY p;Check(view.GetItemPosition(pidl,out p));result.Add(new Item{Id=Identity(pidl),Index=i,Position=p.Point}); }
                finally {if(pidl!=IntPtr.Zero)Marshal.FreeCoTaskMem(pidl);}
            }
            return result;
        }
        public Point ScreenPoint(Point p) { XY xy=new XY(p);if(!ClientToScreen(hwnd,ref xy))throw new InvalidOperationException("桌面坐标转换失败。");return xy.Point; }
        internal Rectangle IconScreenRectangle(Item item){
            IntPtr list=DesktopIconView.Find(hwnd);
            if(list==IntPtr.Zero)throw new InvalidOperationException("当前桌面不提供可读取的图标边界。");
            using(var reader=new DesktopIconView(list)){
                if(item.Index<0||item.Index>=reader.Count||reader.Read(item.Index)!=item.Position)throw new InvalidOperationException("图标位置已改变，请重新选择文件。");
                Rectangle r=reader.IconRectangle(item.Index);XY a=new XY(r.Location),b=new XY(new Point(r.Right,r.Bottom));
                if(!ClientToScreen(list,ref a)||!ClientToScreen(list,ref b))throw new InvalidOperationException("图标屏幕边界转换失败。");
                return Rectangle.FromLTRB(a.X,a.Y,b.X,b.Y);
            }
        }
        internal class PositionFailure : Exception {
            public readonly Point Actual;
            public PositionFailure(Point actual,string message,Exception inner=null):base(message,inner){Actual=actual;}
        }
        internal sealed class PositionRejected : PositionFailure { public PositionRejected(Point actual):base(actual,"桌面未接受连续位置。请关闭桌面“将图标与网格对齐”，或检查第三方桌面管理软件。搬运已暂停，可使用恢复入口。"){} }
        public bool Move(Item item,Point expected,Point target)
        {
            EnsureManual();
            IntPtr pidl=IntPtr.Zero;
            try {
                Check(view.Item(item.Index,out pidl));
                if(Identity(pidl)!=item.Id)return false;
                XY actual;Check(view.GetItemPosition(pidl,out actual));
                if(actual.Point!=expected)return false; // user or Explorer changed it
                IntPtr list=DesktopIconView.Find(hwnd);
                if(list!=IntPtr.Zero) {
                    if(visibleView==null || visibleView.Window!=list) {
                        if(visibleView!=null)visibleView.Dispose();visibleView=new DesktopIconView(list);
                    }
                    int count;Check(view.ItemCount(2,out count));
                    if(visibleView.Count!=count || visibleView.Read(item.Index)!=expected)return false;
                    visibleView.BeginSmooth();
                    Point shown=visibleView.Move(item.Index,expected,target);
                    if(shown!=target)throw new PositionRejected(shown);
                } else {
                    // Retain compatibility with third-party/non-list desktop views.
                    XY destination=new XY(target);
                    Check(view.SelectAndPositionItems(1,new IntPtr[]{pidl},new XY[]{destination},0x80));
                }
                Check(view.GetItemPosition(pidl,out actual));
                if(actual.Point!=target)throw new PositionRejected(visibleView!=null?visibleView.Read(item.Index):actual.Point);
                try { DesktopIconPaint.Present(list!=IntPtr.Zero?list:hwnd,expected,target,Spacing,IconSize); }
                catch(Exception ex) {
                    // Position has already been accepted. Retain it for recovery
                    // even if the view disappears or its paint request fails.
                    throw new PositionFailure(actual.Point,"图标位置已更新，但画面刷新失败，推动已暂停。"+ex.Message,ex);
                }
                return true;
            } finally {if(pidl!=IntPtr.Zero)Marshal.FreeCoTaskMem(pidl);}
        }
        public Point? FinishSmooth(Item item,Point expected)
        {
            if(visibleView==null)return null;
            bool ownedPosition=false;
            foreach(var current in Read())if(current.Id==item.Id){ownedPosition=current.Position==expected;break;}
            // Restoring snap-to-grid may align a cancelled, partial-cell move.
            // Report that accepted location so the original-position journal stays usable.
            try{visibleView.Dispose();}finally{visibleView=null;}
            if(ownedPosition)foreach(var current in Read())if(current.Id==item.Id)return current.Position;
            return null; // Do not adopt a position changed externally before restoration.
        }
        public static bool Unobscured(Point point,IntPtr pet)
        {
            IntPtr hit=WindowFromPoint(new XY(point));
            if(hit==pet)return true;
            uint hitProcess,petProcess;
            GetWindowThreadProcessId(hit,out hitProcess);GetWindowThreadProcessId(pet,out petProcess);
            // Our context menu/tooltip is a separate top-level HWND. Inspecting
            // progress must not cancel the operation as an external obstruction.
            if(pet!=IntPtr.Zero && petProcess!=0 && hitProcess==petProcess)return true;
            IntPtr root=GetAncestor(hit,2);
            var name=new System.Text.StringBuilder(128);GetClassName(root,name,name.Capacity);
            return name.ToString()=="Progman" || name.ToString()=="WorkerW";
        }
        public void Dispose()
        {
            Exception failure=null;
            if(visibleView!=null){try{visibleView.Dispose();}catch(Exception ex){failure=ex;}visibleView=null;}
            if(parent!=IntPtr.Zero){Marshal.FreeCoTaskMem(parent);parent=IntPtr.Zero;}
            // view and ShellViewWindow are casts of viewObject: release each acquired RCW once.
            foreach(object o in new[]{folderObject,viewObject,browserObject,desktop,windows})
                if(o!=null && Marshal.IsComObject(o))Marshal.ReleaseComObject(o);
            folderObject=viewObject=browserObject=desktop=windows=null;view=null;
            if(failure!=null)throw failure;
        }
        [DllImport("shell32.dll")] static extern IntPtr ILCombine(IntPtr a,IntPtr b);
        [DllImport("shell32.dll")] static extern int SHGetNameFromIDList(IntPtr pidl,uint kind,out IntPtr name);
        [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr h,ref XY p);
        [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(XY p);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint process);
        [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr h,uint flags);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h,System.Text.StringBuilder s,int n);

        [ComImport,Guid("6D5140C1-7436-11CE-8034-00AA006009FA"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface ServiceProvider { [PreserveSig]int QueryService(ref Guid service,ref Guid iid,[MarshalAs(UnmanagedType.IUnknown)]out object result); }
        // Query IShellView directly. Some desktop proxies do not expose its base IID_IOleWindow.
        [ComImport,Guid("000214E3-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface ShellViewWindow { [PreserveSig]int GetWindow(out IntPtr h); [PreserveSig]int ContextSensitiveHelp(bool enter); }
        [ComImport,Guid("000214E2-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface ShellBrowser {
            [PreserveSig]int GetWindow(out IntPtr h); [PreserveSig]int ContextSensitiveHelp(bool enter);
            [PreserveSig]int InsertMenusSB(IntPtr menu,IntPtr widths); [PreserveSig]int SetMenuSB(IntPtr menu,IntPtr hole,IntPtr active);
            [PreserveSig]int RemoveMenusSB(IntPtr menu); [PreserveSig]int SetStatusTextSB(IntPtr text);
            [PreserveSig]int EnableModelessSB(bool enable); [PreserveSig]int TranslateAcceleratorSB(IntPtr msg,ushort id);
            [PreserveSig]int BrowseObject(IntPtr pidl,uint flags); [PreserveSig]int GetViewStateStream(uint mode,out IntPtr stream);
            [PreserveSig]int GetControlWindow(uint id,out IntPtr h); [PreserveSig]int SendControlMsg(uint id,uint msg,IntPtr w,IntPtr l,out IntPtr result);
            [PreserveSig]int QueryActiveShellView(out ShellViewWindow shellView);
        }
        [ComImport,Guid("1AC3D9F0-175C-11D1-95BE-00609797EA4F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface PersistFolder2 {
            [PreserveSig]int GetClassID(out Guid id); [PreserveSig]int Initialize(IntPtr pidl); [PreserveSig]int GetCurFolder(out IntPtr pidl);
        }
        [ComImport,Guid("1AF3A467-214F-4298-908E-06B03E0B39F9"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface FolderView2 {
            [PreserveSig]int GetCurrentViewMode(out uint mode); [PreserveSig]int SetCurrentViewMode(uint mode);
            [PreserveSig]int GetFolder(ref Guid iid,[MarshalAs(UnmanagedType.IUnknown)]out object folder);
            [PreserveSig]int Item(int index,out IntPtr pidl); [PreserveSig]int ItemCount(uint flags,out int count);
            [PreserveSig]int Items(uint flags,ref Guid iid,out IntPtr result);
            [PreserveSig]int GetSelectionMarkedItem(out int index); [PreserveSig]int GetFocusedItem(out int index);
            [PreserveSig]int GetItemPosition(IntPtr pidl,out XY p); [PreserveSig]int GetSpacing(out XY p);
            [PreserveSig]int GetDefaultSpacing(out XY p); [PreserveSig]int GetAutoArrange();
            [PreserveSig]int SelectItem(int index,uint flags);
            [PreserveSig]int SelectAndPositionItems(uint count,[MarshalAs(UnmanagedType.LPArray,SizeParamIndex=0)]IntPtr[] pidls,[MarshalAs(UnmanagedType.LPArray,SizeParamIndex=0)]XY[] points,uint flags);
            // Uncalled slots preserve the native IFolderView2 vtable order.
            void SetGroupBy();void GetGroupBy();void SetViewProperty();void GetViewProperty();
            void SetTileViewProperties();void SetExtendedTileViewProperties();void SetText();
            void SetCurrentFolderFlags();void GetCurrentFolderFlags();void GetSortColumnCount();
            void SetSortColumns();void GetSortColumns();void GetItem();void GetVisibleItem();
            void GetSelectedItem();void GetSelection();void GetSelectionState();void InvokeVerbOnSelection();
            void SetViewModeAndIconSize();
            [PreserveSig]int GetViewModeAndIconSize(out uint mode,out int size);
        }

        [ComImport,Guid("CDE725B0-CCC9-4519-917E-325D72FAB4CE"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface FolderView {
            [PreserveSig]int GetCurrentViewMode(out uint mode); [PreserveSig]int SetCurrentViewMode(uint mode);
            [PreserveSig]int GetFolder(ref Guid iid,[MarshalAs(UnmanagedType.IUnknown)]out object folder);
            [PreserveSig]int Item(int index,out IntPtr pidl); [PreserveSig]int ItemCount(uint flags,out int count);
            [PreserveSig]int Items(uint flags,ref Guid iid,out IntPtr result);
            [PreserveSig]int GetSelectionMarkedItem(out int index); [PreserveSig]int GetFocusedItem(out int index);
            [PreserveSig]int GetItemPosition(IntPtr pidl,out XY p); [PreserveSig]int GetSpacing(out XY p);
            [PreserveSig]int GetDefaultSpacing(out XY p); [PreserveSig]int GetAutoArrange();
            [PreserveSig]int SelectItem(int index,uint flags);
            [PreserveSig]int SelectAndPositionItems(uint count,[MarshalAs(UnmanagedType.LPArray,SizeParamIndex=0)]IntPtr[] pidls,[MarshalAs(UnmanagedType.LPArray,SizeParamIndex=0)]XY[] points,uint flags);
        }
    }
}

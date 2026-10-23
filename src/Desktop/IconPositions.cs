using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
namespace JouchoPet
{
    sealed class IconPositions
    {
        sealed class Entry { public string Id,Layout; public Point Original,Previous,Last; }
        readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>(StringComparer.Ordinal);
        readonly string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"IsekaijouchoPet","icon-positions.txt");
        public static string Layout() {
            var b=new StringBuilder();foreach(Screen s in Screen.AllScreens)b.Append(s.DeviceName).Append(s.Bounds.ToString());return b.ToString();
        }
        public IconPositions() {
            if(!File.Exists(path))return;
            foreach(string line in File.ReadAllLines(path)) {
                string[] p=line.Split('|');if(p.Length!=8)throw new InvalidDataException("图标恢复记录格式异常，已禁止搬运以保护原记录。");
                string id=Encoding.UTF8.GetString(Convert.FromBase64String(p[0]));
                entries[id]=new Entry{Id=id,Layout=Encoding.UTF8.GetString(Convert.FromBase64String(p[1])),Original=new Point(int.Parse(p[2],CultureInfo.InvariantCulture),int.Parse(p[3],CultureInfo.InvariantCulture)),Previous=new Point(int.Parse(p[4],CultureInfo.InvariantCulture),int.Parse(p[5],CultureInfo.InvariantCulture)),Last=new Point(int.Parse(p[6],CultureInfo.InvariantCulture),int.Parse(p[7],CultureInfo.InvariantCulture))};
            }
        }
        public void BeforeMove(string id,Point current,Point next) {
            Entry e;string layout=Layout();
            if(!entries.TryGetValue(id,out e) || e.Layout!=layout || (current!=e.Last && current!=e.Previous)) {
                e=new Entry{Id=id,Layout=layout,Original=current};entries[id]=e;
            }
            e.Previous=current;e.Last=next;Save(); // durable intent BEFORE touching Explorer
        }
        void Save() {
            Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";
            using(var writer=new StreamWriter(temp,false,new UTF8Encoding(false)))foreach(Entry e in entries.Values)
                writer.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(e.Id))+"|"+Convert.ToBase64String(Encoding.UTF8.GetBytes(e.Layout))+"|"+e.Original.X+"|"+e.Original.Y+"|"+e.Previous.X+"|"+e.Previous.Y+"|"+e.Last.X+"|"+e.Last.Y);
            if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
        }
        public string Restore(DesktopIcons shell) {
            int restored=0,skipped=0;string layout=Layout();
            List<DesktopIcons.Item> items=shell.Read();
            foreach(DesktopIcons.Item item in items) {
                Entry e;if(!entries.TryGetValue(item.Id,out e))continue;
                if(e.Layout!=layout || (item.Position!=e.Last && item.Position!=e.Previous)){skipped++;continue;}
                bool occupied=false;
                foreach(var other in items)if(other.Id!=item.Id && Math.Abs(other.Position.X-e.Original.X)<shell.Spacing.X*.8 && Math.Abs(other.Position.Y-e.Original.Y)<shell.Spacing.Y*.8)occupied=true;
                if(occupied){skipped++;continue;}
                if(shell.Move(item,item.Position,e.Original)){item.Position=e.Original;entries.Remove(item.Id);Save();restored++;}else skipped++;
            }
            return "已恢复 "+restored+" 个图标。跳过 "+skipped+" 个位置已改变、目标被占用或屏幕布局变化的图标。";
        }
    }
}

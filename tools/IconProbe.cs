using System;
using System.Drawing;
using System.IO;
using System.Text;
using JouchoPet;
class IconProbe {
 [STAThread] static void Main(string[] args) {
  try {using(var icons=new DesktopIcons()) {
   var items=icons.Read();Console.WriteLine("Desktop items: "+items.Count+"; spacing: "+icons.Spacing);
   int visible=0;foreach(var item in items)if(DesktopIcons.Unobscured(icons.ScreenPoint(item.Position),IntPtr.Zero))visible++;
   Console.WriteLine("Unobscured item origins: "+visible);
   try{icons.EnsureManual();Console.WriteLine("Manual arrangement: yes");}catch(Exception ex){Console.WriteLine(ex.Message);}
   if(args.Length==1 && args[0]=="--roundtrip") {
    icons.EnsureManual();if(items.Count==0)throw new Exception("No test item available");
    var item=items[0];Point original=item.Position,last=original,target=new Point(original.X+12,original.Y);
    string recovery=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"qa","v062","icon-roundtrip-recovery.txt");
    File.WriteAllText(recovery,Convert.ToBase64String(Encoding.UTF8.GetBytes(item.Id))+"\n"+original.X+","+original.Y+"\n"+target.X+","+target.Y);
    try {
     try {if(!icons.Move(item,original,target))throw new Exception("Item changed before test");last=target;Console.WriteLine("PASS Explorer accepted +12 pixel displacement");}
     catch(DesktopIcons.PositionRejected ex){last=ex.Actual;throw;}
    }finally {
     if(last!=original) {
      if(!icons.Move(item,last,original))throw new Exception("Restore refused: item changed externally. Recovery coordinates retained.");
      Console.WriteLine("PASS restored original position");
     }else Console.WriteLine("Original position unchanged");
     File.AppendAllText(recovery,"\nRESTORED_OR_UNCHANGED");
    }
   }
  }}catch(Exception ex){Console.WriteLine(ex.ToString());Environment.ExitCode=1;}
 }
}

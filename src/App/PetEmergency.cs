using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace JouchoPet {
 sealed partial class Pet {
  IHungApplications hungApplications;bool emergencyPending,emergencyRunning;int emergencyGeneration;
  IHungApplications HungApps{get{return hungApplications??(hungApplications=new HungApplications());}}
  void CancelEmergency(){emergencyPending=false;emergencyGeneration++;}
  void EndHungAtSigil(double dt){
   if(crossSigil||!emergencyPending||menuOpen||!SigilVisible||!CastingMotion.ValidDelta(dt)||daily.Age>=.68||daily.Age+dt<.68)return;
   emergencyPending=false;emergencyRunning=true;CompleteEmergency(emergencyGeneration);
  }
  async void CompleteEmergency(int generation){
   int ended=0,failed=0;var errors=new List<string>();
   try{
    var service=HungApps;var targets=await Task.Run(()=>service.Read());var seen=new HashSet<string>();
    foreach(var target in targets){
     if(IsDisposed||Disposing||generation!=emergencyGeneration)break;
     if(target==null||!seen.Add(target.Id+":"+target.Created))continue;
     try{var result=await service.EndAsync(target);if(result.Success)ended++;else{failed++;if(errors.Count<3)errors.Add(target.Name+"："+result.Error);}}
     catch(Exception ex){failed++;if(errors.Count<3)errors.Add(target.Name+"："+ex.Message);}
    }
    if(!IsDisposed&&!Disposing){
     string message="米字法阵：已结束 "+ended+" 个程序"+(failed>0?"，未结束 "+failed+" 个":"");
     if(generation!=emergencyGeneration)message+="，剩余处理已取消";
     else if(targets.Count==0)message="没有发现未响应程序";
     CrossSelectionMessage(message);if(errors.Count>0)UtilityMessage(message+"；"+String.Join("；",errors));
    }
   }catch(Exception ex){if(!IsDisposed&&!Disposing)CrossSelectionMessage("读取未响应程序失败："+ex.Message);}
   finally{emergencyRunning=false;}
  }
  void AddEmergencySigilMenu(){
   var item=new ToolStripMenuItem("米字法阵 · 结束全部未响应程序"){ToolTipText="直接施法并强制结束未响应程序，未保存内容会丢失。"};
   item.Click+=delegate{if(!MiSigil())CrossSelectionMessage("请等待当前动作或结束程序操作完成。");};menu.Items.Add(item);
   menu.Items.Add("打开任务管理器",null,delegate{try{using(var process=Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory,"Taskmgr.exe")){UseShellExecute=true})) {}}catch(Exception ex){CrossSelectionMessage(ex.Message);}});
  }
 }
}
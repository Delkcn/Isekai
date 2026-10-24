using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
namespace JouchoPet {
 sealed class AnimationParameter {
  public readonly string Key,Group,Label,Unit,Hint;
  public readonly double Default,Min,Max,Step;
  public AnimationParameter(string key,string group,string label,string unit,double value,double min,double max,double step,string hint) {
   Key=key;Group=group;Label=label;Unit=unit;Default=value;Min=min;Max=max;Step=step;Hint=hint;
  }
 }
 // These are the user's approved 24 values, fixed in the shipped app; run speed updated by request in 0.7.24.
 // The old editor/import helpers remain for internal animation regression tools only.
 // Pet calls UseFixedValues before creating any renderer; it never loads user JSON.
 static class AnimationParameters {
  public static readonly AnimationParameter[] Specs={
   new AnimationParameter("runSpeed","小跑","移动速度","像素/秒",198,60,270,2,"以角色高度 300 像素为基准；步频随移动速度联动。"),
   new AnimationParameter("stride","小跑","步幅","% 身高",19,15,23,.5,"步幅越短，相同移动速度下步频越快。"),
   new AnimationParameter("footLift","小跑","抬脚高度","% 身高",6.8,6,12,.5,"只影响小跑的摆腿阶段。"),
   new AnimationParameter("bodyRise","小跑","身体起伏","% 身高",2.5,0,2.5,.1,"腾起时身体上下运动的幅度。"),
   new AnimationParameter("kneeBase","小跑","基础屈膝","% 身高",.8,0,1.8,.1,"膝部向前的弯曲量；脚底位置保持原轨迹。"),
   new AnimationParameter("kneeLoad","小跑","承重加深屈膝","% 身高",.5,0,1,.1,"脚落地承重时额外增加的弯曲量。"),
   new AnimationParameter("runLean","小跑","身体前倾","度",4,0,7,.5,"以髋部为支点；不改变普通走路。"),
   new AnimationParameter("armSwing","摆臂与手腕","摆臂幅度","度",15.35,0,38,1,"保持左腿配右臂、右腿配左臂的节奏。"),
   new AnimationParameter("elbowBend","摆臂与手腕","屈臂角度","度",38.15,0,70,1,"小跑时相对原姿态向前屈肘的角度。0 保持当前姿态；小臂和手腕惯性继续叠加。"),
   new AnimationParameter("forearmFollow","摆臂与手腕","小臂惯性","倍",.35,0,.5,.05,"越大，小臂越晚跟随大臂。0 表示不额外滞后。"),
   new AnimationParameter("wristFollow","摆臂与手腕","手腕惯性","倍",1,0,1.5,.1,"换向时的手部跟随量，0 表示关闭。"),
   new AnimationParameter("wristLimit","摆臂与手腕","手腕摆角上限","度",9,0,12,.5,"限制手腕相对小臂的最大摆角。"),
   new AnimationParameter("armDelay","摆臂与手腕","惯性延迟","倍",1,.25,1.5,.05,"按步相计算，数值越大，跟随越慢。"),
   new AnimationParameter("turnDuration","单脚转身","左右转身时长","秒",1.09,.55,1.6,.05,"普通单脚转身的时长；跑步衔接使用此值的 65%，下次转身生效。"),
   new AnimationParameter("frontTurnDuration","单脚转身","正侧面转身时长","秒",.87,.45,1.3,.05,"正侧面普通单脚转身的时长；跑步衔接使用此值的 65%，下次转身生效。"),
   new AnimationParameter("turnLift","单脚转身","另一只脚离地高度","% 身高",1.49,0,8,.5,"游离脚跟随身体旋转时的抬高量。"),
   new AnimationParameter("turnLean","单脚转身","身体侧倾","度",1.04,0,2,.1,"绕支撑脚轻轻侧倾。"),
   new AnimationParameter("turnChance","单脚转身","随机出现概率","%",50,0,100,5,"0 只用原有转身；100 只用单脚转身。预览可分别查看两种动作。"),
   new AnimationParameter("waveUpper","挥手问好","大臂外张幅度","度",5.74,0,15,.5,"以原画静止角度为内侧边界，只向外摆动。"),
   new AnimationParameter("waveElbow","挥手问好","小臂抬起角度","度",152.07,110,165,1,"相对自然垂臂向上折起的角度；不影响跑步屈臂。"),
   new AnimationParameter("waveForearm","挥手问好","小臂跟随摆幅","度",5.3,0,6,.5,"小臂随手部轻摆的幅度。"),
   new AnimationParameter("waveWristAngle","挥手问好","手部基准角度","度",0,-20,20,1,"调整手掌在挥动中心位置的倾角。"),
   new AnimationParameter("waveAmplitude","挥手问好","挥手摆幅","度",7.87,0,18,.5,"手腕向两侧摆动的幅度；0 表示保持手掌角度。"),
   new AnimationParameter("leapUpForward","轻跃","向上跳横向距离","% 身高",40.13,28,75,1,"沿当前朝向前移；靠近屏幕边缘自动缩短。下跳保持原距离。")
  };
  static Dictionary<string,double> values=Defaults();
  static bool fixedForApp;
  public static string LoadNotice="";
  public static string SettingsPath {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"IsekaijouchoPet","animation-parameters.json");}}
  static Dictionary<string,double> Defaults(){var result=new Dictionary<string,double>();foreach(var s in Specs)result[s.Key]=s.Default;return result;}
  public static void UseFixedValues(){values=Defaults();fixedForApp=true;LoadNotice="";}
  static void RequireInternalEditor(){if(fixedForApp)throw new InvalidOperationException("当前版本的动画参数已固定。");}
  public static double Get(string key){return values[key];}
  public static Dictionary<string,double> Snapshot(){return new Dictionary<string,double>(values);}
  public static void Reset(){values=Defaults();}
  public static bool Matches(IDictionary<string,double> other){foreach(var s in Specs)if(!other.ContainsKey(s.Key)||values[s.Key]!=other[s.Key])return false;return true;}
  public static void Set(string key,double value) {
   RequireInternalEditor();
   foreach(var s in Specs)if(s.Key==key){if(double.IsNaN(value)||double.IsInfinity(value)||value<s.Min||value>s.Max)throw new ArgumentOutOfRangeException(key,"参数超出允许范围。");values[key]=value;return;}
   throw new ArgumentException("未知动画参数："+key);
  }
  public static void Apply(IDictionary<string,double> data) {
   RequireInternalEditor();
   if(data==null)throw new InvalidDataException("参数内容为空。");
   var next=Defaults();
   foreach(var s in Specs)if(data.ContainsKey(s.Key)) {
    double v=data[s.Key];if(double.IsNaN(v)||double.IsInfinity(v)||v<s.Min||v>s.Max)throw new InvalidDataException(s.Label+" 超出允许范围。");next[s.Key]=v;
   }
   values=next; // Validate everything before changing any live setting.
  }
  public static void Load(string path){RequireInternalEditor();Apply(new JavaScriptSerializer().Deserialize<Dictionary<string,double>>(File.ReadAllText(path)));}
  public static void LoadSaved(){if(fixedForApp || !File.Exists(SettingsPath))return;try{Load(SettingsPath);}catch(Exception ex){LoadNotice="上次参数未能读取，已使用默认值："+ex.Message;}}
  public static void Save(string path) {
   RequireInternalEditor();
   path=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(path));
   string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
   try{File.WriteAllText(temp,new JavaScriptSerializer().Serialize(values));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
   finally{if(File.Exists(temp))File.Delete(temp);}
  }
 }
}

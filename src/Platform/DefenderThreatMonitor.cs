using System;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Xml;
namespace JouchoPet {
 sealed class DefenderThreat {
  internal readonly string Key;internal readonly int Severity;internal readonly DateTime TimeUtc;
  internal int Facing {get{return Severity<=2?-1:1;}}
  internal DefenderThreat(string key,int severity,DateTime time){Key=key;Severity=severity;TimeUtc=time;}
 }
 interface IDefenderThreatSource:IDisposable {
  event Action<DefenderThreat> Detected;
  string Status {get;}
  void Start();
 }
 // Read-only subscription. Never run scans, open threat paths, or change Defender settings.
 sealed class DefenderThreatMonitor:IDefenderThreatSource {
  internal const string Channel="Microsoft-Windows-Windows Defender/Operational";
  internal const string Provider="Microsoft-Windows-Windows Defender";
  internal const string Query="*[System[Provider[@Name='Microsoft-Windows-Windows Defender'] and (EventID=1116 or EventID=1006)]]";
  EventLogWatcher watcher;volatile bool disposed;volatile string status="尚未连接";
  public event Action<DefenderThreat> Detected;
  public string Status {get{return status;}}
  public void Start(){
   if(disposed||watcher!=null)return;
   try {
    // false is essential: launching the pet must not replay protection history.
    watcher=new EventLogWatcher(new EventLogQuery(Channel,PathType.LogName,Query),null,false);
    watcher.EventRecordWritten+=Written;watcher.Enabled=true;status="已连接 · 等待新威胁";
   }catch(Exception ex){status=Failure(ex);if(watcher!=null){watcher.EventRecordWritten-=Written;watcher.Dispose();watcher=null;}}
  }
  static string Failure(Exception ex){
   return ex is UnauthorizedAccessException||(ex.HResult&65535)==5?"未连接 · 无权读取 Defender 日志":"未连接 · Defender 日志不可用，请重连";
  }
  void Written(object sender,EventRecordWrittenEventArgs args){
   using(var record=args.EventRecord){
    if(disposed)return;
    if(args.EventException!=null){status=Failure(args.EventException);return;}
    if(record==null)return;
    try {var threat=Parse(record.ToXml());var callback=Detected;if(threat!=null&&!disposed&&callback!=null)callback(threat);}
    catch(Exception){if(!disposed)status="已连接 · 跳过无法读取的威胁事件";}
   }
  }
  internal static DefenderThreat Parse(string xml){
   if(String.IsNullOrEmpty(xml)||xml.Length>262144)return null;
   try {
    var doc=new XmlDocument{XmlResolver=null};
    using(var reader=XmlReader.Create(new StringReader(xml),new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))doc.Load(reader);
    var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("e","http://schemas.microsoft.com/win/2004/08/events/event");
    Func<string,string> value=path=>{var n=doc.SelectSingleNode(path,ns);return n==null?null:n.InnerText;};
    var provider=doc.SelectSingleNode("/e:Event/e:System/e:Provider/@Name",ns);
    if(provider==null||provider.Value!=Provider||value("/e:Event/e:System/e:Channel")!=Channel)return null;
    int id,severity;
    if(!Int32.TryParse(value("/e:Event/e:System/e:EventID"),out id)||(id!=1116&&id!=1006))return null;
    if(!Int32.TryParse(value("/e:Event/e:EventData/e:Data[@Name='Severity ID']"),NumberStyles.Integer,CultureInfo.InvariantCulture,out severity)||severity<1||severity>4)return null;
    DateTime time;var stamp=value("/e:Event/e:System/e:TimeCreated/@SystemTime");
    if(!DateTime.TryParse(stamp,CultureInfo.InvariantCulture,DateTimeStyles.AdjustToUniversal|DateTimeStyles.AssumeUniversal,out time))return null;
    string key=value("/e:Event/e:EventData/e:Data[@Name='Detection ID']");Guid detection;
    if(Guid.TryParse(key,out detection)&&detection!=Guid.Empty)key="d:"+detection.ToString("D");
    else {long record;if(!Int64.TryParse(value("/e:Event/e:System/e:EventRecordID"),out record)||record<=0)return null;key="r:"+record.ToString(CultureInfo.InvariantCulture)+":"+time.Ticks;}
    return new DefenderThreat(key,severity,time);
   }catch(XmlException){return null;}
  }
  public void Dispose(){disposed=true;var old=watcher;watcher=null;Detected=null;if(old!=null){old.EventRecordWritten-=Written;old.Dispose();}status="已关闭";}
 }
}

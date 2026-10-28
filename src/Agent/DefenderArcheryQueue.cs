using System;
using System.Collections.Generic;
namespace JouchoPet {
 // Event callbacks only enqueue; the UI thread claims shots when existing actions are idle.
 sealed class DefenderArcheryQueue {
  readonly object sync=new object();readonly List<DefenderThreat> pending=new List<DefenderThreat>();
  readonly Dictionary<string,DateTime> seen=new Dictionary<string,DateTime>();
  DateTime nextShot=DateTime.MinValue;
  internal int Count {get{lock(sync)return pending.Count;}}
  internal bool Add(DefenderThreat threat,DateTime now){lock(sync){
   if(threat==null||threat.Severity<1||threat.Severity>4||String.IsNullOrEmpty(threat.Key)||threat.Key.Length>160||now-threat.TimeUtc>TimeSpan.FromMinutes(5)||threat.TimeUtc-now>TimeSpan.FromMinutes(1))return false;
   var expired=new List<string>();foreach(var item in seen)if(now-item.Value>TimeSpan.FromMinutes(10))expired.Add(item.Key);foreach(var key in expired)seen.Remove(key);
   if(seen.ContainsKey(threat.Key))return false;
   if(seen.Count>=512){string oldest=null;DateTime first=DateTime.MaxValue;foreach(var item in seen)if(item.Value<first){first=item.Value;oldest=item.Key;}if(oldest!=null)seen.Remove(oldest);}
   seen[threat.Key]=now;
   if(pending.Count>=32){int small=pending.FindIndex(t=>t.Facing<0);if(threat.Facing<0&&small<0)return false;pending.RemoveAt(small<0?0:small);}
   pending.Add(threat);return true;
  }}
  internal DefenderThreat Take(DateTime now,bool ready){lock(sync){
   pending.RemoveAll(t=>now-t.TimeUtc>TimeSpan.FromMinutes(5));
   if(!ready||now<nextShot||pending.Count==0)return null;
   int index=pending.FindIndex(t=>t.Facing>0);if(index<0)index=0;
   var threat=pending[index];pending.RemoveAt(index);nextShot=now.AddSeconds(8);return threat;
  }}
  internal void Clear(){lock(sync){pending.Clear();nextShot=DateTime.MinValue;}}
 }
}

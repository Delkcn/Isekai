using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
using JouchoPet;

// This test never starts Pet or writes the user's settings directory.
class FixedParametersCheck {
 static int count;
 static void Check(bool passed,string message){if(!passed)throw new Exception(message);count++;Console.WriteLine("PASS "+message);}
 static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static void Reject(Action change,string message){bool blocked=false;try{change();}catch(InvalidOperationException){blocked=true;}Check(blocked,message);}
 static void Main(){try {
  string snapshot=Path.Combine("qa","v0643","baseline","animation-parameters.json");
  var approved=new JavaScriptSerializer().Deserialize<Dictionary<string,double>>(File.ReadAllText(snapshot));
  string userPath=AnimationParameters.SettingsPath;
  byte[] before=File.Exists(userPath)?File.ReadAllBytes(userPath):null;
  AnimationParameters.Reset();
  Check(approved.Count==24 && AnimationParameters.Snapshot().Count==24 && AnimationParameters.Matches(approved),"all 24 baked values exactly equal the user's approved snapshot");
  AnimationParameters.Set("runSpeed",150);
  AnimationParameters.UseFixedValues();
  Check(AnimationParameters.Matches(approved),"application startup restores approved values without importing a file");
  Reject(delegate{AnimationParameters.Set("runSpeed",160);},"application lock rejects live parameter edits");
  Reject(delegate{AnimationParameters.Apply(new Dictionary<string,double>{{"runSpeed",160}});},"application lock rejects preset replacement");
  string nonexistent=Path.Combine("qa","v0643","must-not-create-settings.json");
  Reject(delegate{AnimationParameters.Load(nonexistent);},"application lock rejects import before opening a settings file");
  Reject(delegate{AnimationParameters.Save(nonexistent);},"application lock rejects export before creating a settings file");
  Check(!File.Exists(nonexistent),"blocked editor calls created no file");
  AnimationParameters.LoadSaved();
  AnimationParameters.Reset();
  Check(AnimationParameters.Matches(approved),"legacy load/reset cannot change fixed application values");
  var detached=AnimationParameters.Snapshot();detached["runSpeed"]=170;
  Check(AnimationParameters.Get("runSpeed")==132,"returned snapshots cannot mutate fixed application values");
  Check(typeof(Pet).GetField("animationDebug",BindingFlags.Instance|BindingFlags.NonPublic)==null && typeof(Pet).GetMethod("OpenAnimationDebug",BindingFlags.Instance|BindingFlags.NonPublic)==null,"shipped Pet has no debug window instance or opening action");
  Check(before==null ? !File.Exists(userPath) : File.Exists(userPath)&&Same(before,File.ReadAllBytes(userPath)),"user settings file remains byte-identical");
  Console.WriteLine("All "+count+" fixed-parameter checks passed.");
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}

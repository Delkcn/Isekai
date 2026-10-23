using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;
namespace JouchoPet
{
    static class Json
    {
        public static string Encode(object o)
        {
            return new JavaScriptSerializer
            {
                MaxJsonLength=4000000
            }
            .Serialize(o);
        }
        public static Dictionary<string,object> Object(string s)
        {
            return new JavaScriptSerializer
            {
                MaxJsonLength=4000000
            }
            .Deserialize<Dictionary<string,object>>(s);
        }
        public static string Text(Dictionary<string,object> o,string key,string fallback="")
        {
            object v;
            return o!=null && o.TryGetValue(key,out v) && v!=null?Convert.ToString(v):fallback;
        }
        public static object[] Array(Dictionary<string,object> o,string key)
        {
            object v;
            if(!o.TryGetValue(key,out v)||v==null)return new object[0];
            ArrayList a=v as ArrayList;
            return a!=null?a.ToArray():(v as object[] ?? new object[0]);
        }
    }
}

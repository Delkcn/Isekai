using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace JouchoPet
{
    sealed class ApiSettings
    {
        public string Endpoint="https://api.openai.com/v1";
        public string Model="";
        public string Key="";
        public bool Remember;
        public static readonly string AppDir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"IsekaijouchoPet");
        public static ApiSettings Load()
        {
            ApiSettings s=new ApiSettings();
            try
            {
                var o=Json.Object(File.ReadAllText(Path.Combine(AppDir,"api.json")));
                s.Endpoint=Json.Text(o,"endpoint",s.Endpoint);
                s.Model=Json.Text(o,"model");
                string encrypted=Json.Text(o,"protected_key");
                if(encrypted.Length>0)
                {
                    s.Key=Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(encrypted),null,DataProtectionScope.CurrentUser));
                    s.Remember=true;
                }
            }
            catch(Exception)
            {
                /* A missing/unreadable local preference never blocks the pet. */
            }
            return s;
        }
        public void Save()
        {
            Directory.CreateDirectory(AppDir);
            string encrypted=Remember && Key.Length>0?Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(Key),null,DataProtectionScope.CurrentUser)):"";
            File.WriteAllText(Path.Combine(AppDir,"api.json"),Json.Encode(new
            {
                endpoint=Endpoint,model=Model,protected_key=encrypted
            }),new UTF8Encoding(false));
        }
    }
}

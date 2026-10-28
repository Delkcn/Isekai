using System;
using System.IO;
namespace JouchoPet
{
    sealed partial class Pet
    {
        void LoadSettings()
        {
            try
            {
                string[] s=File.ReadAllLines(settings);
                int n;
                if(s.Length>0 && int.TryParse(s[0],out n) && (n==220 || n==300 || n==400)) petHeight=n;
                if(s.Length>1) wandering=s[1]=="True";
                if(s.Length>2) TopMost=s[2]=="True";
                if(s.Length>3)bubblesEnabled=s[3]!="False";
                if(s.Length>4)defenderEnabled=s[4]!="False";
            }
            catch(IOException)
            {
            }
            catch(UnauthorizedAccessException)
            {
            }
        }
        void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settings));
                File.WriteAllLines(settings,new string[]
                {
                    petHeight.ToString(),wandering.ToString(),TopMost.ToString(),bubblesEnabled.ToString(),defenderEnabled.ToString()
                });
            }
            catch(IOException)
            {
            }
            catch(UnauthorizedAccessException)
            {
            }
        }
    }
}

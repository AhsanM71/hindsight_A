using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EyeCanDo.Editor
{
    // Disk backup survives an interrupted test or an Editor restart. Never called by gameplay.
    [InitializeOnLoad]
    public static class PolishProfileProtection
    {
        const string FilePath="Validation/Polish2/profile-recovery.json";
        [Serializable] public class Backup {public bool existed;public string value;public string phase;public int process;}
        static PolishProfileProtection()
        {
            EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode && File.Exists(FilePath))Restore();};
            EditorApplication.delayCall+=()=>{if(!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(FilePath) && JsonUtility.FromJson<Backup>(File.ReadAllText(FilePath)).phase!="restart")Restore();};
        }
        public static void Protect(string phase)
        {
            if(File.Exists(FilePath))throw new InvalidOperationException("A profile recovery record already exists. Restore it before starting another test.");
            Directory.CreateDirectory("Validation/Polish2");File.WriteAllText(FilePath,JsonUtility.ToJson(new Backup {existed=PlayerPrefs.HasKey(PlayerPrefsProfileStore.UsernameKey),value=PlayerPrefs.GetString(PlayerPrefsProfileStore.UsernameKey),phase=phase,process=System.Diagnostics.Process.GetCurrentProcess().Id},true));
        }
        public static void Navigation(){Protect("navigation");MvpValidation.RunProfileChecks();MvpValidation.RunNavigationChecks();}
        public static void PrepareRestart(){Protect("restart");new PlayerPrefsProfileStore().Write("Alex");}
        [MenuItem("EyeCanDo/Journey Map/Restore Profile After Interrupted Polish Test")]
        public static void Restore()
        {
            if(!File.Exists(FilePath))return;var b=JsonUtility.FromJson<Backup>(File.ReadAllText(FilePath));
            if(b.existed)PlayerPrefs.SetString(PlayerPrefsProfileStore.UsernameKey,b.value);else PlayerPrefs.DeleteKey(PlayerPrefsProfileStore.UsernameKey);PlayerPrefs.Save();
            File.Copy(FilePath,"Validation/Polish2/profile-restored.json",true);File.Delete(FilePath);
        }
    }
}

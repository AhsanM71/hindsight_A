using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EyeCanDo.Editor
{
    [InitializeOnLoad] public static class CharacterShopProtection
    {
        const string Recovery="Validation/CharacterShop/profile-recovery.json";
        [Serializable] public class Entry {public string key,value;public bool existed;}
        [Serializable] public class Record {public string phase;public int process;public Entry[] entries;}
        static CharacterShopProtection()
        {
            EditorApplication.playModeStateChanged+=s=>
            {
                if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("CharacterShopRun",false))
                {SessionState.SetBool("CharacterShopRun",false);var checks=new GameObject("Character Shop interaction checks").AddComponent<AdventureInteractionChecks>();checks.outputFolder="Validation/CharacterShop/Interactions/";}
                if(s==PlayModeStateChange.EnteredEditMode&&File.Exists(Recovery)&&Read().phase!="restart")Restore();
            };
            EditorApplication.delayCall+=()=>{if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Recovery)&&Read().phase!="restart")Restore();};
        }
        static Record Read()=>JsonUtility.FromJson<Record>(File.ReadAllText(Recovery));
        public static void Protect(string phase)
        {
            if(File.Exists(Recovery))throw new InvalidOperationException("Existing recovery record must be restored first.");
            var keys=new[]{PlayerPrefsProfileStore.UsernameKey}.Concat(PlayerPrefsCustomizationStore.Keys);
            var r=new Record {phase=phase,process=System.Diagnostics.Process.GetCurrentProcess().Id,entries=keys.Select(k=>new Entry {key=k,existed=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetString(k)}).ToArray()};
            File.WriteAllText(Recovery,JsonUtility.ToJson(r,true));
        }
        public static void RunInteractions()
        {
            Protect("interactions");foreach(var e in Read().entries)PlayerPrefs.DeleteKey(e.key);PlayerPrefs.Save();
            Directory.CreateDirectory("Validation/CharacterShop/Interactions");EditorSceneManager.OpenScene("Assets/EyeCanDo/Scenes/Start.unity");SessionState.SetBool("CharacterShopRun",true);EditorApplication.isPlaying=true;
        }
        public static void PrepareRestart()
        {
            Protect("restart");new PlayerPrefsProfileStore().Write("Restart friend");foreach(var key in PlayerPrefsCustomizationStore.Keys)PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
            var catalog=AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(CharacterArt.Root+"/Catalog/Cosmetics.asset");var service=new CustomizationService(catalog,new PlayerPrefsCustomizationStore(),new UnityCustomizationCodec());
            if(service.TryPurchaseAndEquip("hat.cap")!=CustomizationResult.Success)throw new Exception("Restart fixture purchase failed");
            File.WriteAllText("Validation/CharacterShop/restart-expected.json",JsonUtility.ToJson(service.Snapshot,true));
        }
        public static void VerifyRestart()
        {
            var before=Read();if(before.process==System.Diagnostics.Process.GetCurrentProcess().Id)throw new Exception("Editor process has not restarted");
            var c=AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(CharacterArt.Root+"/Catalog/Cosmetics.asset");var service=new CustomizationService(c,new PlayerPrefsCustomizationStore(),new UnityCustomizationCodec());
            bool ok=service.Snapshot.coins==160&&service.Snapshot.Get(CosmeticSlot.Hat)=="hat.cap"&&PlayerPrefs.GetString(PlayerPrefsProfileStore.UsernameKey)=="Restart friend";
            File.WriteAllText("Validation/CharacterShop/restart.json", "{\"passed\":"+ok.ToString().ToLowerInvariant()+",\"beforePid\":"+before.process+",\"afterPid\":"+System.Diagnostics.Process.GetCurrentProcess().Id+"}");
            Restore();if(!ok)throw new Exception("Restart persistence failed");
        }
        [MenuItem("EyeCanDo/Character & Shop/Restore protected profile")]
        public static void Restore()
        {
            if(!File.Exists(Recovery))return;var r=Read();
            foreach(var e in r.entries){if(e.existed)PlayerPrefs.SetString(e.key,e.value);else PlayerPrefs.DeleteKey(e.key);}PlayerPrefs.Save();
            bool exact=r.entries.All(e=>PlayerPrefs.HasKey(e.key)==e.existed&&PlayerPrefs.GetString(e.key)==e.value);
            File.WriteAllText("Validation/CharacterShop/profile-restored.json",JsonUtility.ToJson(new RestoreReport{exact=exact,entries=r.entries},true));
            if(!exact)throw new Exception("Profile restoration failed");File.Delete(Recovery);SafeArea.TestOverride=null;
        }
        [Serializable] class RestoreReport {public bool exact;public Entry[] entries;}
    }
}

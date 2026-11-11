using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EyeCanDo.Editor
{
    [InitializeOnLoad]
    public static class MvpValidation
    {
        const string RunKey = "EyeCanDo.Validation.Active";
        static MvpValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(RunKey, false))
                    new GameObject("EyeCanDo navigation checks").AddComponent<NavigationSmokeTest>();
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(RunKey, false)) RestoreProfile();
            };
        }

        sealed class MemoryStore : IProfileStore
        {
            public string value;
            public string Read() => value;
            public void Write(string username) => value = username;
        }

        [Serializable] public class Report { public string editor; public string status; public string[] passed; }

        [MenuItem("EyeCanDo/Validation/1. Profile and Configuration Checks")]
        public static void RunProfileChecks()
        {
            var passed = new List<string>();
            void Check(bool condition, string description)
            { if (!condition) throw new Exception(description); passed.Add(description); }
            foreach (var value in new[] { null, "", "   ", "\t\n" })
                Check(!LocalUserProfile.TryValidate(value, out _, out _), "Reject blank: " + (value == null ? "null" : value.Length.ToString()));
            Check(LocalUserProfile.TryValidate("  Alex  ", out var trimmed, out _) && trimmed == "Alex", "Trim leading and trailing whitespace");
            Check(LocalUserProfile.TryValidate(new string('a',20),out _,out _),"Accept exactly 20 characters");
            Check(!LocalUserProfile.TryValidate(new string('a',21),out _,out _),"Reject 21 characters");
            Check(!LocalUserProfile.TryValidate("Alex\nSam",out _,out _),"Reject multiline input");
            var store = new MemoryStore { value = "   " }; var profile = new LocalUserProfile(store);
            Check(!profile.HasUsername,"Blank saved profile requires setup");
            int events=0;profile.Changed += _ => events++;
            Check(profile.TrySaveUsername("  Alex  ",out _) && store.value == "Alex" && events == 1,"Save trimmed name and emit profile change");
            Check(!profile.TrySaveUsername(" ",out _) && store.value == "Alex" && events == 1,"Invalid save preserves previous name");
            Check(new LocalUserProfile(store).Username == "Alex","Fresh profile service reloads saved name");
            var catalog=AssetDatabase.LoadAssetAtPath<JourneyCatalog>(MvpArt.Root+"/Data/JourneyCatalog.asset");
            Check(catalog!=null && catalog.journeys.Length==3,"MVP catalog contains three journeys");
            var ids=new HashSet<string>();
            foreach(var journey in catalog.journeys)
            {
                Check(ids.Add(journey.id),"Unique journey ID: "+journey.id);
                Check(journey.icon!=null && journey.destinationPrefab!=null,"Destination art references: "+journey.id);
                Check(journey.sublevels.Length==3,"Three sublevels: "+journey.id);
                foreach(var level in journey.sublevels)
                    Check(ids.Add(level.id) && !level.isPlayable && level.icon!=null,"Configured Coming Soon: "+level.id);
            }
            Check(EditorBuildSettings.scenes.Count(x=>x.enabled)==3,"Three registered scenes");
            Check(PlayerSettings.allowedAutorotateToLandscapeLeft && PlayerSettings.allowedAutorotateToLandscapeRight && !PlayerSettings.allowedAutorotateToPortrait && !PlayerSettings.allowedAutorotateToPortraitUpsideDown,"Landscape-only orientation");
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/profile-and-configuration.json",JsonUtility.ToJson(new Report { editor=Application.unityVersion,status="passed",passed=passed.ToArray() },true));
            Debug.Log($"EyeCanDo: {passed.Count} profile/configuration checks passed.");
        }

        [MenuItem("EyeCanDo/Validation/2. Run Navigation Checks in Play Mode")]
        public static void RunNavigationChecks()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            SessionState.SetBool("EyeCanDo.Validation.HadProfile",PlayerPrefs.HasKey(PlayerPrefsProfileStore.UsernameKey));
            SessionState.SetString("EyeCanDo.Validation.PreviousName",PlayerPrefs.GetString(PlayerPrefsProfileStore.UsernameKey,string.Empty));
            SessionState.SetBool(RunKey,true);
            PlayerPrefs.DeleteKey(PlayerPrefsProfileStore.UsernameKey);PlayerPrefs.Save();
            NavigationSmokeTest.SetGameViewSize(1920,1080);
            EditorSceneManager.OpenScene(MvpArt.Root+"/Scenes/Start.unity");
            EditorApplication.isPlaying=true;
        }

        public static void RestoreProfile()
        {
            if(SessionState.GetBool("EyeCanDo.Validation.HadProfile",false))
                PlayerPrefs.SetString(PlayerPrefsProfileStore.UsernameKey,SessionState.GetString("EyeCanDo.Validation.PreviousName",string.Empty));
            else PlayerPrefs.DeleteKey(PlayerPrefsProfileStore.UsernameKey);
            PlayerPrefs.Save();SessionState.SetBool(RunKey,false);
        }
    }
}

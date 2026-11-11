using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EyeCanDo.Editor
{
    [InitializeOnLoad]
    public static class AdventureValidation
    {
        const string Recovery="Validation/UIIteration3/profile-recovery.json";
        [Serializable] public class Backup {public bool existed;public string value;public bool runInBackground;public string editorScene;}
        static AdventureValidation()
        {
            EditorApplication.playModeStateChanged+=s=>
            {
                if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("UI3Run",false))
                {SessionState.SetBool("UI3Run",false);new GameObject("Adventure interaction checks").AddComponent<AdventureInteractionChecks>();}
                if(s==PlayModeStateChange.EnteredEditMode)Restore();
            };
            EditorApplication.delayCall+=()=>{if(!EditorApplication.isPlayingOrWillChangePlaymode)Restore();};
        }
        [MenuItem("EyeCanDo/UI/Validate Adventure UI (Iteration 3)")]
        public static void Run()
        {
            // The fixture now exercises purchases; protect and restore every customization key too.
            CharacterShopProtection.RunInteractions();
        }
        public static void Restore()
        {
            SafeArea.TestOverride=null;
            if(!File.Exists(Recovery))return;
            var b=JsonUtility.FromJson<Backup>(File.ReadAllText(Recovery));
            if(b.existed)PlayerPrefs.SetString(PlayerPrefsProfileStore.UsernameKey,b.value);else PlayerPrefs.DeleteKey(PlayerPrefsProfileStore.UsernameKey);PlayerPrefs.Save();
            bool exact=PlayerPrefs.HasKey(PlayerPrefsProfileStore.UsernameKey)==b.existed && PlayerPrefs.GetString(PlayerPrefsProfileStore.UsernameKey)==b.value;
            File.WriteAllText(AdventureAudit.Folder+"/profile-restored.json","{\"exactOriginalRestored\":"+exact.ToString().ToLowerInvariant()+",\"safeAreaOverrideCleared\":true}");
            if(!exact)throw new InvalidOperationException("Profile restoration failed.");File.Delete(Recovery);
        }
        public static void Assets()
        {
            var passed=new System.Collections.Generic.List<string>();
            void Check(bool value,string message){if(!value)throw new Exception(message);passed.Add(message);}
            foreach(string scene in new[]{"Start","JourneyMap","JourneySelection"})
            {
                EditorSceneManager.OpenScene("Assets/EyeCanDo/Scenes/"+scene+".unity");var roots=SceneManager.GetActiveScene().GetRootGameObjects();
                Check(roots.Sum(r=>r.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)))==0,scene+": no missing scripts");
                Check(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length==1,scene+": one EventSystem");
                Check(roots.SelectMany(r=>r.GetComponentsInChildren<TMPro.TMP_Text>(true)).All(t=>t.font!=null && t.font.name.StartsWith("Fredoka")),scene+": rounded fonts throughout");
                Check(UnityEngine.Object.FindObjectsByType<Camera>().Length==1,scene+": one runtime camera");
                foreach(var r in roots.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)))Check(r.sharedMaterials.All(m=>m!=null && m.shader!=null && m.shader.isSupported),scene+": material/shader "+r.name);
                if(scene=="JourneyMap")AdventureAudit.Compare();
            }
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{AdventureArt.Root}))
            {var root=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));Check(root.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"Prefab scripts: "+root.name);}
            foreach(var guid in AssetDatabase.FindAssets("t:JourneyPresentationTheme",new[]{AdventureArt.Root}))
            {var t=AssetDatabase.LoadAssetAtPath<JourneyPresentationTheme>(AssetDatabase.GUIDToAssetPath(guid));Check(t.courtyardPrefab!=null && t.platform!=null && t.phaseIllustrations.Length==3 && t.phaseIllustrations.All(s=>s!=null),"Theme references: "+t.journeyId);}
            File.WriteAllText(AdventureAudit.Folder+"/assets.json",JsonUtility.ToJson(new AssetReport {editor=Application.unityVersion,status="passed",checks=passed.ToArray()},true));
            EditorSceneManager.OpenScene("Assets/EyeCanDo/Scenes/Start.unity");
        }
        static string PresentationSignature()
        {
            var lines=new System.Collections.Generic.List<string>();
            string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.GetSiblingIndex()+":"+t.name;
            foreach(string scene in new[]{"Start","JourneyMap","JourneySelection"})
            {
                EditorSceneManager.OpenScene("Assets/EyeCanDo/Scenes/"+scene+".unity");
                foreach(var t in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))
                {
                    string line=scene+"/"+PathOf(t)+"|"+t.gameObject.activeSelf+"|"+t.localPosition.ToString("R")+"|"+t.localRotation.ToString("R")+"|"+t.localScale.ToString("R");
                    if(t is RectTransform r)line+="|rect:"+r.anchorMin.ToString("R")+r.anchorMax.ToString("R")+r.pivot.ToString("R")+r.sizeDelta.ToString("R")+r.anchoredPosition.ToString("R");
                    var label=t.GetComponent<TMPro.TMP_Text>();if(label!=null)line+="|text:"+label.text+"|font:"+AssetDatabase.GetAssetPath(label.font)+"|"+label.fontSize+"|"+label.fontSizeMin+"|"+label.fontSizeMax;
                    var image=t.GetComponent<UnityEngine.UI.Image>();if(image!=null)line+="|image:"+AssetDatabase.GetAssetPath(image.sprite)+"|"+image.color;
                    var button=t.GetComponent<UnityEngine.UI.Button>();if(button!=null)for(int i=0;i<button.onClick.GetPersistentEventCount();i++)line+="|callback:"+button.onClick.GetPersistentMethodName(i)+"@"+button.onClick.GetPersistentTarget(i)?.name;
                    var mesh=t.GetComponent<MeshFilter>();if(mesh!=null && label==null)line+="|mesh:"+AssetDatabase.GetAssetPath(mesh.sharedMesh);
                    lines.Add(line);
                }
            }
            lines.Sort(StringComparer.Ordinal);return string.Join("\n",lines);
        }
        public static void Idempotence()
        {
            var before=PresentationSignature();AdventureUIBuilder.Apply();var after=PresentationSignature();
            File.WriteAllText(AdventureAudit.Folder+"/idempotence-before.txt",before);File.WriteAllText(AdventureAudit.Folder+"/idempotence-after.txt",after);
            bool same=before==after;File.WriteAllText(AdventureAudit.Folder+"/idempotence.json","{\"sameHierarchyTransformsTypographySpritesAndCallbacks\":"+same.ToString().ToLowerInvariant()+"}");
            EditorSceneManager.OpenScene("Assets/EyeCanDo/Scenes/Start.unity");
            if(!same)throw new InvalidOperationException("Presentation changed on repeated Apply. Inspect signature files.");
        }
        [Serializable] class AssetReport {public string editor,status;public string[] checks;}
    }
}

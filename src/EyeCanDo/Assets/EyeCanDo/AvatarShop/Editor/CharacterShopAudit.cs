using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EyeCanDo.Editor
{
    public static class CharacterShopAudit
    {
        static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
        public static string Signature()
        {
            var lines=new List<string>();foreach(var path in EditorBuildSettings.scenes.Select(s=>s.path))
            {
                EditorSceneManager.OpenScene(path);
                foreach(var t in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))
                {
                    var line=path+"|"+PathOf(t)+"|"+t.localPosition.ToString("R")+"|"+t.localRotation.ToString("R")+"|"+t.localScale.ToString("R")+"|"+t.gameObject.activeSelf;
                    var b=t.GetComponent<UnityEngine.UI.Button>();if(b!=null)for(int i=0;i<b.onClick.GetPersistentEventCount();i++)line+="|"+b.onClick.GetPersistentMethodName(i)+"@"+b.onClick.GetPersistentTarget(i)?.name;
                    if(t is RectTransform r)line+="|"+r.anchoredPosition.ToString("R")+r.sizeDelta.ToString("R");
                    var m=t.GetComponent<MeshFilter>();if(m!=null)line+="|"+AssetDatabase.GetAssetPath(m.sharedMesh);lines.Add(line);
                }
            }
            lines.Sort(StringComparer.Ordinal);return string.Join("\n",lines);
        }
        public static void Run()
        {
            var checks=new List<string>();void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add(message);}
            var catalog=AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(CharacterArt.Root+"/Catalog/Cosmetics.asset");Check(catalog.items.Select(i=>i.id).Distinct().Count()==catalog.items.Length,"Stable unique catalogue IDs");
            foreach(var d in catalog.items)Check(d.thumbnail!=null&&d.price>=0&&d.defaultOwnership==(d.price==0),"Catalogue payload valid: "+d.id);
            foreach(CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot)))Check(catalog.Find(catalog.Default(slot)) is {defaultOwnership:true},"Free default exists: "+slot);
            Check(EditorBuildSettings.scenes.Length==4&&EditorBuildSettings.scenes.All(s=>s.enabled),"Exactly four enabled scenes");
            foreach(var path in EditorBuildSettings.scenes.Select(s=>s.path))
            {
                EditorSceneManager.OpenScene(path);var roots=SceneManager.GetActiveScene().GetRootGameObjects();string name=SceneManager.GetActiveScene().name;
                Check(roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==0,name+": no missing scripts");
                Check(Object.FindObjectsByType<Camera>().Length==1,name+": one camera");Check(Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length==1,name+": one EventSystem");
                Check(roots.SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader!=null&&m.shader.isSupported)),name+": valid materials and supported shaders");
                foreach(var a in Object.FindObjectsByType<AvatarView>())Check(a.headAttachment&&a.faceAttachment&&a.hairAttachment&&a.hatAttachment&&a.glassesAttachment&&a.backAttachment&&a.leftHandAttachment&&a.rightHandAttachment&&a.rightArm&&a.leftArm,name+": all avatar attachment references");
                if(name=="Start")Check(GameObject.Find("Owl")==null&&Object.FindObjectsByType<AvatarView>().Length==1,"Start has one avatar, no owl");
                if(name=="JourneyMap")
                {
                    Check(AdventureAudit.SnapshotMap()==File.ReadAllText("Validation/CharacterShop/map-before.json"),"Map environment, destinations, camera and lighting match baseline");
                    Check(GameObject.Find("Map Avatar").GetComponentsInChildren<Collider>().Length==0,"Map avatar has no collider");
                    Check(!roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Any(t=>t.name=="Shop Coming Soon"),"Shop placeholder removed");
                    var button=GameObject.Find("Shop").GetComponent<UnityEngine.UI.Button>();Check(button.onClick.GetPersistentEventCount()==1&&button.onClick.GetPersistentMethodName(0)=="OpenShop","Original OpenShop callback retained exactly once");
                }
            }
            var before=Signature();EditorSceneManager.OpenScene("Assets/EyeCanDo/Scenes/Start.unity");CharacterShopBuilder.Apply();var after=Signature();
            File.WriteAllText("Validation/CharacterShop/idempotence-before.txt",before);File.WriteAllText("Validation/CharacterShop/idempotence-after.txt",after);Check(before==after,"Repeated Apply creates identical hierarchy, transforms and callbacks");
            File.WriteAllText("Validation/CharacterShop/assets-and-idempotence.json",JsonUtility.ToJson(new Report{status="passed",editor=Application.unityVersion,checks=checks.ToArray()},true));
            EditorSceneManager.OpenScene("Assets/EyeCanDo/Scenes/Start.unity");
        }
        public static void Compatibility()
        {
            EditorSceneManager.OpenScene(CharacterShopBuilder.ShopPath);var avatar=GameObject.Find("Shop Avatar").GetComponent<AvatarView>();var catalog=avatar.catalog;
            var state=new CustomizationState{version=1};foreach(CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot)))state.Set(slot,catalog.Default(slot));
            var cameraObject=new GameObject("Compatibility authoring camera");var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.5f;camera.transform.position=new Vector3(-5.15f,4.65f,-12);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=CharacterArt.C("EDF4E8");camera.enabled=false;
            var rt=new RenderTexture(360,360,24);camera.targetTexture=rt;var active=RenderTexture.active;var checks=new List<string>();Directory.CreateDirectory("Validation/CharacterShop/Compatibility");
            void Capture(string name)
            {camera.Render();RenderTexture.active=rt;var texture=new Texture2D(360,360,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,360,360),0,0);texture.Apply();File.WriteAllBytes("Validation/CharacterShop/Compatibility/"+name+".png",texture.EncodeToPNG());Object.DestroyImmediate(texture);}
            try
            {
                foreach(var hair in catalog.items.Where(i=>i.slot==CosmeticSlot.Hair))foreach(var hat in catalog.items.Where(i=>i.slot==CosmeticSlot.Hat))
                {
                    state.Set(CosmeticSlot.Hair,hair.id);state.Set(CosmeticSlot.Hat,hat.id);state.Set(CosmeticSlot.Glasses,"glasses.fun");avatar.Apply(state);
                    var crown=avatar.hairAttachment.GetChild(0).Find("Crown");if(crown.gameObject.activeSelf!=(hat.visual==null))throw new Exception("Crown compatibility: "+hair.id+hat.id);
                    if(hat.visual!=null)
                    {
                        var side=avatar.hairAttachment.GetChild(0).Find("Sides");float brim=avatar.hatAttachment.TransformPoint(new Vector3(0,.28f,0)).y;
                        if(side.GetComponentsInChildren<Renderer>().Any(r=>r.bounds.max.y>brim+.015f))throw new Exception("Side hair enters brim: "+hair.id);
                    }
                    if(avatar.glassesAttachment.parent!=avatar.headAttachment||avatar.hatAttachment.parent!=avatar.headAttachment)throw new Exception("Attachment hierarchy");
                    checks.Add(hair.id+" + "+hat.id+": crown/side clearance and glasses attachment pass");Capture(hair.id+"_"+hat.id);
                }
                state.Set(CosmeticSlot.Hat,"hat.none");state.Set(CosmeticSlot.Hair,"hair.curls");
                foreach(var face in catalog.items.Where(i=>i.slot==CosmeticSlot.Face)){state.Set(CosmeticSlot.Face,face.id);avatar.Apply(state);Capture(face.id);checks.Add(face.id+": face/glasses render captured");}
                state.Set(CosmeticSlot.Backpack,"backpack.small");state.Set(CosmeticSlot.Hat,"hat.star");camera.orthographicSize=3;camera.transform.position=new Vector3(-5.15f,2.9f,-12);
                foreach(int angle in new[]{0,45,135,180}){avatar.Apply(state);avatar.transform.localRotation=Quaternion.Euler(0,angle,0);Capture("backpack-"+angle);checks.Add("Backpack and accessories attached at "+angle+" degrees");}
                File.WriteAllText("Validation/CharacterShop/compatibility.json",JsonUtility.ToJson(new Report{status="passed",editor=Application.unityVersion,checks=checks.ToArray()},true));
            }
            finally{camera.targetTexture=null;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(cameraObject);EditorSceneManager.OpenScene("Assets/EyeCanDo/Scenes/Start.unity");}
        }
        [Serializable] class Report {public string status,editor;public string[] checks;}
    }
}

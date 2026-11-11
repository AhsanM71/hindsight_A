using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace EyeCanDo.Editor
{
    public static class GardenValidation
    {
        [Serializable] public class Report {public string status; public string editor; public List<string> passed;public int renderers, triangles, materials;}
        [MenuItem("EyeCanDo/Journey Map/Validate Garden Assets")]
        public static void Run()
        {
            var report=new Report {editor=Application.unityVersion,status="passed",passed=new List<string>()};
            void Check(bool condition,string message){if(!condition)throw new Exception(message);report.passed.Add(message);}
            var layout=Object.FindAnyObjectByType<JourneyMapLayout>();
            Check(layout!=null,"Journey Map is loaded");
            var roots=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            Check(roots.Sum(g=>g.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)))==0,"No missing scripts in the saved map");
            Check(Object.FindObjectsByType<EventSystem>().Length==1,"Exactly one EventSystem");
            Check(layout.mapCamera.GetComponent<PhysicsRaycaster>()!=null,"Equipment has a camera PhysicsRaycaster");
            var destinations=layout.destinations.GetComponentsInChildren<JourneyDestination>();
            Check(destinations.Length==layout.catalog.journeys.Length,"Destination count matches catalog");
            foreach(var destination in destinations)
            {
                Check(layout.catalog.Find(destination.definition.id)==destination.definition,"Stable catalog identity: "+destination.definition.id);
                Check(destination.equipment!=null && destination.GetComponent<Collider>()!=null,"Equipment and touch collider: "+destination.definition.id);
                var button=destination.GetComponentInChildren<UnityEngine.UI.Button>();
                Check(button.onClick.GetPersistentTarget(0)==destination && button.onClick.GetPersistentMethodName(0)=="Open","Badge retains JourneyDestination.Open: "+destination.definition.id);
            }
            var renderers=roots.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).ToArray();
            var mats=renderers.SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
            Check(mats.All(m=>m!=null && m.shader!=null && m.shader.isSupported && m.shader.name!="Hidden/InternalErrorShader"),"Every renderer has supported, nonmissing materials and shaders");
            Check(AssetDatabase.FindAssets("t:GameObject",new[]{StorybookGardenBuilder.Root+"/ThirdParty/Quaternius/Models"}).Length==68,"All 68 Standard-edition Unity FBX imports are present");
            Check(File.Exists(StorybookGardenBuilder.Root+"/ThirdParty/Quaternius/License_Standard.txt"),"Original CC0 license retained");
            var data=layout.mapCamera.GetComponent<UniversalAdditionalCameraData>();var volume=Object.FindAnyObjectByType<Volume>();
            Check(UniversalRenderPipeline.asset.supportsHDR && data.renderPostProcessing && (data.volumeLayerMask.value & 1)!=0,"URP HDR, post-processing, and volume mask configured");
            Check(AssetDatabase.GetAssetPath(volume.sharedProfile).StartsWith(StorybookGardenBuilder.Root),"Lighting profile belongs only to the garden");
            var original=layout.catalog;
            var expanded=Object.Instantiate(original);var future=ScriptableObject.CreateInstance<JourneyDefinition>();future.id="future-garden-check";
            expanded.journeys=original.journeys.Concat(new[]{future}).ToArray();
            try
            {
                layout.catalog=expanded;var p=layout.PositionFor(expanded.journeys.Length-1);
                Check(layout.authoredPlacements.All(a=>Vector3.Distance(a.position,p)>5),"Future catalog entry receives non-overlapping automatic placement");
                for(int i=0;i<original.journeys.Length;i++)Check(layout.PositionFor(i)==layout.authoredPlacements.Single(a=>a.journeyId==original.journeys[i].id).position,"Authored placement stable when catalog grows: "+original.journeys[i].id);
            }
            finally{layout.catalog=original;Object.DestroyImmediate(expanded);Object.DestroyImmediate(future);}
            report.renderers=renderers.Length;report.materials=mats.Length;
            report.triangles=roots.SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).Where(f=>f.sharedMesh!=null).Sum(f=>f.sharedMesh.triangles.Length/3);
            Directory.CreateDirectory("Validation/StorybookGarden");File.WriteAllText("Validation/StorybookGarden/assets.json",JsonUtility.ToJson(report,true));
            Debug.Log("Garden validation: "+report.passed.Count+" checks passed.");
        }
    }
}

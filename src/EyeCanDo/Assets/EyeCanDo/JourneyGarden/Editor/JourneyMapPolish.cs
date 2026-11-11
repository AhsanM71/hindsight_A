using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace EyeCanDo.Editor
{
    public static class JourneyMapPolish
    {
        const string Root="Assets/EyeCanDo/JourneyGarden";
        const string Art=Root+"/Polish2";
        static Color C(string value){ColorUtility.TryParseHtmlString("#"+value,out var c);return c;}
        [MenuItem("EyeCanDo/Journey Map/Polish Journey Map (Iteration 2)")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save scene changes first.");
            EditorSceneManager.OpenScene("Assets/EyeCanDo/Scenes/JourneyMap.unity");
            Directory.CreateDirectory(Art);AssetDatabase.Refresh();
            var layout=Object.FindAnyObjectByType<JourneyMapLayout>();
            foreach(var entry in layout.authoredPlacements)
            {
                string path=AssetDatabase.GetAssetPath(entry.presentationPrefab);var contents=PrefabUtility.LoadPrefabContents(path);
                try{PolishDestination(contents.GetComponent<JourneyDestination>());PrefabUtility.SaveAsPrefabAsset(contents,path);}
                finally{PrefabUtility.UnloadPrefabContents(contents);}
            }
            foreach(var d in layout.destinations.GetComponentsInChildren<JourneyDestination>())PolishDestination(d);
            layout.compositionBounds=new Bounds(new Vector3(0,2.4f,2.6f),new Vector3(25,5.8f,12));
            layout.worldViewport=new Rect(.035f,.10f,.93f,.75f);
            layout.mapCamera.transform.rotation=Quaternion.Euler(30,0,0);layout.FitCamera();
            layout.mapCamera.backgroundColor=C("5ABDEB");
            Backdrop(layout.mapCamera);
            Controls();
            // Preserve the existing environment. Only lower foreground planting directly under enlarged badges.
            var planting=GameObject.Find("Layered perimeter planting").transform;
            foreach(Transform plant in planting)
            {
                if(plant.name.StartsWith("Fern") && plant.localPosition.z < -2.8f && Mathf.Abs(plant.localPosition.x)>6 && Mathf.Abs(plant.localPosition.x)<12)
                    plant.localPosition=new Vector3(plant.localPosition.x,plant.localPosition.y,1.3f);
                if(plant.name=="Storybook blossom" && plant.localPosition.z < -3 && Mathf.Abs(plant.localPosition.x)>6.4f && Mathf.Abs(plant.localPosition.x)<10.8f)
                    plant.localScale=Vector3.one*.5f;
            }
            var sun=GameObject.Find("Warm sunlight").GetComponent<Light>();sun.intensity=1.35f;sun.color=C("FFF1DC");sun.shadowStrength=.55f;
            RenderSettings.ambientSkyColor=C("BDDDF2");RenderSettings.ambientEquatorColor=C("A2C6CB");RenderSettings.ambientGroundColor=C("688B63");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        static Material Material(string name,string color,bool sky=false)
        {
            string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find(sky?"EyeCanDo/GardenSky":"Universal Render Pipeline/Unlit"));m.name=name;AssetDatabase.CreateAsset(m,path);}
            if(!sky)m.SetColor("_BaseColor",C(color));else{m.SetColor("_Top",C("279CE7"));m.SetColor("_Bottom",C("D0F1EC"));}
            m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
        static Transform Child(Transform root,string name)
        {var t=root.Find(name);if(t!=null)return t;var g=new GameObject(name);g.transform.SetParent(root,false);return g.transform;}
        static void PolishDestination(JourneyDestination d)
        {
            var art=d.equipment.Find("Enlarged equipment art");
            if(art==null){var children=d.equipment.Cast<Transform>().ToArray();art=Child(d.equipment,"Enlarged equipment art");foreach(var t in children)t.SetParent(art,false);}
            art.localScale=Vector3.one*1.22f;
            var idle=Ensure<GardenMotion>(art.gameObject);idle.bob=.035f;idle.sway=.35f;idle.speed=.65f;
            var collider=d.GetComponent<BoxCollider>();collider.center=new Vector3(0,2.65f,0);collider.size=new Vector3(5.7f,5.2f,4.5f);
            var canvas=(RectTransform)d.transform.Find("Journey badge");canvas.sizeDelta=new Vector2(580,200);canvas.localPosition=new Vector3(0,1.1f,-3.2f);canvas.localRotation=Quaternion.Euler(30,0,0);
            var button=canvas.GetComponentInChildren<UnityEngine.UI.Button>();var rect=(RectTransform)button.transform;rect.sizeDelta=new Vector2(580,200);
            var image=button.GetComponent<UnityEngine.UI.Image>();image.color=Color.white;
            var outline=image.GetComponent<UnityEngine.UI.Outline>();outline.effectDistance=new Vector2(4,-4);
            SetRect(button.transform.Find("Pictorial medallion"),new Vector2(-196,0),new Vector2(170,170));
            SetRect(button.transform.Find("Journey pictogram"),new Vector2(-196,0),new Vector2(145,145));
            var label=button.transform.Find("Label").GetComponent<TMP_Text>();SetRect(label.transform,new Vector2(88,24),new Vector2(365,68));label.fontSize=48;label.color=C("123B50");
            var action=button.transform.Find("Badge action").GetComponent<TMP_Text>();SetRect(action.transform,new Vector2(88,-40),new Vector2(365,40));action.text="Explore  >";action.fontSize=28;action.color=C("315F66");
            // Retain the same ring mesh, and add a thin brighter rim without duplicating it on reruns.
            var rim=d.transform.Find("Highlight rim");
            if(rim==null){rim=Object.Instantiate(d.transform.Find("Colour edge"),d.transform);rim.name="Highlight rim";}
            rim.localPosition=new Vector3(0,.49f,0);rim.localScale=new Vector3(2.84f,.06f,2.84f);
            var source=d.transform.Find("Colour garden terrace").GetComponent<Renderer>().sharedMaterial;
            string path=Art+"/"+d.definition.id+" highlight.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",Color.Lerp(source.GetColor("_BaseColor"),Color.white,.4f));rim.GetComponent<Renderer>().sharedMaterial=mat;EditorUtility.SetDirty(mat);
        }
        static T Ensure<T>(GameObject g) where T:Component { var c=g.GetComponent<T>();if(c==null)c=g.AddComponent<T>();return c;}
        static void SetRect(Transform t,Vector2 p,Vector2 size){var r=(RectTransform)t;r.anchoredPosition=p;r.sizeDelta=size;}
        static void Controls()
        {
            var controller=Object.FindAnyObjectByType<JourneyMapController>();var greeting=(TMP_Text)new SerializedObject(controller).FindProperty("greeting").objectReferenceValue;
            var header=(RectTransform)greeting.transform.parent;header.anchoredPosition=new Vector2(204,-32);
            greeting.fontSize=30;greeting.fontSizeMax=30;greeting.color=C("24545E");
            var controls=(RectTransform)GameObject.Find("Top right controls").transform;controls.sizeDelta=new Vector2(314,132);controls.anchoredPosition=new Vector2(-36,-32);
            int index=0;
            foreach(string name in new[]{"Shop","Settings"})
            {
                var button=controls.Find(name).GetComponent<UnityEngine.UI.Button>();SetRect(button.transform,new Vector2(index++==0?-78:78,0),new Vector2(140,124));
                button.GetComponent<UnityEngine.UI.Image>().color=C(name=="Shop"?"FFF1C4":"D9F2FF");
                SetRect(button.transform.Find(name+" icon"),new Vector2(0,15),new Vector2(76,76));
                var text=button.transform.Find("Caregiver label").GetComponent<TMP_Text>();text.fontSize=24;text.fontSizeMax=24;SetRect(text.transform,new Vector2(0,-40),new Vector2(132,32));
            }
            var safe=header.parent;var back=safe.Find("Back to Start");
            if(back==null)
            {
                var template=controls.Find("Settings");back=Object.Instantiate(template,safe);back.name="Back to Start";
                var icon=back.Find("Settings icon");icon.name="Back arrow";icon.GetComponent<UnityEngine.UI.Image>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EyeCanDo/Art/Icons/Back.png");
                back.Find("Caregiver label").GetComponent<TMP_Text>().text="Back";
                var button=back.GetComponent<UnityEngine.UI.Button>();while(button.onClick.GetPersistentEventCount()>0)UnityEventTools.RemovePersistentListener(button.onClick,0);UnityEventTools.AddPersistentListener(button.onClick,controller.BackToStart);
            }
            var r=(RectTransform)back;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(36,-32);r.sizeDelta=new Vector2(144,124);back.GetComponent<UnityEngine.UI.Image>().color=C("D9F2FF");back.SetSiblingIndex(header.GetSiblingIndex());
            // Modal roots remain later siblings and block every map control, including Back.
        }
        static Mesh Save(string name,Mesh mesh)
        {var path=Art+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);mesh.name=name;if(old!=null){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);return old;}AssetDatabase.CreateAsset(mesh,path);return mesh;}
        static void Surface(Transform parent,string name,Mesh mesh,Material material,float z)
        {var t=Child(parent,name);t.localPosition=new Vector3(0,0,z);var filter=Ensure<MeshFilter>(t.gameObject);filter.sharedMesh=Save(name,mesh);var renderer=Ensure<MeshRenderer>(t.gameObject);renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
        static Mesh Hill(float height,float amplitude,float phase)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();const int steps=100;
            for(int i=0;i<=steps;i++)
            {float x=-.6f+1.2f*i/steps;float y=height+amplitude*(Mathf.Sin(x*13+phase)*.65f+Mathf.Cos(x*23+phase)*.35f);vertices.Add(new Vector3(x,-.6f,0));vertices.Add(new Vector3(x,y,0));if(i<steps){int n=2*i;triangles.AddRange(new[]{n,n+1,n+2,n+1,n+3,n+2});}}
            var m=new Mesh();m.SetVertices(vertices);m.SetTriangles(triangles,0);m.RecalculateNormals();return m;
        }
        static void Backdrop(Camera camera)
        {
            var existing=GameObject.Find("Scenic sky backdrop");var root=existing!=null?existing:new GameObject("Scenic sky backdrop");
            var fit=Ensure<GardenBackdrop>(root);fit.mapCamera=camera;fit.Fit();
            var quad=new Mesh {vertices=new[]{new Vector3(-.6f,-.6f),new Vector3(-.6f,.6f),new Vector3(.6f,.6f),new Vector3(.6f,-.6f)},uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right},triangles=new[]{0,1,2,0,2,3}};
            Surface(root.transform,"Blue sky gradient",quad,Material("Sky gradient","FFFFFF",true),0);
            Surface(root.transform,"Distant mint hills",Hill(.07f,.07f,.7f),Material("Distant hill mint","83CFC1"),-1);
            Surface(root.transform,"Rolling green hills",Hill(-.03f,.09f,2),Material("Near hill green","63BFA0"),-2);
            var cloudMaterial=Material("Cloud ivory","F4FCFF");cloudMaterial.SetColor("_BaseColor",C("F4FCFF")*1.7f);
            for(int c=0;c<5;c++)
            {
                var cloud=Child(root.transform,"Cloud "+c);cloud.localPosition=new Vector3(-.44f+c*.22f,.22f+(c%2)*.12f,-3);cloud.localScale=Vector3.one;
                for(int i=0;i<5;i++)
                {
                    var t=cloud.Find("Puff "+i);
                    if(t==null){var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);Object.DestroyImmediate(sphere.GetComponent<Collider>());t=sphere.transform;t.name="Puff "+i;t.SetParent(cloud,false);}
                    t.localPosition=new Vector3((i-2)*.023f,i==2?.014f:0,0);t.localScale=new Vector3(.065f,i==2?.052f:.034f,.13f);
                    var renderer=t.GetComponent<Renderer>();renderer.sharedMaterial=cloudMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                }
                var motion=Ensure<GardenMotion>(cloud.gameObject);motion.bob=.003f;motion.sway=0;motion.speed=.22f;
            }
        }
    }
}

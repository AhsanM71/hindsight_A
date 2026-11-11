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
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EyeCanDo.Editor
{
    public static class StorybookGardenBuilder
    {
        public const string Root = "Assets/EyeCanDo/JourneyGarden";
        const string Map = "Assets/EyeCanDo/Scenes/JourneyMap.unity";
        static readonly Dictionary<string,Material> materials = new();
        static readonly Dictionary<string,Mesh> meshes = new();
        static TMP_FontAsset font;
        static Sprite round;
        static Color C(string hex) { ColorUtility.TryParseHtmlString("#"+hex,out var color); return color; }

        [MenuItem("EyeCanDo/Journey Map/Author Storybook Garden")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before authoring the map.");
            if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene before authoring the map.");
            EditorSceneManager.OpenScene(Map);
            foreach(string folder in new[]{"Materials","Meshes","Prefabs","Lighting","UI"})Directory.CreateDirectory(Root+"/"+folder);
            AssetDatabase.Refresh();materials.Clear();meshes.Clear();
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            round=CreateRoundSprite();
            var layout=Object.FindAnyObjectByType<JourneyMapLayout>();
            if(layout==null)throw new InvalidOperationException("Expected JourneyMap layout is missing.");
            foreach(string name in new[]{"Clinic adventure garden","Owl","Storybook garden"})
            { var old=GameObject.Find(name); if(old!=null)Object.DestroyImmediate(old); }
            var world=Group("Storybook garden");
            Environment(world.transform);
            var definitions=layout.catalog.journeys;
            var positions=new[]{new Vector3(-8,0,-.8f),new Vector3(0,0,1.1f),new Vector3(8,0,-.8f)};
            var entries=new List<JourneyMapLayout.Placement>();
            for(int i=0;i<definitions.Length;i++)
            {
                int kind=definitions[i].id=="eye-drops"?0:definitions[i].id=="slit-lamp"?1:definitions[i].id=="vision-testing"?2:-1;
                if(kind<0)continue;
                var prefab=Destination(definitions[i],kind);
                entries.Add(new JourneyMapLayout.Placement {journeyId=definitions[i].id,position=positions[kind],presentationPrefab=prefab});
            }
            layout.authoredPlacements=entries.ToArray();layout.curvedPaths=true;layout.pathWidth=1.65f;
            layout.pathMaterial=Mat("Warm limestone","F6DDA4");
            layout.fitPresentationBounds=true;layout.cameraFocus=new Vector3(0,1.5f,2.5f);
            layout.compositionBounds=new Bounds(new Vector3(0,2.5f,2.5f),new Vector3(32,6.5f,17));
            layout.worldViewport=new Rect(.035f,.115f,.93f,.755f);
            layout.mapCamera.transform.rotation=Quaternion.Euler(36,0,0);
            layout.Rebuild();
            foreach(var filter in layout.paths.GetComponentsInChildren<MeshFilter>())
                filter.sharedMesh=SaveMesh("Path-"+filter.gameObject.name,filter.sharedMesh);
            layout.FitCamera();
            Lighting(layout.mapCamera);
            Header();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("Storybook garden authored. Only JourneyMap and JourneyGarden assets were saved.");
        }

        static GameObject Group(string name,Transform parent=null)
        {var g=new GameObject(name);g.transform.SetParent(parent,false);return g;}
        static Material Mat(string name,string color,float smooth=.22f)
        {
            if(materials.TryGetValue(name,out var cached))return cached;
            string path=Root+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",C(color));m.SetFloat("_Smoothness",smooth);m.enableInstancing=true;
            EditorUtility.SetDirty(m);materials[name]=m;return m;
        }
        static Mesh SaveMesh(string name,Mesh m)
        {
            string path=Root+"/Meshes/"+name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            m.name=name;
            if(existing!=null){EditorUtility.CopySerialized(m,existing);Object.DestroyImmediate(m);m=existing;}
            else AssetDatabase.CreateAsset(m,path);
            meshes[name]=m;return m;
        }
        static Mesh Mesh(string name,Func<Mesh> create)
        {if(meshes.TryGetValue(name,out var m))return m;return SaveMesh(name,create());}
        static GameObject Piece(string name,Transform parent,Vector3 p,Mesh mesh,Material mat)
        {
            var g=Group(name,parent);g.transform.localPosition=p;g.AddComponent<MeshFilter>().sharedMesh=mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial=mat;return g;
        }
        static GameObject Box(string name,Transform parent,Vector3 p,Vector3 size,Material mat,float radius=.12f)
        {
            radius=Mathf.Min(radius,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.49f);
            string key=$"Box-{size.x:F2}-{size.y:F2}-{size.z:F2}-{radius:F2}";
            return Piece(name,parent,p,Mesh(key,()=>GardenGeometry.RoundedBox(size,radius)),mat);
        }
        static GameObject Ball(string name,Transform parent,Vector3 p,Vector3 scale,Material mat)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;
            Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;return g;
        }
        static GameObject Lathe(string name,Transform parent,Vector3 p,Vector2[] profile,Material mat)
        {return Piece(name,parent,p,Mesh(name,()=>GardenGeometry.Lathe(profile)),mat);}
        static GameObject Disc(string name,Transform parent,Vector3 p,float radius,float thickness,Material mat)
        {
            var m=Mesh("Rounded disc",()=>GardenGeometry.Lathe(new[]{new Vector2(0,-.5f),new Vector2(.92f,-.5f),new Vector2(1,-.25f),new Vector2(1,.25f),new Vector2(.92f,.5f),new Vector2(0,.5f)}));
            var g=Piece(name,parent,p,m,mat);g.transform.localScale=new Vector3(radius,thickness,radius);return g;
        }
        static GameObject Rod(string name,Transform parent,Vector3 a,Vector3 b,float radius,Material mat)
        {var g=Disc(name,parent,(a+b)*.5f,radius,(b-a).magnitude,mat);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);return g;}
        static GameObject Symbol(string name,Transform parent,Vector3 p,float scale,Material mat,bool drop=false)
        {var g=Piece(name,parent,p,Mesh(drop?"Drop symbol":"Star symbol",()=>drop?GardenGeometry.Drop():GardenGeometry.Star()),mat);g.transform.localScale=Vector3.one*scale;return g;}
        static void Motion(GameObject g,float bob=.08f,float sway=2)
        {var m=g.AddComponent<GardenMotion>();m.bob=bob;m.sway=sway;}
        static void Static(GameObject g)
        {foreach(var t in g.GetComponentsInChildren<Transform>())if(t.GetComponentInParent<GardenMotion>()==null)GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic);}

        static GameObject Destination(JourneyDefinition definition,int kind)
        {
            string[] accents={"22B5D1","9364D9","FFBD37"};string[] darks={"147FAD","6746AB","E4842E"};
            var accent=Mat("Destination "+kind,accents[kind]);var dark=Mat("Destination rim "+kind,darks[kind]);var cream=Mat("Porcelain","FFF4D9");
            var root=Group(definition.id);var t=root.transform;
            Disc("Sculpted foundation",t,new Vector3(0,.14f,0),2.95f,.36f,Mat("Platform stone","DFC59A"));
            Disc("Colour edge",t,new Vector3(0,.35f,0),2.79f,.27f,dark);
            Disc("Colour garden terrace",t,new Vector3(0,.50f,0),2.64f,.15f,accent);
            // Inlaid petal markers circle each plinth without obstructing its equipment.
            for(int i=0;i<14;i++)
            {float a=i*Mathf.PI*2/14;Ball("Terrace inlay",t,new Vector3(Mathf.Cos(a)*2.42f,.6f,Mathf.Sin(a)*2.42f),new Vector3(.11f,.025f,.11f),cream);}
            var equipment=Group("Equipment",t);equipment.transform.localPosition=new Vector3(0,.62f,0);
            if(kind==0)Bottle(equipment.transform);else if(kind==1)SlitLamp(equipment.transform);else Chart(equipment.transform);
            var dest=root.AddComponent<JourneyDestination>();dest.definition=definition;dest.equipment=equipment.transform;
            root.AddComponent<GardenPressFeedback>().equipment=equipment.transform;
            var hit=root.AddComponent<BoxCollider>();hit.center=new Vector3(0,2.3f,0);hit.size=new Vector3(5.1f,4.5f,3.6f);
            Badge(t,dest,definition,kind,accents[kind]);
            for(int i=0;i<4;i++)
            {
                float x=(i%2==0?-1:1)*(1.85f+(i/2)*.2f),z=.5f+(i/2)*.7f;
                Nature("Flower_3_Group",t,new Vector3(x,.55f,z),.38f,30*i,kind==0?"75D7EF":kind==1?"DDA2F2":"FFD454");
            }
            string path=Root+"/Prefabs/"+definition.id+".prefab";
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);return prefab;
        }
        static void Bottle(Transform t)
        {
            var blue=Mat("Bottle blue","208FD2",.42f);var turquoise=Mat("Bottle turquoise","38D7CC",.4f);var cream=Mat("Porcelain","FFF4D9");
            Lathe("Bottle body",t,new Vector3(-.2f,0,0),new[]{new Vector2(0,0),new Vector2(.63f,0),new Vector2(.83f,.13f),new Vector2(.89f,.38f),new Vector2(.89f,1.72f),new Vector2(.8f,1.99f),new Vector2(.49f,2.22f),new Vector2(.46f,2.36f),new Vector2(0,2.36f)},blue);
            Disc("Bottle base band",t,new Vector3(-.2f,.3f,0),.9f,.15f,turquoise);
            Disc("Ribbed collar",t,new Vector3(-.2f,2.34f,0),.59f,.38f,turquoise);
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12;var rib=Box("Cap grip",t,new Vector3(-.2f+Mathf.Cos(a)*.585f,2.34f,Mathf.Sin(a)*.585f),new Vector3(.07f,.31f,.09f),Mat("Cap highlight","99F4E2"),.03f);rib.transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
            }
            Lathe("Tapered nozzle",t,new Vector3(-.2f,2.53f,0),new[]{new Vector2(0,0),new Vector2(.37f,0),new Vector2(.32f,.23f),new Vector2(.13f,.75f),new Vector2(.1f,.82f),new Vector2(0,.82f)},cream);
            Disc("Nozzle opening",t,new Vector3(-.2f,3.355f,0),.075f,.012f,Mat("Deep ink","173D5D"));
            var label=Disc("Bottle emblem medallion",t,new Vector3(-.2f,1.2f,-.86f),.59f,.09f,cream);label.transform.localRotation=Quaternion.Euler(90,0,0);
            Symbol("Droplet emblem",t,new Vector3(-.2f,1.23f,-.94f),.91f,blue,true);
            var cap=Disc("Resting cap",t,new Vector3(1.17f,.31f,.2f),.48f,.59f,turquoise);
            for(int i=0;i<14;i++)
            {float a=i*Mathf.PI*2/14;Box("Cap ribs",cap.transform,new Vector3(Mathf.Cos(a)*.93f,0,Mathf.Sin(a)*.93f),new Vector3(.1f,.7f,.1f),cream,.03f);}
            for(int i=0;i<3;i++)Motion(Symbol("Floating blue drop",t,new Vector3(-1.34f+i*1.28f,2.05f+i*.63f,-.2f),.52f-i*.07f,Mat("Floating water","76E8F4",.5f),true),.13f,3);
        }
        static void SlitLamp(Transform t)
        {
            var cream=Mat("Porcelain","FFF4D9");var plum=Mat("Optics purple","7447AE",.35f);var dark=Mat("Deep ink","173D5D");var lilac=Mat("Lilac trim","C9A5F3");var lens=Mat("Optics glass","5ED7D7",.65f);
            Disc("Examination table base",t,new Vector3(0,.13f,0),1.1f,.25f,plum);
            Rod("Table pedestal",t,new Vector3(0,.2f,.1f),new Vector3(0,1.3f,.1f),.24f,lilac);
            Box("Examination table",t,new Vector3(0,1.4f,-.05f),new Vector3(3.3f,.23f,1.8f),cream,.1f);
            Box("Optics carriage",t,new Vector3(-.38f,1.59f,0),new Vector3(1.1f,.17f,1.1f),plum);
            Rod("Binocular support",t,new Vector3(-.4f,1.65f,.1f),new Vector3(-.4f,2.54f,.1f),.14f,plum);
            Box("Binocular head",t,new Vector3(-.48f,2.69f,0),new Vector3(1.2f,.58f,.69f),lilac,.18f);
            for(int i=0;i<2;i++)
            {
                float x=-.81f+i*.65f;
                Rod("Binocular optical barrel",t,new Vector3(x,2.7f,.07f),new Vector3(x,2.7f,-.8f),.235f,dark);
                var l=Disc("Blue eyepiece",t,new Vector3(x,2.7f,-.82f),.17f,.035f,lens);l.transform.localRotation=Quaternion.Euler(90,0,0);
                Ball("Lens glint",t,new Vector3(x-.05f,2.75f,-.85f),new Vector3(.065f,.065f,.025f),cream);
            }
            Rod("Separate illumination tower",t,new Vector3(.54f,1.55f,.43f),new Vector3(.54f,3.4f,.43f),.19f,plum);
            Box("Lamp housing",t,new Vector3(.54f,3.37f,.43f),new Vector3(.55f,.43f,.55f),cream,.16f);
            Box("Illumination slit",t,new Vector3(.54f,3.36f,.14f),new Vector3(.07f,.24f,.025f),Mat("Lamp gold","FFD260"),.02f);
            // Patient support frame has independent chin and forehead rests.
            for(int i=0;i<2;i++)Rod("Patient frame upright",t,new Vector3(.78f+i*.68f,1.54f,-.48f),new Vector3(.78f+i*.68f,3.02f,-.48f),.055f,dark);
            Box("Forehead rest",t,new Vector3(1.12f,3.02f,-.48f),new Vector3(.85f,.16f,.24f),cream,.07f);
            Box("Chin rest",t,new Vector3(1.12f,2.12f,-.48f),new Vector3(.81f,.14f,.40f),lilac,.065f);
            Rod("Joystick",t,new Vector3(-.75f,1.54f,-.65f),new Vector3(-.75f,1.89f,-.65f),.055f,dark);Ball("Joystick grip",t,new Vector3(-.75f,1.9f,-.65f),Vector3.one*.21f,plum);
            for(int i=0;i<2;i++)Rod("Adjustment dial",t,new Vector3(-1.08f,1.65f,.1f+i*.43f),new Vector3(-1.26f,1.65f,.1f+i*.43f),.12f,Mat("Lamp gold","FFD260"));
            var chair=Group("Small patient chair",t);chair.transform.localPosition=new Vector3(1.68f,0,-1.06f);
            Disc("Chair seat",chair.transform,new Vector3(0,.75f,0),.47f,.18f,lilac);
            Rod("Chair stem",chair.transform,new Vector3(0,.12f,0),new Vector3(0,.72f,0),.08f,dark);
            for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;Rod("Chair foot",chair.transform,new Vector3(0,.18f,0),new Vector3(Mathf.Cos(a)*.44f,.06f,Mathf.Sin(a)*.44f),.05f,dark);}
            Rod("Chair back support",chair.transform,new Vector3(0,.7f,-.35f),new Vector3(0,1.43f,-.35f),.055f,dark);
            Box("Rounded chair back",chair.transform,new Vector3(0,1.4f,-.35f),new Vector3(.82f,.5f,.16f),plum,.08f);
        }
        static void Chart(Transform t)
        {
            var orange=Mat("Chart orange","F39536",.3f);var gold=Mat("Chart golden yellow","FFD459");var cream=Mat("Chart paper","FFFCED");var ink=Mat("Deep ink","173D5D");
            Disc("Chart foot",t,new Vector3(0,.12f,0),.99f,.23f,orange);
            Rod("Chart stand",t,new Vector3(0,.2f,.1f),new Vector3(0,1.1f,.1f),.16f,orange);
            Box("Orange chart frame",t,new Vector3(0,2.26f,0),new Vector3(2.75f,3.0f,.32f),orange,.16f);
            Box("Golden chart border",t,new Vector3(0,2.28f,-.18f),new Vector3(2.59f,2.83f,.14f),gold,.065f);
            Box("Warm white picture chart",t,new Vector3(0,2.28f,-.27f),new Vector3(2.33f,2.56f,.07f),cream,.03f);
            Picture(t,new Vector3(0,3.13f,-.34f),.68f,0,ink);
            for(int i=0;i<2;i++)Picture(t,new Vector3((i-.5f)*1.0f,2.43f,-.34f),.46f,i+1,ink);
            for(int i=0;i<3;i++)Picture(t,new Vector3((i-1)*.64f,1.9f,-.34f),.31f,i,ink);
            for(int i=0;i<4;i++)Picture(t,new Vector3((i-1.5f)*.49f,1.48f,-.34f),.22f,(i+1)%3,ink);
            for(int i=0;i<3;i++)
            {var star=Symbol("Garden star",t,new Vector3(i==0?-1.85f:1.72f,1.4f+i*.88f,.1f),.7f-i*.1f,gold);Motion(star,.09f,6);}
        }
        static void Picture(Transform parent,Vector3 p,float scale,int kind,Material ink)
        {
            var t=Group(kind==0?"Picture - house":kind==1?"Picture - apple":"Picture - star",parent).transform;t.localPosition=p;t.localScale=Vector3.one*scale;
            if(kind==0)
            {
                Piece("House silhouette",t,Vector3.zero,Mesh("House picture",()=>GardenGeometry.Shape(new[]{new Vector2(-.42f,-.4f),new Vector2(.42f,-.4f),new Vector2(.42f,.12f),new Vector2(.58f,.12f),new Vector2(0,.62f),new Vector2(-.58f,.12f),new Vector2(-.42f,.12f)},.04f)),ink);
                Box("House door",t,new Vector3(0,-.24f,-.04f),new Vector3(.17f,.30f,.02f),Mat("Chart paper","FFFCED"),.01f);
            }
            else if(kind==1)
            {
                Ball("Apple left",t,new Vector3(-.15f,-.03f,0),new Vector3(.62f,.7f,.06f),ink);Ball("Apple right",t,new Vector3(.15f,-.03f,0),new Vector3(.62f,.7f,.06f),ink);
                Rod("Apple stalk",t,new Vector3(0,.28f,0),new Vector3(.1f,.55f,0),.065f,ink);var leaf=Ball("Apple leaf",t,new Vector3(.23f,.43f,0),new Vector3(.31f,.16f,.06f),ink);leaf.transform.localRotation=Quaternion.Euler(0,0,25);
            }
            else Symbol("Star picture",t,Vector3.zero,1,ink);
        }
        static GameObject Nature(string model,Transform parent,Vector3 p,float scale,float angle,string leafColor="79B747")
        {
            string path=Root+"/ThirdParty/Quaternius/Models/"+model+".fbx";
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(source==null)throw new InvalidOperationException("Missing nature model: "+path);
            var sourceRenderers=source.GetComponentsInChildren<Renderer>();
            var bounds=sourceRenderers[0].bounds;foreach(var r in sourceRenderers)bounds.Encapsulate(r.bounds);
            float standardHeight=model.Contains("Tree")||model.Contains("Pine")?7:model.Contains("Fern")?1.7f:model.Contains("Bush")?1.8f:model.Contains("Rock")?2:model.Contains("Pebble")?.3f:model.Contains("Grass")?.8f:model.Contains("Mushroom")?1.2f:2;
            scale*=standardHeight/Mathf.Max(.01f,bounds.size.y);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(source);g.transform.SetParent(parent,false);g.transform.localPosition=p-Vector3.up*bounds.min.y*scale;g.transform.localScale=Vector3.one*scale;g.transform.localRotation=Quaternion.Euler(0,angle,0);
            foreach(var renderer in g.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(original=>
                {
                    string name=original==null?"Leaves":original.name;
                    if(name.Contains("Bark"))return Mat("Nature bark","A47648");
                    if(name.Contains("Rock"))return Mat("Nature stone","B6B79C");
                    if(name.Contains("Flower"))return NatureCutout("Flowers","FFFFFF");
                    if(name.Contains("Mushroom"))return Mat("Nature mushroom","E98864");
                    return NatureCutout(name=="Leaves_Pine"?"Leaf_Pine":name,name=="Leaves"?"A5D69A":leafColor);
                }).ToArray();
            }
            foreach(var collider in g.GetComponentsInChildren<Collider>())Object.DestroyImmediate(collider);
            if(model.Contains("Tree") || model.Contains("Pine"))Motion(g,0,.65f);
            return g;
        }
        static Material NatureCutout(string textureName,string color)
        {
            var mat=Mat("Nature "+textureName+" "+color,color,.05f);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/ThirdParty/Quaternius/Textures/"+textureName+".png");
            if(texture==null)texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/ThirdParty/Quaternius/Textures/Leaves.png");
            mat.SetTexture("_BaseMap",texture);mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.45f);mat.SetFloat("_Cull",0);mat.EnableKeyword("_ALPHATEST_ON");mat.SetOverrideTag("RenderType","TransparentCutout");mat.renderQueue=(int)RenderQueue.AlphaTest;return mat;
        }
        static void Flower(Transform parent,Vector3 p,string color,float scale=1)
        {
            var flower=Group("Storybook blossom",parent);flower.transform.localPosition=p;flower.transform.localScale=Vector3.one*scale;
            var t=flower.transform;var green=Mat("Flower stems","348665");Rod("Stem",t,Vector3.zero,new Vector3(0,.48f,0),.035f,green);
            var leaf=Ball("Little leaf",t,new Vector3(.11f,.2f,0),new Vector3(.25f,.08f,.11f),green);leaf.transform.localRotation=Quaternion.Euler(0,0,25);
            for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Ball("Petal",t,new Vector3(Mathf.Sin(a)*.16f,.5f+Mathf.Cos(a)*.16f,-.02f),new Vector3(.22f,.24f,.11f),Mat("Petals "+color,color));}
            Ball("Flower heart",t,new Vector3(0,.5f,-.10f),Vector3.one*.16f,Mat("Pollen gold","FFD461"));
        }
        static void Path(string name,Transform parent,Vector3 a,Vector3 b,Vector3 c,Vector3 d,float width)
        {
            Piece(name+" edging",parent,Vector3.zero,Mesh(name+" edge",()=>GardenPathMesh.Create(a-Vector3.up*.02f,b-Vector3.up*.02f,c-Vector3.up*.02f,d-Vector3.up*.02f,width+.22f)),Mat("Path edge","D8BD86"));
            Piece(name,parent,Vector3.zero,Mesh(name,()=>GardenPathMesh.Create(a,b,c,d,width)),Mat("Warm limestone","F6DDA4"));
        }
        static void Environment(Transform t)
        {
            var earth=Piece("Rounded earth bank",t,new Vector3(0,-.25f,2),Mesh("Earth bank",()=>GardenGeometry.Island(15.4f,10.8f,.15f,-.75f)),Mat("Earth ochre","BF9660"));
            Piece("Gently raised lawn",t,new Vector3(0,0,2),Mesh("Lawn island",()=>GardenGeometry.Island(15.3f,10.7f,.07f,-.3f)),Mat("Meadow green","92C85D"));
            // Broad connected trails: from the foreground into the destination arc and back to the clinic door.
            Path("Welcome path",t,new Vector3(-1,.10f,-8),new Vector3(-4,.10f,-5.5f),new Vector3(-1.5f,.10f,-3.7f),new Vector3(0,.10f,1.1f),1.7f);
            Path("Clinic path",t,new Vector3(0,.10f,1.3f),new Vector3(3,.10f,3.5f),new Vector3(3.6f,.10f,5.6f),new Vector3(2.3f,.10f,6.5f),1.7f);
            var clinic=Clinic();clinic.transform.SetParent(t,false);clinic.transform.localPosition=new Vector3(2.3f,.12f,8);
            var planting=Group("Layered perimeter planting",t).transform;
            // A varied backdrop with gaps around the clinic's sign and silhouette.
            float[] xs={-12.7f,-9.8f,-6.5f,6.8f,10.0f,13};
            for(int i=0;i<xs.Length;i++)
            {
                string model=i%3==0?"TwistedTree_1":i%3==1?"CommonTree_3":"Pine_2";
                Nature(model,planting,new Vector3(xs[i],.05f,6.2f+(i%2)*2),.61f+(i%3)*.08f,37*i,i%2==0?"59AD65":"A2C750");
            }
            Nature("CommonTree_2",planting,new Vector3(-13.3f,0,1.9f),.68f,45,"4DAE85");
            Nature("CommonTree_4",planting,new Vector3(13.5f,0,2.4f),.66f,-24,"65B880");
            // Low foreground banks leave a clear approach to every badge.
            var random=new System.Random(73);
            for(int i=0;i<46;i++)
            {
                float a=i*Mathf.PI*2/46;
                float x=Mathf.Cos(a)*13.9f,z=2+Mathf.Sin(a)*8.9f;
                if(z < -5.5f && x>-4.1f && x<1.6f)continue;
                // Keep a foreground opening in front of the left and right touch badges.
                if(z < -3.2f && (Mathf.Abs(x+8)<2.7f || Mathf.Abs(x-8)<2.7f))continue;
                float s=.40f+(float)random.NextDouble()*.34f;
                Nature(i%3==0?"Bush_Common_Flowers":"Bush_Common",planting,new Vector3(x,.0f,z),s,i*47,i%3==0?"78B657":i%3==1?"4F9D68":"AAD163");
                if(i%3==0)Nature("Rock_Medium_"+(i%2+1),planting,new Vector3(x*.94f,0,z+.25f),.28f,i*23);
                if(i%2==0)Nature("Fern_1",planting,new Vector3(x*.92f,0,z-.3f),.5f,i*29,"41986A");
            }
            for(int side=-1;side<=1;side+=2)
            {
                // Terraced planting islands between the destinations and the outer trees.
                Ball("Small grassy rise",t,new Vector3(side*11.6f,-.17f,3.7f),new Vector3(5,1.1f,4.5f),Mat("Mound green","83BB51"));
                for(int j=0;j<7;j++)Nature(j%2==0?"Flower_4_Group":"Flower_3_Group",planting,new Vector3(side*(10.6f+j*.43f),.15f,3.1f+(j%3)*.6f),.37f,j*38,j%2==0?"FFD65E":"EE9DCC");
            }
            Vector3[] beds={new Vector3(-9,0,-4),new Vector3(-5.1f,0,-3.6f),new Vector3(4.8f,0,-3.8f),new Vector3(9.5f,0,-4),new Vector3(-6.1f,0,4),new Vector3(5.7f,0,4.1f)};
            for(int b=0;b<beds.Length;b++)
            {
                Nature("Bush_Common",planting,beds[b]+new Vector3(0,0,.5f),.49f,b*31,"5AA66B");
                for(int j=0;j<7;j++)Flower(planting,beds[b]+new Vector3((j%3-1)*.35f,0,(j/3)*.38f),b<2?"74CDEE":b<4?"FFD36E":"DFA0EF",.72f+(j%2)*.15f);
                Nature("Grass_Common_Short",planting,beds[b]+new Vector3(.65f,0,-.1f),.5f,12,"549C59");
            }
            // Small rock groups and mushrooms provide scale, without introducing colliders.
            for(int i=0;i<8;i++)
            {float x=-11+i*3.1f;Nature("Pebble_Round_"+(i%5+1),planting,new Vector3(x,.1f,-6.5f+(i%2)*.5f),.55f,i*57);}
            Nature("Mushroom_Common",planting,new Vector3(-11.8f,.05f,-3.6f),.48f,0);
            Nature("Mushroom_Common",planting,new Vector3(11.3f,.05f,-3.8f),.35f,50);
            Static(t.gameObject);
        }
        static GameObject Clinic()
        {
            var root=Group("Whimsical eye clinic");var t=root.transform;
            var wall=Mat("Clinic buttercream","FFE4A0");var cream=Mat("Porcelain","FFF4D9");var roof=Mat("Clinic coral roof","E87569");var teal=Mat("Clinic teal","309F9B");var glass=Mat("Clinic window blue","8FDFE6",.4f);
            Box("Clinic foundation",t,new Vector3(0,.18f,0),new Vector3(6.8f,.36f,3.4f),Mat("Platform stone","DFC59A"),.16f);
            Box("Rounded clinic walls",t,new Vector3(0,1.75f,0),new Vector3(6.2f,3.2f,3.1f),wall,.3f);
            Piece("Cream roof fascia",t,new Vector3(0,3.15f,0),Mesh("Clinic roof trim",()=>GardenGeometry.Roof(7.05f,1.8f,3.7f)),cream);
            Piece("Coral gabled roof",t,new Vector3(0,3.29f,0),Mesh("Clinic roof",()=>GardenGeometry.Roof(6.95f,1.76f,3.6f)),roof);
            // Layered caps soften the roof ridge and eaves.
            Rod("Rounded ridge",t,new Vector3(0,5.05f,-1.85f),new Vector3(0,5.05f,1.85f),.12f,Mat("Roof rose trim","BE5868"));
            for(int i=0;i<9;i++)
            {float z=-1.65f+i*.42f;Rod("Roof tile seam left",t,new Vector3(-3.3f,3.45f,z),new Vector3(0,5.02f,z),.025f,Mat("Roof rose trim","BE5868"));Rod("Roof tile seam right",t,new Vector3(0,5.02f,z),new Vector3(3.3f,3.45f,z),.025f,Mat("Roof rose trim","BE5868"));}
            Box("Door arch frame",t,new Vector3(0,1.39f,-1.60f),new Vector3(1.68f,2.57f,.22f),cream,.37f);
            Box("Visible turquoise entrance",t,new Vector3(0,1.33f,-1.74f),new Vector3(1.38f,2.35f,.13f),teal,.31f);
            Box("Entrance window",t,new Vector3(0,1.81f,-1.82f),new Vector3(.92f,.91f,.05f),glass,.2f);
            Ball("Brass doorknob",t,new Vector3(.46f,.99f,-1.85f),Vector3.one*.13f,Mat("Pollen gold","FFD461"));
            Box("Welcome step",t,new Vector3(0,.22f,-2),new Vector3(2.5f,.25f,.9f),cream,.1f);
            for(int side=-1;side<=1;side+=2)
            {
                Box("Window surround",t,new Vector3(side*2.05f,1.83f,-1.6f),new Vector3(1.32f,1.5f,.18f),cream,.23f);
                Box("Blue window",t,new Vector3(side*2.05f,1.85f,-1.71f),new Vector3(1.07f,1.23f,.05f),glass,.17f);
                Box("Window mullion",t,new Vector3(side*2.05f,1.85f,-1.75f),new Vector3(.065f,1.23f,.04f),cream,.02f);
                Box("Window crossbar",t,new Vector3(side*2.05f,1.85f,-1.75f),new Vector3(1.07f,.065f,.04f),cream,.02f);
                Box("Window flower box",t,new Vector3(side*2.05f,1.07f,-1.9f),new Vector3(1.4f,.32f,.45f),teal,.1f);
                for(int j=0;j<3;j++)Flower(t,new Vector3(side*2.05f+(j-1)*.36f,1.18f,-1.91f),"F39CC0",.55f);
            }
            Box("Clinic sign backing",t,new Vector3(0,3.55f,-1.9f),new Vector3(3.1f,.63f,.16f),cream,.13f);
            var sign=WorldText("Clinic sign",t,"EYE CLINIC",.32f,new Vector3(0,3.55f,-2.0f),new Vector2(2.8f,.45f));sign.fontStyle=FontStyles.Bold;
            var eye=Ball("Eye clinic emblem",t,new Vector3(0,4.31f,-1.87f),new Vector3(.84f,.47f,.09f),cream);Ball("Eye emblem iris",t,new Vector3(0,4.31f,-1.94f),new Vector3(.30f,.30f,.06f),teal);Ball("Eye emblem pupil",t,new Vector3(0,4.31f,-1.98f),new Vector3(.14f,.14f,.04f),Mat("Deep ink","173D5D"));
            Box("Chimney",t,new Vector3(2.12f,4.17f,.58f),new Vector3(.65f,1.6f,.65f),wall,.1f);Box("Chimney cap",t,new Vector3(2.12f,5.0f,.58f),new Vector3(.85f,.21f,.82f),teal,.09f);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/StorybookClinic.prefab");Object.DestroyImmediate(root);return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }
        static TMP_Text WorldText(string name,Transform parent,string text,float size,Vector3 position,Vector2 bounds)
        {
            var g=Group(name,parent);g.transform.localPosition=position;var label=g.AddComponent<TextMeshPro>();label.font=font;label.text=text;label.fontSize=size*10;label.color=C("244955");label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=bounds;return label;
        }
        static void Lighting(Camera camera)
        {
            if(UniversalRenderPipeline.asset==null)throw new InvalidOperationException("URP is required.");
            camera.backgroundColor=C("E8F1DF");camera.clearFlags=CameraClearFlags.SolidColor;camera.allowHDR=true;
            var data=camera.GetComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;data.volumeLayerMask=1;
            var light=GameObject.Find("Warm sunlight").GetComponent<Light>();light.color=C("FFF0CD");light.intensity=1.8f;light.shadows=LightShadows.Soft;light.shadowStrength=.65f;light.shadowBias=.03f;light.transform.rotation=Quaternion.Euler(48,-35,0);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=C("B7DBEC");RenderSettings.ambientEquatorColor=C("9CC5BB");RenderSettings.ambientGroundColor=C("769867");RenderSettings.ambientIntensity=1;
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/Lighting/GardenSunlight.asset");
            if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Root+"/Lighting/GardenSunlight.asset");}
            T Effect<T>() where T:VolumeComponent {if(!profile.TryGet<T>(out var effect)){effect=profile.Add<T>();AssetDatabase.AddObjectToAsset(effect,profile);}return effect;}
            Effect<Tonemapping>().mode.Override(TonemappingMode.Neutral);
            var grading=Effect<ColorAdjustments>();grading.saturation.Override(9);grading.contrast.Override(6);grading.postExposure.Override(.12f);
            var bloom=Effect<Bloom>();bloom.intensity.Override(.08f);bloom.threshold.Override(1.25f);bloom.maxIterations.Override(3);
            var volume=(GameObject.Find("Garden sunlight volume")??GameObject.Find("Soft clinic lighting")).GetComponent<Volume>();volume.name="Garden sunlight volume";volume.sharedProfile=profile;EditorUtility.SetDirty(profile);
        }
        static Sprite CreateRoundSprite()
        {
            const int size=128;const float radius=30;
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=Mathf.Max(Mathf.Abs(x+ .5f-size*.5f)-(size*.5f-radius),0),dy=Mathf.Max(Mathf.Abs(y+.5f-size*.5f)-(size*.5f-radius),0);
                float alpha=Mathf.Clamp01(radius-Mathf.Sqrt(dx*dx+dy*dy)+.5f);tex.SetPixel(x,y,new Color(1,1,1,alpha));
            }
            tex.Apply();string path=Root+"/UI/GardenRound.png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            // A separate Sprite asset explicitly owns the sliced border on all Editor versions.
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var sprite=Sprite.Create(texture,new UnityEngine.Rect(0,0,size,size),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(32,32,32,32));sprite.name="GardenRound";
            var existing=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/UI/GardenRound.asset");
            if(existing!=null){EditorUtility.CopySerialized(sprite,existing);Object.DestroyImmediate(sprite);return existing;}
            AssetDatabase.CreateAsset(sprite,Root+"/UI/GardenRound.asset");return sprite;
        }
        static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;return r;}
        static UnityEngine.UI.Image Image(string name,Transform parent,Vector2 p,Vector2 size,Color color,Sprite sprite=null)
        {var r=Rect(name,parent,p,size);var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=sprite??round;image.type=sprite==null?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;image.color=color;image.raycastTarget=false;return image;}
        static TMP_Text Text(string name,Transform parent,string text,float size,Vector2 p,Vector2 bounds,Color color)
        {var r=Rect(name,parent,p,bounds);var label=r.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.text=text;label.fontSize=size;label.color=color;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;}
        static void Decorate(UnityEngine.UI.Image image,Color border)
        {
            var outline=image.GetComponent<UnityEngine.UI.Outline>()??image.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=border;outline.effectDistance=new Vector2(2,-2);
            var shadows=image.GetComponents<UnityEngine.UI.Shadow>().Where(s=>s.GetType()==typeof(UnityEngine.UI.Shadow)).ToArray();
            var shadow=shadows.FirstOrDefault()??image.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            foreach(var duplicate in shadows.Skip(1))Object.DestroyImmediate(duplicate);
            shadow.effectColor=new Color(.07f,.2f,.19f,.24f);shadow.effectDistance=new Vector2(0,-6);
        }
        static void Badge(Transform parent,JourneyDestination destination,JourneyDefinition definition,int kind,string accent)
        {
            var badge=Rect("Journey badge",parent,Vector2.zero,new Vector2(400,118));badge.localScale=Vector3.one*.01f;badge.localPosition=new Vector3(0,.88f,-2.98f);badge.localRotation=Quaternion.Euler(36,0,0);
            var canvas=badge.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;badge.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var image=Image("Open "+definition.shortTitle,badge,Vector2.zero,new Vector2(400,118),C("FFF8E4"));image.raycastTarget=true;Decorate(image,C(accent));
            var button=image.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;var colors=button.colors;colors.highlightedColor=C("ECFFF3");colors.pressedColor=C("C9E7DE");button.colors=colors;button.gameObject.AddComponent<ButtonFeedback>();UnityEventTools.AddPersistentListener(button.onClick,destination.Open);
            Image("Pictorial medallion",button.transform,new Vector2(-141,5),new Vector2(96,96),C(accent));
            Image("Journey pictogram",button.transform,new Vector2(-141,6),new Vector2(70,70),Color.white,definition.icon);
            var label=Text("Label",button.transform,definition.shortTitle,36,new Vector2(35,9),new Vector2(245,48),C("183D4A"));label.fontStyle=FontStyles.Bold;
            Text("Badge action",button.transform,"EXPLORE  >",18,new Vector2(35,-27),new Vector2(245,26),C("355B58"));
            // Short stems attach the badge visually to its terrace.
            for(int i=-1;i<=1;i+=2)Rod("Badge mount",parent,new Vector3(i*1.15f,.38f,-2.13f),new Vector3(i*1.15f,.88f,-2.98f),.055f,Mat("Porcelain","FFF4D9"));
        }
        static void Header()
        {
            var controller=Object.FindAnyObjectByType<JourneyMapController>();
            var so=new SerializedObject(controller);var greeting=(TMP_Text)so.FindProperty("greeting").objectReferenceValue;
            var header=(RectTransform)greeting.transform.parent;header.sizeDelta=new Vector2(430,124);header.anchoredPosition=new Vector2(42,-32);
            var background=header.GetComponent<UnityEngine.UI.Image>();background.sprite=round;background.color=C("FFFAE9");Decorate(background,C("FFFFFF"));
            var label=greeting.rectTransform;label.anchorMin=label.anchorMax=Vector2.one*.5f;label.pivot=Vector2.one*.5f;label.anchoredPosition=new Vector2(30,-29);label.sizeDelta=new Vector2(324,42);greeting.fontSize=26;greeting.fontSizeMax=26;greeting.fontSizeMin=18;greeting.fontStyle=FontStyles.Normal;greeting.alignment=TextAlignmentOptions.MidlineLeft;greeting.color=C("496E66");
            var invitation=header.Find("Map invitation");if(invitation!=null)Object.DestroyImmediate(invitation.gameObject);
            var brand=header.Find("EyeCanDo brand");if(brand!=null)Object.DestroyImmediate(brand.gameObject);
            var brandLabel=Text("EyeCanDo brand",header,"EyeCanDo",36,new Vector2(30,18),new Vector2(324,50),C("204D50"));brandLabel.fontStyle=FontStyles.Bold;brandLabel.alignment=TextAlignmentOptions.MidlineLeft;
            var oldMark=header.Find("Brand eye");if(oldMark!=null)Object.DestroyImmediate(oldMark.gameObject);
            var mark=Image("Brand eye",header,new Vector2(-163,12),new Vector2(60,39),C("22AAA6"));Image("Brand iris",mark.transform,Vector2.zero,new Vector2(26,26),C("FFF8DF"));Image("Brand pupil",mark.transform,new Vector2(2,0),new Vector2(12,17),C("204D50"));
            var controls=(RectTransform)GameObject.Find("Top right controls").transform;controls.sizeDelta=new Vector2(260,116);controls.anchoredPosition=new Vector2(-42,-32);
            int i=0;
            foreach(string name in new[]{"Shop","Settings"})
            {
                var button=controls.Find(name).GetComponent<UnityEngine.UI.Button>();var r=(RectTransform)button.transform;r.anchoredPosition=new Vector2(i++==0?-65:65,0);r.sizeDelta=new Vector2(112,112);
                var image=button.GetComponent<UnityEngine.UI.Image>();image.sprite=round;image.color=C("FFFAE9");Decorate(image,C("FFFFFF"));
                var icon=button.transform.Find(name+" icon").GetComponent<UnityEngine.UI.Image>();icon.rectTransform.sizeDelta=new Vector2(59,59);icon.rectTransform.anchoredPosition=new Vector2(0,12);
                var text=button.transform.Find("Caregiver label").GetComponent<TMP_Text>();text.fontSize=19;text.fontSizeMax=19;text.fontStyle=FontStyles.Bold;text.color=C("244955");text.rectTransform.anchoredPosition=new Vector2(0,-35);text.rectTransform.sizeDelta=new Vector2(104,28);
            }
            var footer=(RectTransform)GameObject.Find("Map footer").transform;footer.sizeDelta=new Vector2(560,58);footer.anchoredPosition=new Vector2(0,28);footer.GetComponent<UnityEngine.UI.Image>().sprite=round;footer.GetComponent<UnityEngine.UI.Image>().color=C("FFFAE9");
            var footerText=footer.GetComponentInChildren<TMP_Text>();footerText.text="Pick a place. Let curiosity bloom.";footerText.fontSize=24;footerText.fontSizeMax=24;footerText.rectTransform.sizeDelta=new Vector2(530,48);footerText.color=C("355E59");
        }
    }
}

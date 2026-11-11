using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace EyeCanDo.Editor
{
    // Original miniature illustrations. Geometry is authored once and baked to transparent sprites.
    public static class AdventureArt
    {
        public const string Root = "Assets/EyeCanDo/UIAdventure";
        public static Color C(string hex) { ColorUtility.TryParseHtmlString("#"+hex, out var c); return c; }
        public static GameObject Group(string name, Transform parent = null)
        {var g = new GameObject(name); g.transform.SetParent(parent, false); return g;}
        public static Material Mat(string name, string hex)
        {
            string path=Root+"/Materials/"+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",C(hex));m.SetFloat("_Smoothness",.3f);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
        public static Mesh Mesh(string name, Func<Mesh> create)
        {
            string path=Root+"/Meshes/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=create();mesh.name=name;AssetDatabase.CreateAsset(mesh,path);}return mesh;
        }
        public static GameObject Piece(string name, Transform parent, Vector3 p, Mesh mesh, Material material)
        {var g=Group(name,parent);g.transform.localPosition=p;g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=material;return g;}
        public static GameObject Box(string name,Transform parent,Vector3 p,Vector3 size,Material mat,float radius=.15f)
        {radius=Mathf.Min(radius,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.48f);return Piece(name,parent,p,Mesh($"Box-{size.x:F2}-{size.y:F2}-{size.z:F2}-{radius:F2}",()=>GardenGeometry.RoundedBox(size,radius)),mat);}
        public static GameObject Ball(string name,Transform parent,Vector3 p,Vector3 scale,Material mat)
        {var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;return g;}
        public static GameObject Disc(string name,Transform parent,Vector3 p,float radius,float thickness,Material mat)
        {var mesh=Mesh("Rounded disc",()=>GardenGeometry.Lathe(new[]{new Vector2(0,-.5f),new Vector2(.92f,-.5f),new Vector2(1,-.22f),new Vector2(1,.22f),new Vector2(.92f,.5f),new Vector2(0,.5f)}));var g=Piece(name,parent,p,mesh,mat);g.transform.localScale=new Vector3(radius,thickness,radius);return g;}
        public static GameObject Rod(string name,Transform parent,Vector3 a,Vector3 b,float radius,Material mat)
        {var g=Disc(name,parent,(a+b)*.5f,radius,(b-a).magnitude,mat);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);return g;}
        public static GameObject Ring(string name,Transform parent,Vector3 p,float outer,float inner,float thickness,Material mat)
        {
            var g=Piece(name,parent,p,Mesh($"Ring-{outer}-{inner}-{thickness}",()=>GardenGeometry.Lathe(new[]{new Vector2(inner,-thickness/2),new Vector2(outer-.06f,-thickness/2),new Vector2(outer,0),new Vector2(outer-.06f,thickness/2),new Vector2(inner,thickness/2),new Vector2(inner,-thickness/2)})),mat);g.transform.localRotation=Quaternion.Euler(90,0,0);return g;
        }
        public static GameObject Equipment(int kind, Transform parent,Vector3 p,float scale)
        {
            string id=new[]{"eye-drops","slit-lamp","vision-testing"}[kind];
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EyeCanDo/JourneyGarden/Prefabs/"+id+".prefab");
            var g=Object.Instantiate(prefab.transform.Find("Equipment").gameObject,parent);g.name="Hero "+id;g.transform.localPosition=p;g.transform.localScale=Vector3.one*scale;
            foreach(var b in g.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(b);
            foreach(var c in g.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
            return g;
        }
        static void Play(Transform parent,Vector3 p,float scale)
        {
            var t=Group("Play symbol",parent).transform;t.localPosition=p;t.localScale=Vector3.one*scale;
            var disk=Disc("Play turquoise",t,Vector3.zero,.72f,.18f,Mat("Deep turquoise","087F86"));disk.transform.localRotation=Quaternion.Euler(90,0,0);
            Piece("White play arrow",t,new Vector3(.06f,0,-.13f),Mesh("Play triangle",()=>GardenGeometry.Shape(new[]{new Vector2(-.23f,-.36f),new Vector2(.38f,0),new Vector2(-.23f,.36f)},.10f)),Mat("Ivory","FFF8DF"));
        }
        static void Magnifier(Transform parent, Vector3 p)
        {
            var t=Group("Magnifier",parent).transform;t.localPosition=p;t.localRotation=Quaternion.Euler(0,-8,-22);
            Ring("Magnifying rim",t,Vector3.zero,.72f,.53f,.17f,Mat("Magnifier gold","FFBD48"));
            Rod("Magnifying handle",t,new Vector3(0,-.62f,0),new Vector3(0,-1.65f,0),.17f,Mat("Deep turquoise","087F86"));
            Rod("Lens reflection",t,new Vector3(-.3f,.1f,-.13f),new Vector3(-.1f,.36f,-.13f),.045f,Mat("Ivory","FFF8DF"));
        }
        static Transform Person(Transform parent,Vector3 p,bool clinician=true)
        {
            var t=Group(clinician?"Friendly clinician":"Child",parent).transform;t.localPosition=p;
            var skin=Mat(clinician?"Clinician skin":"Child skin",clinician?"BE805C":"F3B989");var hair=Mat("Warm brown hair","583C35");var ink=Mat("Ink","183D50");var coat=Mat("Ivory","FFF8DF");
            Ball("Rounded coat",t,new Vector3(0,1.1f,0),new Vector3(1.2f,1.7f,.74f),clinician?coat:Mat("Child coral","F38B72"));
            for(int side=-1;side<=1;side+=2){Rod("Trouser leg",t,new Vector3(side*.3f,.14f,0),new Vector3(side*.3f,.7f,0),.19f,Mat("Trousers","396A83"));Ball("Shoe",t,new Vector3(side*.3f,.1f,-.13f),new Vector3(.48f,.23f,.67f),ink);}
            Ball("Head",t,new Vector3(0,2.46f,-.05f),new Vector3(1.13f,1.28f,1.0f),skin);
            Ball("Hair cap",t,new Vector3(0,2.91f,.02f),new Vector3(1.16f,.57f,1.04f),hair);
            Ball("Hair curl",t,new Vector3(-.35f,2.78f,-.47f),new Vector3(.49f,.48f,.18f),hair);
            for(int side=-1;side<=1;side+=2){Ball("Eye",t,new Vector3(side*.22f,2.48f,-.53f),new Vector3(.11f,.15f,.07f),ink);Ball("Eye glint",t,new Vector3(side*.22f-.018f,2.51f,-.572f),Vector3.one*.035f,coat);Ball("Ear",t,new Vector3(side*.56f,2.43f,-.02f),new Vector3(.23f,.35f,.28f),skin);}
            Ball("Nose",t,new Vector3(0,2.35f,-.58f),new Vector3(.18f,.18f,.16f),skin);
            Rod("Smile left",t,new Vector3(-.15f,2.17f,-.5f),new Vector3(0,2.12f,-.54f),.025f,ink);Rod("Smile right",t,new Vector3(0,2.12f,-.54f),new Vector3(.15f,2.17f,-.5f),.025f,ink);
            Rod("Left sleeve",t,new Vector3(-.49f,1.65f,0),new Vector3(-.72f,.97f,-.08f),.2f,coat);Ball("Left hand",t,new Vector3(-.72f,.87f,-.08f),Vector3.one*.35f,skin);
            Rod("Pointing sleeve",t,new Vector3(.5f,1.65f,0),new Vector3(.95f,1.88f,-.05f),.19f,coat);Rod("Pointing hand",t,new Vector3(.95f,1.88f,-.05f),new Vector3(1.34f,2.05f,-.05f),.12f,skin);
            if(clinician){Rod("Coat seam",t,new Vector3(0,.5f,-.36f),new Vector3(0,1.7f,-.35f),.018f,Mat("Coat trim","86CFC6"));Box("Pocket",t,new Vector3(.3f,1.33f,-.37f),new Vector3(.28f,.27f,.045f),Mat("Coat trim","86CFC6"),.035f);Ring("Stethoscope",t,new Vector3(-.17f,1.62f,-.34f),.20f,.155f,.045f,ink);}
            return t;
        }
        static void Hand(Transform parent)
        {
            var skin=Mat("Child skin","F3B989");var t=Group("Hand holding bottle",parent).transform;
            Box("Palm",t,new Vector3(.65f,1.1f,-.65f),new Vector3(1.35f,1.35f,.45f),skin,.22f);
            Rod("Wrist",t,new Vector3(.85f,.8f,-.59f),new Vector3(1.15f,-.1f,-.5f),.35f,skin);
            for(int i=0;i<4;i++)Rod("Curved finger "+i,t,new Vector3(.62f,1.45f-i*.23f,-.9f),new Vector3(-.25f,1.45f-i*.23f,-.92f),.14f,skin);
            Rod("Thumb",t,new Vector3(.86f,1.14f,-.75f),new Vector3(.7f,1.95f,-.58f),.18f,skin);
        }
        static void Matching(Transform parent)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EyeCanDo/JourneyGarden/Prefabs/vision-testing.prefab");
            var symbols=source.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Picture - ")).Take(3).ToArray();
            for(int row=0;row<2;row++)for(int i=0;i<3;i++)
            {
                var t=Group("Matching picture tile",parent).transform;t.localPosition=new Vector3((i-1)*1.25f,2.7f-row*1.45f,0);
                Box("Tile",t,Vector3.zero,new Vector3(1.13f,1.15f,.25f),Mat("Ivory","FFF8DF"),.17f);
                var pic=Object.Instantiate(symbols[i].gameObject,t);pic.transform.localPosition=new Vector3(0,0,-.17f);pic.transform.localScale=Vector3.one*.76f;
                foreach(var r in pic.GetComponentsInChildren<Renderer>())r.sharedMaterial=Mat("Match "+i,new[]{"E88B35","19A9AD","A36BD1"}[i]);
            }
            for(int i=0;i<3;i++)Rod("Match line",parent,new Vector3((i-1)*1.25f,1.94f,.18f),new Vector3((i-1)*1.25f,2.0f,.18f),.08f,Mat("Match line","E5A237"));
        }
        public static Sprite Phase(int kind,int phase)
        {
            string id=new[]{"eye-drops","slit-lamp","vision-testing"}[kind];string name=id+"-"+((PreparationPhase)phase).ToString().ToLowerInvariant();
            var root=Group(name);
            if(phase==0){Equipment(kind,root.transform,new Vector3(.5f,0,.1f),.75f);Magnifier(root.transform,new Vector3(-1.1f,2.45f,-1));}
            else if(phase==1){Person(root.transform,new Vector3(-1.25f,0,-.1f));Equipment(kind,root.transform,new Vector3(1.15f,0,.3f),.62f);Play(root.transform,new Vector3(.5f,.55f,-1.25f),1.04f);}
            else if(kind==0){var b=Equipment(0,root.transform,new Vector3(-.35f,.65f,.1f),.72f);b.transform.localRotation=Quaternion.Euler(0,0,-13);Hand(root.transform);}
            else if(kind==1)
            {
                var station=Equipment(1,root.transform,Vector3.zero,.85f);
                // Head rests meet the chin and forehead; use the same equipment coordinate system.
                var art=station.transform.Find("Enlarged equipment art");
                var skin=Mat("Child skin","F3B989");
                Ball("Child head aligned in rests",art,new Vector3(1.12f,2.58f,-.72f),new Vector3(.65f,.85f,.64f),skin);
                Ball("Child hair",art,new Vector3(1.12f,2.88f,-.62f),new Vector3(.68f,.32f,.63f),Mat("Warm brown hair","583C35"));
                Ball("Child torso",art,new Vector3(1.35f,1.6f,-1.05f),new Vector3(.79f,1,.63f),Mat("Child coral","F38B72"));
                for(int i=0;i<2;i++)Ball("Child eye",art,new Vector3(.98f+i*.27f,2.63f,-1.025f),new Vector3(.075f,.1f,.04f),Mat("Ink","183D50"));
            }
            else Matching(root.transform);
            return SaveAndRender(root,name);
        }
        public static Sprite Icon(string name)
        {
            var root=Group(name);var t=root.transform;var cream=Mat("Ivory","FFF8DF");
            if(name=="Shop")
            {
                Box("Golden shopping bag",t,new Vector3(0,0,0),new Vector3(1.65f,1.7f,.6f),Mat("Bag gold","FFB82F"),.22f);
                Box("Bag front",t,new Vector3(0,-.08f,-.34f),new Vector3(1.42f,1.36f,.11f),Mat("Bag amber","FFCD60"),.15f);
                Ring("Bag handle",t,new Vector3(0,.84f,0),.48f,.32f,.17f,Mat("Bag gold","FFB82F"));
                Piece("Bag star",t,new Vector3(0,-.05f,-.43f),Mesh("Star",GardenGeometry.Star),cream);
            }
            else if(name=="Settings")
            {
                Ring("Ivory gear rim",t,Vector3.zero,.80f,.32f,.34f,cream);
                for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var tooth=Box("Rounded gear tooth",t,new Vector3(Mathf.Cos(a)*.88f,Mathf.Sin(a)*.88f,0),new Vector3(.46f,.45f,.36f),cream,.12f);tooth.transform.localRotation=Quaternion.Euler(0,0,i*45);}
            }
            else if(name=="Medallion")
            {var d=Disc("Porcelain medallion",t,Vector3.zero,1,.10f,Mat("Medallion white","FFFFFF"));d.transform.localRotation=Quaternion.Euler(90,0,0);Ring("Soft medallion rim",t,new Vector3(0,0,-.06f),1,.94f,.08f,Mat("Medallion white","FFFFFF"));}
            else if(name=="Eye")
            {Ball("Eye motif",t,Vector3.zero,new Vector3(2.5f,1.45f,.35f),Mat("Deep turquoise","087F86"));Ball("Eye white",t,new Vector3(0,0,-.22f),new Vector3(1.1f,1.17f,.12f),cream);Ball("Eye pupil",t,new Vector3(0,0,-.31f),new Vector3(.60f,.82f,.10f),Mat("Ink","183D50"));Ball("Eye sparkle",t,new Vector3(-.12f,.22f,-.38f),Vector3.one*.19f,cream);}
            else if(name=="Clock")
            {Ring("Clock rim",t,Vector3.zero,.8f,.66f,.16f,Mat("Deep turquoise","087F86"));var d=Disc("Clock face",t,Vector3.zero,.66f,.1f,cream);d.transform.localRotation=Quaternion.Euler(90,0,0);Rod("Clock minute",t,new Vector3(0,0,-.13f),new Vector3(0,.45f,-.13f),.055f,Mat("Ink","183D50"));Rod("Clock hour",t,new Vector3(0,0,-.13f),new Vector3(.3f,-.1f,-.13f),.055f,Mat("Ink","183D50"));}
            else Play(t,Vector3.zero,1);
            return SaveAndRender(root,name,true);
        }
        public static Sprite Platform(int kind)
        {
            var root=Group("Platform "+kind);var t=root.transform;
            Disc("Warm stone foot",t,new Vector3(0,-.2f,0),2.5f,.35f,Mat("Stone","D4B68C"));
            Disc("Coloured platform rim",t,Vector3.zero,2.48f,.35f,Mat("Rim "+kind,new[]{"0987AA","7651B7","DE8B28"}[kind]));
            Disc("Coloured platform top",t,new Vector3(0,.2f,0),2.40f,.14f,Mat("Top "+kind,new[]{"31C8D5","BB93E9","FFCD59"}[kind]));
            return SaveAndRender(root,"Platform-"+kind,false,true);
        }
        static Sprite SaveAndRender(GameObject root,string name,bool icon=false,bool platform=false)
        {
            string path=Root+"/Models/"+name+".prefab";PrefabUtility.SaveAsPrefabAsset(root,path);
            var preview=new PreviewRenderUtility();
            try
            {
                preview.AddSingleGO(root);var renderers=root.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                var cam=preview.camera;cam.orthographic=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;cam.allowHDR=false;
                Vector3 direction=icon?new Vector3(1.1f,1.1f,-12):platform?new Vector3(0,4,-14):new Vector3(1.2f,2.4f,-14);
                cam.transform.position=bounds.center+direction;cam.transform.LookAt(bounds.center);
                float maxX=0,maxY=0;
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var p=Quaternion.Inverse(cam.transform.rotation)*Vector3.Scale(bounds.extents,new Vector3(x,y,z));maxX=Mathf.Max(maxX,Mathf.Abs(p.x));maxY=Mathf.Max(maxY,Mathf.Abs(p.y));}
                int width=platform?1024:768,height=platform?384:768;cam.aspect=(float)width/height;cam.orthographicSize=Mathf.Max(maxY,maxX/cam.aspect)*1.10f;cam.nearClipPlane=.01f;cam.farClipPlane=100;
                preview.lights[0].intensity=1.35f;preview.lights[0].color=C("FFF0D7");preview.lights[0].transform.rotation=Quaternion.Euler(35,-25,0);
                preview.lights[1].intensity=.65f;preview.lights[1].color=C("DDF1FF");preview.lights[1].transform.rotation=Quaternion.Euler(315,120,0);preview.ambientColor=C("AFC8D0");
                preview.BeginPreview(new Rect(0,0,width,height),GUIStyle.none);preview.Render(true);var rendered=preview.EndPreview();
                var previous=RenderTexture.active;RenderTexture.active=(RenderTexture)rendered;var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();RenderTexture.active=previous;
                string png=Root+"/Illustrations/"+name+".png";File.WriteAllBytes(png,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(png);
                var importer=(TextureImporter)AssetImporter.GetAtPath(png);importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=1024;importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Sprite>(png);
            }
            finally{preview.Cleanup();}
        }
    }
}

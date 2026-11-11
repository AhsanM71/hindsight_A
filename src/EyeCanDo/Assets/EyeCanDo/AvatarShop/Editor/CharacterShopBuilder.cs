using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
using static EyeCanDo.Editor.CharacterArt;
namespace EyeCanDo.Editor
{
    public static class CharacterShopBuilder
    {
        public const string ShopPath=Root+"/Scenes/Shop.unity";
        static TMP_FontAsset font,bold;static Sprite round;
        [MenuItem("EyeCanDo/Character & Shop/Apply")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play and save the open scene before Apply.");
            foreach(var dir in new[]{"Materials","Meshes","Prefabs","Catalog","Thumbnails","Scenes"})Directory.CreateDirectory(Root+"/"+dir);AssetDatabase.Refresh();
            var catalog=BuildCatalog();var avatar=Avatar(catalog);AssignApp(catalog);
            Thumbnails(catalog,avatar);BuildShop(catalog,avatar);Integrate("Start",avatar);Integrate("JourneyMap",avatar);
            var scenes=EditorBuildSettings.scenes.Where(s=>s.path!=ShopPath).ToList();scenes.Add(new EditorBuildSettingsScene(ShopPath,true));EditorBuildSettings.scenes=scenes.ToArray();
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/EyeCanDo/Scenes/Start.unity");
            Debug.Log("Character & Shop applied: original avatar, catalogue, 200-coin migration, and Shop scene.");
        }
        static CosmeticCatalog BuildCatalog()
        {
            string path=Root+"/Catalog/Cosmetics.asset";var c=AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(path);if(c==null){c=ScriptableObject.CreateInstance<CosmeticCatalog>();AssetDatabase.CreateAsset(c,path);}
            var list=new List<CosmeticDefinition>();
            void Add(string id,string label,CosmeticCategory cat,CosmeticSlot slot,GameObject visual=null,int price=0,string color="FFFFFF",string secondary="FFFFFF")=>list.Add(new CosmeticDefinition{id=id,label=label,category=cat,slot=slot,visual=visual,price=price,defaultOwnership=price==0,color=C(color),secondary=C(secondary)});
            string[] faces={"Round","Oval","Freckles","Bright smile"};for(int i=0;i<4;i++)Add("face."+new[]{"round","oval","freckles","smile"}[i],faces[i],CosmeticCategory.Looks,CosmeticSlot.Face,Face(i));
            string[] skin={"F6D4B7","E5B58D","C99063","A66C48","784D35","4C302A"};for(int i=0;i<6;i++)Add("skin."+i,"Skin tone "+(i+1),CosmeticCategory.Looks,CosmeticSlot.Skin,color:skin[i]);
            string[] hair={"Short","Bob","Curls","Puffs","No hair"};for(int i=0;i<5;i++)Add("hair."+new[]{"short","bob","curls","puffs","none"}[i],hair[i],CosmeticCategory.Hair,CosmeticSlot.Hair,Hair(i));
            string[] colors={"322725","75482D","BD743F","DDB854","8B668E","C3BDB4"};string[] names={"Midnight","Chestnut","Copper","Honey","Plum","Silver"};for(int i=0;i<6;i++)Add("color."+i,names[i],CosmeticCategory.Hair,CosmeticSlot.HairColor,color:colors[i]);
            string[] shirts={"269F9C","DA7B53","8B68B4","D8AA40","4B88B7","C76B88"},shorts={"344F73","596A85","354E70","326D78","536877","61537B"},outfits={"Ocean","Sunset","Lavender","Sunshine","Sky","Berry"};
            for(int i=0;i<6;i++)Add("outfit."+i,outfits[i],CosmeticCategory.Clothes,CosmeticSlot.Outfit,color:shirts[i],secondary:shorts[i]);
            Add("hat.none","No hat",CosmeticCategory.Hats,CosmeticSlot.Hat);Add("hat.cap","Colourful cap",CosmeticCategory.Hats,CosmeticSlot.Hat,Hat(false),40);Add("hat.star","Star hat",CosmeticCategory.Hats,CosmeticSlot.Hat,Hat(true),60);
            Add("glasses.none","No glasses",CosmeticCategory.Extras,CosmeticSlot.Glasses);Add("glasses.fun","Fun glasses",CosmeticCategory.Extras,CosmeticSlot.Glasses,Glasses(),50);
            Add("backpack.none","No backpack",CosmeticCategory.Extras,CosmeticSlot.Backpack);Add("backpack.small","Small backpack",CosmeticCategory.Extras,CosmeticSlot.Backpack,Backpack(),80);
            // Preserve additional catalogue entries authored outside the built-in starter set.
            list.AddRange(c.items.Where(existing=>existing!=null&&!list.Any(item=>item.id==existing.id)));
            c.items=list.ToArray();EditorUtility.SetDirty(c);return c;
        }
        static void AssignApp(CosmeticCatalog catalog)
        {
            string path="Assets/EyeCanDo/Prefabs/AppRoot.prefab";var g=PrefabUtility.LoadPrefabContents(path);var so=new SerializedObject(g.GetComponent<AppRoot>());so.FindProperty("cosmeticCatalog").objectReferenceValue=catalog;so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(g,path);PrefabUtility.UnloadPrefabContents(g);
        }
        static CustomizationState Default(CosmeticCatalog catalog)
        {var s=new CustomizationState {version=1};foreach(CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot)))s.Set(slot,catalog.Default(slot));return s;}
        static void Thumbnails(CosmeticCatalog catalog,GameObject prefab)
        {
            var sceneLights=Object.FindObjectsByType<Light>().Where(l=>l.enabled).ToArray();foreach(var l in sceneLights)l.enabled=false;
            var ambientMode=RenderSettings.ambientMode;var ambientLight=RenderSettings.ambientLight;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=C("B7C6BF");
            var holder=Group("Thumbnail authoring");holder.transform.position=new Vector3(1000,1000,1000);
            var avatar=Object.Instantiate(prefab,holder.transform).GetComponent<AvatarView>();avatar.committed=false;
            var camera=Group("Authoring camera",holder.transform).AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.1f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=C("E9F3EC");camera.cullingMask=1<<30;camera.nearClipPlane=.1f;camera.farClipPlane=30;camera.enabled=false;camera.allowHDR=false;
            var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.antialiasing=AntialiasingMode.None;
            var light=Group("Thumbnail softbox",holder.transform).AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(32,-30,0);light.cullingMask=1<<30;
            var rt=new RenderTexture(320,320,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;var previous=RenderTexture.active;
            try
            {
                foreach(var item in catalog.items)
                {
                    string file=Root+"/Thumbnails/"+item.id+".png";
                    {
                        var state=Default(catalog);state.Set(item.slot,item.id);avatar.Apply(state);avatar.transform.localRotation=Quaternion.Euler(0,item.slot==CosmeticSlot.Backpack?155:0,0);
                        foreach(var t in holder.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                        bool face=item.category==CosmeticCategory.Looks||item.category==CosmeticCategory.Hair||item.slot==CosmeticSlot.Hat||item.slot==CosmeticSlot.Glasses;
                        camera.orthographicSize=face?1.18f:2.85f;camera.transform.localPosition=new Vector3(0,face?4.15f:2.55f,-12);camera.transform.localRotation=Quaternion.identity;
                        camera.Render();RenderTexture.active=rt;var tex=new Texture2D(320,320,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,320,320),0,0);tex.Apply();File.WriteAllBytes(file,tex.EncodeToPNG());Object.DestroyImmediate(tex);
                    }
                    AssetDatabase.ImportAsset(file);var importer=(TextureImporter)AssetImporter.GetAtPath(file);if(importer.textureType!=TextureImporterType.Sprite){importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=100;importer.mipmapEnabled=false;importer.SaveAndReimport();}item.thumbnail=AssetDatabase.LoadAssetAtPath<Sprite>(file);
                }
            }
            finally{camera.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(holder);foreach(var l in sceneLights)if(l!=null)l.enabled=true;RenderSettings.ambientMode=ambientMode;RenderSettings.ambientLight=ambientLight;}
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
        static void Integrate(string scene,GameObject prefab)
        {
            EditorSceneManager.OpenScene("Assets/EyeCanDo/Scenes/"+scene+".unity");bool start=scene=="Start";string name=start?"Start Avatar":"Map Avatar";
            var old=GameObject.Find(start?"Owl":name);if(old!=null)Object.DestroyImmediate(old);old=GameObject.Find(name);if(old!=null)Object.DestroyImmediate(old);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);g.name=name;g.transform.position=start?new Vector3(6.1f,.1f,-3):new Vector3(-4.4f,.1f,3.4f);g.transform.localScale=Vector3.one*(start?1.35f:.44f);
            var view=g.GetComponent<AvatarView>();view.waveOnEntry=start;view.Apply(Default(view.catalog));
            if(!start)
            {
                foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Shop Coming Soon").ToArray())Object.DestroyImmediate(t.gameObject);
                string snapshot=AdventureAudit.SnapshotMap();File.WriteAllText("Validation/CharacterShop/map-after.json",snapshot);
                if(snapshot!=File.ReadAllText("Validation/CharacterShop/map-before.json"))throw new InvalidOperationException("Map baseline changed outside the approved avatar/UI.");
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        public static RectTransform Rect(string name,Transform parent,Vector2 p,Vector2 size)
        {var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=p;r.sizeDelta=size;return r;}
        static UnityEngine.UI.Image Image(string name,Transform parent,Vector2 p,Vector2 size,Color color,Sprite sprite=null)
        {var r=Rect(name,parent,p,size);var i=r.gameObject.AddComponent<UnityEngine.UI.Image>();i.sprite=sprite??round;i.type=sprite==null?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;i.preserveAspect=sprite!=null;i.color=color;i.raycastTarget=false;return i;}
        static TMP_Text Text(string name,Transform parent,string value,float size,Vector2 p,Vector2 bounds,Color color,bool strong=false)
        {var r=Rect(name,parent,p,bounds);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=strong?bold:font;t.text=value;t.fontSize=size;t.color=color;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
        static UnityEngine.UI.Button Button(string name,Transform parent,Vector2 p,Vector2 size,Color color,string label="",float textSize=36)
        {
            var i=Image(name,parent,p,size,color);i.raycastTarget=true;var b=i.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=i;
            var c=b.colors;c.highlightedColor=C("E3F5EE");c.pressedColor=C("A7D7CE");c.disabledColor=new Color(.75f,.79f,.77f,.85f);b.colors=c;
            var outline=i.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(.13f,.29f,.32f,.13f);outline.effectDistance=new Vector2(2,-4);
            if(label.Length>0)Text("Label",b.transform,label,textSize,Vector2.zero,size-new Vector2(12,10),C("234B55"),true);return b;
        }
        static GameObject Checkmark(Transform parent,Vector2 position,float size)
        {
            var badge=Image("Check",parent,position,new Vector2(size,size),C("236D61"));
            var a=Image("Tick short",badge.transform,new Vector2(-size*.13f,-size*.05f),new Vector2(size*.33f,size*.10f),Color.white);a.sprite=null;a.transform.localRotation=Quaternion.Euler(0,0,-45);
            var b=Image("Tick long",badge.transform,new Vector2(size*.12f,size*.035f),new Vector2(size*.53f,size*.10f),Color.white);b.sprite=null;b.transform.localRotation=Quaternion.Euler(0,0,48);
            return badge.gameObject;
        }
        static void BuildShop(CosmeticCatalog catalog,GameObject prefab)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/EyeCanDo/UIAdventure/Fonts/Fredoka-Regular SDF.asset");bold=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/EyeCanDo/UIAdventure/Fonts/Fredoka-SemiBold SDF.asset");round=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EyeCanDo/JourneyGarden/UI/GardenRound.asset");
            var camera=Group("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=5.4f;camera.transform.position=new Vector3(0,2.7f,-15);camera.backgroundColor=C("EDF4E8");camera.clearFlags=CameraClearFlags.SolidColor;camera.gameObject.AddComponent<AudioListener>();camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var light=Group("Shop sunlight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(32,-28,0);light.shadows=LightShadows.Soft;light.shadowStrength=.3f;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=C("D5E8F0");RenderSettings.ambientEquatorColor=C("D4DED1");RenderSettings.ambientGroundColor=C("7D9691");
            var avatar=(GameObject)PrefabUtility.InstantiatePrefab(prefab);avatar.name="Shop Avatar";avatar.transform.position=new Vector3(-5.15f,.13f,0);avatar.transform.localScale=Vector3.one*1.12f;var view=avatar.GetComponent<AvatarView>();view.committed=false;view.Apply(Default(catalog));
            Ball("Display platform",null,new(-5.15f,.02f,0),new(4.8f,.30f,2.6f),"CBDCD0");Ball("Platform top",null,new(-5.15f,.11f,-.02f),new(4.5f,.18f,2.4f),"F9EDD2");
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EyeCanDo/Prefabs/AppRoot.prefab"));
            var e=Group("EventSystem");e.AddComponent<EventSystem>();e.AddComponent<InputSystemUIInputModule>();
            var root=Rect("Shop UI",null,Vector2.zero,Vector2.zero);var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();var scaler=root.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var safe=Rect("Safe area",root,Vector2.zero,Vector2.zero);safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.offsetMin=safe.offsetMax=Vector2.zero;safe.gameObject.AddComponent<SafeArea>();
            // Content scales uniformly inside the physical safe area, including wide notches.
            var content=Rect("Shop content",safe,Vector2.zero,new Vector2(1920,1080));content.gameObject.AddComponent<ShopSafeLayout>();
            Image("Choices panel",content,new Vector2(423,-8),new Vector2(990,1000),C("F8F8ED"));
            var shop=Group("Shop controller").AddComponent<ShopController>();shop.catalog=catalog;shop.avatar=view;
            shop.back=Button("Shop Back",content,new Vector2(-800,442),new Vector2(240,144),C("FFFFFF"),"‹  Back",38);
            Text("Shop title",content,"My little style shop",52,new Vector2(-320,440),new Vector2(670,100),C("234B55"),true);
            Image("Coin pill",content,new Vector2(765,440),new Vector2(285,132),C("F6D779"));Text("Coin icon",content,"●",60,new Vector2(678,442),new Vector2(76,90),C("9E671F"));shop.coins=Text("Coin balance",content,"200",48,new Vector2(790,440),new Vector2(155,90),C("503D28"),true);
            shop.categories=new UnityEngine.UI.Button[5];string[] names={"Looks","Hair","Clothes","Hats","Extras"},ids={"face.round","hair.curls","outfit.0","hat.cap","glasses.fun"};
            for(int i=0;i<5;i++)
            {
                var b=Button("Category "+names[i],content,new Vector2(42+i*187,286),new Vector2(174,158),Color.white);shop.categories[i]=b;
                Image("Category picture",b.transform,new Vector2(0,21),new Vector2(82,82),Color.white,catalog.Find(ids[i]).thumbnail);Text("Category label",b.transform,names[i],29,new Vector2(0,-49),new Vector2(162,42),C("234B55"),true);
            }
            shop.cards=new ShopCard[6];
            for(int i=0;i<6;i++)
            {
                var b=Button("Item "+i,content,new Vector2(105+(i%3)*310,86-(i/3)*258),new Vector2(286,238),Color.white);
                var picture=Image("Item picture",b.transform,new Vector2(0,30),new Vector2(190,140),Color.white,catalog.items[0].thumbnail);
                var title=Text("Item title",b.transform,"",30,new Vector2(0,-59),new Vector2(272,44),C("234B55"),true);var price=Text("Item ownership",b.transform,"",26,new Vector2(0,-96),new Vector2(272,32),C("52686A"));
                shop.cards[i]=new ShopCard {button=b,image=picture,background=b.GetComponent<UnityEngine.UI.Image>(),title=title,price=price,check=Checkmark(b.transform,new Vector2(-107,-94),28)};
            }
            shop.previous=Button("Previous page",content,new Vector2(41,-454),new Vector2(144,144),Color.white,"‹",65);shop.next=Button("Next page",content,new Vector2(789,-454),new Vector2(144,144),Color.white,"›",65);shop.pageLabel=Text("Page",content,"1 / 1",28,new Vector2(415,-492),new Vector2(150,40),C("52686A"));
            shop.swatchLabel=Text("Swatch label",content,"Skin tone",27,new Vector2(415,-324),new Vector2(350,40),C("52686A"));shop.swatches=new UnityEngine.UI.Button[6];
            // Swatches sit above pagination. Both are full 128-unit touch targets.
            for(int i=0;i<6;i++)
            {
                var b=Button("Swatch "+i,content,new Vector2(52+i*153,-414),new Vector2(144,144),Color.white);shop.swatches[i]=b;
                Image("Colour",b.transform,Vector2.zero,new Vector2(102,102),Color.white);
                Checkmark(b.transform,Vector2.zero,44);
            }
            // Keep arrows clear of colour swatches; pagination is visible only for multi-page categories.
            shop.previous.transform.localPosition=new Vector3(100,440,0);shop.next.transform.localPosition=new Vector3(470,440,0);shop.pageLabel.transform.localPosition=new Vector3(285,440,0);
            shop.selectionLabel=Text("Selected item",content,"Make it yours",43,new Vector2(-520,-300),new Vector2(650,70),C("234B55"),true);
            shop.rotateLeft=Button("Rotate left",content,new Vector2(-810,-195),new Vector2(144,144),Color.white,"‹",60);shop.rotateRight=Button("Rotate right",content,new Vector2(-230,-195),new Vector2(144,144),Color.white,"›",60);
            shop.action=Button("Shop action",content,new Vector2(-520,-405),new Vector2(550,140),C("399F82"));shop.actionLabel=Text("Action label",shop.action.transform,"Choose an item",43,Vector2.zero,new Vector2(525,120),Color.white,true);
            shop.actionCheck=Checkmark(shop.action.transform,new Vector2(-170,0),44);shop.actionCheck.SetActive(false);
            shop.hint=Text("Shop hint",content,"Pick a look to try it on",27,new Vector2(-520,-507),new Vector2(650,42),C("52686A"));
            shop.sparkle=Rect("Purchase sparkles",content,new Vector2(-520,20),new Vector2(600,570));for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var sp=Rect("Sparkle",shop.sparkle,new Vector2(Mathf.Cos(a)*260,Mathf.Sin(a)*245),new Vector2(64,64));var one=Image("Ray",sp,Vector2.zero,new Vector2(14,54),C("CA9340"));one.transform.localRotation=Quaternion.Euler(0,0,35);var two=Image("Ray",sp,Vector2.zero,new Vector2(14,38),C("CA9340"));two.transform.localRotation=Quaternion.Euler(0,0,125);}shop.sparkle.gameObject.SetActive(false);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ShopPath);
        }
    }
}

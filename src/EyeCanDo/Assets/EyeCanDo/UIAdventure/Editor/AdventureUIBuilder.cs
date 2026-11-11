using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
using static EyeCanDo.Editor.AdventureArt;

namespace EyeCanDo.Editor
{
    public static class AdventureUIBuilder
    {
        const string Scenes="Assets/EyeCanDo/Scenes/";
        static TMP_FontAsset regular, semibold;
        static Sprite round;
        static readonly string[] ids={"eye-drops","slit-lamp","vision-testing"};
        [MenuItem("EyeCanDo/UI/Apply Adventure UI (Iteration 3)")]
        public static void Apply()
        {
            Prepare();ApplyStart();ApplyMap();ApplySelection(0);ApplySelection(1);ApplySelection(2);
            EditorSceneManager.OpenScene(Scenes+"Start.unity");
            Debug.Log("Adventure UI iteration 3 authored and saved.");
        }
        public static void Prepare()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before authoring.");
            if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the open scene before authoring.");
            foreach(string dir in new[]{"Fonts","Materials","Meshes","Prefabs","Models","Illustrations","Themes","Lighting"})Directory.CreateDirectory(Root+"/"+dir);
            AssetDatabase.Refresh();LoadFonts();
            EditorSceneManager.OpenScene(Scenes+"JourneyMap.unity");AdventureAudit.Baseline();
            SaveSource("Storybook garden","Garden source");SaveSource("Scenic sky backdrop","Sky source");
            string lighting=Root+"/Lighting/Adventure sunlight.asset";
            if(!File.Exists(lighting))AssetDatabase.CopyAsset("Assets/EyeCanDo/JourneyGarden/Lighting/GardenSunlight.asset",lighting);
            foreach(string icon in new[]{"Shop","Settings","Eye","Clock","Medallion"})if(!File.Exists(Root+"/Illustrations/"+icon+".png"))Icon(icon);
            BuildIconPrefab();BuildDestinationPrefab();AssetDatabase.SaveAssets();
        }
        static void SaveSource(string name,string target)
        {
            string path=Root+"/Prefabs/"+target+".prefab";if(File.Exists(path))return;
            var clone=Object.Instantiate(GameObject.Find(name));clone.name=target;
            var backdrop=clone.GetComponent<GardenBackdrop>();if(backdrop!=null){Object.DestroyImmediate(backdrop);clone.AddComponent<AdventureBackdrop>();}
            Typography(clone);PrefabUtility.SaveAsPrefabAsset(clone,path);Object.DestroyImmediate(clone);
        }
        static void LoadFonts()
        {
            regular=Font("Regular");semibold=Font("SemiBold");
            round=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EyeCanDo/JourneyGarden/UI/GardenRound.asset");
        }
        static TMP_FontAsset Font(string style)
        {
            string path=Root+"/Fonts/Fredoka-"+style+" SDF.asset";var f=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(f!=null)return f;
            var source=AssetDatabase.LoadAssetAtPath<UnityEngine.Font>(Root+"/Fonts/Fredoka-"+style+".ttf");
            if(source==null)throw new InvalidOperationException("Missing static Fredoka "+style);
            f=TMP_FontAsset.CreateFontAsset(source,90,9,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);f.name="Fredoka "+style+" SDF";
            f.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>{AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset")};
            f.TryAddCharacters(string.Concat(Enumerable.Range(32,560).Select(i=>(char)i)),out _);
            AssetDatabase.CreateAsset(f,path);f.material.name=f.name+" Material";AssetDatabase.AddObjectToAsset(f.material,f);
            foreach(var texture in f.atlasTextures){texture.name=f.name+" Atlas";AssetDatabase.AddObjectToAsset(texture,f);}
            EditorUtility.SetDirty(f);return f;
        }
        static void Typography(GameObject root)
        {
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                bool bold=(text.fontStyle&FontStyles.Bold)!=0 || text.font==semibold;
                text.font=bold?semibold:regular;text.fontStyle&=~FontStyles.Bold;
                // Use the actual semibold face rather than synthetic emboldening.
                if(text.GetComponentInParent<TMP_InputField>()!=null && text.name=="Username text")
                {text.enableAutoSizing=true;text.fontSizeMax=37;text.fontSizeMin=24;}
                EditorUtility.SetDirty(text);
            }
        }
        static void TypographyScene(){foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())Typography(root);}
        static void Set(Object target,string property,Object value)
        {var s=new SerializedObject(target);s.FindProperty(property).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        static RectTransform Rect(string name,Transform parent,Vector2 p,Vector2 size)
        {var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=p;r.sizeDelta=size;return r;}
        static void Position(Transform t,Vector2 p,Vector2 size)
        {var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=p;r.sizeDelta=size;}
        static void Corner(Transform t,Vector2 corner,Vector2 p,Vector2 size)
        {var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=corner;r.anchoredPosition=p;r.sizeDelta=size;}
        static UnityEngine.UI.Image Image(string name,Transform parent,Vector2 p,Vector2 size,Color color,Sprite sprite=null)
        {var r=Rect(name,parent,p,size);var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=sprite??round;image.type=sprite==null?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;image.color=color;image.raycastTarget=false;image.preserveAspect=sprite!=null;return image;}
        static TMP_Text Text(string name,Transform parent,string content,float size,Vector2 p,Vector2 bounds,Color color,bool bold=false)
        {var r=Rect(name,parent,p,bounds);var text=r.gameObject.AddComponent<TextMeshProUGUI>();text.font=bold?semibold:regular;text.text=content;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.color=color;text.raycastTarget=false;text.enableAutoSizing=true;text.fontSizeMin=size*.85f;text.fontSizeMax=size;return text;}
        static void Raise(UnityEngine.UI.Image image,Color shadow,float depth=9)
        {
            var s=image.GetComponents<UnityEngine.UI.Shadow>().FirstOrDefault(c=>c.GetType()==typeof(UnityEngine.UI.Shadow))??image.gameObject.AddComponent<UnityEngine.UI.Shadow>();s.effectColor=shadow;s.effectDistance=new Vector2(0,-depth);
            var o=image.GetComponent<UnityEngine.UI.Outline>()??image.gameObject.AddComponent<UnityEngine.UI.Outline>();o.effectColor=new Color(1,1,1,.86f);o.effectDistance=new Vector2(2,-2);
        }
        static UnityEngine.UI.Button Button(UnityEngine.UI.Image image)
        {
            image.raycastTarget=true;var b=image.gameObject.GetComponent<UnityEngine.UI.Button>()??image.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;
            var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=C("EFFFF7");colors.pressedColor=C("BDE8D6");colors.fadeDuration=.1f;b.colors=colors;
            if(image.GetComponent<ButtonFeedback>()==null)image.gameObject.AddComponent<ButtonFeedback>();return b;
        }
        static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Illustrations/"+name+".png");
        static void BuildIconPrefab()
        {
            var image=Image("Adventure icon button",null,Vector2.zero,new Vector2(160,160),C("9664CD"));Button(image);Raise(image,C("69449A"));
            var icon=Image("Decorative icon",image.transform,Vector2.zero,new Vector2(125,125),Color.white,Sprite("Shop"));icon.gameObject.AddComponent<AdventureIconMotion>();
            PrefabUtility.SaveAsPrefabAsset(image.gameObject,Root+"/Prefabs/AdventureIconButton.prefab");Object.DestroyImmediate(image.gameObject);
        }
        static void BuildDestinationPrefab()
        {
            var root=Image("Adventure destination",null,Vector2.zero,new Vector2(475,550),Color.clear);var button=Button(root);
            var medallion=Image("Pictorial medallion",root.transform,new Vector2(0,80),new Vector2(378,378),C("C3F3ED"),Sprite("Medallion"));
            var illustration=Image("Phase illustration",root.transform,new Vector2(0,94),new Vector2(385,385),Color.white);
            var motion=illustration.gameObject.AddComponent<AdventureIconMotion>();motion.bob=3;motion.angle=.6f;motion.speed=.7f;
            var platform=Image("Raised platform",root.transform,new Vector2(0,-125),new Vector2(475,180),Color.white);
            var plaque=Image("Phase plaque",root.transform,new Vector2(0,-122),new Vector2(335,74),C("FFFAE9"));Raise(plaque,new Color(.1f,.25f,.26f,.18f),4);
            var phase=Text("Phase",root.transform,"Discover",49,new Vector2(0,-121),new Vector2(320,70),C("163D50"),true);
            var hiddenTitle=Text("Activity",root.transform,"",26,Vector2.zero,new Vector2(1,1),Color.clear);hiddenTitle.gameObject.SetActive(false);
            var badgeImage=Image("Availability",root.transform,new Vector2(0,-242),new Vector2(310,132),C("FFF7DB"));Raise(badgeImage,new Color(.15f,.23f,.22f,.22f),5);var badge=Button(badgeImage);
            Image("Clock",badge.transform,new Vector2(-103,0),new Vector2(54,54),Color.white,Sprite("Clock"));
            var state=Text("State",badge.transform,"Coming soon",29,new Vector2(27,0),new Vector2(223,56),C("345660"));
            var card=root.gameObject.AddComponent<SublevelCard>();Set(card,"phase",phase);Set(card,"title",hiddenTitle);Set(card,"state",state);Set(card,"icon",illustration);Set(card,"accent",medallion);Set(card,"button",button);
            var view=root.gameObject.AddComponent<AdventureDestinationView>();view.illustration=illustration;view.medallion=medallion;view.platform=platform;view.phase=phase;view.badge=badge;
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root+"/Prefabs/AdventureDestination.prefab");Object.DestroyImmediate(root.gameObject);
        }
        static void Save(){TypographyScene();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());}
        static void Remove(Transform root,string name){var t=root.Find(name);if(t!=null)Object.DestroyImmediate(t.gameObject);}
        static void Lighting(Camera camera)
        {
            var volume=Object.FindAnyObjectByType<Volume>();volume.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/Lighting/Adventure sunlight.asset");
            var sun=Object.FindAnyObjectByType<Light>();sun.intensity=1.35f;sun.color=C("FFF1DC");sun.shadowStrength=.55f;sun.transform.rotation=Quaternion.Euler(45,-28,0);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=C("BDDDF2");RenderSettings.ambientEquatorColor=C("A2C6CB");RenderSettings.ambientGroundColor=C("688B63");
            camera.backgroundColor=C("5ABDEB");
        }
        static GameObject Instantiate(string name,Transform parent=null)
        {var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+name+".prefab"));g.transform.SetParent(parent,false);g.name=name;return g;}
        static void World(string name)
        {
            var old=GameObject.Find(name);if(old!=null)old.SetActive(false);
            var existing=GameObject.Find("Adventure scenery");if(existing!=null)Object.DestroyImmediate(existing);
            var root=Group("Adventure scenery");Instantiate("Garden source",root.transform);Instantiate("Sky source",root.transform);
        }
        public static void ApplyStart()
        {
            LoadFonts();EditorSceneManager.OpenScene(Scenes+"Start.unity");World("Welcome garden");
            var camera=Camera.main;camera.transform.rotation=Quaternion.Euler(20,0,0);var fit=camera.GetComponent<AdventureCameraFit>()??camera.gameObject.AddComponent<AdventureCameraFit>();fit.referenceSize=7.8f;fit.focus=new Vector3(0,2.5f,2);fit.Fit();Lighting(camera);
            var owl=GameObject.Find("Owl");if(owl!=null){owl.transform.position=new Vector3(6.1f,.1f,-3.0f);owl.transform.localScale=Vector3.one*1.7f;}
            var safe=GameObject.Find("Screen UI").transform.Find("Safe area");var welcome=safe.Find("Welcome");var card=welcome.Find("Welcome card");
            if(card!=null)
            {
                foreach(string child in new[]{"EyeCanDo","Start button"})card.Find(child).SetParent(welcome,false);
                Object.DestroyImmediate(card.gameObject);
            }
            Remove(safe,"Mascot greeting");
            var title=welcome.Find("EyeCanDo").GetComponent<TMP_Text>();Position(title.transform,new Vector2(-440,112),new Vector2(800,165));title.font=semibold;title.fontSize=132;title.fontSizeMax=132;title.fontSizeMin=120;title.color=C("164F57");
            var outline=title.GetComponent<UnityEngine.UI.Outline>()??title.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=C("FFF9DD");outline.effectDistance=new Vector2(3,-3);
            Remove(welcome,"Eye motif");Image("Eye motif",welcome,new Vector2(-440,290),new Vector2(210,135),Color.white,Sprite("Eye"));
            var start=welcome.Find("Start button");Position(start,new Vector2(-440,-87),new Vector2(540,156));var image=start.GetComponent<UnityEngine.UI.Image>();image.sprite=round;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=C("3AAB63");Raise(image,C("217547"),12);
            var label=start.Find("Label").GetComponent<TMP_Text>();Position(label.transform,new Vector2(55,3),new Vector2(300,120));label.font=semibold;label.fontSize=69;label.fontSizeMax=69;
            Position(start.Find("Play icon"),new Vector2(-153,4),new Vector2(88,88));
            var play=start.Find("Play icon");if(play.GetComponent<AdventureIconMotion>()==null){var motion=play.gameObject.AddComponent<AdventureIconMotion>();motion.bob=2;motion.angle=0;}
            Save();
        }
        public static void ApplyMap()
        {
            LoadFonts();EditorSceneManager.OpenScene(Scenes+"JourneyMap.unity");
            var controls=GameObject.Find("Top right controls").transform;Corner(controls,Vector2.one,new Vector2(-36,-32),new Vector2(344,160));
            for(int i=0;i<2;i++)
            {
                string name=i==0?"Shop":"Settings";var button=controls.Find(name);
                var template=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/AdventureIconButton.prefab");
                if(PrefabUtility.GetCorrespondingObjectFromSource(button.gameObject)!=template)
                {
                    int order=button.GetSiblingIndex();var callbacks=button.GetComponent<UnityEngine.UI.Button>().onClick;
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(template,controls);instance.name=name;
                    var newButton=instance.GetComponent<UnityEngine.UI.Button>();newButton.onClick=callbacks;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(newButton);
                    Object.DestroyImmediate(button.gameObject);button=instance.transform;button.SetSiblingIndex(order);
                }
                Position(button,new Vector2(i==0?-92:92,0),new Vector2(160,160));
                var image=button.GetComponent<UnityEngine.UI.Image>();image.sprite=round;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=C(i==0?"9561CB":"28B7BD");Raise(image,C(i==0?"684396":"15818E"),9);
                foreach(var t in button.GetComponentsInChildren<TMP_Text>(true))t.gameObject.SetActive(false);
                var icon=button.Find("Decorative icon")??button.Find(name+" icon");Position(icon,Vector2.zero,new Vector2(128,128));icon.GetComponent<UnityEngine.UI.Image>().sprite=Sprite(name);
                var motion=icon.GetComponent<AdventureIconMotion>()??icon.gameObject.AddComponent<AdventureIconMotion>();motion.bob=i==0?2.5f:0;motion.angle=i==0?1.5f:3;
            }
            var controller=Object.FindAnyObjectByType<JourneyMapController>();
            foreach(string field in new[]{"settingsPanel"})
            {
                var panel=(GameObject)new SerializedObject(controller).FindProperty(field).objectReferenceValue;
                var rect=(RectTransform)panel.transform;rect.SetParent(GameObject.Find("Screen UI").transform,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;rect.SetAsLastSibling();
            }
            var greeting=(TMP_Text)new SerializedObject(controller).FindProperty("greeting").objectReferenceValue;
            var header=(RectTransform)greeting.transform.parent;header.sizeDelta=new Vector2(660,124);
            Position(header.Find("Brand eye"),new Vector2(-285,12),new Vector2(60,39));
            Position(greeting.transform,new Vector2(39,-29),new Vector2(550,48));greeting.fontSizeMin=24;greeting.fontSizeMax=30;
            Position(header.Find("EyeCanDo brand"),new Vector2(39,18),new Vector2(550,50));
            Save();AdventureAudit.Compare();
        }
        public static void ApplySelection(int kind)
        {
            LoadFonts();EditorSceneManager.OpenScene(Scenes+"JourneySelection.unity");
            var theme=BuildTheme(kind);
            var controller=Object.FindAnyObjectByType<JourneySelectionController>();var presenter=controller.GetComponent<AdventureSelectionPresenter>()??controller.gameObject.AddComponent<AdventureSelectionPresenter>();
            presenter.themes=ids.Select(id=>AssetDatabase.LoadAssetAtPath<JourneyPresentationTheme>(Root+"/Themes/"+id+".asset")).ToArray();
            var old=GameObject.Find("Journey garden");if(old!=null)old.SetActive(false);
            var scenery=GameObject.Find("Adventure courtyard");if(scenery==null)scenery=Group("Adventure courtyard");presenter.courtyardRoot=scenery.transform;
            var sky=GameObject.Find("Sky source");if(sky==null)Instantiate("Sky source");
            var camera=Camera.main;camera.transform.rotation=Quaternion.Euler(26,0,0);var fit=camera.GetComponent<AdventureCameraFit>()??camera.gameObject.AddComponent<AdventureCameraFit>();fit.referenceSize=8.7f;fit.focus=new Vector3(0,2.25f,2);fit.Fit();Lighting(camera);
            var safe=GameObject.Find("Screen UI").transform.Find("Safe area");
            foreach(string name in new[]{"Soft scene wash","Journey invitation","Sublevel pathway","Journey footer","Path arrow"})
            {while(safe.Find(name)!=null)Remove(safe,name);}
            var cards=safe.Find("Sublevels");var layout=cards.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();if(layout!=null)Object.DestroyImmediate(layout);
            for(int i=cards.childCount-1;i>=0;i--)Object.DestroyImmediate(cards.GetChild(i).gameObject);
            Position(cards,new Vector2(0,-118),new Vector2(1640,550));
            Set(controller,"cardPrefab",AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/AdventureDestination.prefab").GetComponent<SublevelCard>());
            var heading=safe.Find("Journey heading").GetComponent<TMP_Text>();Corner(heading.transform,new Vector2(.5f,1),new Vector2(0,-46),new Vector2(1300,110));heading.font=semibold;heading.fontSize=72;heading.fontSizeMax=72;heading.color=C("163E50");
            var shadow=heading.GetComponent<UnityEngine.UI.Shadow>()??heading.gameObject.AddComponent<UnityEngine.UI.Shadow>();shadow.effectColor=C("F5FFF3");shadow.effectDistance=new Vector2(2,-3);
            safe.Find("Procedure icon").gameObject.SetActive(false);
            var back=safe.Find("Back to map");Corner(back,new Vector2(0,1),new Vector2(36,-32),new Vector2(160,160));back.GetComponent<UnityEngine.UI.Image>().sprite=round;back.GetComponent<UnityEngine.UI.Image>().color=C("FFF8DF");Raise(back.GetComponent<UnityEngine.UI.Image>(),new Color(.1f,.3f,.3f,.25f),8);
            Position(back.Find("Back to map icon"),new Vector2(0,16),new Vector2(84,84));var caption=back.Find("Caregiver label").GetComponent<TMP_Text>();Position(caption.transform,new Vector2(0,-48),new Vector2(140,44));caption.fontSize=28;caption.fontSizeMax=28;
            // Full-screen modal blockers cover unsafe edges as well as the content area.
            var modal=(GameObject)new SerializedObject(controller).FindProperty("comingSoonPanel").objectReferenceValue;
            var modalRect=(RectTransform)modal.transform;modalRect.SetParent(safe.parent,false);modalRect.anchorMin=Vector2.zero;modalRect.anchorMax=Vector2.one;modalRect.offsetMin=modalRect.offsetMax=Vector2.zero;modalRect.SetAsLastSibling();
            Save();
        }
        static JourneyPresentationTheme BuildTheme(int kind)
        {
            var root=Group(ids[kind]+" courtyard");var garden=Instantiate("Garden source",root.transform);
            var clinic=garden.GetComponentsInChildren<Transform>().First(t=>t.name=="StorybookClinic" || t.name=="Whimsical eye clinic");clinic.localPosition=new Vector3(7.1f,.12f,8.8f);clinic.localScale=Vector3.one*.70f;
            foreach(string name in new[]{"Welcome path","Welcome path edging","Clinic path","Clinic path edging"})Remove(garden.transform,name);
            var lawn=garden.transform.Find("Gently raised lawn").GetComponent<Renderer>();lawn.sharedMaterial=Mat("Courtyard lawn "+kind,new[]{"91CDB7","B5BFCA","C9D88A"}[kind]);
            var path=GardenPathMesh.Create(new Vector3(-13,.13f,-4),new Vector3(-6,.13f,-1),new Vector3(6,.13f,-1),new Vector3(13,.13f,-4),3.2f);
            var saved=Mesh("Courtyard curved path",()=>path);if(saved!=path)Object.DestroyImmediate(path);
            Piece("Broad curved courtyard path",root.transform,Vector3.zero,saved,Mat("Courtyard path "+kind,new[]{"DAEFD5","E9DFF2","F9E3A7"}[kind]));
            Disc("Hero terrace",root.transform,new Vector3(-1.2f,.18f,12),2.2f,.32f,Mat("Hero terrace "+kind,new[]{"37BCCC","AE8ADD","F9C14B"}[kind]));
            var hero=Equipment(kind,root.transform,new Vector3(-1.2f,.4f,12),.70f);hero.AddComponent<GardenMotion>().bob=.025f;
            Typography(root);string courtyardPath=Root+"/Prefabs/"+ids[kind]+" courtyard.prefab";var prefab=PrefabUtility.SaveAsPrefabAsset(root,courtyardPath);Object.DestroyImmediate(root);
            string pathTheme=Root+"/Themes/"+ids[kind]+".asset";var theme=AssetDatabase.LoadAssetAtPath<JourneyPresentationTheme>(pathTheme);if(theme==null){theme=ScriptableObject.CreateInstance<JourneyPresentationTheme>();AssetDatabase.CreateAsset(theme,pathTheme);}
            theme.journeyId=ids[kind];theme.courtyardPrefab=prefab;theme.accent=C(new[]{"21BACB","AD85D9","F3BC43"}[kind]);theme.pale=C(new[]{"D4F6ED","EEE0FC","FFF1C2"}[kind]);theme.ink=C("163D50");theme.platform=Platform(kind);
            theme.phaseIllustrations=Enumerable.Range(0,3).Select(p=>Phase(kind,p)).ToArray();EditorUtility.SetDirty(theme);return theme;
        }
    }
}

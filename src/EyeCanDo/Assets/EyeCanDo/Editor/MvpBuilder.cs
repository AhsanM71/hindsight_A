using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using static EyeCanDo.Editor.MvpArt;

namespace EyeCanDo.Editor
{
    public static class MvpBuilder
    {
        static TMP_FontAsset font;
        static JourneyCatalog catalog;
        static GameObject owlPrefab, appPrefab;
        static SublevelCard cardPrefab;
        const string SceneFolder = Root + "/Scenes";

        [MenuItem("EyeCanDo/1. Prepare Text Resources")]
        public static void PrepareTypography()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length == 0)
                TMP_PackageResourceImporter.ImportResources(true, false, false);
        }

        [MenuItem("EyeCanDo/2. Generate MVP Scenes and Assets")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before building scenes.");
            foreach (var folder in new[] { "Art/Materials", "Art/Icons", "Prefabs", "Data", "Scenes", "Tests/Results" }) Folder($"{Root}/{folder}");
            AssetDatabase.Refresh();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) throw new InvalidOperationException("Run EyeCanDo / Prepare Text Resources first, then retry.");
            foreach (var name in new[] { "Round", "Gear", "Bag", "Back", "Play", "Drop", "Lamp", "Chart", "Discover", "Watch", "Practice", "Soon" }) Icon(name);
            owlPrefab = SavePrefab(Owl(), "Owl");
            BuildDefinitions();
            appPrefab = BuildApp();
            cardPrefab = BuildCard();
            BuildStart();
            BuildMap();
            BuildJourney();
            EditorBuildSettings.scenes = new[] { "Start", "JourneyMap", "JourneySelection" }
                .Select(x => new EditorBuildSettingsScene($"{SceneFolder}/{x}.unity", true)).ToArray();
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene($"{SceneFolder}/Start.unity");
            Debug.Log("EyeCanDo MVP: three scenes, three journeys and nine sublevels generated and saved.");
        }

        public static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "EyeCanDo";
            PlayerSettings.productName = "EyeCanDo";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            // Use the shared Editor start scene even when a content scene is being edited.
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>($"{SceneFolder}/Start.unity");
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler");
            if (input != null) { input.intValue = 1; settings.ApplyModifiedPropertiesWithoutUndo(); }
        }

        static void BuildDefinitions()
        {
            catalog = LoadOrCreate<JourneyCatalog>($"{Root}/Data/JourneyCatalog.asset");
            var ids = new[] { "eye-drops", "slit-lamp", "vision-testing" };
            var titles = new[] { "Eye-Drop Preparation", "Slit-Lamp Preparation", "Vision Testing" };
            var shortTitles = new[] { "Eye drops", "Slit lamp", "Vision test" };
            var icons = new[] { "Drop", "Lamp", "Chart" };
            var colors = new[] { Teal, Purple, Gold };
            var labels = new[] {
                new[] { "Discover the Eye Drops", "Watch the Doctor", "Practice Eye Drops" },
                new[] { "Discover the Slit Lamp", "Watch the Examination", "Practice Positioning" },
                new[] { "Discover the Vision Chart", "Watch a Vision Test", "Practice Picture Matching" }
            };
            catalog.journeys = new JourneyDefinition[3];
            for (int i = 0; i < 3; i++)
            {
                var definition = LoadOrCreate<JourneyDefinition>($"{Root}/Data/{ids[i]}.asset");
                definition.id = ids[i]; definition.title = titles[i]; definition.shortTitle = shortTitles[i];
                definition.accent = colors[i]; definition.icon = Icon(icons[i]);
                definition.sublevels = new SublevelDefinition[3];
                for (int p = 0; p < 3; p++) definition.sublevels[p] = new SublevelDefinition {
                    id = ids[i] + "." + ((PreparationPhase)p).ToString().ToLowerInvariant(), phase = (PreparationPhase)p,
                    title = labels[i][p], icon = Icon(((PreparationPhase)p).ToString()), isPlayable = false, scenePath = string.Empty
                };
                definition.destinationPrefab = BuildDestination(definition, i);
                catalog.journeys[i] = definition;
                EditorUtility.SetDirty(definition);
            }
            EditorUtility.SetDirty(catalog);
        }

        static GameObject BuildDestination(JourneyDefinition definition, int kind)
        {
            var go = Group(definition.id);
            var t = go.transform;
            Disc("Cream island", t, new(0, .12f, 0), new(5.4f, .18f, 4.5f), Cream);
            Disc("Coloured plinth", t, new(0, .31f, 0), new(4.5f, .12f, 3.8f), definition.accent);
            var equipment = Equipment(kind);
            equipment.transform.SetParent(t, false); equipment.transform.localPosition = new(0, .45f, 0);
            equipment.AddComponent<GentleIdle>().amplitude = .045f;
            var destination = go.AddComponent<JourneyDestination>(); destination.definition = definition; destination.equipment = equipment.transform;
            var collider = go.AddComponent<BoxCollider>(); collider.center = new(0, 2, 0); collider.size = new(4.7f, 4.2f, 3.5f);
            var badge = UI("Journey badge", t); badge.sizeDelta = new(480, 148); badge.localScale = Vector3.one * .01f;
            badge.localPosition = new(0, .55f, -2.35f); badge.localRotation = Quaternion.Euler(34, 0, 0);
            var canvas = badge.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            badge.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var button = MakeButton("Open " + definition.shortTitle, badge, definition.shortTitle, Vector2.zero, new(480, 148), Cream, Ink, destination.Open, 43);
            Image("Pictogram", button.transform, definition.icon, new(-175, 0), new(94, 94), Color.white);
            var label = button.GetComponentInChildren<TMP_Text>().rectTransform;
            label.offsetMin = new(120, 14); label.offsetMax = new(-28, -14);
            return SavePrefab(go, definition.id);
        }

        static GameObject BuildApp()
        {
            var go = Group("EyeCanDo App");
            var app = go.AddComponent<AppRoot>();
            Set(app, "catalog", catalog);
            var canvas = UI("Transition", go.transform);
            var c = canvas.gameObject.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 1000;
            canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var image = canvas.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = Cream;
            var group = canvas.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false;
            Set(go.GetComponent<SceneNavigator>(), "fade", group);
            return SavePrefab(go, "AppRoot");
        }

        static SublevelCard BuildCard()
        {
            var root = UI("Sublevel card", null); root.sizeDelta = new(395, 495);
            var image = Round(root.gameObject, Cream);
            image.raycastTarget = true;
            var button = root.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            button.colors = ButtonColors(Cream); root.gameObject.AddComponent<ButtonFeedback>();
            var layout = root.gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); layout.preferredWidth = 395; layout.preferredHeight = 495; layout.flexibleWidth = 1;
            var accent = Image("Icon circle", root, Icon("Round"), new(0, 83), new(218, 218), Teal);
            var icon = Image("Phase icon", root, Icon("Discover"), new(0, 83), new(170, 170), Color.white);
            var phase = Text("Phase", root, "1  Discover", 42, new(0, -61), new(350, 64)); phase.fontStyle = FontStyles.Bold;
            var title = Text("Activity", root, "Discover the Eye Drops", 28, new(0, -133), new(325, 83));
            var pill = Panel("Availability", root, new(0, -208), new(250, 52), Hex("E6EEE3"));
            var state = Text("State", pill, "Coming soon", 25, Vector2.zero, new(230, 46));
            var card = root.gameObject.AddComponent<SublevelCard>();
            Set(card, "phase", phase); Set(card, "title", title); Set(card, "state", state); Set(card, "icon", icon); Set(card, "accent", accent); Set(card, "button", button);
            return SavePrefab(root.gameObject, "SublevelCard").GetComponent<SublevelCard>();
        }

        static void BuildStart()
        {
            var camera = NewScene("Start", new(0, 7.8f, -17), new(0, 1.8f, 0), 6.2f);
            var world = Group("Welcome garden"); Garden(world.transform);
            var owl = (GameObject)PrefabUtility.InstantiatePrefab(owlPrefab); owl.transform.position = new(4.4f, .2f, -.7f); owl.transform.localScale = Vector3.one * 1.35f;
            Disc("Owl welcome stage", world.transform, new(4.4f, .05f, -.7f), new(6.5f, .2f, 5), Cream);
            var ui = CanvasRoot();
            var controller = Group("Start screen controller").AddComponent<StartMenuController>();
            var welcome = UI("Welcome", ui); Stretch(welcome);
            var wash = Panel("Welcome card", welcome, new(-450, 0), new(760, 820), new Color(Cream.r, Cream.g, Cream.b, .96f));
            Text("Eyebrow", wash, "A LITTLE ADVENTURE FOR YOUR EYES", 24, new(0, 275), new(660, 60));
            var title = Text("EyeCanDo", wash, "EyeCanDo", 108, new(0, 157), new(680, 150)); title.fontStyle = FontStyles.Bold;
            Text("Invitation", wash, "Big smiles.\nBrave little steps.", 42, new(0, 5), new(650, 155));
            var start = MakeButton("Start button", wash, "Start", new(0, -174), new(540, 132), Green, Color.white, controller.PressStart, 53);
            Image("Play icon", start.transform, Icon("Play"), new(-165, 0), new(68, 68), Color.white);
            Text("Subtitle", wash, "Let's explore together", 26, new(0, -290), new(650, 55));
            Text("Mascot greeting", ui, "Your adventure buddy", 30, new(435, -398), new(560, 60));
            var setup = Panel("Username setup", ui, new(-450, 0), new(760, 850), Cream);
            var back = IconButton("Setup back", setup, "Back", new(-270, 316), controller.Back);
            Text("Setup heading", setup, "Hello, little explorer!", 51, new(0, 198), new(650, 110));
            Text("Caregiver guidance", setup, "Grown-up helper:\nwhat name should we use?", 30, new(0, 72), new(620, 100));
            var form = Form(setup, new(0, -80), "Continue", controller.Continue);
            Text("Local profile note", setup, "Just a name. Saved on this device.", 23, new(0, -335), new(650, 60));
            Set(controller, "welcome", welcome.gameObject); Set(controller, "usernamePanel", setup.gameObject); Set(controller, "usernameForm", form);
            setup.gameObject.SetActive(false);
            SaveScene("Start");
        }

        static void BuildMap()
        {
            var camera = NewScene("JourneyMap", new(0, 14, -20), new(0, .6f, 0), 7.2f);
            var world = Group("Clinic adventure garden"); Garden(world.transform);
            var owl = (GameObject)PrefabUtility.InstantiatePrefab(owlPrefab); owl.transform.position = new(-10, 0, 4); owl.transform.localScale = Vector3.one * .5f;
            var pebble = Disc("Path pebble", null, Vector3.zero, new(.62f, .07f, .72f), Cream);
            var pathPrefab = SavePrefab(pebble, "PathPebble");
            var layout = Group("Journey map layout").AddComponent<JourneyMapLayout>(); layout.catalog = catalog;
            layout.destinations = Group("Destinations", layout.transform).transform; layout.paths = Group("Connecting paths", layout.transform).transform;
            layout.pathPebble = pathPrefab; layout.mapCamera = camera; layout.Rebuild();
            var ui = CanvasRoot();
            var controller = Group("Journey map controller").AddComponent<JourneyMapController>();
            var header = Panel("Greeting card", ui, Vector2.zero, new(1170, 154), Cream);
            Anchor(header, new(0, 1), new(0, 1), new(0, 1), new(48, -38));
            var greeting = Text("Greeting", header, "Hello, explorer!", 48, new(0, 28), new(1080, 65)); greeting.fontStyle = FontStyles.Bold;
            Text("Map invitation", header, "Pick a place to explore", 29, new(0, -35), new(1080, 52));
            var buttons = UI("Top right controls", ui); buttons.sizeDelta = new(330, 154); Anchor(buttons, Vector2.one, Vector2.one, Vector2.one, new(-48, -38));
            IconButton("Shop", buttons, "Bag", new(-83, 0), controller.OpenShop);
            IconButton("Settings", buttons, "Gear", new(83, 0), controller.OpenSettings);
            var footer = Panel("Map footer", ui, Vector2.zero, new(790, 72), Cream);
            Anchor(footer, new(.5f,0), new(.5f,0), new(.5f,0), new(0,34));
            Text("Explore freely", footer, "Three journeys. Start anywhere!", 29, Vector2.zero, new(735, 60));
            var shop = Modal("Shop Coming Soon", ui, out var shopBody);
            Image("Shopping bag", shopBody, Icon("Bag"), new(0, 180), new(180, 180), Color.white);
            Text("Shop heading", shopBody, "A little shop is on its way!", 46, new(0, 28), new(840, 105));
            Text("Shop message", shopBody, "Coming soon", 34, new(0, -70), new(780, 70));
            MakeButton("Shop close", shopBody, "Back to the map", new(0, -217), new(560, 112), Green, Color.white, controller.ClosePanels, 34);
            var settings = Modal("Settings", ui, out var settingsBody);
            settingsBody.sizeDelta = new(960, 880);
            Text("Settings heading", settingsBody, "Your explorer name", 49, new(0, 315), new(840, 90));
            Text("Settings guidance", settingsBody, "Grown-up helper: tap Edit to change it.", 26, new(0, 225), new(820, 65));
            var form = Form(settingsBody, new(0, 10), "Save", controller.SaveUsername);
            MakeButton("Edit username", settingsBody, "Edit", new(0, -230), new(250, 112), Hex("E6EEE3"), Ink, controller.EditUsername, 30);
            MakeButton("Cancel settings", settingsBody, "Cancel", new(0, -355), new(420, 112), Cream, Ink, controller.ClosePanels, 30);
            Set(controller, "greeting", greeting); Set(controller, "settingsPanel", settings); Set(controller, "shopPanel", shop); Set(controller, "settingsForm", form);
            shop.SetActive(false); settings.SetActive(false);
            SaveScene("JourneyMap");
        }

        static void BuildJourney()
        {
            NewScene("JourneySelection", new(0, 14, -20), new(0, .6f, 0), 7.2f);
            var world = Group("Journey garden"); Garden(world.transform);
            var ui = CanvasRoot();
            var controller = Group("Journey selection controller").AddComponent<JourneySelectionController>();
            var wash = Image("Soft scene wash", ui, null, Vector2.zero, Vector2.zero, new Color(.87f,.94f,.88f,.83f)); Stretch(wash.rectTransform);
            var back = IconButton("Back to map", ui, "Back", Vector2.zero, controller.Back);
            Anchor((RectTransform)back.transform,new(0,1),new(0,1),new(0,1),new(48,-38));
            var heading = Text("Journey heading", ui, "Eye-Drop Preparation", 56, new(70, 359), new(1220, 95)); heading.fontStyle = FontStyles.Bold;
            var procedure = Image("Procedure icon", ui, Icon("Drop"), new(-630, 359), new(120,120), Color.white);
            Text("Journey invitation", ui, "Discover. Watch. Practice.", 31, new(0, 264), new(1100, 65));
            // The path remains visible behind the three cards.
            var path = Panel("Sublevel pathway", ui, new(0,-25),new(1280,24),Gold);
            for(int i=0;i<2;i++)
                Image("Path arrow",ui,Icon("Play"),new(-215+i*430,-20),new(60,60),Green);
            var cards = UI("Sublevels", ui); cards.sizeDelta = new(1280, 495); cards.anchoredPosition = new(0,-40);
            var group = cards.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            group.spacing = 36; group.childControlWidth = true; group.childControlHeight = true; group.childForceExpandHeight = true; group.childForceExpandWidth = true;
            for(int i=0;i<3;i++)
            {
                var card = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab.gameObject,cards);
                card.GetComponent<SublevelCard>().Bind(catalog.journeys[0].sublevels[i],i+1,Teal,_=>{});
            }
            Text("Journey footer", ui, "Little steps for a big adventure", 28, new(0,-405),new(1000,65));
            var soon = Modal("Sublevel Coming Soon",ui,out var body);
            Image("Coming soon clock",body,Icon("Soon"),new(0,191),new(184,184),Color.white);
            Text("Coming soon heading",body,"More adventures are on the way!",44,new(0,52),new(840,100));
            var soonTitle = Text("Activity name",body,"Discover the Eye Drops",30,new(0,-55),new(800,80));
            Text("Coming soon message",body,"Coming soon",29,new(0,-125),new(750,60));
            MakeButton("Close coming soon",body,"Back to the journey",new(0,-251),new(560,112),Green,Color.white,controller.CloseComingSoon,34);
            Set(controller,"heading",heading);Set(controller,"procedureIcon",procedure);Set(controller,"cardsRoot",cards);
            Set(controller,"cardPrefab",cardPrefab);Set(controller,"comingSoonPanel",soon);Set(controller,"comingSoonTitle",soonTitle);
            soon.SetActive(false);
            SaveScene("JourneySelection");
        }

        static Camera NewScene(string name, Vector3 position, Vector3 target, float size)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = Group("Main Camera"); cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = size;
            camera.backgroundColor = Hex("DCEEE5"); camera.clearFlags = CameraClearFlags.SolidColor; camera.farClipPlane = 120;
            camera.transform.position = position; camera.transform.LookAt(target);
            camera.allowHDR = true; cameraObject.AddComponent<AudioListener>(); cameraObject.AddComponent<PhysicsRaycaster>();
            var data = cameraObject.AddComponent<UniversalAdditionalCameraData>(); data.renderPostProcessing = true; data.volumeLayerMask = 1;
            var lightObject = Group("Warm sunlight"); var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.5f; light.color = Hex("FFF5DB"); light.shadows = LightShadows.Soft; light.shadowStrength = .45f;
            light.transform.rotation = Quaternion.Euler(45,-28,0);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Hex("C9DFE1"); RenderSettings.ambientIntensity = 1;
            var profile = LoadOrCreate<VolumeProfile>($"{Root}/Art/SoftClinicLighting.asset");
            if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>(true);AssetDatabase.AddObjectToAsset(tone,profile);}
            tone.mode.Override(TonemappingMode.Neutral);
            if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>(true);AssetDatabase.AddObjectToAsset(bloom,profile);}
            bloom.intensity.Override(.25f);bloom.threshold.Override(1);
            var volume=Group("Soft clinic lighting").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;EditorUtility.SetDirty(profile);
            PrefabUtility.InstantiatePrefab(appPrefab);
            var eventObject=Group("EventSystem");eventObject.AddComponent<EventSystem>();eventObject.AddComponent<InputSystemUIInputModule>();
            return camera;
        }

        static RectTransform CanvasRoot()
        {
            var root=UI("Screen UI",null);
            var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=root.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new(1920,1080);scaler.matchWidthOrHeight=.5f;
            root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var safe=UI("Safe area",root);Stretch(safe);safe.gameObject.AddComponent<SafeArea>();return safe;
        }
        static RectTransform UI(string name,Transform parent)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new(.5f,.5f);return rect;
        }
        static void Anchor(RectTransform r,Vector2 min,Vector2 max,Vector2 pivot,Vector2 position)
        { r.anchorMin=min;r.anchorMax=max;r.pivot=pivot;r.anchoredPosition=position; }
        static void Stretch(RectTransform r) { r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero; }
        static UnityEngine.UI.Image Round(GameObject go,Color color)
        { var image=go.AddComponent<UnityEngine.UI.Image>();image.sprite=Icon("Round");image.type=UnityEngine.UI.Image.Type.Sliced;image.color=color;image.raycastTarget=false;return image; }
        static RectTransform Panel(string name,Transform parent,Vector2 position,Vector2 size,Color color)
        { var r=UI(name,parent);r.anchoredPosition=position;r.sizeDelta=size;Round(r.gameObject,color);return r; }
        static UnityEngine.UI.Image Image(string name,Transform parent,Sprite sprite,Vector2 position,Vector2 size,Color color)
        {
            var r=UI(name,parent);r.anchoredPosition=position;r.sizeDelta=size;
            var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=sprite;image.color=color;image.raycastTarget=false;image.preserveAspect=sprite!=null;return image;
        }
        static TMP_Text Text(string name,Transform parent,string value,float size,Vector2 position,Vector2 bounds,Color? color=null)
        {
            var r=UI(name,parent);r.anchoredPosition=position;r.sizeDelta=bounds;
            var label=r.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.text=value;label.fontSize=size;
            label.color=color??Ink;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;label.richText=false;
            label.enableAutoSizing=true;label.fontSizeMax=size;label.fontSizeMin=Mathf.Max(18,size*.72f);return label;
        }
        static UnityEngine.UI.ColorBlock ButtonColors(Color background)
        {
            var c=UnityEngine.UI.ColorBlock.defaultColorBlock;c.normalColor=Color.white;c.highlightedColor=new(.94f,1,.96f,1);c.pressedColor=new(.8f,.9f,.85f,1);c.disabledColor=new(.7f,.7f,.7f,.65f);c.fadeDuration=.1f;return c;
        }
        static UnityEngine.UI.Button MakeButton(string name,Transform parent,string label,Vector2 position,Vector2 size,Color color,Color textColor,UnityAction action,float fontSize=35)
        {
            var r=Panel(name,parent,position,size,color);var image=r.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=true;
            var button=r.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.colors=ButtonColors(color);
            r.gameObject.AddComponent<ButtonFeedback>();
            var text=Text("Label",r,label,fontSize,Vector2.zero,size-new Vector2(40,20),textColor);text.fontStyle=FontStyles.Bold;
            Stretch(text.rectTransform);text.rectTransform.offsetMin=new(20,10);text.rectTransform.offsetMax=new(-20,-10);
            if(action!=null)UnityEventTools.AddPersistentListener(button.onClick,action);
            return button;
        }
        static UnityEngine.UI.Button IconButton(string name,Transform parent,string icon,Vector2 position,UnityAction action)
        {
            var button=MakeButton(name,parent,string.Empty,position,new(138,138),Cream,Ink,action);
            Image(name+" icon",button.transform,Icon(icon),new(0,8),new(83,83),Color.white);
            Text("Caregiver label",button.transform,name=="Back to map"||name=="Setup back"?"Back":name,20,new(0,-45),new(126,35));
            return button;
        }
        static UsernameForm Form(Transform parent,Vector2 position,string submitLabel,UnityAction onSubmit)
        {
            var root=UI(submitLabel+" username form",parent);root.anchoredPosition=position;root.sizeDelta=new(620,260);
            var field=Panel("Username field",root,new(0,60),new(580,112),Hex("E8F0E6"));
            var background=field.GetComponent<UnityEngine.UI.Image>();background.raycastTarget=true;
            var textArea=UI("Text area",field);Stretch(textArea);textArea.offsetMin=new(28,15);textArea.offsetMax=new(-28,-15);
            textArea.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var text=Text("Username text",textArea,string.Empty,37,Vector2.zero,new(520,75));Stretch(text.rectTransform);
            text.alignment=TextAlignmentOptions.MidlineLeft;text.enableAutoSizing=false;
            var placeholder=Text("Placeholder",textArea,"Explorer name",34,Vector2.zero,new(520,75),Hex("647C7C"));Stretch(placeholder.rectTransform);placeholder.alignment=TextAlignmentOptions.MidlineLeft;
            var input=field.gameObject.AddComponent<TMP_InputField>();input.targetGraphic=background;input.textViewport=textArea;
            input.textComponent=text;input.placeholder=placeholder;input.characterLimit=20;input.lineType=TMP_InputField.LineType.SingleLine;input.richText=false;
            var error=Text("Validation message",root,string.Empty,22,new(0,-15),new(600,48),Hex("9D3D49"));
            var submit=MakeButton(submitLabel+" button",root,submitLabel,new(0,-105),new(580,112),Green,Color.white,onSubmit,38);
            var form=root.gameObject.AddComponent<UsernameForm>();Set(form,"input",input);Set(form,"errorLabel",error);Set(form,"submit",submit);return form;
        }
        static GameObject Modal(string name,Transform parent,out RectTransform body)
        {
            var root=UI(name,parent);Stretch(root);
            var image=root.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new(.08f,.18f,.2f,.64f);image.raycastTarget=true;
            body=Panel(name+" card",root,Vector2.zero,new(960,750),Cream);
            body.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;return root.gameObject;
        }
        static void Set(UnityEngine.Object target,string field,UnityEngine.Object value)
        { var serialized=new SerializedObject(target);var property=serialized.FindProperty(field);if(property==null)throw new InvalidOperationException(field);property.objectReferenceValue=value;serialized.ApplyModifiedPropertiesWithoutUndo(); }
        static T LoadOrCreate<T>(string path) where T:ScriptableObject
        { var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset==null){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}return asset; }
        static GameObject SavePrefab(GameObject go,string name)
        { var asset=PrefabUtility.SaveAsPrefabAsset(go,$"{Root}/Prefabs/{name}.prefab");UnityEngine.Object.DestroyImmediate(go);return asset; }
        static void SaveScene(string name) => EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),$"{SceneFolder}/{name}.unity");
    }
}

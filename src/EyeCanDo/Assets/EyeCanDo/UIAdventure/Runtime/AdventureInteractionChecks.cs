#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace EyeCanDo
{
    // Protected Editor fixture. All pointer actions enter the real Input System/EventSystem.
    public sealed class AdventureInteractionChecks : MonoBehaviour
    {
        [Serializable] class Report {public string status,editor,failure;public List<string> passed;public int firstFrame,lastFrame;}
        readonly List<string> passed=new();
        Mouse mouse;Touchscreen touch;
        InputSettings.EditorInputBehaviorInPlayMode focus;InputSettings.BackgroundBehavior background;bool initialized;
        string failure;int firstFrame;
        public string outputFolder="Validation/UIIteration3/";
        string Folder=>outputFolder;
        void Awake(){DontDestroyOnLoad(gameObject);Application.logMessageReceived+=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)failure=message;}
        IEnumerator Start()
        {
            focus=InputSystem.settings.editorInputBehaviorInPlayMode;background=InputSystem.settings.backgroundBehavior;initialized=true;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            mouse=InputSystem.AddDevice<Mouse>("Adventure test mouse");touch=InputSystem.AddDevice<Touchscreen>("Adventure test touch");firstFrame=Time.frameCount;
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                object value;try{if(!stack.Peek().MoveNext()){stack.Pop();continue;}value=stack.Peek().Current;}catch(Exception e){failure=e.ToString();break;}
                if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;if(failure!=null)break;
            }
            File.WriteAllText(Folder+"interactions.json",JsonUtility.ToJson(new Report {status=failure==null?"passed":"failed",editor=Application.unityVersion,failure=failure,passed=passed,firstFrame=firstFrame,lastFrame=Time.frameCount},true));
            Restore();EditorApplication.isPlaying=false;
        }
        void OnDestroy()=>Restore();
        void Restore()
        {
            Application.logMessageReceived-=Log;SafeArea.TestOverride=null;if(!initialized)return;initialized=false;
            if(mouse!=null)InputSystem.RemoveDevice(mouse);if(touch!=null)InputSystem.RemoveDevice(touch);
            InputSystem.settings.editorInputBehaviorInPlayMode=focus;InputSystem.settings.backgroundBehavior=background;
        }
        void Check(bool value,string message){if(!value)throw new Exception(message);passed.Add(message);}
        static T Find<T>(string name) where T:Component {var g=GameObject.Find(name);if(g==null)throw new Exception("Not active: "+name);return g.GetComponent<T>();}
        Vector2 Point(UnityEngine.UI.Button button)
        {Canvas.ForceUpdateCanvases();var c=button.GetComponentInParent<Canvas>();return RectTransformUtility.WorldToScreenPoint(c.renderMode==RenderMode.ScreenSpaceOverlay?null:Camera.main,button.transform.position);}
        IEnumerator Click(string name,bool useTouch=false)=>Click(Find<UnityEngine.UI.Button>(name),useTouch);
        IEnumerator Click(UnityEngine.UI.Button button,bool useTouch=false)
        {
            var point=Point(button);var ray=new PointerEventData(EventSystem.current){position=point};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(ray,hits);
            Check(hits.Count>0 && hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()==button,"EventSystem target: "+button.name+(useTouch?" (touch)":" (mouse)"));
            yield return At(point,useTouch);
        }
        IEnumerator At(Vector2 p,bool useTouch)
        {
            if(useTouch){InputSystem.QueueStateEvent(touch,new TouchState {touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=p});yield return new WaitForSecondsRealtime(.1f);InputSystem.QueueStateEvent(touch,new TouchState {touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=p});}
            else{InputSystem.QueueStateEvent(mouse,new MouseState{position=p});yield return new WaitForSecondsRealtime(.12f);InputSystem.QueueStateEvent(mouse,new MouseState{position=p}.WithButton(MouseButton.Left,true));yield return new WaitForSecondsRealtime(.12f);InputSystem.QueueStateEvent(mouse,new MouseState{position=p});}
            yield return new WaitForSecondsRealtime(.25f);
        }
        IEnumerator Settle(string scene)
        {
            float end=Time.realtimeSinceStartup+15;while((SceneManager.GetActiveScene().name!=scene || AppRoot.Instance.Navigation.IsTransitioning)&&Time.realtimeSinceStartup<end)yield return null;
            Check(SceneManager.GetActiveScene().name==scene&&!AppRoot.Instance.Navigation.IsTransitioning,"Transition complete: "+scene);
            yield return new WaitForSecondsRealtime(.3f);Check(FindObjectsByType<EventSystem>().Length==1,"One EventSystem: "+scene);
        }
        IEnumerator Size(int w,int h)
        {
            NavigationSmokeTest.SetGameViewSize(w,h);yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();
            Check(Screen.width==w&&Screen.height==h,"Actual Game view "+w+"x"+h+" (was "+Screen.width+"x"+Screen.height+")");
        }
        IEnumerator Capture(string name)
        {InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(-100,-100)});yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Folder+name+".png");yield return null;}
        void Texts(string context)
        {
            foreach(var t in FindObjectsByType<TMP_Text>())
            {
                if(!t.isActiveAndEnabled)continue;
                t.ForceMeshUpdate();Check(t.font!=null&&t.font.name.StartsWith("Fredoka"),context+": Fredoka "+t.name);
                if(string.IsNullOrWhiteSpace(t.text))continue;
                Check(t.textInfo.characterCount>0,context+": visible text "+t.name);
                if(t is TextMeshProUGUI && t.GetComponentInParent<TMP_InputField>()==null)Check(!t.isTextOverflowing,context+": text fits "+t.name+" at "+t.fontSize);
            }
        }
        IEnumerator Safe(string[] controls,string screen)
        {
            yield return Size(1280,720);
            var areas=new[]{new Rect(76,20,1204,680),new Rect(0,20,1204,680),new Rect(34,34,1212,652)};
            for(int i=0;i<areas.Length;i++)
            {
                SafeArea.TestOverride=areas[i];yield return new WaitForSecondsRealtime(.2f);Canvas.ForceUpdateCanvases();
                foreach(var b in controls.Select(name=>Find<UnityEngine.UI.Button>(name)).Concat(FindObjectsByType<AdventureDestinationView>().Select(v=>v.badge)))
                {
                    string name=b.name;var corners=new Vector3[4];((RectTransform)b.transform).GetWorldCorners(corners);var canvas=b.GetComponentInParent<Canvas>();var cam=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:Camera.main;
                    var lo=RectTransformUtility.WorldToScreenPoint(cam,corners[0]);var hi=RectTransformUtility.WorldToScreenPoint(cam,corners[2]);
                    Check(hi.x-lo.x>=80 && hi.y-lo.y>=80,screen+": 80px minimum touch target "+name+" safe "+i);
                    Check(lo.x>=areas[i].xMin && hi.x<=areas[i].xMax && lo.y>=areas[i].yMin && hi.y<=areas[i].yMax,screen+": inside safe area "+name+" "+i);
                }
                yield return Capture(screen+"-safe-"+i);
            }
            SafeArea.TestOverride=null;yield return new WaitForSecondsRealtime(.2f);
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(.5f);Check(Time.frameCount>firstFrame,"Player loop advances");
            yield return Size(1920,1080);Texts("Start");yield return Capture("start-1920x1080");
            var avatar=Find<AvatarView>("Start Avatar");var eye=avatar.faceAttachment.GetComponentsInChildren<Transform>().First(t=>t.name=="Blink eye");var cameraPosition=Camera.main.transform.position;var rootPosition=avatar.transform.position;float min=1,max=0;float end=Time.realtimeSinceStartup+5.2f;
            avatar.Celebrate();float armStart=avatar.rightArm.localEulerAngles.z;bool armMoved=false;
            while(Time.realtimeSinceStartup<end){min=Mathf.Min(min,eye.localScale.y);max=Mathf.Max(max,eye.localScale.y);armMoved|=Mathf.Abs(avatar.rightArm.localEulerAngles.z-armStart)>20;yield return null;}
            Check(min<max*.5f,"Avatar blinks");Check(armMoved,"Avatar wave animates its shoulder");Check(rootPosition==avatar.transform.position,"Avatar animation has no root motion");Check(Camera.main.transform.position==cameraPosition,"Start camera stationary between viewport changes");
            var start=Find<UnityEngine.UI.Button>("Start button");var p=Point(start);InputSystem.QueueStateEvent(mouse,new MouseState{position=p});yield return new WaitForSecondsRealtime(.25f);Check(start.transform.localScale.x>1.01f,"Existing hover feedback");
            InputSystem.QueueStateEvent(mouse,new MouseState{position=p}.WithButton(MouseButton.Left,true));yield return new WaitForSecondsRealtime(.25f);Check(start.transform.localScale.x<.99f,"Existing press feedback");InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(-100,-100)});yield return new WaitForSecondsRealtime(.3f);
            yield return Safe(new[]{"Start button"},"start");yield return Capture("start-1280x720");yield return Click("Start button",true);
            Check(GameObject.Find("Username setup")!=null,"First Start opens username setup; scene="+SceneManager.GetActiveScene().name+"; hasProfile="+AppRoot.Instance.Profile.HasUsername);var input=Find<TMP_InputField>("Username field");
            foreach(var blank in new[]{"","   "}){input.text=blank;yield return null;Check(!Find<UnityEngine.UI.Button>("Continue button").interactable,"Blank name rejected: "+blank.Length);}
            input.text="  Élodie  ";yield return null;Texts("Username setup");yield return Capture("username-accented-1280x720");yield return Click("Continue button");yield return Settle("JourneyMap");
            Check(AppRoot.Instance.Profile.Username=="Élodie","Trim and save accented username");Check(Find<TMP_Text>("Greeting").text=="Hello, Élodie!","Greeting uses saved name");
            yield return Size(1920,1080);Texts("Map");yield return Capture("map-1920x1080");yield return Safe(new[]{"Shop","Settings","Back to Start"},"map");yield return Capture("map-1280x720");
            yield return Click("Shop",true);yield return Settle("Shop");
            var shop=Find<ShopController>("Shop controller");var service=AppRoot.Instance.Customization;
            Check(service.Snapshot.coins==200,"New profile gets 200 coins once");Check(FindObjectsByType<Camera>().Length==1,"Shop uses one real stage camera");
            yield return Size(1920,1080);Texts("Shop free");yield return Capture("shop-free-1920x1080");
            yield return Safe(new[]{"Shop Back","Shop action","Rotate left","Rotate right","Swatch 0","Swatch 5","Category Extras"},"shop");yield return Capture("shop-free-1280x720");
            yield return Click("Item 1");Check(shop.PreviewId=="face.oval"&&service.Snapshot.Get(CosmeticSlot.Face)=="face.round","Face preview does not commit");
            yield return Click("Swatch 1",true);Check(shop.PreviewState.Get(CosmeticSlot.Face)=="face.round"&&shop.PreviewState.Get(CosmeticSlot.Skin)=="skin.1","Selecting another item replaces the single preview");
            yield return Click("Category Hair");Check(shop.PreviewId==null&&shop.PreviewState.Get(CosmeticSlot.Skin)=="skin.3","Category change discards preview");
            yield return Click("Item 3");yield return Click("Shop action",true);Check(service.Snapshot.Get(CosmeticSlot.Hair)=="hair.puffs"&&service.Snapshot.coins==200,"Free hair equips without charging");
            yield return Click("Category Looks");yield return Click("Item 1");yield return Click("Shop action");Check(service.Snapshot.Get(CosmeticSlot.Face)=="face.oval"&&service.Snapshot.Get(CosmeticSlot.Hair)=="hair.puffs","Face equip preserves other selections");
            yield return Click("Category Hats");yield return Click("Item 1",true);Check(shop.PreviewId=="hat.cap"&&shop.actionLabel.text.Contains("40"),"Affordable cap preview offers Buy 40");
            yield return Size(1920,1080);Texts("Shop purchasable");yield return Capture("shop-purchasable-1920x1080");yield return Size(1280,720);yield return Capture("shop-purchasable-1280x720");
            yield return Click("Shop action");Check(service.Snapshot.coins==160&&service.IsOwned("hat.cap")&&service.Snapshot.Get(CosmeticSlot.Hat)=="hat.cap","Mouse purchase saves ownership, deducts 40, and equips");
            yield return Click("Shop action",true);Check(service.Snapshot.coins==160,"Repeated tap on equipped action cannot deduct coins");
            yield return Capture("shop-equipped-1280x720");yield return Size(1920,1080);yield return Capture("shop-equipped-1920x1080");yield return Size(1280,720);
            yield return Click("Item 0");yield return Click("Shop action");Check(service.Snapshot.Get(CosmeticSlot.Hat)=="hat.none"&&service.Snapshot.coins==160,"Remove hat free");
            yield return Click("Item 1");yield return Click("Shop action");Check(service.Snapshot.Get(CosmeticSlot.Hat)=="hat.cap"&&service.Snapshot.coins==160,"Owned hat re-equips free");
            yield return Click("Category Extras");yield return Click("Item 1");yield return Click("Shop action",true);Check(service.Snapshot.coins==110,"Touch purchase deducts glasses price 50");
            yield return Click("Category Hats");yield return Click("Item 2");yield return Click("Shop action");Check(service.Snapshot.coins==50,"Star hat purchase deducts 60");
            yield return Click("Category Extras");yield return Click("Item 3");Check(!shop.action.interactable&&shop.hint.text.Contains("30"),"Insufficient funds still previews backpack and shows shortfall");
            Check(Mathf.Abs(Mathf.DeltaAngle(shop.avatar.transform.localEulerAngles.y,135))<.1f,"Backpack preview turns to rear three-quarter view");yield return Capture("shop-insufficient-1280x720");yield return Size(1920,1080);Texts("Shop insufficient");yield return Capture("shop-insufficient-1920x1080");yield return Size(1280,720);
            yield return Click("Rotate right",true);Check(Mathf.Abs(Mathf.DeltaAngle(shop.avatar.transform.localEulerAngles.y,180))<.1f,"Rotation uses 45-degree steps");
            yield return Click("Shop Back",true);yield return Settle("JourneyMap");Check(service.Snapshot.Get(CosmeticSlot.Backpack)=="backpack.none"&&service.Snapshot.coins==50,"Back discards preview and retains completed purchases");
            Check(Find<AvatarView>("Map Avatar").GetComponentInChildren<Collider>()==null,"Map avatar has no interaction collider");
            yield return Click("Shop");yield return Settle("Shop");shop=Find<ShopController>("Shop controller");Check(service.Snapshot.coins==50,"Reopening Shop does not replenish coins");
            yield return Click("Category Clothes");yield return Click("Item 2");var keyboard=InputSystem.AddDevice<Keyboard>("Shop escape test");InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return new WaitForSecondsRealtime(.15f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Settle("JourneyMap");InputSystem.RemoveDevice(keyboard);Check(service.Snapshot.Get(CosmeticSlot.Outfit)=="outfit.0","Escape cancels preview and returns to map");
            yield return Click("Settings");Check(!Find<TMP_InputField>("Username field").interactable,"Settings opens saved name read-only");yield return Click("Edit username");Find<TMP_InputField>("Username field").text="Cancelled";yield return Click("Cancel settings");Check(AppRoot.Instance.Profile.Username=="Élodie","Settings cancel preserves saved profile");
            yield return Click("Settings",true);yield return Click("Edit username");input=Find<TMP_InputField>("Username field");input.text=" ";yield return null;Check(!Find<UnityEngine.UI.Button>("Save button").interactable,"Blank Settings draft rejected");
            foreach(string name in new[]{"Élodie José Zoë Åsa","WWWWWWWWWWWWWWWWWWWW"})
            {
                input.text=name;yield return null;input.textComponent.ForceMeshUpdate();Check(input.text==name && new string(input.textComponent.textInfo.characterInfo.Take(input.textComponent.textInfo.characterCount).Where(c=>c.character!='\u200B').Select(c=>c.character).ToArray())==name,"Username glyphs: "+name);Check(!input.textComponent.isTextOverflowing,"Long username fits input");Check(input.textComponent.fontSize>=24,"Username stays legible");
            }
            Texts("Settings modal");yield return Capture("settings-long-name-1280x720");yield return Click("Save button");Check(Find<TMP_Text>("Greeting").text=="Hello, WWWWWWWWWWWWWWWWWWWW!","Save updates greeting");Check(AppRoot.Instance.Customization.Snapshot.coins==50,"Username edits do not replenish coins");Texts("Long greeting");yield return Capture("map-long-name-1280x720");
            yield return Click("Settings");yield return Click("Edit username");Find<TMP_InputField>("Username field").text="Élodie";yield return Click("Save button");
            var app=AppRoot.Instance;yield return Click("Back to Start");yield return Settle("Start");yield return Click("Start button");yield return Settle("JourneyMap");Check(AppRoot.Instance==app&&GameObject.Find("Username setup")==null,"Back to Start then returning Start preserves profile and routing");
            foreach(var journey in AppRoot.Instance.Catalog.journeys)
            {
                yield return Click("Open "+journey.shortTitle,journey.id=="slit-lamp");yield return Settle("JourneySelection");Check(Find<JourneySelectionController>("Journey selection controller").Journey==journey,"Resolved stable journey "+journey.id);
                Check(FindObjectsByType<Camera>().Length==1,"No runtime illustration camera: "+journey.id);
                yield return Size(1920,1080);Texts(journey.id);yield return Capture(journey.id+"-1920x1080");yield return Safe(new[]{"Back to map",journey.sublevels[0].id,journey.sublevels[2].id},journey.id);yield return Capture(journey.id+"-1280x720");
                foreach(var level in journey.sublevels)
                {
                    Check(!level.isPlayable&&string.IsNullOrEmpty(level.scenePath),"Coming Soon data unchanged: "+level.id);
                    yield return Click(level.id);Check(GameObject.Find("Sublevel Coming Soon")!=null,"Platform opens Coming Soon: "+level.id);
                    SafeArea.TestOverride=new Rect(76,20,1204,680);yield return new WaitForSecondsRealtime(.15f);
                    var edgeHits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=new Vector2(10,360)},edgeHits);
                    Check(edgeHits.Count>0 && edgeHits[0].gameObject.name=="Sublevel Coming Soon","Modal blocks unsafe screen edge: "+level.id);SafeArea.TestOverride=null;yield return new WaitForSecondsRealtime(.15f);
                    Texts("Coming Soon "+level.id);
                    yield return At(Point(Find<UnityEngine.UI.Button>("Back to map")),false);Check(GameObject.Find("Sublevel Coming Soon")!=null&&SceneManager.GetActiveScene().name=="JourneySelection","Modal blocks Back: "+level.id);
                    if(level.phase==PreparationPhase.Discover)yield return Capture(journey.id+"-modal-1280x720");yield return Click("Close coming soon");
                    yield return Click(Find<AdventureDestinationView>(level.id).badge,true);Check(GameObject.Find("Sublevel Coming Soon")!=null,"Badge touch opens Coming Soon: "+level.id);yield return Click("Close coming soon",true);
                }
                yield return Click("Back to map",true);yield return Settle("JourneyMap");
            }
            Destroy(AppRoot.Instance.gameObject);yield return null;SceneManager.LoadScene("Start");yield return new WaitForSecondsRealtime(.5f);yield return Click("Start button");yield return Settle("JourneyMap");Check(AppRoot.Instance.Profile.Username=="Élodie","Returning launch reloads persisted profile");Check(AppRoot.Instance.Customization.Snapshot.coins==50,"Returning launch reloads spent balance");
            SafeArea.TestOverride=null;Check(!SafeArea.TestOverride.HasValue,"Safe area override cleared");
        }
    }
}
#endif

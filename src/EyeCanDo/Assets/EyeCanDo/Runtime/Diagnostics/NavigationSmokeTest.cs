#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace EyeCanDo
{
    // Editor-only integration checks. Mouse and touch events pass through the real EventSystem.
    public sealed class NavigationSmokeTest : MonoBehaviour
    {
        [Serializable] class Report { public string editor; public string status; public string failure; public List<string> passed; public List<string> editorIssues; }
        readonly List<string> passed=new();
        readonly List<string> editorIssues=new();
        Mouse mouse;
        Touchscreen touch;
        InputSettings.EditorInputBehaviorInPlayMode originalFocus;
        InputSettings.BackgroundBehavior originalBackground;
        bool settingsChanged;
        string failure;
        void Awake() { DontDestroyOnLoad(gameObject); Application.logMessageReceived += OnLog; }
        void OnLog(string message,string stack,LogType type)
        {
            if(type!=LogType.Exception && type!=LogType.Error)return;
            if(stack.Contains("UnityEditor.Search.SearchDatabase")){editorIssues.Add(message);return;}
            failure=message;
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory("Validation/Screenshots");
            originalFocus=InputSystem.settings.editorInputBehaviorInPlayMode;
            originalBackground=InputSystem.settings.backgroundBehavior;
            settingsChanged=true;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            mouse=InputSystem.AddDevice<Mouse>("EyeCanDoValidationMouse");
            touch=InputSystem.AddDevice<Touchscreen>("EyeCanDoValidationTouch");
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                object current;
                try { if(!stack.Peek().MoveNext()){stack.Pop();continue;} current=stack.Peek().Current; }
                catch(Exception e) { failure=e.ToString();break; }
                if(current is IEnumerator nested){stack.Push(nested);continue;}
                yield return current;
                if(failure!=null)break;
            }
            File.WriteAllText("Validation/navigation.json",JsonUtility.ToJson(new Report { editor=Application.unityVersion,status=failure==null?"passed":"failed",failure=failure,passed=passed,editorIssues=editorIssues },true));
            if(failure==null)Debug.Log($"EyeCanDo: {passed.Count} navigation checks passed.");else Debug.LogWarning("EyeCanDo navigation check failed: "+failure);
            InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(touch);
            RestoreSettings();
            Application.logMessageReceived-=OnLog;
            EditorApplication.isPlaying=false;
        }
        void OnDestroy()
        {
            Application.logMessageReceived-=OnLog;
            RestoreSettings();
        }
        void RestoreSettings()
        {
            if(!settingsChanged)return;
            InputSystem.settings.editorInputBehaviorInPlayMode=originalFocus;
            InputSystem.settings.backgroundBehavior=originalBackground;
            settingsChanged=false;
        }
        void Check(bool condition,string description) { if(!condition)throw new Exception(description);passed.Add(description); }
        static T Find<T>(string name) where T:Component
        {
            var go=GameObject.Find(name);if(go==null)throw new Exception("Active object not found: "+name);
            var component=go.GetComponent<T>();if(component==null)throw new Exception("Component missing: "+name);return component;
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(.4f);
            int initial=Time.frameCount;yield return null;Check(Time.frameCount>initial,"Player loop advances");
            Check(SceneManager.GetActiveScene().name=="Start","Launch to Start");
            yield return Capture("01-start");
            yield return Click(Find<UnityEngine.UI.Button>("Start button"));
            Check(GameObject.Find("Username setup")!=null,"First Start opens username setup");
            var input=Find<TMP_InputField>("Username field");
            input.text="   ";yield return null;
            Check(!Find<UnityEngine.UI.Button>("Continue button").interactable,"Whitespace disables Continue");
            input.text="  Alex  ";yield return null;
            yield return Capture("02-username");
            yield return Click(Find<UnityEngine.UI.Button>("Continue button"));yield return Settle("JourneyMap");
            Check(AppRoot.Instance.Profile.Username=="Alex","Setup trims and saves the username");
            Check(Find<TMP_Text>("Greeting").text=="Hello, Alex!","Map displays saved username");
            Check(FindObjectsByType<JourneyDestination>(FindObjectsSortMode.None).Length==3,"All three 3D destinations are present");
            Check(FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"One active EventSystem after transition");
            yield return Capture("03-map");
            foreach(var size in new[]{new Vector2Int(1920,1200),new Vector2Int(1440,1080),new Vector2Int(2520,1080),new Vector2Int(1920,1080)})
            {
                SetGameViewSize(size.x,size.y);
                yield return new WaitForSecondsRealtime(.3f);
                Check(Screen.width==size.x && Screen.height==size.y,"Landscape resolution: "+size.x+"x"+size.y);
                foreach(var target in FindObjectsByType<JourneyDestination>())
                {
                    var viewport=Camera.main.WorldToViewportPoint(target.transform.position+Vector3.up*2);
                    Check(viewport.x>.05f && viewport.x<.95f && viewport.y>.08f && viewport.y<.82f,"Destination visible at "+size.x+"x"+size.y+": "+target.definition.id);
                }
                yield return Capture("map-"+size.x+"x"+size.y);
            }
            yield return Click(Find<UnityEngine.UI.Button>("Shop"),true);
            Check(GameObject.Find("Shop Coming Soon")!=null,"Touch opens Shop Coming Soon");
            var destination=FindObjectsByType<JourneyDestination>(FindObjectsSortMode.None)[0];
            yield return ClickAt(Camera.main.WorldToScreenPoint(destination.transform.position+Vector3.up*2),false);
            Check(SceneManager.GetActiveScene().name=="JourneyMap" && GameObject.Find("Shop Coming Soon")!=null,"Modal blocks destination clicks");
            yield return Capture("04-shop");
            yield return Click(Find<UnityEngine.UI.Button>("Shop close"));
            Check(GameObject.Find("Shop Coming Soon")==null,"Shop closes to map");
            yield return Click(Find<UnityEngine.UI.Button>("Settings"));
            input=Find<TMP_InputField>("Username field");
            Check(input.text=="Alex" && !input.interactable,"Settings opens with saved read-only name");
            yield return Click(Find<UnityEngine.UI.Button>("Edit username"));
            input.text="Cancelled";yield return null;
            yield return Click(Find<UnityEngine.UI.Button>("Cancel settings"));
            Check(AppRoot.Instance.Profile.Username=="Alex","Cancel preserves saved username");
            yield return Click(Find<UnityEngine.UI.Button>("Settings"));
            yield return Click(Find<UnityEngine.UI.Button>("Edit username"));
            input=Find<TMP_InputField>("Username field");input.text=" ";yield return null;
            Check(!Find<UnityEngine.UI.Button>("Save button").interactable,"Invalid settings draft cannot be saved");
            input.text="  Robin  ";yield return null;
            yield return Capture("05-settings");
            yield return Click(Find<UnityEngine.UI.Button>("Save button"));
            Check(Find<TMP_Text>("Greeting").text=="Hello, Robin!","Settings Save updates greeting");
            Check(new LocalUserProfile(new PlayerPrefsProfileStore()).Username=="Robin","Saved edit survives fresh profile service");
            var definitions=AppRoot.Instance.Catalog.journeys;
            for(int j=0;j<definitions.Length;j++)
            {
                yield return Click(Find<UnityEngine.UI.Button>("Open "+definitions[j].shortTitle),j==1);yield return Settle("JourneySelection");
                Check(Find<TMP_Text>("Journey heading").text==definitions[j].title,"Open journey: "+definitions[j].id);
                yield return Capture("06-journey-"+definitions[j].id);
                foreach(var level in definitions[j].sublevels)
                {
                    yield return Click(Find<UnityEngine.UI.Button>(level.id));
                    Check(GameObject.Find("Sublevel Coming Soon")!=null,"Coming Soon: "+level.id);
                    if(j==0 && level.phase==PreparationPhase.Discover)yield return Capture("07-sublevel-coming-soon");
                    yield return Click(Find<UnityEngine.UI.Button>("Close coming soon"));
                }
                yield return Click(Find<UnityEngine.UI.Button>("Back to map"));yield return Settle("JourneyMap");
                Check(SceneManager.GetActiveScene().name=="JourneyMap","Back to map: "+definitions[j].id);
            }
            // Recreate all application services to simulate a fresh local launch.
            Destroy(AppRoot.Instance.gameObject);yield return null;
            SceneManager.LoadScene("Start");yield return new WaitForSecondsRealtime(.5f);
            yield return Click(Find<UnityEngine.UI.Button>("Start button"));yield return Settle("JourneyMap");
            Check(GameObject.Find("Username setup")==null && AppRoot.Instance.Profile.Username=="Robin","Returning launch skips setup and reloads edited name");
        }
        IEnumerator Click(UnityEngine.UI.Button button,bool useTouch=false)
        {
            Canvas.ForceUpdateCanvases();
            var canvas=button.GetComponentInParent<Canvas>();
            Vector2 position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:Camera.main,button.transform.position);
            yield return ClickAt(position,useTouch);
        }
        IEnumerator ClickAt(Vector2 position,bool useTouch)
        {
            if(useTouch)
            {
                InputSystem.QueueStateEvent(touch,new TouchState { touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=position });
                yield return null;yield return null;
                InputSystem.QueueStateEvent(touch,new TouchState { touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=position });
            }
            else
            {
                InputSystem.QueueStateEvent(mouse,new MouseState { position=position });yield return new WaitForSecondsRealtime(.08f);
                InputSystem.QueueStateEvent(mouse,new MouseState { position=position }.WithButton(MouseButton.Left,true));yield return new WaitForSecondsRealtime(.08f);
                InputSystem.QueueStateEvent(mouse,new MouseState { position=position });
            }
            yield return null;yield return new WaitForSecondsRealtime(.12f);
        }
        IEnumerator Settle(string scene)
        {
            float deadline=Time.realtimeSinceStartup+12;
            while((AppRoot.Instance.Navigation.IsTransitioning||SceneManager.GetActiveScene().name!=scene)&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(SceneManager.GetActiveScene().name==scene && !AppRoot.Instance.Navigation.IsTransitioning,"Transition completed: "+scene);
            yield return new WaitForSecondsRealtime(.15f);
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Validation/Screenshots/"+name+".png");yield return null;
        }

        public static void SetGameViewSize(int width,int height)
        {
            var sizesType=Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
            var sizes=sizesType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
            var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{Enum.Parse(Type.GetType("UnityEditor.GameViewSizeGroupType,UnityEditor"),"Standalone")});
            var texts=(string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group,null);
            int index=Array.FindIndex(texts,x=>x.Contains(width+"x"+height));
            if(index<0)
            {
                var sizeType=Type.GetType("UnityEditor.GameViewSize,UnityEditor");
                var fixedType=Enum.Parse(Type.GetType("UnityEditor.GameViewSizeType,UnityEditor"),"FixedResolution");
                var size=Activator.CreateInstance(sizeType,new object[]{fixedType,width,height,"EyeCanDo "+width+"x"+height});
                group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
                index=texts.Length;
            }
            var viewType=Type.GetType("UnityEditor.GameView,UnityEditor");
            var view=EditorWindow.GetWindow(viewType);
            viewType.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,index);
            viewType.GetProperty("lowResolutionForAspectRatios",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)?.SetValue(view,false);
            view.Repaint();view.Focus();
        }
    }
}
#endif

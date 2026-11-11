#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace EyeCanDo
{
    public sealed class GardenInteractionChecks : MonoBehaviour
    {
        [Serializable] class Report { public string status; public string failure; public List<string> passed; }
        readonly List<string> passed=new();
        Mouse mouse;Touchscreen touch;
        InputSettings.EditorInputBehaviorInPlayMode focus;
        InputSettings.BackgroundBehavior background;
        bool restored;
        void Awake(){DontDestroyOnLoad(gameObject);}
        IEnumerator Start()
        {
            focus=InputSystem.settings.editorInputBehaviorInPlayMode;background=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            mouse=InputSystem.AddDevice<Mouse>("GardenCheckMouse");touch=InputSystem.AddDevice<Touchscreen>("GardenCheckTouch");
            string failure=null;var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                object value;
                try {if(!stack.Peek().MoveNext()){stack.Pop();continue;}value=stack.Peek().Current;}
                catch(Exception e){failure=e.ToString();break;}
                if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
            }
            Directory.CreateDirectory("Validation/Polish2");File.WriteAllText("Validation/Polish2/interactions.json",JsonUtility.ToJson(new Report {status=failure==null?"passed":"failed",failure=failure,passed=passed},true));
            Restore();if(failure==null)Debug.Log("Garden interaction checks passed: "+passed.Count);else Debug.LogWarning(failure);
            Destroy(gameObject);
        }
        void OnDestroy()=>Restore();
        void Restore()
        {
            if(restored)return;restored=true;
            if(mouse!=null)InputSystem.RemoveDevice(mouse);if(touch!=null)InputSystem.RemoveDevice(touch);
            InputSystem.settings.editorInputBehaviorInPlayMode=focus;InputSystem.settings.backgroundBehavior=background;
        }
        void Check(bool condition,string label){if(!condition)throw new Exception(label);passed.Add(label);}
        IEnumerator Settle(string scene)
        {float until=Time.realtimeSinceStartup+10;while((SceneManager.GetActiveScene().name!=scene || AppRoot.Instance.Navigation.IsTransitioning)&&Time.realtimeSinceStartup<until)yield return null;Check(SceneManager.GetActiveScene().name==scene&&!AppRoot.Instance.Navigation.IsTransitioning,"Transition settles: "+scene);yield return new WaitForSecondsRealtime(.2f);}
        IEnumerator Click(Vector2 p,bool useTouch)
        {
            if(useTouch)
            {
                InputSystem.QueueStateEvent(touch,new TouchState {touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=p});yield return null;yield return null;
                InputSystem.QueueStateEvent(touch,new TouchState {touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=p});
            }
            else
            {
                InputSystem.QueueStateEvent(mouse,new MouseState {position=p});yield return new WaitForSecondsRealtime(.08f);
                InputSystem.QueueStateEvent(mouse,new MouseState {position=p}.WithButton(MouseButton.Left,true));yield return new WaitForSecondsRealtime(.08f);
                InputSystem.QueueStateEvent(mouse,new MouseState {position=p});
            }
            yield return new WaitForSecondsRealtime(.16f);
        }
        IEnumerator Button(string name)
        {
            var b=GameObject.Find(name).GetComponent<UnityEngine.UI.Button>();var canvas=b.GetComponentInParent<Canvas>();
            yield return Click(RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:Camera.main,b.transform.position),false);
        }
        IEnumerator Capture(string name)
        {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Validation/Polish2/"+name+".png");yield return null;}
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(.4f);Check(SceneManager.GetActiveScene().name=="JourneyMap","Garden is running");
            int frame=Time.frameCount;yield return null;Check(Time.frameCount>frame,"Player frames advance");
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})
            {
                NavigationSmokeTest.SetGameViewSize(size.x,size.y);yield return new WaitForSecondsRealtime(.5f);
                Check(Screen.width==size.x&&Screen.height==size.y,"Actual Game view resolution: "+size);
                var camera=Camera.main;var destinations=FindObjectsByType<JourneyDestination>();
                var rects=new List<Rect>();
                foreach(var d in destinations)
                {
                    var points=new List<Vector3>();
                    foreach(var r in d.GetComponentsInChildren<Renderer>())
                    {
                        var bounds=r.bounds;
                        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)points.Add(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));
                    }
                    var corners=new Vector3[4];foreach(var rt in d.GetComponentsInChildren<RectTransform>()){rt.GetWorldCorners(corners);points.AddRange(corners);}
                    Check(points.Select(camera.WorldToViewportPoint).All(p=>p.x>.025f&&p.x<.975f&&p.y>.09f&&p.y<.88f),"Entire destination fits "+size+": "+d.definition.id);
                    var badge=d.GetComponentInChildren<UnityEngine.UI.Button>().GetComponent<RectTransform>();badge.GetWorldCorners(corners);
                    var screens=corners.Select(v=>(Vector2)camera.WorldToScreenPoint(v)).ToArray();var min=screens.Aggregate(Vector2.Min);var max=screens.Aggregate(Vector2.Max);var rect=new Rect(min,max-min);
                    Check(rect.height>=64&&rect.width>=150,"Badge has generous touch bounds "+size+": "+d.definition.id);rects.Add(rect);
                    var icon=d.transform.Find("Journey badge").GetComponentsInChildren<UnityEngine.UI.Image>().Single(v=>v.name=="Journey pictogram");icon.rectTransform.GetWorldCorners(corners);Check(Vector2.Distance(camera.WorldToScreenPoint(corners[0]),camera.WorldToScreenPoint(corners[1]))>=48,"Large pictogram at "+size+": "+d.definition.id);
                }
                Check(!rects[0].Overlaps(rects[1])&&!rects[0].Overlaps(rects[2])&&!rects[1].Overlaps(rects[2]),"Badges do not overlap at "+size);
                foreach(string name in new[]{"Back to Start","Shop","Settings"})
                {
                    var control=GameObject.Find(name).GetComponent<RectTransform>();var corners=new Vector3[4];control.GetWorldCorners(corners);var canvas=control.GetComponentInParent<Canvas>();var p=corners.Select(v=>RectTransformUtility.WorldToScreenPoint(null,v)).ToArray();
                    Check(p.All(v=>v.x>=0&&v.x<=Screen.width&&v.y>=0&&v.y<=Screen.height)&&Vector2.Distance(p[0],p[1])>=64,"Safe and large control "+size+": "+name);
                }
                var origin=camera.transform.position;yield return new WaitForSecondsRealtime(.2f);Check(camera.transform.position==origin,"Stationary camera at "+size);
                yield return Capture("map-"+size.x+"x"+size.y);
            }
            NavigationSmokeTest.SetGameViewSize(1920,1080);yield return new WaitForSecondsRealtime(.3f);
            var ids=AppRoot.Instance.Catalog.journeys.Select(j=>j.id).ToArray();
            for(int i=0;i<ids.Length;i++)
            {
                var d=FindObjectsByType<JourneyDestination>().Single(x=>x.definition.id==ids[i]);
                Vector2 p=Camera.main.WorldToScreenPoint(d.transform.position+Vector3.up*2.3f);
                if(i==0)
                {
                    InputSystem.QueueStateEvent(mouse,new MouseState {position=p});yield return new WaitForSecondsRealtime(.15f);Check(d.equipment.localScale.x>1,"Equipment hover feedback");
                    InputSystem.QueueStateEvent(mouse,new MouseState {position=p}.WithButton(MouseButton.Left,true));yield return new WaitForSecondsRealtime(.12f);Check(d.equipment.localScale.x<1,"Equipment press feedback");
                    InputSystem.QueueStateEvent(mouse,new MouseState {position=p});yield return null;
                }
                else yield return Click(p,i==1);
                yield return Settle("JourneySelection");Check(AppRoot.Instance.Navigation.SelectedJourneyId==ids[i],"Equipment opens correct journey "+(i==1?"by touch: ":"by mouse: ")+ids[i]);
                yield return Button("Back to map");yield return Settle("JourneyMap");
            }
            yield return Button("Settings");yield return Capture("settings-unchanged");
            var dest=FindObjectsByType<JourneyDestination>()[0];yield return Click(Camera.main.WorldToScreenPoint(dest.transform.position+Vector3.up*2.3f),true);
            Check(SceneManager.GetActiveScene().name=="JourneyMap"&&GameObject.Find("Cancel settings")!=null,"Settings blocks touch through to equipment");
            yield return Button("Cancel settings");yield return Button("Shop");yield return Capture("shop-unchanged");yield return Button("Shop close");
        }
    }
}
#endif

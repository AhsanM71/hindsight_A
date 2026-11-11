#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace EyeCanDo
{
    public sealed class PolishFlowChecks:MonoBehaviour
    {
        [Serializable] class Report{public string status;public string failure;public int process;public List<string> passed;}
        [Serializable] class Backup{public bool existed;public string value;public string phase;public int process;}
        readonly List<string> passed=new();Mouse mouse;InputSettings.EditorInputBehaviorInPlayMode focus;InputSettings.BackgroundBehavior background;bool initialized;
        void Awake()=>DontDestroyOnLoad(gameObject);
        IEnumerator Start()
        {
            focus=InputSystem.settings.editorInputBehaviorInPlayMode;background=InputSystem.settings.backgroundBehavior;initialized=true;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            mouse=InputSystem.AddDevice<Mouse>("PolishFlowMouse");string failure=null;var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){object value;try{if(!stack.Peek().MoveNext()){stack.Pop();continue;}value=stack.Peek().Current;}catch(Exception e){failure=e.ToString();break;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;}
            File.WriteAllText("Validation/Polish2/restart-and-back.json",JsonUtility.ToJson(new Report {status=failure==null?"passed":"failed",failure=failure,process=System.Diagnostics.Process.GetCurrentProcess().Id,passed=passed},true));
            Restore();EditorApplication.isPlaying=false;
        }
        void OnDestroy()=>Restore();
        void Restore(){if(!initialized)return;initialized=false;if(mouse!=null)InputSystem.RemoveDevice(mouse);InputSystem.settings.editorInputBehaviorInPlayMode=focus;InputSystem.settings.backgroundBehavior=background;}
        void Check(bool c,string m){if(!c)throw new Exception(m);passed.Add(m);}
        IEnumerator Click(string name)
        {
            Canvas.ForceUpdateCanvases();
            var b=GameObject.Find(name).GetComponent<UnityEngine.UI.Button>();Vector2 p=RectTransformUtility.WorldToScreenPoint(null,b.transform.position);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=p});yield return new WaitForSecondsRealtime(.1f);InputSystem.QueueStateEvent(mouse,new MouseState{position=p}.WithButton(MouseButton.Left,true));yield return new WaitForSecondsRealtime(.1f);InputSystem.QueueStateEvent(mouse,new MouseState{position=p});yield return new WaitForSecondsRealtime(.5f);
        }
        IEnumerator Settle(string scene)
        {
            float deadline=Time.realtimeSinceStartup+12;
            while((SceneManager.GetActiveScene().name!=scene || AppRoot.Instance.Navigation.IsTransitioning) && Time.realtimeSinceStartup<deadline)yield return null;
            Check(SceneManager.GetActiveScene().name==scene && !AppRoot.Instance.Navigation.IsTransitioning,"Transition completed: "+scene);
            yield return new WaitForSecondsRealtime(.2f);
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(.5f);
            var backup=JsonUtility.FromJson<Backup>(File.ReadAllText("Validation/Polish2/profile-recovery.json"));
            Check(backup.process!=System.Diagnostics.Process.GetCurrentProcess().Id,"Actual Unity process restarted");
            Check(PlayerPrefs.GetString(PlayerPrefsProfileStore.UsernameKey)=="Alex","Alex persisted across process exit and reopen");
            Check(SceneManager.GetActiveScene().name=="Start","Relaunch opens Start");yield return Click("Start button");yield return Settle("JourneyMap");
            Check(SceneManager.GetActiveScene().name=="JourneyMap" && GameObject.Find("Username setup")==null,"Returning launch skips username setup");
            Check(GameObject.Find("Greeting").GetComponent<TMP_Text>().text=="Hello, Alex!","Saved-name greeting after process restart");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Validation/Polish2/persisted-Alex.png");yield return null;
            var app=AppRoot.Instance;yield return Click("Back to Start");yield return Settle("Start");Check(SceneManager.GetActiveScene().name=="Start","Map Back returns to Start");
            Check(AppRoot.Instance==app && app.Profile.Username=="Alex","Back preserves app instance and username");
            yield return Click("Start button");yield return Settle("JourneyMap");Check(SceneManager.GetActiveScene().name=="JourneyMap" && GameObject.Find("Username setup")==null,"Start after Back skips setup");
            // Blank variants use only this protected development session, never a gameplay branch.
            foreach(string value in new[]{"","   "})
            {
                new PlayerPrefsProfileStore().Write(value);Destroy(AppRoot.Instance.gameObject);yield return null;SceneManager.LoadScene("Start");yield return new WaitForSecondsRealtime(.5f);yield return Click("Start button");
                Check(GameObject.Find("Username setup")!=null,"Setup appears for saved "+(value.Length==0?"empty":"whitespace")+" name");
            }
        }
    }
}
#endif

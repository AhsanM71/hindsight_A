using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EyeCanDo
{
    public sealed class JourneyMapController : MonoBehaviour
    {
        [SerializeField] TMP_Text greeting;
        [SerializeField] GameObject settingsPanel;
        
        [SerializeField] UsernameForm settingsForm;
        void Start()
        {
            if (!AppRoot.Instance.Profile.HasUsername) { AppRoot.Instance.Navigation.GoToStart(); return; }
            AppRoot.Instance.Profile.Changed += UpdateGreeting;
            UpdateGreeting(AppRoot.Instance.Profile.Username);
        }
        void OnDestroy() { if (AppRoot.Instance != null) AppRoot.Instance.Profile.Changed -= UpdateGreeting; }
        void UpdateGreeting(string name) => greeting.text = $"Hello, {name}!";
        public void OpenSettings()
        {
            settingsPanel.SetActive(true);
            settingsForm.Begin(AppRoot.Instance.Profile.Username, false);
        }
        public void EditUsername() => settingsForm.Edit();
        public void SaveUsername() { if (settingsForm.Save()) settingsPanel.SetActive(false); }
        public void OpenShop() => AppRoot.Instance.Navigation.GoToShop();
        public void BackToStart() => AppRoot.Instance.Navigation.GoToStart();
        public void ClosePanels() { settingsPanel.SetActive(false); }
        void Update() { if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) ClosePanels(); }
    }
}

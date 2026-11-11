using UnityEngine;
using UnityEngine.InputSystem;

namespace EyeCanDo
{
    public sealed class StartMenuController : MonoBehaviour
    {
        [SerializeField] GameObject welcome;
        [SerializeField] GameObject usernamePanel;
        [SerializeField] UsernameForm usernameForm;
        public void PressStart()
        {
            if (AppRoot.Instance.Profile.HasUsername) AppRoot.Instance.Navigation.GoToMap();
            else
            {
                welcome.SetActive(false);
                usernamePanel.SetActive(true);
                usernameForm.Begin(string.Empty, true);
            }
        }
        public void Continue() { if (usernameForm.Save()) AppRoot.Instance.Navigation.GoToMap(); }
        public void Back() { usernamePanel.SetActive(false); welcome.SetActive(true); }
        void Update() { if (Keyboard.current?.escapeKey.wasPressedThisFrame == true && usernamePanel.activeSelf) Back(); }
    }
}

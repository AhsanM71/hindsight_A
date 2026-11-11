using TMPro;
using UnityEngine;

namespace EyeCanDo
{
    public sealed class UsernameForm : MonoBehaviour
    {
        [SerializeField] TMP_InputField input;
        [SerializeField] TMP_Text errorLabel;
        [SerializeField] UnityEngine.UI.Button submit;
        public TMP_InputField Input => input;

        void Awake()
        {
            input.characterLimit = LocalUserProfile.MaximumLength;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.onValueChanged.AddListener(_ => Refresh());
        }
        public void Begin(string username, bool editable)
        {
            input.SetTextWithoutNotify(username);
            input.interactable = editable;
            errorLabel.text = string.Empty;
            Refresh(false);
        }
        public void Edit() { input.interactable = true; Refresh(false); input.ActivateInputField(); }
        void Refresh(bool showError = true)
        {
            bool valid = LocalUserProfile.TryValidate(input.text, out _, out var message);
            submit.interactable = input.interactable && valid;
            errorLabel.text = showError && !valid ? message : string.Empty;
        }
        public bool Save()
        {
            bool result = AppRoot.Instance.Profile.TrySaveUsername(input.text, out var message);
            errorLabel.text = message;
            return result;
        }
    }
}

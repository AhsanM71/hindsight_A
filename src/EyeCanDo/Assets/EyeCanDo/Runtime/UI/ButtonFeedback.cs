using UnityEngine;
using UnityEngine.EventSystems;

namespace EyeCanDo
{
    public sealed class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        Vector3 original;
        float target = 1;
        void Awake() => original = transform.localScale;
        void OnDisable() { transform.localScale = original == Vector3.zero ? Vector3.one : original; target = 1; }
        void Update() => transform.localScale = Vector3.Lerp(transform.localScale, original * target, Time.unscaledDeltaTime * 18);
        public void OnPointerEnter(PointerEventData e) => target = 1.025f;
        public void OnPointerExit(PointerEventData e) => target = 1;
        public void OnPointerDown(PointerEventData e) => target = .96f;
        public void OnPointerUp(PointerEventData e) => target = 1;
    }
}

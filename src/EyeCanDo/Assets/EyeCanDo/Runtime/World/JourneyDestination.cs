using UnityEngine;
using UnityEngine.EventSystems;

namespace EyeCanDo
{
    public sealed class JourneyDestination : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public JourneyDefinition definition;
        public Transform equipment;
        public void Open() { if (definition != null && AppRoot.Instance != null) AppRoot.Instance.Navigation.OpenJourney(definition.id); }
        public void OnPointerClick(PointerEventData e) => Open();
        public void OnPointerEnter(PointerEventData e) { if (equipment != null) equipment.localScale = Vector3.one * 1.04f; }
        public void OnPointerExit(PointerEventData e) { if (equipment != null) equipment.localScale = Vector3.one; }
    }
}

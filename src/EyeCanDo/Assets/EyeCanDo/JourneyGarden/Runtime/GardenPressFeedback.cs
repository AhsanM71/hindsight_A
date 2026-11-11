using UnityEngine;
using UnityEngine.EventSystems;

namespace EyeCanDo
{
    // Added only to the new map prefabs; navigation remains JourneyDestination.Open().
    public sealed class GardenPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Transform equipment;
        public void OnPointerDown(PointerEventData e) { if (equipment != null) equipment.localScale = Vector3.one * .96f; }
        public void OnPointerUp(PointerEventData e) { if (equipment != null) equipment.localScale = Vector3.one * 1.04f; }
        public void OnPointerExit(PointerEventData e) { if (equipment != null) equipment.localScale = Vector3.one; }
    }
}

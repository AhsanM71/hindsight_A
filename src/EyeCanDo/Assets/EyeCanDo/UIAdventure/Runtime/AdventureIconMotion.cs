using UnityEngine;

namespace EyeCanDo
{
    // Only placed on decorative children. Pointer targets retain their authored bounds.
    public sealed class AdventureIconMotion : MonoBehaviour
    {
        public float bob = 3, angle = 2, speed = .85f;
        Vector3 origin;
        Quaternion rotation;
        void OnEnable() { origin = transform.localPosition; rotation = transform.localRotation; }
        void OnDisable() { transform.localPosition = origin; transform.localRotation = rotation; }
        void Update()
        {
            float wave = Mathf.Sin(Time.unscaledTime * speed + origin.x);
            transform.localPosition = origin + Vector3.up * (wave * bob);
            transform.localRotation = rotation * Quaternion.Euler(0, 0, wave * angle);
        }
    }
}

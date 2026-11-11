using UnityEngine;

namespace EyeCanDo
{
    public sealed class GardenMotion : MonoBehaviour
    {
        public float bob = .06f;
        public float sway = 1.5f;
        public float speed = .7f;
        Vector3 origin; Quaternion orientation;
        void Awake() { origin = transform.localPosition; orientation = transform.localRotation; }
        void Update()
        {
            float wave = Mathf.Sin(Time.time * speed + origin.x * 1.7f + origin.z);
            transform.localPosition = origin + Vector3.up * (wave * bob);
            transform.localRotation = orientation * Quaternion.Euler(wave * sway * .3f, 0, wave * sway);
        }
    }
}

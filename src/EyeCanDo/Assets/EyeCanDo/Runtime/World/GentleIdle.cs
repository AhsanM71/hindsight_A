using UnityEngine;

namespace EyeCanDo
{
    public sealed class GentleIdle : MonoBehaviour
    {
        public float amplitude = .07f;
        public float rotation = 2;
        Vector3 position;
        Quaternion orientation;
        float phase;
        void Awake() { position = transform.localPosition; orientation = transform.localRotation; phase = transform.position.x; }
        void Update()
        {
            float wave = Mathf.Sin(Time.time * 1.3f + phase);
            transform.localPosition = position + Vector3.up * (wave * amplitude);
            transform.localRotation = orientation * Quaternion.Euler(0, wave * rotation, 0);
        }
    }
}

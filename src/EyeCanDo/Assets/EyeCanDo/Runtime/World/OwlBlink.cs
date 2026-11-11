using UnityEngine;

namespace EyeCanDo
{
    public sealed class OwlBlink : MonoBehaviour
    {
        public Transform[] eyes;
        Vector3[] sizes;
        void Awake() { sizes = new Vector3[eyes.Length]; for (int i = 0; i < eyes.Length; i++) sizes[i] = eyes[i].localScale; }
        void Update()
        {
            float cycle = Time.time % 4.8f;
            float scale = cycle < .2f ? Mathf.Max(.08f, Mathf.Abs(cycle - .1f) * 10) : 1;
            for (int i = 0; i < eyes.Length; i++)
                eyes[i].localScale = new Vector3(sizes[i].x, sizes[i].y * scale, sizes[i].z);
        }
    }
}

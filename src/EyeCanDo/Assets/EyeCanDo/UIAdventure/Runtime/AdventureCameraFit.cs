using UnityEngine;

namespace EyeCanDo
{
    [RequireComponent(typeof(Camera))]
    public sealed class AdventureCameraFit : MonoBehaviour
    {
        public Vector3 focus = new Vector3(0, 2, 2);
        public float referenceSize = 8.5f;
        public Vector2 referenceAspect = new Vector2(16, 9);
        Vector2Int dimensions;
        Rect safe;
        void OnEnable() => Fit();
        void LateUpdate()
        {
            if (dimensions != new Vector2Int(Screen.width, Screen.height) || safe != SafeArea.Current) Fit();
        }
        public void Fit()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            dimensions = new Vector2Int(Screen.width, Screen.height); safe = SafeArea.Current;
            var camera = GetComponent<Camera>();
            float aspect = safe.width / Mathf.Max(1, safe.height);
            camera.orthographicSize = referenceSize * Mathf.Max(1, referenceAspect.x / referenceAspect.y / aspect);
            transform.position = focus - transform.forward * 35;
        }
    }
}

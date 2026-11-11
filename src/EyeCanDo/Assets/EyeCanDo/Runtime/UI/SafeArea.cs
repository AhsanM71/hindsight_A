using UnityEngine;

namespace EyeCanDo
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        // Editor-only fixture; player builds always use the physical safe area.
#if UNITY_EDITOR
        public static Rect? TestOverride;
#endif
        public static Rect Current
        {
            get
            {
#if UNITY_EDITOR
                if (TestOverride.HasValue) return TestOverride.Value;
#endif
                return Screen.safeArea;
            }
        }
        Rect previous;
        Vector2Int dimensions;
        void OnEnable() => Apply();
        void Update()
        {
            if (previous != Current || dimensions != new Vector2Int(Screen.width, Screen.height)) Apply();
        }
        void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            previous = Current;
            dimensions = new Vector2Int(Screen.width, Screen.height);
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(previous.xMin / Screen.width, previous.yMin / Screen.height);
            rect.anchorMax = new Vector2(previous.xMax / Screen.width, previous.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

using System;
using UnityEngine;

namespace EyeCanDo
{
    public enum PreparationPhase { Discover, Watch, Practice }

    [Serializable]
    public sealed class SublevelDefinition
    {
        public string id;
        public PreparationPhase phase;
        public string title;
        public Sprite icon;
        public bool isPlayable;
        [Tooltip("Full scene path, also enabled in Build Profiles. Leave empty for Coming Soon.")]
        public string scenePath;
    }

    [CreateAssetMenu(menuName = "EyeCanDo/Journey")]
    public sealed class JourneyDefinition : ScriptableObject
    {
        public string id;
        public string title;
        public string shortTitle;
        public Color accent;
        public Sprite icon;
        public GameObject destinationPrefab;
        public SublevelDefinition[] sublevels = Array.Empty<SublevelDefinition>();
    }
}

using UnityEngine;

namespace EyeCanDo
{
    [CreateAssetMenu(menuName = "EyeCanDo/UI/Adventure Presentation Theme")]
    public sealed class JourneyPresentationTheme : ScriptableObject
    {
        public string journeyId;
        public GameObject courtyardPrefab;
        public Color accent, pale, ink;
        public Sprite platform;
        public Sprite[] phaseIllustrations = new Sprite[3];
    }
}

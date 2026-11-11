using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EyeCanDo
{
    public sealed class SceneNavigator : MonoBehaviour
    {
        public const string StartScene = "Start";
        public const string MapScene = "JourneyMap";
        public const string ShopScene = "Shop";
        public const string JourneyScene = "JourneySelection";
        [SerializeField] CanvasGroup fade;
        public string SelectedJourneyId { get; private set; }
        public bool IsTransitioning { get; private set; }

        void Awake() { if (fade != null) { fade.alpha = 0; fade.blocksRaycasts = false; } }
        public void GoToShop() => Load(ShopScene);
        public void GoToMap() => Load(MapScene);
        public void GoToStart() => Load(StartScene);
        public void OpenJourney(string id)
        {
            if (IsTransitioning || AppRoot.Instance.Catalog.Find(id) == null) return;
            SelectedJourneyId = id;
            Load(JourneyScene);
        }

        public bool TryOpenSublevel(SublevelDefinition level)
        {
            if (level == null || !level.isPlayable || string.IsNullOrWhiteSpace(level.scenePath)) return false;
            if (!Application.CanStreamedLevelBeLoaded(level.scenePath))
            {
                Debug.LogWarning($"EyeCanDo: scene is unavailable for sublevel {level.id}; showing Coming Soon.");
                return false;
            }
            Load(level.scenePath);
            return true;
        }

        void Load(string scene)
        {
            if (!IsTransitioning) StartCoroutine(Transition(scene));
        }

        IEnumerator Transition(string scene)
        {
            IsTransitioning = true;
            if (fade != null) fade.blocksRaycasts = true;
            yield return Fade(0, 1);
            yield return SceneManager.LoadSceneAsync(scene);
            yield return Fade(1, 0);
            if (fade != null) fade.blocksRaycasts = false;
            IsTransitioning = false;
        }

        IEnumerator Fade(float from, float to)
        {
            if (fade == null) yield break;
            for (float time = 0; time < .18f; time += Time.unscaledDeltaTime)
            { fade.alpha = Mathf.Lerp(from, to, time / .18f); yield return null; }
            fade.alpha = to;
        }
    }
}

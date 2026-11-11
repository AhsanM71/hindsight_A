using System.Collections.Generic;
using UnityEngine;

namespace EyeCanDo
{
    public sealed class AdventureSelectionPresenter : MonoBehaviour
    {
        public JourneyPresentationTheme[] themes;
        public Transform courtyardRoot;
        GameObject courtyard;
        public void Present(JourneyDefinition journey, IReadOnlyList<SublevelCard> views)
        {
            var theme = System.Array.Find(themes, t => t != null && t.journeyId == journey.id);
            if (theme == null) return;
            if (courtyard != null) Destroy(courtyard);
            courtyard = Instantiate(theme.courtyardPrefab, courtyardRoot);
            for (int i = 0; i < views.Count; i++)
            {
                var rect = (RectTransform)views[i].transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
                float x = (i - (views.Count - 1) * .5f) * 545;
                rect.anchoredPosition = new Vector2(x, i == 1 ? 35 : -30);
                rect.sizeDelta = new Vector2(475, 550);
                views[i].GetComponent<AdventureDestinationView>()?.Apply(theme, journey.sublevels[i].phase);
            }
        }
    }
}

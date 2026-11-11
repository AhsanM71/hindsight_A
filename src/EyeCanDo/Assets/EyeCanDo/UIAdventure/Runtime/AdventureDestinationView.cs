using System;
using TMPro;
using UnityEngine;

namespace EyeCanDo
{
    public sealed class AdventureDestinationView : MonoBehaviour
    {
        public UnityEngine.UI.Image illustration, medallion, platform;
        public TMP_Text phase;
        public UnityEngine.UI.Button badge;
        public void Bind(SublevelDefinition level, Action<SublevelDefinition> select)
        {
            phase.text = level.phase.ToString();
            badge.onClick.RemoveAllListeners();
            badge.onClick.AddListener(() => select(level));
        }
        public void Apply(JourneyPresentationTheme theme, PreparationPhase preparation)
        {
            int index = (int)preparation;
            if (index < theme.phaseIllustrations.Length) illustration.sprite = theme.phaseIllustrations[index];
            illustration.color = Color.white;
            medallion.color = theme.pale;
            platform.sprite = theme.platform;
            phase.color = theme.ink;
        }
    }
}

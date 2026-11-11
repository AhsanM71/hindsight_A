using System;
using TMPro;
using UnityEngine;

namespace EyeCanDo
{
    public sealed class SublevelCard : MonoBehaviour
    {
        [SerializeField] TMP_Text phase;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text state;
        [SerializeField] UnityEngine.UI.Image icon;
        [SerializeField] UnityEngine.UI.Image accent;
        [SerializeField] UnityEngine.UI.Button button;
        public void Bind(SublevelDefinition level, int number, Color color, Action<SublevelDefinition> onSelect)
        {
            gameObject.name = level.id;
            phase.text = $"{number}  {level.phase}";
            title.text = level.title;
            state.text = level.isPlayable && Application.CanStreamedLevelBeLoaded(level.scenePath) ? "Let's go!" : "Coming soon";
            icon.sprite = level.icon;
            accent.color = color;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelect(level));
            GetComponent<AdventureDestinationView>()?.Bind(level, onSelect);
        }
    }
}

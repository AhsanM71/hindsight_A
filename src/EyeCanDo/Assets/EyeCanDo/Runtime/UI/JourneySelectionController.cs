using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EyeCanDo
{
    public sealed class JourneySelectionController : MonoBehaviour
    {
        [SerializeField] TMP_Text heading;
        [SerializeField] UnityEngine.UI.Image procedureIcon;
        [SerializeField] Transform cardsRoot;
        [SerializeField] SublevelCard cardPrefab;
        [SerializeField] GameObject comingSoonPanel;
        [SerializeField] TMP_Text comingSoonTitle;
        public JourneyDefinition Journey { get; private set; }
        void Start()
        {
            var app = AppRoot.Instance;
            if (!app.Profile.HasUsername) { app.Navigation.GoToStart(); return; }
            Journey = app.Catalog.Find(app.Navigation.SelectedJourneyId);
            if (Journey == null) { app.Navigation.GoToMap(); return; }
            heading.text = Journey.title;
            procedureIcon.sprite = Journey.icon;
            foreach (Transform child in cardsRoot) Destroy(child.gameObject);
            var views = new List<SublevelCard>();
            for (int i = 0; i < Journey.sublevels.Length; i++)
            {
                var view = Instantiate(cardPrefab, cardsRoot);
                view.Bind(Journey.sublevels[i], i + 1, Journey.accent, Select);
                views.Add(view);
            }
            GetComponent<AdventureSelectionPresenter>()?.Present(Journey, views);
        }
        void Select(SublevelDefinition level)
        {
            if (AppRoot.Instance.Navigation.TryOpenSublevel(level)) return;
            comingSoonTitle.text = level.title;
            comingSoonPanel.SetActive(true);
        }
        public void CloseComingSoon() => comingSoonPanel.SetActive(false);
        public void Back() => AppRoot.Instance.Navigation.GoToMap();
        void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame != true) return;
            if (comingSoonPanel.activeSelf) CloseComingSoon(); else Back();
        }
    }
}

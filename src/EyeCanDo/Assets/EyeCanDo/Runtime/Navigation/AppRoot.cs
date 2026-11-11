using UnityEngine;

namespace EyeCanDo
{
    [RequireComponent(typeof(SceneNavigator))]
    public sealed class AppRoot : MonoBehaviour
    {
        public static AppRoot Instance { get; private set; }
        [SerializeField] JourneyCatalog catalog;
        public JourneyCatalog Catalog => catalog;
        public LocalUserProfile Profile { get; private set; }
        public CustomizationService Customization { get; private set; }
        [SerializeField] CosmeticCatalog cosmeticCatalog;
        public CosmeticCatalog Cosmetics => cosmeticCatalog;
        public SceneNavigator Navigation { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Profile = new LocalUserProfile(new PlayerPrefsProfileStore());
            Customization = new CustomizationService(cosmeticCatalog, new PlayerPrefsCustomizationStore(), new UnityCustomizationCodec());
            Navigation = GetComponent<SceneNavigator>();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}

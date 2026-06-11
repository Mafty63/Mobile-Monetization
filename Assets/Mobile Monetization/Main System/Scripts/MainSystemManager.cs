#pragma warning disable 0649

using MobileCore.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MobileCore.MainModule
{
    [DefaultExecutionOrder(-999)]
    public class MainSystemManager : SingletonMonoBehaviour<MainSystemManager>
    {
        private MobileCoreConfig config;

        public static bool IsInitialized { get; private set; }
        public static MobileCoreConfig InitSettings { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (IsInitialized) return;

            // Load the settings from Resources
            MobileCoreConfig settings = Resources.Load<MobileCoreConfig>("Plugin Settings/MobileCoreConfig");
            if (settings == null)
            {
                Debug.LogError("[MainSystemManager] Failed to load MobileCoreConfig from Resources! " +
                               "Make sure it exists at: Resources/Plugin Settings/MobileCoreConfig");
                return;
            }

            InitSettings = settings;

            // Create persistent GameObject — no need to place it in the scene manually
            GameObject container = new GameObject("[MobileCore]");
            DontDestroyOnLoad(container);

            // MonoBehaviourExecution must be added first
            container.AddComponent<MonoBehaviourExecution>();

            // Add MainSystemManager and assign settings
            var manager = container.AddComponent<MainSystemManager>();
            manager.config = settings;

            IsInitialized = true;

            // Ensure an EventSystem exists in every scene
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureEventSystem();
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
                                           UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            EnsureEventSystem();
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystem = eventSystemGO.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                eventSystemGO.AddComponent<StandaloneInputModule>();
#endif
            }
            else
            {
#if ENABLE_INPUT_SYSTEM
                if (eventSystem.gameObject.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                {
                    var standalone = eventSystem.gameObject.GetComponent<StandaloneInputModule>();
                    if (standalone != null) Destroy(standalone);
                    eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
#else
                if (eventSystem.gameObject.GetComponent<StandaloneInputModule>() == null)
                    eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        protected override void Awake()
        {
            base.Awake();

            if (config == null)
                config = InitSettings;

            if (config != null)
                InitializeModules(config);
        }

        private void InitializeModules(MobileCoreConfig cfg)
        {
            foreach (MobileModule module in cfg.Modules)
            {
                if (module == null) continue;
                if (!module.ModuleEnabled) continue;

                module.Initialize(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                IsInitialized = false;
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }
    }
}
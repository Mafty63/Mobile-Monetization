#pragma warning disable 0649

using System;

using MobileCore.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MobileCore.MainModule
{
    [DefaultExecutionOrder(-999)]
    public class MainSystemManager : SingletonMonoBehaviour<MainSystemManager>
    {
        private MainSystemSettings mainSystemSettings;

        public static bool IsInitialized { get; private set; }
        public static MainSystemSettings InitSettings { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (IsInitialized) return;

            // Load the settings from Resources
            MainSystemSettings settings = Resources.Load<MainSystemSettings>("Plugin Settings/MainSystemSettings");
            if (settings == null)
            {
                Debug.LogError("[MainSystemManager] Failed to load MainSystemSettings from Resources! Make sure it is in a Resources folder at path: Resources/Plugin Settings/MainSystemSettings");
                return;
            }

            InitSettings = settings;

            // Create persistent GameObject
            GameObject container = new GameObject("[MainSystemManager]");
            DontDestroyOnLoad(container);

            // Add MonoBehaviourExecution first so it initializes its singleton
            container.AddComponent<MonoBehaviourExecution>();

            // Add MainSystemManager and assign settings
            var manager = container.AddComponent<MainSystemManager>();
            manager.mainSystemSettings = settings;

            IsInitialized = true;

            // Register EventSystem helper
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            
            EnsureEventSystem();
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
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
                {
                    eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                }
#endif
            }
        }

        protected override void Awake()
        {
            base.Awake();

            if (mainSystemSettings == null)
            {
                mainSystemSettings = InitSettings;
            }

            if (mainSystemSettings != null)
            {
                mainSystemSettings.Initialize(this);
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

        public static bool IsModuleInitialized(Type moduleType)
        {
            MainSystemSettings projectInitSettings = InitSettings;

            BaseManagerInitializer coreModule = null;
            BaseManagerInitializer[] initModules = null;


            if (projectInitSettings != null)
            {
                coreModule = projectInitSettings.CoreModule;
                initModules = projectInitSettings.Modules;
            }

            if (coreModule != null && coreModule.GetType() == moduleType)
            {
                return true;
            }

            if (initModules != null)
            {
                for (int i = 0; i < initModules.Length; i++)
                {
                    if (initModules[i] != null && initModules[i].GetType() == moduleType)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
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
        [SerializeField] private MainSystemSettings initSettings;
        [SerializeField] private EventSystem eventSystem;


        public static bool IsInitialized { get; private set; }
        public static MainSystemSettings InitSettings { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            if (!IsInitialized)
            {
                IsInitialized = true;

                InitSettings = initSettings;

                gameObject.AddComponent<MonoBehaviourExecution>();

#if ENABLE_INPUT_SYSTEM
                eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif

                DontDestroyOnLoad(gameObject);

                initSettings.Initialize(this);
            }
        }

        private void OnDestroy()
        {
            IsInitialized = false;
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

            if (coreModule.GetType() == moduleType)
            {
                return true;
            }

            for (int i = 0; i < initModules.Length; i++)
            {
                if (initModules[i].GetType() == moduleType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
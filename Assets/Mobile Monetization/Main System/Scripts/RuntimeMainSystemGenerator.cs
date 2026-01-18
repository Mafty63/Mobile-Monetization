// using UnityEngine;
// using UnityEngine.SceneManagement;

// namespace MobileCore.MainModule
// {
//     public static class RuntimeMainSystemGenerator
//     {
//         [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
//         private static void Initialize()
//         {
//             if (SceneManager.GetActiveScene().name == "Initialization")
//                 return;

//             MainSystemManager mainModuleManager = GameObject.FindFirstObjectByType<MainSystemManager>();
//             if (mainModuleManager != null)
//                 return;

//             GameObject initializerPrefab = Resources.Load<GameObject>("Initializer");
//             if (initializerPrefab != null)
//             {
//                 GameObject initializerObject = GameObject.Instantiate(initializerPrefab);
//                 initializerObject.name = "Initializer (Runtime)";

//                 MainSystemManager mainModuleManagerComponent = initializerObject.GetComponent<MainSystemManager>();
//                 if (mainModuleManagerComponent != null)
//                 {
//                     // mainModuleManagerComponent.Awake();

//                 }

//                 GameObject.DontDestroyOnLoad(initializerObject);
//             }
//             else
//             {
//                 Debug.LogError("[RuntimeInitializer]: Initializer prefab is missing from Resources folder!");
//             }
//         }
//     }
// }
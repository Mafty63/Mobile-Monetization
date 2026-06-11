using UnityEngine;

namespace MobileCore.MainModule
{
    /// <summary>
    /// Base class for all Mobile Core module configuration assets.
    /// Subclass this to create a new module that can be registered in MobileCoreConfig.
    /// </summary>
    public abstract class MobileModule : ScriptableObject
    {
        /// <summary>
        /// Whether this module is enabled and should be initialized at startup.
        /// </summary>
        [SerializeField] private bool moduleEnabled = true;
        public bool ModuleEnabled => moduleEnabled;

        /// <summary>
        /// Display name shown in the MobileCoreConfig inspector.
        /// Override to return a custom label.
        /// </summary>
        public virtual string ModuleName => GetType().Name;

        /// <summary>
        /// Called by MainSystemManager during the Bootstrap phase.
        /// Implement all initialization logic here.
        /// </summary>
        /// <param name="parent">The persistent DontDestroyOnLoad container GameObject.</param>
        public abstract void Initialize(GameObject parent);
    }
}

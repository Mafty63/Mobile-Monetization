using System.Collections.Generic;
using UnityEngine;

namespace MobileCore.MainModule
{
    [HelpURL("https://quick-setup-website.pages.dev/documentation/mobile-monetization/getting-started/")]
    [CreateAssetMenu(fileName = "MobileCoreConfig", menuName = "Mobile Core/Mobile Core Config")]
    public class MobileCoreConfig : ScriptableObject
    {
        [Tooltip("List of all modules to initialize at startup. Order matters — modules are initialized top to bottom.")]
        [SerializeField] private List<MobileModule> modules = new List<MobileModule>();
        public IReadOnlyList<MobileModule> Modules => modules;
    }
}

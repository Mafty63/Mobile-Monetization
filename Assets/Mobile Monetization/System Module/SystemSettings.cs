using UnityEngine;

namespace MobileCore.SystemModule
{
    [CreateAssetMenu(fileName = "SystemSettings", menuName = "Mobile Core/Settings/System Settings")]
    public class SystemSettings : ScriptableObject
    {
        [InspectorName("System Canvas")]
        [Tooltip("Canvas prefab containing core UI elements like message system and loading panels.")]
        [SerializeField] private GameObject systemCanvas;
        public GameObject SystemCanvas => systemCanvas;

        [SerializeField] private ScreenSettings screenSettings = new ScreenSettings();
        public ScreenSettings ScreenSettings => screenSettings;
    }
}

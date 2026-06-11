using UnityEngine;
using MobileCore.MainModule;

namespace MobileCore.Advertisements
{
    [CreateAssetMenu(fileName = "AdsModuleConfig", menuName = "Mobile Core/Modules/Ads Module Config")]
    public class AdsModuleConfig : MobileModule
    {
        public override string ModuleName => "Advertisement Module";

        [SerializeField] private AdsSettings settings;
        public AdsSettings Settings => settings;

        [Header("Prefabs")]
        [SerializeField] private GameObject dummyCanvasPrefab;
        public GameObject DummyCanvasPrefab => dummyCanvasPrefab;

        [SerializeField] private GameObject gdprPrefab;
        public GameObject GDPRPrefab => gdprPrefab;

        public override void Initialize(GameObject parent)
        {
            if (settings == null)
            {
                Debug.LogError("[AdsModuleConfig] AdsSettings sub-asset is missing! " +
                               "Select this asset and click 'Repair Sub-Asset' in the Inspector.");
                return;
            }

            AdsManager.Initialize(settings, dummyCanvasPrefab, gdprPrefab);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Called by the editor to embed a fresh AdsSettings as a child of this asset.
        /// </summary>
        public void CreateEmbeddedSettings()
        {
            if (settings != null) return;

            settings = CreateInstance<AdsSettings>();
            settings.name = "AdsSettings";
            UnityEditor.AssetDatabase.AddObjectToAsset(settings, this);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            // If the sub-asset was deleted externally, clear the reference
            if (settings != null && !UnityEditor.AssetDatabase.IsSubAsset(settings))
                settings = null;
        }
#endif
    }
}

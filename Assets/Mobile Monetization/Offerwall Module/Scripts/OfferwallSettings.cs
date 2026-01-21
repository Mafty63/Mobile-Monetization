using UnityEngine;
using System.Collections.Generic;

namespace MobileCore.Offerwall
{
    [System.Serializable]
    public class OfferwallSettings : ScriptableObject
    {
        [Header("General")]
        [SerializeField] private bool testMode = false;
        [SerializeField] private bool showLogs = true;

        public bool TestMode => testMode;
        public bool ShowLogs => showLogs;

        [Header("Tapjoy Settings")]
        [SerializeField] private Providers.Tapjoy.TapjoyContainer tapjoyContainer;
        public Providers.Tapjoy.TapjoyContainer TapjoyContainer => tapjoyContainer;
    }
}

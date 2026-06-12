using System.Threading.Tasks;
using System.Collections;
using MobileCore.SystemModule;
using MobileCore.Utilities;
using UnityEngine;

namespace MobileCore.IAPModule
{
    public class DummyIAPWrapper : BaseIAPWrapper
    {
        private const string DummyIAPKeyPrefix = "DummyIAP_";

        public override void BuyProduct(ProductKeyType productKeyType)
        {
            if (!IAPManager.IsInitialized)
            {
                SystemManager.ShowMessage("Network error. Please try again later");
                IAPManager.NotifyPurchaseFailed(productKeyType, PurchaseFailureReason.Unknown);
                return;
            }

            SystemManager.ChangeLoadingMessage("Payment in progress..");

            // Mengganti Tween.NextFrame dengan Coroutine
            MonoBehaviourExecution.Instance.StartCoroutine(ExecuteAfterFrame(() =>
            {
                IAPItem item = IAPManager.GetIAPItem(productKeyType);
                if (item != null && (item.ProductType == ProductType.NonConsumable || item.ProductType == ProductType.Subscription))
                {
                    PlayerPrefs.SetInt(DummyIAPKeyPrefix + item.ID, 1);
                    PlayerPrefs.Save();
                }

                IAPManager.Log(string.Format("[IAPManager]: Purchasing - {0} is completed!", productKeyType));

                IAPManager.NotifyPurchaseComplete(productKeyType);

                SystemManager.ChangeLoadingMessage("Payment complete!");
                SystemManager.HideLoadingPanel();
            }));
        }

        private IEnumerator ExecuteAfterFrame(System.Action action)
        {
            // Menunggu hingga akhir frame
            yield return new WaitForEndOfFrame();
            action?.Invoke();
        }

        public override ProductData GetProductData(ProductKeyType productKeyType)
        {
            IAPItem iapItem = IAPManager.GetIAPItem(productKeyType);
            if (iapItem != null)
            {
                bool isPurchased = false;
                bool isSubscribed = false;
                if (iapItem.ProductType == ProductType.NonConsumable || iapItem.ProductType == ProductType.Subscription)
                {
                    isPurchased = PlayerPrefs.GetInt(DummyIAPKeyPrefix + iapItem.ID, 0) == 1;
                    isSubscribed = isPurchased && (iapItem.ProductType == ProductType.Subscription);
                }
                return new ProductData(iapItem.ProductType, isPurchased, isSubscribed);
            }

            return null;
        }

        public override void Initialize(IAPSettings settings)
        {
            IAPManager.LogWarning("[IAPManager]: Dummy mode is activated. Configure the module before uploading the game to stores!");
            IAPManager.OnModuleInitialized();
        }

        public override bool IsSubscribed(ProductKeyType productKeyType)
        {
            IAPItem item = IAPManager.GetIAPItem(productKeyType);
            if (item != null && item.ProductType == ProductType.Subscription)
            {
                return PlayerPrefs.GetInt(DummyIAPKeyPrefix + item.ID, 0) == 1;
            }
            return false;
        }

        public override void RestorePurchases()
        {
            if (!IAPManager.IsInitialized)
            {
                SystemManager.ShowMessage("Network error. Please try again later");
                return;
            }

            SystemManager.ShowMessage("Restoring purchases (Dummy Mode)..");

            MonoBehaviourExecution.Instance.StartCoroutine(SimulateDummyRestore());
        }

        private IEnumerator SimulateDummyRestore()
        {
            // Simulate 1.5 second network delay
            yield return new WaitForSeconds(1.5f);

            // In dummy mode, we will just pretend all NonConsumable and Subscription items are being restored
            int restoredCount = 0;
            IAPSettings settings = IAPManager.Settings;

            if (settings != null && settings.StoreItems != null)
            {
                foreach (var item in settings.StoreItems)
                {
                    if (item.ProductType == ProductType.NonConsumable || item.ProductType == ProductType.Subscription)
                    {
                        restoredCount++;
                        PlayerPrefs.SetInt(DummyIAPKeyPrefix + item.ID, 1);
                        IAPManager.Log($"[IAPManager]: Dummy Restore triggering for: {item.ProductKeyType}");
                        IAPManager.NotifyPurchaseComplete(item.ProductKeyType);
                    }
                }
                PlayerPrefs.Save();
            }

            if (restoredCount > 0)
            {
                SystemManager.ShowMessage("Dummy Restoration completed!");
            }
            else
            {
                SystemManager.ShowMessage("No NonConsumable/Subscription items to restore.");
            }
        }
    }
}
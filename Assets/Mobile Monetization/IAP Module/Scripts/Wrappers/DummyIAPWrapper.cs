using System.Threading.Tasks;
using System.Collections;
using MobileCore.SystemModule;
using MobileCore.Utilities;
using UnityEngine;

namespace MobileCore.IAPModule
{
    public class DummyIAPWrapper : BaseIAPWrapper
    {
        public override void BuyProduct(ProductKeyType productKeyType)
        {
            if (!IAPManager.IsInitialized)
            {
                SystemManager.ShowMessage("Network error. Please try again later");
                IAPManager.OnPurchaseFailed(productKeyType, (PurchaseFailureReason)7);
                return;
            }

            SystemManager.ChangeLoadingMessage("Payment in progress..");

            // Mengganti Tween.NextFrame dengan Coroutine
            MonoBehaviourExecution.Instance.StartCoroutine(ExecuteAfterFrame(() =>
            {
                IAPManager.Log(string.Format("[IAPManager]: Purchasing - {0} is completed!", productKeyType));

                IAPManager.OnPurchaseCompled(productKeyType);

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
                return new ProductData(iapItem.ProductType);
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
                        IAPManager.Log($"[IAPManager]: Dummy Restore triggering for: {item.ProductKeyType}");
                        IAPManager.OnPurchaseCompled(item.ProductKeyType);
                    }
                }
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
using System.Threading.Tasks;
using MobileCore.SystemModule;
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
                return;
            }

            SystemManager.ChangeLoadingMessage("Payment in progress..");

            // Mengganti Tween.NextFrame dengan Coroutine
            MonoBehaviourExecution.Instance.StartCoroutine(ExecuteAfterFrame(() =>
            {
                Debug.Log(string.Format("[IAPManager]: Purchasing - {0} is completed!", productKeyType));

                IAPManager.OnPurchaseCompled(productKeyType);

                SystemManager.ChangeLoadingMessage("Payment complete!");
                SystemManager.HideLoadingPanel();
            }));
        }

        private System.Collections.IEnumerator ExecuteAfterFrame(System.Action action)
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
            Debug.LogWarning("[IAP Manager]: Dummy mode is activated. Configure the module before uploading the game to stores!");
            IAPManager.OnModuleInitialized();
        }

        public override bool IsSubscribed(ProductKeyType productKeyType)
        {
            return false;
        }

        public override void RestorePurchases()
        {
            // DO NOTHING
        }
    }
}
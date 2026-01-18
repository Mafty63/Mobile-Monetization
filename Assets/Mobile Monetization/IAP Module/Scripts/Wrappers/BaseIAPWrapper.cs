namespace MobileCore.IAPModule
{
    public abstract class BaseIAPWrapper
    {
        public abstract void Initialize(IAPSettings settings);
        public abstract void RestorePurchases();
        public abstract void BuyProduct(ProductKeyType productKeyType);
        public abstract ProductData GetProductData(ProductKeyType productKeyType);
        public abstract bool IsSubscribed(ProductKeyType productKeyType);
    }
}
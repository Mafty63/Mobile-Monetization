#if MODULE_IAP
using UnityEngine.Purchasing;
#endif

namespace MobileCore.IAPModule
{
    public class ProductData
    {
        public ProductType ProductType { get; }
        public bool IsPurchased { get; }

        public decimal Price { get; }
        public string ISOCurrencyCode { get; }

        public bool IsSubscribed { get; }

#if MODULE_IAP
        public Product Product { get; }
#endif

        public ProductData()
        {
            Price = 0.00m;
            ISOCurrencyCode = "USD";

            IsPurchased = false;

            IsSubscribed = false;
        }

        public ProductData(ProductType productType)
        {
            ProductType = productType;

            Price = 0.00m;
            ISOCurrencyCode = "USD";

            IsPurchased = false;

            IsSubscribed = false;
        }

        public string GetLocalPrice()
        {
            return string.Format("{0} {1}", ISOCurrencyCode, Price);
        }

#if MODULE_IAP
        public ProductData(Product product, bool isPurchased)
        {
            Product = product;

            ProductType = (ProductType)product.definition.type;

            IsPurchased = isPurchased;

            Price = product.metadata.localizedPrice;
            ISOCurrencyCode = product.metadata.isoCurrencyCode;
        }
#endif
    }
}

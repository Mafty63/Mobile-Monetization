#pragma warning disable 0649

using System;

using UnityEngine;
using UnityEngine.UI;

namespace MobileCore.IAPModule.Example
{
    public class IAPManagerExampleScript : MonoBehaviour
    {
        [SerializeField] private Button restoreButton;
        [Space]
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private RectTransform contentParent;

        private void Start()
        {
            InitItems();
        }

        private void InitItems()
        {
#if MODULE_IAP
            ProductKeyType[] values = (ProductKeyType[])Enum.GetValues(typeof(ProductKeyType));
            ItemPanelScript itemPanelScript;
            ProductData product;
            GameObject itemGameObject;

            for (int i = 0; i < values.Length; i++)
            {
                product = IAPManager.GetProductData(values[i]);

                if (product == null)
                {
                    continue;
                }

                itemGameObject = Instantiate(itemPrefab, contentParent);
                itemGameObject.transform.position = new Vector3(0, -200 * i);
                itemPanelScript = itemGameObject.GetComponent<ItemPanelScript>();
                itemPanelScript.Item = values[i];
                itemPanelScript.Type = product.ProductType.ToString();
                itemPanelScript.Name = values[i].ToString();
                itemPanelScript.Price = string.Format("({0} {1})", product.Price, product.ISOCurrencyCode);

                itemPanelScript.SetPurchasedTextActive(product.IsPurchased);

                if (product.IsPurchased)
                {
                    if (product.ProductType == ProductType.Subscription && IAPManager.IsSubscribed(values[i]))
                    {
                        itemPanelScript.Purchased = "(subscribed)";
                    }
                    else if (product.ProductType == ProductType.NonConsumable)
                    {
                        itemPanelScript.Purchased = "(owned)";
                    }
                    else
                    {
                        itemPanelScript.Purchased = "(purchased)";
                    }
                }

                if (product.IsPurchased && product.ProductType != ProductType.Consumable)
                {
                    itemPanelScript.SetButtonInteractable(false);
                }
            }
            contentParent.sizeDelta = new Vector2(contentParent.sizeDelta.x, values.Length * 200);
#else
            Log("IAP Define is disabled!");
#endif
        }

        private void RestoreButton()
        {
            IAPManager.RestorePurchases();
        }

        private void OnEnable()
        {
            restoreButton.onClick.AddListener(RestoreButton);
        }

        private void OnDisable()
        {
            restoreButton.onClick.RemoveListener(RestoreButton);
        }
    }
}

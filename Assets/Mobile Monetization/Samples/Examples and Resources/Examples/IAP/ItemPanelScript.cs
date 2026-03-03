using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace MobileCore.IAPModule.Example
{
    public class ItemPanelScript : MonoBehaviour
    {
        [SerializeField] private Button buyButton;
        [SerializeField] private Text nameText;
        [SerializeField] private Text purchasedText;
        [SerializeField] private Text typeText;
        [SerializeField] private Text priceText;

        ProductKeyType item;

        public ProductKeyType Item { get => item; set => item = value; }
        public string Name { get => nameText.text; set => nameText.text = value; }
        public string Purchased { get => purchasedText.text; set => purchasedText.text = value; }
        public string Type { get => typeText.text; set => typeText.text = value; }
        public string Price { get => priceText.text; set => priceText.text = value; }

        private void OnEnable()
        {
            buyButton.onClick.AddListener(BuyButton);
        }

        private void OnDisable()
        {
            buyButton.onClick.RemoveListener(BuyButton);
        }

        public void SetPurchasedTextActive(bool isActive)
        {
            purchasedText.gameObject.SetActive(isActive);
        }

        private void BuyButton()
        {
            IAPManager.BuyProduct(item);
            IAPManager.OnPurchaseComplete += HandlePurchaseComplete;
        }

        private void HandlePurchaseComplete(ProductKeyType productKeyType)
        {
            if (productKeyType == item)
            {
                SetPurchasedTextActive(true);
            }
        }
    }
}

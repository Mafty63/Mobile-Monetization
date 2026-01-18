# Mobile Monetization System

A modular, extensible system for handling Monetization (Ads & IAP) and Save systems in Unity projects. This package provides unified wrappers around Unity's standard services, making it easier to switch providers or handle initialization logic centrally.

## Features

- **Advertising Module**: 
  - Wrapper for Unity Ads (and potentially others like AdMob).
  - Handles Banner, Interstitial, and Rewarded ads easily.
  - Built-in event handling for rewards.
- **IAP Module**:
  - Wrapper for Unity In-App Purchasing (IAP).
  - Simplified product catalog management.
  - Auto-initialization and transaction restoration.
- **Save Module**:
  - Modular save system to handle game data persistence.
- **Unified Settings**: Centralized configuration for all modules.

## Installation

### Option 1: Install via Git URL
1. Open the Unity Package Manager (**Window > Package Manager**).
2. Click the **+** button in the top-left corner.
3. Select **Add package from git URL...**.
4. Enter the git URL of this repository:
   ```
   https://github.com/YourUsername/YourRepoName.git?path=/Assets/Mobile Monetization
   ```
   *(Note: Ensure you point to the folder containing package.json if it's in a subdirectory)*

## Dependencies

This package relies on the following Unity Service packages. They should be installed automatically or manually via Package Manager:

- **Unity Purchasing** (`com.unity.purchasing`)
- **Unity Advertisement** (`com.unity.ads`)

## Usage

### Initialization
The system is designed to initialize automatically or via a central manager. Ensure your `ProjectSettings` are configured with the correct keys for Ads and IAP.

### Ads Example
```csharp
// Show a rewarded ad
AdsManager.ShowRewardedAd(onComplete: () => {
    Debug.Log("Ad finished! Reward the player.");
    // Grant reward logic here
}, onFail: () => {
    Debug.Log("Ad skipped or failed.");
});
```

### IAP Example
```csharp
// Purchase a product
IAPManager.PurchaseProduct(ProductKeyType.Coin500, (success, product) => {
    if (success) {
        Debug.Log("Purchase Successful!");
    } else {
        Debug.Log("Purchase Failed.");
    }
});
```
*(Note: Code snippets are illustrative based on common patterns in this package).*

## Directory Structure
- `Advertisement Module/`: Scripts and resources for Ads.
- `IAP Module/`: Scripts and resources for In-App Purchases.
- `Save Module/`: Serialization and save logic.
- `Main System/`: Core managers and initialization logic.
- `Example/`: Demo scenes and scripts showing usage.

# Mobile Monetization System

A modular, extensible system for handling Monetization (Ads & IAP) and System utilities (Loading, Saves, Device Settings) in Unity projects. This package serves as a unified wrapper around standard Unity services and 3rd party SDKs.

## Features

### 1. Advertisement Module
Unified interface for multiple ad networks (AdMob, Unity Ads, IronSource, AppLovin, etc.).
- **Features**: Banner, Interstitial, Rewarded Video.
- **Handling**: Auto-reloading, delay logic, forced ad disabling (NoAds), GDPR, and iOS IDFA support.
- **API**:
  ```csharp
  // Show Banner
  AdsManager.ShowBanner();
  
  // Show Interstitial with callback
  AdsManager.ShowInterstitial((success) => {
      Debug.Log($"Ad Closed. Success: {success}");
  });

  // Show Rewarded Video
  AdsManager.ShowRewardBasedVideo((success) => {
      if (success) GrantReward();
  });
  ```

### 2. IAP Module
Wrapper for Unity IAP to simplify product management and purchase flows.
- **Features**: Consumables, Non-Consumables, Subscriptions.
- **Handling**: Auto-initialization, Restore Purchases (iOS).
- **API**:
  ```csharp
  // Buy Product
  IAPManager.BuyProduct(ProductKeyType.NoAds);

  // Listen for completion
  IAPManager.OnPurchaseComplete += (productKey) => {
      if (productKey == ProductKeyType.NoAds) {
          AdsManager.DisableForcedAd();
      }
  };
  ```

### 3. System Module
Generic system utilities for UI feedback and device settings.
- **Features**: Toast Messages, Loading Overlay, Frame Rate & Sleep Timeout management.
- **API**:
  ```csharp
  // Show Toast
  SystemManager.ShowMessage("Hello World!");

  // Loading Screen
  SystemManager.ShowLoadingPanel();
  SystemManager.HideLoadingPanel();
  ```

### 4. Main System
The backbone of the package.
- **Features**: Ensures all modules (Ads, IAP, System) are initialized in the correct order.
- **Setup**: Add `MainSystemManager` to your first scene and assign `MainSystemSettings`.

### 5. Define System
Automated management of Scripting Define Symbols based on SDK existence.
- **Usage**: Add `[Define("SYMBOL", "Namespace.Class")]` to your scripts.
- **Editor**: Use `MobileCore > Define Symbols Manager` to refresh symbols.

---

## Installation

This package is designed to be installed as a local or git package.

- **Option A (Git)**: In Package Manager, add package from git URL:
  `https://your-repo-url.git?path=/Assets/Mobile Monetization`
- **Option B (Local)**: Drop the `Mobile Monetization` folder into your `Packages` (or `Assets`) directory.

**Note**: The `package.json` file must remain at the root of the `Mobile Monetization` folder for Unity to recognize it as a package.

## Setup Guide

1. **Create Settings**:
   - Create `AdsSettings`, `IAPSettings`, and `MainSystemSettings` in your Resources or Settings folder.
   - Link `AdsSettings` and `IAPSettings` into their respective Initializers.
2. **Main Manager**:
   - Create a GameObject (e.g., `MainSystem`).
   - Add `MainSystemManager` component.
   - Assign `MainSystemSettings`. Only `SystemModuleInitializer` is required as Core; add others (Ads, IAP) to the "Modules" list.
3. **Run**:
   - The system marks itself `DontDestroyOnLoad` and persists throughout the game.

## Directory Structure

- `Advertisement Module/`: Scripts, IDFA, Providers.
- `IAP Module/`: Scripts, Product Definitions, Wrappers.
- `System Module/`: UI Managers, Screen Settings.
- `Main System/`: Initialization logic.
- `Define System/`: Editor tools for define symbols.
- `Documentation/`: Additional guides.

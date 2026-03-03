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

### Option 1: Install via Unity Package (Recommended)
1. Go to the [**Releases**](https://github.com/Mafty63/Mobile-Monetization/releases) page.
2. Download the latest `MobileMonetization-vX.X.X.unitypackage` file.
3. In your Unity project, go to **Assets > Import Package > Custom Package...**.
4. Select the downloaded `.unitypackage` file and click **Import**.

> 💡 **Note:** Files will be imported directly into your `Assets/` folder, so you can freely edit them to fit your project.

### Option 2: Manual Installation (via Clone)
1. Download or Clone this repository.
2. Copy the entire `Assets/Mobile Monetization` folder.
3. Paste it into your project's `Assets` folder.

## Quick Start

### Step 1: Import the Package
After downloading and importing the `.unitypackage`, you will find everything you need under `Assets/Mobile Monetization/`:
- ✅ **Prefabs** - Initializer, System Canvas, GDPR Panel, UI components
- ✅ **Settings** - Pre-configured AdsSettings, IAPSettings, MainSystemSettings
- ✅ **Example Scenes** - Demo implementation of Ads, IAP, and Offerwall
- ✅ **Plugin Resources** - All necessary runtime assets

> 💡 **Note:** All imported files are fully editable. Feel free to delete example scenes after learning the API to reduce build size.

### Step 2: Configure Settings

1. Navigate to `Assets/Mobile Monetization/Samples/Examples and Resources/Settings/`.
2. Customize the settings for your project:
   - **AdsSettings**: Configure Ad Provider IDs (AdMob, Unity Ads, etc.)
   - **IAPSettings**: Add your IAP Product IDs
   - **MainSystemSettings**: Link the module initializers

### Step 3: Setup Main System Manager

1. Drag the **Initializer** prefab from `.../Prefabs/` into your first scene.
2. Verify the Initializer has `MainSystemSettings` assigned.
3. In `MainSystemSettings`, ensure:
   - **Core Module** → `SystemModuleInitializer`
   - **Modules** → Add `AdsManagerInitializer`, `IAPManagerInitializer` as needed

### Step 4: Test
Press Play! The system will initialize automatically. Check Console for initialization logs if `System Logs` is enabled.

## Documentation

For more complete and detailed documentation, please visit:
**[Mobile Monetization - Getting Started Guide](https://quick-setup-website.pages.dev/documentation/mobile-monetization/getting-started/)**

## Directory Structure

- `Advertisement Module/`: Scripts, IDFA, Providers.
- `IAP Module/`: Scripts, Product Definitions, Wrappers.
- `System Module/`: UI Managers, Screen Settings.
- `Main System/`: Initialization logic.
- `Define System/`: Editor tools for define symbols.
- `Documentation/`: Additional guides.

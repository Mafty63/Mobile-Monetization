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

This package is hosted inside a full Unity project repository. To install it properly via Package Manager, you **must** specify the subfolder path.

### Option 1: Install via Git URL (Recommended)
1. Open Unity Package Manager (**Window > Package Manager**).
2. Click the **+** button and select **Add package from git URL...**.
3. Enter the following URL **exactly**:
   ```
   https://github.com/Mafty63/Mobile-Monetization.git?path=/Assets/Mobile Monetization
   ```
   > ⚠️ **Important:** You must include the `?path=/Assets/Mobile Monetization` suffix. If you omit this, Unity will fail to find the `package.json` and give an error.

### Option 2: Local Installation
1. Download or Clone this repository.
2. Copy the entire `Assets/Mobile Monetization` folder.
3. Paste it into your project's `Packages` folder (recommended) or `Assets` folder.

## Quick Start

### Step 1: Import Sample Package (Essential)
After installing the package, you need to import the sample to get all essential resources:

1. Open **Window > Package Manager**.
2. Find **Mobile Monetization** in the package list.
3. In the right panel, click on the **Samples** tab.
4. Click **Import** on **"Examples and Resources"**.

This will copy everything you need to `Assets/Samples/Mobile Monetization/[version]/Examples and Resources/`:
- ✅ **Prefabs** - Initializer, System Canvas, GDPR Panel, UI components
- ✅ **Settings** - Pre-configured AdsSettings, IAPSettings, MainSystemSettings
- ✅ **Example Scenes** - Demo implementation of Ads, IAP, and Offerwall
- ✅ **Plugin Resources** - All necessary runtime assets

> 💡 **Note:** All imported files are fully editable. You can delete example scenes after learning the API to reduce build size.

### Step 2: Configure Settings

1. Navigate to `Assets/Samples/Mobile Monetization/[version]/Examples and Resources/Settings/`.
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

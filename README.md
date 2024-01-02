# XamarinCommonAndroidServices
Xamarin implementation of Common Android Services:
- Interstitial Ads version 122.3.0
- Firebase Analytics version 121.3.0.4
- In-App-Purchases supporting Billing library 5.2.0

### Build Status:

master | dev
------------ | -------------
[![Build Status](https://dev.azure.com/cn-github-builds/GitHub%20Builds/_apis/build/status/companova.XamarinCommonAndroidServices?branchName=main)](https://dev.azure.com/cn-github-builds/GitHub%20Builds/_build/latest?definitionId=3&branchName=main)|[![Build Status](https://dev.azure.com/cn-github-builds/GitHub%20Builds/_apis/build/status/companova.XamarinCommonAndroidServices?branchName=dev)](https://dev.azure.com/cn-github-builds/GitHub%20Builds/_build/latest?definitionId=3&branchName=dev)

### Setup:
<a href="https://www.nuget.org/packages/Companova.Xamarin.Common.Android.Services/">
  <img alt="Nuget" src="https://img.shields.io/nuget/v/Companova.Xamarin.Common.Android.Services">
</a>

**Available on NuGet:** [Companova.Xamarin.Common.Android.Services](https://www.nuget.org/packages/Companova.Xamarin.Common.Android.Services/)

# Test App
### Refer to Test App for detailed usage patterns
https://github.com/companova/XamarinCommonAndroidServices/tree/dev/test

How to setup:
- Ensure your Application is published to Google Play and registered in Firebase.
- Update google-services.json with your Application google-services.json file from Firebase
- Replace package name 'com.company.appname' in AndroidManifest.xml to your application name
- If you wish to test Interstitial Ads with your Application Ad Ids, replace AdMob Test ApplicationId and ```_interstitialAdUnitId``` with your Application specific values.

## Interstitial Ads

```csharp
private IInterstitialService _interstitialService => CrossAndroidServices.InterstitialService;

private async Task InitializeAds()
{
   // Interstitial Ads
   _interstitialService.Initialize(true, _interstitialAdUnitId, null);
   await _interstitialService.LoadInterstitialAsync();
}
```
## Firebase Analytics

```csharp
// Initialize Firebase Analytics
FirebaseAnalytics firebaseAnalytics = FirebaseAnalytics.GetInstance(Application);

//Set Firebase to the Analytics Service
CrossAndroidServices.AnalyticsService.SetFirebaseAnalytics(firebaseAnalytics);

// Use Analytics to Log Event
CrossAndroidServices.AnalyticsService.LogEvent("test_event");
```

## In-App-Purchases 

```csharp
public async Task InitializeInAppPurchaseAsync()
{
  _inAppPurchaseService = CrossAndroidServices.InAppPurchaseService;
  
  // Set Current Activity
  _inAppPurchaseService.SetActivity(this);
  
  // Connect to the Billing Client
  await _inAppPurchaseService.StartAsync();

  // First, get the products 
  IEnumerable<Product> products = await _inAppPurchaseService.LoadProductsAsync(new string[] { productId }, ProductType.NonConsumable);

  // Initiate the purchase process
  InAppPurchaseResult purchase = await _inAppPurchaseService.PurchaseAsync(productId);

  // If the product got purchased and is not yet acknowledged,
  // then activate the product. Otherwise, Google refunds it.
  // From: https://developer.android.com/google/play/billing/integrate#acknowledge
  // If you do not acknowledge a purchase within three days, the user automatically receives a refund, and Google Play revokes the purchase.
  if (purchase.State == PurchaseState.Purchased && !purchase.Acknowledged)
  {
     await _inAppPurchaseService.FinalizePurchaseAsync(purchase.PurchaseToken, ProductType.NonConsumable);
  }
}
```

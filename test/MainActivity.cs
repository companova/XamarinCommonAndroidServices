using System;
using System.Timers;
using System.Threading.Tasks;
using System.Collections.Generic;

using Android.OS;
using Android.Widget;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.AppCompat.App;

using Google.Android.Material.Snackbar;
using Google.Android.Material.FloatingActionButton;

using Firebase.Analytics;

using Companova.Xamarin.Common.Android.Services;

namespace TestApp
{
    [Android.App.Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private const string _tag = "MainActivity";
        // TODO: Replace with your Ad Unit Id
        private const string _interstitialAdUnitId = "ca-app-pub-4626693327904217/8710621545";

        private bool _firebaseInitialized = false;
        private bool _inAppPuchaseInitialized = false;

        private IInAppPurchaseService _inAppPurchaseService = null;

        private Timer _uiInitTimer = null;

        // Controls:
        private Button _btnShowAd;
        private Button _btnLogEvent;
        private Button _btnPurchase;
        private Button _btnRestore;
        /// <summary>
        /// Cross Platform Interstitial Ad Service
        /// </summary>
        private IInterstitialService _interstitialService => CrossAndroidServices.InterstitialService;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            // Get Controls
            SetupControls();

            // Initialize the MobileAd (not sure if it is needed)
            Android.Gms.Ads.MobileAds.Initialize(Android.App.Application.Context);

            // Set UI Timer to initialize Interstitial Ads with some delay
            _uiInitTimer = new Timer();
            _uiInitTimer.Interval = 100;    // 100ms
            _uiInitTimer.Elapsed += OnUIInitializeTimerTick;
            _uiInitTimer.Enabled = true;
            _uiInitTimer.Start();
        }

        protected override async void OnDestroy()
        {
            base.OnDestroy();

            try
            {
                // Stop the Billing Connection
                if (_inAppPuchaseInitialized)
                    await _inAppPurchaseService.StopAsync();
            }
            catch
            {
            }

            if (_btnShowAd != null)
                _btnShowAd.Click -= OnShowAdClick;

            if (_btnLogEvent != null)
                _btnLogEvent.Click -= OnLogEventClick;

            if (_btnPurchase != null)
                _btnPurchase.Click -= OnPurchaseClick;

            if (_btnRestore != null)
                _btnRestore.Click -= OnRestoreClick;
        }

        private void SetupControls()
        {
            _btnShowAd = FindViewById<Button>(Resource.Id.showAdButton);
            _btnLogEvent = FindViewById<Button>(Resource.Id.logEventButton);
            _btnPurchase = FindViewById<Button>(Resource.Id.purchaseButton);
            _btnRestore = FindViewById<Button>(Resource.Id.restoreButton);

            _btnShowAd.Click += OnShowAdClick;
            _btnLogEvent.Click += OnLogEventClick;
            _btnPurchase.Click += OnPurchaseClick;
            _btnRestore.Click += OnRestoreClick;
        }

        private void OnUIInitializeTimerTick(object sender, ElapsedEventArgs e)
        {
            RunOnUiThread(async () =>
            {
                // Initialize UI components (e.g. Grid or Ads), so the game page UI loads up quicker
                await InitializeUI();
            });
        }

        /// <summary>
        /// Initializes UI components after the page was loaded. This way the page loads faster
        /// 1. Initialize Ads
        /// </summary>
        private async Task InitializeUI()
        {
            // Stop the UI Initialize Timer
            _uiInitTimer.Enabled = false;
            _uiInitTimer.Stop();

            // Interstitial Ads
            _interstitialService.Initialize(true, _interstitialAdUnitId, null);
            await _interstitialService.LoadInterstitialAsync();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View)sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (View.IOnClickListener)null).Show();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private async void OnShowAdClick(object sender, EventArgs e)
        {
            // Show Interstitial Ad
            if (_interstitialService.IsReadyToShow())
            {
                await _interstitialService.ShowInterstitialAsync(this);

                // Reload the Ad
                await _interstitialService.LoadInterstitialAsync();
            }
            else
            {
                View view = (View)sender;
                Snackbar.Make(view, "Ad has not been loaded yet. Try again later", Snackbar.LengthLong)
                    .SetAction("Action", (View.IOnClickListener)null).Show();

                // Try to Load the Ad
                await _interstitialService.LoadInterstitialAsync();
            }
        }

        private void OnLogEventClick(object sender, EventArgs e)
        {
            // Log Event 
            try
            {
                InitializeFirebase();

                // Use Analytics to Log Event
                CrossAndroidServices.AnalyticsService.LogEvent("test_event");

                View view = (View)sender;
                Snackbar.Make(view, "Event has been logged.", Snackbar.LengthLong)
                    .SetAction("Action", (View.IOnClickListener)null).Show();
            }
            catch (Exception ex)
            {
                View view = (View)sender;
                Snackbar.Make(view, ex.ToString(), Snackbar.LengthLong)
                    .SetAction("Action", (View.IOnClickListener)null).Show();
            }
        }

        /// <summary>
        /// Initializes Firebase. Usually runs on the start up. For example, in Application.OnCreate method
        /// </summary>
        private void InitializeFirebase()
        {
            if (_firebaseInitialized)
                return;

            // Initialize Firebase Analytics
            FirebaseAnalytics firebaseAnalytics = FirebaseAnalytics.GetInstance(Application);

            //Set Firebase to the Analytics Service
            CrossAndroidServices.AnalyticsService.SetFirebaseAnalytics(firebaseAnalytics);

            _firebaseInitialized = true;
        }

        private async void OnPurchaseClick(object sender, EventArgs e)
        {
            // Make sure you set you product Id
            string productId = "noads";

            try
            {
                // Initialize Billing Client
                await InitializeInAppPurchaseAsync();

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
            catch (InAppPurchaseException iapEx)
            {
                Log.Debug(_tag, $"PurchaseError: {iapEx.PurchaseError}, Message: {iapEx.Message}");

                // If User canceled the transaction, then there is no need to show the error
                if (iapEx.PurchaseError != PurchaseError.UserCancelled)
                {
                    AlertDialog.Builder dialog = new AlertDialog.Builder(this);

                    dialog.SetTitle(productId);
                    dialog.SetMessage("Failed to Purchase" + System.Environment.NewLine + iapEx.Message);
                    dialog.SetPositiveButton("OK", delegate { });

                    // Show the alert dialog to the user and wait for response.
                    dialog.Show();
                }

                // TODO: Add if (iapEx.PurchaseError != PurchaseError.AlreadyOwned)
                // TODO: Either display a message to Restore, or autorestore
            }
            catch (Exception ex)
            {
                Log.Debug(_tag, ex.ToString());

                AlertDialog.Builder dialog = new AlertDialog.Builder(this);

                dialog.SetTitle(productId);
                dialog.SetMessage("Failed to Purchase");
                dialog.SetPositiveButton("OK", delegate { });

                // Show the alert dialog to the user and wait for response.
                dialog.Show();
            }
        }

        private async void OnRestoreClick(object sender, EventArgs e)
        {
            try
            {
                // Initialize Billing Client
                await InitializeInAppPurchaseAsync();

                // Initiate the purchase process
                List<InAppPurchaseResult> purchases = await _inAppPurchaseService.RestoreAsync(ProductType.NonConsumable);

                if (purchases == null || purchases.Count == 0)
                {
                    // Nothing to Restore.

                    AlertDialog.Builder dialog = new AlertDialog.Builder(this);

                    dialog.SetTitle("Restore Failed");
                    dialog.SetMessage("Nothing to Restore");
                    dialog.SetPositiveButton("OK", delegate { });

                    // Show the alert dialog to the user and wait for response.
                    dialog.Show();

                    return;
                }

                foreach (InAppPurchaseResult purchase in purchases)
                {
                    // If the product got purchased and is not yet acknowledged,
                    // then activate the product. Otherwise, Google refunds it.
                    // From: https://developer.android.com/google/play/billing/integrate#acknowledge
                    // If you do not acknowledge a purchase within three days, the user automatically receives a refund, and Google Play revokes the purchase.
                    if (purchase.State == PurchaseState.Purchased && !purchase.Acknowledged)
                    {
                        await _inAppPurchaseService.FinalizePurchaseAsync(purchase.PurchaseToken, ProductType.NonConsumable);
                    }

                    // Restored the Product
                    AlertDialog.Builder dialog = new AlertDialog.Builder(this);

                    dialog.SetTitle("Restored");
                    dialog.SetMessage($"Restored {purchase.ProductId}");
                    dialog.SetPositiveButton("OK", delegate { });

                    // Show the alert dialog to the user and wait for response.
                    dialog.Show();
                }
            }
            catch (InAppPurchaseException iapEx)
            {
                Log.Debug(_tag, $"PurchaseError: {iapEx.PurchaseError}, Message: {iapEx.Message}");

                // If User canceled the transaction, then there is no need to show the error
                if (iapEx.PurchaseError != PurchaseError.UserCancelled)
                {
                    AlertDialog.Builder dialog = new AlertDialog.Builder(this);

                    dialog.SetTitle("Failed");
                    dialog.SetMessage("Restore Failed" + System.Environment.NewLine + iapEx.Message);
                    dialog.SetPositiveButton("OK", delegate { });

                    // Show the alert dialog to the user and wait for response.
                    dialog.Show();
                }
            }
            catch (Exception ex)
            {
                Log.Debug(_tag, ex.ToString());

                AlertDialog.Builder dialog = new AlertDialog.Builder(this);

                dialog.SetTitle("Failed");
                dialog.SetMessage("Restore Failed");
                dialog.SetPositiveButton("OK", delegate { });

                // Show the alert dialog to the user and wait for response.
                dialog.Show();
            }
        }

        public async Task InitializeInAppPurchaseAsync()
        {
            if (_inAppPuchaseInitialized)
                return;

            _inAppPurchaseService = CrossAndroidServices.InAppPurchaseService;
            // Set Current Activity
            _inAppPurchaseService.SetActivity(this);
            // Connect to the Billing Client
            await _inAppPurchaseService.StartAsync();

            // Mark as initialized.
            _inAppPuchaseInitialized = true;
        }
    }
}

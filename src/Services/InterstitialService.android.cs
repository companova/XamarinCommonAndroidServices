using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Android.Gms.Ads;
using Android.Gms.Ads.Interstitial;

using Android.App;
using Android.Runtime;

using Companova.Xamarin.Common.Android.Helpers;

namespace Companova.Xamarin.Common.Android.Services
{
    /// <summary>
    /// See: https://docs.microsoft.com/en-us/answers/questions/540463/xamarinandroid-admob-v20-intertitialads-problem.html
    /// </summary>
    [Preserve(AllMembers = true)]
    public abstract class InterstitialCallback : InterstitialAdLoadCallback
    {
        /// <summary>
        /// Callback by Ads
        /// </summary>
        /// <param name="interstitialAd">InterstitialAd</param>
        [Register("onAdLoaded", "(Lcom/google/android/gms/ads/interstitial/InterstitialAd;)V", "GetOnAdLoadedHandler")]
        public virtual void OnAdLoaded(InterstitialAd interstitialAd)
        {
        }
        private static Delegate cb_onAdLoaded;
        private static Delegate GetOnAdLoadedHandler()
        {
            if (cb_onAdLoaded is null)
                cb_onAdLoaded = JNINativeWrapper.CreateDelegate((Action<IntPtr, IntPtr, IntPtr>)n_onAdLoaded);
            return cb_onAdLoaded;
        }
        private static void n_onAdLoaded(IntPtr jnienv, IntPtr native__this, IntPtr native_p0)
        {
            InterstitialCallback thisobject = GetObject<InterstitialCallback>(jnienv, native__this, JniHandleOwnership.DoNotTransfer);
            InterstitialAd resultobject = GetObject<InterstitialAd>(native_p0, JniHandleOwnership.DoNotTransfer);
            thisobject.OnAdLoaded(resultobject);
        }
    }

    /// <summary>
    /// Interstitial Ad Service implementation on Android
    /// </summary>
    [Preserve(AllMembers = true)]
    public class InterstitialService : InterstitialCallback, IInterstitialService
    {
        private const string InterstitialAdLoadError = "ad_int_load_error";
        internal const string InterstitialAdShowError = "ad_int_show_error";

        //public static InterstitialService Instance { get; } = new InterstitialService();

        // Indicates that the ad is being loaded
        private bool _isLoading;

        // Ad Unit
        private string _adUnitId;

        // Interstitial Ad Instance
        private InterstitialAd _adInterstitial;

        // Lister object to react to Ad events
        private InterstitialAdServiceFullScreenContentCallback _adCallback;

        // Task Source which gets sets/complete when the Ad closes
        private TaskCompletionSource<object> _adClosed;

        // Analytics Service (if any)
        private IAnalyticsService _analyticsService;

        private bool _isEnabled;
        /// <summary>
        /// Enables to Disables Ads. E.g. when NoAds In-App-Purchase is purchased
        /// </summary>
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
            }
        }

        /// <summary>
        /// Default Constructor for Interstitial Service Implementation on Android
        /// </summary>
        public InterstitialService()
        {
            _isEnabled = false;
            _isLoading = false;
            _adInterstitial = null;
            _analyticsService = null;
        }

        /// <summary>
        /// Initializes the Ad Service
        /// </summary>
        /// <param name="enabled">Is it Enabled</param>
        /// <param name="unitId">Ad Unit Id</param>
        /// <param name="analyticsService">Analytics Service (if any)</param>
        public void Initialize(bool enabled, string unitId, IAnalyticsService analyticsService)
        {
            _isEnabled = enabled;
            _adUnitId = unitId;
            _analyticsService = analyticsService;
        }

        /// <summary>
        /// Requests to loads a new Ad.
        /// </summary>
        /// <returns>Task</returns>
        public Task LoadInterstitialAsync()
        {
            // Do not Load if not Enabled.
            if (!_isEnabled)
                return Task.CompletedTask;

            try
            {
                // Return if already loaded and ready to show (or being loaded)
                if (_adInterstitial != null || _isLoading)
                    return Task.CompletedTask;

                // Indicate that the ad is being loaded to prevent doubnle loading at the same time.
                _isLoading = true;

                // Build Request and Load Ad
                AdRequest request = new AdRequest.Builder().Build();
                InterstitialAd.Load(Application.Context, _adUnitId, request, this);
            }
            catch (Exception ex)
            {
                ExceptionHelper.Publish(ex, "LoadInterstitial", _analyticsService);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Displays the Ad. 
        /// </summary>
        /// <param name="activity">Activity</param>
        /// <returns>Task which completes when the Ad closes</returns>
        public Task ShowInterstitialAsync(Activity activity)
        {
            // Do not attempt to show if not enabled or not ready (Interstitial is not loaded)
            if (!_isEnabled || _adInterstitial == null)
                return Task.CompletedTask;

            try
            {
                // Set the Completion Source
                _adClosed = new TaskCompletionSource<object>();
                _adCallback.AdClosed = _adClosed;

                // Show the Ad
                _adInterstitial.Show(activity);

                // Wait till it is closed.
                Task closedTask = _adClosed.Task;

                // Reset the ad and callback instances 
                _adInterstitial = null;
                _adCallback = null;

                System.Diagnostics.Debug.WriteLine("After await _adClosed.Task");

                return closedTask;
            }
            catch (Exception ex)
            {
                ExceptionHelper.Publish(ex, "ShowInterstitial", _analyticsService);

                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Returns true if the Ad was loaded and ready to be showed
        /// </summary>
        /// <returns>true/false</returns>
        public bool IsReadyToShow()
        {
            // Return true if the Interstitial Ad instance is reday to show
            return _adInterstitial != null && !_isLoading;
        }

        /// <summary>
        /// Callback when Ad is loaded
        /// </summary>
        /// <param name="adInterstitial">InterstitialAd</param>
        public override void OnAdLoaded(InterstitialAd adInterstitial)
        {
            base.OnAdLoaded(adInterstitial);

            // Done loading
            _isLoading = false;

            // Create and set a callback for FullScreen Display
            _adCallback = new InterstitialAdServiceFullScreenContentCallback(_analyticsService);

            _adInterstitial = adInterstitial;
            _adInterstitial.FullScreenContentCallback = _adCallback;
        }

        /// <summary>
        /// Callback when Ad failed to load
        /// </summary>
        /// <param name="error">Error</param>
        public override void OnAdFailedToLoad(LoadAdError error)
        {
            base.OnAdFailedToLoad(error);

            // Done loading
            _isLoading = false;
            // Didn't succeeded. Ensure the Ad instance is null
            _adInterstitial = null;

            try
            {
                // Debug output
                System.Diagnostics.Debug.WriteLine($"Description {error?.Cause?.Message}, Code: {error?.Code}, " +
                    $"Domain: {error?.Domain}, LocalizedDescription: {error?.Message}, Domain: {error?.Domain}");

                // Allocate properties all the time
                IDictionary<string, string> properties = new Dictionary<string, string>();

                // Error Code, Description and Domain
                properties.Add("ad_error_code", error?.Code.ToString());
                // Report Exception Stack and Details
                if (error?.Cause?.Message != null)
                    properties.Add("ad_error_loc", error?.Cause?.Message);

                if (error?.Message != null)
                    properties.Add("ad_error_desc", error?.Message);

                if (error?.Domain != null)
                    properties.Add("ad_error_domain", error?.Domain);

                // Log to Analytics
                _analyticsService?.LogEvent(InterstitialAdLoadError, properties);
            }
            catch (Exception ex)
            {
                ExceptionHelper.Publish(ex, "OnAdFailedToLoad", _analyticsService);
            }
        }
    }

    [Preserve(AllMembers = true)]
    internal class InterstitialAdServiceFullScreenContentCallback : FullScreenContentCallback
    {
        public TaskCompletionSource<object> AdClosed;

        // Analytics Service (if any)
        private IAnalyticsService _analyticsService;

        internal InterstitialAdServiceFullScreenContentCallback(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        public override void OnAdDismissedFullScreenContent()
        {
            System.Diagnostics.Debug.WriteLine("OnAdDismissedFullScreenContent");

            try
            {
                // Set the Task, so we know the user closed the Ad
                AdClosed.TrySetResult(null);
            }
            catch (Exception ex)
            {
                ExceptionHelper.Publish(ex, "OnAdClosed", _analyticsService);
            }

            base.OnAdDismissedFullScreenContent();
        }

        public override void OnAdShowedFullScreenContent()
        {
            base.OnAdShowedFullScreenContent();
        }

        public override void OnAdFailedToShowFullScreenContent(AdError error)
        {
            try
            {
                // Debug output
                System.Diagnostics.Debug.WriteLine($"Description {error?.Cause?.Message}, Code: {error?.Code}, " +
                    $"Domain: {error?.Domain}, LocalizedDescription: {error?.Message}, Domain: {error?.Domain}");

                // Allocate properties all the time
                IDictionary<string, string> properties = new Dictionary<string, string>();

                // Error Code, Description and Domain
                properties.Add("ad_error_code", error?.Code.ToString());
                // Report Exception Stack and Details
                if (error?.Cause?.Message != null)
                    properties.Add("ad_error_loc", error?.Cause?.Message);

                if (error?.Message != null)
                    properties.Add("ad_error_desc", error?.Message);

                if (error?.Domain != null)
                    properties.Add("ad_error_domain", error?.Domain);

                // Log to Analytics
                _analyticsService?.LogEvent(InterstitialService.InterstitialAdShowError, properties);
            }
            catch (Exception ex)
            {
                ExceptionHelper.Publish(ex, "OnAdFailedToLoad", _analyticsService);
            }
            finally
            {
                // Set the Task, so we know the user closed the Ad
                AdClosed?.TrySetResult(null);
            }

            base.OnAdFailedToShowFullScreenContent(error);
        }
    }
}
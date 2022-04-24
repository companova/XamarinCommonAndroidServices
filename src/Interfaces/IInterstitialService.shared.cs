using System;
using System.Threading.Tasks;

using Android.App;

namespace Companova.Xamarin.Common.Android.Services
{
    /// <summary>
    /// Interstitial Ad Service Interface
    /// </summary>
    public interface IInterstitialService
    {
        /// <summary>
        /// Enables to Disables Ads. E.g. when NoAds In-App-Purchase is purchased
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Initializes the Ad Service
        /// </summary>
        /// <param name="enabled">Is it Enabled</param>
        /// <param name="unitId">Ad Unit Id</param>
        /// <param name="analyticsService">Analytics Service (if any)</param>
        void Initialize(bool enabled, string unitId, IAnalyticsService analyticsService);

        /// <summary>
        /// Returns true if the Ad was loaded and ready to be showed
        /// </summary>
        /// <returns>true/false</returns>
        bool IsReadyToShow();

        /// <summary>
        /// Requests to loads a new Ad.
        /// </summary>
        /// <returns>Task</returns>
        Task LoadInterstitialAsync();

        /// <summary>
        /// Displays the Ad. 
        /// </summary>
        /// <param name="activity">Activity</param>
        /// <returns>Task which completes when the Ad closes</returns>
        Task ShowInterstitialAsync(Activity activity);
    }
}

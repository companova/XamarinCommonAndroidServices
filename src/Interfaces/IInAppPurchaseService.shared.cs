using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Android.App;

namespace Companova.Xamarin.Common.Android.Services
{
    /// <summary>
    /// InAppPurchaseService Interface
    /// </summary>
    public interface IInAppPurchaseService
    {
        /// <summary>
        /// Sets Activity for the Billing Client
        /// </summary>
        /// <param name="activity">Activity instance</param>
        void SetActivity(Activity activity);

        /// <summary>
        /// Initializes and Connects to Billing Client. Needs to be called in the Activity OnCreate
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Disconnects BillingClient. Needs to be called in the Activity OnDestroy
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Returns product info for the given Product Ids
        /// </summary>
        /// <param name="productIds">Product Ids</param>
        /// <param name="productType">Product Type (either BillingClient.SkuType.Inapp or BillingClient.SkuType.Subs)</param>
        /// <returns></returns>
        Task<IEnumerable<Product>> LoadProductsAsync(string[] productIds, ProductType productType);

        /// <summary>
        /// Determines whether the User can make payments on the given device.
        /// Always returns true for Android Devices
        /// </summary>
        /// <returns>true/false</returns>
        bool CanMakePayments();

        /// <summary>
        /// Initializes an async process to purchase the product. Only one purchase request can be happening at a time
        /// </summary>
        /// <param name="productId">Product to buy</param>
        /// <returns>Purchase object</returns>
        Task<InAppPurchaseResult> PurchaseAsync(string productId);

        /// <summary>
        /// Restores all previous purchases
        /// </summary>
        /// <param name="productType">Product Type (inapp or subs)</param>
        /// <returns>An array of previous purchases</returns>
        Task<List<InAppPurchaseResult>> RestoreAsync(ProductType productType);
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android.App;
using Android.Util;
using Android.Runtime;

using Android.BillingClient.Api;

namespace Companova.Xamarin.Common.Android.Services
{
    /// <summary>
    /// Key class that provides access to StoreKit in-app-purchases features.
    /// Call:
    /// - Start to add an Observer to the Queue
    /// - Stop to remove the Observer from the Queue
    /// - LoadProductsAsync to retrieve a list of Products
    /// - CanMakePayment to check if the user can buy in-app-purchases
    /// - PurchaseAsync to buy a product
    /// - RestoreAsync to restore previously bought products
    /// </summary>
    [Preserve(AllMembers = true)]
    public class InAppPurchaseService : Java.Lang.Object, IInAppPurchaseService, 
        IPurchasesUpdatedListener, IBillingClientStateListener,
        IPurchaseHistoryResponseListener, IPurchasesResponseListener
    {

        #region IBillingClientStateListener
        /// <summary>
        /// Callback used by Billing Client
        /// </summary>
        /// <param name="result">BillingResult</param>
        public void OnBillingSetupFinished(BillingResult result)
        {
            Log.Debug(_billingTag, $"In OnBillingSetupFinished: Code: {result.ResponseCode}, Message: {result.DebugMessage}");

            if (result.ResponseCode == BillingResponseCode.Ok)
            {
                // Read https://devblogs.microsoft.com/pfxteam/the-nature-of-taskcompletionsourcetresult/
                // Return just Task vs Task<T>
                _connected?.SetResult(null);
            }
            else
            {
                _connected?.TrySetException(new InAppPurchaseException(
                    result.ResponseCode.ToPurchaseError(),
                    result.DebugMessage));
            }
        }

        /// <summary>
        /// Callback used by Billing Client
        /// </summary>
        public void OnBillingServiceDisconnected()
        {
            Log.Debug(_billingTag, "In OnBillingServiceDisconnected");
        }

        #endregion

        #region IPurchasesUpdatedListener

        /// <summary>
        /// Callback used by Billing Client when the Purchase completed
        /// </summary>
        /// <param name="result">BillingResult</param>
        /// <param name="listOfPurchases">List of Purchases</param>
        public void OnPurchasesUpdated(BillingResult result, IList<Purchase> listOfPurchases)
        {
            Log.Debug(_billingTag, $"In OnPurchasesUpdated: {result.ResponseCode}, Message: {result.DebugMessage}");

            // We succeeded only when the ReponseCode is Ok.

            if (result.ResponseCode == BillingResponseCode.Ok)
            {
                // Success. The Item has been Purchased
                _transactionPurchased?.TrySetResult(listOfPurchases?[0].ToInAppPurchase());
                return;
            }

            // Otherwise, it is an error (even for BillingResponseCode.ItemAlreadyOwned
            _transactionPurchased?.TrySetException(new InAppPurchaseException(result.ResponseCode.ToPurchaseError(),
                result.DebugMessage));
            _transactionPurchased = null;

            return;
        }

        #endregion

        #region IPurchaseHistoryResponseListener

        /// <summary>
        /// Callback used by Billing Client when the Purchases are Restored
        /// </summary>
        /// <param name="result">BillingResult</param>
        /// <param name="listOfPurchaseHistoryRecords">List of History Records</param>
        public void OnPurchaseHistoryResponse(BillingResult result, IList<PurchaseHistoryRecord> listOfPurchaseHistoryRecords)
        {
            Log.Debug(_billingTag, $"In OnSkuDetailsResponse: Code: {result.ResponseCode}, Message: {result.DebugMessage}");
            Log.Debug(_billingTag, $"listOfPurchaseHistoryRecords is null == {listOfPurchaseHistoryRecords == null}");

            if (listOfPurchaseHistoryRecords == null)
            {
                return;
            }

            foreach (PurchaseHistoryRecord r in listOfPurchaseHistoryRecords)
            {
                Log.Debug(_billingTag, $"{r.Skus.FirstOrDefault()}, {r.PurchaseTime}, {r.OriginalJson}");
            }
        }

        #endregion

        #region IPurchasesResponseListener

        /// <summary>
        /// Callback for the Async call to Restore Purchases. Behaves similarly to OnPurchasesUpdated
        /// </summary>
        /// <param name="result">BillingResult</param>
        /// <param name="listOfPurchases">List of Restored Purchases</param>
        public void OnQueryPurchasesResponse(BillingResult result, IList<Purchase> listOfPurchases)
        {
            Log.Debug(_billingTag, $"In OnQueryPurchasesResponse: {result.ResponseCode}, Message: {result.DebugMessage}");

            // We succeeded only when the ReponseCode is Ok.
            if (result.ResponseCode == BillingResponseCode.Ok)
            {
                // Success. The Item has been Purchased
                _purchasesRestored?.TrySetResult(listOfPurchases);
                return;
            }

            // Otherwise, it is an error (even for BillingResponseCode.ItemAlreadyOwned
            _purchasesRestored?.TrySetException(new InAppPurchaseException(result.ResponseCode.ToPurchaseError(),
                result.DebugMessage));
            _purchasesRestored = null;

            return;
        }

        #endregion

        /// <summary>
        /// Singleton Access to the Service
        /// </summary>
        internal static InAppPurchaseService Instance { get; } = new InAppPurchaseService();

        // Current Activity. Used to launch the Billing flow
        private static Activity _activity = null;

        // Billing Client. It is a one-time use and needs to be initialized every time.
        // See https://github.com/android/play-billing-samples/blob/34b49bc0929e5cead8e69df7a6e0d41793fe645f/ClassyTaxiJava/app/src/main/java/com/example/android/classytaxijava/billing/BillingClientLifecycle.java#L93
        private BillingClient _billingClient = null;

        // Task Source that is set to Complete when the Billing Client is connected
        // https://devblogs.microsoft.com/pfxteam/the-nature-of-taskcompletionsourcetresult/
        private TaskCompletionSource<object> _connected;

        /// Task Source that is set to Complete when the individual purchase is complete
        /// Only one purchase is supported at a time
        private TaskCompletionSource<InAppPurchaseResult> _transactionPurchased;

        // Task that is set to Complete when Purchases are restored
        private TaskCompletionSource<IList<Purchase>> _purchasesRestored;

        // A dictionary of retrieved products
        private readonly Dictionary<string, SkuDetails> _rertievedProducts = new Dictionary<string, SkuDetails>();

        // Log Tag
        private const string _billingTag = "InAppPurchase";

        internal InAppPurchaseService()
        {
        }

        /// <summary>
        /// Sets Current Activity. Required to launch the Billing flow
        /// </summary>
        /// <param name="activity">Current Activity</param>
        public void SetActivity(Activity activity)
        {
            _activity = activity;
        }

        /// <summary>
        /// Initializes and Connects to Billing Client. Needs to be called in the Activity OnCreate
        /// </summary>
        public Task StartAsync()
        {
            Log.Debug(_billingTag, "Build BillingClient. connection...");

            // Setup the Task Source to wait for Connection
            if (_connected != null)
                throw new InAppPurchaseException(PurchaseError.DeveloperError, "BillingClient has been already started");

            _connected = new TaskCompletionSource<object>();

            _billingClient = BillingClient.NewBuilder(Application.Context)
                .SetListener(this)
                .EnablePendingPurchases()
                .Build();

            // Attempt to connect to the service
            if (!_billingClient.IsReady)
            {
                Log.Debug(_billingTag, "Start Connection...");
                _billingClient.StartConnection(this);
            }
            else
            {
                // Already connected. Complete the _connected Task Source
                _connected.TrySetResult(null);
            }

            // Return awaitable Task which is signaled when the BillingClient calls OnBillingServiceDisconnected
            return _connected.Task;
        }

        /// <summary>
        /// Disconnects BillingClient. Needs to be called in the Activity OnDestroy
        /// </summary>
        public Task StopAsync()
        {
            try
            {
                // Reset the Task Source for Billing Client Connection
                _connected?.TrySetCanceled();
                _connected = null;

                if (_billingClient != null && _billingClient.IsReady)
                {
                    _billingClient.EndConnection();
                    _billingClient.Dispose();
                    _billingClient = null;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(_billingTag, $"Unable to EndConnection: {ex.Message}");
            }

            Log.Debug(_billingTag, "Disconnected");

            // Completed Task
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns product info for the specified Ids
        /// </summary>
        /// <param name="productIds">Product Ids</param>
        /// <param name="productType">Product Type (either BillingClient.SkuType.Inapp or BillingClient.SkuType.Subs)</param>
        /// <returns></returns>
        public async Task<IEnumerable<Product>> LoadProductsAsync(string[] productIds, ProductType productType)
        {
            string skuType = GetBillingSkuType(productType);

            // Build the Sku Params
            SkuDetailsParams skuParams = SkuDetailsParams.NewBuilder()
                .SetSkusList(productIds)
                .SetType(skuType)
                .Build();

            // Query the Play store
            QuerySkuDetailsResult queryResult = await _billingClient.QuerySkuDetailsAsync(skuParams);
            BillingResult result = queryResult?.Result;

            if (result == null)
            {
                // Failed to get products. Set the Exception to the Task, so the caller can react to the issue
                throw new InAppPurchaseException(PurchaseError.Unknown, "BillingResult is null");
            }

            if (result.ResponseCode != BillingResponseCode.Ok)
            {
                PurchaseError purchaseError = result.ResponseCode.ToPurchaseError();
                // Failed to get products. Set the Exception to the Task, so the caller can react to the issue
                throw new InAppPurchaseException(purchaseError, result.DebugMessage);
            }

            // Wait till the products are received in the callback
            IList<SkuDetails> skuDetails = queryResult?.SkuDetails;
            if (skuDetails == null)
                skuDetails = new List<SkuDetails>();

            // Add more Skus to the Dictionary of SkuDetails
            // We need SkuDetails to initiate the Purchase
            foreach (SkuDetails sku in skuDetails)
                _rertievedProducts.TryAdd(sku.Sku, sku);

            // Return products
            return skuDetails.Select(p => new Product
            {
                Name = p.Title,
                Description = p.Description,
                CurrencyCode = p.PriceCurrencyCode,
                FormattedPrice = p.Price,
                ProductId = p.Sku,
                MicrosPrice = p.PriceAmountMicros,
                LocalizedIntroductoryPrice = p.IntroductoryPrice,
                MicrosIntroductoryPrice = p.IntroductoryPriceAmountMicros
            });
        }

        /// <summary>
        /// Determines whether the User can make payments on the given device
        /// </summary>
        /// <returns>true/false</returns>
        public bool CanMakePayments()
        {
            // Always true for Android
            return true;
        }

        /// <summary>
        /// Initializes an async process to purchas the product. Only one purchase request can be happening at a time
        /// </summary>
        /// <param name="productId">Product to buy</param>
        /// <returns>Purchase object</returns>
        public async Task<InAppPurchaseResult> PurchaseAsync(string productId)
        {
            // Make sure no purchases are being currently made 
            if (_transactionPurchased != null && !_transactionPurchased.Task.IsCanceled)
                throw new InAppPurchaseException(PurchaseError.DeveloperError, "Another Purchase is in progress");

            // First, get the SkuDetail
            SkuDetails sku;
            if (!_rertievedProducts.TryGetValue(productId, out sku))
                throw new InAppPurchaseException(PurchaseError.DeveloperError,
                    $"Cannot find a retrieved Product with {productId} SKU. Products must be first queried from the Play Store");

            // Build FlowParam for the Purchase
            BillingFlowParams flowParams = BillingFlowParams.NewBuilder()
                    .SetSkuDetails(sku)
                    .Build();

            // Set a new Task Source to wait for completion
            _transactionPurchased = new TaskCompletionSource<InAppPurchaseResult>();
            Task<InAppPurchaseResult> taskPurchaseComplete = _transactionPurchased.Task;

            //_billingClient.QueryPurchaseHistoryAsync(BillingClient.SkuType.Inapp, this);

            // Initiate the Billing Process.
            BillingResult response = _billingClient.LaunchBillingFlow(_activity, flowParams);
            if (response.ResponseCode != BillingResponseCode.Ok)
            {
                // Reset the in-app-purchase flow
                _transactionPurchased?.TrySetCanceled();
                _transactionPurchased = null;
                throw new InAppPurchaseException(response.ResponseCode.ToPurchaseError(), response.DebugMessage);
            }

            // Wait till the Task is complete (e.g. Succeeded or Failed - which will result in Exception)
            InAppPurchaseResult purchase = await taskPurchaseComplete;
            _transactionPurchased = null;

            return purchase;
        }

        /// <summary>
        /// Restores all previous purchases
        /// </summary>
        /// <param name="productType">Product Type (inapp or subs)</param>
        /// <returns>An array of previous purchases</returns>
        public async Task<List<InAppPurchaseResult>> RestoreAsync(ProductType productType)
        {
            string skuType = GetBillingSkuType(productType);

            // Setup the Task Source first for the Async callback
            _purchasesRestored = new TaskCompletionSource<IList<Purchase>>();
            Task<IList<Purchase>> taskRestoreComplete = _purchasesRestored.Task;

            // Query existing Purchases
            _billingClient.QueryPurchasesAsync(skuType, this);

            // Wait till the Task is complete (e.g. Succeeded or Failed - which will result in Exception)
            IList<Purchase> listOfRestoredPurchases = await taskRestoreComplete;
            _transactionPurchased = null;

            List<InAppPurchaseResult> purchases = new List<InAppPurchaseResult>(listOfRestoredPurchases.Count);
            foreach (Purchase p in listOfRestoredPurchases)
            {
                Log.Debug(_billingTag, $"Sku: {p.Skus.FirstOrDefault()}, Acknowledged: {p.IsAcknowledged}, State: {p.PurchaseState}");

                // Convert BillingClient Purchases
                purchases.Add(p.ToInAppPurchase());
            }

            return purchases;


            //// Query existing Purchases
            //Purchase.PurchasesResult result = _billingClient.QueryPurchasesAsync(skuType, this);
            //if (result.BillingResult.ResponseCode != BillingResponseCode.Ok)
            //{
            //    throw new InAppPurchaseException(result.BillingResult.ResponseCode.ToPurchaseError(),
            //        result.BillingResult.DebugMessage);
            //}

            //// Return null array
            //if (result.PurchasesList == null)
            //    return Task.FromResult<List<InAppPurchaseResult>>(null);

            //// Otherwise, create an array and return it
            //List<InAppPurchaseResult> purchases = new List<InAppPurchaseResult>(result.PurchasesList.Count);
            //foreach (Purchase p in result.PurchasesList)
            //{
            //    Log.Debug(_billingTag, $"Sku: {p.Skus.FirstOrDefault()}, Acknowledged: {p.IsAcknowledged}, State: {p.PurchaseState}");

            //    // Convert BillingClient Purchases
            //    purchases.Add(p.ToInAppPurchase());
            //}

            //// Return purchases
            //return Task.FromResult(purchases);
        }

        private string GetBillingSkuType(ProductType productType)
        {
            switch (productType)
            {
                case ProductType.Subscription:
                    return BillingClient.SkuType.Subs;
                case ProductType.Consumable:
                case ProductType.NonConsumable:
                default:
                    return BillingClient.SkuType.Inapp;
            }
        }

        /// <summary>
        /// Finalizes the Purchase by calling AcklowledgePurchase or ConsumePurchase
        /// </summary>
        /// <param name="token">Purchase Token</param>
        /// <param name="productType">Product Type</param>
        /// <returns>Task</returns>
        public async Task FinalizePurchaseAsync(string token, ProductType productType)
        {
            switch (productType)
            {
                case ProductType.Subscription:
                // Subscriptions are acknowledged the same way as non-consumable
                // https://developer.android.com/google/play/billing/integrate#acknowledge
                case ProductType.NonConsumable:
                    await AcklowledgePurchaseAsync(token);
                    break;
                case ProductType.Consumable:
                    await ConsumePurchaseAsync(token);
                    break;
                default:
                    throw new InAppPurchaseException(PurchaseError.DeveloperError, $"Unsupported Product Type {productType}");
            }
        }

        private async Task ConsumePurchaseAsync(string token)
        {
            ConsumeParams consumeParams =
                ConsumeParams.NewBuilder()
                .SetPurchaseToken(token)
                .Build();

            // Consume the Consumable Product
            ConsumeResult consumeResult = await _billingClient.ConsumeAsync(consumeParams);
            BillingResult result = consumeResult?.BillingResult;

            if (result == null)
            {
                // Failed to get result back.
                throw new InAppPurchaseException(PurchaseError.Unknown, "BillingResult is null");
            }

            if (result.ResponseCode != BillingResponseCode.Ok)
            {
                throw new InAppPurchaseException(
                    result.ResponseCode.ToPurchaseError(),
                    result.DebugMessage);
            }

            // Otherwise, the ConsumeAsync succeeded. 
        }

        private async Task AcklowledgePurchaseAsync(string token)
        {
            AcknowledgePurchaseParams acknowledgePurchaseParams =
                AcknowledgePurchaseParams.NewBuilder()
                    .SetPurchaseToken(token)
                    .Build();

            // Consume the Non-Consumable Product
            BillingResult result = await _billingClient.AcknowledgePurchaseAsync(acknowledgePurchaseParams);

            if (result == null)
            {
                // Failed to get result back.
                throw new InAppPurchaseException(PurchaseError.Unknown, "BillingResult is null");
            }

            if (result.ResponseCode != BillingResponseCode.Ok)
            {
                throw new InAppPurchaseException(
                    result.ResponseCode.ToPurchaseError(),
                    result.DebugMessage);
            }

            // Otherwise, the Acknowledgement succeeded. 
        }
    }

    /// <summary>
    /// In-App Service Specifi Exception
    /// </summary>
    [Preserve(AllMembers = true)]
    public class InAppPurchaseException : Exception
    {
        /// <summary>
        /// Type of error
        /// </summary>
        public PurchaseError PurchaseError { get; }

        /// <summary>
        /// Default ctor
        /// </summary>
        /// <param name="error">Purchase Error</param>
        /// <param name="ex">Inner Exception</param>
        public InAppPurchaseException(PurchaseError error, Exception ex) : base("Unable to process purchase.", ex)
        {
            PurchaseError = error;
        }

        /// <summary>
        /// Default ctor
        /// </summary>
        /// <param name="error">Purchase Error</param>
        public InAppPurchaseException(PurchaseError error) : base("Unable to process purchase.")
        {
            PurchaseError = error;
        }

        /// <summary>
        /// Default ctor
        /// </summary>
        /// <param name="error">Purchase Error</param>
        /// <param name="message">Error Message</param>
        public InAppPurchaseException(PurchaseError error, string message) : base(message)
        {
            PurchaseError = error;
        }

        /// <summary>
        /// ToString for Exception
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return $"Error: {PurchaseError}. {base.ToString()}";
        }
    }

    /// <summary>
    /// Product Type: Subscription, Consumable, Non-Consumable
    /// </summary>
    public enum ProductType
    {
        /// <summary>
        /// Unknown/Invalid Product Type
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Subscription Type
        /// </summary>
        Subscription,
        /// <summary>
        /// Consumable Type
        /// </summary>
        Consumable,
        /// <summary>
        /// Non-Consumable Type
        /// </summary>
        NonConsumable,
    }

    /// <summary>
    /// Purchase state of a Product
    /// </summary>
    public enum ProductState
    {
        /// <summary>
        /// The Purchase state of Product is unknown (usually not Purchased)
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Product has been purchased and in good standing (Active)
        /// </summary>
        Active,
        /// <summary>
        /// Pending Purchase. The payment is pending
        /// </summary>
        Pending,
        /// <summary>
        /// Free Product. Could be used to promote Free products/apps
        /// </summary>
        Free,
    }

    /// <summary>
    /// Gets the current status of the purchase
    /// </summary>
    public enum PurchaseState
    {
        /// <summary>
        /// Purchase state unknown
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Purchased and in good standing
        /// </summary>
        Purchased = 1,
        /// <summary>
        /// Purchase was canceled
        /// </summary>
        Canceled = 2,
        /// <summary>
        /// Purchase was refunded
        /// </summary>
        Refunded = 3,
        /// <summary>
        /// In the process of being processed
        /// </summary>
        Purchasing,
        /// <summary>
        /// Transaction has failed
        /// </summary>
        Failed,
        /// <summary>
        /// Was restored.
        /// </summary>
        Restored,
        /// <summary>
        /// In queue, pending external action
        /// </summary>
        Deferred,
        /// <summary>
        /// In free trial
        /// </summary>
        FreeTrial,
        /// <summary>
        /// Pending Purchase
        /// </summary>
        PaymentPending,
        /// <summary>
        /// Free Product
        /// </summary>
        Free,
    }

    /// <summary>
    /// Type of purchase error
    /// </summary>
    public enum PurchaseError
    {
        /// <summary>
        /// Unknown Error
        /// </summary>
        Unknown,
        /// <summary>
        /// Billing API version is not supported for the type requested (Android), client error (iOS)
        /// </summary>
        BillingUnavailable,
        /// <summary>
        /// Developer issue
        /// </summary>
        DeveloperError,
        /// <summary>
        /// Product sku not available
        /// </summary>
        ItemUnavailable,
        /// <summary>
        /// Other error
        /// </summary>
        GeneralError,
        /// <summary>
        /// User cancelled the purchase
        /// </summary>
        UserCancelled,
        /// <summary>
        /// App store unavailable on device
        /// </summary>
        AppStoreUnavailable,
        /// <summary>
        /// User is not allowed to authorize payments
        /// </summary>
        PaymentNotAllowed,
        /// <summary>
        /// One of the payment parameters was not recognized by app store
        /// </summary>
        PaymentInvalid,
        /// <summary>
        /// The requested product is invalid
        /// </summary>
        InvalidProduct,
        /// <summary>
        /// The product request failed
        /// </summary>
        ProductRequestFailed,
        /// <summary>
        /// The user has not allowed access to Cloud service information
        /// </summary>
        PermissionDenied,
        /// <summary>
        /// The device could not connect to the network
        /// </summary>
        NetworkConnectionFailed,
        /// <summary>
        /// The user has revoked permission to use this cloud service
        /// </summary>
        CloudServiceRevoked,
        /// <summary>
        /// The user has not yet acknowledged Apple’s privacy policy for Apple Music
        /// </summary>
        PrivacyError,
        /// <summary>
        /// The app is attempting to use a property for which it does not have the required entitlement
        /// </summary>
        UnauthorizedRequest,
        /// <summary>
        /// The offer identifier cannot be found or is not active
        /// </summary>
        InvalidOffer,
        /// <summary>
        /// The signature in a payment discount is not valid
        /// </summary>
        InvalidSignature,
        /// <summary>
        /// Parameters are missing in a payment discount
        /// </summary>
        MissingOfferParams,
        /// <summary>
        /// The price you specified in App Store Connect is no longer valid.
        /// </summary>
        InvalidOfferPrice,
        /// <summary>
        /// Restoring the transaction failed
        /// </summary>
        RestoreFailed,
        /// <summary>
        /// Network connection is down
        /// </summary>
        ServiceUnavailable,
        /// <summary>
        /// Product is already owned
        /// </summary>
        AlreadyOwned,
        /// <summary>
        /// Item is not owned and can not be consumed
        /// </summary>
        NotOwned,
        /// <summary>
        /// Billing Client Service is Disconnected
        /// </summary>
        ServiceDisconnected,
    }

    internal static class Extensions
    {
        public static PurchaseError ToPurchaseError(this BillingResponseCode code)
        {
            PurchaseError error;
            switch (code)
            {
                case BillingResponseCode.BillingUnavailable:
                    error = PurchaseError.BillingUnavailable;
                    break;
                case BillingResponseCode.DeveloperError:
                    error = PurchaseError.DeveloperError;
                    break;
                case BillingResponseCode.Error:
                    error = PurchaseError.GeneralError;
                    break;
                case BillingResponseCode.FeatureNotSupported:
                    error = PurchaseError.GeneralError;
                    break;
                case BillingResponseCode.ItemAlreadyOwned:
                    error = PurchaseError.AlreadyOwned;
                    break;
                case BillingResponseCode.ItemNotOwned:
                    error = PurchaseError.NotOwned;
                    break;
                case BillingResponseCode.ItemUnavailable:
                    error = PurchaseError.ItemUnavailable;
                    break;
                case BillingResponseCode.ServiceDisconnected:
                    error = PurchaseError.ServiceDisconnected;
                    break;
                case BillingResponseCode.ServiceTimeout:
                    error = PurchaseError.NetworkConnectionFailed;
                    break;
                case BillingResponseCode.ServiceUnavailable:
                    error = PurchaseError.ServiceUnavailable;
                    break;
                case BillingResponseCode.UserCancelled:
                    error = PurchaseError.UserCancelled;
                    break;
                default:
                    error = PurchaseError.Unknown;
                    break;
            }

            return error;
        }

        public static InAppPurchaseResult ToInAppPurchase(this Purchase p)
        {
            return new InAppPurchaseResult
            {
                TransactionDateUtc = new DateTime(p.PurchaseTime),
                Id = p.OrderId,
                ProductId = p.Skus.FirstOrDefault(),
                Acknowledged = p.IsAcknowledged,
                AutoRenewing = p.IsAutoRenewing,
                State = p.GetPurchaseState(),
                PurchaseToken = p.PurchaseToken
            };
        }

        private static PurchaseState GetPurchaseState(this Purchase transaction)
        {
            if (transaction?.PurchaseState == null)
                return PurchaseState.Unknown;

            switch (transaction.PurchaseState)
            {
                case global::Android.BillingClient.Api.PurchaseState.Unspecified:
                    return PurchaseState.Unknown;
                case global::Android.BillingClient.Api.PurchaseState.Pending:
                    return PurchaseState.PaymentPending;
                case global::Android.BillingClient.Api.PurchaseState.Purchased:
                    return PurchaseState.Purchased;
            }

            return PurchaseState.Unknown;
        }
    }

    /// <summary>
    /// Represents the Product
    /// </summary>
    [Preserve(AllMembers = true)]
    public class Product
    {
        /// <summary>
        /// Name of the product
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the product
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Product ID or Sku
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// Localized Price (not including tax)
        /// </summary>
        public string FormattedPrice { get; set; }

        /// <summary>
        /// ISO 4217 currency code for price. For example, if price is specified in British pounds sterling is "GBP".
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Price in micro-units, where 1,000,000 micro-units equal one unit of the 
        /// currency. For example, if price is "€7.99", price_amount_micros is "7990000". 
        /// This value represents the localized, rounded price for a particular currency.
        /// </summary>
        public Int64 MicrosPrice { get; set; }

        /// <summary>
        /// Gets or sets the localized introductory price.
        /// </summary>
        /// <value>The localized introductory price.</value>
        public string LocalizedIntroductoryPrice { get; set; }

        /// <summary>
        /// Introductory price of the product in micor-units
        /// </summary>
        /// <value>The introductory price.</value>
        public Int64 MicrosIntroductoryPrice { get; set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Plugin.InAppBilling.Abstractions.InAppBillingProduct"/>
        /// has introductory price. This is an optional value in the answer from the server, requires a boolean to check if this exists
        /// </summary>
        /// <value><c>true</c> if has introductory price; otherwise, <c>false</c>.</value>
        public bool HasIntroductoryPrice => !string.IsNullOrEmpty(LocalizedIntroductoryPrice);

        /// <summary>
        /// Indicates the Purchase state of the Product.
        /// Active, Pending, Free or Unknown/Not Purchased
        /// Free could be used for promotional product (e.g. Links to other Apps)
        /// </summary>
        public ProductState State { get; set; }

        /// <summary>
        /// Source of the product Image/Logo
        /// </summary>
        public string ImageSource { get; set; }
    }

    /// <summary>
    /// In App Purchase Results
    /// </summary>
    [Preserve(AllMembers = true)]
    public class InAppPurchaseResult
    {
        /// <summary>
        /// Purchase/Order Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Trasaction date in UTC
        /// </summary>
        public DateTime TransactionDateUtc { get; set; }

        /// <summary>
        /// Product Id/Sku
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// Indicates whether the purchase has been already acknowledge.
        /// </summary>
        public bool Acknowledged { get; set; }

        /// <summary>
        /// Indicates whether the subscritpion renewes automatically. If true, the sub is active, else false the user has canceled.
        /// </summary>
        public bool AutoRenewing { get; set; }

        /// <summary>
        /// Unique token identifying the purchase for a given item
        /// </summary>
        public string PurchaseToken { get; set; }

        /// <summary>
        /// Gets the current purchase/subscription state
        /// </summary>
        public PurchaseState State { get; set; }
    }
}

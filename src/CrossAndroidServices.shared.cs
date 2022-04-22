using System;

namespace Companova.Xamarin.Common.Android.Services
{
    /// <summary>
    /// Cross Companova.Xamarin.Android.Service
    /// </summary>
    public static class CrossAndroidServices
    {
        static Lazy<IAnalyticsService> _analyticsService = new Lazy<IAnalyticsService>(() => CreateAnalyticsService(), System.Threading.LazyThreadSafetyMode.PublicationOnly);
        static Lazy<IInAppPurchaseService> _inAppPurchaseService = new Lazy<IInAppPurchaseService>(() => CreateInAppPurchaseService(), System.Threading.LazyThreadSafetyMode.PublicationOnly);
        static Lazy<IInterstitialService> _interstitialService = new Lazy<IInterstitialService>(() => CreateInterstitialService(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

        /// <summary>
        /// Gets if the Analytics Service is supported on the current platform.
        /// </summary>
        public static bool IsAnalyticsServiceSupported => _analyticsService.Value == null ? false : true;

        /// <summary>
        /// Gets if the InAppPurchase Service is supported on the current platform.
        /// </summary>
        public static bool IsInAppPurchaseServiceSupported => _inAppPurchaseService.Value == null ? false : true;


        /// <summary>
        /// Gets if the Interstitial Service is supported on the current platform.
        /// </summary>
        public static bool IsInterstitialServiceSupported => _interstitialService.Value == null ? false : true;

        /// <summary>
        /// Current Analytics Service implementation to use
        /// </summary>
        public static IAnalyticsService AnalyticsService
        {
            get
            {
                IAnalyticsService ret = _analyticsService.Value;
                if (ret == null)
                {
                    throw NotImplementedInReferenceAssembly();
                }
                return ret;
            }
        }

        /// <summary>
        /// Current InAppPurchase Service implementation to use
        /// </summary>
        public static IInAppPurchaseService InAppPurchaseService
        {
            get
            {
                IInAppPurchaseService ret = _inAppPurchaseService.Value;
                if (ret == null)
                {
                    throw NotImplementedInReferenceAssembly();
                }
                return ret;
            }
        }

        /// <summary>
        /// Current Interstitial Service implementation to use
        /// </summary>
        public static IInterstitialService InterstitialService
        {
            get
            {
                IInterstitialService ret = _interstitialService.Value;
                if (ret == null)
                {
                    throw NotImplementedInReferenceAssembly();
                }
                return ret;
            }
        }

        static IAnalyticsService CreateAnalyticsService()
        {
#if NETSTANDARD1_0 || NETSTANDARD2_0
            return null;
#else
#pragma warning disable IDE0022 // Use expression body for methods
            return new AnalyticsService();
#pragma warning restore IDE0022 // Use expression body for methods
#endif
        }

        static IInAppPurchaseService CreateInAppPurchaseService()
        {
#if NETSTANDARD1_0 || NETSTANDARD2_0
            return null;
#else
#pragma warning disable IDE0022 // Use expression body for methods
            return new InAppPurchaseService();
#pragma warning restore IDE0022 // Use expression body for methods
#endif
        }

        static IInterstitialService CreateInterstitialService()
        {
#if NETSTANDARD1_0 || NETSTANDARD2_0
            return null;
#else
#pragma warning disable IDE0022 // Use expression body for methods
            return new InterstitialService();
#pragma warning restore IDE0022 // Use expression body for methods
#endif
        }

        internal static Exception NotImplementedInReferenceAssembly() =>
            new NotImplementedException("This functionality is not implemented in the portable version of this assembly. You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");

    }
}

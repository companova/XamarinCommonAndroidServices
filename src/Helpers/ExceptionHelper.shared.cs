using System;
using System.Diagnostics;
using System.Collections.Generic;

using Companova.Xamarin.Common.Android.Services;

namespace Companova.Xamarin.Common.Android.Helpers
{
    internal class ExceptionHelper
    {
        // Exception Event Name
        internal const string Exception = "cust_exception";

        internal static void Publish(Exception e, string method, IAnalyticsService analyticsService)
        {
            IDictionary<string, string> properties = null;

            try
            {
                Debug.WriteLine(e.ToString());

                properties = new Dictionary<string, string>(4);
                properties.Add("Method", method);
                properties.Add("Message", e.Message);
                properties.Add("Exception", e.ToString());

                // For now, just log the Exception
                analyticsService?.LogEvent(Exception, properties);
            }
            catch
            {
                // Should not fail;
            }
        }
    }
}
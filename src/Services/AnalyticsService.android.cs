using System;
using System.Collections.Generic;

using Android.OS;
using Android.Runtime;

using Firebase.Analytics;

namespace Companova.Xamarin.Common.Android.Services
{
    /// <summary>
    /// Analytics Service Implementation for Android
    /// </summary>
    [Preserve(AllMembers = true)]
    public class AnalyticsService : IAnalyticsService
    {
        //public static AnalyticsService Instance { get; } = new AnalyticsService();

        private FirebaseAnalytics _firebaseAnalytics;

        /// <summary>
        /// Default Constructor for Analytics Service Implementation on Android
        /// </summary>
        public AnalyticsService()
        {
        }

        /// <summary>
        /// Sets the Firebase Instance. Usually called on the startup
        /// </summary>
        /// <param name="firebaseAnalytics">Firebase Instance</param>
        public void SetFirebaseAnalytics(FirebaseAnalytics firebaseAnalytics)
        {
            _firebaseAnalytics = firebaseAnalytics;
        }

        /// <summary>
        /// Logs a simple Firebase Event
        /// </summary>
        /// <param name="eventId">Event Id</param>
        public void LogEvent(string eventId)
        {
            LogEvent(eventId, (IDictionary<string, string>)null);
        }

        /// <summary>
        /// Logs Firebase Event with a single parameter
        /// </summary>
        /// <param name="eventId">Event Id</param>
        /// <param name="paramName">Parameter Name</param>
        /// <param name="value">Parameter Value</param>
        public void LogEvent(string eventId, string paramName, string value)
        {
            LogEvent(eventId, new Dictionary<string, string>
            {
                { paramName, value }
            });
        }

        /// <summary>
        /// Logs Firebase Event with multiple parameters
        /// </summary>
        /// <param name="eventId">Event Id</param>
        /// <param name="parameters">Parameters</param>
        public void LogEvent(string eventId, IDictionary<string, string> parameters)
        {
            if (parameters == null)
            {
                _firebaseAnalytics.LogEvent(eventId, new Bundle());
                return;
            }

            Bundle firebaseBundle = new Bundle();
            foreach (KeyValuePair<string, string> p in parameters)
            {
                firebaseBundle.PutString(p.Key, Trim(p.Value));
            }

            _firebaseAnalytics.LogEvent(eventId, firebaseBundle);
        }

        private static string Trim(string s)
        {
            // 100 is the Firebase limit
            if (s.Length <= 99)
                return s;

            // otherwise, trim it to 100
            return s.Substring(0, 99);
        }
    }
}

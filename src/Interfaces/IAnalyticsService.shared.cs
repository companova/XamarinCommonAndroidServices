using System;
using System.Collections.Generic;

using Firebase.Analytics;

namespace Companova.Xamarin.Common.Android.Services
{
    /// <summary>
    /// Interface for Analytics Service
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// Sets the Firebase Instance. Usually called on the startup
        /// </summary>
        /// <param name="firebaseAnalytics">Firebase Instance</param>
        void SetFirebaseAnalytics(FirebaseAnalytics firebaseAnalytics);

        /// <summary>
        /// Logs a simple Firebase Event
        /// </summary>
        /// <param name="eventId">Event Id</param>
        void LogEvent(string eventId);

        /// <summary>
        /// Logs Firebase Event with a single parameter
        /// </summary>
        /// <param name="eventId">Event Id</param>
        /// <param name="paramName">Parameter Name</param>
        /// <param name="value">Parameter Value</param>
        void LogEvent(string eventId, string paramName, string value);

        /// <summary>
        /// Logs Firebase Event with multiple parameters
        /// </summary>
        /// <param name="eventId">Event Id</param>
        /// <param name="parameters">Parameters</param>
        void LogEvent(string eventId, IDictionary<string, string> parameters);
    }
}

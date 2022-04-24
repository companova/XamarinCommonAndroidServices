using System;
using Android.App;
using Firebase.Analytics;
using Companova.Xamarin.Common.Android.Services;

namespace TestApp
{
    [Application]
    internal class TestAppApplication : Application
    {
        public TestAppApplication(IntPtr handle, Android.Runtime.JniHandleOwnership ownerShip) : base(handle, ownerShip)
        {
        }

        public override void OnCreate()
        {
            // If OnCreate is overridden, the overridden c'tor will also be called.
            base.OnCreate();

            Android.Util.Log.Debug("TestAppApplication", $"OnCreate()");

            // Initialize Firebase Analytics
            //FirebaseAnalytics firebaseAnalytics = FirebaseAnalytics.GetInstance(this);

            // Set Firebase to the Analytics Service
            //CrossAndroidServices.AnalyticsService.SetFirebaseAnalytics(firebaseAnalytics);
        }
    }
}
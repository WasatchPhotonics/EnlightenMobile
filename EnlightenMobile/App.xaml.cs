using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using EnlightenMobile.Services;
using EnlightenMobile.Views;
using Xamarin.Essentials;

namespace EnlightenMobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            VersionTracking.Track();
            MainPage = new MainPage();
        }

        protected override void OnStart() { }
        protected override void OnSleep() { }
        protected override void OnResume() { }
    }
}

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
        Logger logger = Logger.getInstance();

        public App()
        {
            logger.debug("App: starting ctor");
            InitializeComponent();

            DependencyService.Register<IPlatformUtil>();
            VersionTracking.Track();
            MainPage = new MainPage();
            logger.debug("App: finished ctor");
        }

        protected override void OnStart() { }
        protected override void OnSleep() { }
        protected override void OnResume() { }
    }
}

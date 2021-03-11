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

        public static string appProjectSetting;

        public App(string appProj)
        {
            //public string 
            
            appProjectSetting = appProj;

            InitializeComponent();

            // register platform-specific Services here
            DependencyService.Register<IPlatformUtil>();

            VersionTracking.Track();
            // MainPage = new MainPage();
            if (appProj == "Enlighten")
            {
                MainPage = new AppShell();
            }
            else if (appProj == "Raman")
            {
                MainPage = new RamanAppShell();
            }
            else if (appProj == "Absorbance")
            {
                MainPage = new AbsorbanceAppShell();
            }
            else if (appProj == "Fluoresence")
            {
                MainPage = new FluoresenceAppShell();
            }
            else if (appProj == "Irradiance")
            {
                MainPage = new IrradianceAppShell();
            }
            else if (appProj == "Irradiance")
            {
                MainPage = new IrradianceAppShell();
            }
            else if (appProj == "Color")
            {
                MainPage = new ColorAppShell();
            }
        }


        protected override void OnStart() { }
        protected override void OnSleep() { }
        protected override void OnResume() { }
    }

}

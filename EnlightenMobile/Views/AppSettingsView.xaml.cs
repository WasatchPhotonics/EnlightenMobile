using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System;
using EnlightenMobile.Models;

namespace EnlightenMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AppSettingsView : ContentPage
    {
        Logger logger = Logger.getInstance();
        AppSettings appSettings = AppSettings.getInstance();

        public AppSettingsView()
        {
            InitializeComponent();
        }

        /*
        // Since we use the GUI to provide verification, this is properly
        // implemented in the View rather than ViewModel.  The confirmed value
        // is written to the ViewModel at the end.
        void qcMode_Toggled(object sender, EventArgs e)
        {
            logger.debug("Raman Mode changed on GUI");
            var enabled = switchQC.IsToggled;
            if (!enabled)
            {
                // if the switch is off, just disable Raman Mode and go
                appSettings.qcModeEnabled = false;
                return;
            }

            // we were asked to enable QC...this merits confirmation
            var confirmed = DisplayAlert("QC Mode",
                "Enabling QC mode gives the user direct control over the laser, " +
                "including overriding the internal watchdog timeout. " +
                "Do you wish to continue?",
                "Yes", "Cancel").Result;
            if (!confirmed)
            {
                // user clicked "Cancel"
                appSettings.qcModeEnabled = false;
                return;
            }

            // apparently they're sure
            appSettings.qcModeEnabled = true;
        }
        */

    }
}

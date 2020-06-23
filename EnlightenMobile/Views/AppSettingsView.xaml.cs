using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System;
using EnlightenMobile.Models;
using EnlightenMobile.ViewModels;

namespace EnlightenMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AppSettingsView : ContentPage
    {
        AppSettingsViewModel asvm;

        Logger logger = Logger.getInstance();

        public AppSettingsView()
        {
            InitializeComponent();

            asvm = (AppSettingsViewModel)BindingContext;
        }

        // the user clicked "return" or "done" when entering the password, so
        // hide what he entered, then ask the ViewModel to authenticate
        void entryPassword_Completed(Object sender, EventArgs e)
        {
            var password = entryPassword.Text;
            entryPassword.Text = AppSettings.stars;
            asvm.authenticate(password);
        }

        // the user clicked in an Entry, so clear the field
        void entry_Focused(Object sender, FocusEventArgs e)
        {
            var entry = sender as Entry;
            entry.Text = "";
        }
        
        void entryVerticalROIStartLine_Completed(Object sender, EventArgs e)
        {
            var entry = sender as Entry;
            asvm.setVerticalROIStartLine(entry.Text);
        }

        void entryVerticalROIStopLine_Completed(Object sender, EventArgs e)
        {
            var entry = sender as Entry;
            asvm.setVerticalROIStopLine(entry.Text);
        }
    }
}

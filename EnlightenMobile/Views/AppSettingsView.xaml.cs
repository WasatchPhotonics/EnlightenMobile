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

        // the user clicked to enter a password, so clear the field
        void entryPassword_Focused(Object sender, FocusEventArgs e)
        {
            entryPassword.Text = "";
        }
    }
}

using EnlightenMobile.Models;
using EnlightenMobile.ViewModels;
using Xamarin.Forms.Xaml;
using Xamarin.Forms;

namespace EnlightenMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BluetoothView : ContentPage
    {
        BluetoothViewModel bvm;

        public BluetoothView()
        {
            InitializeComponent();

            bvm = (BluetoothViewModel)BindingContext;

            listView.ItemsSource = bvm.bleDeviceList;

            // Give the ViewModel an ability to display user messages on
            // the View
            bvm.notifyUser += notifyUserAsync;
            bvm.showProgress += showProgress;
        }

        async void notifyUserAsync(string title, string message, string button) =>
            await DisplayAlert(title, message, button);

        void showProgress(double progress) => 
            Util.updateProgressBar(progressBarConnect, progress);

        // Step 3a: the user has explicitly selected a device from the listView
        void listView_ItemSelected(object sender, SelectedItemChangedEventArgs e) =>
            bvm.selectBLEDevice(listView.SelectedItem as BLEDevice);

    }
}
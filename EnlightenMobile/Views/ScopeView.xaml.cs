using System;
using System.Threading;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using EnlightenMobile.ViewModels;
using ZXing.Net.Mobile.Forms;

namespace EnlightenMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScopeView : ContentPage
    {
        bool lastLandscape;
        bool showingControls = true;
        static readonly SemaphoreSlim semRotate = new SemaphoreSlim(1, 1);

        ZXingScannerPage scanPage;
        const string rightArrow = ">>";
        const string leftArrow = "<<";

        ScopeViewModel svm;

        Logger logger = Logger.getInstance();

        public ScopeView()
        {
            InitializeComponent();

            // needed?
            OnSizeAllocated(Width, Height);

            // ScopeView has numerous View <--> ViewModel interactions, so grab
            // a handle to the ViewModel
            svm = (ScopeViewModel)BindingContext;

            // Give the ScopeViewModel an ability to display "toast" messages on
            // the View (such as "saved foo.csv") by having the View monitor for
            // notifications, and if one is received
            // https://stackoverflow.com/a/26038700/11615696
            svm.notifyToast += (string msg) => Util.toast(msg, scrollOptions);

            svm.theChart = chart;

            // since the ramanModeEnabled switch is not "bound" to the ViewModel,
            // we must manually process PropertyChange notifications
            svm.PropertyChanged += viewModelPropertyChanged;
        }

        private void viewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var name = e.PropertyName;
            if (name == "ramanModeEnabled" && switchRamanMode.IsToggled != svm.ramanModeEnabled)
                switchRamanMode.IsToggled = svm.ramanModeEnabled;
        }

        ////////////////////////////////////////////////////////////////////////
        // Entries
        ////////////////////////////////////////////////////////////////////////

        // the user clicked in an Entry, so clear the field
        void entry_Focused(Object sender, FocusEventArgs e)
        {
            var entry = sender as Entry;
            entry.Text = "";
        }
        
        void entryIntegrationTimeMS_Completed(Object sender, EventArgs e)
        {
            var entry = sender as Entry;
            svm.setIntegrationTimeMS(entry.Text);
        }

        void entryGainDb_Completed(Object sender, EventArgs e)
        {
            var entry = sender as Entry;
            svm.setGainDb(entry.Text);
        }
        async void notifyUserAsync(string title, string message, string button) =>
           await DisplayAlert(title, message, button);
        void entryScansToAverage_Completed(Object sender, EventArgs e)
        {
            var entry = sender as Entry;
            svm.setScansToAverage(entry.Text);
        }

        ////////////////////////////////////////////////////////////////////////
        // Expand / hide control palette in Landscape mode
        ////////////////////////////////////////////////////////////////////////
        
        private void buttonExpander_Clicked(object sender, EventArgs e)
        {
            logger.debug("Clicked the expander button");
            scrollOptions.IsVisible = showingControls = !showingControls;
            buttonExpander.Text = showingControls ? rightArrow : leftArrow;
            updateLandscapeGridColumns();
        }

        // Since we use the GUI to provide verification, this is properly
        // implemented in the View rather than ViewModel.  The confirmed value
        // is written to the ViewModel at the end.
        async void ramanMode_Toggled(object sender, EventArgs e)
        {
            logger.debug("ScopeView: Raman Mode changed on GUI");
            var enabled = switchRamanMode.IsToggled;
            if (!enabled)
            {
                // if the switch is off, just disable Raman Mode and go
                logger.debug("ScopeView: disabling Raman Mode in SVM");
                svm.ramanModeEnabled = false;
                return;
            }

            // apparently we were asked to enable Raman Mode...this deserves
            // confirmation
            logger.debug("ScopeView.confirmRamanMode: Raising a DisplayAlert");
            var confirmed = await DisplayAlert("Raman Mode",
                    "Enabling Raman mode means the laser will AUTOMATICALLY fire each " +
                    "time you take a measurement.  Do you wish to continue?",
                    "Yes", "Cancel");
            logger.debug($"ScopeView.confirmRamanMode: confirmed = {confirmed}");
            svm.ramanModeEnabled = confirmed;
            if (!confirmed)
                switchRamanMode.IsToggled = false;
        }

        // This event is used to reformat the ScopeView from Portrait to Landscape 
        // and back again.
        // 
        // @see https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/layouts/device-orientation
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            bool doRotate = false;
            var landscape = width > height;

            if (!semRotate.Wait(5))
            {
                logger.debug($"OnSizeAllocated: timeout");
                return;
            }

            if (landscape != lastLandscape)
            {
                logger.debug($"OnSizeAllocated: Width {width}, Height {height}");
                logger.debug($"OnSizeAllocated: rotated from lastLandscape {lastLandscape} to landscape {landscape}");
                lastLandscape = landscape;
                doRotate = true;
            }
            semRotate.Release();

            if (doRotate)
            {
                logger.debug($"OnSizeAllocated: performing rotation");
                if (landscape)
                {
                    // transition to Landscape
                    logger.debug("OnSizeAllocated: transitioning to Landscape");
                    updateLandscapeGridColumns();
                }
                else
                {
                    // transition to Portrait
                    logger.debug("OnSizeAllocated: transitioning to Portrait");

                    // change Grid to [ chart    ]
                    //                [ hide     ]
                    //                [ controls ]
                    outerGrid.RowDefinitions.Clear();
                    outerGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) } );
                    outerGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Star) } );
                    outerGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) } );
                    outerGrid.ColumnDefinitions.Clear();
                    outerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) } );

                    stackChart.SetValue(Grid.RowProperty, 0);
                    stackChart.SetValue(Grid.ColumnProperty, 0);
                    stackExpander.SetValue(Grid.RowProperty, 1);
                    stackExpander.SetValue(Grid.ColumnProperty, 0);
                    scrollOptions.SetValue(Grid.RowProperty, 2);
                    scrollOptions.SetValue(Grid.ColumnProperty, 0);

                    // always show controls in portrait
                    showingControls = scrollOptions.IsVisible = true;
                    stackExpander.IsVisible = false;
                    buttonExpander.Text = rightArrow;
                }

                logoVertical.IsVisible = !landscape;
                logoHorizontal.IsVisible = landscape;

                logger.debug("OnSizeAllocated: rotation complete");
            }
        }

        void updateLandscapeGridColumns()
        {
            outerGrid.RowDefinitions.Clear();
            outerGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) } );
            outerGrid.ColumnDefinitions.Clear();

            stackExpander.IsVisible = true;

            logger.debug($"updateLandscapeGridColumns: showingControls {showingControls}");

            // change Grid to [ chart | expander | controls ]
            if (showingControls)
            {
                buttonExpander.Text = rightArrow;
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) } );
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0.1, GridUnitType.Star) } );
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) } );
            }
            else
            {
                // not showing controls
                buttonExpander.Text = leftArrow;
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) } );
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0.05, GridUnitType.Star) } );
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Star) } );
            }

            stackChart.SetValue(Grid.RowProperty, 0);
            stackChart.SetValue(Grid.ColumnProperty, 0);
            stackExpander.SetValue(Grid.RowProperty, 0);
            stackExpander.SetValue(Grid.ColumnProperty, 1);
            scrollOptions.SetValue(Grid.RowProperty, 0);
            scrollOptions.SetValue(Grid.ColumnProperty, 2);

            logger.debug($"updateLandscapeGridColumns: done");
        }

        private void qrScan(object sender, EventArgs e)
        {
            performQRScan();
        }

        private void photoCapture(object sender, EventArgs e)
        {
            svm.performPhotoCapture();
        }

        private async void performQRScan()
        {
            scanPage = new ZXingScannerPage();
            scanPage.OnScanResult += (result) =>
            {
                scanPage.IsScanning = false;

                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PopAsync();
                    svm.setQRText(result.Text);
                });
            };

            await Navigation.PushAsync(scanPage);
        }
    }
}

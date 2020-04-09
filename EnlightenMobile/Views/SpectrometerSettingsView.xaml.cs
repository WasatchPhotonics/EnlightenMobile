using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using EnlightenMobile.ViewModels;
using EnlightenMobile.Models;

namespace EnlightenMobile.Views
{
    /// <summary>
    /// Code-behind for SpectrometerSettingsView.xaml.
    /// </summary>
    /// <remarks>
    /// This is the View for SpectrometerSettings, which currently means the EEPROM.  
    /// (FPGA Compilation Options, firmware revisions etc can be added later.)
    ///
    /// Note this class owns and instantiates the actual ObservableCollection of 
    /// ViewableSettings, injecting the (empty) collection into the EEPROM object
    /// to populate.  
    ///
    /// The XAML's listView is an implicit member of this class.  Here we set the
    /// listView's .ItemSource to the ObservableCollection in the ctor.  Therefore,
    /// when the listView tries to display items, it will draw them from the
    /// viewableSettings collection.
    ///
    /// However, the display of any one particular ViewableSetting is mediated
    /// through the SpectrometerSettingsViewModel, which allows for any transforms
    /// between the raw Model data (ViewableSetting) and the display version shown
    /// on the View.  In this case, we're not applying any transforms or display
    /// logic (ViewableSetting is internally stored as a string tuple, with no
    /// transformations required), but this is the MVVM architecture.  
    ///
    /// Note that the XAML directs the binding context to the SpectrometerSettingsViewModel,
    /// but the XAML ListSettings can reference attributes directly within the ViewModel's
    /// public ViewableSetting object.
    /// </remarks>
    /// <todo>
    /// Make a new SpectrometerSettingsModel class, which "has" an EEPROM, but 
    /// also FPGACompilationOptions, FirmwareRevisions (µC/FPGA ver), BatteryStatus
    /// etc objects, and which populates the ObservableCollection from all of them.
    /// </todo>
    public partial class SpectrometerSettingsView : ContentPage
    {
        ObservableCollection<ViewableSetting> viewableSettings;

        public SpectrometerSettingsView()
        {
            InitializeComponent();

            viewableSettings = new ObservableCollection<ViewableSetting>();

            EEPROM eeprom = EEPROM.getInstance();
            if (eeprom != null)
                eeprom.viewableSettings = viewableSettings;

            listView.ItemsSource = viewableSettings;
        }
    }
}

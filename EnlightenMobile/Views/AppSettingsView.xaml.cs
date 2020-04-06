using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EnlightenMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AppSettingsView : ContentPage
    {
        Logger logger = Logger.getInstance();
        public AppSettingsView()
        {
            logger.debug("AppSettingsView: starting ctor");
            InitializeComponent();
            logger.debug("AppSettingsView: finished ctor");
        }
    }
}

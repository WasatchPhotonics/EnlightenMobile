using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EnlightenMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AboutView : ContentPage
    {
        Logger logger = Logger.getInstance();

        public AboutView()
        {
            logger.debug("AboutView: starting ctor");

            InitializeComponent();
            
            logger.debug("AboutView: finished ctor");
        }
    }
}

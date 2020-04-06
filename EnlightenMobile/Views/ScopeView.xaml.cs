using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EnlightenMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScopeView : ContentPage
    {
        double lastWidth;
        double lastHeight;

        Logger logger = Logger.getInstance();

        public ScopeView()
        {
            logger.debug("ScopeView: starting ctor");
            InitializeComponent();

            // needed?
            OnSizeAllocated(Width, Height);
            logger.debug("ScopeView: finished ctor");
        }

        // This event is used to reformat the ScopeView from Portrait to Landscape 
        // and back again.
        // 
        // @see https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/layouts/device-orientation
        protected override void OnSizeAllocated(double newWidth, double newHeight)
        {
            base.OnSizeAllocated(newWidth, newHeight);
            if (newWidth != lastWidth || newHeight != lastHeight) 
            {
                lastWidth = newWidth;
                lastHeight = newHeight;
                var landscape = newWidth > newHeight;

                outerStack.Orientation = landscape ? StackOrientation.Horizontal : StackOrientation.Vertical;
                logoVertical.IsVisible = !landscape;
                logoHorizontal.IsVisible = landscape;
            }
        }
    }
}

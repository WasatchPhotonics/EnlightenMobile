using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using EnlightenMobile;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;

namespace EnlightenMobile.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : Xamarin.Forms.TabbedPage
    {
        PageNav pageNav = PageNav.getInstance();
        Logger logger = Logger.getInstance();

        public MainPage()
        {
            // all other Views are implicitly instantiated here; their respective 
            // ViewModels are instantiated as each View calls InitializeComponent
            InitializeComponent();

            // page swiping was cool, but it really complicated the pan-and-zoom
            // finger interaction with the chart  
            On<Android>().SetIsSwipePagingEnabled(false);

            logger.debug("MainPage: registering tabs with PageNav");
            pageNav.tabbedPage = this;
            foreach (var child in Children)
                pageNav.add(child.Title, child);

            CurrentPageChanged += MainPage_CurrentPageChanged;
        }

        // a callback for whenever the tab is changed; handy to notify the Logger
        // that it should start issuing notifications on each new log message
        private void MainPage_CurrentPageChanged(object sender, EventArgs e)
        {
            if (pageNav is null)
                return;

            var newPageName = pageNav.currentPageName();

            // if we just changed to the log page, send an update now, then 
            // updated on each log message
            //
            // What exactly is the use-case for updates on each log message? If
            // we're looking at the log page, what events (spectra, laser etc)
            // can we trigger?  I can imagine this being marginally useful for
            // Notify Characteristics, but little else.
            logger.liveUpdates = newPageName is null ? false : newPageName == "Log";
            if (logger.liveUpdates)
                logger.update();
        }
    }
}
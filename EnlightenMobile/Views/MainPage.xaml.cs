using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using EnlightenMobile;

namespace EnlightenMobile.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : TabbedPage
    {
        PageNav pageNav = PageNav.getInstance();
        Logger logger = Logger.getInstance();

        public MainPage()
        {
            InitializeComponent();

            pageNav.tabbedPage = this;
            foreach (var child in Children)
                pageNav.add(child.Title, child);
        }

        // a callback for whenever the tab is changed; handy to notify the Logger
        // that it should start issuing notifications on each new log message
        private void TabbedPage_CurrentPageChanged(object sender, EventArgs e)
        {
            var newPageName = pageNav.currentPageName();

            // if we just changed to the log page, send an update now, then 
            // updated on each log message
            logger.liveUpdates = newPageName is null ? false : newPageName == "log";
            if (logger.liveUpdates)
                logger.update();
        }
    }
}
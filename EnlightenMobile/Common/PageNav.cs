using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace EnlightenMobile
{
    // The genesis of this class was to allow BluetoothView to automatically 
    // transition to ScopeView upon successful completion.
    public class PageNav
    {
        static PageNav instance;
        Logger logger = Logger.getInstance();

        // the "TabControl widget"
        public TabbedPage tabbedPage;

        // map of pages (tabs) by name
        Dictionary<string, Page> pages = new Dictionary<string, Page>();

        public static PageNav getInstance()
        {
            if (instance is null)
                instance = new PageNav();
            return instance;
        }

        PageNav() { }

        public Page add(string name, Page page)
        {
            logger.debug($"PageNav.add: {name}");
            return pages[name] = page;
        }

        public string currentPageName()
        {
            try
            {
                foreach (var pair in pages)
                    if (tabbedPage.CurrentPage == pair.Value)
                        return pair.Key;
            }
            catch(Exception ex)
            {
                logger.error($"PageNav exception: {ex}");
            }
            return null;
        }

        public void select(string name)
        {
            if (pages.ContainsKey(name))
                tabbedPage.CurrentPage = pages[name];
        }
    }
}

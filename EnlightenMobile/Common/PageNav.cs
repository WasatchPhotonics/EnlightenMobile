using System.Collections.Generic;
using Xamarin.Forms;

namespace EnlightenMobile
{
    // The genesis of this class was to allow BluetoothView to automatically 
    // transition to ScopeView upon successful completion.
    public class PageNav
    {
        static PageNav instance;

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
            return pages[name] = page;
        }

        public string currentPageName()
        {
            foreach (var pair in pages)
                if (tabbedPage.CurrentPage == pair.Value)
                    return pair.Key;
            return null;
        }

        public void select(string name)
        {
            if (pages.ContainsKey(name))
                tabbedPage.CurrentPage = pages[name];
        }
    }
}

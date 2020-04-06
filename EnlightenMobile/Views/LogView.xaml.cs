using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EnlightenMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LogView : ContentPage
    {
        Logger logger = Logger.getInstance();
        public LogView()
        {
            logger.debug("LogView: starting ctor");
            InitializeComponent();
            logger.debug("LogView: finished ctor");
        }
    }
}

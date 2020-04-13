using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using EnlightenMobile.ViewModels;

namespace EnlightenMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScopeView : ContentPage
    {
        bool lastLandscape;
        bool showingControls = true;

        Logger logger = Logger.getInstance();

        public ScopeView()
        {
            InitializeComponent();

            // needed?
            OnSizeAllocated(Width, Height);

            // https://stackoverflow.com/a/26038700/11615696
            var vm = (ScopeViewModel)BindingContext;
            vm.scopeViewNotification += (string msg) => Util.toast(msg, scrollOptions);
        }

        private void buttonExpander_Clicked(object sender, EventArgs e)
        {
            logger.debug("Clicked the expander button");
            scrollOptions.IsVisible = showingControls = !showingControls;
            buttonExpander.Text = showingControls ? ">>" : "<<";
            updateLandscapeGridColumns();
        }

        // This event is used to reformat the ScopeView from Portrait to Landscape 
        // and back again.
        // 
        // @see https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/layouts/device-orientation
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            var landscape = width > height;
            if (landscape != lastLandscape)
            {
                lastLandscape = landscape;
                logger.debug($"OnSizeAllocated: Width {width}, Height {height}");

                if (landscape)
                {
                    // transition to Landscape
                    updateLandscapeGridColumns();
                }
                else
                {
                    // transition to Portrait

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
                    buttonExpander.Text = ">>";
                }

                logoVertical.IsVisible = !landscape;
                logoHorizontal.IsVisible = landscape;

                logger.debug($"OnSizeAllocated: stackExpander.IsVisible = {stackExpander.IsVisible}");
            }
        }

        void updateLandscapeGridColumns()
        {
            outerGrid.RowDefinitions.Clear();
            outerGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) } );
            outerGrid.ColumnDefinitions.Clear();

            stackExpander.IsVisible = true;

            // change Grid to [ chart | expander | controls ]
            if (showingControls)
            { 
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) } );
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0.1, GridUnitType.Star) } );
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) } );
            }
            else
            {
                // not showing controls
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
        }
    }
}

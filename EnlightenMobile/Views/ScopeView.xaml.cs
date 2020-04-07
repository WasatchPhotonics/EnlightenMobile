﻿using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EnlightenMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScopeView : ContentPage
    {
        bool lastLandscape;

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
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            var landscape = width > height;
            if (landscape != lastLandscape)
            {
                lastLandscape = landscape;

                if (landscape)
                {
                    // change Grid to [ cell | cell ]    
                    outerGrid.RowDefinitions.Clear();
                    outerGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) } );
                    outerGrid.ColumnDefinitions.Clear();
                    outerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) } );
                    outerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) } );

                    stackChart.SetValue(Grid.RowProperty, 0);
                    stackChart.SetValue(Grid.ColumnProperty, 0);
                    scrollOptions.SetValue(Grid.RowProperty, 0);
                    scrollOptions.SetValue(Grid.ColumnProperty, 1);
                }
                else
                {
                    // change Grid to [ cell ]
                    //                [ cell ]
                    outerGrid.RowDefinitions.Clear();
                    outerGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) } );
                    outerGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) } );
                    outerGrid.ColumnDefinitions.Clear();
                    outerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) } );

                    stackChart.SetValue(Grid.RowProperty, 0);
                    stackChart.SetValue(Grid.ColumnProperty, 0);
                    scrollOptions.SetValue(Grid.RowProperty, 1);
                    scrollOptions.SetValue(Grid.ColumnProperty, 0);
                }

                logoVertical.IsVisible = !landscape;
                logoHorizontal.IsVisible = landscape;
            }
        }
    }
}

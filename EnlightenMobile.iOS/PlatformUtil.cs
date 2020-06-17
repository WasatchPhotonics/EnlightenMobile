using System;
using Xamarin.Forms;
using EnlightenMobile.Services;
using EnlightenMobile;
using UIKit;
using Xamarin.Forms.Platform.iOS;
using CoreGraphics;

[assembly: Dependency(typeof(EnlightenMobile.iOS.PlatformUtil))]
namespace EnlightenMobile.iOS
{
    public class PlatformUtil : IPlatformUtil
    {
        Logger logger = Logger.getInstance();
        
        public PlatformUtil() { }

        // @see https://stackoverflow.com/a/43754366/11615696
        public bool bluetoothEnabled()
        {
            return true;
        }

        // @see https://stackoverflow.com/a/43754366/11615696
        public bool enableBluetooth(bool flag)
        {
            logger.error("enableBluetooth: begging your pardon, Apple has not graced us with such powers :-(");
            return false;
        }

        // Make a little pop-up message notification appear (no buttons, it just 
        // fades away after a few seconds).  Currently used when a Measurement 
        // is successfully saved.
        //
        // @see https://stackoverflow.com/a/42844156/11615696
        public void toast(string message, View view = null)  
        {  
            if (view is null)
                return;

            // PageNav pageNav = PageNav.getInstance();
            // var currentPage = pageNav.tabbedPage.CurrentPage;

            UIView uiView = viewToUIView(view);

            UIView residualView = uiView.ViewWithTag(1989);
            if (residualView != null)
                residualView.RemoveFromSuperview();

            var viewBack = new UIView(new CoreGraphics.CGRect(83, 0, 300, 100));
            viewBack.BackgroundColor = UIColor.Black;
            viewBack.Tag = 1989;
            UILabel lblMsg = new UILabel(new CoreGraphics.CGRect(0, 20, 300, 60));
            lblMsg.Lines = 2;
            lblMsg.Text = message;
            lblMsg.TextColor = UIColor.White;
            lblMsg.TextAlignment = UITextAlignment.Center;
            viewBack.Center = uiView.Center;
            viewBack.AddSubview(lblMsg);
            uiView.AddSubview(viewBack);
            viewBack.Layer.CornerRadius=10; 
            viewBack.Layer.MasksToBounds=true;
            UIView.BeginAnimations("Toast");
            UIView.SetAnimationDuration(3.0f);
            viewBack.Alpha = 0.0f;
            UIView.CommitAnimations();
        }

        // @todo implement on iOS
        public string getSavePath()
        {
            return null;
        }

        // @see https://michaelridland.com/xamarin/creating-native-view-xamarin-forms-viewpage/
        UIView viewToUIView(Xamarin.Forms.View view)
        {
            CGRect size = new CGRect(view.X, view.Y, view.Width, view.Height);

            var renderer = Platform.CreateRenderer(view);
            renderer.NativeView.Frame = size;
            renderer.NativeView.AutoresizingMask = UIViewAutoresizing.All;
            renderer.NativeView.ContentMode = UIViewContentMode.ScaleToFill;
            renderer.Element.Layout(size.ToRectangle());
     
            var nativeView = renderer.NativeView;
     
            nativeView.SetNeedsLayout();
     
            return nativeView;
        }
    }
}


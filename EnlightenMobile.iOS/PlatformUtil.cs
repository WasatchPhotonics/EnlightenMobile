using System;
using Xamarin.Forms;
using EnlightenMobile.Services;
using EnlightenMobile;
using UIKit;
using System.IO;
using Xamarin.Forms.Platform.iOS;
using CoreGraphics;

[assembly: Dependency(typeof(EnlightenMobile.iOS.PlatformUtil))]
namespace EnlightenMobile.iOS
{
    public class PlatformUtil : IPlatformUtil
    {
        Logger logger = Logger.getInstance();
        string savePath = null;
        
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
            UIView viewPresent = null;
            // None of the toast invokes pass a view so this doesn't work unless we get a view
            // see https://forums.xamarin.com/discussion/24689/how-to-acces-the-current-view-uiviewcontroller-from-an-external-service
            var window = UIApplication.SharedApplication.KeyWindow;
            var vc = window.RootViewController;
            //while (vc.PresentedViewController != null)
            //{
            //    vc = vc.PresentedViewController;
            //}
            viewPresent = vc.View;
            

            if(viewPresent is null)
            {
                return;
            }
            // PageNav pageNav = PageNav.getInstance();
            // var currentPage = pageNav.tabbedPage.CurrentPage;

            UIView uiView = viewPresent;

            UIView residualView = uiView.ViewWithTag(1989);
            if (residualView != null)
                residualView.RemoveFromSuperview();

            var viewBack = new UIView(new CoreGraphics.CGRect(0, 0, 300, 60)); //size shape of box
            viewBack.BackgroundColor = UIColor.White;
            viewBack.Tag = 1989;
            UILabel lblMsg = new UILabel(new CoreGraphics.CGRect(0, 0, 300, 60)); //size shape of text
            lblMsg.Lines = 2;
            lblMsg.Text = message;
            lblMsg.TextColor = UIColor.Black;
            lblMsg.TextAlignment = UITextAlignment.Center;
            viewBack.Center = new CGPoint(x: uiView.Bounds.GetMidX(), y: uiView.Bounds.GetMaxY() - 50); //positioning of box
            viewBack.AddSubview(lblMsg);
            uiView.AddSubview(viewBack);
            viewBack.Layer.CornerRadius=10; 
            viewBack.Layer.MasksToBounds=true;
            UIView.BeginAnimations("Toast");
            UIView.SetAnimationDuration(4.0f);
            viewBack.Alpha = 0.0f;
            UIView.CommitAnimations();
        }

        // @todo implement on iOS
        public string getSavePath()
        {
             if (savePath != null)
            {
                logger.debug($"getSavePath: returning previous savePath {savePath}");
                return savePath;
            }

            // create EnlightenSpectra if necessary
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var defaultPath = documents;
            logger.debug($"getSavePath: defaultPath = {defaultPath}");
            if (!writeable(defaultPath))
            {
                logger.error($"getSavePath: unable to write defaultPath {defaultPath}");
                return null;
            }

            // create today if necessary
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string todayPath = string.Format("{0}/{1}", defaultPath, today);
            logger.debug($"getSavePath: todayPath {todayPath}");
            if (!writeable(todayPath))
            {
                logger.error($"getSavePath: unable to write todayPath {todayPath}");
                return null;
            }
                
            logger.debug($"getSavePath: returning writeable todayPath {todayPath}");
            return savePath = todayPath;
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
        bool writeable(string path)
        {
            logger.debug($"writeable: testing {path}");
            if (Directory.Exists(path))
                logger.debug($"exists: {path}");
            else
            {
                logger.debug($"calling Mkdirs({path})");
                Directory.CreateDirectory(path);
                if (!Directory.Exists(path))
                {
                    logger.error($"writeable: Mkdirs failed to create {path}");
                    return false;
                }
            }
            // Before, and as can be seen in Android, these were File.Create
            // iOS does not seem to like this for some reason and since they were
            // directroies and not files I changed these to their respective dir commands
            // and it now appears to work as intended

            return true;
        }
    }
}


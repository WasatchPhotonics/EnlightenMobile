# Targets

It's pretty impressive (to me) that this works on Android and iOS relatively 
flawlessly.  Some notes about that.

Virtually everything is in EnlightenMobile, the cross-platform
folder.

Android stuff is in EnlightenMobile.Droid, iOS EnlightenMobile.iOS.  About the
only things you'll see in there are custom platform-specific files like shown
below.

You'll see differences between the rendered Android and iOS GUIs; Android
traditionally puts TabBars at the top, and iOS at the bottom for instance.  

# Services

Sometimes you need to code implementions just for iOS or just for Android.
The official way to do that is with Services.  Add a C# Interface to the Service
in the shared project (EnlightenMobile), then add an implementation in each 
target project (EnlightenMobile.Droid etc).  You "register" the Service in
the App.xaml.cs constructor, then grab the registered local implementation
via DependencyService.  See Util.toast() for an example.

# Platform-Specific Files

## Android

- AndroidManifest.xml
- styles.xml
- images

## iOS

- Info.plist
- images

# iOS Notes

-   If you don't set your permissions right in Info.plist, the whole thing dies
    (SIGABRT) at launch with nary an error message or usable stack-trace.  I 
    blame that on iOS being based on C, whilst Android has thick layers of 
    exception-catching Java.

# Future Targets

I think we can probably roll MacOS and Windows builds without too much 
trouble.  Have to look into that down the road — would be especially handy
on MacOS.

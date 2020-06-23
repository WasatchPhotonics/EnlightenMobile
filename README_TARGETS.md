# Targets

It's pretty impressive (to me) that this works on Android and iOS relatively 
flawlessly.  Some notes about that.

Virtually everything (Views, ViewModels, Models and utilities) are in the
EnlightenMobile directory, which is the basic application built and used for all 
targets.

Android stuff is in EnlightenMobile.Droid, and iOS stuff is in EnlightenMobile.iOS.  
About the only things you'll see in there are custom platform-specific files:
a basic main() class to instantiate and start the application, images/icons, 
and a per-target implementation of any services (see below).

You will see some visual differences between the rendered Android and iOS apps. 
For instance, Android traditionally puts TabBars at the top of the screen, and 
iOS at the bottom.  ProgressBars and "loading" animations have a native look-and-
feel, etc.

# Services

Sometimes you need to code implementions just for iOS or just for Android.
The official way to do that is with Services.  Add a C# Interface to the Service
in the shared project (EnlightenMobile), then add an implementation in each 
target project (EnlightenMobile.Droid etc).  You "register" the Service in
the App.xaml.cs constructor, then grab the registered local implementation
via DependencyService.  

For instance, see PlatformUtil:

- EnlightenMobile/Services/IPlatformUtil.cs
    - the interface of the cross-platform service
- EnlightenMobile.Android/PlatformUtil.cs
    - the Android implementation
- EnlightenMobile.iOS/PlatformUtil.cs
    - the iOS implementation

# Key Platform-Specific Files

## Android

- AndroidManifest.xml
- styles.xml
- images

## iOS

- Info.plist
- images

# iOS Notes

- If you don't set your permissions right in Info.plist, the whole thing dies
  (SIGABRT) at launch with nary an error message or usable stack-trace.  I 
  blame that on iOS being based on [Objective] C, whilst Android has thick 
  layers of exception-catching Java.

# Future Targets

I think we can probably roll MacOS and Windows builds without too much 
trouble.  Have to look into that down the road — would be especially handy
on MacOS.

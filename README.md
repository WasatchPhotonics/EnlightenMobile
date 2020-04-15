# Overview

A lightweight ENLIGHTEN-like (-lite?) GUI for Android and iOS, designed for 
Wasatch Photonics SiG spectrometers with BLE.

# Architecture

For a walk-through of how the application is structured, see 
[Architecture](README_ARCHITECTURE.md).

# Dependencies

Builds with Visual Studio Community 2019 (tested using MacOS version).

Requires several NuGet packages (Plugin.BLE, Plugin.Permissions etc), but all 
should self-download and install under Visual Studio.

## Telerik ##

Telerik packages are a little more complex, because they are licensed to 
individual developers and are provided through a private NuGet server requiring
authentication.

- Follow Telerik's [instructions](https://docs.telerik.com/devtools/xamarin/installation-and-deployment/telerik-nuget-server#visual-studio-for-mac) 
  to add their NuGet repository to Visual Studio.
- Make sure you enter your licensed username (email) and password when configuring 
  the private source.

## Android Target Version

I'd love to target older Android versions (7.x), but Google doesn't people going
below 8:

    /Library/Frameworks/Mono.framework/External/xbuild/Xamarin/Android/Xamarin.Android.Common.targets(2,2): 
    Warning XA0113: Google Play requires that new applications and updates must use 
    a TargetFrameworkVersion of v8.0 (API level 26) or above. You are currently 
    targeting v7.0 (API level 24). (XA0113) (EnlightenSimple)

And Xamarin.Forms doesn't want me going below Android 9:

    /Users/mzieg/work/code/EnlightenSimple/packages/Xamarin.Forms.4.5.0.495/build/Xamarin.Forms.targets(5,5): 
    Error XF005: The $(TargetFrameworkVersion) for EnlightenSimple (v7.0) is less
    than the minimum required $(TargetFrameworkVersion) for Xamarin.Forms (9.0). You
    need to increase the $(TargetFrameworkVersion) for EnlightenSimple. (XF005) 
    (EnlightenSimple)

That said, I _think_ the newest version of EnlightenMobile.Droid may work down 
to Android 5...testing to come.

# Release Procedure

## Android 

Follow the process here:

- https://docs.microsoft.com/en-us/xamarin/android/deploy-test/release-prep/

Essentially:
- Target -> Release (or Ad-Hoc?)
- Build -> Archive for Publishing -> Sign and Distribute -> Ad-Hoc

## iOS

see [README_IOS.md](README_IOS.md)

## Final

- run scripts/deploy

# Backlog

See [Backlog](https://wiki.wasatchphotonics.com/index.php?title=ENLIGHTEN_Mobile#Backlog)

# Changelog

See [Changelog](README_CHANGELOG.md).

# References

Xamarin 101 

- https://www.youtube.com/playlist?list=PLdo4fOcmZ0oU10SXt2W58pu2L0v2dOW-1
- https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/controls/views

Xamarin BLE

- https://github.com/didourebai/BLEPluginDemo
- (based on) https://github.com/xabre/xamarin-bluetooth-le

Xamarin Charting

- https://github.com/dotnet-ad/Microcharts
- (based on) https://github.com/mono/SkiaSharp 

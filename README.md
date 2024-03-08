<img src="https://raw.githubusercontent.com/WasatchPhotonics/EnlightenMobile/main/screenshots/ble.png" width="256" height="541"><img src="https://raw.githubusercontent.com/WasatchPhotonics/EnlightenMobile/main/screenshots/scope-top.png" width="256" height="541">

# Overview

A lightweight ENLIGHTEN-like (-lite?) GUI for Android and iOS, designed for 
Wasatch Photonics SiG spectrometers with BLE.

# Architecture

For a walk-through of how the application is structured, see 
[Architecture](README_ARCHITECTURE.md).

# Dependencies

Builds with Visual Studio Community 2019 (tested using MacOS Visual Studio 8.6.4).

Requires several NuGet packages (Plugin.BLE, Plugin.Permissions etc), but all 
should self-download and install themselves under Visual Studio.

## Telerik ##

Telerik packages are a little more complex, because they are licensed to 
individual developers and are provided through a private NuGet server requiring
authentication.

- Follow Telerik's [instructions](https://docs.telerik.com/devtools/xamarin/installation-and-deployment/telerik-nuget-server#visual-studio-for-mac) 
  to add their NuGet repository to Visual Studio.
- Make sure you enter your licensed username (email) and password when configuring 
  the private source.

# Release Procedure

## Android 

Follow the process here:

- https://docs.microsoft.com/en-us/xamarin/android/deploy-test/release-prep/

Essentially:
- Target -> Release 
- Build -> Archive for Publishing -> Sign and Distribute -> Ad-Hoc

Note that you must have physically selected the Android project, or a file
in that project, in the Solution pane, for the correct "Archive for Publishing"
option to appear under the Build menu.

## iOS

see [README_IOS](README_IOS.md)

## Final

- run scripts/deploy

# Backlog

See [Backlog](https://wiki.wasatchphotonics.com/index.php?title=ENLIGHTEN_Mobile#Backlog)

# Changelog

See [Changelog](README_CHANGELOG.md).

# References

Many thanks to the following resources for getting me started:

Xamarin 101 
- https://www.youtube.com/playlist?list=PLdo4fOcmZ0oU10SXt2W58pu2L0v2dOW-1
- https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/controls/views

Xamarin BLE
- https://github.com/didourebai/BLEPluginDemo
- (itself based on) https://github.com/xabre/xamarin-bluetooth-le

<img src="https://raw.githubusercontent.com/WasatchPhotonics/EnlightenMobile/main/screenshots/spec-settings.png" width="256" height="541"><img src="https://raw.githubusercontent.com/WasatchPhotonics/EnlightenMobile/main/screenshots/app-settings.png" width="256" height="541">
<img src="https://raw.githubusercontent.com/WasatchPhotonics/EnlightenMobile/main/screenshots/scope-landscape-zoom.png">

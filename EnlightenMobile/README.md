
# Developer's Introduction to EnlightenMobile

## This repo

The status of this repo should be described in the top-level README file. Notice that that file and any other's on the same level are not visible within Visual Studio, but it's the first thing people see when they open the repo in Github.

EnlightenMobile is an app that takes spectra over bluetooth, and it has been redesigned again to be tab-centric.

This branch contains the .NET 7 MAUI rewrite. The primary reason for rewriting is to be prepared for advanced spectral matching use-cases. It's also a convenient time to switch to .NET 7 to promote longevity with respect to phone OS versions. See [#34](https://github.com/WasatchPhotonics/EnlightenMobile/issues/34).

## Multi-Platform

- EnlightenLite.Android and EnlightenLite.iOS should do nothing more than initialize the cross-platform App class.
- Platform-specific artifacts such as iOS Entitlements or app icons can be managed in these subprojects.
- Avoid additional platform-specific code beyond the existing initialization basics. 
- Where necessary, create wrapping interfaces and keep logic in the main project. Do not do this unless you know what you are doing.
- Multiplatform BLE can be handled using Xamarin.Forms [DependencyService](https://learn.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/dependency-service/introduction). 

## XAML

~~This repository avoids using XAML.~~

As of 2023-08-22, I need to reassess the decision about XAML with respect to .NET 7 MAUI.

The original decision was made about Xamarin for the reasons listed below. 
I need to verify how easy it is to proceed with(out) XAML. 
It seems like Microsoft overwhelmingly wants developer's to use it, 
and with newly released MAUI, workarounds may be harder to find.

- XAML exposes buginess of Xamarin.Forms. https://github.com/dotnet/maui/issues/109
- For example dealing with Sliders and Max/Min is easier in C# https://github.com/xamarin/Xamarin.Forms/issues/1943 (error reporting indicates a line number, instead of gesturing wildly at the XAML)
- Xamarin Forms is beyond EOL, XAML increases dependency and makes it harder to port out https://dotnet.microsoft.com/en-us/platform/support/policy/xamarin
- XAML adds bulk to the application architecture. There are too many places to look for things.
- We don't even get the Designer view in Visual Studio Community 2022 (though I've heard the designer is not great anyway)
- It's easy to shadow behaviors when mixing C#/XAML
- XAML is less common among developers than C#.

To understand how to write Xamarin code without XAML and be adept at understanding the many code samples that do use XAML, you should understand XAML. https://learn.microsoft.com/en-us/xamarin/xamarin-forms/xaml/xaml-basics/
It should be clear what the corresponding C# code is when looking at a XAML sample.

## C# Programming

See [#33](https://github.com/WasatchPhotonics/EnlightenMobile/issues/33)
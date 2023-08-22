
# Developer's Introduction to EnlightenMobile
#### Samie Bee 2023-08-22

## This repo

The status of this repo should be described in the top-level README file. Notice that that file and any other's on the same level are not visible within Visual Studio, but it's the first thing people see when they open the repo in Github.

EnlightenMobile is an app that takes spectra over bluetooth, and it has been redesigned again to be tab-centric.

This branch contains the .NET 7 MAUI rewrite. The primary reason for rewriting is to be prepared for advanced spectral matching use-cases. It's also a convenient time to switch to .NET 7 to promote longevity with respect to phone OS versions. See [#34](https://github.com/WasatchPhotonics/EnlightenMobile/issues/34).

## UI Terminology

I wrote this originally about Xamarin. I think the same terminology applies to MAUI as well.

> Use the following Development Reference. 
https://learn.microsoft.com/en-us/dotnet/maui/ [1]
> 
>Find documentation about something specific, such as `Xamarin.Forms.Application`
using the table of contents. For example "Application Fundamentals > App Class".
>
> Xamarin does not have a single object that can be used
as either a control or hiearchial object or both. This matches how Qt works and signifies something of a relic in software UI culture.
>
> There are two competing concepts of 'Page' in the world of Xamarin and MAUI. I did my best to disambiguate below.
>
> - **Shell** - AppShell provides user navigation via tabs and menus.
> - **\<Subject\>Page.xaml** - Within the repository, there are files named in this form. These pages group related actions for the user. They consist of a single Layout which consists of several Views. Some examples include ScopePage.xaml and DevicePage.xaml. This naming convention follows from MainPage.xaml from the default project. The Pages folder of this repository is referring to this kind of page.
> - **Page (Navigation)** - As you will see in the documentation, Some pages are used for navigation such as TabbedPage and FlyoutPage. These pages will link to various \<Subject\>Page.xaml. We do not directly use these kinds of pages in this repository. AppShell orchestrates navigation for us.
> - **Layout** - Includes StackLayout, Grid, ScrollView; These orchestrate a collection of Views on screen, making decisions about spacing and location.
> - **View** - Includes Label, Button, Checkbox; This is what you may know of as 'Widgets' or 'Controls'. They are interactive screen elements that provide output and take input.
> This is explained in the official documentation: https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/ [2]
>
> ~1: Formerly https://learn.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/~ \
> ~2: Formerly https://learn.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/custom-renderer/renderers~

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
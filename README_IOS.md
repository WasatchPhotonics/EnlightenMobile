# iOS Instructions

## App Store 

### Screenshots

When generating screenshots for Apple Store, note that "Save Screenshot"
in the Simulator often gives the wrong pixel size, but you can "Copy Screen"
and Paste into Photoshop or otherwise to get the full image.

### App Icon

Even if your PNG only has a single layer, it may be a "floating" layer;
use "Flatten Image" in Photoshop.

### Build Settings

I fought with "ITMS-90338" (Non-Public API Usage) forever.  When I finally
got through, these were the settings I used:

- Info.plist
    - Identity
        - Application Name: EnlightenMobile
        - Bundle Identifier: com.wasatchphotonics.EnlightenMobile
    - Signing
        - Schema: Manual
        - Bundle Signing Options
            - Configuration: Release
            - Platform: iPhone
            - Signing Identity: Distribution: Mark Zieg (939LQPEVSQ)
            - Provisioning Profile: Wasatch Photonics
            - Custom Entitlements: Entitlements.plist
            - Additional Args: (none)
    - Deployment Info
        - Deployment Target: 13.4
        - Device Family: Universal
        - Main Interface: (none)
        - Device Orientations: Portrait, Left, Right

- EnlightenMobile.iOS Project Options
    - iOS Build
        - Configuration: Release
        - Platform: iPhone
        - Code Generation & Runtime
            - SDK Version: Default
            - Linker Behavior: Link Framework SDKs Only
            - Supported Architectures: ARM64
            - HttpClient: NSUrlSession (iOS 7+)
            - [ ] use LLVM
            - [ ] Enable Mono interpreter
            - [x] Perform float32 as float64
            - [x] Strip native debugging symbols
            - [ ] Enable incremental builds
            - [x] Use the concurrent garbage collector
            - [ ] Enable device-specific builds
            - Additional mtouch arguments: --linksdkonly

See etc/Apple Store Submission for screenshots of key screens.

Things that possibly contributed to success:

- bump your Info.plist Identity Build number
- increase the deployment target (I used 13.4)
- Following some advice on the internet, I commented-out the AppDelegate.cs
  call to Xamarin.Calabash.Start().
- I think it also helped to do a "make clean", which resolved some Info.plist
  warnings which simply were no longer appropriate / relevant (had been fixed).
- note that iPad requires orientation "PortraitUpsideDown" even if iPhone doesn't

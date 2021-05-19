# Security

Android .apk files, like .jar, are nothing more than zip archives; you can unzip
them to see all the underlying .dll and configuration files.

Furthermore, Xamarin DLLs themselves can then be decompiled to show all internal
methods signatures and even basic algorithm details: (MacOS example shown)

    $ unzip EnlightenMobile.apk
    $ which monodis
    /Library/Frameworks/Mono.framework/Versions/6.10.0/bin/monodis 
    $ monodis assemblies/EnlightenMobile.dll

# Obfuscation

There are DLL obfuscators which work with Xamarin and Android:

- https://forums.xamarin.com/discussion/comment/241880/#Comment_241880

Unclear whether such would work with iOS, or whether they're needed (assuming 
neither).

# Architecture

The application was based on a GitHub sample project, and uses a MVVM 
(Model-View-ViewModel) architecture.  I'm not an expert in that (makes my eyes 
bleed just to read it), but will summarize the basic definition I'm using.  It 
kinda matters because Xamarin's XAML "binding" functionality is intended to work
with MVVM structure, and most online tutorials / examples assume you're using it.

I think the short-form of why MVC (Model-View-Controller) was replaced by MVVM is
that some people saw MVC as this:

         Controller
            .  
           / \
    Model <---> View

...when the N-Tier Gods wanted them to see it like this:

    Model <--> Controller <--> View

So now they gave us this to make it _emphatically_ clear:

    Model <--> ModelView <--> View

- Model (Data)
    - Basically, the underlying data regarding Spectrometer state (including the
      EEPROM), any Measurements that have been taken, AppSettings (configuration
      of the ENLIGHTEN Android application itself, similar to enlighten.ini), etc.

- View (GUI)
    - All the buttons and labels and screen layouts.  These are primarily 
      defined in XAML (similar to Qt's enlighten\_layout.ui XML), with small
      "code-behind" classes in C# (e.g., ScopeView.xaml has the ScopeView.cs
      code-behind) for programmatic things you just can't do in XML.

- ModelView (Business Logic and Data Display Transformation)
    - I don't know why they thought it would be clever to call it "ModelView", other 
      than to make it really, really clear that this goes between the "Model" and the
      "View".

Xamarin lets View definitions be split across XAML (XML) and "code-behind" C#
classes, so it looks more like this:

                                  .--- View.xaml
    Model <--> ModelView <--> View         |
                                  '--- View.cs

There are things that are easier to do in XML, and others that are easier to
do in C#, and some things you can do in either and it's just a matter of 
preference.  All the elements defined in the XAML file are automatically
visible in the code-behind C# file.

XAML supports "bindings" in the XML, which automatically associate View 
widgets (Xamarin.Forms objects) with named Properties in the corresponding
ViewModel class.  If the user changes the value on one of those widgets in
the GUI, it automatically calls the ViewModel's Property's setter so you
can store / act on the new value.

Likewise, if something changes in the Model, the ViewModel can flow the new
data back up to the GUI by raising a PropertyChangedNotification with that
specific "bound" Property name.

I'm still learning how to use notifications propertly, so sometimes in my 
Model <--> ModelView communications, you'll see I'm still using old-fashioned
callbacks (okay, delegates and closures).  Some of that can probably be made
more elegant, I just went with what I knew worked.

Also, there were times that I wanted to pass calls between the ModelView and
the View's code-behind, and I didn't always see the "intended" way to do that,
so I cheated to some Singletons (which I know are bad).  So my structure looks
a little more like this:

                                  .--- View.xaml
    Model <--> ModelView <--> View         |
      |            |              '--- View.cs
      v            |                       |
     _Common_______v_______________________v__
    |                                         |
    |         PageNav    Logger    (...)      |
    |_________________________________________|

I use the MVVM where I can and understand how.

One big possible exception is that most of the BLE control logic is actually 
implemented in the BluetoothView.cs code-behind, rather than (as it probably
should be) BluetoothViewModel.cs.  That's the way it was in the sample code
I started with, and I didn't want to break things by moving it.  

Also, if we decide to turn this into a multi-platform app with iOS targets, then
it may make perfect sense to have the (platform-dependent) BLE code in the View's 
code-behind...I'm just not sure.  We can move it if and when it makes sense to do
so, but what we have works.

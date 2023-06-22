# Architecture

The application was based on a GitHub sample project, and uses the MVVM 
(Model-View-ViewModel) architecture.  I'm not an expert in that, but will 
summarize the basic definition I'm using.  It matters because Xamarin's XAML 
"binding" functionality is expressly designed to work with MVVM structure, and 
most online tutorials / examples assume you're using it.

I think the short-form of why MVC (Model-View-Controller) was replaced by MVVM is
that some people saw MVC as this:

         Controller
            .  
           / \
     View <---> Model

...when evolving practice was converged toward a more directed dataflow like this:

    View <--> Controller <--> Model

So they renamed things to make this emphatically clear (while also allowing the 
human operator to act as "Controller" via the View):

    View <--> ViewModel <--> Model

- Model (Data)
    - Basically, the underlying data regarding Spectrometer state (including the
      EEPROM), any Measurements that have been taken, Settings (configuration
      of the ENLIGHTEN Android application itself, similar to enlighten.ini), etc.

- View (GUI)
    - All the buttons and labels and screen layouts.  These are primarily 
      defined in XAML (similar to Qt's enlighten\_layout.ui XML), with small
      "code-behind" classes in C# (e.g., ScopeView.xaml has the ScopeView.xaml.cs
      code-behind) for programmatic things you just can't do in XML.

- ViewModel (Business Logic and Data Display Transformation)

Xamarin lets View definitions be split across XAML (XML) and "code-behind" C#
classes, so it looks more like this:

    View.xaml ----.
        |          View <--> ViewModel <--> Model
    View.xaml.cs -'

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

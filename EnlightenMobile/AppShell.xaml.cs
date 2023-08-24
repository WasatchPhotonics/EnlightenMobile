namespace EnlightenMobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
		InitializeComponent();

		// For now, we don't yet have persistent device connections,
		// and so DeviceTab is simply the default.
        bool hasExistingConnection = false;

        if (hasExistingConnection)
            this.CurrentItem = ScopeTab;
        else
            this.CurrentItem = DeviceTab;
    }
}

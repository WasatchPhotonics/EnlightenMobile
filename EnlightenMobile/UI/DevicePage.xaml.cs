namespace EnlightenMobile;

public partial class DevicePage : ContentPage
{
	public DevicePage()
	{
		InitializeComponent();
	}

	// Left over from the demo project, shows how to interact with screen reader
	// for accessibility
	private void OnCounterClicked(object sender, EventArgs e)
	{
		// SemanticScreenReader.Announce();
	}
}


namespace Ui.Accessibility;

/// <summary>
/// The screen's heading: the one h1 in the layout's main region. Focus goes there on arrival (the app's FocusOnNavigate) and when a
/// closing Sheet has no opener left to return to.
/// </summary>
public static class ScreenHeading
{
    public const string Selector = "#main-content h1";
}

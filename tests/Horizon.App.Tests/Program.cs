using System.Windows;
using Horizon.App;

var area = new Rect(100, 50, 1200, 800);

AssertRect(
    PanelLayout.GetBounds(PanelDisplayState.CollapsedSliver, area, 200),
    new Rect(1294, 210, 6, 72),
    "sliver bounds");
AssertRect(
    PanelLayout.GetBounds(PanelDisplayState.HoverHandle, area, 200),
    new Rect(1270, 200, 30, 92),
    "hover handle bounds");
AssertRect(
    PanelLayout.GetBounds(PanelDisplayState.ExpandedPanel, area, 200),
    new Rect(940, 50, 360, 800),
    "expanded panel bounds");
AssertEqual(70d, PanelLayout.CoerceHandleTop(area, -500), "top clamp");
AssertEqual(738d, PanelLayout.CoerceHandleTop(area, 5000), "bottom clamp");
AssertEqual(false, PanelLayout.IsDragDelta(4), "exact threshold remains a click");
AssertEqual(true, PanelLayout.IsDragDelta(4.01), "movement above threshold is a drag");
AssertEqual(
    true,
    PanelInteractionRules.ShouldCollapseAfterDeactivation(
        PanelDisplayState.ExpandedPanel,
        isWindowActive: false,
        isApplicationMenuOpen: false),
    "inactive expanded panel collapses");
AssertEqual(
    false,
    PanelInteractionRules.ShouldCollapseAfterDeactivation(
        PanelDisplayState.ExpandedPanel,
        isWindowActive: true,
        isApplicationMenuOpen: false),
    "reactivated window stays open");
AssertEqual(
    false,
    PanelInteractionRules.ShouldCollapseAfterDeactivation(
        PanelDisplayState.ExpandedPanel,
        isWindowActive: false,
        isApplicationMenuOpen: true),
    "application menu keeps panel open");
AssertEqual(
    false,
    PanelInteractionRules.ShouldCollapseAfterDeactivation(
        PanelDisplayState.CollapsedSliver,
        isWindowActive: false,
        isApplicationMenuOpen: false),
    "collapsed state does not transition again");

Console.WriteLine("Panel layout tests passed.");

static void AssertRect(Rect actual, Rect expected, string name)
{
    if (actual != expected)
    {
        throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
    }
}

static void AssertEqual<T>(T expected, T actual, string name)
    where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
    }
}

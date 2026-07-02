namespace Horizon.App;

internal static class PanelInteractionRules
{
    internal static bool ShouldCollapseAfterDeactivation(
        PanelDisplayState state,
        bool isWindowActive,
        bool isApplicationMenuOpen)
    {
        return state == PanelDisplayState.ExpandedPanel &&
               !isWindowActive &&
               !isApplicationMenuOpen;
    }
}

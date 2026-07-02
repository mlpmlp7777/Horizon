using System.Windows;

namespace Horizon.App;

internal static class PanelLayout
{
    internal const double ExpandedPanelWidth = 360;
    internal const double SliverWidth = 6;
    internal const double SliverHeight = 72;
    internal const double HoverHandleWidth = 30;
    internal const double HoverHandleHeight = 92;
    internal const double TopMargin = 20;
    internal const double DragThreshold = 4;

    internal static Rect GetBounds(
        PanelDisplayState state,
        Rect workArea,
        double requestedHandleTop)
    {
        var handleTop = CoerceHandleTop(workArea, requestedHandleTop);

        return state switch
        {
            PanelDisplayState.CollapsedSliver => new Rect(
                workArea.Right - SliverWidth,
                handleTop + ((HoverHandleHeight - SliverHeight) / 2),
                SliverWidth,
                SliverHeight),
            PanelDisplayState.HoverHandle => new Rect(
                workArea.Right - HoverHandleWidth,
                handleTop,
                HoverHandleWidth,
                HoverHandleHeight),
            PanelDisplayState.ExpandedPanel => new Rect(
                workArea.Right - ExpandedPanelWidth,
                workArea.Top,
                ExpandedPanelWidth,
                workArea.Height),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }

    internal static double CoerceHandleTop(Rect workArea, double requestedTop)
    {
        var minTop = workArea.Top + TopMargin;
        var maxTop = workArea.Bottom - HoverHandleHeight - TopMargin;
        return Math.Clamp(requestedTop, minTop, maxTop);
    }

    internal static bool IsDragDelta(double delta) => Math.Abs(delta) > DragThreshold;
}

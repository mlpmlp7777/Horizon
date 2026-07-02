# Horizon Three-State Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the oversized collapsed handle with a 6×72 px edge sliver that reveals a draggable 30×92 px handle on hover and opens the existing 360 px panel on click.

**Architecture:** Keep one WPF window and model its presentation with one `PanelDisplayState` enum. Put dimensions, bounds, clamping, and the drag threshold in a pure helper; keep timers, mouse capture, animations, and shell visibility in `MainWindow`.

**Tech Stack:** C# 13, .NET 9, WPF, dependency-free console test harness

---

## File Structure

- Create `src/Horizon.App/PanelDisplayState.cs` for the three exclusive states.
- Create `src/Horizon.App/PanelLayout.cs` for geometry and drag-threshold rules.
- Modify `src/Horizon.App/AssemblyInfo.cs` to expose internals to tests.
- Create `tests/Horizon.App.Tests/` as a package-free executable test harness.
- Modify `Horizon.sln` to include the test harness.
- Modify `src/Horizon.App/MainWindow.xaml` only around the collapsed shell.
- Modify `src/Horizon.App/MainWindow.xaml.cs` only around state, input, and animation logic.

`MainWindow.xaml` and `MainWindow.xaml.cs` already contain user-owned working-tree changes. Preserve them and do not stage or commit those files without explicit approval for their broader diff.

### Task 1: Add Tested Panel Geometry and State Types

**Files:**
- Create: `tests/Horizon.App.Tests/Horizon.App.Tests.csproj`
- Create: `tests/Horizon.App.Tests/Program.cs`
- Create: `src/Horizon.App/PanelDisplayState.cs`
- Create: `src/Horizon.App/PanelLayout.cs`
- Modify: `src/Horizon.App/AssemblyInfo.cs`
- Modify: `Horizon.sln`

- [ ] **Step 1: Create the failing test harness**

Create `tests/Horizon.App.Tests/Horizon.App.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Horizon.App\Horizon.App.csproj" />
  </ItemGroup>
</Project>
```

Create `tests/Horizon.App.Tests/Program.cs`:

```csharp
using System.Windows;
using Horizon.App;

var area = new Rect(100, 50, 1200, 800);
AssertRect(PanelLayout.GetBounds(PanelDisplayState.CollapsedSliver, area, 200), new Rect(1294, 210, 6, 72), "sliver");
AssertRect(PanelLayout.GetBounds(PanelDisplayState.HoverHandle, area, 200), new Rect(1270, 200, 30, 92), "handle");
AssertRect(PanelLayout.GetBounds(PanelDisplayState.ExpandedPanel, area, 200), new Rect(940, 50, 360, 800), "panel");
AssertEqual(70d, PanelLayout.CoerceHandleTop(area, -500), "top clamp");
AssertEqual(738d, PanelLayout.CoerceHandleTop(area, 5000), "bottom clamp");
AssertEqual(false, PanelLayout.IsDragDelta(4), "click threshold");
AssertEqual(true, PanelLayout.IsDragDelta(4.01), "drag threshold");
Console.WriteLine("Panel layout tests passed.");

static void AssertRect(Rect actual, Rect expected, string name)
{
    if (actual != expected) throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
}

static void AssertEqual<T>(T expected, T actual, string name) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
}
```

Add it to the solution:

```powershell
dotnet sln Horizon.sln add tests\Horizon.App.Tests\Horizon.App.Tests.csproj --solution-folder tests
```

- [ ] **Step 2: Verify the test fails before implementation**

Run `dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj`.

Expected: compiler errors because `PanelLayout` and `PanelDisplayState` do not exist.

- [ ] **Step 3: Implement the pure state and geometry types**

Create `src/Horizon.App/PanelDisplayState.cs`:

```csharp
namespace Horizon.App;

internal enum PanelDisplayState
{
    CollapsedSliver,
    HoverHandle,
    ExpandedPanel
}
```

Create `src/Horizon.App/PanelLayout.cs`:

```csharp
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

    internal static Rect GetBounds(PanelDisplayState state, Rect area, double requestedTop)
    {
        var top = CoerceHandleTop(area, requestedTop);
        return state switch
        {
            PanelDisplayState.CollapsedSliver => new Rect(area.Right - SliverWidth, top + 10, SliverWidth, SliverHeight),
            PanelDisplayState.HoverHandle => new Rect(area.Right - HoverHandleWidth, top, HoverHandleWidth, HoverHandleHeight),
            PanelDisplayState.ExpandedPanel => new Rect(area.Right - ExpandedPanelWidth, area.Top, ExpandedPanelWidth, area.Height),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }

    internal static double CoerceHandleTop(Rect area, double requestedTop)
    {
        var minTop = area.Top + TopMargin;
        var maxTop = area.Bottom - HoverHandleHeight - TopMargin;
        return Math.Clamp(requestedTop, minTop, maxTop);
    }

    internal static bool IsDragDelta(double delta) => Math.Abs(delta) > DragThreshold;
}
```

Add `using System.Runtime.CompilerServices;` and `[assembly: InternalsVisibleTo("Horizon.App.Tests")]` to `AssemblyInfo.cs`, leaving its existing `ThemeInfo` declaration intact.

- [ ] **Step 4: Run tests and the solution build**

Run:

```powershell
dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj
dotnet build Horizon.sln
```

Expected: `Panel layout tests passed.` and `Build succeeded.`

- [ ] **Step 5: Commit only isolated clean/new files**

```powershell
git add Horizon.sln src\Horizon.App\AssemblyInfo.cs src\Horizon.App\PanelDisplayState.cs src\Horizon.App\PanelLayout.cs tests\Horizon.App.Tests
git commit -m "test: cover panel state geometry"
```

### Task 2: Implement Three-State WPF Interaction

**Files:**
- Modify: `src/Horizon.App/MainWindow.xaml:663-715`
- Modify: `src/Horizon.App/MainWindow.xaml.cs:1-28,38-68,283-473`

- [ ] **Step 1: Replace the old collapsed handle with two shells**

Replace `CollapsedHandleShell` with:

```xml
<Border x:Name="CollapsedSliverShell"
        Width="6" Height="72" Background="#173B63"
        CornerRadius="6,0,0,6" HorizontalAlignment="Right"
        VerticalAlignment="Top" Visibility="Collapsed"
        MouseEnter="CollapsedSliverShell_OnMouseEnter" />

<Border x:Name="HoverHandleShell"
        Width="30" Height="92" Background="#173B63"
        BorderBrush="#0E2640" BorderThickness="1"
        CornerRadius="14,0,0,14" HorizontalAlignment="Right"
        VerticalAlignment="Top" Visibility="Collapsed" Cursor="Hand"
        MouseEnter="HoverHandleShell_OnMouseEnter"
        MouseLeave="HoverHandleShell_OnMouseLeave"
        LostMouseCapture="HoverHandleShell_OnLostMouseCapture"
        PreviewMouseLeftButtonDown="HoverHandleShell_OnMouseLeftButtonDown"
        PreviewMouseMove="HoverHandleShell_OnMouseMove"
        PreviewMouseLeftButtonUp="HoverHandleShell_OnMouseLeftButtonUp">
    <Grid Margin="5,9">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>
        <Border Width="16" Height="3" CornerRadius="2" HorizontalAlignment="Center" Background="#6E8FB4" />
        <TextBlock Grid.Row="1" Text="H" HorizontalAlignment="Center" VerticalAlignment="Center"
                   FontSize="15" FontWeight="Bold" Foreground="White" />
        <TextBlock Grid.Row="2" Text="拖" HorizontalAlignment="Center" FontSize="9" Foreground="#B6CAE0" />
    </Grid>
</Border>
```

- [ ] **Step 2: Introduce enum state, hover timer, and exact timings**

Add `using System.Windows.Threading;`. Replace the old handle constants and `_isExpanded` with:

```csharp
private const double ExpandedPanelInset = 6;
private const int HoverAnimationMilliseconds = 120;
private const int PanelAnimationMilliseconds = 220;
private static readonly TimeSpan HoverLeaveDelay = TimeSpan.FromMilliseconds(250);
private readonly DispatcherTimer _hoverLeaveTimer;
private PanelDisplayState _panelState = PanelDisplayState.CollapsedSliver;
private int _animationVersion;
```

Keep the existing view-model, status-update, and drag fields. Initialize the timer after `DataContext`:

```csharp
_hoverLeaveTimer = new DispatcherTimer { Interval = HoverLeaveDelay };
_hoverLeaveTimer.Tick += HoverLeaveTimer_OnTick;
```

Replace the load and resize handlers with:

```csharp
private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
{
    _collapsedButtonTop = PanelLayout.CoerceHandleTop(SystemParameters.WorkArea, _viewModel.GetCollapsedButtonTop());
    ConfigureWindow();
    SetPanelState(PanelDisplayState.CollapsedSliver, animated: false);
}

private void MainWindow_OnLocationOrSizeChanged(object sender, EventArgs e)
{
    if (_isAnimating) return;
    _collapsedButtonTop = PanelLayout.CoerceHandleTop(SystemParameters.WorkArea, _collapsedButtonTop);
    ApplyWindowState(animated: false, animationMilliseconds: 0);
}
```

In the existing Escape handler, use this condition and leave the editor branch unchanged:

```csharp
if (e.Key == Key.Escape && _panelState == PanelDisplayState.ExpandedPanel)
{
    CollapsePanel();
    e.Handled = true;
}
```

- [ ] **Step 3: Implement hover and drag event methods**

Implement these behaviors with the exact named handlers wired in XAML:

```csharp
private void CollapsedSliverShell_OnMouseEnter(object sender, MouseEventArgs e)
{
    CancelHoverLeaveCountdown();
    SetPanelState(PanelDisplayState.HoverHandle);
}

private void HoverHandleShell_OnMouseEnter(object sender, MouseEventArgs e) => CancelHoverLeaveCountdown();

private void HoverHandleShell_OnMouseLeave(object sender, MouseEventArgs e)
{
    if (!_isDraggingHandle) StartHoverLeaveCountdown();
}
```

Add the remaining handlers exactly as follows:

```csharp
private void HoverHandleShell_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    if (_panelState != PanelDisplayState.HoverHandle || !TryGetCursorPosition(out var cursor)) return;
    CancelHoverLeaveCountdown();
    _isDraggingHandle = true;
    _dragMoved = false;
    _dragStartTop = _collapsedButtonTop;
    _dragStartScreenY = cursor.Y;
    HoverHandleShell.CaptureMouse();
    e.Handled = true;
}

private void HoverHandleShell_OnMouseMove(object sender, MouseEventArgs e)
{
    if (!_isDraggingHandle || e.LeftButton != MouseButtonState.Pressed || !TryGetCursorPosition(out var cursor)) return;
    var delta = cursor.Y - _dragStartScreenY;
    if (!_dragMoved && !PanelLayout.IsDragDelta(delta)) return;
    _dragMoved = true;
    _collapsedButtonTop = PanelLayout.CoerceHandleTop(SystemParameters.WorkArea, _dragStartTop + delta);
    ApplyWindowState(animated: false, animationMilliseconds: 0);
}

private void HoverHandleShell_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
{
    if (!_isDraggingHandle) return;
    var wasDrag = _dragMoved;
    _isDraggingHandle = false;
    HoverHandleShell.ReleaseMouseCapture();
    if (wasDrag)
    {
        _viewModel.UpdateCollapsedButtonTop(_collapsedButtonTop);
        if (!HoverHandleShell.IsMouseOver) StartHoverLeaveCountdown();
    }
    else
    {
        ExpandPanel();
    }
    e.Handled = true;
}

private void HoverHandleShell_OnLostMouseCapture(object sender, MouseEventArgs e)
{
    if (!_isDraggingHandle) return;
    _isDraggingHandle = false;
    if (_dragMoved) _viewModel.UpdateCollapsedButtonTop(_collapsedButtonTop);
    if (_panelState == PanelDisplayState.HoverHandle && !HoverHandleShell.IsMouseOver)
        StartHoverLeaveCountdown();
}
```

- [ ] **Step 4: Implement interruption-safe state transitions**

Use one setter to choose animation duration:

```csharp
private void SetPanelState(PanelDisplayState target, bool animated = true)
{
    var previous = _panelState;
    if (previous == target)
    {
        if (!animated) ApplyWindowState(false, 0);
        return;
    }

    _panelState = target;
    var milliseconds = previous == PanelDisplayState.ExpandedPanel || target == PanelDisplayState.ExpandedPanel
        ? PanelAnimationMilliseconds
        : HoverAnimationMilliseconds;
    ApplyWindowState(animated, milliseconds);
}
```

Replace the old expand, collapse, countdown, bounds, animation, and shell-visibility methods with:

```csharp
private void ConfigureWindow()
{
    Topmost = true;
    ApplyWindowState(animated: false, animationMilliseconds: 0);
}

private void ExpandPanel(bool animated = true)
{
    CancelHoverLeaveCountdown();
    SetPanelState(PanelDisplayState.ExpandedPanel, animated);
}

private void CollapsePanel(bool animated = true)
{
    if (_panelState != PanelDisplayState.ExpandedPanel || _viewModel.IsEditorOpen) return;
    SetPanelState(PanelDisplayState.CollapsedSliver, animated);
}

private void StartHoverLeaveCountdown()
{
    if (_panelState != PanelDisplayState.HoverHandle || _isDraggingHandle) return;
    _hoverLeaveTimer.Stop();
    _hoverLeaveTimer.Start();
}

private void CancelHoverLeaveCountdown() => _hoverLeaveTimer.Stop();

private void HoverLeaveTimer_OnTick(object? sender, EventArgs e)
{
    CancelHoverLeaveCountdown();
    if (_panelState == PanelDisplayState.HoverHandle && !_isDraggingHandle && !HoverHandleShell.IsMouseOver)
        SetPanelState(PanelDisplayState.CollapsedSliver);
}

private void ApplyWindowState(bool animated, int animationMilliseconds)
{
    var targetState = _panelState;
    var bounds = PanelLayout.GetBounds(targetState, SystemParameters.WorkArea, _collapsedButtonTop);
    var targetOpacity = targetState == PanelDisplayState.ExpandedPanel ? 1.0 : 0.98;
    UpdateShellVisibility(targetState);
    if (!animated)
    {
        _animationVersion++;
        _isAnimating = false;
        ApplyBounds(bounds);
        Opacity = targetOpacity;
        return;
    }
    _isAnimating = true;
    AnimateWindow(bounds, targetOpacity, animationMilliseconds, targetState);
}

private void ApplyBounds(Rect bounds)
{
    BeginAnimation(WidthProperty, null);
    BeginAnimation(HeightProperty, null);
    BeginAnimation(LeftProperty, null);
    BeginAnimation(TopProperty, null);
    BeginAnimation(OpacityProperty, null);
    Left = bounds.Left;
    Top = bounds.Top;
    Width = bounds.Width;
    Height = bounds.Height;
}

private void AnimateWindow(Rect bounds, double targetOpacity, int milliseconds, PanelDisplayState targetState)
{
    var version = ++_animationVersion;
    var duration = new Duration(TimeSpan.FromMilliseconds(milliseconds));
    var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
    BeginAnimation(WidthProperty, CreateAnimation(bounds.Width, duration, easing));
    BeginAnimation(HeightProperty, CreateAnimation(bounds.Height, duration, easing));
    BeginAnimation(LeftProperty, CreateAnimation(bounds.Left, duration, easing));
    BeginAnimation(TopProperty, CreateAnimation(bounds.Top, duration, easing));
    var opacityAnimation = CreateAnimation(targetOpacity, duration, easing);
    opacityAnimation.Completed += (_, _) =>
    {
        if (version != _animationVersion || targetState != _panelState) return;
        _isAnimating = false;
        ApplyBounds(bounds);
        Opacity = targetOpacity;
        UpdateShellVisibility(targetState);
    };
    BeginAnimation(OpacityProperty, opacityAnimation);
}

private static DoubleAnimation CreateAnimation(double target, Duration duration, IEasingFunction easing) =>
    new(target, duration) { EasingFunction = easing };

private void UpdateShellVisibility(PanelDisplayState state)
{
    ExpandedPanelShell.Visibility = state == PanelDisplayState.ExpandedPanel ? Visibility.Visible : Visibility.Collapsed;
    CollapsedSliverShell.Visibility = state == PanelDisplayState.CollapsedSliver ? Visibility.Visible : Visibility.Collapsed;
    HoverHandleShell.Visibility = state == PanelDisplayState.HoverHandle ? Visibility.Visible : Visibility.Collapsed;
    if (state == PanelDisplayState.ExpandedPanel)
        ExpandedPanelShell.Margin = new Thickness(ExpandedPanelInset);
}
```

Remove the old `GetExpandedBounds`, `GetCollapsedBounds`, `CoerceCollapsedButtonTop`, `CollapsedHandleShell_*`, `_isExpanded`, and old handle-size constants.

- [ ] **Step 5: Build, test, and inspect without staging dirty UI files**

Run:

```powershell
dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj
dotnet build Horizon.sln
git diff --check
git diff -- src\Horizon.App\MainWindow.xaml src\Horizon.App\MainWindow.xaml.cs
git status --short
```

Expected: tests pass, build succeeds, no whitespace errors, and the two pre-existing dirty UI files remain unstaged.

### Task 3: Publish and Verify the Interaction Loop

**Files:**
- Verify: `artifacts/Horizon.App/Horizon.App.exe`
- Verify persistence: `%LocalAppData%\Horizon\data\horizon-data.json`

- [ ] **Step 1: Publish Release output**

Run:

```powershell
dotnet publish src\Horizon.App\Horizon.App.csproj -c Release -o artifacts\Horizon.App
```

Expected: success and an updated `artifacts\Horizon.App\Horizon.App.exe`. If locked, stop only the running Horizon app and retry.

- [ ] **Step 2: Launch and verify all three states**

Run:

```powershell
Start-Process -FilePath (Resolve-Path 'artifacts\Horizon.App\Horizon.App.exe')
```

Verify: 6×72 sliver at the right edge; 30×92 handle after 120 ms hover; return after 250 ms leave; no flicker when re-entering; click opens the existing 360 px panel; “收起” returns to the same center.

- [ ] **Step 3: Verify dragging and persistence**

Verify: movement up to 4 px remains a click; movement over 4 px drags; leaving while held continues the drag; releasing after a drag never opens; top and bottom retain 20 px margins; capture loss ends cleanly; restart restores the saved clamped top.

- [ ] **Step 4: Report without broad staging**

Run `git status --short`. Report tests, build, publish path, runtime checks, and exact changed files. Leave the dirty `MainWindow` files unstaged unless the user explicitly requests staging or committing their complete diff.

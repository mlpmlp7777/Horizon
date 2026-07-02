# Horizon Click-Outside Collapse Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the full panel's collapse button and collapse the panel when Horizon loses activation, while preserving any open editor and unsaved draft.

**Architecture:** Use WPF's `Window.Deactivated` event and defer the decision to `DispatcherPriority.ApplicationIdle` so application menus can settle first. Keep the decision in a pure `PanelInteractionRules` helper for deterministic tests; keep the actual state transition in `MainWindow`.

**Tech Stack:** C# 13, .NET 9, WPF, existing dependency-free test harness

---

## File Structure

- Create `src/Horizon.App/PanelInteractionRules.cs`: pure deactivation decision.
- Modify `tests/Horizon.App.Tests/Program.cs`: cover expanded, active, menu-open, and collapsed cases.
- Modify `src/Horizon.App/MainWindow.xaml`: wire `Deactivated` and remove the button.
- Modify `src/Horizon.App/MainWindow.xaml.cs`: remove the button handler, defer the deactivation check, and collapse without cancelling the editor.

The two `MainWindow` files already contain broader user-owned working-tree changes. Patch only the named regions and leave them unstaged.

### Task 1: Add a Tested Deactivation Rule

**Files:**
- Create: `src/Horizon.App/PanelInteractionRules.cs`
- Modify: `tests/Horizon.App.Tests/Program.cs`

- [ ] **Step 1: Add failing rule assertions**

Insert before the success message in `tests/Horizon.App.Tests/Program.cs`:

```csharp
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
```

- [ ] **Step 2: Verify the test fails**

Run:

```powershell
dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj
```

Expected: compiler errors because `PanelInteractionRules` does not exist.

- [ ] **Step 3: Implement the pure rule**

Create `src/Horizon.App/PanelInteractionRules.cs`:

```csharp
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
```

- [ ] **Step 4: Run the test and build**

Run:

```powershell
dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj
dotnet build Horizon.sln
```

Expected: `Panel layout tests passed.` and `Build succeeded.` with zero warnings and errors.

- [ ] **Step 5: Commit only the isolated rule and test**

```powershell
git add src\Horizon.App\PanelInteractionRules.cs tests\Horizon.App.Tests\Program.cs
git commit -m "test: cover panel deactivation rules"
```

### Task 2: Remove the Button and Collapse on Deactivation

**Files:**
- Modify: `src/Horizon.App/MainWindow.xaml:19-22,506-527`
- Modify: `src/Horizon.App/MainWindow.xaml.cs:65-100,404-445`

- [ ] **Step 1: Wire window deactivation and remove the button**

Add this Window event next to `Loaded`:

```xml
Deactivated="MainWindow_OnDeactivated"
```

Delete only this button:

```xml
<Button Style="{StaticResource ActionButtonStyle}"
        Content="收起"
        Click="CollapsePanelButton_OnClick" />
```

Keep the `新增内容`, `设置`, archive, and history buttons unchanged.

- [ ] **Step 2: Remove the obsolete click handler**

Delete:

```csharp
private void CollapsePanelButton_OnClick(object sender, RoutedEventArgs e)
{
    CollapsePanel();
}
```

- [ ] **Step 3: Add deferred outside-collapse behavior**

Add beside the existing window event handlers:

```csharp
private void MainWindow_OnDeactivated(object? sender, EventArgs e)
{
    Dispatcher.BeginInvoke(
        DispatcherPriority.ApplicationIdle,
        new Action(CollapsePanelAfterDeactivation));
}

private void CollapsePanelAfterDeactivation()
{
    var isApplicationMenuOpen = QuickAddButton.ContextMenu?.IsOpen == true;
    if (!PanelInteractionRules.ShouldCollapseAfterDeactivation(
            _panelState,
            IsActive,
            isApplicationMenuOpen))
    {
        return;
    }

    SetPanelState(PanelDisplayState.CollapsedSliver);
}
```

This path intentionally calls `SetPanelState` directly. It does not call `CollapsePanel`, `_viewModel.CancelEditor`, or any save method, so an open editor and its draft remain intact.

- [ ] **Step 4: Build and run all deterministic tests**

Run:

```powershell
dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj
dotnet build Horizon.sln
git diff --check
```

Expected: tests pass, the solution builds with zero warnings and errors, and no whitespace errors are reported.

### Task 3: Publish and Verify Runtime Behavior

**Files:**
- Verify: `artifacts/Horizon.App/Horizon.App.exe`

- [ ] **Step 1: Stop only the running Horizon process and publish**

Run:

```powershell
Get-Process Horizon.App -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet publish src\Horizon.App\Horizon.App.csproj -c Release -o artifacts\Horizon.App
```

Expected: Release publish succeeds and updates `artifacts\Horizon.App\Horizon.App.exe`.

- [ ] **Step 2: Launch the published app**

Using the Windows app-control runtime, run:

```javascript
await sky.launch_app({
  app: String.raw`D:\工作\个人\记录程序\Horizon\artifacts\Horizon.App\Horizon.App.exe`,
});
```

Expected: Horizon launches in the 6×72 px trigger state.

- [ ] **Step 3: Verify click-away and draft preservation**

Verify these exact cases:

1. Open the panel; the header has no `收起` button.
2. Click another application or the desktop; the panel returns to the sliver.
3. Open `新增内容`; interacting with its context menu does not collapse the panel.
4. Open an editor, enter a draft, then click outside; the panel collapses.
5. Reopen Horizon; the same editor and draft remain.
6. Press Escape while editing; the editor cancels. Press Escape again; the panel collapses.

- [ ] **Step 4: Leave broader dirty UI files unstaged**

Run `git status --short`. Report validation and the executable path. Do not stage `MainWindow.xaml` or `MainWindow.xaml.cs` because their diffs include pre-existing user changes.

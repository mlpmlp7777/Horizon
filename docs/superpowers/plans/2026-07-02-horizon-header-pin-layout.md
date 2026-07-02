# Horizon Header and Pin Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove search, fix top-left clipping, and add a persisted pin toggle that suppresses click-away collapse.

**Architecture:** Store `IsPinned` in `HorizonSettings`, expose it through `MainViewModel`, and include it in the existing pure deactivation rule. Recompose only the header rows in XAML and leave the panel state machine unchanged.

**Tech Stack:** C# 13, .NET 9, WPF, existing console test harness

---

### Task 1: Persist and Test Pin State

**Files:**
- Modify: `src/Horizon.App/Models/HorizonModels.cs`
- Modify: `src/Horizon.App/PanelInteractionRules.cs`
- Modify: `tests/Horizon.App.Tests/Program.cs`
- Modify: `src/Horizon.App/ViewModels/MainViewModel.cs`

- [ ] Add failing assertions proving expanded/inactive windows collapse only when `isPinned` is false.

```csharp
AssertEqual(false, PanelInteractionRules.ShouldCollapseAfterDeactivation(
    PanelDisplayState.ExpandedPanel, false, false, true), "pinned panel stays open");
AssertEqual(true, PanelInteractionRules.ShouldCollapseAfterDeactivation(
    PanelDisplayState.ExpandedPanel, false, false, false), "unpinned panel collapses");
```

- [ ] Run `dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj` and expect a signature mismatch.

- [ ] Add `public bool IsPinned { get; set; }` to `HorizonSettings`; add `isPinned` to `ShouldCollapseAfterDeactivation` and require `!isPinned`.

```csharp
return state == PanelDisplayState.ExpandedPanel &&
       !isWindowActive && !isApplicationMenuOpen && !isPinned;
```

- [ ] Add ViewModel API and persistence.

```csharp
public bool IsPinned => _data.Settings.IsPinned;
public string PinButtonText => IsPinned ? "已置顶" : "置顶";

public void TogglePinned()
{
    _data.Settings.IsPinned = !_data.Settings.IsPinned;
    _store.Save(_data);
    OnPropertyChanged(nameof(IsPinned));
    OnPropertyChanged(nameof(PinButtonText));
    StatusMessage = IsPinned ? "主面板已置顶。" : "已恢复点击外部自动收起。";
}
```

- [ ] Run tests and `dotnet build Horizon.sln`; expect zero warnings/errors.

### Task 2: Recompose Header and Wire Pin

**Files:**
- Modify: `src/Horizon.App/MainWindow.xaml:482-544`
- Modify: `src/Horizon.App/MainWindow.xaml.cs:72-130`

- [ ] Change the internal panel margin to `18,22,16,16`; remove the search row and search `TextBox`; use rows `Auto,Auto,Auto,*` for title, actions, status, content.

- [ ] Add a first-row `ToggleButton` bound one-way to `IsPinned`, with click handler and selected blue styling; move the four actions to the second row.

```xml
<ToggleButton Grid.Column="1" Content="{Binding PinButtonText}"
              IsChecked="{Binding IsPinned, Mode=OneWay}"
              Click="PinButton_OnClick" />
```

- [ ] Add the click handler and pass pin state to the deactivation rule.

```csharp
private void PinButton_OnClick(object sender, RoutedEventArgs e) => _viewModel.TogglePinned();
```

```csharp
PanelInteractionRules.ShouldCollapseAfterDeactivation(
    _panelState, IsActive, isApplicationMenuOpen, _viewModel.IsPinned)
```

- [ ] Run tests, build, `git diff --check`, and inspect the 360 px header layout.

### Task 3: Publish Checkpoint

- [ ] Stop only `Horizon.App`, publish Release to `artifacts\Horizon.App`, launch it, and verify search removal, unclipped title, pin behavior, and restart persistence.
- [ ] Leave pre-existing dirty `MainWindow` and ViewModel/model files unstaged.

# Horizon Bulk Project Expansion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add one dynamic main-page button that expands or collapses every currently visible weekly and long-term project section.

**Architecture:** `MainViewModel` derives the button label and enabled state from the two current section collections, updates all visible expansion keys in one method, saves once, and refreshes once. `MainWindow` adds one compact button and delegates its click to the ViewModel; the existing executable test harness covers mixed state, persistence, filtering, and XAML wiring.

**Tech Stack:** .NET 9, WPF XAML, C#, existing JSON settings and `Horizon.App.Tests` executable harness

---

### Task 1: Add failing ViewModel and XAML contract tests

**Files:**
- Modify: `tests/Horizon.App.Tests/Program.cs`

- [ ] Insert this block after the existing current-view search and archive assertions, while both `project` and `emptyProject` are available:

```csharp
AssertEqual(true, viewModel.CanBulkToggleProjectExpansion,
    "current view enables the bulk expansion control");
AssertEqual("全部展开", viewModel.BulkProjectExpansionActionText,
    "collapsed projects offer the expand-all action");

AssertEqual(true, viewModel.ToggleAllVisibleProjectExpansions(),
    "bulk action expands when at least one visible project is collapsed");
AssertEqual(true, viewModel.WeeklySections.All(section => section.IsExpanded),
    "bulk action expands every visible weekly project");
AssertEqual(true, viewModel.LongTermSections.All(section => section.IsExpanded),
    "bulk action expands every visible long-term project");
AssertEqual("全部收起", viewModel.BulkProjectExpansionActionText,
    "fully expanded projects offer the collapse-all action");

viewModel.ToggleProjectExpansion(emptyProject.Id, ProjectSectionKind.Weekly);
AssertEqual("全部展开", viewModel.BulkProjectExpansionActionText,
    "mixed project state offers the expand-all action");
viewModel.ToggleAllVisibleProjectExpansions();
AssertEqual(true, viewModel.WeeklySections.All(section => section.IsExpanded),
    "mixed project state expands to a uniform state");

AssertEqual(false, viewModel.ToggleAllVisibleProjectExpansions(),
    "fully expanded projects collapse together");
AssertEqual(true, viewModel.WeeklySections.All(section => !section.IsExpanded),
    "bulk collapse closes every visible weekly project");
AssertEqual(true, viewModel.LongTermSections.All(section => !section.IsExpanded),
    "bulk collapse closes every visible long-term project");

viewModel.SearchText = project.Name;
viewModel.ToggleAllVisibleProjectExpansions();
viewModel.SearchText = string.Empty;
AssertEqual(true,
    viewModel.WeeklySections.Single(section => section.ProjectId == project.Id).IsExpanded,
    "filtered bulk action expands the visible weekly project");
AssertEqual(false,
    viewModel.WeeklySections.Single(section => section.ProjectId == emptyProject.Id).IsExpanded,
    "filtered bulk action leaves hidden weekly projects unchanged");
AssertEqual(false,
    viewModel.LongTermSections.Single(section => section.ProjectId == emptyProject.Id).IsExpanded,
    "filtered bulk action leaves hidden long-term projects unchanged");
```

- [ ] Extend the existing XAML source contract block with:

```csharp
AssertContains("Content=\"{Binding BulkProjectExpansionActionText}\"", mainWindowXaml,
    "main page binds the bulk expansion button label");
AssertContains("IsEnabled=\"{Binding CanBulkToggleProjectExpansion}\"", mainWindowXaml,
    "main page disables bulk expansion when no project is visible");
AssertContains("Click=\"BulkProjectExpansionButton_OnClick\"", mainWindowXaml,
    "main page wires the bulk expansion click handler");
```
- [ ] Run `dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj` and verify it fails because the new ViewModel API is not yet defined.

### Task 2: Implement batch state and the single button

**Files:**
- Modify: `src/Horizon.App/ViewModels/MainViewModel.cs`
- Modify: `src/Horizon.App/MainWindow.xaml`
- Modify: `src/Horizon.App/MainWindow.xaml.cs`

- [ ] Add these derived properties to `MainViewModel`:

```csharp
public bool CanBulkToggleProjectExpansion =>
    WeeklySections.Any(section => section.IsCollapsible)
    || LongTermSections.Any(section => section.IsCollapsible);

public bool AreAllVisibleProjectSectionsExpanded =>
    CanBulkToggleProjectExpansion
    && WeeklySections
        .Concat(LongTermSections)
        .Where(section => section.IsCollapsible)
        .All(section => section.IsExpanded);

public string BulkProjectExpansionActionText =>
    AreAllVisibleProjectSectionsExpanded ? "全部收起" : "全部展开";
```

- [ ] Add this method to `MainViewModel`:

```csharp
public bool ToggleAllVisibleProjectExpansions()
{
    var sections = WeeklySections
        .Concat(LongTermSections)
        .Where(section => section.IsCollapsible)
        .ToList();
    if (sections.Count == 0)
    {
        return false;
    }

    var expand = sections.Any(section => !section.IsExpanded);
    foreach (var section in sections)
    {
        ProjectExpansionRules.SetExpanded(
            _data.Settings,
            section.SectionKind,
            section.ProjectId,
            expand);
    }

    var saved = true;
    try
    {
        _store.Save(_data);
    }
    catch
    {
        saved = false;
        StatusMessage = "批量展开状态已更新，但暂时无法保存。";
    }

    RefreshSections();
    if (saved)
    {
        StatusMessage = expand ? "已展开全部项目任务。" : "已收起全部项目任务。";
    }

    return expand;
}
```
- [ ] Immediately after assigning `LongTermSections` in `RefreshSections`, add:

```csharp
OnPropertyChanged(nameof(CanBulkToggleProjectExpansion));
OnPropertyChanged(nameof(AreAllVisibleProjectSectionsExpanded));
OnPropertyChanged(nameof(BulkProjectExpansionActionText));
```
- [ ] Replace the standalone “本周任务” heading with a two-column `Grid`; keep the heading on the left and add this button on the right:

```xml
<Button Grid.Column="1"
        MinWidth="64"
        Style="{StaticResource MiniButtonStyle}"
        Content="{Binding BulkProjectExpansionActionText}"
        IsEnabled="{Binding CanBulkToggleProjectExpansion}"
        AutomationProperties.HelpText="展开或收起当前页面中的全部项目任务"
        Click="BulkProjectExpansionButton_OnClick" />
```

- [ ] Add the code-behind handler:

```csharp
private void BulkProjectExpansionButton_OnClick(object sender, RoutedEventArgs e)
{
    _viewModel.ToggleAllVisibleProjectExpansions();
}
```

- [ ] Run the executable tests and `dotnet build Horizon.sln --disable-build-servers -nodeReuse:false -p:UseSharedCompilation=false`; expect all tests and compilation to pass with zero errors.
- [ ] Commit only the three source files and test file with `feat: add bulk project expansion toggle`.

### Task 3: Publish and verify

**Files:**
- Verify: `artifacts/Horizon.App/Horizon.App.exe`

- [ ] Run `dotnet publish src\Horizon.App\Horizon.App.csproj -c Release -o artifacts\Horizon.App --disable-build-servers -nodeReuse:false -p:UseSharedCompilation=false`.
- [ ] Verify the button is confined to the current main page, mixed state expands all, all-expanded state collapses all, and no-project state is disabled.
- [ ] Run `git diff --check` and confirm `AGENTS.md` and unrelated untracked files remain untouched.

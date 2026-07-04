# Horizon Rounded Flat Project Collapse Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add independently persisted project collapse states for weekly and long-term tasks, then restyle the full 360 px Horizon panel with the approved rounded, flat “Orbit Blue” visual system.

**Architecture:** Persist expansion state in `HorizonSettings` as a dictionary keyed by section kind and stable project ID. Keep key construction and cleanup in a focused pure-logic helper, project the state into immutable `ProjectSectionViewModel` instances, and let the WPF project-summary button call one view-model toggle method. Apply the visual system through centralized WPF resources plus shared project-summary/card templates so weekly and long-term sections stay visually consistent.

**Tech Stack:** C# 13, .NET 9, WPF/XAML, `System.Text.Json`, existing console-style `Horizon.App.Tests` test project.

---

## File map

- Create `src/Horizon.App/ProjectExpansionRules.cs`: stable key generation, default-collapsed lookup, toggle, project removal, and stale-key pruning.
- Modify `src/Horizon.App/Models/HorizonModels.cs`: persist `ProjectExpansionStates` in `HorizonSettings`.
- Modify `src/Horizon.App/Services/HorizonDataStore.cs`: normalize a missing/null expansion dictionary when loading old data.
- Modify `src/Horizon.App/ViewModels/ViewModelTypes.cs`: expose section kind, expanded state, glyph, completion count, and summary text to XAML.
- Modify `src/Horizon.App/ViewModels/MainViewModel.cs`: restore/toggle/persist independent section states, calculate summary data, keep history detail expanded, and prune removed project keys.
- Modify `src/Horizon.App/MainWindow.xaml.cs`: route project-summary clicks to the view model without affecting child controls.
- Modify `src/Horizon.App/MainWindow.xaml`: add Orbit Blue resources, shared rounded project summary, conditional task visibility, and consistent shell/editor styling.
- Modify `tests/Horizon.App.Tests/Program.cs`: cover legacy compatibility, independent keys, persistence, default collapse, summary calculation, and history behavior.

### Task 1: Persist independent project expansion state

**Files:**
- Create: `src/Horizon.App/ProjectExpansionRules.cs`
- Modify: `src/Horizon.App/Models/HorizonModels.cs:43-50`
- Modify: `src/Horizon.App/Services/HorizonDataStore.cs:38-53`
- Test: `tests/Horizon.App.Tests/Program.cs`

- [ ] **Step 1: Add failing compatibility and rule tests**

Add these assertions after the existing legacy settings assertion in `tests/Horizon.App.Tests/Program.cs`:

```csharp
var legacyExpansionSettings = JsonSerializer.Deserialize<HorizonSettings>("{}", JsonOptions.Default);
AssertEqual(0, legacyExpansionSettings?.ProjectExpansionStates.Count ?? -1,
    "legacy settings start with no project expansion state");

var expansionSettings = new HorizonSettings();
AssertEqual(false,
    ProjectExpansionRules.IsExpanded(expansionSettings, ProjectSectionKind.Weekly, "project-a"),
    "unknown weekly project defaults to collapsed");

ProjectExpansionRules.SetExpanded(
    expansionSettings,
    ProjectSectionKind.Weekly,
    "project-a",
    isExpanded: true);

AssertEqual(true,
    ProjectExpansionRules.IsExpanded(expansionSettings, ProjectSectionKind.Weekly, "project-a"),
    "weekly expansion is stored");
AssertEqual(false,
    ProjectExpansionRules.IsExpanded(expansionSettings, ProjectSectionKind.LongTerm, "project-a"),
    "long-term expansion is independent");

ProjectExpansionRules.SetExpanded(
    expansionSettings,
    ProjectSectionKind.LongTerm,
    "missing-project",
    isExpanded: true);
AssertEqual(true,
    ProjectExpansionRules.PruneUnknownProjects(expansionSettings, ["project-a"]),
    "stale project expansion keys are pruned");
AssertEqual(false,
    ProjectExpansionRules.IsExpanded(expansionSettings, ProjectSectionKind.LongTerm, "missing-project"),
    "pruned state is no longer visible");
```

- [ ] **Step 2: Run the test project and confirm it fails**

Run:

```powershell
dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj
```

Expected: compile failure because `ProjectExpansionStates`, `ProjectExpansionRules`, and `ProjectSectionKind` do not exist.

- [ ] **Step 3: Add the persisted setting**

Add this property to `HorizonSettings` in `src/Horizon.App/Models/HorizonModels.cs`:

```csharp
public Dictionary<string, bool> ProjectExpansionStates { get; set; } = [];
```

The resulting type is:

```csharp
public sealed class HorizonSettings
{
    public List<string> WeeklyProjectNames { get; set; } = [];
    public List<string> LongTermProjectNames { get; set; } = [];
    public double CollapsedButtonTop { get; set; } = 160;
    public bool IsPinned { get; set; }
    public bool StartWithWindows { get; set; } = true;
    public Dictionary<string, bool> ProjectExpansionStates { get; set; } = [];
}
```

- [ ] **Step 4: Implement the pure expansion-state rules**

Create `src/Horizon.App/ProjectExpansionRules.cs`:

```csharp
using Horizon.App.Models;

namespace Horizon.App;

public enum ProjectSectionKind
{
    Weekly,
    LongTerm
}

public static class ProjectExpansionRules
{
    public static bool IsExpanded(
        HorizonSettings settings,
        ProjectSectionKind sectionKind,
        string projectId)
    {
        return settings.ProjectExpansionStates.TryGetValue(
            BuildKey(sectionKind, projectId),
            out var isExpanded) && isExpanded;
    }

    public static void SetExpanded(
        HorizonSettings settings,
        ProjectSectionKind sectionKind,
        string projectId,
        bool isExpanded)
    {
        settings.ProjectExpansionStates[BuildKey(sectionKind, projectId)] = isExpanded;
    }

    public static bool Toggle(
        HorizonSettings settings,
        ProjectSectionKind sectionKind,
        string projectId)
    {
        var next = !IsExpanded(settings, sectionKind, projectId);
        SetExpanded(settings, sectionKind, projectId, next);
        return next;
    }

    public static bool RemoveProject(HorizonSettings settings, string projectId)
    {
        var weeklyRemoved = settings.ProjectExpansionStates.Remove(
            BuildKey(ProjectSectionKind.Weekly, projectId));
        var longTermRemoved = settings.ProjectExpansionStates.Remove(
            BuildKey(ProjectSectionKind.LongTerm, projectId));
        return weeklyRemoved || longTermRemoved;
    }

    public static bool PruneUnknownProjects(
        HorizonSettings settings,
        IEnumerable<string> knownProjectIds)
    {
        var knownKeys = knownProjectIds
            .SelectMany(projectId => new[]
            {
                BuildKey(ProjectSectionKind.Weekly, projectId),
                BuildKey(ProjectSectionKind.LongTerm, projectId)
            })
            .ToHashSet(StringComparer.Ordinal);

        var staleKeys = settings.ProjectExpansionStates.Keys
            .Where(key => !knownKeys.Contains(key))
            .ToList();

        foreach (var key in staleKeys)
        {
            settings.ProjectExpansionStates.Remove(key);
        }

        return staleKeys.Count > 0;
    }

    private static string BuildKey(ProjectSectionKind sectionKind, string projectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        var prefix = sectionKind == ProjectSectionKind.Weekly ? "weekly" : "longterm";
        return $"{prefix}:{projectId}";
    }
}
```

- [ ] **Step 5: Normalize old or explicitly null settings data**

Immediately after `var normalized = data ?? CreateEmpty();` in `HorizonDataStore.Load`, add:

```csharp
normalized.Settings ??= new HorizonSettings();
normalized.Settings.ProjectExpansionStates ??= [];
```

This keeps both missing JSON fields and explicit `null` values compatible.

- [ ] **Step 6: Run tests and commit the persistence slice**

Run:

```powershell
dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj
```

Expected: exit code `0` and the existing final success line.

Commit only this slice:

```powershell
git add src/Horizon.App/ProjectExpansionRules.cs src/Horizon.App/Models/HorizonModels.cs src/Horizon.App/Services/HorizonDataStore.cs tests/Horizon.App.Tests/Program.cs
git commit -m "feat: persist project expansion state"
```

### Task 2: Project expansion and summary behavior in the view model

**Files:**
- Modify: `src/Horizon.App/ViewModels/ViewModelTypes.cs:228-242`
- Modify: `src/Horizon.App/ViewModels/MainViewModel.cs:243-256, 940-1021, 1064-1166`
- Test: `tests/Horizon.App.Tests/Program.cs`

- [ ] **Step 1: Add a failing end-to-end view-model test**

Add the following isolated test block before the panel-layout assertions in `tests/Horizon.App.Tests/Program.cs`:

```csharp
var expansionViewModelRoot = Path.Combine(
    Path.GetTempPath(),
    $"Horizon.Expansion.Tests.{Guid.NewGuid():N}");
try
{
    var store = new HorizonDataStore(expansionViewModelRoot);
    var project = new ProjectItem { Id = "project-a", Name = "Horizon 产品迭代" };
    var emptyProject = new ProjectItem { Id = "project-empty", Name = "空项目" };
    var data = new HorizonDataFile
    {
        Projects = [project, emptyProject],
        WeeklyTasks =
        [
            new WeeklyTaskItem
            {
                ProjectId = project.Id,
                Title = "完成折叠交互",
                Status = WeeklyTaskStatus.InProgress,
                Progress = 40,
                WeekStartDate = DateHelper.GetStartOfWeek(DateTime.Today)
            }
        ],
        LongTermTasks =
        [
            new LongTermTaskItem
            {
                ProjectId = project.Id,
                Title = "发布 1.0",
                Status = LongTermTaskStatus.Planned,
                Progress = 20,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(2)
            }
        ]
    };
    store.Save(data);

    var viewModel = new Horizon.App.ViewModels.MainViewModel(
        store,
        new FakeStartupRegistrationService());

    AssertEqual(false,
        viewModel.WeeklySections.Single(section => section.ProjectId == project.Id).IsExpanded,
        "new weekly section starts collapsed");
    AssertEqual(false,
        viewModel.LongTermSections.Single(section => section.ProjectId == project.Id).IsExpanded,
        "new long-term section starts collapsed");
    AssertEqual("1 项任务 · 已完成 0 项 · 40%",
        viewModel.WeeklySections.Single(section => section.ProjectId == project.Id).ProjectMeta,
        "weekly project summary contains count, completion and progress");
    AssertEqual("0 项任务 · 已完成 0 项 · 0%",
        viewModel.WeeklySections.Single(section => section.ProjectId == emptyProject.Id).ProjectMeta,
        "empty project remains visible with a zero summary");

    AssertEqual(true,
        viewModel.ToggleProjectExpansion("project-a", ProjectSectionKind.Weekly),
        "weekly project expands");
    AssertEqual(true,
        viewModel.WeeklySections.Single(section => section.ProjectId == project.Id).IsExpanded,
        "expanded state reaches projected section");
    AssertEqual(false,
        viewModel.LongTermSections.Single(section => section.ProjectId == project.Id).IsExpanded,
        "weekly toggle does not affect long-term section");

    var reloaded = new Horizon.App.ViewModels.MainViewModel(
        new HorizonDataStore(expansionViewModelRoot),
        new FakeStartupRegistrationService());
    AssertEqual(true,
        reloaded.WeeklySections.Single(section => section.ProjectId == project.Id).IsExpanded,
        "weekly expansion survives reload");
    AssertEqual(false,
        reloaded.LongTermSections.Single(section => section.ProjectId == project.Id).IsExpanded,
        "long-term section remains independently collapsed after reload");
}
finally
{
    if (Directory.Exists(expansionViewModelRoot))
    {
        Directory.Delete(expansionViewModelRoot, recursive: true);
    }
}
```

- [ ] **Step 2: Run the test and confirm it fails**

Run:

```powershell
dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj
```

Expected: compile failure for missing `IsExpanded` and `ToggleProjectExpansion`.

- [ ] **Step 3: Extend the project-section projection**

Replace `ProjectSectionViewModel` in `ViewModelTypes.cs` with:

```csharp
public sealed class ProjectSectionViewModel
{
    public string ProjectId { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public ProjectSectionKind SectionKind { get; init; }
    public string ProjectMeta { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
    public string StatusBadgeBackground { get; init; } = "#DCE7FF";
    public string StatusBadgeForeground { get; init; } = "#2458D4";
    public string AccentBrush { get; init; } = "#2D68FF";
    public int Progress { get; init; }
    public string ProgressText => $"{Progress}%";
    public int TaskCount { get; init; }
    public int CompletedTaskCount { get; init; }
    public bool IsExpanded { get; init; }
    public bool IsCollapsible { get; init; } = true;
    public string ExpansionGlyph => IsExpanded ? "⌄" : "›";
    public IReadOnlyList<WeeklyTaskRowViewModel> WeeklyTasks { get; init; } = [];
    public IReadOnlyList<LongTermTaskRowViewModel> LongTermTasks { get; init; } = [];
    public bool ShowAddTaskAction { get; init; } = true;
}
```

- [ ] **Step 4: Add the toggle method and load-time pruning**

Add this public method near `TogglePinned` in `MainViewModel.cs`:

```csharp
public bool ToggleProjectExpansion(string projectId, ProjectSectionKind sectionKind)
{
    if (!_data.Projects.Any(project => project.Id == projectId))
    {
        return false;
    }

    var isExpanded = ProjectExpansionRules.Toggle(_data.Settings, sectionKind, projectId);
    try
    {
        _store.Save(_data);
    }
    catch
    {
        StatusMessage = "展开状态已更新，但暂时无法保存。";
    }

    RefreshSections();
    return isExpanded;
}
```

At the start of `Load`, preserve the existing rollover logic and include expansion cleanup:

```csharp
var reconciled = WeeklyRolloverService.Reconcile(_data, DateTime.Today, DateTime.UtcNow);
var prunedExpansionStates = ProjectExpansionRules.PruneUnknownProjects(
    _data.Settings,
    _data.Projects.Select(project => project.Id));
if (reconciled | AutoUpdateProjectStatus() | prunedExpansionStates)
{
    _store.Save(_data);
}
```

In the existing project-deletion path, call this before persisting deletion:

```csharp
ProjectExpansionRules.RemoveProject(_data.Settings, project.Id);
```

- [ ] **Step 5: Keep empty projects visible in the current weekly and long-term sections**

Replace the current task-only grouping at `MainViewModel.RefreshSections` with lookups that can return an empty task sequence for a known project:

```csharp
var currentWeeklyTasks = activeWeeklyTasks
    .Where(task => task.WeekStartDate.Date == currentWeekStart.Date)
    .Where(task => WeeklyRolloverService.IsWeeklyInCurrentView(task, localToday))
    .ToLookup(task => task.ProjectId);

var weeklyProjectIds = ShowArchivedTasks
    ? currentWeeklyTasks.Select(group => group.Key)
    : visibleProjects.Keys;

WeeklySections = weeklyProjectIds
    .Select(projectId => BuildWeeklySection(
        visibleProjects[projectId],
        currentWeeklyTasks[projectId]))
    .OrderBy(section => section.ProjectName, StringComparer.CurrentCultureIgnoreCase)
    .ToList();

var currentLongTermTasks = activeLongTermTasks
    .Where(task => WeeklyRolloverService.IsLongTermInCurrentView(task, localToday))
    .ToLookup(task => task.ProjectId);

var longTermProjectIds = ShowArchivedTasks
    ? currentLongTermTasks.Select(group => group.Key)
    : visibleProjects.Keys;

LongTermSections = longTermProjectIds
    .Select(projectId => BuildLongTermSection(
        visibleProjects[projectId],
        currentLongTermTasks[projectId]))
    .OrderBy(section => section.ProjectName, StringComparer.CurrentCultureIgnoreCase)
    .ToList();
```

The current view therefore shows a `0 项任务 · 已完成 0 项 · 0%` summary for a project with no tasks. The archive view remains task-driven, so it does not fill with projects that have no archived content. Leave the existing history grouping unchanged.

- [ ] **Step 6: Project collapse state and complete summary data into current sections**

In `BuildWeeklySection`, calculate completion and return these additional values:

```csharp
var sectionProgress = rows.Count == 0 ? 0 : (int)Math.Round(rows.Average(row => row.Progress));
var completedTaskCount = rows.Count(row => row.Status == WeeklyTaskStatus.Done);

return new ProjectSectionViewModel
{
    ProjectId = project.Id,
    ProjectName = project.Name,
    SectionKind = ProjectSectionKind.Weekly,
    ProjectMeta = $"{rows.Count} 项任务 · 已完成 {completedTaskCount} 项 · {sectionProgress}%",
    StatusText = GetProjectStatusText(project.Status),
    StatusBadgeBackground = projectTone.Background,
    StatusBadgeForeground = projectTone.Foreground,
    AccentBrush = projectTone.Accent,
    Progress = sectionProgress,
    TaskCount = rows.Count,
    CompletedTaskCount = completedTaskCount,
    IsExpanded = !showAddTaskAction || ProjectExpansionRules.IsExpanded(
        _data.Settings,
        ProjectSectionKind.Weekly,
        project.Id),
    IsCollapsible = showAddTaskAction,
    WeeklyTasks = rows,
    ShowAddTaskAction = showAddTaskAction
};
```

In `BuildLongTermSection`, use the corresponding calculation and projection:

```csharp
var sectionProgress = rows.Count == 0 ? 0 : (int)Math.Round(rows.Average(row => row.Progress));
var completedTaskCount = rows.Count(row => row.Status == LongTermTaskStatus.Completed);

return new ProjectSectionViewModel
{
    ProjectId = project.Id,
    ProjectName = project.Name,
    SectionKind = ProjectSectionKind.LongTerm,
    ProjectMeta = $"{rows.Count} 项任务 · 已完成 {completedTaskCount} 项 · {sectionProgress}%",
    StatusText = GetProjectStatusText(project.Status),
    StatusBadgeBackground = projectTone.Background,
    StatusBadgeForeground = projectTone.Foreground,
    AccentBrush = projectTone.Accent,
    Progress = sectionProgress,
    TaskCount = rows.Count,
    CompletedTaskCount = completedTaskCount,
    IsExpanded = !showAddTaskAction || ProjectExpansionRules.IsExpanded(
        _data.Settings,
        ProjectSectionKind.LongTerm,
        project.Id),
    IsCollapsible = showAddTaskAction,
    LongTermTasks = rows,
    ShowAddTaskAction = showAddTaskAction
};
```

`showAddTaskAction: false` is already used by history detail, so history remains expanded and non-collapsible without changing the history flow.

- [ ] **Step 7: Run tests and commit the view-model slice**

Run:

```powershell
dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj
```

Expected: exit code `0`; the independent weekly/long-term and reload assertions pass.

Commit:

```powershell
git add src/Horizon.App/ViewModels/ViewModelTypes.cs src/Horizon.App/ViewModels/MainViewModel.cs tests/Horizon.App.Tests/Program.cs
git commit -m "feat: add collapsible project sections"
```

### Task 3: Add the rounded project-summary interaction in WPF

**Files:**
- Modify: `src/Horizon.App/MainWindow.xaml:228-605`
- Modify: `src/Horizon.App/MainWindow.xaml.cs:219-303`

- [ ] **Step 1: Add shared project-card and summary-button styles**

Add these resources before the project data templates in `MainWindow.xaml`:

```xml
<Style x:Key="ProjectCardStyle" TargetType="Border">
    <Setter Property="Margin" Value="0,0,0,8" />
    <Setter Property="Padding" Value="10" />
    <Setter Property="Background" Value="#EAF0F9" />
    <Setter Property="BorderBrush" Value="Transparent" />
    <Setter Property="BorderThickness" Value="2" />
    <Setter Property="CornerRadius" Value="18" />
    <Style.Triggers>
        <DataTrigger Binding="{Binding IsExpanded}" Value="True">
            <Setter Property="Background" Value="#FFFFFF" />
            <Setter Property="BorderBrush" Value="#9DB9FF" />
        </DataTrigger>
    </Style.Triggers>
</Style>

<Style x:Key="ProjectSummaryButtonStyle" TargetType="Button">
    <Setter Property="Padding" Value="0" />
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="BorderBrush" Value="Transparent" />
    <Setter Property="BorderThickness" Value="0" />
    <Setter Property="HorizontalContentAlignment" Value="Stretch" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border Background="{TemplateBinding Background}" CornerRadius="14">
                    <ContentPresenter HorizontalAlignment="Stretch" />
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

- [ ] **Step 2: Add one shared project-summary template**

Add this resource after the styles:

```xml
<DataTemplate x:Key="ProjectSummaryTemplate" DataType="{x:Type vm:ProjectSectionViewModel}">
    <Button Style="{StaticResource ProjectSummaryButtonStyle}"
            Click="ProjectSummaryButton_OnClick">
        <StackPanel>
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto" />
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>

                <Border Width="22" Height="22" CornerRadius="11"
                        VerticalAlignment="Center"
                        Background="#D9E5FF">
                    <TextBlock Text="{Binding ExpansionGlyph}"
                               HorizontalAlignment="Center"
                               VerticalAlignment="Center"
                               FontSize="11"
                               FontWeight="Bold"
                               Foreground="#2458D4" />
                </Border>

                <StackPanel Grid.Column="1" Margin="9,0,8,0">
                    <TextBlock Text="{Binding ProjectName}"
                               FontSize="12.5"
                               FontWeight="SemiBold"
                               Foreground="#14213D"
                               TextTrimming="CharacterEllipsis" />
                    <TextBlock Margin="0,3,0,0"
                               Text="{Binding ProjectMeta}"
                               FontSize="9.5"
                               Foreground="#697B9B"
                               TextTrimming="CharacterEllipsis" />
                </StackPanel>

                <Border Grid.Column="2" Padding="7,4" CornerRadius="10"
                        VerticalAlignment="Center"
                        Background="#DCE7FF">
                    <TextBlock Text="{Binding StatusText}"
                               FontSize="9"
                               FontWeight="SemiBold"
                               Foreground="#2458D4" />
                </Border>
            </Grid>

            <ProgressBar Margin="31,9,0,0"
                         Height="5"
                         Value="{Binding Progress, Mode=OneWay}"
                         Foreground="#2D68FF"
                         Background="#D4DEED" />
        </StackPanel>
    </Button>
</DataTemplate>
```

- [ ] **Step 3: Make weekly and long-term bodies conditional**

For both `WeeklyProjectSectionTemplate` and `LongTermProjectSectionTemplate`:

1. Replace the outer border attributes with `Style="{StaticResource ProjectCardStyle}"`.
2. Replace the duplicated project header grid with:

```xml
<ContentControl Content="{Binding}"
                ContentTemplate="{StaticResource ProjectSummaryTemplate}" />
```

3. Insert this opening tag immediately before the existing `ItemsControl`:

```xml
<StackPanel Margin="0,10,0,0"
            Visibility="{Binding IsExpanded, Converter={StaticResource BooleanToVisibilityConverter}}">
```

4. Insert this closing tag immediately after the existing “添加任务” button:

```xml
</StackPanel>
```

5. Remove the original `Margin="0,10,0,0"` from each nested `ItemsControl` so the body spacing is owned by the wrapper.

This ensures a collapsed project has no task-card or add-button layout footprint.

- [ ] **Step 4: Route summary clicks without intercepting task controls**

Add this handler near the other project/task handlers in `MainWindow.xaml.cs`:

```csharp
private void ProjectSummaryButton_OnClick(object sender, RoutedEventArgs e)
{
    if (sender is FrameworkElement
        {
            DataContext: ProjectSectionViewModel { IsCollapsible: true } section
        })
    {
        _viewModel.ToggleProjectExpansion(section.ProjectId, section.SectionKind);
    }

    e.Handled = true;
}
```

Because only the summary is a button, task edit/archive/status/annotation controls remain outside it and cannot accidentally toggle the project.

- [ ] **Step 5: Build and inspect XAML compilation**

Run:

```powershell
dotnet build Horizon.sln
```

Expected: build succeeds with `0` errors. If XAML reports nested-content errors, confirm each project card has one root `StackPanel` and the new conditional body is inside it.

- [ ] **Step 6: Commit the interaction slice**

```powershell
git add src/Horizon.App/MainWindow.xaml src/Horizon.App/MainWindow.xaml.cs
git commit -m "feat: add rounded project collapse interaction"
```

### Task 4: Apply the Orbit Blue visual system across the full panel

**Files:**
- Modify: `src/Horizon.App/MainWindow.xaml:27-765`

- [ ] **Step 1: Centralize the approved palette**

Add these resources at the start of `Window.Resources`:

```xml
<SolidColorBrush x:Key="OrbitPanelBrush" Color="#F7F9FD" />
<SolidColorBrush x:Key="OrbitHeaderBrush" Color="#DCE9FF" />
<SolidColorBrush x:Key="OrbitPrimaryBrush" Color="#2D68FF" />
<SolidColorBrush x:Key="OrbitInkBrush" Color="#14213D" />
<SolidColorBrush x:Key="OrbitMutedBrush" Color="#697B9B" />
<SolidColorBrush x:Key="OrbitCollapsedCardBrush" Color="#EAF0F9" />
<SolidColorBrush x:Key="OrbitExpandedCardBrush" Color="#FFFFFF" />
<SolidColorBrush x:Key="OrbitExpandedBorderBrush" Color="#9DB9FF" />
<SolidColorBrush x:Key="OrbitTaskBrush" Color="#EEF3FF" />
<SolidColorBrush x:Key="OrbitTrackBrush" Color="#D4DEED" />
<SolidColorBrush x:Key="OrbitSignalBrush" Color="#D6FF58" />
```

Replace the literal colors in Task 3’s resources with these `StaticResource` brushes so the palette has one source of truth.

- [ ] **Step 2: Restyle shared controls with rounded flat surfaces**

In the existing base `Button` style, replace the foreground/background/border setters with the following values, keep the current interaction triggers, and change the template border from `CornerRadius="14"` to `CornerRadius="12"`:

```xml
<Style TargetType="Button">
    <Setter Property="Foreground" Value="{StaticResource OrbitInkBrush}" />
    <Setter Property="Background" Value="#F3F6FC" />
    <Setter Property="BorderBrush" Value="#D6E0F0" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="Padding" Value="10,6" />
    <Setter Property="Cursor" Value="Hand" />
    <Setter Property="FontSize" Value="11.5" />
</Style>

<Style x:Key="PrimaryActionButtonStyle"
       TargetType="Button"
       BasedOn="{StaticResource {x:Type Button}}">
    <Setter Property="Background" Value="{StaticResource OrbitPrimaryBrush}" />
    <Setter Property="BorderBrush" Value="{StaticResource OrbitPrimaryBrush}" />
    <Setter Property="Foreground" Value="White" />
    <Setter Property="Padding" Value="12,7" />
    <Setter Property="FontWeight" Value="SemiBold" />
</Style>
```

Apply these exact property values to the remaining shared styles while preserving their existing sizes and triggers:

| Style/control | Background | Border | Foreground | Corner radius |
|---|---|---|---|---|
| `ActionButtonStyle` | `#F3F6FC` | `#D6E0F0` | `OrbitInkBrush` | inherited `12` |
| `PinToggleButtonStyle` unchecked | `#F3F6FC` | `#D6E0F0` | `OrbitMutedBrush` | `12` |
| `PinToggleButtonStyle` checked | `#DCE7FF` | `#9DB9FF` | `#2458D4` | `12` |
| `MiniButtonStyle` | inherit action style | inherit action style | inherit action style | inherited `12` |
| `LinkButtonStyle` | `Transparent` | `Transparent` | `OrbitPrimaryBrush` | inherited `12` |
| `SegmentRadioButtonStyle` unchecked | `Transparent` | none | `OrbitMutedBrush` | `10` |
| `SegmentRadioButtonStyle` checked | `White` | none | `#2458D4` | `10` |
| `ComboBox` | `White` | `#D6E0F0` | `OrbitInkBrush` | native template unchanged |
| `TextBox` | `White` | `#D6E0F0` | `OrbitInkBrush` | `12` |
| `ProgressBar` | `OrbitTrackBrush` | none | `OrbitPrimaryBrush` | existing progress template |
| `PanelCardStyle` | `White` | `#D9E4F5` | inherited | `18` |

- [ ] **Step 3: Add the flat Orbit Blue header background and orbit motif**

Replace the opening attributes of `ExpandedPanelShell` with:

```xml
<Border x:Name="ExpandedPanelShell"
        Margin="6"
        Background="{StaticResource OrbitPanelBrush}"
        BorderBrush="#B8C8E1"
        BorderThickness="1"
        CornerRadius="30"
        ClipToBounds="True">
```

Inside the existing four-row content grid, insert these three elements immediately after `Grid.RowDefinitions` and before the current header row. Negative margins extend the decoration into the shell padding while `ClipToBounds` preserves the outer round shape:

```xml
<Border Grid.RowSpan="4"
        Height="112"
        Margin="-18,-22,-16,0"
        VerticalAlignment="Top"
        Background="{StaticResource OrbitHeaderBrush}"
        CornerRadius="29,29,0,0"
        IsHitTestVisible="False" />
<Ellipse Grid.RowSpan="4"
         Width="118" Height="118"
         Margin="0,-68,-52,0"
         HorizontalAlignment="Right"
         VerticalAlignment="Top"
         Stroke="#B8CEFF"
         StrokeThickness="22"
         Opacity="0.75"
         IsHitTestVisible="False" />
<Ellipse Grid.RowSpan="4"
         Width="8" Height="8"
         Margin="0,-5,10,0"
         HorizontalAlignment="Right"
         VerticalAlignment="Top"
         Fill="{StaticResource OrbitSignalBrush}"
         IsHitTestVisible="False" />
```

Set the Horizon title to `OrbitInkBrush`, its subtitle to `OrbitMutedBrush`, and the pin style to the values in Step 2. Keep all content-grid margins and bindings unchanged. All three decoration elements remain non-interactive.

- [ ] **Step 4: Unify weekly and long-term task cards**

Replace the old green-specific long-term palette and the old gray-blue weekly palette with the same hierarchy:

```xml
<Border Margin="0,0,0,8"
        Padding="10"
        Background="{StaticResource OrbitTaskBrush}"
        BorderBrush="#D9E4F5"
        BorderThickness="1"
        CornerRadius="14">
```

Use `OrbitInkBrush` for task titles, `OrbitMutedBrush` for metadata, `OrbitPrimaryBrush` for progress and links, `OrbitTrackBrush` for progress tracks and segmented-control wells, and white for notes previews. Keep status semantics and three-state labels unchanged.

Run this audit after replacements:

```powershell
rg -n "#(0A2E22|0D7D5A|10A06D|2F6C55|5A7D6F|F4FAF8|D3EBE3|B7D9CF)" src\Horizon.App\MainWindow.xaml
```

Expected: no matches. This prevents the previous green long-term theme from surviving inside the unified Orbit Blue design.

- [ ] **Step 5: Align section headings, empty states, status message, and editor surface**

Use the shared resources for all visible full-panel text and containers:

- Section headings and editor headings: `OrbitInkBrush`.
- Explanatory text and empty-state copy: `OrbitMutedBrush`.
- Status message background: `#EDF2FB`, border `#D6E0F0`, text `#466086`, corner radius `14`.
- Editor overlay card: white or `OrbitPanelBrush`, border `OrbitExpandedBorderBrush`, outer corner radius `22`.
- Input corner radius: `12`; action/status pills remain fully rounded.

Then list all remaining literal colors for deliberate review:

```powershell
rg -o "#[0-9A-Fa-f]{6}" src\Horizon.App\MainWindow.xaml | Sort-Object -Unique
```

Expected: remaining literals are semantic exception colors (for example destructive confirmation red) or colors already represented by the approved palette. Replace accidental near-duplicate blues with resources.

- [ ] **Step 6: Build and commit the visual slice**

Run:

```powershell
dotnet build Horizon.sln
```

Expected: build succeeds with `0` errors.

Commit:

```powershell
git add src/Horizon.App/MainWindow.xaml
git commit -m "style: apply rounded Orbit Blue interface"
```

### Task 5: Regression, publish, and manual design verification

**Files:**
- Verify: `tests/Horizon.App.Tests/Program.cs`
- Verify: `src/Horizon.App/MainWindow.xaml`
- Verify: `src/Horizon.App/MainWindow.xaml.cs`
- Output: `artifacts/Horizon.App/`

- [ ] **Step 1: Run the automated behavior suite**

```powershell
dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj
```

Expected: exit code `0` and `Panel layout tests passed.`

- [ ] **Step 2: Run the full Debug build**

```powershell
dotnet build Horizon.sln
```

Expected: build succeeds with `0` warnings and `0` errors. If a stale Horizon process locks output files, close that process and rerun; do not delete unrelated worktree files.

- [ ] **Step 3: Publish the runnable build**

```powershell
dotnet publish src\Horizon.App\Horizon.App.csproj -c Release -o artifacts\Horizon.App
```

Expected: `artifacts\Horizon.App\Horizon.App.exe` exists.

- [ ] **Step 4: Manually verify the interaction contract**

Launch:

```powershell
& '.\artifacts\Horizon.App\Horizon.App.exe'
```

Verify all of the following:

1. New/current weekly and long-term projects start collapsed when they have no saved expansion state.
2. Collapsed cards show project name, state, task count, completed count, percent, and progress bar; no task content reserves height.
3. Weekly and long-term states for the same project change independently.
4. Expansion state survives closing and relaunching the application.
5. Edit, archive, status, annotation, and add-task controls do not collapse their parent project.
6. History detail remains expanded and its summary does not toggle.
7. At 360 px width, glyph, project name, summary, and state pill do not overlap or clip.
8. Header decoration never blocks buttons; the panel uses flat solid colors, rounded hierarchy, and no heavy shadow or gradient.
9. Chinese copy remains complete and readable; only the `Horizon` product name is non-Chinese.

- [ ] **Step 5: Review only the feature diff**

```powershell
git diff HEAD~4 -- src/Horizon.App/ProjectExpansionRules.cs src/Horizon.App/Models/HorizonModels.cs src/Horizon.App/Services/HorizonDataStore.cs src/Horizon.App/ViewModels/ViewModelTypes.cs src/Horizon.App/ViewModels/MainViewModel.cs src/Horizon.App/MainWindow.xaml src/Horizon.App/MainWindow.xaml.cs tests/Horizon.App.Tests/Program.cs
```

Expected: only expansion behavior, summary projection, Orbit Blue styling, and tests are present. Existing unrelated user changes remain untouched.

- [ ] **Step 6: Commit any final verification-only correction**

Only if manual verification required a correction:

```powershell
git add src/Horizon.App tests/Horizon.App.Tests/Program.cs
git commit -m "fix: polish project collapse verification issues"
```

If no correction was needed, do not create an empty commit.

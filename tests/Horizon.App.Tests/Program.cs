using System.Windows;
using System.Text.Json;
using System.IO;
using Horizon.App;
using Horizon.App.Models;
using Horizon.App.Services;

var area = new Rect(100, 50, 1200, 800);

var legacySettings = JsonSerializer.Deserialize<HorizonSettings>("{}", JsonOptions.Default);
AssertEqual(true, legacySettings?.StartWithWindows ?? false, "legacy settings enable autostart by default");
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
AssertEqual(true,
    ProjectExpansionRules.IsExpanded(expansionSettings, ProjectSectionKind.Weekly, "project-a"),
    "pruning preserves known project state");

var toggleSettings = new HorizonSettings();
AssertEqual(true,
    ProjectExpansionRules.Toggle(toggleSettings, ProjectSectionKind.Weekly, "project-toggle"),
    "toggle expands a collapsed project");
AssertEqual(false,
    ProjectExpansionRules.Toggle(toggleSettings, ProjectSectionKind.Weekly, "project-toggle"),
    "toggle collapses an expanded project");

var removalSettings = new HorizonSettings();
ProjectExpansionRules.SetExpanded(removalSettings, ProjectSectionKind.Weekly, "project-remove", true);
ProjectExpansionRules.SetExpanded(removalSettings, ProjectSectionKind.LongTerm, "project-remove", true);
ProjectExpansionRules.SetExpanded(removalSettings, ProjectSectionKind.Weekly, "project-keep", true);
AssertEqual(true,
    ProjectExpansionRules.RemoveProject(removalSettings, "project-remove"),
    "removing a project clears its expansion states");
AssertEqual(false,
    ProjectExpansionRules.IsExpanded(removalSettings, ProjectSectionKind.Weekly, "project-remove"),
    "removed weekly state is no longer visible");
AssertEqual(false,
    ProjectExpansionRules.IsExpanded(removalSettings, ProjectSectionKind.LongTerm, "project-remove"),
    "removed long-term state is no longer visible");
AssertEqual(true,
    ProjectExpansionRules.IsExpanded(removalSettings, ProjectSectionKind.Weekly, "project-keep"),
    "removing a project preserves other projects");
AssertEqual(false,
    ProjectExpansionRules.RemoveProject(removalSettings, "project-remove"),
    "removing missing project state reports no change");

var explicitlyNullExpansionSettings = JsonSerializer.Deserialize<HorizonSettings>(
    "{\"ProjectExpansionStates\":null}",
    JsonOptions.Default)!;
AssertEqual(false,
    ProjectExpansionRules.IsExpanded(
        explicitlyNullExpansionSettings,
        ProjectSectionKind.Weekly,
        "project-null"),
    "rules treat an explicit null state dictionary as empty");
ProjectExpansionRules.SetExpanded(
    explicitlyNullExpansionSettings,
    ProjectSectionKind.Weekly,
    "project-null",
    true);
AssertEqual(true,
    ProjectExpansionRules.IsExpanded(
        explicitlyNullExpansionSettings,
        ProjectSectionKind.Weekly,
        "project-null"),
    "rules initialize an explicit null state dictionary");

AssertThrows<ArgumentOutOfRangeException>(
    () => ProjectExpansionRules.IsExpanded(
        new HorizonSettings(),
        (ProjectSectionKind)999,
        "project-a"),
    "unknown project section kind is rejected");

var expansionStoreRoot = Path.Combine(
    Path.GetTempPath(),
    $"Horizon.Expansion.Persistence.Tests.{Guid.NewGuid():N}");
try
{
    var expansionStore = new HorizonDataStore(expansionStoreRoot);
    Directory.CreateDirectory(Path.GetDirectoryName(expansionStore.DataFilePath)!);

    File.WriteAllText(expansionStore.DataFilePath, "{\"Settings\":{}}");
    var missingFieldData = expansionStore.Load();
    AssertEqual(0,
        missingFieldData.Settings.ProjectExpansionStates.Count,
        "data store loads settings with a missing expansion field");

    File.WriteAllText(
        expansionStore.DataFilePath,
        "{\"Settings\":{\"ProjectExpansionStates\":null}}");
    var explicitNullData = expansionStore.Load();
    AssertEqual(0,
        explicitNullData.Settings.ProjectExpansionStates.Count,
        "data store normalizes an explicit null expansion field");

    ProjectExpansionRules.SetExpanded(
        explicitNullData.Settings,
        ProjectSectionKind.Weekly,
        "project-persisted",
        true);
    expansionStore.Save(explicitNullData);

    var reloadedExpansionData = new HorizonDataStore(expansionStoreRoot).Load();
    AssertEqual(true,
        ProjectExpansionRules.IsExpanded(
            reloadedExpansionData.Settings,
            ProjectSectionKind.Weekly,
            "project-persisted"),
        "weekly expansion survives save and reload");
    AssertEqual(false,
        ProjectExpansionRules.IsExpanded(
            reloadedExpansionData.Settings,
            ProjectSectionKind.LongTerm,
            "project-persisted"),
        "save and reload preserves independent section state");
}
finally
{
    if (Directory.Exists(expansionStoreRoot))
    {
        Directory.Delete(expansionStoreRoot, recursive: true);
    }
}
AssertEqual(
    "\"C:\\Program Files\\Horizon\\Horizon.App.exe\"",
    WindowsStartupService.BuildCommand(@"C:\Program Files\Horizon\Horizon.App.exe"),
    "startup command quotes executable path");

var viewModelTestRoot = Path.Combine(Path.GetTempPath(), $"Horizon.Tests.{Guid.NewGuid():N}");
try
{
    var fakeStartupService = new FakeStartupRegistrationService();
    var viewModelStore = new HorizonDataStore(viewModelTestRoot);
    var viewModel = new Horizon.App.ViewModels.MainViewModel(viewModelStore, fakeStartupService);

    AssertEqual(true, viewModel.StartWithWindows, "autostart is enabled for new data");
    AssertEqual(true, viewModel.ReconcileStartupRegistration(), "startup registration reconciles");
    AssertEqual(true, fakeStartupService.LastRequestedValue ?? false, "reconcile uses saved preference");
    AssertEqual(true, viewModel.ToggleStartWithWindows(), "autostart can be disabled");
    AssertEqual(false, viewModel.StartWithWindows, "disabled preference is reflected");
    AssertEqual(false, new HorizonDataStore(viewModelTestRoot).Load().Settings.StartWithWindows, "disabled preference is persisted");

    fakeStartupService.ShouldSucceed = false;
    AssertEqual(false, viewModel.ToggleStartWithWindows(), "failed registry update is reported");
    AssertEqual(false, viewModel.StartWithWindows, "failed registry update preserves preference");
}
finally
{
    if (Directory.Exists(viewModelTestRoot))
    {
        Directory.Delete(viewModelTestRoot, recursive: true);
    }
}

var expansionViewModelRoot = Path.Combine(
    Path.GetTempPath(),
    $"Horizon.Expansion.Tests.{Guid.NewGuid():N}");
try
{
    var store = new HorizonDataStore(expansionViewModelRoot);
    var project = new ProjectItem { Id = "project-a", Name = "Horizon 产品迭代" };
    var emptyProject = new ProjectItem { Id = "project-empty", Name = "空项目" };
    var expansionCurrentWeekStart = WeeklyRolloverService.GetStartOfWeek(DateTime.Today);
    var historyWeekStart = expansionCurrentWeekStart.AddDays(-7);
    var historyCompletedAt = DateTime.SpecifyKind(
        historyWeekStart.AddDays(2).AddHours(12),
        DateTimeKind.Local).ToUniversalTime();
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
                WeekStartDate = expansionCurrentWeekStart
            },
            new WeeklyTaskItem
            {
                ProjectId = project.Id,
                Title = "历史周任务",
                Status = WeeklyTaskStatus.Done,
                Progress = 100,
                WeekStartDate = historyWeekStart,
                CompletedAt = historyCompletedAt
            },
            new WeeklyTaskItem
            {
                ProjectId = project.Id,
                Title = "归档周任务",
                Status = WeeklyTaskStatus.Todo,
                Progress = 10,
                WeekStartDate = expansionCurrentWeekStart,
                Archived = true
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
            },
            new LongTermTaskItem
            {
                ProjectId = project.Id,
                Title = "历史长期任务",
                Status = LongTermTaskStatus.Completed,
                Progress = 100,
                StartDate = historyWeekStart.AddMonths(-1),
                EndDate = historyWeekStart,
                CompletedAt = historyCompletedAt
            },
            new LongTermTaskItem
            {
                ProjectId = project.Id,
                Title = "归档长期任务",
                Status = LongTermTaskStatus.Planned,
                Progress = 10,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(1),
                Archived = true
            }
        ]
    };
    ProjectExpansionRules.SetExpanded(
        data.Settings,
        ProjectSectionKind.Weekly,
        "unknown-project",
        true);
    store.Save(data);

    var viewModel = new Horizon.App.ViewModels.MainViewModel(
        store,
        new FakeStartupRegistrationService());

    var afterLoad = store.Load();
    AssertEqual(0, afterLoad.Settings.ProjectExpansionStates.Count,
        "load prunes and persists unknown project expansion state");

    var weeklySection = viewModel.WeeklySections.Single(section => section.ProjectId == project.Id);
    var emptyWeeklySection = viewModel.WeeklySections.Single(section => section.ProjectId == emptyProject.Id);
    var longTermSection = viewModel.LongTermSections.Single(section => section.ProjectId == project.Id);
    var emptyLongTermSection = viewModel.LongTermSections.Single(section => section.ProjectId == emptyProject.Id);

    AssertEqual(false, weeklySection.IsExpanded, "new weekly section starts collapsed");
    AssertEqual(false, longTermSection.IsExpanded, "new long-term section starts collapsed");
    AssertEqual("1 项任务 · 已完成 0 项 · 40%", weeklySection.ProjectMeta,
        "weekly project summary contains count, completion and progress");
    AssertEqual(1, weeklySection.TaskCount, "weekly project summary exposes task count");
    AssertEqual(0, weeklySection.CompletedTaskCount, "weekly project summary exposes completed count");
    AssertEqual("1 项任务 · 已完成 0 项 · 20%", longTermSection.ProjectMeta,
        "long-term project summary contains count, completion and progress");
    AssertEqual("0 项任务 · 已完成 0 项 · 0%", emptyWeeklySection.ProjectMeta,
        "empty weekly project remains visible with a zero summary");
    AssertEqual("0 项任务 · 已完成 0 项 · 0%", emptyLongTermSection.ProjectMeta,
        "empty long-term project remains visible with a zero summary");

    viewModel.SearchText = "折叠";
    AssertEqual(project.Id, viewModel.WeeklySections.Single().ProjectId,
        "weekly task search only keeps its matching project");
    AssertEqual(0, viewModel.LongTermSections.Count,
        "weekly-only task search does not create empty long-term project cards");

    viewModel.SearchText = "发布";
    AssertEqual(0, viewModel.WeeklySections.Count,
        "long-term-only task search does not create empty weekly project cards");
    AssertEqual(project.Id, viewModel.LongTermSections.Single().ProjectId,
        "long-term task search only keeps its matching project");

    viewModel.SearchText = emptyProject.Name;
    AssertEqual(emptyProject.Id, viewModel.WeeklySections.Single().ProjectId,
        "project-name search keeps a matching weekly project with no tasks");
    AssertEqual(0, viewModel.WeeklySections.Single().TaskCount,
        "project-name search keeps a zero-task weekly summary");
    AssertEqual(emptyProject.Id, viewModel.LongTermSections.Single().ProjectId,
        "project-name search keeps a matching long-term project with no tasks");
    AssertEqual(0, viewModel.LongTermSections.Single().TaskCount,
        "project-name search keeps a zero-task long-term summary");

    viewModel.SearchText = "无匹配内容";
    AssertEqual(0, viewModel.WeeklySections.Count, "unmatched search hides all weekly projects");
    AssertEqual(0, viewModel.LongTermSections.Count, "unmatched search hides all long-term projects");
    AssertEqual(true, viewModel.ShowWeeklyEmptyState, "unmatched weekly search shows empty state");
    AssertEqual(true, viewModel.ShowLongTermEmptyState, "unmatched long-term search shows empty state");

    viewModel.SearchText = string.Empty;
    AssertEqual(2, viewModel.WeeklySections.Count, "clearing search restores all weekly projects");
    AssertEqual(2, viewModel.LongTermSections.Count, "clearing search restores all long-term projects");

    viewModel.ToggleArchivedTasks();
    AssertEqual(project.Id, viewModel.WeeklySections.Single().ProjectId,
        "archive view only shows projects with archived weekly tasks");
    AssertEqual(project.Id, viewModel.LongTermSections.Single().ProjectId,
        "archive view only shows projects with archived long-term tasks");
    AssertEqual(false,
        viewModel.WeeklySections.Any(section => section.ProjectId == emptyProject.Id),
        "archive view does not generate empty weekly project cards");
    AssertEqual(false,
        viewModel.LongTermSections.Any(section => section.ProjectId == emptyProject.Id),
        "archive view does not generate empty long-term project cards");
    viewModel.ToggleArchivedTasks();

    AssertEqual(true,
        viewModel.ToggleProjectExpansion(project.Id, ProjectSectionKind.Weekly),
        "weekly project expands");
    AssertEqual(true,
        viewModel.WeeklySections.Single(section => section.ProjectId == project.Id).IsExpanded,
        "expanded state reaches projected section");
    AssertEqual("⌄",
        viewModel.WeeklySections.Single(section => section.ProjectId == project.Id).ExpansionGlyph,
        "expanded section exposes a downward glyph");
    AssertEqual(false,
        viewModel.LongTermSections.Single(section => section.ProjectId == project.Id).IsExpanded,
        "weekly toggle does not affect long-term section");
    AssertEqual(false,
        viewModel.ToggleProjectExpansion("missing-project", ProjectSectionKind.Weekly),
        "missing projects cannot create expansion state");

    var reloaded = new Horizon.App.ViewModels.MainViewModel(
        new HorizonDataStore(expansionViewModelRoot),
        new FakeStartupRegistrationService());
    AssertEqual(true,
        reloaded.WeeklySections.Single(section => section.ProjectId == project.Id).IsExpanded,
        "weekly expansion survives reload");
    AssertEqual(false,
        reloaded.LongTermSections.Single(section => section.ProjectId == project.Id).IsExpanded,
        "long-term section remains independently collapsed after reload");

    reloaded.OpenHistoryWeek(historyWeekStart);
    var weeklyHistorySection = reloaded.HistoryDetailSections.Single();
    AssertEqual(true, weeklyHistorySection.IsExpanded, "weekly history detail stays expanded");
    AssertEqual(false, weeklyHistorySection.IsCollapsible, "weekly history detail cannot be collapsed");

    reloaded.SelectLongTermHistory();
    reloaded.OpenHistoryWeek(historyWeekStart);
    var longTermHistorySection = reloaded.HistoryDetailSections.Single();
    AssertEqual(true, longTermHistorySection.IsExpanded, "long-term history detail stays expanded");
    AssertEqual(false, longTermHistorySection.IsCollapsible, "long-term history detail cannot be collapsed");

    AssertEqual(true,
        reloaded.ToggleProjectExpansion(project.Id, ProjectSectionKind.LongTerm),
        "long-term project expands independently");
    reloaded.DeleteProjectInSettings(project.Id);
    var afterDelete = new HorizonDataStore(expansionViewModelRoot).Load();
    AssertEqual(false,
        ProjectExpansionRules.IsExpanded(afterDelete.Settings, ProjectSectionKind.Weekly, project.Id),
        "deleting a project clears its weekly expansion state");
    AssertEqual(false,
        ProjectExpansionRules.IsExpanded(afterDelete.Settings, ProjectSectionKind.LongTerm, project.Id),
        "deleting a project clears its long-term expansion state");
}
finally
{
    if (Directory.Exists(expansionViewModelRoot))
    {
        Directory.Delete(expansionViewModelRoot, recursive: true);
    }
}

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
        isApplicationMenuOpen: false,
        isPinned: false),
    "inactive expanded panel collapses");
AssertEqual(
    false,
    PanelInteractionRules.ShouldCollapseAfterDeactivation(
        PanelDisplayState.ExpandedPanel,
        isWindowActive: true,
        isApplicationMenuOpen: false,
        isPinned: false),
    "reactivated window stays open");
AssertEqual(
    false,
    PanelInteractionRules.ShouldCollapseAfterDeactivation(
        PanelDisplayState.ExpandedPanel,
        isWindowActive: false,
        isApplicationMenuOpen: true,
        isPinned: false),
    "application menu keeps panel open");
AssertEqual(
    false,
    PanelInteractionRules.ShouldCollapseAfterDeactivation(
        PanelDisplayState.CollapsedSliver,
        isWindowActive: false,
        isApplicationMenuOpen: false,
        isPinned: false),
    "collapsed state does not transition again");
AssertEqual(
    false,
    PanelInteractionRules.ShouldCollapseAfterDeactivation(
        PanelDisplayState.ExpandedPanel,
        isWindowActive: false,
        isApplicationMenuOpen: false,
        isPinned: true),
    "pinned expanded panel stays open");
AssertEqual(false, TaskAnnotationRules.IsValid("   "), "blank annotation rejected");
AssertEqual(true, TaskAnnotationRules.IsValid(new string('a', 500)), "500 characters accepted");
AssertEqual(false, TaskAnnotationRules.IsValid(new string('a', 501)), "501 characters rejected");
AssertEqual("今天 14:20", TaskAnnotationRules.FormatLocalTime(
    new DateTime(2026, 7, 2, 14, 20, 0), new DateTime(2026, 7, 2, 18, 0, 0)), "today label");
AssertEqual("昨天 09:05", TaskAnnotationRules.FormatLocalTime(
    new DateTime(2026, 7, 1, 9, 5, 0), new DateTime(2026, 7, 2, 18, 0, 0)), "yesterday label");
var annotationList = new List<TaskAnnotation>();
var addedAnnotation = TaskAnnotationRules.Add(annotationList, "  初始批注  ", new DateTime(2026, 7, 2, 1, 0, 0, DateTimeKind.Utc));
AssertEqual("初始批注", addedAnnotation.Content, "annotation content cleaned");
AssertEqual(true, TaskAnnotationRules.Update(annotationList, addedAnnotation.Id, "修改后", new DateTime(2026, 7, 2, 2, 0, 0, DateTimeKind.Utc)), "annotation updated");
AssertEqual("修改后", addedAnnotation.Content, "updated content stored");
AssertEqual(false, TaskAnnotationRules.Update(annotationList, "missing", "x", DateTime.UtcNow), "unknown annotation not updated");
AssertEqual(true, TaskAnnotationRules.Delete(annotationList, addedAnnotation.Id), "annotation deleted");
AssertEqual(false, TaskAnnotationRules.Delete(annotationList, addedAnnotation.Id), "annotation cannot be deleted twice");
var rolloverData = new HorizonDataFile();
var oldWeekStart = new DateTime(2026, 6, 29);
var currentWeekStart = new DateTime(2026, 7, 6);
var rolloverNowUtc = new DateTime(2026, 7, 6, 0, 5, 0, DateTimeKind.Utc);
var unfinishedWeekly = new WeeklyTaskItem
{
    WeekStartDate = oldWeekStart,
    Status = WeeklyTaskStatus.InProgress,
    UpdatedAt = rolloverNowUtc.AddDays(-2)
};
var completedWeekly = new WeeklyTaskItem
{
    WeekStartDate = oldWeekStart,
    Status = WeeklyTaskStatus.Done,
    UpdatedAt = rolloverNowUtc.AddDays(-2)
};
var completedLongTerm = new LongTermTaskItem
{
    Status = LongTermTaskStatus.Completed,
    UpdatedAt = rolloverNowUtc.AddDays(-2)
};
rolloverData.WeeklyTasks.Add(unfinishedWeekly);
rolloverData.WeeklyTasks.Add(completedWeekly);
rolloverData.LongTermTasks.Add(completedLongTerm);
AssertEqual(true, WeeklyRolloverService.Reconcile(rolloverData, currentWeekStart, rolloverNowUtc), "rollover changes data");
AssertEqual(currentWeekStart, unfinishedWeekly.WeekStartDate, "unfinished weekly task carried forward");
AssertEqual(oldWeekStart, completedWeekly.WeekStartDate, "completed weekly task keeps original week");
AssertEqual(false, WeeklyRolloverService.IsWeeklyInCurrentView(completedWeekly, currentWeekStart), "old completed weekly leaves current view");
AssertEqual(false, WeeklyRolloverService.IsLongTermInCurrentView(completedLongTerm, currentWeekStart), "old completed long-term leaves current view");
completedWeekly.Status = WeeklyTaskStatus.InProgress;
WeeklyRolloverService.Reconcile(rolloverData, currentWeekStart, rolloverNowUtc.AddMinutes(1));
AssertEqual(true, completedWeekly.CompletedAt is null, "restored weekly clears completion time");

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

static void AssertThrows<TException>(Action action, string name)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }
    catch (Exception exception)
    {
        throw new InvalidOperationException(
            $"{name}: expected {typeof(TException).Name}, got {exception.GetType().Name}",
            exception);
    }

    throw new InvalidOperationException(
        $"{name}: expected {typeof(TException).Name}, but no exception was thrown");
}

sealed class FakeStartupRegistrationService : IStartupRegistrationService
{
    public bool ShouldSucceed { get; set; } = true;
    public bool? LastRequestedValue { get; private set; }

    public bool TrySetEnabled(bool enabled, out string? errorMessage)
    {
        LastRequestedValue = enabled;
        errorMessage = ShouldSucceed ? null : "simulated failure";
        return ShouldSucceed;
    }
}

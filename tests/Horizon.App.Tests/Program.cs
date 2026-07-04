using System.Windows;
using System.Text.Json;
using System.IO;
using Horizon.App;
using Horizon.App.Models;
using Horizon.App.Services;

var area = new Rect(100, 50, 1200, 800);

var legacySettings = JsonSerializer.Deserialize<HorizonSettings>("{}", JsonOptions.Default);
AssertEqual(true, legacySettings?.StartWithWindows ?? false, "legacy settings enable autostart by default");
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

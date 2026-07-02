# Horizon Weekly Rollover and History Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Carry unfinished weekly tasks forward, move completed tasks into separate weekly/long-term history on Monday, and browse history through week summaries and details.

**Architecture:** Add UTC completion timestamps and a pure rollover service with an injected local date. Replace the old history boolean with explicit view/type/detail state and derive week summaries from current task records without snapshots.

**Tech Stack:** C# 13, .NET 9, WPF, DispatcherTimer

---

### Task 1: Add Completion Timestamps and Rollover Service

**Files:**
- Modify: `src/Horizon.App/Models/HorizonModels.cs`
- Create: `src/Horizon.App/Services/WeeklyRolloverService.cs`
- Modify: `tests/Horizon.App.Tests/Program.cs`

- [ ] Add failing tests covering Sunday-to-Monday, multi-week downtime, completed weekly retention, long-term eligibility, and status restoration.

```csharp
var currentWeek = new DateTime(2026, 7, 6);
var changed = WeeklyRolloverService.Reconcile(data, currentWeek, nowUtc);
AssertEqual(currentWeek, unfinished.WeekStartDate, "unfinished weekly carried forward");
AssertEqual(oldWeek, completed.WeekStartDate, "completed weekly keeps original week");
```

- [ ] Add `DateTime? CompletedAt` to both task models.

- [ ] Implement `Reconcile(HorizonDataFile data, DateTime localToday, DateTime nowUtc)` to migrate missing completion times, clear inconsistent completion times, and carry unfinished weekly tasks to the current week. Return whether data changed.

- [ ] Add helpers `IsWeeklyInCurrentView`, `IsLongTermInCurrentView`, and `GetLongTermHistoryWeek` using local conversion of `CompletedAt`.

- [ ] Run tests and build.

### Task 2: Track Completion and Periodic Reconciliation

**Files:**
- Modify: `src/Horizon.App/ViewModels/MainViewModel.cs`
- Modify: `src/Horizon.App/MainWindow.xaml.cs`

- [ ] Update status methods and save methods: set `CompletedAt = DateTime.UtcNow` on first completion, clear it on restoration.

- [ ] In `Load`, run reconciliation before refresh and save only when changed.

- [ ] Add `MainViewModel.ReconcileForDate(DateTime localToday, DateTime nowUtc)` for the window timer.

- [ ] Add a one-minute `DispatcherTimer` in `MainWindow`; when the local date changes, call reconciliation. Stop the timer on window close.

- [ ] Run tests and build.

### Task 3: Replace History State and Build Week Summaries

**Files:**
- Modify: `src/Horizon.App/ViewModels/ViewModelTypes.cs`
- Modify: `src/Horizon.App/ViewModels/MainViewModel.cs`

- [ ] Add enums `MainContentMode { Current, History }` and `HistoryTaskKind { Weekly, LongTerm }`.

- [ ] Add `HistoryWeekSummaryViewModel` with week start/end, title, task count, project count, progress, and selection key; add detail sections.

- [ ] Replace `ShowWeeklyHistory` with explicit properties for content mode, history kind, selected week, list visibility, detail visibility, and type-specific empty states.

- [ ] Build weekly summaries from completed weekly tasks eligible before current Monday, grouped by `WeekStartDate`; build long-term summaries from eligible completed tasks grouped by local completion week.

- [ ] Add methods `OpenHistory`, `CloseHistory`, `SelectWeeklyHistory`, `SelectLongTermHistory`, `OpenHistoryWeek`, and `BackToHistoryWeeks`.

- [ ] Filter current sections with rollover eligibility so same-week completed tasks remain visible and older completed tasks leave the main page.

- [ ] Run tests and build.

### Task 4: Build Week Cards and Detail UI

**Files:**
- Modify: `src/Horizon.App/MainWindow.xaml`
- Modify: `src/Horizon.App/MainWindow.xaml.cs`

- [ ] Replace the old directly expanded `WeeklyHistoryGroups` block with a history landing view containing weekly/long-term segmented tabs and week cards.

- [ ] Each week card displays date range, task count, project count, and a compact progress bar; clicking opens detail.

- [ ] Add detail header with back button and selected-week summary; reuse weekly or long-term project section templates for details.

- [ ] Update history button handler and add type/week/back handlers with stable tags.

- [ ] Add distinct empty states for weekly and long-term history.

- [ ] Run tests, build, and verify list/detail/back state and annotation access.

### Task 5: Publish and End-to-End Verification

- [ ] Publish Release and launch.
- [ ] Verify missed-Monday catch-up, unfinished carry-forward, same-week completed visibility, next-Monday history movement, separate history tabs, week details, back navigation, and persistence.
- [ ] Run `git status --short` and leave broader dirty code files unstaged.

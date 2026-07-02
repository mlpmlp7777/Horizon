# Horizon Task Annotations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add compact, editable annotations to weekly and long-term tasks across current and historical views.

**Architecture:** Embed annotation lists in each task for JSON compatibility. Put validation and timestamp formatting in a pure helper, expose latest-preview fields through row ViewModels, and reuse the existing editor overlay for annotation CRUD.

**Tech Stack:** C# 13, .NET 9, WPF, System.Text.Json

---

### Task 1: Add Annotation Model and Pure Rules

**Files:**
- Modify: `src/Horizon.App/Models/HorizonModels.cs`
- Create: `src/Horizon.App/TaskAnnotationRules.cs`
- Modify: `tests/Horizon.App.Tests/Program.cs`

- [ ] Add failing tests for whitespace rejection, 500-character acceptance, 501-character rejection, and local timestamp labels.

```csharp
AssertEqual(false, TaskAnnotationRules.IsValid("   "), "blank annotation rejected");
AssertEqual(true, TaskAnnotationRules.IsValid(new string('a', 500)), "500 accepted");
AssertEqual(false, TaskAnnotationRules.IsValid(new string('a', 501)), "501 rejected");
```

- [ ] Add `TaskAnnotation` and list properties.

```csharp
public sealed class TaskAnnotation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

Add `public List<TaskAnnotation> Annotations { get; set; } = [];` to both task types. In `HorizonDataStore.Load`, normalize null annotation lists to `[]` after deserialization.

- [ ] Implement `TaskAnnotationRules.IsValid`, `Clean`, `FormatLocalTime`, `Add`, `Update`, and `Delete`; the CRUD methods operate on `List<TaskAnnotation>` by stable ID and update UTC timestamps. Run tests and build.

### Task 2: Add Annotation View State and CRUD

**Files:**
- Modify: `src/Horizon.App/ViewModels/ViewModelTypes.cs`
- Modify: `src/Horizon.App/ViewModels/MainViewModel.cs`

- [ ] Add `TaskKind`, `AnnotationRowViewModel`, and `AnnotationEditorModel` with task ID, content, edit ID, delete-confirm ID, and observable fields.

- [ ] Extend weekly and long-term row ViewModels with:

```csharp
public string LatestAnnotationText { get; init; } = string.Empty;
public string LatestAnnotationTimeText { get; init; } = string.Empty;
public int AnnotationCount { get; init; }
public bool HasAnnotations => AnnotationCount > 0;
public string AnnotationActionText => HasAnnotations ? $"共 {AnnotationCount} 条批注 ›" : "添加批注";
```

- [ ] Add `EditorKind.Annotations`, open methods for both task kinds, and CRUD methods `SaveAnnotation`, `StartEditAnnotation`, `RequestDeleteAnnotation`, `CancelDeleteAnnotation`, `ConfirmDeleteAnnotation`.

- [ ] Resolve target task by `TaskKind` and ID; validate 1–500 chars; update UTC timestamps; persist and refresh after every change.

- [ ] In `BuildWeeklySection` and `BuildLongTermSection`, sort annotations by `CreatedAt`, map latest content/time/count, and preserve the same fields in history sections.

- [ ] Add deterministic CRUD tests against `TaskAnnotationRules.Add`, `Update`, and `Delete`, including an unknown ID returning `false`; run tests and build.

### Task 3: Add Compact Preview and Annotation Editor UI

**Files:**
- Modify: `src/Horizon.App/MainWindow.xaml`
- Modify: `src/Horizon.App/MainWindow.xaml.cs`

- [ ] Add a separated single-line annotation preview to both task templates, with ellipsis, timestamp, count, and a click target tagged by task ID and kind.

- [ ] Add handlers `OpenWeeklyAnnotationsButton_OnClick` and `OpenLongTermAnnotationsButton_OnClick`.

- [ ] Add an annotations section inside the existing editor overlay: task title, list ordered newest-first, add/edit text box, save/cancel buttons, and inline delete confirmation.

- [ ] Hide the generic editor footer for annotation mode and use annotation-specific controls.

- [ ] Run tests, build, and verify long text truncation, CRUD, history access, and restart persistence.

### Task 4: Publish Checkpoint

- [ ] Publish Release, launch, and manually verify weekly and long-term annotation flows.
- [ ] Leave broader dirty UI/model/ViewModel files unstaged.

using Horizon.App.Models;

namespace Horizon.App.ViewModels;

public enum TaskKind
{
    Weekly,
    LongTerm
}

public enum MainContentMode
{
    Current,
    History
}

public enum HistoryTaskKind
{
    Weekly,
    LongTerm
}

public sealed class AnnotationEditorModel : ObservableObject
{
    private string _taskId = string.Empty;
    private string _taskTitle = string.Empty;
    private string _content = string.Empty;
    private string? _editingAnnotationId;
    private string? _pendingDeleteAnnotationId;

    public TaskKind TaskKind { get; set; }
    public string TaskId { get => _taskId; set => SetProperty(ref _taskId, value); }
    public string TaskTitle { get => _taskTitle; set => SetProperty(ref _taskTitle, value); }
    public string Content { get => _content; set => SetProperty(ref _content, value); }
    public string? EditingAnnotationId { get => _editingAnnotationId; set => SetProperty(ref _editingAnnotationId, value); }
    public string? PendingDeleteAnnotationId { get => _pendingDeleteAnnotationId; set => SetProperty(ref _pendingDeleteAnnotationId, value); }
}

public sealed class AnnotationRowViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string TimeText { get; init; } = string.Empty;
    public bool IsEdited { get; init; }
    public bool IsDeletePending { get; init; }
    public string EditMarker => IsEdited ? "已编辑" : string.Empty;
}

public sealed class WeeklyTaskFormModel : ObservableObject
{
    private string _projectName = string.Empty;
    private string _title = string.Empty;
    private WeeklyTaskStatus _status = WeeklyTaskStatus.Todo;
    private string _notes = string.Empty;
    private int _progress;

    public string? EditingId { get; set; }

    public string ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public WeeklyTaskStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public int Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, Math.Clamp(value, 0, 100));
    }
}

public sealed class LongTermTaskFormModel : ObservableObject
{
    private string _projectName = string.Empty;
    private string _title = string.Empty;
    private LongTermTaskStatus _status = LongTermTaskStatus.Planned;
    private DateTime? _startDate = DateTime.Today;
    private DateTime? _endDate = DateTime.Today.AddDays(14);
    private string _notes = string.Empty;
    private int _progress;

    public string? EditingId { get; set; }

    public string ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public LongTermTaskStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public DateTime? StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public DateTime? EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public int Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, Math.Clamp(value, 0, 100));
    }
}

public sealed class SettingsFormModel : ObservableObject
{
    private string _projectNameInput = string.Empty;
    private string? _editingProjectId;

    public string ProjectNameInput
    {
        get => _projectNameInput;
        set => SetProperty(ref _projectNameInput, value);
    }

    public string? EditingProjectId
    {
        get => _editingProjectId;
        set => SetProperty(ref _editingProjectId, value);
    }
}

public sealed class ProjectCatalogRowViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int WeeklyTaskCount { get; init; }
    public int LongTermTaskCount { get; init; }
    public string Summary => $"本周 {WeeklyTaskCount} 项 · 长期 {LongTermTaskCount} 项";
}

public sealed class WeeklyTaskRowViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public WeeklyTaskStatus Status { get; init; }
    public string StatusText { get; init; } = string.Empty;
    public string StatusPillBackground { get; init; } = "#EAF3FD";
    public string StatusPillForeground { get; init; } = "#1B5DAE";
    public string MetaText { get; init; } = string.Empty;
    public string NotesPreview { get; init; } = string.Empty;
    public bool HasMetaText => !string.IsNullOrWhiteSpace(MetaText);
    public bool HasNotesPreview => !string.IsNullOrWhiteSpace(NotesPreview);
    public bool IsTodo => Status == WeeklyTaskStatus.Todo;
    public bool IsInProgress => Status == WeeklyTaskStatus.InProgress;
    public bool IsDone => Status == WeeklyTaskStatus.Done;
    public bool IsArchived { get; init; }
    public string ArchiveActionText => IsArchived ? "恢复" : "归档";
    public int Progress { get; init; }
    public double ProgressValue => Progress;
    public string ProgressText => $"{Progress}%";
    public string GroupName => $"wt-{Id}";
    public string LatestAnnotationText { get; init; } = string.Empty;
    public string LatestAnnotationTimeText { get; init; } = string.Empty;
    public int AnnotationCount { get; init; }
    public bool HasAnnotations => AnnotationCount > 0;
    public string AnnotationActionText => HasAnnotations ? $"共 {AnnotationCount} 条批注 ›" : "添加批注";
}

public sealed class LongTermTaskRowViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public LongTermTaskStatus Status { get; init; }
    public string StatusText { get; init; } = string.Empty;
    public string StatusPillBackground { get; init; } = "#E9F8F3";
    public string StatusPillForeground { get; init; } = "#0D7D5A";
    public string MetaText { get; init; } = string.Empty;
    public string NotesPreview { get; init; } = string.Empty;
    public bool HasMetaText => !string.IsNullOrWhiteSpace(MetaText);
    public bool HasNotesPreview => !string.IsNullOrWhiteSpace(NotesPreview);
    public bool IsPlanned => Status == LongTermTaskStatus.Planned;
    public bool IsActive => Status == LongTermTaskStatus.Active || Status == LongTermTaskStatus.Paused;
    public bool IsCompleted => Status == LongTermTaskStatus.Completed;
    public bool IsArchived { get; init; }
    public string ArchiveActionText => IsArchived ? "恢复" : "归档";
    public int Progress { get; init; }
    public double ProgressValue => Progress;
    public string ProgressText => $"{Progress}%";
    public string GroupName => $"lt-{Id}";
    public string LatestAnnotationText { get; init; } = string.Empty;
    public string LatestAnnotationTimeText { get; init; } = string.Empty;
    public int AnnotationCount { get; init; }
    public bool HasAnnotations => AnnotationCount > 0;
    public string AnnotationActionText => HasAnnotations ? $"共 {AnnotationCount} 条批注 ›" : "添加批注";
}

public sealed class ProjectSectionViewModel
{
    public string ProjectId { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string ProjectMeta { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
    public string StatusBadgeBackground { get; init; } = "#EAF3FD";
    public string StatusBadgeForeground { get; init; } = "#1B5DAE";
    public string AccentBrush { get; init; } = "#2A73C9";
    public int Progress { get; init; }
    public string ProgressText => $"{Progress}%";
    public IReadOnlyList<WeeklyTaskRowViewModel> WeeklyTasks { get; init; } = [];
    public IReadOnlyList<LongTermTaskRowViewModel> LongTermTasks { get; init; } = [];
    public bool ShowAddTaskAction { get; init; } = true;
}

public sealed class WeeklyHistoryGroupViewModel
{
    public string WeekTitle { get; init; } = string.Empty;
    public string WeekSubtitle { get; init; } = string.Empty;
    public IReadOnlyList<ProjectSectionViewModel> Sections { get; init; } = [];
}

public sealed class HistoryWeekSummaryViewModel
{
    public DateTime WeekStartDate { get; init; }
    public string WeekTitle { get; init; } = string.Empty;
    public string SummaryText { get; init; } = string.Empty;
    public int TaskCount { get; init; }
    public int ProjectCount { get; init; }
    public int Progress { get; init; }
    public double ProgressValue => Progress;
}

public static class StatusOptions
{
    public static WeeklyTaskStatus[] WeeklyTaskStatuses { get; } = Enum.GetValues<WeeklyTaskStatus>();
    public static LongTermTaskStatus[] LongTermTaskStatuses { get; } =
    [
        LongTermTaskStatus.Planned,
        LongTermTaskStatus.Active,
        LongTermTaskStatus.Completed
    ];
}

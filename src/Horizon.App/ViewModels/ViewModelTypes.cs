using Horizon.App.Models;

namespace Horizon.App.ViewModels;

public sealed class ProjectFormModel : ObservableObject
{
    private string _name = string.Empty;
    private ProjectStatus _status = ProjectStatus.Active;

    public string? EditingId { get; set; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public ProjectStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
}

public sealed class WeeklyTaskFormModel : ObservableObject
{
    private string _projectId = string.Empty;
    private string _title = string.Empty;
    private WeeklyTaskStatus _status = WeeklyTaskStatus.Todo;
    private DateTime? _weekStartDate = DateTime.Today;
    private DateTime? _dueDate;
    private string _notes = string.Empty;

    public string? EditingId { get; set; }

    public string ProjectId
    {
        get => _projectId;
        set => SetProperty(ref _projectId, value);
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

    public DateTime? WeekStartDate
    {
        get => _weekStartDate;
        set => SetProperty(ref _weekStartDate, value);
    }

    public DateTime? DueDate
    {
        get => _dueDate;
        set => SetProperty(ref _dueDate, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }
}

public sealed class LongTermTaskFormModel : ObservableObject
{
    private string _projectId = string.Empty;
    private string _title = string.Empty;
    private LongTermTaskStatus _status = LongTermTaskStatus.Planned;
    private DateTime? _startDate = DateTime.Today;
    private DateTime? _endDate = DateTime.Today.AddDays(14);
    private string _notes = string.Empty;

    public string? EditingId { get; set; }

    public string ProjectId
    {
        get => _projectId;
        set => SetProperty(ref _projectId, value);
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
}

public sealed class ProjectOption
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed class WeeklyTaskRowViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string StatusGroupName { get; init; } = string.Empty;
    public WeeklyTaskStatus Status { get; init; }
    public string StatusText { get; init; } = string.Empty;
    public string StatusPillBackground { get; init; } = "#E9F2FC";
    public string StatusPillForeground { get; init; } = "#1E63B5";
    public string MetaText { get; init; } = string.Empty;
    public string NotesPreview { get; init; } = string.Empty;
    public bool HasMetaText => !string.IsNullOrWhiteSpace(MetaText);
    public bool HasNotesPreview => !string.IsNullOrWhiteSpace(NotesPreview);
    public bool IsTodo => Status == WeeklyTaskStatus.Todo;
    public bool IsInProgress => Status == WeeklyTaskStatus.InProgress;
    public bool IsDone => Status == WeeklyTaskStatus.Done;
}

public sealed class LongTermTaskRowViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string StatusGroupName { get; init; } = string.Empty;
    public LongTermTaskStatus Status { get; init; }
    public string StatusText { get; init; } = string.Empty;
    public string StatusPillBackground { get; init; } = "#E9F2FC";
    public string StatusPillForeground { get; init; } = "#1E63B5";
    public string MetaText { get; init; } = string.Empty;
    public string NotesPreview { get; init; } = string.Empty;
    public bool HasMetaText => !string.IsNullOrWhiteSpace(MetaText);
    public bool HasNotesPreview => !string.IsNullOrWhiteSpace(NotesPreview);
    public bool IsPlanned => Status == LongTermTaskStatus.Planned;
    public bool IsActive => Status == LongTermTaskStatus.Active || Status == LongTermTaskStatus.Paused;
    public bool IsPaused => Status == LongTermTaskStatus.Paused;
    public bool IsCompleted => Status == LongTermTaskStatus.Completed;
}

public sealed class ProjectSectionViewModel
{
    public string ProjectId { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string ProjectMeta { get; init; } = string.Empty;
    public string ArchiveActionText { get; init; } = "归档";
    public string StatusText { get; init; } = string.Empty;
    public string StatusBadgeBackground { get; init; } = "#E9F2FC";
    public string StatusBadgeForeground { get; init; } = "#1E63B5";
    public string AccentBrush { get; init; } = "#1E63B5";
    public IReadOnlyList<WeeklyTaskRowViewModel> WeeklyTasks { get; init; } = [];
    public IReadOnlyList<LongTermTaskRowViewModel> LongTermTasks { get; init; } = [];
}

public sealed class WeeklyHistoryGroupViewModel
{
    public string WeekTitle { get; init; } = string.Empty;
    public string WeekSubtitle { get; init; } = string.Empty;
    public IReadOnlyList<ProjectSectionViewModel> Sections { get; init; } = [];
}

public static class StatusOptions
{
    public static ProjectStatus[] ProjectStatuses { get; } = Enum.GetValues<ProjectStatus>();
    public static WeeklyTaskStatus[] WeeklyTaskStatuses { get; } = Enum.GetValues<WeeklyTaskStatus>();
    public static LongTermTaskStatus[] LongTermTaskStatuses { get; } =
    [
        LongTermTaskStatus.Planned,
        LongTermTaskStatus.Active,
        LongTermTaskStatus.Completed
    ];
}

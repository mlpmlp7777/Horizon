using Horizon.App.Models;
using Horizon.App.Services;

namespace Horizon.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly HorizonDataStore _store;
    private HorizonDataFile _data = new();
    private string _statusMessage = string.Empty;
    private string _editorValidationMessage = string.Empty;
    private string _searchText = string.Empty;
    private bool _showArchivedProjects;
    private bool _showWeeklyHistory;
    private EditorKind _editorKind = EditorKind.None;
    private IReadOnlyList<ProjectSectionViewModel> _weeklySections = [];
    private IReadOnlyList<ProjectSectionViewModel> _longTermSections = [];
    private IReadOnlyList<WeeklyHistoryGroupViewModel> _weeklyHistoryGroups = [];
    private IReadOnlyList<ProjectOption> _activeProjectOptions = [];

    public MainViewModel(HorizonDataStore store)
    {
        _store = store;
        Load();
    }

    public ProjectFormModel ProjectForm { get; } = new();
    public WeeklyTaskFormModel WeeklyTaskForm { get; } = new();
    public LongTermTaskFormModel LongTermTaskForm { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                RefreshSections();
            }
        }
    }

    public bool ShowArchivedProjects
    {
        get => _showArchivedProjects;
        private set
        {
            if (SetProperty(ref _showArchivedProjects, value))
            {
                RefreshSections();
            }
        }
    }

    public bool ShowWeeklyHistory
    {
        get => _showWeeklyHistory;
        private set
        {
            if (SetProperty(ref _showWeeklyHistory, value))
            {
                OnPropertyChanged(nameof(IsCurrentWeeklyView));
                OnPropertyChanged(nameof(HistoryToggleText));
                OnPropertyChanged(nameof(ShowWeeklyEmptyState));
                OnPropertyChanged(nameof(ShowLongTermEmptyState));
                OnPropertyChanged(nameof(ShowWeeklyHistoryEmptyState));
                OnPropertyChanged(nameof(HeaderSubtitle));
            }
        }
    }

    public IReadOnlyList<ProjectSectionViewModel> WeeklySections
    {
        get => _weeklySections;
        private set => SetProperty(ref _weeklySections, value);
    }

    public IReadOnlyList<ProjectSectionViewModel> LongTermSections
    {
        get => _longTermSections;
        private set => SetProperty(ref _longTermSections, value);
    }

    public IReadOnlyList<WeeklyHistoryGroupViewModel> WeeklyHistoryGroups
    {
        get => _weeklyHistoryGroups;
        private set => SetProperty(ref _weeklyHistoryGroups, value);
    }

    public IReadOnlyList<ProjectOption> ActiveProjectOptions
    {
        get => _activeProjectOptions;
        private set => SetProperty(ref _activeProjectOptions, value);
    }

    public string ArchiveToggleText => ShowArchivedProjects ? "查看当前" : "查看归档";

    public string HistoryToggleText => ShowWeeklyHistory ? "返回本周" : "查看历史";

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public string EditorValidationMessage
    {
        get => _editorValidationMessage;
        private set
        {
            if (SetProperty(ref _editorValidationMessage, value))
            {
                OnPropertyChanged(nameof(HasEditorValidationMessage));
            }
        }
    }

    public bool HasEditorValidationMessage => !string.IsNullOrWhiteSpace(EditorValidationMessage);

    public bool HasProjectsInCurrentFilter =>
        _data.Projects.Any(project => project.Archived == ShowArchivedProjects && ProjectMatches(project));

    public bool HasActiveProjects => _data.Projects.Any(project => !project.Archived);

    public bool IsCurrentWeeklyView => !ShowWeeklyHistory;

    public bool ShowProjectFirstEmptyState => !HasProjectsInCurrentFilter && _data.Projects.Count == 0;

    public bool ShowWeeklyEmptyState =>
        !ShowWeeklyHistory && HasProjectsInCurrentFilter && WeeklySections.Count == 0;

    public bool ShowLongTermEmptyState =>
        !ShowWeeklyHistory && HasProjectsInCurrentFilter && LongTermSections.Count == 0;

    public bool ShowWeeklyHistoryEmptyState =>
        ShowWeeklyHistory && WeeklyHistoryGroups.Count == 0;

    public string HeaderSubtitle =>
        ShowWeeklyHistory
            ? "按周回看往期任务"
            : $"本周起点：{DateHelper.GetStartOfWeek(DateTime.Today):yyyy-MM-dd}";

    public bool IsEditorOpen => _editorKind != EditorKind.None;
    public bool IsProjectEditorOpen => _editorKind == EditorKind.Project;
    public bool IsWeeklyTaskEditorOpen => _editorKind == EditorKind.WeeklyTask;
    public bool IsLongTermTaskEditorOpen => _editorKind == EditorKind.LongTermTask;

    public string EditorTitle => _editorKind switch
    {
        EditorKind.Project => ProjectForm.EditingId is null ? "新建项目" : "编辑项目",
        EditorKind.WeeklyTask => WeeklyTaskForm.EditingId is null ? "新建本周任务" : "编辑本周任务",
        EditorKind.LongTermTask => LongTermTaskForm.EditingId is null ? "新建长期任务" : "编辑长期任务",
        _ => string.Empty
    };

    public string EditorSubtitle => _editorKind switch
    {
        EditorKind.Project => "项目是长期任务和每周任务的归属大类。",
        EditorKind.WeeklyTask => "本周任务会按周归档，方便后续查看历史。",
        EditorKind.LongTermTask => "长期任务挂在项目下方，并保留明确的开始和结束时间。",
        _ => string.Empty
    };

    public void Load()
    {
        _data = _store.Load();
        RefreshSections();
        StatusMessage = string.Empty;
    }

    public void ToggleArchiveFilter()
    {
        ShowArchivedProjects = !ShowArchivedProjects;
        StatusMessage = ShowArchivedProjects ? "已切换到归档项目视图。" : "已切换到当前项目视图。";
    }

    public void ToggleWeeklyHistory()
    {
        ShowWeeklyHistory = !ShowWeeklyHistory;
        StatusMessage = ShowWeeklyHistory ? "已展开往期周任务。" : "已返回本周任务。";
    }

    public void OpenCreateProject()
    {
        ResetValidation();
        ProjectForm.EditingId = null;
        ProjectForm.Name = string.Empty;
        ProjectForm.Status = ProjectStatus.Active;
        _editorKind = EditorKind.Project;
        RaiseEditorStateChanged();
    }

    public void OpenEditProject(string projectId)
    {
        var project = _data.Projects.FirstOrDefault(item => item.Id == projectId);
        if (project is null)
        {
            return;
        }

        ResetValidation();
        ProjectForm.EditingId = project.Id;
        ProjectForm.Name = project.Name;
        ProjectForm.Status = project.Status;
        _editorKind = EditorKind.Project;
        RaiseEditorStateChanged();
    }

    public void OpenCreateWeeklyTask(string? preferredProjectId = null)
    {
        if (!HasActiveProjects)
        {
            StatusMessage = "请先创建项目，再添加本周任务。";
            return;
        }

        ResetValidation();
        WeeklyTaskForm.EditingId = null;
        WeeklyTaskForm.ProjectId = ResolveProjectId(preferredProjectId);
        WeeklyTaskForm.Title = string.Empty;
        WeeklyTaskForm.Status = WeeklyTaskStatus.Todo;
        WeeklyTaskForm.WeekStartDate = DateHelper.GetStartOfWeek(DateTime.Today);
        WeeklyTaskForm.DueDate = null;
        WeeklyTaskForm.Notes = string.Empty;
        _editorKind = EditorKind.WeeklyTask;
        RaiseEditorStateChanged();
    }

    public void OpenEditWeeklyTask(string taskId)
    {
        var task = _data.WeeklyTasks.FirstOrDefault(item => item.Id == taskId);
        if (task is null)
        {
            return;
        }

        ResetValidation();
        WeeklyTaskForm.EditingId = task.Id;
        WeeklyTaskForm.ProjectId = task.ProjectId;
        WeeklyTaskForm.Title = task.Title;
        WeeklyTaskForm.Status = task.Status;
        WeeklyTaskForm.WeekStartDate = task.WeekStartDate;
        WeeklyTaskForm.DueDate = task.DueDate;
        WeeklyTaskForm.Notes = task.Notes ?? string.Empty;
        _editorKind = EditorKind.WeeklyTask;
        RaiseEditorStateChanged();
    }

    public void OpenCreateLongTermTask(string? preferredProjectId = null)
    {
        if (!HasActiveProjects)
        {
            StatusMessage = "请先创建项目，再添加长期任务。";
            return;
        }

        ResetValidation();
        LongTermTaskForm.EditingId = null;
        LongTermTaskForm.ProjectId = ResolveProjectId(preferredProjectId);
        LongTermTaskForm.Title = string.Empty;
        LongTermTaskForm.Status = LongTermTaskStatus.Planned;
        LongTermTaskForm.StartDate = DateTime.Today;
        LongTermTaskForm.EndDate = DateTime.Today.AddDays(14);
        LongTermTaskForm.Notes = string.Empty;
        _editorKind = EditorKind.LongTermTask;
        RaiseEditorStateChanged();
    }

    public void OpenEditLongTermTask(string taskId)
    {
        var task = _data.LongTermTasks.FirstOrDefault(item => item.Id == taskId);
        if (task is null)
        {
            return;
        }

        ResetValidation();
        LongTermTaskForm.EditingId = task.Id;
        LongTermTaskForm.ProjectId = task.ProjectId;
        LongTermTaskForm.Title = task.Title;
        LongTermTaskForm.Status = task.Status;
        LongTermTaskForm.StartDate = task.StartDate;
        LongTermTaskForm.EndDate = task.EndDate;
        LongTermTaskForm.Notes = task.Notes ?? string.Empty;
        _editorKind = EditorKind.LongTermTask;
        RaiseEditorStateChanged();
    }

    public void CancelEditor()
    {
        ResetValidation();
        _editorKind = EditorKind.None;
        RaiseEditorStateChanged();
    }

    public bool SaveEditor()
    {
        return _editorKind switch
        {
            EditorKind.Project => SaveProject(),
            EditorKind.WeeklyTask => SaveWeeklyTask(),
            EditorKind.LongTermTask => SaveLongTermTask(),
            _ => false
        };
    }

    public void ToggleProjectArchive(string projectId)
    {
        var project = _data.Projects.FirstOrDefault(item => item.Id == projectId);
        if (project is null)
        {
            return;
        }

        project.Archived = !project.Archived;
        project.UpdatedAt = DateTime.UtcNow;
        Persist(project.Archived ? $"已归档项目：{project.Name}" : $"已恢复项目：{project.Name}");
    }

    public void UpdateWeeklyTaskStatus(string taskId, WeeklyTaskStatus status)
    {
        var task = _data.WeeklyTasks.FirstOrDefault(item => item.Id == taskId);
        if (task is null || task.Status == status)
        {
            return;
        }

        task.Status = status;
        task.UpdatedAt = DateTime.UtcNow;
        Persist("已更新本周任务状态。");
    }

    public void UpdateLongTermTaskStatus(string taskId, LongTermTaskStatus status)
    {
        var task = _data.LongTermTasks.FirstOrDefault(item => item.Id == taskId);
        if (task is null)
        {
            return;
        }

        var normalizedStatus = status == LongTermTaskStatus.Paused ? LongTermTaskStatus.Active : status;
        if (task.Status == normalizedStatus)
        {
            return;
        }

        task.Status = normalizedStatus;
        task.UpdatedAt = DateTime.UtcNow;
        Persist("已更新长期任务状态。");
    }

    private bool SaveProject()
    {
        var trimmedName = (ProjectForm.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            EditorValidationMessage = "项目名称不能为空。";
            return false;
        }

        if (trimmedName.Length > 40)
        {
            EditorValidationMessage = "项目名称建议控制在 40 个字以内。";
            return false;
        }

        var now = DateTime.UtcNow;
        if (ProjectForm.EditingId is null)
        {
            _data.Projects.Add(new ProjectItem
            {
                Name = trimmedName,
                Status = ProjectForm.Status,
                Archived = false,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            var existing = _data.Projects.First(item => item.Id == ProjectForm.EditingId);
            existing.Name = trimmedName;
            existing.Status = ProjectForm.Status;
            existing.UpdatedAt = now;
        }

        Persist(ProjectForm.EditingId is null ? "已创建项目。" : "已更新项目。");
        CancelEditor();
        return true;
    }

    private bool SaveWeeklyTask()
    {
        var trimmedTitle = (WeeklyTaskForm.Title ?? string.Empty).Trim();
        var weekStart = WeeklyTaskForm.WeekStartDate?.Date;

        if (!HasValidProjectSelection(WeeklyTaskForm.ProjectId))
        {
            EditorValidationMessage = "请选择一个当前项目。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(trimmedTitle))
        {
            EditorValidationMessage = "本周任务标题不能为空。";
            return false;
        }

        if (weekStart is null)
        {
            EditorValidationMessage = "请为本周任务选择周起始日期。";
            return false;
        }

        weekStart = DateHelper.GetStartOfWeek(weekStart.Value);
        var dueDate = WeeklyTaskForm.DueDate?.Date;

        if (dueDate is not null && DateHelper.GetStartOfWeek(dueDate.Value) != weekStart.Value)
        {
            EditorValidationMessage = "截止日期必须落在同一周内。";
            return false;
        }

        var now = DateTime.UtcNow;
        if (WeeklyTaskForm.EditingId is null)
        {
            _data.WeeklyTasks.Add(new WeeklyTaskItem
            {
                ProjectId = WeeklyTaskForm.ProjectId,
                Title = trimmedTitle,
                Status = WeeklyTaskForm.Status,
                WeekStartDate = weekStart.Value,
                DueDate = dueDate,
                Notes = CleanNotes(WeeklyTaskForm.Notes),
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            var existing = _data.WeeklyTasks.First(item => item.Id == WeeklyTaskForm.EditingId);
            existing.ProjectId = WeeklyTaskForm.ProjectId;
            existing.Title = trimmedTitle;
            existing.Status = WeeklyTaskForm.Status;
            existing.WeekStartDate = weekStart.Value;
            existing.DueDate = dueDate;
            existing.Notes = CleanNotes(WeeklyTaskForm.Notes);
            existing.UpdatedAt = now;
        }

        Persist(WeeklyTaskForm.EditingId is null ? "已创建本周任务。" : "已更新本周任务。");
        CancelEditor();
        return true;
    }

    private bool SaveLongTermTask()
    {
        var trimmedTitle = (LongTermTaskForm.Title ?? string.Empty).Trim();
        var startDate = LongTermTaskForm.StartDate?.Date;
        var endDate = LongTermTaskForm.EndDate?.Date;

        if (!HasValidProjectSelection(LongTermTaskForm.ProjectId))
        {
            EditorValidationMessage = "请选择一个当前项目。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(trimmedTitle))
        {
            EditorValidationMessage = "长期任务标题不能为空。";
            return false;
        }

        if (startDate is null || endDate is null)
        {
            EditorValidationMessage = "开始日期和结束日期都不能为空。";
            return false;
        }

        if (endDate.Value < startDate.Value)
        {
            EditorValidationMessage = "结束日期不能早于开始日期。";
            return false;
        }

        var now = DateTime.UtcNow;
        var normalizedStatus = LongTermTaskForm.Status == LongTermTaskStatus.Paused
            ? LongTermTaskStatus.Active
            : LongTermTaskForm.Status;

        if (LongTermTaskForm.EditingId is null)
        {
            _data.LongTermTasks.Add(new LongTermTaskItem
            {
                ProjectId = LongTermTaskForm.ProjectId,
                Title = trimmedTitle,
                Status = normalizedStatus,
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                Notes = CleanNotes(LongTermTaskForm.Notes),
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            var existing = _data.LongTermTasks.First(item => item.Id == LongTermTaskForm.EditingId);
            existing.ProjectId = LongTermTaskForm.ProjectId;
            existing.Title = trimmedTitle;
            existing.Status = normalizedStatus;
            existing.StartDate = startDate.Value;
            existing.EndDate = endDate.Value;
            existing.Notes = CleanNotes(LongTermTaskForm.Notes);
            existing.UpdatedAt = now;
        }

        Persist(LongTermTaskForm.EditingId is null ? "已创建长期任务。" : "已更新长期任务。");
        CancelEditor();
        return true;
    }

    private void Persist(string message)
    {
        _store.Save(_data);
        RefreshSections();
        StatusMessage = message;
    }

    private void RefreshSections()
    {
        ActiveProjectOptions = _data.Projects
            .Where(project => !project.Archived)
            .OrderBy(project => project.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(project => new ProjectOption
            {
                Id = project.Id,
                Name = project.Name
            })
            .ToList();

        var currentWeekStart = DateHelper.GetStartOfWeek(DateTime.Today);
        var visibleProjects = _data.Projects
            .Where(project => project.Archived == ShowArchivedProjects)
            .Where(ProjectMatches)
            .OrderBy(project => project.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToDictionary(project => project.Id);

        var visibleWeeklyTasks = _data.WeeklyTasks
            .Where(task => visibleProjects.ContainsKey(task.ProjectId))
            .Where(WeeklyTaskMatches)
            .ToList();

        WeeklySections = visibleWeeklyTasks
            .Where(task => task.WeekStartDate.Date == currentWeekStart.Date)
            .GroupBy(task => task.ProjectId)
            .Select(group => BuildWeeklySection(visibleProjects[group.Key], group))
            .OrderBy(section => section.ProjectName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        WeeklyHistoryGroups = visibleWeeklyTasks
            .Where(task => task.WeekStartDate.Date < currentWeekStart.Date)
            .GroupBy(task => task.WeekStartDate.Date)
            .OrderByDescending(group => group.Key)
            .Select(group => new WeeklyHistoryGroupViewModel
            {
                WeekTitle = $"{group.Key:yyyy-MM-dd} 这一周",
                WeekSubtitle = $"共 {group.Count()} 个任务",
                Sections = group
                    .GroupBy(task => task.ProjectId)
                    .Select(projectGroup => BuildWeeklySection(visibleProjects[projectGroup.Key], projectGroup))
                    .OrderBy(section => section.ProjectName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()
            })
            .ToList();

        LongTermSections = _data.LongTermTasks
            .Where(task => visibleProjects.ContainsKey(task.ProjectId))
            .Where(LongTermTaskMatches)
            .GroupBy(task => task.ProjectId)
            .Select(group => BuildLongTermSection(visibleProjects[group.Key], group))
            .OrderBy(section => section.ProjectName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        OnPropertyChanged(nameof(HasProjectsInCurrentFilter));
        OnPropertyChanged(nameof(HasActiveProjects));
        OnPropertyChanged(nameof(ShowProjectFirstEmptyState));
        OnPropertyChanged(nameof(ShowWeeklyEmptyState));
        OnPropertyChanged(nameof(ShowLongTermEmptyState));
        OnPropertyChanged(nameof(ShowWeeklyHistoryEmptyState));
        OnPropertyChanged(nameof(HeaderSubtitle));
        OnPropertyChanged(nameof(ArchiveToggleText));
        OnPropertyChanged(nameof(HistoryToggleText));
    }

    private ProjectSectionViewModel BuildWeeklySection(ProjectItem project, IEnumerable<WeeklyTaskItem> tasks)
    {
        var projectTone = GetProjectTone(project.Status);
        var rows = tasks
            .OrderBy(task => task.Status == WeeklyTaskStatus.Done ? 1 : 0)
            .ThenBy(task => task.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(task => task.UpdatedAt)
            .Select(task =>
            {
                var tone = GetWeeklyTone(task.Status);
                return new WeeklyTaskRowViewModel
                {
                    Id = task.Id,
                    Title = task.Title,
                    StatusGroupName = $"weekly-{task.Id}",
                    Status = task.Status,
                    StatusText = GetWeeklyStatusText(task.Status),
                    StatusPillBackground = tone.Background,
                    StatusPillForeground = tone.Foreground,
                    MetaText = BuildWeeklyMetaText(task),
                    NotesPreview = task.Notes ?? string.Empty
                };
            })
            .ToList();

        return new ProjectSectionViewModel
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            ProjectMeta = $"{GetProjectStatusText(project.Status)} · {rows.Count} 项本周任务",
            ArchiveActionText = project.Archived ? "恢复" : "归档",
            StatusText = GetProjectStatusText(project.Status),
            StatusBadgeBackground = projectTone.Background,
            StatusBadgeForeground = projectTone.Foreground,
            AccentBrush = projectTone.Accent,
            WeeklyTasks = rows
        };
    }

    private ProjectSectionViewModel BuildLongTermSection(ProjectItem project, IEnumerable<LongTermTaskItem> tasks)
    {
        var projectTone = GetProjectTone(project.Status);
        var order = new Dictionary<LongTermTaskStatus, int>
        {
            [LongTermTaskStatus.Active] = 0,
            [LongTermTaskStatus.Paused] = 0,
            [LongTermTaskStatus.Planned] = 1,
            [LongTermTaskStatus.Completed] = 2
        };

        var rows = tasks
            .OrderBy(task => order[task.Status])
            .ThenBy(task => task.EndDate)
            .Select(task =>
            {
                var tone = GetLongTermTone(task.Status);
                return new LongTermTaskRowViewModel
                {
                    Id = task.Id,
                    Title = task.Title,
                    StatusGroupName = $"longterm-{task.Id}",
                    Status = task.Status,
                    StatusText = GetLongTermStatusText(task.Status),
                    StatusPillBackground = tone.Background,
                    StatusPillForeground = tone.Foreground,
                    MetaText = BuildLongTermMetaText(task),
                    NotesPreview = task.Notes ?? string.Empty
                };
            })
            .ToList();

        return new ProjectSectionViewModel
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            ProjectMeta = $"{GetProjectStatusText(project.Status)} · {rows.Count} 项长期任务",
            ArchiveActionText = project.Archived ? "恢复" : "归档",
            StatusText = GetProjectStatusText(project.Status),
            StatusBadgeBackground = projectTone.Background,
            StatusBadgeForeground = projectTone.Foreground,
            AccentBrush = projectTone.Accent,
            LongTermTasks = rows
        };
    }

    private bool ProjectMatches(ProjectItem project)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return project.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase);
    }

    private bool WeeklyTaskMatches(WeeklyTaskItem task)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return task.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
            || (task.Notes?.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ?? false);
    }

    private bool LongTermTaskMatches(LongTermTaskItem task)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return task.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
            || (task.Notes?.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ?? false);
    }

    private string ResolveProjectId(string? preferredProjectId)
    {
        if (!string.IsNullOrWhiteSpace(preferredProjectId) && HasValidProjectSelection(preferredProjectId))
        {
            return preferredProjectId;
        }

        return ActiveProjectOptions.First().Id;
    }

    private bool HasValidProjectSelection(string? projectId)
    {
        return !string.IsNullOrWhiteSpace(projectId)
            && _data.Projects.Any(project => project.Id == projectId && !project.Archived);
    }

    private void ResetValidation()
    {
        EditorValidationMessage = string.Empty;
    }

    private void RaiseEditorStateChanged()
    {
        OnPropertyChanged(nameof(IsEditorOpen));
        OnPropertyChanged(nameof(IsProjectEditorOpen));
        OnPropertyChanged(nameof(IsWeeklyTaskEditorOpen));
        OnPropertyChanged(nameof(IsLongTermTaskEditorOpen));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(EditorSubtitle));
    }

    private static string? CleanNotes(string? notes)
    {
        var trimmed = notes?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string BuildWeeklyMetaText(WeeklyTaskItem task)
    {
        if (task.DueDate is null)
        {
            return $"时间：{task.WeekStartDate:yyyy-MM-dd}";
        }

        return $"时间：{task.WeekStartDate:yyyy-MM-dd} - {task.DueDate:yyyy-MM-dd}";
    }

    private static string BuildLongTermMetaText(LongTermTaskItem task)
    {
        return $"时间：{task.StartDate:yyyy-MM-dd} - {task.EndDate:yyyy-MM-dd}";
    }

    private static string GetProjectStatusText(ProjectStatus status)
    {
        return status switch
        {
            ProjectStatus.Active => "进行中",
            ProjectStatus.OnHold => "已暂停",
            ProjectStatus.Completed => "已完成",
            _ => status.ToString()
        };
    }

    private static string GetWeeklyStatusText(WeeklyTaskStatus status)
    {
        return status switch
        {
            WeeklyTaskStatus.Todo => "未开始",
            WeeklyTaskStatus.InProgress => "进行中",
            WeeklyTaskStatus.Done => "已完成",
            _ => status.ToString()
        };
    }

    private static string GetLongTermStatusText(LongTermTaskStatus status)
    {
        return status switch
        {
            LongTermTaskStatus.Planned => "未开始",
            LongTermTaskStatus.Active => "进行中",
            LongTermTaskStatus.Paused => "进行中",
            LongTermTaskStatus.Completed => "已完成",
            _ => status.ToString()
        };
    }

    private static StatusTone GetProjectTone(ProjectStatus status)
    {
        return status switch
        {
            ProjectStatus.Active => new StatusTone("#EAF3FD", "#1B5DAE", "#2A73C9"),
            ProjectStatus.OnHold => new StatusTone("#FFF3DE", "#8E5A00", "#D98B00"),
            ProjectStatus.Completed => new StatusTone("#E7F7EE", "#1E7A4A", "#2DAA68"),
            _ => new StatusTone("#EAF3FD", "#1B5DAE", "#2A73C9")
        };
    }

    private static StatusTone GetWeeklyTone(WeeklyTaskStatus status)
    {
        return status switch
        {
            WeeklyTaskStatus.Todo => new StatusTone("#EEF2F7", "#4F6277", "#A5B4C4"),
            WeeklyTaskStatus.InProgress => new StatusTone("#EAF3FD", "#1B5DAE", "#2A73C9"),
            WeeklyTaskStatus.Done => new StatusTone("#E7F7EE", "#1E7A4A", "#2DAA68"),
            _ => new StatusTone("#EEF2F7", "#4F6277", "#A5B4C4")
        };
    }

    private static StatusTone GetLongTermTone(LongTermTaskStatus status)
    {
        return status switch
        {
            LongTermTaskStatus.Planned => new StatusTone("#EEF2F7", "#4F6277", "#A5B4C4"),
            LongTermTaskStatus.Active => new StatusTone("#EAF3FD", "#1B5DAE", "#2A73C9"),
            LongTermTaskStatus.Paused => new StatusTone("#EAF3FD", "#1B5DAE", "#2A73C9"),
            LongTermTaskStatus.Completed => new StatusTone("#E7F7EE", "#1E7A4A", "#2DAA68"),
            _ => new StatusTone("#EEF2F7", "#4F6277", "#A5B4C4")
        };
    }

    private readonly record struct StatusTone(string Background, string Foreground, string Accent);

    private enum EditorKind
    {
        None,
        Project,
        WeeklyTask,
        LongTermTask
    }
}

internal static class DateHelper
{
    public static DateTime GetStartOfWeek(DateTime date)
    {
        var localDate = date.Date;
        var delta = ((int)localDate.DayOfWeek + 6) % 7;
        return localDate.AddDays(-delta);
    }
}

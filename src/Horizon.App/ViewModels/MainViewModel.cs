using Horizon.App.Models;
using Horizon.App.Services;

namespace Horizon.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly HorizonDataStore _store;
    private readonly IStartupRegistrationService _startupRegistrationService;
    private HorizonDataFile _data = new();
    private string _statusMessage = string.Empty;
    private string _editorValidationMessage = string.Empty;
    private string _searchText = string.Empty;
    private MainContentMode _mainContentMode;
    private HistoryTaskKind _historyTaskKind;
    private DateTime? _selectedHistoryWeekStart;
    private bool _showArchivedTasks;
    private EditorKind _editorKind = EditorKind.None;
    private IReadOnlyList<ProjectSectionViewModel> _weeklySections = [];
    private IReadOnlyList<ProjectSectionViewModel> _longTermSections = [];
    private IReadOnlyList<WeeklyHistoryGroupViewModel> _weeklyHistoryGroups = [];
    private IReadOnlyList<HistoryWeekSummaryViewModel> _historyWeeks = [];
    private IReadOnlyList<ProjectSectionViewModel> _historyDetailSections = [];
    private IReadOnlyList<AnnotationRowViewModel> _annotationRows = [];
    private IReadOnlyList<ProjectCatalogRowViewModel> _projectCatalogRows = [];
    private IReadOnlyList<string> _projectNameOptions = [];

    public MainViewModel(
        HorizonDataStore store,
        IStartupRegistrationService startupRegistrationService)
    {
        _store = store;
        _startupRegistrationService = startupRegistrationService;
        Load();
    }

    public WeeklyTaskFormModel WeeklyTaskForm { get; } = new();
    public LongTermTaskFormModel LongTermTaskForm { get; } = new();
    public SettingsFormModel SettingsForm { get; } = new();
    public AnnotationEditorModel AnnotationEditor { get; } = new();

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

    public MainContentMode ContentMode
    {
        get => _mainContentMode;
        private set
        {
            if (SetProperty(ref _mainContentMode, value))
            {
                RaiseHistoryStateChanged();
            }
        }
    }

    public HistoryTaskKind SelectedHistoryKind
    {
        get => _historyTaskKind;
        private set
        {
            if (SetProperty(ref _historyTaskKind, value))
            {
                _selectedHistoryWeekStart = null;
                OnPropertyChanged(nameof(SelectedHistoryWeekStart));
                RefreshSections();
                RaiseHistoryStateChanged();
            }
        }
    }

    public DateTime? SelectedHistoryWeekStart
    {
        get => _selectedHistoryWeekStart;
        private set
        {
            if (SetProperty(ref _selectedHistoryWeekStart, value))
            {
                RaiseHistoryStateChanged();
            }
        }
    }

    public bool ShowArchivedTasks
    {
        get => _showArchivedTasks;
        private set
        {
            if (SetProperty(ref _showArchivedTasks, value))
            {
                OnPropertyChanged(nameof(ArchiveToggleText));
                RefreshSections();
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

    public IReadOnlyList<HistoryWeekSummaryViewModel> HistoryWeeks
    {
        get => _historyWeeks;
        private set => SetProperty(ref _historyWeeks, value);
    }

    public IReadOnlyList<ProjectSectionViewModel> HistoryDetailSections
    {
        get => _historyDetailSections;
        private set => SetProperty(ref _historyDetailSections, value);
    }

    public IReadOnlyList<ProjectCatalogRowViewModel> ProjectCatalogRows
    {
        get => _projectCatalogRows;
        private set => SetProperty(ref _projectCatalogRows, value);
    }

    public IReadOnlyList<AnnotationRowViewModel> AnnotationRows
    {
        get => _annotationRows;
        private set
        {
            if (SetProperty(ref _annotationRows, value))
            {
                OnPropertyChanged(nameof(ShowAnnotationEmptyState));
            }
        }
    }

    public IReadOnlyList<string> WeeklyProjectNameOptions => _projectNameOptions;
    public IReadOnlyList<string> LongTermProjectNameOptions => _projectNameOptions;

    public string ArchiveToggleText => ShowArchivedTasks ? "查看当前" : "查看归档";
    public string HistoryToggleText => ContentMode == MainContentMode.History ? "返回主页" : "历史";
    public bool IsPinned => _data.Settings.IsPinned;
    public string PinButtonText => IsPinned ? "已置顶" : "置顶";
    public bool StartWithWindows => _data.Settings.StartWithWindows;
    public string StartWithWindowsText => StartWithWindows ? "已开启" : "已关闭";

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
    public bool IsCurrentWeeklyView => ContentMode == MainContentMode.Current;
    public bool ShowHistoryView => ContentMode == MainContentMode.History;
    public bool ShowHistoryWeekList => ShowHistoryView && SelectedHistoryWeekStart is null;
    public bool ShowHistoryWeekDetail => ShowHistoryView && SelectedHistoryWeekStart is not null;
    public bool IsWeeklyHistoryKind => SelectedHistoryKind == HistoryTaskKind.Weekly;
    public bool IsLongTermHistoryKind => SelectedHistoryKind == HistoryTaskKind.LongTerm;
    public bool ShowWeeklyEmptyState => IsCurrentWeeklyView && WeeklySections.Count == 0;
    public bool ShowLongTermEmptyState => IsCurrentWeeklyView && LongTermSections.Count == 0;
    public bool ShowHistoryEmptyState => ShowHistoryWeekList && HistoryWeeks.Count == 0;
    public string SelectedHistoryTitle => SelectedHistoryWeekStart is null
        ? string.Empty
        : $"{SelectedHistoryWeekStart:yyyy-MM-dd} — {SelectedHistoryWeekStart.Value.AddDays(6):yyyy-MM-dd}";
    public string SelectedHistorySummary => SelectedHistoryWeekStart is null
        ? string.Empty
        : $"{(IsWeeklyHistoryKind ? "每周任务" : "长期任务")} · {HistoryDetailSections.Sum(section => IsWeeklyHistoryKind ? section.WeeklyTasks.Count : section.LongTermTasks.Count)} 项";

    public string HeaderSubtitle =>
        ShowHistoryView
            ? "按周查看已完成任务"
            : $"当前周：{DateHelper.GetStartOfWeek(DateTime.Today):yyyy-MM-dd}";

    public bool IsEditorOpen => _editorKind != EditorKind.None;
    public bool IsWeeklyTaskEditorOpen => _editorKind == EditorKind.WeeklyTask;
    public bool IsLongTermTaskEditorOpen => _editorKind == EditorKind.LongTermTask;
    public bool IsSettingsOpen => _editorKind == EditorKind.Settings;
    public bool IsAnnotationEditorOpen => _editorKind == EditorKind.Annotations;
    public bool ShowAnnotationEmptyState => IsAnnotationEditorOpen && AnnotationRows.Count == 0;
    public bool IsEditingProjectInSettings => !string.IsNullOrWhiteSpace(SettingsForm.EditingProjectId);
    public bool ShowDualFooterButtons => !IsSettingsOpen;
    public bool ShowGenericEditorFooter => !IsAnnotationEditorOpen;
    public string AnnotationSaveButtonText => AnnotationEditor.EditingAnnotationId is null ? "添加批注" : "保存修改";
    public string SettingsProjectActionText => IsEditingProjectInSettings ? "保存修改" : "新增项目";
    public string EditorConfirmButtonText => _editorKind == EditorKind.Settings ? "关闭" : "保存";

    public string EditorTitle => _editorKind switch
    {
        EditorKind.WeeklyTask => WeeklyTaskForm.EditingId is null ? "新建本周任务" : "编辑本周任务",
        EditorKind.LongTermTask => LongTermTaskForm.EditingId is null ? "新建长期任务" : "编辑长期任务",
        EditorKind.Settings => "项目设置",
        EditorKind.Annotations => "任务批注",
        _ => string.Empty
    };

    public string EditorSubtitle => _editorKind switch
    {
        EditorKind.WeeklyTask => "本周任务默认归入当前周，不再单独设置周起始时间和截止时间。",
        EditorKind.LongTermTask => "长期任务保留开始时间和结束时间，项目名称可以下拉选择，也支持手动输入。",
        EditorKind.Settings => "这里维护项目列表。新增和修改后，项目名称会立即出现在任务表单的下拉选择中。",
        EditorKind.Annotations => AnnotationEditor.TaskTitle,
        _ => string.Empty
    };

    public void Load()
    {
        _data = _store.Load();

        var reconciled = WeeklyRolloverService.Reconcile(_data, DateTime.Today, DateTime.UtcNow);
        if (reconciled | AutoUpdateProjectStatus())
        {
            _store.Save(_data);
        }

        RefreshProjectCatalogRows();
        RefreshProjectNameOptions();
        RefreshSections();
        StatusMessage = string.Empty;
    }

    public bool ReconcileForDate(DateTime localToday, DateTime nowUtc)
    {
        var changed = WeeklyRolloverService.Reconcile(_data, localToday, nowUtc);
        if (changed)
        {
            _store.Save(_data);
        }

        RefreshSections();
        StatusMessage = "已完成新一周任务整理。";
        return changed;
    }

    public void ToggleHistory()
    {
        ContentMode = ContentMode == MainContentMode.Current
            ? MainContentMode.History
            : MainContentMode.Current;
        SelectedHistoryWeekStart = null;
        RefreshSections();
        StatusMessage = ShowHistoryView ? "已打开历史。" : "已返回主页。";
    }

    public void SelectWeeklyHistory()
    {
        SelectedHistoryKind = HistoryTaskKind.Weekly;
        StatusMessage = "正在查看每周任务历史。";
    }

    public void SelectLongTermHistory()
    {
        SelectedHistoryKind = HistoryTaskKind.LongTerm;
        StatusMessage = "正在查看长期任务历史。";
    }

    public void OpenHistoryWeek(DateTime weekStart)
    {
        SelectedHistoryWeekStart = weekStart.Date;
        RefreshSections();
    }

    public void BackToHistoryWeeks()
    {
        SelectedHistoryWeekStart = null;
        RefreshSections();
    }

    public void ToggleArchivedTasks()
    {
        ShowArchivedTasks = !ShowArchivedTasks;
        StatusMessage = ShowArchivedTasks ? "已切换到归档任务视图。" : "已切换到当前任务视图。";
    }

    public void TogglePinned()
    {
        _data.Settings.IsPinned = !_data.Settings.IsPinned;
        _store.Save(_data);
        OnPropertyChanged(nameof(IsPinned));
        OnPropertyChanged(nameof(PinButtonText));
        StatusMessage = IsPinned ? "主面板已置顶。" : "已恢复点击外部自动收起。";
    }

    public bool ReconcileStartupRegistration()
    {
        if (_startupRegistrationService.TrySetEnabled(StartWithWindows, out var errorMessage))
        {
            return true;
        }

        StatusMessage = errorMessage ?? "无法同步开机启动设置。";
        return false;
    }

    public bool ToggleStartWithWindows()
    {
        var requestedValue = !StartWithWindows;
        if (!_startupRegistrationService.TrySetEnabled(requestedValue, out var errorMessage))
        {
            StatusMessage = errorMessage ?? "无法更新开机启动设置。";
            return false;
        }

        _data.Settings.StartWithWindows = requestedValue;
        _store.Save(_data);
        OnPropertyChanged(nameof(StartWithWindows));
        OnPropertyChanged(nameof(StartWithWindowsText));
        StatusMessage = StartWithWindows ? "已开启开机自动启动。" : "已关闭开机自动启动。";
        return true;
    }

    public void OpenCreateWeeklyTask()
    {
        ResetValidation();
        WeeklyTaskForm.EditingId = null;
        WeeklyTaskForm.ProjectName = string.Empty;
        WeeklyTaskForm.Title = string.Empty;
        WeeklyTaskForm.Status = WeeklyTaskStatus.Todo;
        WeeklyTaskForm.Notes = string.Empty;
        WeeklyTaskForm.Progress = 0;
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

        var project = _data.Projects.FirstOrDefault(item => item.Id == task.ProjectId);

        ResetValidation();
        WeeklyTaskForm.EditingId = task.Id;
        WeeklyTaskForm.ProjectName = project?.Name ?? string.Empty;
        WeeklyTaskForm.Title = task.Title;
        WeeklyTaskForm.Status = task.Status;
        WeeklyTaskForm.Notes = task.Notes ?? string.Empty;
        WeeklyTaskForm.Progress = task.Progress;
        _editorKind = EditorKind.WeeklyTask;
        RaiseEditorStateChanged();
    }

    public void OpenCreateLongTermTask()
    {
        ResetValidation();
        LongTermTaskForm.EditingId = null;
        LongTermTaskForm.ProjectName = string.Empty;
        LongTermTaskForm.Title = string.Empty;
        LongTermTaskForm.Status = LongTermTaskStatus.Planned;
        LongTermTaskForm.StartDate = DateTime.Today;
        LongTermTaskForm.EndDate = DateTime.Today.AddDays(14);
        LongTermTaskForm.Notes = string.Empty;
        LongTermTaskForm.Progress = 0;
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

        var project = _data.Projects.FirstOrDefault(item => item.Id == task.ProjectId);

        ResetValidation();
        LongTermTaskForm.EditingId = task.Id;
        LongTermTaskForm.ProjectName = project?.Name ?? string.Empty;
        LongTermTaskForm.Title = task.Title;
        LongTermTaskForm.Status = task.Status;
        LongTermTaskForm.StartDate = task.StartDate;
        LongTermTaskForm.EndDate = task.EndDate;
        LongTermTaskForm.Notes = task.Notes ?? string.Empty;
        LongTermTaskForm.Progress = task.Progress;
        _editorKind = EditorKind.LongTermTask;
        RaiseEditorStateChanged();
    }

    public void OpenSettings()
    {
        ResetValidation();
        ResetSettingsProjectEditor();
        RefreshProjectCatalogRows();
        _editorKind = EditorKind.Settings;
        RaiseEditorStateChanged();
    }

    public void StartCreateProjectInSettings()
    {
        ResetValidation();
        ResetSettingsProjectEditor();
    }

    public void StartEditProjectInSettings(string projectId)
    {
        var project = _data.Projects.FirstOrDefault(item => item.Id == projectId);
        if (project is null)
        {
            return;
        }

        ResetValidation();
        SettingsForm.EditingProjectId = project.Id;
        SettingsForm.ProjectNameInput = project.Name;
        OnPropertyChanged(nameof(IsEditingProjectInSettings));
        OnPropertyChanged(nameof(SettingsProjectActionText));
    }

    public bool SaveProjectInSettings()
    {
        var trimmedName = (SettingsForm.ProjectNameInput ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            EditorValidationMessage = "请输入项目名称。";
            return false;
        }

        if (trimmedName.Length > 40)
        {
            EditorValidationMessage = "项目名称建议控制在 40 个字符以内。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SettingsForm.EditingProjectId))
        {
            if (FindProjectByName(trimmedName) is not null)
            {
                EditorValidationMessage = "这个项目已经存在。";
                return false;
            }

            var now = DateTime.UtcNow;
            _data.Projects.Add(new ProjectItem
            {
                Name = trimmedName,
                Status = ProjectStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            });

            PersistWithoutClosing($"已新增项目：{trimmedName}");
        }
        else
        {
            var project = _data.Projects.FirstOrDefault(item => item.Id == SettingsForm.EditingProjectId);
            if (project is null)
            {
                EditorValidationMessage = "找不到要修改的项目。";
                return false;
            }

            var duplicate = FindProjectByName(trimmedName);
            if (duplicate is not null && duplicate.Id != project.Id)
            {
                EditorValidationMessage = "已经有同名项目，请换一个名称。";
                return false;
            }

            project.Name = trimmedName;
            project.UpdatedAt = DateTime.UtcNow;
            PersistWithoutClosing($"已修改项目：{trimmedName}");
        }

        ResetSettingsProjectEditor();
        return true;
    }

    public void DeleteProjectInSettings(string projectId)
    {
        var project = _data.Projects.FirstOrDefault(item => item.Id == projectId);
        if (project is null)
        {
            return;
        }

        var weeklyCount = _data.WeeklyTasks.RemoveAll(item => item.ProjectId == projectId);
        var longTermCount = _data.LongTermTasks.RemoveAll(item => item.ProjectId == projectId);
        _data.Projects.Remove(project);

        if (SettingsForm.EditingProjectId == projectId)
        {
            ResetSettingsProjectEditor();
        }

        PersistWithoutClosing($"已删除项目：{project.Name}，并移除 {weeklyCount + longTermCount} 项任务。");
    }

    public void OpenWeeklyAnnotations(string taskId)
    {
        var task = _data.WeeklyTasks.FirstOrDefault(item => item.Id == taskId);
        if (task is not null)
        {
            OpenAnnotations(TaskKind.Weekly, task.Id, task.Title);
        }
    }

    public void OpenLongTermAnnotations(string taskId)
    {
        var task = _data.LongTermTasks.FirstOrDefault(item => item.Id == taskId);
        if (task is not null)
        {
            OpenAnnotations(TaskKind.LongTerm, task.Id, task.Title);
        }
    }

    public bool SaveAnnotation()
    {
        ResetValidation();
        if (!TaskAnnotationRules.IsValid(AnnotationEditor.Content))
        {
            EditorValidationMessage = string.IsNullOrWhiteSpace(AnnotationEditor.Content)
                ? "批注内容不能为空。"
                : $"批注内容不能超过 {TaskAnnotationRules.MaxLength} 个字符。";
            return false;
        }

        var annotations = GetAnnotationTarget(out _);
        if (annotations is null)
        {
            CancelEditor();
            StatusMessage = "任务不存在，无法保存批注。";
            return false;
        }

        var now = DateTime.UtcNow;
        var message = "已添加批注。";
        if (AnnotationEditor.EditingAnnotationId is null)
        {
            TaskAnnotationRules.Add(annotations, AnnotationEditor.Content, now);
        }
        else if (TaskAnnotationRules.Update(
                     annotations,
                     AnnotationEditor.EditingAnnotationId,
                     AnnotationEditor.Content,
                     now))
        {
            message = "已更新批注。";
        }
        else
        {
            EditorValidationMessage = "批注不存在，无法修改。";
            return false;
        }

        AnnotationEditor.Content = string.Empty;
        AnnotationEditor.EditingAnnotationId = null;
        PersistWithoutClosing(message);
        RefreshAnnotationRows();
        OnPropertyChanged(nameof(AnnotationSaveButtonText));
        return true;
    }

    public void StartEditAnnotation(string annotationId)
    {
        var annotation = GetAnnotationTarget(out _)?.FirstOrDefault(item => item.Id == annotationId);
        if (annotation is null)
        {
            return;
        }

        ResetValidation();
        AnnotationEditor.EditingAnnotationId = annotation.Id;
        AnnotationEditor.Content = annotation.Content;
        AnnotationEditor.PendingDeleteAnnotationId = null;
        RefreshAnnotationRows();
        OnPropertyChanged(nameof(AnnotationSaveButtonText));
    }

    public void CancelAnnotationEdit()
    {
        ResetValidation();
        AnnotationEditor.EditingAnnotationId = null;
        AnnotationEditor.Content = string.Empty;
        OnPropertyChanged(nameof(AnnotationSaveButtonText));
    }

    public void RequestDeleteAnnotation(string annotationId)
    {
        AnnotationEditor.PendingDeleteAnnotationId = annotationId;
        RefreshAnnotationRows();
    }

    public void CancelDeleteAnnotation()
    {
        AnnotationEditor.PendingDeleteAnnotationId = null;
        RefreshAnnotationRows();
    }

    public void ConfirmDeleteAnnotation(string annotationId)
    {
        var annotations = GetAnnotationTarget(out _);
        if (annotations is null || !TaskAnnotationRules.Delete(annotations, annotationId))
        {
            return;
        }

        if (AnnotationEditor.EditingAnnotationId == annotationId)
        {
            CancelAnnotationEdit();
        }

        AnnotationEditor.PendingDeleteAnnotationId = null;
        PersistWithoutClosing("已删除批注。");
        RefreshAnnotationRows();
    }

    public void CancelEditor()
    {
        ResetValidation();
        if (_editorKind == EditorKind.Annotations)
        {
            ResetAnnotationEditor();
        }
        _editorKind = EditorKind.None;
        RaiseEditorStateChanged();
    }

    public bool SaveEditor()
    {
        return _editorKind switch
        {
            EditorKind.WeeklyTask => SaveWeeklyTask(),
            EditorKind.LongTermTask => SaveLongTermTask(),
            EditorKind.Settings => CloseSettingsEditor(),
            _ => false
        };
    }

    public void UpdateWeeklyTaskStatus(string taskId, WeeklyTaskStatus status)
    {
        var task = _data.WeeklyTasks.FirstOrDefault(item => item.Id == taskId);
        if (task is null || task.Status == status)
        {
            return;
        }

        var now = DateTime.UtcNow;
        task.Status = status;
        task.CompletedAt = status == WeeklyTaskStatus.Done ? task.CompletedAt ?? now : null;
        task.UpdatedAt = now;
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

        var now = DateTime.UtcNow;
        task.Status = normalizedStatus;
        task.CompletedAt = normalizedStatus == LongTermTaskStatus.Completed ? task.CompletedAt ?? now : null;
        task.UpdatedAt = now;
        Persist("已更新长期任务状态。");
    }

    public void ToggleWeeklyTaskArchive(string taskId)
    {
        var task = _data.WeeklyTasks.FirstOrDefault(item => item.Id == taskId);
        if (task is null)
        {
            return;
        }

        task.Archived = !task.Archived;
        task.UpdatedAt = DateTime.UtcNow;
        Persist(task.Archived ? $"已归档本周任务：{task.Title}" : $"已恢复本周任务：{task.Title}");
    }

    public void ToggleLongTermTaskArchive(string taskId)
    {
        var task = _data.LongTermTasks.FirstOrDefault(item => item.Id == taskId);
        if (task is null)
        {
            return;
        }

        task.Archived = !task.Archived;
        task.UpdatedAt = DateTime.UtcNow;
        Persist(task.Archived ? $"已归档长期任务：{task.Title}" : $"已恢复长期任务：{task.Title}");
    }

    public double GetCollapsedButtonTop() => _data.Settings.CollapsedButtonTop;

    public void UpdateCollapsedButtonTop(double top)
    {
        if (Math.Abs(_data.Settings.CollapsedButtonTop - top) < 0.5)
        {
            return;
        }

        _data.Settings.CollapsedButtonTop = top;
        _store.Save(_data);
    }

    private ProjectItem ResolveOrCreateProject(string projectName)
    {
        var trimmedName = projectName.Trim();
        var existing = FindProjectByName(trimmedName);
        if (existing is not null)
        {
            existing.UpdatedAt = DateTime.UtcNow;
            return existing;
        }

        var now = DateTime.UtcNow;
        var project = new ProjectItem
        {
            Name = trimmedName,
            Status = ProjectStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        _data.Projects.Add(project);
        return project;
    }

    private ProjectItem? FindProjectByName(string name)
    {
        return _data.Projects.FirstOrDefault(
            item => item.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
    }

    private bool SaveWeeklyTask()
    {
        var trimmedProjectName = (WeeklyTaskForm.ProjectName ?? string.Empty).Trim();
        var trimmedTitle = (WeeklyTaskForm.Title ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(trimmedProjectName))
        {
            EditorValidationMessage = "请输入项目名称。";
            return false;
        }

        if (trimmedProjectName.Length > 40)
        {
            EditorValidationMessage = "项目名称建议控制在 40 个字符以内。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(trimmedTitle))
        {
            EditorValidationMessage = "本周任务标题不能为空。";
            return false;
        }

        var project = ResolveOrCreateProject(trimmedProjectName);
        var now = DateTime.UtcNow;

        if (WeeklyTaskForm.EditingId is null)
        {
            _data.WeeklyTasks.Add(new WeeklyTaskItem
            {
                ProjectId = project.Id,
                Title = trimmedTitle,
                Status = WeeklyTaskForm.Status,
                WeekStartDate = DateHelper.GetStartOfWeek(DateTime.Today),
                DueDate = null,
                Notes = CleanNotes(WeeklyTaskForm.Notes),
                Progress = WeeklyTaskForm.Progress,
                CompletedAt = WeeklyTaskForm.Status == WeeklyTaskStatus.Done ? now : null,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            var existing = _data.WeeklyTasks.First(item => item.Id == WeeklyTaskForm.EditingId);
            existing.ProjectId = project.Id;
            existing.Title = trimmedTitle;
            existing.Status = WeeklyTaskForm.Status;
            existing.CompletedAt = WeeklyTaskForm.Status == WeeklyTaskStatus.Done
                ? existing.CompletedAt ?? now
                : null;
            existing.DueDate = null;
            existing.Notes = CleanNotes(WeeklyTaskForm.Notes);
            existing.Progress = WeeklyTaskForm.Progress;
            existing.UpdatedAt = now;
        }

        Persist(WeeklyTaskForm.EditingId is null ? "已创建本周任务。" : "已更新本周任务。");
        CancelEditor();
        return true;
    }

    private bool SaveLongTermTask()
    {
        var trimmedProjectName = (LongTermTaskForm.ProjectName ?? string.Empty).Trim();
        var trimmedTitle = (LongTermTaskForm.Title ?? string.Empty).Trim();
        var startDate = LongTermTaskForm.StartDate?.Date;
        var endDate = LongTermTaskForm.EndDate?.Date;

        if (string.IsNullOrWhiteSpace(trimmedProjectName))
        {
            EditorValidationMessage = "请输入项目名称。";
            return false;
        }

        if (trimmedProjectName.Length > 40)
        {
            EditorValidationMessage = "项目名称建议控制在 40 个字符以内。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(trimmedTitle))
        {
            EditorValidationMessage = "长期任务标题不能为空。";
            return false;
        }

        if (startDate is null || endDate is null)
        {
            EditorValidationMessage = "开始时间和结束时间都不能为空。";
            return false;
        }

        if (endDate.Value < startDate.Value)
        {
            EditorValidationMessage = "结束时间不能早于开始时间。";
            return false;
        }

        var project = ResolveOrCreateProject(trimmedProjectName);
        var now = DateTime.UtcNow;
        var normalizedStatus = LongTermTaskForm.Status == LongTermTaskStatus.Paused
            ? LongTermTaskStatus.Active
            : LongTermTaskForm.Status;

        if (LongTermTaskForm.EditingId is null)
        {
            _data.LongTermTasks.Add(new LongTermTaskItem
            {
                ProjectId = project.Id,
                Title = trimmedTitle,
                Status = normalizedStatus,
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                Notes = CleanNotes(LongTermTaskForm.Notes),
                Progress = LongTermTaskForm.Progress,
                CompletedAt = normalizedStatus == LongTermTaskStatus.Completed ? now : null,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            var existing = _data.LongTermTasks.First(item => item.Id == LongTermTaskForm.EditingId);
            existing.ProjectId = project.Id;
            existing.Title = trimmedTitle;
            existing.Status = normalizedStatus;
            existing.CompletedAt = normalizedStatus == LongTermTaskStatus.Completed
                ? existing.CompletedAt ?? now
                : null;
            existing.StartDate = startDate.Value;
            existing.EndDate = endDate.Value;
            existing.Notes = CleanNotes(LongTermTaskForm.Notes);
            existing.Progress = LongTermTaskForm.Progress;
            existing.UpdatedAt = now;
        }

        Persist(LongTermTaskForm.EditingId is null ? "已创建长期任务。" : "已更新长期任务。");
        CancelEditor();
        return true;
    }

    private void Persist(string message)
    {
        PersistCore(message);
    }

    private void PersistWithoutClosing(string message)
    {
        PersistCore(message);
        RefreshProjectCatalogRows();
        RaiseEditorStateChanged();
    }

    private void PersistCore(string message)
    {
        AutoUpdateProjectStatus();
        RefreshProjectNameOptions();
        RefreshProjectCatalogRows();
        _store.Save(_data);
        RefreshSections();
        StatusMessage = message;
    }

    private void RefreshSections()
    {
        var localToday = DateTime.Today;
        var currentWeekStart = DateHelper.GetStartOfWeek(localToday);
        var visibleProjects = _data.Projects.ToDictionary(item => item.Id);

        var activeWeeklyTasks = _data.WeeklyTasks
            .Where(task => ShowArchivedTasks ? task.Archived : !task.Archived)
            .Where(task => visibleProjects.ContainsKey(task.ProjectId))
            .Where(WeeklyTaskMatches)
            .ToList();

        var activeLongTermTasks = _data.LongTermTasks
            .Where(task => ShowArchivedTasks ? task.Archived : !task.Archived)
            .Where(task => visibleProjects.ContainsKey(task.ProjectId))
            .Where(LongTermTaskMatches)
            .ToList();

        WeeklySections = activeWeeklyTasks
            .Where(task => task.WeekStartDate.Date == currentWeekStart.Date)
            .Where(task => WeeklyRolloverService.IsWeeklyInCurrentView(task, localToday))
            .GroupBy(task => task.ProjectId)
            .Select(group => BuildWeeklySection(visibleProjects[group.Key], group))
            .OrderBy(section => section.ProjectName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        WeeklyHistoryGroups = [];

        LongTermSections = activeLongTermTasks
            .Where(task => WeeklyRolloverService.IsLongTermInCurrentView(task, localToday))
            .GroupBy(task => task.ProjectId)
            .Select(group => BuildLongTermSection(visibleProjects[group.Key], group))
            .OrderBy(section => section.ProjectName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var historicalWeeklyTasks = activeWeeklyTasks
            .Where(task => task.Status == WeeklyTaskStatus.Done)
            .Where(task => !WeeklyRolloverService.IsWeeklyInCurrentView(task, localToday))
            .ToList();
        var historicalLongTermTasks = activeLongTermTasks
            .Where(task => task.Status == LongTermTaskStatus.Completed)
            .Where(task => !WeeklyRolloverService.IsLongTermInCurrentView(task, localToday))
            .ToList();

        HistoryWeeks = SelectedHistoryKind == HistoryTaskKind.Weekly
            ? historicalWeeklyTasks
                .GroupBy(task => task.WeekStartDate.Date)
                .OrderByDescending(group => group.Key)
                .Select(group => BuildHistoryWeekSummary(
                    group.Key,
                    group.Count(),
                    group.Select(task => task.ProjectId).Distinct().Count()))
                .ToList()
            : historicalLongTermTasks
                .GroupBy(WeeklyRolloverService.GetLongTermHistoryWeek)
                .OrderByDescending(group => group.Key)
                .Select(group => BuildHistoryWeekSummary(
                    group.Key,
                    group.Count(),
                    group.Select(task => task.ProjectId).Distinct().Count()))
                .ToList();

        if (SelectedHistoryWeekStart is DateTime selectedWeek)
        {
            HistoryDetailSections = SelectedHistoryKind == HistoryTaskKind.Weekly
                ? historicalWeeklyTasks
                    .Where(task => task.WeekStartDate.Date == selectedWeek.Date)
                    .GroupBy(task => task.ProjectId)
                    .Select(group => BuildWeeklySection(visibleProjects[group.Key], group, showAddTaskAction: false))
                    .OrderBy(section => section.ProjectName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()
                : historicalLongTermTasks
                    .Where(task => WeeklyRolloverService.GetLongTermHistoryWeek(task) == selectedWeek.Date)
                    .GroupBy(task => task.ProjectId)
                    .Select(group => BuildLongTermSection(visibleProjects[group.Key], group, showAddTaskAction: false))
                    .OrderBy(section => section.ProjectName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
        }
        else
        {
            HistoryDetailSections = [];
        }

        OnPropertyChanged(nameof(ShowWeeklyEmptyState));
        OnPropertyChanged(nameof(ShowLongTermEmptyState));
        OnPropertyChanged(nameof(ShowHistoryEmptyState));
        OnPropertyChanged(nameof(HeaderSubtitle));
        OnPropertyChanged(nameof(HistoryToggleText));
        OnPropertyChanged(nameof(SelectedHistorySummary));
    }

    private static HistoryWeekSummaryViewModel BuildHistoryWeekSummary(
        DateTime weekStart,
        int taskCount,
        int projectCount)
    {
        return new HistoryWeekSummaryViewModel
        {
            WeekStartDate = weekStart,
            WeekTitle = $"{weekStart:yyyy-MM-dd} — {weekStart.AddDays(6):yyyy-MM-dd}",
            SummaryText = $"完成 {taskCount} 项 · {projectCount} 个项目",
            TaskCount = taskCount,
            ProjectCount = projectCount,
            Progress = 100
        };
    }

    private void RaiseHistoryStateChanged()
    {
        OnPropertyChanged(nameof(IsCurrentWeeklyView));
        OnPropertyChanged(nameof(ShowHistoryView));
        OnPropertyChanged(nameof(ShowHistoryWeekList));
        OnPropertyChanged(nameof(ShowHistoryWeekDetail));
        OnPropertyChanged(nameof(IsWeeklyHistoryKind));
        OnPropertyChanged(nameof(IsLongTermHistoryKind));
        OnPropertyChanged(nameof(ShowWeeklyEmptyState));
        OnPropertyChanged(nameof(ShowLongTermEmptyState));
        OnPropertyChanged(nameof(ShowHistoryEmptyState));
        OnPropertyChanged(nameof(SelectedHistoryTitle));
        OnPropertyChanged(nameof(SelectedHistorySummary));
        OnPropertyChanged(nameof(HeaderSubtitle));
        OnPropertyChanged(nameof(HistoryToggleText));
    }

    private ProjectSectionViewModel BuildWeeklySection(
        ProjectItem project,
        IEnumerable<WeeklyTaskItem> tasks,
        bool showAddTaskAction = true)
    {
        var projectTone = GetProjectTone(project.Status);
        var rows = tasks
            .OrderBy(task => task.CreatedAt)
            .Select(task =>
            {
                var tone = GetWeeklyTone(task.Status);
                var latestAnnotation = task.Annotations
                    .OrderByDescending(annotation => annotation.CreatedAt)
                    .FirstOrDefault();
                return new WeeklyTaskRowViewModel
                {
                    Id = task.Id,
                    Title = task.Title,
                    Status = task.Status,
                    StatusText = GetWeeklyStatusText(task.Status),
                    StatusPillBackground = tone.Background,
                    StatusPillForeground = tone.Foreground,
                    MetaText = string.Empty,
                    NotesPreview = task.Notes ?? string.Empty,
                    Progress = task.Progress,
                    IsArchived = task.Archived,
                    AnnotationCount = task.Annotations.Count,
                    LatestAnnotationText = latestAnnotation?.Content ?? string.Empty,
                    LatestAnnotationTimeText = latestAnnotation is null
                        ? string.Empty
                        : TaskAnnotationRules.FormatLocalTime(latestAnnotation.CreatedAt.ToLocalTime(), DateTime.Now)
                };
            })
            .ToList();

        var sectionProgress = rows.Count == 0 ? 0 : (int)Math.Round(rows.Average(row => row.Progress));

        return new ProjectSectionViewModel
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            ProjectMeta = $"{rows.Count} 项本周任务",
            StatusText = GetProjectStatusText(project.Status),
            StatusBadgeBackground = projectTone.Background,
            StatusBadgeForeground = projectTone.Foreground,
            AccentBrush = projectTone.Accent,
            Progress = sectionProgress,
            WeeklyTasks = rows,
            ShowAddTaskAction = showAddTaskAction
        };
    }

    private ProjectSectionViewModel BuildLongTermSection(
        ProjectItem project,
        IEnumerable<LongTermTaskItem> tasks,
        bool showAddTaskAction = true)
    {
        var projectTone = GetProjectTone(project.Status);
        var rows = tasks
            .OrderBy(task => task.CreatedAt)
            .Select(task =>
            {
                var tone = GetLongTermTone(task.Status);
                var latestAnnotation = task.Annotations
                    .OrderByDescending(annotation => annotation.CreatedAt)
                    .FirstOrDefault();
                return new LongTermTaskRowViewModel
                {
                    Id = task.Id,
                    Title = task.Title,
                    Status = task.Status,
                    StatusText = GetLongTermStatusText(task.Status),
                    StatusPillBackground = tone.Background,
                    StatusPillForeground = tone.Foreground,
                    MetaText = BuildLongTermMetaText(task),
                    NotesPreview = task.Notes ?? string.Empty,
                    Progress = task.Progress,
                    IsArchived = task.Archived,
                    AnnotationCount = task.Annotations.Count,
                    LatestAnnotationText = latestAnnotation?.Content ?? string.Empty,
                    LatestAnnotationTimeText = latestAnnotation is null
                        ? string.Empty
                        : TaskAnnotationRules.FormatLocalTime(latestAnnotation.CreatedAt.ToLocalTime(), DateTime.Now)
                };
            })
            .ToList();

        var sectionProgress = rows.Count == 0 ? 0 : (int)Math.Round(rows.Average(row => row.Progress));

        return new ProjectSectionViewModel
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            ProjectMeta = $"{rows.Count} 项长期任务",
            StatusText = GetProjectStatusText(project.Status),
            StatusBadgeBackground = projectTone.Background,
            StatusBadgeForeground = projectTone.Foreground,
            AccentBrush = projectTone.Accent,
            Progress = sectionProgress,
            LongTermTasks = rows,
            ShowAddTaskAction = showAddTaskAction
        };
    }

    private bool AutoUpdateProjectStatus()
    {
        var changed = false;

        foreach (var project in _data.Projects)
        {
            var weeklyTasks = _data.WeeklyTasks
                .Where(task => task.ProjectId == project.Id && !task.Archived)
                .ToList();
            var longTermTasks = _data.LongTermTasks
                .Where(task => task.ProjectId == project.Id && !task.Archived)
                .ToList();

            var totalTaskCount = weeklyTasks.Count + longTermTasks.Count;
            var allWeeklyDone = weeklyTasks.All(task => task.Status == WeeklyTaskStatus.Done);
            var allLongTermDone = longTermTasks.All(task => task.Status == LongTermTaskStatus.Completed);
            var newStatus = totalTaskCount > 0 && allWeeklyDone && allLongTermDone
                ? ProjectStatus.Completed
                : ProjectStatus.Active;

            if (project.Status != newStatus)
            {
                project.Status = newStatus;
                project.UpdatedAt = DateTime.UtcNow;
                changed = true;
            }
        }

        return changed;
    }

    private void RefreshProjectCatalogRows()
    {
        ProjectCatalogRows = _data.Projects
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(project => new ProjectCatalogRowViewModel
            {
                Id = project.Id,
                Name = project.Name,
                WeeklyTaskCount = _data.WeeklyTasks.Count(task => task.ProjectId == project.Id && !task.Archived),
                LongTermTaskCount = _data.LongTermTasks.Count(task => task.ProjectId == project.Id && !task.Archived)
            })
            .ToList();
    }

    private void RefreshProjectNameOptions()
    {
        _projectNameOptions = _data.Projects
            .Select(item => item.Name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        OnPropertyChanged(nameof(WeeklyProjectNameOptions));
        OnPropertyChanged(nameof(LongTermProjectNameOptions));
    }

    private bool WeeklyTaskMatches(WeeklyTaskItem task)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        var projectName = _data.Projects.FirstOrDefault(project => project.Id == task.ProjectId)?.Name ?? string.Empty;
        return task.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
            || projectName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
            || (task.Notes?.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ?? false);
    }

    private bool LongTermTaskMatches(LongTermTaskItem task)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        var projectName = _data.Projects.FirstOrDefault(project => project.Id == task.ProjectId)?.Name ?? string.Empty;
        return task.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
            || projectName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
            || (task.Notes?.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ?? false);
    }

    private void OpenAnnotations(TaskKind taskKind, string taskId, string taskTitle)
    {
        ResetValidation();
        AnnotationEditor.TaskKind = taskKind;
        AnnotationEditor.TaskId = taskId;
        AnnotationEditor.TaskTitle = taskTitle;
        AnnotationEditor.Content = string.Empty;
        AnnotationEditor.EditingAnnotationId = null;
        AnnotationEditor.PendingDeleteAnnotationId = null;
        _editorKind = EditorKind.Annotations;
        RefreshAnnotationRows();
        RaiseEditorStateChanged();
    }

    private List<TaskAnnotation>? GetAnnotationTarget(out string taskTitle)
    {
        if (AnnotationEditor.TaskKind == TaskKind.Weekly)
        {
            var task = _data.WeeklyTasks.FirstOrDefault(item => item.Id == AnnotationEditor.TaskId);
            taskTitle = task?.Title ?? string.Empty;
            return task?.Annotations;
        }

        var longTermTask = _data.LongTermTasks.FirstOrDefault(item => item.Id == AnnotationEditor.TaskId);
        taskTitle = longTermTask?.Title ?? string.Empty;
        return longTermTask?.Annotations;
    }

    private void RefreshAnnotationRows()
    {
        var annotations = GetAnnotationTarget(out var taskTitle);
        if (annotations is null)
        {
            AnnotationRows = [];
            return;
        }

        AnnotationEditor.TaskTitle = taskTitle;
        var localNow = DateTime.Now;
        AnnotationRows = annotations
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new AnnotationRowViewModel
            {
                Id = item.Id,
                Content = item.Content,
                TimeText = TaskAnnotationRules.FormatLocalTime(item.CreatedAt.ToLocalTime(), localNow),
                IsEdited = item.UpdatedAt > item.CreatedAt,
                IsDeletePending = AnnotationEditor.PendingDeleteAnnotationId == item.Id
            })
            .ToList();
    }

    private void ResetAnnotationEditor()
    {
        AnnotationEditor.TaskId = string.Empty;
        AnnotationEditor.TaskTitle = string.Empty;
        AnnotationEditor.Content = string.Empty;
        AnnotationEditor.EditingAnnotationId = null;
        AnnotationEditor.PendingDeleteAnnotationId = null;
        AnnotationRows = [];
    }

    private void ResetValidation()
    {
        EditorValidationMessage = string.Empty;
    }

    private bool CloseSettingsEditor()
    {
        CancelEditor();
        return true;
    }

    private void ResetSettingsProjectEditor()
    {
        SettingsForm.EditingProjectId = null;
        SettingsForm.ProjectNameInput = string.Empty;
        OnPropertyChanged(nameof(IsEditingProjectInSettings));
        OnPropertyChanged(nameof(SettingsProjectActionText));
    }

    private void RaiseEditorStateChanged()
    {
        OnPropertyChanged(nameof(IsEditorOpen));
        OnPropertyChanged(nameof(IsWeeklyTaskEditorOpen));
        OnPropertyChanged(nameof(IsLongTermTaskEditorOpen));
        OnPropertyChanged(nameof(IsSettingsOpen));
        OnPropertyChanged(nameof(IsAnnotationEditorOpen));
        OnPropertyChanged(nameof(ShowAnnotationEmptyState));
        OnPropertyChanged(nameof(IsEditingProjectInSettings));
        OnPropertyChanged(nameof(ShowDualFooterButtons));
        OnPropertyChanged(nameof(ShowGenericEditorFooter));
        OnPropertyChanged(nameof(SettingsProjectActionText));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(EditorSubtitle));
        OnPropertyChanged(nameof(EditorConfirmButtonText));
        OnPropertyChanged(nameof(AnnotationSaveButtonText));
    }

    private static string? CleanNotes(string? notes)
    {
        var trimmed = notes?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string BuildLongTermMetaText(LongTermTaskItem task)
    {
        return $"开始：{task.StartDate:yyyy-MM-dd} · 结束：{task.EndDate:yyyy-MM-dd}";
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
            LongTermTaskStatus.Paused => "暂停中",
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
            LongTermTaskStatus.Active => new StatusTone("#E9F8F3", "#0D7D5A", "#10A06D"),
            LongTermTaskStatus.Paused => new StatusTone("#E9F8F3", "#0D7D5A", "#10A06D"),
            LongTermTaskStatus.Completed => new StatusTone("#E7F7EE", "#1E7A4A", "#2DAA68"),
            _ => new StatusTone("#EEF2F7", "#4F6277", "#A5B4C4")
        };
    }

    private readonly record struct StatusTone(string Background, string Foreground, string Accent);

    private enum EditorKind
    {
        None,
        WeeklyTask,
        LongTermTask,
        Settings,
        Annotations
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

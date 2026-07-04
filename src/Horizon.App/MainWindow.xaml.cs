using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Horizon.App.Models;
using Horizon.App.Services;
using Horizon.App.ViewModels;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using RadioButton = System.Windows.Controls.RadioButton;

namespace Horizon.App;

public partial class MainWindow : Window
{
    private const double ExpandedPanelInset = 6;
    private const int HoverAnimationMilliseconds = 120;
    private const int PanelAnimationMilliseconds = 220;
    private static readonly TimeSpan HoverLeaveDelay = TimeSpan.FromMilliseconds(250);

    private readonly MainViewModel _viewModel;
    private readonly DispatcherTimer _hoverLeaveTimer;
    private readonly DispatcherTimer _dateCheckTimer;
    private TrayIconService? _trayIconService;
    private PanelDisplayState _panelState = PanelDisplayState.CollapsedSliver;
    private bool _isAnimating;
    private int _animationVersion;
    private bool _isUpdatingStatus;
    private bool _isDraggingHandle;
    private bool _dragMoved;
    private double _dragStartScreenY;
    private double _dragStartTop;
    private double _collapsedButtonTop;
    private DateTime _lastObservedDate;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel(new HorizonDataStore(), new WindowsStartupService());
        DataContext = _viewModel;

        _hoverLeaveTimer = new DispatcherTimer { Interval = HoverLeaveDelay };
        _hoverLeaveTimer.Tick += HoverLeaveTimer_OnTick;

        _dateCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _dateCheckTimer.Tick += DateCheckTimer_OnTick;
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        _collapsedButtonTop = PanelLayout.CoerceHandleTop(
            SystemParameters.WorkArea,
            _viewModel.GetCollapsedButtonTop());
        ConfigureWindow();
        SetPanelState(PanelDisplayState.CollapsedSliver, animated: false);
        _viewModel.ReconcileStartupRegistration();
        _trayIconService ??= new TrayIconService(OpenFromTray, ExitFromTray);
        _lastObservedDate = DateTime.Today;
        _dateCheckTimer.Start();
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        _hoverLeaveTimer.Stop();
        _dateCheckTimer.Stop();
        _trayIconService?.Dispose();
        _trayIconService = null;
    }

    private void OpenFromTray()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!IsVisible)
            {
                Show();
            }

            SetPanelState(PanelDisplayState.ExpandedPanel);
            Activate();
            Focus();
        }));
    }

    private void ExitFromTray()
    {
        Dispatcher.BeginInvoke(new Action(() => Application.Current.Shutdown()));
    }

    private void DateCheckTimer_OnTick(object? sender, EventArgs e)
    {
        var today = DateTime.Today;
        if (today == _lastObservedDate)
        {
            return;
        }

        _lastObservedDate = today;
        _viewModel.ReconcileForDate(today, DateTime.UtcNow);
    }

    private void MainWindow_OnLocationOrSizeChanged(object sender, EventArgs e)
    {
        if (_isAnimating)
        {
            return;
        }

        _collapsedButtonTop = PanelLayout.CoerceHandleTop(
            SystemParameters.WorkArea,
            _collapsedButtonTop);
        ApplyWindowState(animated: false, animationMilliseconds: 0);
    }

    private void MainWindow_OnDeactivated(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(
            DispatcherPriority.ApplicationIdle,
            new Action(CollapsePanelAfterDeactivation));
    }

    private void CollapsePanelAfterDeactivation()
    {
        var isApplicationMenuOpen = QuickAddButton.ContextMenu?.IsOpen == true;
        if (!PanelInteractionRules.ShouldCollapseAfterDeactivation(
                _panelState,
                IsActive,
                isApplicationMenuOpen,
                _viewModel.IsPinned))
        {
            return;
        }

        SetPanelState(PanelDisplayState.CollapsedSliver);
    }

    private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _viewModel.IsEditorOpen)
        {
            _viewModel.CancelEditor();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && _panelState == PanelDisplayState.ExpandedPanel)
        {
            CollapsePanel();
            e.Handled = true;
        }
    }

    private void QuickAddButton_OnClick(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        menu.Items.Add(BuildMenuItem("新建本周任务", (_, _) => _viewModel.OpenCreateWeeklyTask()));
        menu.Items.Add(BuildMenuItem("新建长期任务", (_, _) => _viewModel.OpenCreateLongTermTask()));

        QuickAddButton.ContextMenu = menu;
        menu.IsOpen = true;
    }

    private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.OpenSettings();
        ExpandPanel();
    }

    private void PinButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.TogglePinned();
    }

    private void StartWithWindowsButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleStartWithWindows();
    }

    private void ArchiveToggleButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleArchivedTasks();
        ExpandPanel();
    }

    private void HistoryToggleButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleHistory();
        ExpandPanel();
    }

    private void SelectWeeklyHistoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectWeeklyHistory();
    }

    private void SelectLongTermHistoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectLongTermHistory();
    }

    private void OpenHistoryWeekButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: DateTime weekStart })
        {
            _viewModel.OpenHistoryWeek(weekStart);
        }
    }

    private void BackToHistoryWeeksButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.BackToHistoryWeeks();
    }

    private void CreateWeeklyTaskButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.OpenCreateWeeklyTask();
        ExpandPanel();
    }

    private void CreateLongTermTaskButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.OpenCreateLongTermTask();
        ExpandPanel();
    }

    private void AddWeeklyTaskForProjectButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var projectId))
        {
            _viewModel.OpenCreateWeeklyTask();
            _viewModel.WeeklyTaskForm.ProjectName = FindProjectName(projectId);
            ExpandPanel();
        }
    }

    private void AddLongTermTaskForProjectButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var projectId))
        {
            _viewModel.OpenCreateLongTermTask();
            _viewModel.LongTermTaskForm.ProjectName = FindProjectName(projectId);
            ExpandPanel();
        }
    }

    private void EditWeeklyTaskButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var taskId))
        {
            _viewModel.OpenEditWeeklyTask(taskId);
            ExpandPanel();
        }
    }

    private void EditLongTermTaskButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var taskId))
        {
            _viewModel.OpenEditLongTermTask(taskId);
            ExpandPanel();
        }
    }

    private void OpenWeeklyAnnotationsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var taskId))
        {
            _viewModel.OpenWeeklyAnnotations(taskId);
            ExpandPanel();
        }
    }

    private void OpenLongTermAnnotationsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var taskId))
        {
            _viewModel.OpenLongTermAnnotations(taskId);
            ExpandPanel();
        }
    }

    private void ArchiveWeeklyTaskButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var taskId))
        {
            _viewModel.ToggleWeeklyTaskArchive(taskId);
            ExpandPanel();
        }
    }

    private void ArchiveLongTermTaskButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var taskId))
        {
            _viewModel.ToggleLongTermTaskArchive(taskId);
            ExpandPanel();
        }
    }

    private void WeeklyTodoButton_OnChecked(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingStatus)
        {
            return;
        }

        _isUpdatingStatus = true;
        UpdateWeeklyStatusFromButton(sender, WeeklyTaskStatus.Todo);
        Dispatcher.BeginInvoke(() => _isUpdatingStatus = false);
    }

    private void WeeklyInProgressButton_OnChecked(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingStatus)
        {
            return;
        }

        _isUpdatingStatus = true;
        UpdateWeeklyStatusFromButton(sender, WeeklyTaskStatus.InProgress);
        Dispatcher.BeginInvoke(() => _isUpdatingStatus = false);
    }

    private void WeeklyDoneButton_OnChecked(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingStatus)
        {
            return;
        }

        _isUpdatingStatus = true;
        UpdateWeeklyStatusFromButton(sender, WeeklyTaskStatus.Done);
        Dispatcher.BeginInvoke(() => _isUpdatingStatus = false);
    }

    private void LongTermPlannedButton_OnChecked(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingStatus)
        {
            return;
        }

        _isUpdatingStatus = true;
        UpdateLongTermStatusFromButton(sender, LongTermTaskStatus.Planned);
        Dispatcher.BeginInvoke(() => _isUpdatingStatus = false);
    }

    private void LongTermActiveButton_OnChecked(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingStatus)
        {
            return;
        }

        _isUpdatingStatus = true;
        UpdateLongTermStatusFromButton(sender, LongTermTaskStatus.Active);
        Dispatcher.BeginInvoke(() => _isUpdatingStatus = false);
    }

    private void LongTermCompletedButton_OnChecked(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingStatus)
        {
            return;
        }

        _isUpdatingStatus = true;
        UpdateLongTermStatusFromButton(sender, LongTermTaskStatus.Completed);
        Dispatcher.BeginInvoke(() => _isUpdatingStatus = false);
    }

    private void CancelEditorButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.CancelEditor();
    }

    private void SaveEditorButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SaveEditor())
        {
            ExpandPanel();
        }
    }

    private void SaveAnnotationButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveAnnotation();
    }

    private void CancelAnnotationEditButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.CancelAnnotationEdit();
    }

    private void EditAnnotationButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var annotationId))
        {
            _viewModel.StartEditAnnotation(annotationId);
        }
    }

    private void RequestDeleteAnnotationButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var annotationId))
        {
            _viewModel.RequestDeleteAnnotation(annotationId);
        }
    }

    private void ConfirmDeleteAnnotationButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var annotationId))
        {
            _viewModel.ConfirmDeleteAnnotation(annotationId);
        }
    }

    private void CancelDeleteAnnotationButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.CancelDeleteAnnotation();
    }

    private void SaveProjectCatalogButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveProjectInSettings();
    }

    private void EditProjectCatalogButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var projectId))
        {
            _viewModel.StartEditProjectInSettings(projectId);
        }
    }

    private void CancelProjectCatalogEditButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.StartCreateProjectInSettings();
    }

    private void DeleteProjectCatalogButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var projectId))
        {
            _viewModel.DeleteProjectInSettings(projectId);
        }
    }

    private void CollapsedSliverShell_OnMouseEnter(object sender, MouseEventArgs e)
    {
        CancelHoverLeaveCountdown();
        SetPanelState(PanelDisplayState.HoverHandle);
    }

    private void HoverHandleShell_OnMouseEnter(object sender, MouseEventArgs e)
    {
        CancelHoverLeaveCountdown();
    }

    private void HoverHandleShell_OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (!_isDraggingHandle)
        {
            StartHoverLeaveCountdown();
        }
    }

    private void HoverHandleShell_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_panelState != PanelDisplayState.HoverHandle ||
            !TryGetCursorPosition(out var cursor))
        {
            return;
        }

        CancelHoverLeaveCountdown();
        _isDraggingHandle = true;
        _dragMoved = false;
        _dragStartTop = _collapsedButtonTop;
        _dragStartScreenY = cursor.Y;
        HoverHandleShell.CaptureMouse();
        e.Handled = true;
    }

    private void HoverHandleShell_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingHandle ||
            e.LeftButton != MouseButtonState.Pressed ||
            !TryGetCursorPosition(out var cursor))
        {
            return;
        }

        var delta = cursor.Y - _dragStartScreenY;
        if (!_dragMoved && !PanelLayout.IsDragDelta(delta))
        {
            return;
        }

        _dragMoved = true;
        _collapsedButtonTop = PanelLayout.CoerceHandleTop(
            SystemParameters.WorkArea,
            _dragStartTop + delta);
        ApplyWindowState(animated: false, animationMilliseconds: 0);
    }

    private void HoverHandleShell_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingHandle)
        {
            return;
        }

        var wasDrag = _dragMoved;
        _isDraggingHandle = false;
        HoverHandleShell.ReleaseMouseCapture();

        if (wasDrag)
        {
            _viewModel.UpdateCollapsedButtonTop(_collapsedButtonTop);
            if (!HoverHandleShell.IsMouseOver)
            {
                StartHoverLeaveCountdown();
            }
        }
        else
        {
            ExpandPanel();
        }

        e.Handled = true;
    }

    private void HoverHandleShell_OnLostMouseCapture(object sender, MouseEventArgs e)
    {
        if (!_isDraggingHandle)
        {
            return;
        }

        _isDraggingHandle = false;
        if (_dragMoved)
        {
            _viewModel.UpdateCollapsedButtonTop(_collapsedButtonTop);
        }

        if (_panelState == PanelDisplayState.HoverHandle && !HoverHandleShell.IsMouseOver)
        {
            StartHoverLeaveCountdown();
        }
    }

    private string FindProjectName(string projectId)
    {
        var section = _viewModel.WeeklySections.Concat(_viewModel.LongTermSections)
            .FirstOrDefault(item => item.ProjectId == projectId);
        return section?.ProjectName ?? string.Empty;
    }

    private void ConfigureWindow()
    {
        Topmost = true;
        ApplyWindowState(animated: false, animationMilliseconds: 0);
    }

    private void ExpandPanel(bool animated = true)
    {
        CancelHoverLeaveCountdown();
        SetPanelState(PanelDisplayState.ExpandedPanel, animated);
    }

    private void CollapsePanel(bool animated = true)
    {
        if (_panelState != PanelDisplayState.ExpandedPanel || _viewModel.IsEditorOpen)
        {
            return;
        }

        SetPanelState(PanelDisplayState.CollapsedSliver, animated);
    }

    private void SetPanelState(PanelDisplayState targetState, bool animated = true)
    {
        var previousState = _panelState;
        if (previousState == targetState)
        {
            if (!animated)
            {
                ApplyWindowState(animated: false, animationMilliseconds: 0);
            }

            return;
        }

        _panelState = targetState;
        var animationMilliseconds =
            previousState == PanelDisplayState.ExpandedPanel ||
            targetState == PanelDisplayState.ExpandedPanel
                ? PanelAnimationMilliseconds
                : HoverAnimationMilliseconds;
        ApplyWindowState(animated, animationMilliseconds);
    }

    private void StartHoverLeaveCountdown()
    {
        if (_panelState != PanelDisplayState.HoverHandle || _isDraggingHandle)
        {
            return;
        }

        _hoverLeaveTimer.Stop();
        _hoverLeaveTimer.Start();
    }

    private void CancelHoverLeaveCountdown()
    {
        _hoverLeaveTimer.Stop();
    }

    private void HoverLeaveTimer_OnTick(object? sender, EventArgs e)
    {
        CancelHoverLeaveCountdown();
        if (_panelState == PanelDisplayState.HoverHandle &&
            !_isDraggingHandle &&
            !HoverHandleShell.IsMouseOver)
        {
            SetPanelState(PanelDisplayState.CollapsedSliver);
        }
    }

    private void ApplyWindowState(bool animated, int animationMilliseconds)
    {
        var targetState = _panelState;
        var bounds = PanelLayout.GetBounds(
            targetState,
            SystemParameters.WorkArea,
            _collapsedButtonTop);
        var targetOpacity = targetState == PanelDisplayState.ExpandedPanel ? 1.0 : 0.98;

        UpdateShellVisibility(targetState);

        if (!animated)
        {
            _animationVersion++;
            _isAnimating = false;
            ApplyBounds(bounds);
            Opacity = targetOpacity;
            return;
        }

        _isAnimating = true;
        AnimateWindow(bounds, targetOpacity, animationMilliseconds, targetState);
    }

    private void ApplyBounds(Rect bounds)
    {
        BeginAnimation(WidthProperty, null);
        BeginAnimation(HeightProperty, null);
        BeginAnimation(LeftProperty, null);
        BeginAnimation(TopProperty, null);
        BeginAnimation(OpacityProperty, null);
        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;
    }

    private void AnimateWindow(
        Rect bounds,
        double targetOpacity,
        int animationMilliseconds,
        PanelDisplayState targetState)
    {
        var version = ++_animationVersion;
        var duration = new Duration(TimeSpan.FromMilliseconds(animationMilliseconds));
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

        BeginAnimation(WidthProperty, CreateAnimation(bounds.Width, duration, easing));
        BeginAnimation(HeightProperty, CreateAnimation(bounds.Height, duration, easing));
        BeginAnimation(LeftProperty, CreateAnimation(bounds.Left, duration, easing));
        BeginAnimation(TopProperty, CreateAnimation(bounds.Top, duration, easing));

        var opacityAnimation = CreateAnimation(targetOpacity, duration, easing);
        opacityAnimation.Completed += (_, _) =>
        {
            if (version != _animationVersion || targetState != _panelState)
            {
                return;
            }

            _isAnimating = false;
            ApplyBounds(bounds);
            Opacity = targetOpacity;
            UpdateShellVisibility(targetState);
        };

        BeginAnimation(OpacityProperty, opacityAnimation);
    }

    private static DoubleAnimation CreateAnimation(double target, Duration duration, IEasingFunction easing)
    {
        return new DoubleAnimation(target, duration) { EasingFunction = easing };
    }

    private void UpdateShellVisibility(PanelDisplayState state)
    {
        ExpandedPanelShell.Visibility = state == PanelDisplayState.ExpandedPanel
            ? Visibility.Visible
            : Visibility.Collapsed;
        CollapsedSliverShell.Visibility = state == PanelDisplayState.CollapsedSliver
            ? Visibility.Visible
            : Visibility.Collapsed;
        HoverHandleShell.Visibility = state == PanelDisplayState.HoverHandle
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (state == PanelDisplayState.ExpandedPanel)
        {
            ExpandedPanelShell.Margin = new Thickness(ExpandedPanelInset);
        }
    }

    private void UpdateWeeklyStatusFromButton(object sender, WeeklyTaskStatus status)
    {
        if (sender is RadioButton { IsChecked: true } && TryGetTag(sender, out var taskId))
        {
            _viewModel.UpdateWeeklyTaskStatus(taskId, status);
        }
    }

    private void UpdateLongTermStatusFromButton(object sender, LongTermTaskStatus status)
    {
        if (sender is RadioButton { IsChecked: true } && TryGetTag(sender, out var taskId))
        {
            _viewModel.UpdateLongTermTaskStatus(taskId, status);
        }
    }

    private static MenuItem BuildMenuItem(string header, RoutedEventHandler onClick)
    {
        var item = new MenuItem { Header = header };
        item.Click += onClick;
        return item;
    }

    private static bool TryGetTag(object sender, out string id)
    {
        id = string.Empty;

        if (sender is FrameworkElement element && element.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
        {
            id = tag;
            return true;
        }

        return false;
    }

    private static bool TryGetCursorPosition(out Point point)
    {
        point = default;

        if (!GetCursorPos(out var nativePoint))
        {
            return false;
        }

        point = new Point(nativePoint.X, nativePoint.Y);
        return true;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}

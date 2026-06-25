using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Horizon.App.Models;
using Horizon.App.Services;
using Horizon.App.ViewModels;

namespace Horizon.App;

public partial class MainWindow : Window
{
    private const double ExpandedPanelWidth = 338;
    private const double CollapsedSensorWidth = 22;
    private const double CollapsedSensorHeight = 128;
    private const double ScreenPadding = 6;
    private static readonly TimeSpan HideDelay = TimeSpan.FromMilliseconds(220);
    private static readonly TimeSpan PointerPollInterval = TimeSpan.FromMilliseconds(90);

    private readonly DispatcherTimer _hideTimer;
    private readonly DispatcherTimer _pointerTimer;
    private readonly MainViewModel _viewModel;
    private bool _isAnimating;
    private bool _isExpanded;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel(new HorizonDataStore());
        DataContext = _viewModel;

        _hideTimer = new DispatcherTimer { Interval = HideDelay };
        _hideTimer.Tick += HideTimer_OnTick;

        _pointerTimer = new DispatcherTimer { Interval = PointerPollInterval };
        _pointerTimer.Tick += PointerTimer_OnTick;
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        ConfigureWindow();
        CollapsePanel(animated: false);
        _pointerTimer.Start();
    }

    private void MainWindow_OnLocationOrSizeChanged(object sender, EventArgs e)
    {
        if (_isAnimating)
        {
            return;
        }

        ApplyWindowState(animated: false);
    }

    private void MainWindow_OnMouseEnter(object sender, MouseEventArgs e)
    {
        ExpandPanel();
    }

    private void MainWindow_OnMouseLeave(object sender, MouseEventArgs e)
    {
        StartHideCountdown();
    }

    private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _viewModel.IsEditorOpen)
        {
            _viewModel.CancelEditor();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            CollapsePanel();
            e.Handled = true;
        }
    }

    private void QuickAddButton_OnClick(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        menu.Items.Add(BuildMenuItem("新建项目", (_, _) => _viewModel.OpenCreateProject()));
        menu.Items.Add(BuildMenuItem("新建本周任务", (_, _) => _viewModel.OpenCreateWeeklyTask()));
        menu.Items.Add(BuildMenuItem("新建长期任务", (_, _) => _viewModel.OpenCreateLongTermTask()));

        QuickAddButton.ContextMenu = menu;
        menu.IsOpen = true;
    }

    private void ArchiveToggleButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleArchiveFilter();
        ExpandPanel();
    }

    private void HistoryToggleButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleWeeklyHistory();
        ExpandPanel();
    }

    private void CreateProjectButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.OpenCreateProject();
        ExpandPanel();
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

    private void EditProjectButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var projectId))
        {
            _viewModel.OpenEditProject(projectId);
            ExpandPanel();
        }
    }

    private void ToggleProjectArchiveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var projectId))
        {
            _viewModel.ToggleProjectArchive(projectId);
            ExpandPanel();
        }
    }

    private void AddWeeklyTaskForProjectButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var projectId))
        {
            _viewModel.OpenCreateWeeklyTask(projectId);
            ExpandPanel();
        }
    }

    private void AddLongTermTaskForProjectButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var projectId))
        {
            _viewModel.OpenCreateLongTermTask(projectId);
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

    private void WeeklyTodoButton_OnChecked(object sender, RoutedEventArgs e)
    {
        UpdateWeeklyStatusFromButton(sender, WeeklyTaskStatus.Todo);
    }

    private void WeeklyInProgressButton_OnChecked(object sender, RoutedEventArgs e)
    {
        UpdateWeeklyStatusFromButton(sender, WeeklyTaskStatus.InProgress);
    }

    private void WeeklyDoneButton_OnChecked(object sender, RoutedEventArgs e)
    {
        UpdateWeeklyStatusFromButton(sender, WeeklyTaskStatus.Done);
    }

    private void LongTermPlannedButton_OnChecked(object sender, RoutedEventArgs e)
    {
        UpdateLongTermStatusFromButton(sender, LongTermTaskStatus.Planned);
    }

    private void LongTermActiveButton_OnChecked(object sender, RoutedEventArgs e)
    {
        UpdateLongTermStatusFromButton(sender, LongTermTaskStatus.Active);
    }

    private void LongTermPausedButton_OnChecked(object sender, RoutedEventArgs e)
    {
        UpdateLongTermStatusFromButton(sender, LongTermTaskStatus.Paused);
    }

    private void LongTermCompletedButton_OnChecked(object sender, RoutedEventArgs e)
    {
        UpdateLongTermStatusFromButton(sender, LongTermTaskStatus.Completed);
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

    private void ConfigureWindow()
    {
        Topmost = true;
        ApplyWindowState(animated: false);
    }

    private void ExpandPanel(bool animated = true)
    {
        if (_isExpanded)
        {
            CancelPendingHide();
            return;
        }

        _isExpanded = true;
        CancelPendingHide();
        ApplyWindowState(animated);
    }

    private void CollapsePanel(bool animated = true)
    {
        if (!_isExpanded || _viewModel.IsEditorOpen)
        {
            return;
        }

        _isExpanded = false;
        CancelPendingHide();
        ApplyWindowState(animated);
    }

    private void ApplyWindowState(bool animated)
    {
        var bounds = _isExpanded ? GetExpandedBounds() : GetCollapsedBounds();
        var targetOpacity = _isExpanded ? 1.0 : 0.94;

        if (_isExpanded)
        {
            ExpandedPanelShell.Visibility = Visibility.Visible;
            CollapsedSensorShell.Visibility = Visibility.Collapsed;
        }

        if (!animated)
        {
            ApplyBounds(bounds);
            Opacity = targetOpacity;
            UpdateShellVisibility();
            return;
        }

        _isAnimating = true;
        AnimateWindow(bounds, targetOpacity);
    }

    private Rect GetExpandedBounds()
    {
        var workArea = SystemParameters.WorkArea;
        var width = ExpandedPanelWidth;
        var left = workArea.Right - width - ScreenPadding;
        return new Rect(left, workArea.Top, width, workArea.Height);
    }

    private Rect GetCollapsedBounds()
    {
        var workArea = SystemParameters.WorkArea;
        var left = workArea.Right - CollapsedSensorWidth - ScreenPadding;
        var top = workArea.Bottom - CollapsedSensorHeight - ScreenPadding;
        return new Rect(left, top, CollapsedSensorWidth, CollapsedSensorHeight);
    }

    private void ApplyBounds(Rect bounds)
    {
        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;
    }

    private void AnimateWindow(Rect bounds, double targetOpacity)
    {
        var duration = new Duration(TimeSpan.FromMilliseconds(280));
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

        BeginAnimation(WidthProperty, CreateAnimation(bounds.Width, duration, easing));
        BeginAnimation(HeightProperty, CreateAnimation(bounds.Height, duration, easing));
        BeginAnimation(LeftProperty, CreateAnimation(bounds.Left, duration, easing));
        BeginAnimation(TopProperty, CreateAnimation(bounds.Top, duration, easing));

        var opacityAnimation = CreateAnimation(targetOpacity, duration, easing);
        opacityAnimation.Completed += (_, _) =>
        {
            _isAnimating = false;
            ApplyBounds(bounds);
            Opacity = targetOpacity;
            UpdateShellVisibility();
        };

        BeginAnimation(OpacityProperty, opacityAnimation);
    }

    private static DoubleAnimation CreateAnimation(double target, Duration duration, IEasingFunction easing)
    {
        return new DoubleAnimation(target, duration) { EasingFunction = easing };
    }

    private void UpdateShellVisibility()
    {
        ExpandedPanelShell.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        CollapsedSensorShell.Visibility = _isExpanded ? Visibility.Collapsed : Visibility.Visible;
    }

    private void PointerTimer_OnTick(object? sender, EventArgs e)
    {
        if (!TryGetCursorPosition(out var cursor))
        {
            return;
        }

        var collapsedBounds = GetCollapsedBounds();
        var revealZone = new Rect(
            collapsedBounds.Left - 42,
            collapsedBounds.Top - 30,
            collapsedBounds.Width + 72,
            collapsedBounds.Height + 60);

        var activeBounds = new Rect(Left, Top, ActualWidth, ActualHeight);
        if (revealZone.Contains(cursor))
        {
            ExpandPanel();
            return;
        }

        if (_isExpanded && activeBounds.Contains(cursor))
        {
            CancelPendingHide();
            return;
        }

        if (_isExpanded && !_viewModel.IsEditorOpen)
        {
            CollapsePanel();
        }
    }

    private void HideTimer_OnTick(object? sender, EventArgs e)
    {
        CancelPendingHide();

        if (_viewModel.IsEditorOpen)
        {
            return;
        }

        if (TryGetCursorPosition(out var cursor))
        {
            var windowRect = new Rect(Left, Top, ActualWidth, ActualHeight);
            if (windowRect.Contains(cursor))
            {
                return;
            }
        }

        CollapsePanel();
    }

    private void StartHideCountdown()
    {
        if (_viewModel.IsEditorOpen || !_isExpanded)
        {
            return;
        }

        _hideTimer.Stop();
        _hideTimer.Start();
    }

    private void CancelPendingHide()
    {
        _hideTimer.Stop();
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

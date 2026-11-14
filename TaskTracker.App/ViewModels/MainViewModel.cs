using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly TaskService _service = new();
    private readonly AppSettings _settings;
    private List<TaskItemViewModel> _allTasks = new();

    public MainViewModel()
    {
        _settings = SettingsService.Load();
        _isDarkTheme = _settings.DarkTheme;
        App.ApplyTheme(_isDarkTheme);

        _service.EnsureDatabase();
        _selectedDate = DateTime.Today;
        LoadDay();
        RefreshDashboard();
    }

    // ───────────────────────── Day view ─────────────────────────

    public ObservableCollection<TaskItemViewModel> VisibleTasks { get; } = new();

    public static string[] Filters { get; } = { "All", "Pending", "Completed" };
    public static TaskPriority[] Priorities { get; } = Enum.GetValues<TaskPriority>();

    [ObservableProperty] private DateTime _selectedDate;
    [ObservableProperty] private string _selectedFilter = "All";

    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _completedCount;
    [ObservableProperty] private int _pendingCount;
    [ObservableProperty] private double _completionPercent;
    [ObservableProperty] private int _overdueCount;

    public string SelectedDateTitle =>
        SelectedDate.Date == DateTime.Today ? "Today"
        : SelectedDate.Date == DateTime.Today.AddDays(-1) ? "Yesterday"
        : SelectedDate.Date == DateTime.Today.AddDays(1) ? "Tomorrow"
        : SelectedDate.ToString("dddd");

    public string SelectedDateSubtitle => SelectedDate.ToString("dd MMMM yyyy");
    public bool HasNoTasks => VisibleTasks.Count == 0;
    public string EmptyMessage => TotalCount == 0
        ? "No tasks for this day yet. Add one above!"
        : $"No {SelectedFilter.ToLower()} tasks.";
    public bool HasOverdue => OverdueCount > 0;
    public string OverdueMessage => $"You have {OverdueCount} unfinished task(s) from previous days.";

    private DateOnly Day => DateOnly.FromDateTime(SelectedDate);

    partial void OnSelectedDateChanged(DateTime value)
    {
        CancelEdit();
        OnPropertyChanged(nameof(SelectedDateTitle));
        OnPropertyChanged(nameof(SelectedDateSubtitle));
        LoadDay();

        var month = new DateTime(value.Year, value.Month, 1);
        if (DisplayMonth != month) DisplayMonth = month; // rebuilds calendar
        else BuildCalendar();
    }

    partial void OnSelectedFilterChanged(string value) => ApplyFilter();
    partial void OnOverdueCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasOverdue));
        OnPropertyChanged(nameof(OverdueMessage));
    }

    private void LoadDay()
    {
        _allTasks = _service.GetTasksForDate(Day)
            .Select(t => new TaskItemViewModel(t, OnTaskCompletedChanged))
            .ToList();
        ApplyFilter();
        UpdateDayStats();
    }

    private void ApplyFilter()
    {
        VisibleTasks.Clear();
        IEnumerable<TaskItemViewModel> items = SelectedFilter switch
        {
            "Pending" => _allTasks.Where(t => !t.IsCompleted),
            "Completed" => _allTasks.Where(t => t.IsCompleted),
            _ => _allTasks
        };
        foreach (var t in items.OrderBy(t => t.IsCompleted).ThenByDescending(t => t.Priority))
            VisibleTasks.Add(t);
        OnPropertyChanged(nameof(HasNoTasks));
        OnPropertyChanged(nameof(EmptyMessage));
    }

    private void UpdateDayStats()
    {
        TotalCount = _allTasks.Count;
        CompletedCount = _allTasks.Count(t => t.IsCompleted);
        PendingCount = TotalCount - CompletedCount;
        CompletionPercent = TotalCount == 0 ? 0 : Math.Round(CompletedCount * 100.0 / TotalCount, 1);
        OverdueCount = _service.CountOverdue(DateOnly.FromDateTime(DateTime.Today));
        OnPropertyChanged(nameof(EmptyMessage));
    }

    private void OnTaskCompletedChanged(TaskItemViewModel task)
    {
        _service.SetCompleted(task.Id, task.IsCompleted);
        ApplyFilter();
        UpdateDayStats();
        RefreshDashboard();
    }

    [RelayCommand] private void PreviousDay() => SelectedDate = SelectedDate.AddDays(-1);
    [RelayCommand] private void NextDay() => SelectedDate = SelectedDate.AddDays(1);
    [RelayCommand] private void GoToday() => SelectedDate = DateTime.Today;

    [RelayCommand]
    private void MoveOverdueToToday()
    {
        var moved = _service.MoveOverdueTo(DateOnly.FromDateTime(DateTime.Today));
        SelectedDate = DateTime.Today;
        LoadDay();
        RefreshDashboard();
        StatusMessage = $"Moved {moved} task(s) to today.";
    }

    // ───────────────────────── Add / edit form ─────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    private string _newTitle = "";

    [ObservableProperty] private string _newNotes = "";
    [ObservableProperty] private TaskPriority _newPriority = TaskPriority.Medium;
    [ObservableProperty] private TaskItemViewModel? _editingTask;
    [ObservableProperty] private string _statusMessage = "";

    public bool IsEditing => EditingTask is not null;
    public string FormButtonText => IsEditing ? "Save changes" : "Add task";

    partial void OnEditingTaskChanged(TaskItemViewModel? value)
    {
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(FormButtonText));
    }

    private bool CanSaveTask() => !string.IsNullOrWhiteSpace(NewTitle);

    [RelayCommand(CanExecute = nameof(CanSaveTask))]
    private void SaveTask()
    {
        if (EditingTask is { } editing)
        {
            _service.Update(editing.Id, NewTitle, NewNotes, NewPriority);
            StatusMessage = "Task updated.";
        }
        else
        {
            _service.Add(NewTitle, NewNotes, Day, NewPriority);
            StatusMessage = "Task added.";
        }
        CancelEdit();
        LoadDay();
        RefreshDashboard();
    }

    [RelayCommand]
    private void EditTask(TaskItemViewModel task)
    {
        EditingTask = task;
        NewTitle = task.Title;
        NewNotes = task.Notes ?? "";
        NewPriority = task.Priority;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        EditingTask = null;
        NewTitle = "";
        NewNotes = "";
        NewPriority = TaskPriority.Medium;
    }

    [RelayCommand]
    private void DeleteTask(TaskItemViewModel task)
    {
        var answer = MessageBox.Show($"Delete \"{task.Title}\"?", "Delete task",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        if (EditingTask?.Id == task.Id) CancelEdit();
        _service.Delete(task.Id);
        LoadDay();
        RefreshDashboard();
        StatusMessage = "Task deleted.";
    }

    // ───────────────────────── Dashboard ─────────────────────────

    public ObservableCollection<ChartBar> ChartBars { get; } = new();
    public static string[] ChartRanges { get; } = { "Last 7 days", "Last 30 days" };

    [ObservableProperty] private string _selectedChartRange = "Last 7 days";
    [ObservableProperty] private RangeStat _weekStat = new(0, 0);
    [ObservableProperty] private RangeStat _monthStat = new(0, 0);
    [ObservableProperty] private RangeStat _allTimeStat = new(0, 0);
    [ObservableProperty] private int _streak;

    partial void OnSelectedChartRangeChanged(string value) => RefreshDashboard();

    private void RefreshDashboard()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Week = Monday..Sunday containing today
        int offset = ((int)today.DayOfWeek + 6) % 7;
        var weekStart = today.AddDays(-offset);
        WeekStat = _service.GetRangeStat(weekStart, weekStart.AddDays(6));

        var monthStart = new DateOnly(today.Year, today.Month, 1);
        MonthStat = _service.GetRangeStat(monthStart, monthStart.AddMonths(1).AddDays(-1));

        AllTimeStat = _service.GetAllTimeStat();
        Streak = _service.GetStreak(today);

        int days = SelectedChartRange == "Last 30 days" ? 30 : 7;
        ChartBars.Clear();
        foreach (var s in _service.GetDailyStats(today.AddDays(-(days - 1)), today))
        {
            ChartBars.Add(new ChartBar
            {
                Label = days == 7 ? s.Date.ToString("ddd") : s.Date.Day.ToString(),
                Tooltip = $"{s.Date:dd MMM yyyy}\n{s.Completed}/{s.Total} completed ({s.Percent:0}%)",
                Percent = s.Percent,
                HasTasks = s.Total > 0,
                IsToday = s.Date == today
            });
        }

        BuildCalendar();
    }

    // ───────────────────────── Sidebar calendar ─────────────────────────

    public ObservableCollection<CalendarDay> CalendarDays { get; } = new();
    public static string[] WeekDayHeaders { get; } = { "Mo", "Tu", "We", "Th", "Fr", "Sa", "Su" };

    [ObservableProperty] private DateTime _displayMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public string DisplayMonthTitle => DisplayMonth.ToString("MMMM yyyy");

    partial void OnDisplayMonthChanged(DateTime value)
    {
        OnPropertyChanged(nameof(DisplayMonthTitle));
        BuildCalendar();
    }

    [RelayCommand] private void PreviousMonth() => DisplayMonth = DisplayMonth.AddMonths(-1);
    [RelayCommand] private void NextMonth() => DisplayMonth = DisplayMonth.AddMonths(1);
    [RelayCommand] private void SelectDay(CalendarDay day) => SelectedDate = day.Date;

    private void BuildCalendar()
    {
        int offset = ((int)DisplayMonth.DayOfWeek + 6) % 7; // Monday-first grid
        var gridStart = DisplayMonth.AddDays(-offset);
        var gridEnd = gridStart.AddDays(41);

        var stats = _service.GetDailyStats(DateOnly.FromDateTime(gridStart), DateOnly.FromDateTime(gridEnd))
            .ToDictionary(s => s.Date);

        CalendarDays.Clear();
        for (var d = gridStart; d <= gridEnd; d = d.AddDays(1))
        {
            var s = stats[DateOnly.FromDateTime(d)];
            CalendarDays.Add(new CalendarDay
            {
                Date = d,
                IsCurrentMonth = d.Month == DisplayMonth.Month,
                IsSelected = d.Date == SelectedDate.Date,
                IsToday = d.Date == DateTime.Today,
                Total = s.Total,
                Completed = s.Completed
            });
        }
    }

    // ───────────────────────── Settings / data ─────────────────────────

    [ObservableProperty] private bool _isDarkTheme;

    partial void OnIsDarkThemeChanged(bool value)
    {
        App.ApplyTheme(value);
        _settings.DarkTheme = value;
        SettingsService.Save(_settings);
    }

    [RelayCommand]
    private void ExportCsv()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "CSV file (*.csv)|*.csv",
            FileName = $"tasks-{DateTime.Today:yyyy-MM-dd}.csv"
        };
        if (dlg.ShowDialog() != true) return;
        _service.ExportCsv(dlg.FileName);
        StatusMessage = $"Exported to {dlg.FileName}";
    }

    [RelayCommand]
    private void Backup()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "Task Tracker backup (*.db)|*.db",
            FileName = $"tasktracker-backup-{DateTime.Today:yyyy-MM-dd}.db"
        };
        if (dlg.ShowDialog() != true) return;
        _service.Backup(dlg.FileName);
        StatusMessage = $"Backup saved to {dlg.FileName}";
    }

    [RelayCommand]
    private void Restore()
    {
        var dlg = new OpenFileDialog { Filter = "Task Tracker backup (*.db)|*.db" };
        if (dlg.ShowDialog() != true) return;

        var answer = MessageBox.Show(
            "Restoring will REPLACE all your current tasks with the backup. Continue?",
            "Restore backup", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        try
        {
            _service.Restore(dlg.FileName);
            CancelEdit();
            LoadDay();
            RefreshDashboard();
            StatusMessage = "Backup restored.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not restore backup:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void OpenDataFolder() =>
        System.Diagnostics.Process.Start("explorer.exe", Data.AppDbContext.DataFolder);
}

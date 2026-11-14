using CommunityToolkit.Mvvm.ComponentModel;
using TaskTracker.Models;

namespace TaskTracker.ViewModels;

public partial class TaskItemViewModel : ObservableObject
{
    private readonly Action<TaskItemViewModel> _onCompletedChanged;
    private bool _loading = true;

    public int Id { get; }

    [ObservableProperty] private string _title;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private TaskPriority _priority;
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private DateTime? _completedAt;

    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    public string PriorityLabel => Priority.ToString();
    public string CompletedLabel => CompletedAt is { } c ? $"Done at {c:hh:mm tt}" : "";

    public TaskItemViewModel(TaskItem item, Action<TaskItemViewModel> onCompletedChanged)
    {
        _onCompletedChanged = onCompletedChanged;
        Id = item.Id;
        _title = item.Title;
        _notes = item.Notes;
        _priority = item.Priority;
        _isCompleted = item.IsCompleted;
        _completedAt = item.CompletedAt;
        _loading = false;
    }

    partial void OnIsCompletedChanged(bool value)
    {
        CompletedAt = value ? DateTime.Now : null;
        if (!_loading) _onCompletedChanged(this);
    }

    partial void OnNotesChanged(string? value) => OnPropertyChanged(nameof(HasNotes));
    partial void OnPriorityChanged(TaskPriority value) => OnPropertyChanged(nameof(PriorityLabel));
    partial void OnCompletedAtChanged(DateTime? value) => OnPropertyChanged(nameof(CompletedLabel));
}

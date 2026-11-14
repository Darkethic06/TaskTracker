namespace TaskTracker.ViewModels;

/// <summary>One cell in the sidebar month calendar.</summary>
public class CalendarDay
{
    public DateTime Date { get; init; }
    public string DayNumber => Date.Day.ToString();
    public bool IsCurrentMonth { get; init; }
    public bool IsSelected { get; init; }
    public bool IsToday { get; init; }
    public int Total { get; init; }
    public int Completed { get; init; }

    public bool HasTasks => Total > 0;
    public bool IsAllDone => Total > 0 && Completed == Total;
    public bool HasPending => Total > 0 && Completed < Total;
    public string Tooltip => Total == 0
        ? Date.ToString("dd MMM yyyy")
        : $"{Date:dd MMM yyyy}\n{Completed}/{Total} completed";
}

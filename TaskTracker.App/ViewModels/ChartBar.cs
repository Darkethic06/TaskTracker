namespace TaskTracker.ViewModels;

/// <summary>One bar in the dashboard chart.</summary>
public class ChartBar
{
    public const double MaxHeight = 160;

    public string Label { get; init; } = "";
    public string Tooltip { get; init; } = "";
    public double Percent { get; init; }
    public bool HasTasks { get; init; }
    public bool IsToday { get; init; }

    /// <summary>Pixel height of the bar; small stub for days with tasks but 0%.</summary>
    public double BarHeight => !HasTasks ? 0 : Math.Max(3, Percent / 100.0 * MaxHeight);
    public string PercentText => HasTasks ? $"{Percent:0}%" : "–";
}

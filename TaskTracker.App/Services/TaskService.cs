using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models;

namespace TaskTracker.Services;

public record DayStat(DateOnly Date, int Total, int Completed)
{
    public int Pending => Total - Completed;
    public double Percent => Total == 0 ? 0 : Math.Round(Completed * 100.0 / Total, 1);
}

public record RangeStat(int Total, int Completed)
{
    public int Pending => Total - Completed;
    public double Percent => Total == 0 ? 0 : Math.Round(Completed * 100.0 / Total, 1);
}

/// <summary>All database access lives here. Each call uses a short-lived DbContext.</summary>
public class TaskService
{
    public void EnsureDatabase()
    {
        using var db = new AppDbContext();
        db.Database.EnsureCreated();
    }

    public List<TaskItem> GetTasksForDate(DateOnly date)
    {
        using var db = new AppDbContext();
        return db.Tasks.AsNoTracking()
            .Where(t => t.TaskDate == date)
            .ToList()
            .OrderBy(t => t.IsCompleted)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToList();
    }

    public TaskItem Add(string title, string? notes, DateOnly date, TaskPriority priority)
    {
        using var db = new AppDbContext();
        var item = new TaskItem
        {
            Title = title.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            TaskDate = date,
            Priority = priority,
            CreatedAt = DateTime.Now
        };
        db.Tasks.Add(item);
        db.SaveChanges();
        return item;
    }

    public void Update(int id, string title, string? notes, TaskPriority priority)
    {
        using var db = new AppDbContext();
        var item = db.Tasks.Find(id);
        if (item is null) return;
        item.Title = title.Trim();
        item.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        item.Priority = priority;
        db.SaveChanges();
    }

    public void SetCompleted(int id, bool completed)
    {
        using var db = new AppDbContext();
        var item = db.Tasks.Find(id);
        if (item is null) return;
        item.IsCompleted = completed;
        item.CompletedAt = completed ? DateTime.Now : null;
        db.SaveChanges();
    }

    public void Delete(int id)
    {
        using var db = new AppDbContext();
        var item = db.Tasks.Find(id);
        if (item is null) return;
        db.Tasks.Remove(item);
        db.SaveChanges();
    }

    /// <summary>Number of unfinished tasks on dates before <paramref name="today"/>.</summary>
    public int CountOverdue(DateOnly today)
    {
        using var db = new AppDbContext();
        return db.Tasks.Count(t => !t.IsCompleted && t.TaskDate < today);
    }

    /// <summary>Moves all unfinished past tasks to <paramref name="today"/>. Returns how many moved.</summary>
    public int MoveOverdueTo(DateOnly today)
    {
        using var db = new AppDbContext();
        var overdue = db.Tasks.Where(t => !t.IsCompleted && t.TaskDate < today).ToList();
        foreach (var t in overdue) t.TaskDate = today;
        db.SaveChanges();
        return overdue.Count;
    }

    public RangeStat GetRangeStat(DateOnly from, DateOnly to)
    {
        using var db = new AppDbContext();
        var q = db.Tasks.Where(t => t.TaskDate >= from && t.TaskDate <= to);
        return new RangeStat(q.Count(), q.Count(t => t.IsCompleted));
    }

    public RangeStat GetAllTimeStat()
    {
        using var db = new AppDbContext();
        return new RangeStat(db.Tasks.Count(), db.Tasks.Count(t => t.IsCompleted));
    }

    /// <summary>One entry per day in the range (days without tasks have Total = 0).</summary>
    public List<DayStat> GetDailyStats(DateOnly from, DateOnly to)
    {
        using var db = new AppDbContext();
        var grouped = db.Tasks.AsNoTracking()
            .Where(t => t.TaskDate >= from && t.TaskDate <= to)
            .Select(t => new { t.TaskDate, t.IsCompleted })
            .ToList()
            .GroupBy(t => t.TaskDate)
            .ToDictionary(g => g.Key, g => (Total: g.Count(), Done: g.Count(x => x.IsCompleted)));

        var result = new List<DayStat>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            grouped.TryGetValue(d, out var s);
            result.Add(new DayStat(d, s.Total, s.Done));
        }
        return result;
    }

    /// <summary>
    /// Consecutive days (ending today, or yesterday if today is not finished yet)
    /// that had at least one task and all tasks completed.
    /// </summary>
    public int GetStreak(DateOnly today)
    {
        var stats = GetDailyStats(today.AddDays(-365), today)
            .ToDictionary(s => s.Date);

        var day = today;
        var todayStat = stats[today];
        if (!(todayStat.Total > 0 && todayStat.Pending == 0))
            day = today.AddDays(-1);

        int streak = 0;
        while (stats.TryGetValue(day, out var s) && s.Total > 0 && s.Pending == 0)
        {
            streak++;
            day = day.AddDays(-1);
        }
        return streak;
    }

    public void ExportCsv(string path)
    {
        using var db = new AppDbContext();
        var all = db.Tasks.AsNoTracking().ToList().OrderBy(t => t.TaskDate).ThenBy(t => t.CreatedAt);
        var sb = new StringBuilder();
        sb.AppendLine("Date,Title,Notes,Priority,Status,CompletedAt");
        foreach (var t in all)
        {
            sb.AppendLine(string.Join(",",
                t.TaskDate.ToString("yyyy-MM-dd"),
                Csv(t.Title),
                Csv(t.Notes ?? ""),
                t.Priority,
                t.IsCompleted ? "Completed" : "Pending",
                t.CompletedAt?.ToString("yyyy-MM-dd HH:mm") ?? ""));
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static string Csv(string s) =>
        s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? "\"" + s.Replace("\"", "\"\"") + "\""
            : s;

    /// <summary>Writes a consistent snapshot of the database (includes un-checkpointed WAL data).</summary>
    public void Backup(string destination)
    {
        if (File.Exists(destination)) File.Delete(destination);
        using var db = new AppDbContext();
        db.Database.ExecuteSqlRaw("VACUUM INTO {0}", destination);
    }

    public void Restore(string source)
    {
        // Validate the backup first: it must be a SQLite file with a Tasks table.
        using (var check = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={source};Mode=ReadOnly;Pooling=False"))
        {
            check.Open();
            using var cmd = check.CreateCommand();
            cmd.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='Tasks'";
            if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                throw new InvalidDataException("This file is not a Task Tracker backup.");
        }

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "-wal", "-shm" })
            if (File.Exists(AppDbContext.DbPath + suffix)) File.Delete(AppDbContext.DbPath + suffix);
        File.Copy(source, AppDbContext.DbPath, overwrite: true);
        EnsureDatabase();
    }
}

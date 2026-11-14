using System.IO;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Models;

namespace TaskTracker.Data;

public class AppDbContext : DbContext
{
    /// <summary>%AppData%\TaskTracker, or the TASKTRACKER_DATA env var if set (handy for testing).</summary>
    public static string DataFolder { get; } =
        Environment.GetEnvironmentVariable("TASKTRACKER_DATA") is { Length: > 0 } custom
            ? custom
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskTracker");

    public static string DbPath { get; } = Path.Combine(DataFolder, "tasks.db");

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        Directory.CreateDirectory(DataFolder);
        options.UseSqlite($"Data Source={DbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var task = modelBuilder.Entity<TaskItem>();
        task.ToTable("Tasks");
        task.HasKey(t => t.Id);
        task.Property(t => t.Title).IsRequired().HasMaxLength(300);
        task.Property(t => t.Priority).HasConversion<int>();
        task.HasIndex(t => t.TaskDate);
    }
}

using System.IO;
using System.Text.Json;
using TaskTracker.Data;

namespace TaskTracker.Services;

public class AppSettings
{
    public bool DarkTheme { get; set; }
}

public static class SettingsService
{
    private static string FilePath => Path.Combine(AppDbContext.DataFolder, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { /* corrupted settings: fall back to defaults */ }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(AppDbContext.DataFolder);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings));
    }
}

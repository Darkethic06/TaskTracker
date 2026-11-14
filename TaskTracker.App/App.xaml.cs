using System.IO;
using System.Windows;
using System.Windows.Threading;
using TaskTracker.Data;

namespace TaskTracker;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnUnhandledException;
    }

    /// <summary>Swaps the colour dictionary (index 0) between Light and Dark.</summary>
    public static void ApplyTheme(bool dark)
    {
        var dicts = Current.Resources.MergedDictionaries;
        var uri = new Uri(dark ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative);
        dicts[0] = new ResourceDictionary { Source = uri };
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(AppDbContext.DataFolder);
            File.AppendAllText(Path.Combine(AppDbContext.DataFolder, "error.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.Exception}\n\n");
        }
        catch { /* ignore logging failures */ }

        MessageBox.Show($"Something went wrong:\n{e.Exception.Message}\n\nDetails were saved to error.log in the data folder.",
            "Task Tracker", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}

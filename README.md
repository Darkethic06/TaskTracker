# Task Tracker

A Windows desktop app for tracking daily tasks: what's completed, what's pending, and your completion percentage.

Built with C# / .NET 8 / WPF and a local SQLite database.

## Features
- Tasks per date, picked from a month calendar. Coloured dots show which days are fully done (green) or have pending tasks (orange).
- Add, edit and delete tasks, with optional notes and a Low/Medium/High priority.
- Mark tasks done or pending with one click.
- For each day: total, completed and pending counts, plus a completion % progress bar.
- Filter the list by All, Pending or Completed.
- Overdue banner: moves unfinished tasks from past days to today with one click.
- Dashboard: completion % for this week, this month and all time, a streak counter, and a daily bar chart (last 7 or 30 days).
- Light and dark themes.
- Export to CSV, plus database backup and restore.

## Where is my data?
`%AppData%\TaskTracker\tasks.db`. Click **Data folder** in the app to open it.
Uninstalling the app does **not** delete your tasks.

## Development
```powershell
cd TaskTracker.App
dotnet run
```
To run against a separate test database, set `TASKTRACKER_DATA` to another folder:
```powershell
$env:TASKTRACKER_DATA = "C:\temp\tt-test"; dotnet run
```

## Build the installer
Requires the .NET 8 SDK and Inno Setup 6 (`winget install JRSoftware.InnoSetup`).
```powershell
.\build-installer.ps1
```
Output: `installer\output\TaskTrackerSetup-1.0.0.exe`. It's self-contained, so the target PC doesn't need .NET installed.

## Project layout
```
TaskTracker.App/
  Models/       TaskItem
  Data/         AppDbContext (EF Core + SQLite)
  Services/     TaskService (all DB access, stats, backup), SettingsService
  ViewModels/   MainViewModel, TaskItemViewModel, CalendarDay, ChartBar
  Converters/   XAML value converters
  Themes/       Light.xaml, Dark.xaml, Styles.xaml
  MainWindow.xaml
installer/      TaskTracker.iss (Inno Setup script)
```

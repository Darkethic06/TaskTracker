# Contributing to Task Tracker

Thanks for helping improve Task Tracker! 🎉 This guide explains how to get set up and what we look for in contributions.

## Ways to contribute

- 🐛 **Report bugs.** Open an issue with steps to reproduce, what you expected, and what happened. Attach `%AppData%\TaskTracker\error.log` if it exists.
- 💡 **Suggest features.** Open an issue describing the problem it solves. The [Roadmap](README.md#-roadmap) lists ideas already planned.
- 🎨 **Improve the UI or UX**, including accessibility and theme polish.
- 📝 **Improve the docs.**
- 🧑‍💻 **Send code.** Bug fixes and features are both welcome.

For larger changes, please **open an issue first** so we can agree on the approach before you put time in.

## Development setup

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Fork and clone the repo.
3. Point the app at a throwaway database so you don't touch your real tasks:
   ```powershell
   $env:TASKTRACKER_DATA = "C:\temp\tasktracker-dev"
   cd TaskTracker.App
   dotnet run
   ```

## Architecture in 30 seconds

The app follows **MVVM**:

| Layer | Folder | Responsibility |
|---|---|---|
| Model | `Models/` | Plain data classes (`TaskItem`) |
| Data | `Data/` | `AppDbContext`, which configures EF Core + SQLite |
| Services | `Services/` | **All** database access and statistics (`TaskService`) |
| ViewModels | `ViewModels/` | UI state and commands (CommunityToolkit.Mvvm source generators) |
| Views | `MainWindow.xaml`, `Themes/` | XAML only, with no business logic in code-behind |

Guidelines:
- **No database code in ViewModels or Views.** Add a method to `TaskService` instead.
- Each `TaskService` method creates a short-lived `AppDbContext` (`using var db = new AppDbContext();`).
- Use `[ObservableProperty]` and `[RelayCommand]` rather than hand-written `INotifyPropertyChanged`.
- Colours **must** come from the theme dictionaries (`{DynamicResource Accent}` and so on) so light and dark mode both work. Don't hard-code colours in XAML.
- Add new colour keys to **both** `Themes/Light.xaml` and `Themes/Dark.xaml`.

## Database changes

The schema is created with `EnsureCreated()`. If you change the `TaskItem` model, existing users' databases **won't update automatically**. Please mention schema changes in your PR so we can plan a migration path.

## Before you open a PR

- [ ] `dotnet build` passes with **no warnings**
- [ ] You ran the app and tried your change in **both light and dark mode**
- [ ] Existing data still loads (try a database from before your change)
- [ ] The PR description explains *what* changed and *why*; add screenshots for UI changes
- [ ] The change is focused, with one feature or fix per PR

## Code style

- Follow standard C# conventions: PascalCase for public members, `_camelCase` for private fields.
- File-scoped namespaces (`namespace TaskTracker.Services;`).
- Nullable reference types are enabled. Please don't suppress warnings with `!` unless it's truly safe.
- Keep methods small, and use comments to explain *why*, not *what*.

## Commit messages

Use short, imperative messages:
```
Add recurring task support
Fix streak calculation when today has no tasks
Improve dark theme contrast on calendar
```

## Code of conduct

Be kind and respectful. We're all here to build something useful together.

Thanks again for contributing! 💙

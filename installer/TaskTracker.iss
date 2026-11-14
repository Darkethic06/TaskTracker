; Inno Setup script for Task Tracker
; Build with:  build-installer.ps1   (publishes the app, then compiles this script)

#define AppName "Task Tracker"
#define AppVersion "1.0.0"
#define AppExe "TaskTracker.exe"
#define PublishDir "..\TaskTracker.App\bin\Release\net8.0-windows\win-x64\publish"

[Setup]
AppId={{8B6E4C2A-3F1D-4E7B-9A52-6C1D2E8F4A17}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Arijit Paul
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Installs per-user by default (no admin prompt); user can choose "all users".
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=output
OutputBaseFilename=TaskTrackerSetup-{#AppVersion}
SetupIconFile=..\TaskTracker.App\Assets\app.ico
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

; Note: your tasks live in %AppData%\TaskTracker\tasks.db and are intentionally
; NOT removed on uninstall, so reinstalling/upgrading keeps all your data.

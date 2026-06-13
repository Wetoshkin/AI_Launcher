; Inno Setup script for AI Launcher Studio.
; Per-user install (no admin), Start Menu + Desktop shortcuts, clean uninstaller.

#define MyAppName "AI Launcher Studio"
#define MyAppVersion "1.0.11"
#define MyAppPublisher "Wetoshkin"
#define MyAppURL "https://github.com/Wetoshkin/AI_Launcher"
#define MyAppExeName "Launcher.Desktop.exe"

[Setup]
AppId={{8F3A9C12-4B7D-4E2A-9C31-1A2B3C4D5E6F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={localappdata}\Programs\AI Launcher Studio
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=Output
OutputBaseFilename=AI-Launcher-Studio-Setup-{#MyAppVersion}
SetupIconFile=..\src\Launcher.Desktop\Assets\ai-launcher-studio.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Авто-обновление из приложения: закрыть запущенный экземпляр и снова запустить его.
AppMutex=AILauncherStudio_SingleInstance
CloseApplications=yes
RestartApplications=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

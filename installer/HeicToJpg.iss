; Inno Setup script for HEIC to JPG Converter.
; Installs per-user (no admin/UAC required) under %LocalAppData%\Programs.
; Build the app first: dotnet publish src\HeicToJpg.App\HeicToJpg.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\win-x64
; Then compile: ISCC installer\HeicToJpg.iss

#define MyAppName "HEIC to JPG Converter"
#define MyAppVersion "1.2"
#define MyAppVersionInfo "1.2.0.0"
#define MyAppPublisher "Ben Conrad"
#define MyAppExeName "HeicToJpg.exe"
#define MyPublishDir "..\publish\win-x64"

[Setup]
AppId={{9851A820-AB2A-407D-937A-9F6FB6E6D53B}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\HeicToJpg
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\dist
OutputBaseFilename=HeicToJpgSetup
SetupIconFile=..\src\HeicToJpg.App\Assets\icon.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion={#MyAppVersionInfo}
VersionInfoDescription={#MyAppName} Setup
VersionInfoCompany={#MyAppPublisher}
VersionInfoCopyright=Copyright (C) {#MyAppPublisher}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked
Name: "sendtoicon"; Description: "Add ""{#MyAppName}"" to the Send To menu"; GroupDescription: "Additional icons:"

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Windows\SendTo\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: sendtoicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

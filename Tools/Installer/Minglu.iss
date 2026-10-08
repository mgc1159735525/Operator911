#define GameName "明路·地下"
[Setup]
AppId={{31982D7D-14E4-4AE1-B273-EBCDC960DA63}
AppName={#GameName}
AppVersion=0.17.3
AppPublisher=Minglu
DefaultDirName={code:DefaultInstallDir}
DefaultGroupName={#GameName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\..\Builds\Installer
OutputBaseFilename=MingluSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\MingluCity.exe
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Languages]
Name: "chinesesimp"; MessagesFile: "ChineseSimplified.isl"

[Files]
Source: "..\..\Builds\MingluCity\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,*.log"
Source: "..\..\Docs\Minglu\安装与游玩指南.html"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userdesktop}\{#GameName}"; Filename: "{app}\MingluCity.exe"; WorkingDir: "{app}"
Name: "{group}\{#GameName}"; Filename: "{app}\MingluCity.exe"; WorkingDir: "{app}"
Name: "{group}\安装与游玩指南"; Filename: "{app}\安装与游玩指南.html"
Name: "{group}\卸载明路·地下"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\MingluCity.exe"; Description: "启动明路·地下"; Flags: nowait postinstall skipifsilent

; Save files are outside {app}. No uninstall deletion is applied to user saves.

[Code]
function DefaultInstallDir(Param: string): string;
begin
  if DirExists('D:\') then
    Result := 'D:\Games\MingluCity'
  else
    Result := ExpandConstant('{localappdata}\Programs\MingluCity');
end;

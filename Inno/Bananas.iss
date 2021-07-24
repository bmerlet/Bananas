; -- Bananas.iss --
; Installs Bananas and all dependents.

[Setup]
AppName=Bananas
AppVersion=1.0
WizardStyle=modern
DefaultDirName={autopf}\Bananas
DefaultGroupName=Bananas
UninstallDisplayIcon={app}\Bananas.exe
Compression=lzma2
SolidCompression=yes
OutputDir=Debug
OutputBaseFilename=BananasSetup
PrivilegesRequired=Admin

[Files]
Source: "..\XamlUI\bin\Debug\XamlUI.exe"; DestDir: "{app}"; DestName: "Bananas.exe"
Source: "..\XamlUI\bin\Debug\BanaData.dll"; DestDir: "{app}"
Source: "..\XamlUI\bin\Debug\ToolBox.dll"; DestDir: "{app}"
Source: "..\XamlUI\bin\Debug\Sounds\kaching.wav"; DestDir: "{app}\Sounds"

[Icons]
Name: "{group}\Bananas"; Filename: "{app}\Bananas.exe"

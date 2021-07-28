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
ChangesAssociations=yes

[Files]
Source: "..\XamlUI\bin\Debug\XamlUI.exe"; DestDir: "{app}"; DestName: "Bananas.exe"
Source: "..\XamlUI\bin\Debug\BanaData.dll"; DestDir: "{app}"
Source: "..\XamlUI\bin\Debug\ToolBox.dll"; DestDir: "{app}"
Source: "..\XamlUI\bin\Debug\Sounds\kaching.wav"; DestDir: "{app}\Sounds"

[Icons]
Name: "{group}\Bananas"; Filename: "{app}\Bananas.exe"

[Registry]
Root: HKA; Subkey: "Software\Classes\.ban\OpenWithProgids"; ValueType: string; ValueName: "Bananas.ban"; ValueData: ""; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\Bananas.ban"; ValueType: string; ValueName: ""; ValueData: "Bananas"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Bananas.ban\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\Bananas.exe,0"
Root: HKA; Subkey: "Software\Classes\Bananas.ban\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Bananas.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\Applications\Bananas.exe\SupportedTypes"; ValueType: string; ValueName: ".ban"; ValueData: ""
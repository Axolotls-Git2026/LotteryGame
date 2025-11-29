; -- LotteryGame Installer Script --

[Setup]
AppName=Lottery Game
AppVersion=0.8
DefaultDirName={pf}\LotteryGame
DefaultGroupName=Lottery Game
OutputDir=D:\
OutputBaseFilename=LotteryGameInstaller
Compression=lzma
SolidCompression=yes

[Files]
; Include only Unity build output files (NOT the whole Unity project!)
Source: "H:\LotteryGameNewVersion\LotteryGame (2)\LotteryGame\LotteryGame\LotteryGameV1.5\Lottery Game.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "H:\LotteryGameNewVersion\LotteryGame (2)\LotteryGame\LotteryGame\LotteryGameV1.5\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start Menu shortcut
Name: "{group}\Lottery Game"; Filename: "{app}\Lottery Game.exe"
; Desktop shortcut
Name: "{commondesktop}\Lottery Game"; Filename: "{app}\Lottery Game.exe"

[Run]
; Auto-run after installation
Filename: "{app}\Lottery Game.exe"; Description: "Launch Lottery Game"; Flags: nowait postinstall skipifsilent

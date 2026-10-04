; Inno owns only the wizard; Velopack owns application files, updates and uninstall.
#ifndef AppVersion
  #error AppVersion is required
#endif
#ifndef AppPackageId
  #define AppPackageId "LovenseIntegratorApp"
#endif
#ifndef Bootstrapper
  #error Bootstrapper is required
#endif
#ifndef ReleaseDirectory
  #error ReleaseDirectory is required
#endif

[Setup]
AppId={#AppPackageId}.Wizard
AppName=Lovense Integrator
AppVersion={#AppVersion}
AppPublisher=timurchak
AppPublisherURL=https://github.com/timurchak/LovenseIntegrator
DefaultDirName={code:DefaultInstallFolder}
UsePreviousAppDir=no
DisableDirPage=no
DisableWelcomePage=no
DisableReadyPage=no
AlwaysShowDirOnReadyPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
WizardStyle=modern
WizardSizePercent=115
Uninstallable=no
CreateAppDir=yes
DirExistsWarning=no
AllowRootDirectory=no
AllowUNCPath=no
CloseApplications=no
RestartApplications=no
SetupLogging=yes
OutputDir={#ReleaseDirectory}
OutputBaseFilename={#AppPackageId}-win-Setup
Compression=none
SolidCompression=no
ExtraDiskSpaceRequired=350000000
DiskSpanning=no

[Files]
Source: "{#Bootstrapper}"; DestDir: "{tmp}"; DestName: "LovenseIntegrator-Payload.exe"; Flags: deleteafterinstall

[Run]
Filename: "{app}\current\LovenseIntegrator.exe"; Description: "Launch Lovense Integrator"; Flags: postinstall nowait skipifsilent unchecked

[Messages]
WelcomeLabel2=This wizard will install Lovense Integrator {#AppVersion} on your computer.%n%nYou will choose the installation folder and review the changes before any application files are installed.%n%nYour saved rules and profiles will be kept. Close Lovense Integrator before continuing.
SelectDirDesc=Choose where to install the application.
SelectDirLabel3=Application files and the included .NET runtime will be installed in the folder below. Your profiles are stored separately and will be preserved.
ReadyLabel1=Setup is ready. Review the destination and changes below, then click Install to continue.
FinishedLabel=Lovense Integrator has been installed.%n%nUse the Desktop or Start menu shortcut to open it. Automatic updates keep the installation folder you selected. Rules start paused.

[Code]
const
  RegistryKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#AppPackageId}';
var
  PreviousFolder: String;

function FileAttributes(Path: String): LongWord;
  external 'GetFileAttributesW@kernel32.dll stdcall';

function Canonical(Path: String): String;
begin
  Result := RemoveBackslashUnlessRoot(ExpandFileName(Path));
end;

function IsWithin(Child, Parent: String): Boolean;
begin
  Child := Lowercase(AddBackslash(Canonical(Child)));
  Parent := Lowercase(AddBackslash(Canonical(Parent)));
  Result := Pos(Parent, Child) = 1;
end;

function OwnedInstallation(Path: String): Boolean;
var Manifest: AnsiString;
begin
  Result := FileExists(AddBackslash(Path) + 'Update.exe') and
    LoadStringFromFile(AddBackslash(Path) + 'current\sq.version', Manifest);
  if Result then Result := Pos('<id>{#AppPackageId}</id>', String(Manifest)) > 0;
end;

function DefaultInstallFolder(Param: String): String;
begin
  Result := ExpandConstant('{localappdata}\{#AppPackageId}');
  if PreviousFolder <> '' then Result := PreviousFolder;
end;

function InitializeSetup(): Boolean;
var Candidate: String;
begin
  if RegQueryStringValue(HKCU, RegistryKey, 'InstallLocation', Candidate) and OwnedInstallation(Candidate) then
    PreviousFolder := Canonical(Candidate);
  Result := True;
end;

function DirectoryNotEmpty(Path: String): Boolean;
var Item: TFindRec;
begin
  Result := False;
  if FindFirst(AddBackslash(Path) + '*', Item) then begin
    try
      repeat
        if (Item.Name <> '.') and (Item.Name <> '..') then begin Result := True; Break; end;
      until not FindNext(Item);
    finally FindClose(Item); end;
  end;
end;

function ValidateDestination(): String;
var Destination, DataFolder, Parent: String;
begin
  Result := '';
  Destination := Canonical(WizardDirValue);
  if Length(Destination) <= 3 then begin Result := 'Choose a dedicated application folder, not a drive root.'; Exit; end;
  DataFolder := ExpandConstant('{localappdata}\LovenseIntegrator');
  if IsWithin(Destination, DataFolder) or IsWithin(DataFolder, Destination) then begin
    Result := 'Choose a dedicated application folder outside the folder containing your profiles.'; Exit;
  end;
  if (PreviousFolder <> '') and (CompareText(Destination, PreviousFolder) <> 0) then begin
    Result := 'An existing installation was found at:' + #13#10 + PreviousFolder + #13#10#13#10 +
      'Use that folder to upgrade. To move the app, first uninstall Lovense Integrator in Windows Settings, then run this wizard again and choose the new folder. Your saved profiles are kept.';
    Exit;
  end;
  if DirectoryNotEmpty(Destination) and not ((CompareText(Destination, PreviousFolder) = 0) and OwnedInstallation(Destination)) then begin
    Result := 'Choose an empty folder. Setup will not replace files in a folder belonging to another application.'; Exit;
  end;
  Parent := Destination;
  while Length(Parent) > 3 do begin
    if DirExists(Parent) and ((FileAttributes(Parent) and $400) <> 0) then begin
      Result := 'Choose a local folder without symbolic links or junctions.'; Exit;
    end;
    Parent := ExtractFileDir(Parent);
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var Problem: String;
begin
  Result := True;
  if CurPageID = wpSelectDir then begin
    Problem := ValidateDestination();
    if Problem <> '' then begin SuppressibleMsgBox(Problem, mbError, MB_OK, IDOK); Result := False; end;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var Destination, Probe: String;
begin
  Result := ValidateDestination();
  if Result <> '' then Exit;
#if AppPackageId == "LovenseIntegratorApp"
  if CheckForMutexes('Local\LovenseIntegrator.UI.' + GetUserNameString) then begin
    Result := 'Close Lovense Integrator first. This lets the app stop active effects and disconnect devices before installation.'; Exit;
  end;
#endif
  Destination := Canonical(WizardDirValue);
  if not ForceDirectories(Destination) then begin Result := 'Cannot create the selected folder. Choose a folder you can write to.'; Exit; end;
  Probe := AddBackslash(Destination) + 'lovense-install-write-test.tmp';
  if not SaveStringToFile(Probe, 'Installation write check', False) then
    Result := 'The selected folder is not writable. Choose a folder in your user account or another writable drive.'
  else DeleteFile(Probe);
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
begin
  Result := MemoDirInfo + NewLine + NewLine +
    'Setup will:' + NewLine + Space + 'Install Lovense Integrator {#AppVersion} and its .NET runtime' + NewLine +
    Space + 'Create Desktop and Start menu shortcuts' + NewLine + Space + 'Keep GitHub automatic updates available' + NewLine + NewLine +
    'Your existing profiles and rules will be preserved in:' + NewLine + Space + ExpandConstant('{localappdata}\LovenseIntegrator') + NewLine + NewLine +
    'Bluetooth support downloads its verified SDK from Lovense on first use.';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var ExitCode: Integer; Started: Boolean;
begin
  if CurStep = ssPostInstall then begin
    WizardForm.StatusLabel.Caption := 'Installing application files and creating shortcuts...';
    WizardForm.FilenameLabel.Caption := WizardDirValue;
    WizardForm.ProgressGauge.Style := npbstMarquee;
    Started := Exec(ExpandConstant('{tmp}\LovenseIntegrator-Payload.exe'),
      '--silent --installto "' + WizardDirValue + '" --log "' + ExpandConstant('{tmp}\LovenseIntegrator-install.log') + '"',
      '', SW_HIDE, ewWaitUntilTerminated, ExitCode);
    WizardForm.ProgressGauge.Style := npbstNormal;
    if not Started or (ExitCode <> 0) then
      RaiseException('Installation did not complete (error ' + IntToStr(ExitCode) + '). See ' + ExpandConstant('{tmp}\LovenseIntegrator-install.log') + ' for details.');
    WizardForm.StatusLabel.Caption := 'Installation complete. Your profiles have been preserved.';
    WizardForm.FilenameLabel.Caption := '';
    WizardForm.ProgressGauge.Position := WizardForm.ProgressGauge.Max;
  end;
end;

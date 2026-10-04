# Windows releases and auto-updates

## User workflow

Install `LovenseIntegratorApp-win-Setup.exe` from [GitHub Releases](https://github.com/timurchak/LovenseIntegrator/releases). Version 0.2.0 uses an Inno Setup wizard: welcome → destination/Browse → review → extraction/installation progress → finish with an unchecked launch option. No application files are installed before Install. It is a self-contained Windows x64 WPF application; no separate .NET runtime is required. The embedded Velopack bootstrapper installs per user, creates shortcuts and registers Windows uninstall support. Inno does not register a second uninstaller.

Application files live in the chosen folder, default `%LOCALAPPDATA%/LovenseIntegratorApp`. Profiles, language/update preferences and the Bluetooth cache live separately in `%LOCALAPPDATA%/LovenseIntegrator`. **Never change the package ID to the profile folder name:** uninstall owns the entire install directory. Updates and uninstall retain the separate profile directory. The wizard recognizes the registered installation and keeps its folder for upgrades. To move an existing install, uninstall it through Windows Settings, then rerun the wizard. Nonempty unrelated folders, profile folders/ancestors, drive roots and junctions are rejected. Close the running app before installing; the wizard never kills its UI or BLE worker.

The Updates tab shows the running version, status, download progress, release notes and restart action. Checks start after 10 seconds and repeat every six hours while enabled. Manual checks also download a newer stable release. A ready update marks the tab and is applied at the next normal startup, or through Restart to update. Disabling automatic checks affects future checks; an already downloaded update still applies on startup. Network/checksum errors leave the current app usable. GitHub authentication is not required because the repository is public; never embed a GitHub token in the app.

Restart first disables the window, pauses rules, sends Stop, shuts down the BLE worker and cancels update work. Only then does Velopack restart the app. The new process starts in demo mode with rules paused. A per-user UI mutex prevents two normal instances from racing an update. Workers, smoke tests and other diagnostic entry points disable automatic application of pending updates. Install/update hooks run before WPF startup.

## Build and publish

Version lives in `Directory.Build.props`. Velopack is pinned to **1.2.161** in the desktop project and root `dotnet-tools.json`; upgrade them together. SDK is pinned to **10.0.401** in CI and Setup-Sdk. Inno Setup **6.7.3** is downloaded from the official GitHub release, SHA-256 verified and installed in portable mode under `.tools/` by `Setup-InstallerTool.ps1`; it does not register system associations/shortcuts. The stable Windows feed/channel is `win`, package ID `LovenseIntegratorApp`.

```powershell
./scripts/Build.ps1
./scripts/Test-Desktop.ps1
./scripts/Test-UiHarness.ps1
./scripts/Test-BleRecovery.ps1
./scripts/Package.ps1
./scripts/Test-Installer.ps1 -ReleaseDirectory dist/releases/0.3.0
```

Package.ps1 publishes to a fresh staging folder and refuses a nonempty release output directory. To repeat packaging, choose a fresh `-OutputDirectory dist/release-check-2`. It creates a Setup EXE, full `.nupkg`, `releases.win.json`, legacy `RELEASES` and Velopack's `assets.win.json` upload manifest. Full packages are used initially; deltas/portable archives/MSI are not generated. No local profile, logs, PDBs or vendor DLL belongs in a release.

`Wrap-Installer.ps1` compiles `installer/Setup.iss` around the raw bootstrapper. The public EXE retains Velopack's asset filename; the raw bootstrapper stays outside the release output. The `.nupkg` and update feed remain standard Velopack, so in-app updates do not display the setup wizard. Keep `CreateAppDir=yes`: `no` suppresses the directory page even with `DisableDirPage=no`. The inner bootstrapper reports no granular file progress, so its step uses a marquee and a clear status instead of a fabricated percentage. Quiet test installs use Inno flags `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR="..."`, not the raw bootstrapper flags.

To release a new version:

1. Set a higher stable `major.minor.patch` in `Directory.Build.props` and add `releases/<version>.md`.
2. Run the checks, inspect staged files, commit and push `main` normally.
3. Create an annotated `v<version>` tag at that commit and push it. Do not move existing release tags.
4. Check **Windows release** in GitHub Actions and verify the release has the installer, full package and `releases.win.json` assets.

The workflow validates tag/version/notes, builds, runs logic/WPF/BLE demo checks, packages, installs and upgrades test versions, then publishes with `vpk upload github`. GitHub's job-scoped `GITHUB_TOKEN` is supplied through `VPK_TOKEN`, with `contents: write` only on the release job. No developer PAT or SSH private key needs to be stored in repository secrets. The normal **Windows checks** workflow runs on main pushes and pull requests. Verification artifacts are retained for 14 days; they use demo devices and isolated profiles.

A failed pre-publication check leaves the release unpublished. A partial upload can leave a draft: inspect its assets before using `vpk upload github --merge` or publishing it. Do not overwrite assets in a published version; ship a higher patch version. A rollback is also a higher version containing reverted code, since clients do not downgrade automatically.

## Verification and implementation

- `Services/UpdateService.cs`: testable update state, scheduling, cancellation, independent `updates.json` preferences. This file has no WPF or Velopack dependency.
- `Services/GithubUpdateBackend.cs`: stable-only GithubSource, UpdateManager, package verification/download and restart.
- `App.Main`: Velopack bootstrap before WPF, UI instance guard, automatic application only for normal startup.
- `MainWindow`: Updates bindings and graceful shutdown before restart.
- `Services/BleSdkInstaller.cs`: direct vendor download with pinned SHA-256, bounded size/time, atomic cache replacement; no redistribution.
- `UpdateTests.cs`: uninstalled builds, offline/retry, integrity failures, pending state, cancellation, concurrent checks and settings.
- UI harness: real WPF Updates bindings, disabled actions in development and minimum-size rendering, alongside existing keyboard/mouse coverage.
- `Test-Installer.ps1`: builds isolated 0.0.1/0.0.2 test packages from the release payload with a distinct test ID and no shortcuts. It rejects an occupied folder without modifying its sentinel, installs through the wizard into a custom path containing spaces, rejects a corrupt update, applies a valid update, verifies the running version, then reinstalls through the wizard without `/DIR` to verify registered path reuse. It checks an external sentinel and uninstalls only that test installation. Reports stay under `artifacts/installer/<run>/`. No toys are opened.
- `--update-harness <local-feed|github> <report.json> [--download] [--apply]`: installed-package diagnostic without a VM, profile access or hardware. Never use `--apply` against a user's active installation during device control.

The installer is currently **unsigned**. Package hashes protect against corruption but are not a code-signing identity. Windows SmartScreen may show a prompt. Signing requires the project owner's signing certificate or configured trusted-signing service; none is assumed or purchased by these scripts.

First Bluetooth use needs access to the Lovense vendor URL. If the vendor changes the binary, a mismatch deliberately blocks loading until the new binary is reviewed and the pin updated. App update checks/downloads need GitHub/API/release-asset access; offline operation uses the installed app and cached SDK.

References: [Velopack Windows packaging](https://docs.velopack.io/packaging/operating-systems/windows), [GitHub Actions](https://docs.velopack.io/distributing/github-actions), [UpdateManager](https://docs.velopack.io/reference/cs/Velopack/UpdateManager), [VelopackApp startup behavior](https://docs.velopack.io/reference/cs/Velopack/VelopackApp).

# Horizon Tray and Autostart Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep Horizon out of the Windows taskbar, expose it through a notification-area icon, and add a default-enabled per-user autostart setting.

**Architecture:** Add two focused platform services: one owns `NotifyIcon`, and one owns the current-user `Run` registry value. `MainWindow` maps tray events onto its existing panel state machine, while `MainViewModel` persists the autostart preference and coordinates registry changes without putting Windows API code in the view.

**Tech Stack:** .NET 9, WPF, Windows Forms `NotifyIcon`, `Microsoft.Win32.Registry`, Inno Setup 6, existing console-style test project.

---

### Task 1: Add the Windows startup registration service

**Files:**
- Create: `src/Horizon.App/Services/IStartupRegistrationService.cs`
- Create: `src/Horizon.App/Services/WindowsStartupService.cs`
- Modify: `src/Horizon.App/Models/HorizonModels.cs:43-49`
- Modify: `tests/Horizon.App.Tests/Program.cs`

- [ ] **Step 1: Write failing tests for the default setting and quoted command**

Add assertions that an old JSON settings object with no `StartWithWindows` field deserializes to `true`, and that `WindowsStartupService.BuildCommand(@"C:\Program Files\Horizon\Horizon.App.exe")` returns `"\"C:\Program Files\Horizon\Horizon.App.exe\""`.

- [ ] **Step 2: Run the tests and verify the new assertions fail**

Run: `dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj -c Release`

Expected: compilation fails because `StartWithWindows` and `WindowsStartupService` do not exist.

- [ ] **Step 3: Add the setting and service contract**

Add `public bool StartWithWindows { get; set; } = true;` to `HorizonSettings`, and create:

```csharp
public interface IStartupRegistrationService
{
    bool TrySetEnabled(bool enabled, out string? errorMessage);
}
```

- [ ] **Step 4: Implement current-user startup registration**

Implement `WindowsStartupService` with constants for `Software\Microsoft\Windows\CurrentVersion\Run` and value name `Horizon`. Build the command from `Environment.ProcessPath`, always quote the executable path, write it with `Registry.CurrentUser.CreateSubKey`, and delete it when disabled. Catch registry/path exceptions, return `false`, and provide a short Chinese error message without throwing into application startup.

- [ ] **Step 5: Run tests and commit**

Run: `dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj -c Release`

Expected: `Panel layout tests passed.`

```powershell
git add src/Horizon.App/Services/IStartupRegistrationService.cs src/Horizon.App/Services/WindowsStartupService.cs src/Horizon.App/Models/HorizonModels.cs tests/Horizon.App.Tests/Program.cs
git commit -m "feat: add Windows autostart registration"
```

### Task 2: Connect autostart to the settings view model and UI

**Files:**
- Modify: `src/Horizon.App/ViewModels/MainViewModel.cs:6-31,153-217,237-251,387-394`
- Modify: `src/Horizon.App/MainWindow.xaml:1007-1074`
- Modify: `src/Horizon.App/MainWindow.xaml.cs:34-46,138-147`
- Modify: `tests/Horizon.App.Tests/Program.cs`

- [ ] **Step 1: Add a fake service and failing view-model tests**

Create a test-only fake implementing `IStartupRegistrationService`. Assert that startup reconciliation requests `enabled: true`, toggling requests `false`, the JSON preference is saved, and a failed registry update leaves the previous preference unchanged.

- [ ] **Step 2: Run the tests and verify failure**

Run: `dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj -c Release`

Expected: compilation fails because the view model has no autostart API.

- [ ] **Step 3: Implement the view-model behavior**

Inject `IStartupRegistrationService` into `MainViewModel`. Add `StartWithWindows`, `StartWithWindowsText`, `ReconcileStartupRegistration()`, and `ToggleStartWithWindows()`. Persist only after the registry operation succeeds; on failure preserve the old JSON value and set `StatusMessage`.

- [ ] **Step 4: Add and wire the system setting control**

At the top of the settings card, add a compact “系统” section with a `ToggleButton` bound one-way to `StartWithWindows`, text bound to `StartWithWindowsText`, and description “登录 Windows 后自动启动，并保持右侧触发条状态。” Wire its click through `MainWindow` to `ToggleStartWithWindows()` and call startup reconciliation once after loading.

- [ ] **Step 5: Run tests and commit**

Run: `dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj -c Release`

Expected: all assertions pass.

```powershell
git add src/Horizon.App/ViewModels/MainViewModel.cs src/Horizon.App/MainWindow.xaml src/Horizon.App/MainWindow.xaml.cs tests/Horizon.App.Tests/Program.cs
git commit -m "feat: add autostart setting"
```

### Task 3: Add the notification-area icon and remove the taskbar button

**Files:**
- Create: `src/Horizon.App/Services/TrayIconService.cs`
- Modify: `src/Horizon.App/Horizon.App.csproj`
- Modify: `src/Horizon.App/MainWindow.xaml:1-24`
- Modify: `src/Horizon.App/MainWindow.xaml.cs:1-64,602-637`

- [ ] **Step 1: Enable Windows Forms and implement `TrayIconService`**

Add `<UseWindowsForms>true</UseWindowsForms>`. Create an `IDisposable` service that owns `NotifyIcon`, its icon, and a `ContextMenuStrip`. Set tooltip text to `Horizon`; invoke the supplied open callback only for a left mouse click; add “打开 Horizon” and “退出 Horizon” menu items; and set `Visible = false` before disposing resources. Extract the executable icon when available and fall back to `SystemIcons.Application`.

- [ ] **Step 2: Remove the taskbar entry**

Change `ShowInTaskbar="True"` to `ShowInTaskbar="False"` in `MainWindow.xaml`.

- [ ] **Step 3: Wire tray lifecycle to the panel state machine**

Create the tray service in `MainWindow_OnLoaded`. The open callback dispatches to the WPF thread, calls `SetPanelState(PanelDisplayState.ExpandedPanel)`, then `Activate()` and `Focus()`. The exit callback calls `Application.Current.Shutdown()`. Dispose the tray service in `MainWindow_OnClosed` and tolerate repeated disposal.

- [ ] **Step 4: Build, manually check, and commit**

Run: `dotnet build Horizon.sln -c Release`

Expected: build succeeds with zero errors. Launch the app and confirm: no taskbar button, tray icon visible, left-click expands, right-click shows both commands, and “退出 Horizon” removes the process and icon.

```powershell
git add src/Horizon.App/Services/TrayIconService.cs src/Horizon.App/Horizon.App.csproj src/Horizon.App/MainWindow.xaml src/Horizon.App/MainWindow.xaml.cs
git commit -m "feat: move Horizon to the system tray"
```

### Task 4: Clean the startup registration during uninstall

**Files:**
- Modify: `installer/Horizon.iss`

- [ ] **Step 1: Add uninstall-only registry cleanup**

Add:

```pascal
[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    RegDeleteValue(HKEY_CURRENT_USER,
      'Software\Microsoft\Windows\CurrentVersion\Run', 'Horizon');
end;
```

This removes only the startup value and does not touch `%LocalAppData%\Horizon\data`.

- [ ] **Step 2: Compile and commit the installer change**

Run: `& .\scripts\build-installer.ps1 -Version '0.2.0'`

Expected: `artifacts\installer\Horizon-Setup-v0.2.0-x64.exe` is created.

```powershell
git add installer/Horizon.iss
git commit -m "fix: remove Horizon autostart entry on uninstall"
```

### Task 5: End-to-end verification and packaging

**Files:**
- Verify: `artifacts/installer/Horizon-Setup-v0.2.0-x64.exe`

- [ ] **Step 1: Run automated validation**

```powershell
dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj -c Release
dotnet build Horizon.sln -c Release
```

Expected: tests pass and the solution builds with zero errors.

- [ ] **Step 2: Verify installed behavior**

Install v0.2.0 for the current user. Confirm the executable is self-contained, the taskbar remains clear, the tray icon behaves as specified, and the default-enabled setting creates a quoted `HKCU` Run value pointing to the installed `Horizon.App.exe`.

- [ ] **Step 3: Verify preference persistence and uninstall**

Disable autostart and confirm the Run value disappears. Restart Horizon and confirm it stays disabled. Enable it again and confirm the value returns. Uninstall Horizon and confirm the Run value is gone while `%LocalAppData%\Horizon\data\horizon-data.json` remains.

- [ ] **Step 4: Record artifact integrity**

Run: `Get-FileHash artifacts\installer\Horizon-Setup-v0.2.0-x64.exe -Algorithm SHA256`

Report the absolute path, size, SHA-256, supported OS/architecture, and unsigned SmartScreen caveat.

# Horizon Logo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce the approved C1 neon task-orbit logo and install it consistently across the Horizon executable, tray icon, main panel, installer, and Windows shortcuts.

**Architecture:** Generate a high-resolution visual reference, then keep production output deterministic through a source-controlled SVG and Windows drawing/export script. The WPF project embeds the PNG and ICO, while Inno Setup reuses the same ICO so every Windows surface receives one identity.

**Tech Stack:** GPT Image generation, SVG, PowerShell 5.1, `System.Drawing`, .NET 9 WPF, Inno Setup 6.

---

### Task 1: Generate and approve the visual master

**Files:**
- Create: `src/Horizon.App/Assets/horizon-logo-master.png`

- [ ] **Step 1: Generate the C1 master artwork**

Use the image-generation skill with a square-app-icon prompt specifying: transparent outer corners, dark navy rounded-square plate, cyan-to-electric-blue-to-violet orbit, centered ice-white check, one violet orbital node, restrained glass depth, no text, no letters, no watermark, centered composition, and enough safe margin for Windows icon masks.

- [ ] **Step 2: Inspect the generated image**

Check it at full size and as a small preview. Reject outputs with distorted checkmarks, multiple nodes, noisy particles, text-like artifacts, clipped glow, or weak contrast.

- [ ] **Step 3: Save the approved 1024px master**

Place the image at `src/Horizon.App/Assets/horizon-logo-master.png`. Preserve it as the visual reference; production icon raster sizes are generated separately.

### Task 2: Create deterministic vector and Windows icon assets

**Files:**
- Create: `src/Horizon.App/Assets/horizon-logo.svg`
- Create: `src/Horizon.App/Assets/horizon-logo.png`
- Create: `src/Horizon.App/Assets/Horizon.ico`
- Create: `scripts/build-logo-assets.ps1`

- [ ] **Step 1: Write the SVG source**

Create a 1024-square SVG using the approved geometry and colors. Keep the rounded plate inside a 72px transparent perimeter. Use a 288px-radius orbit with a 68px stroke, a centered rounded check with a 76px stroke, and one violet node. Use restrained blur only for a large-size glow layer.

- [ ] **Step 2: Write the deterministic export script**

Implement `scripts/build-logo-assets.ps1` with `System.Drawing`. Draw the same plate, orbit, check, and node at `1024 px` for `horizon-logo.png`; then render `16, 20, 24, 32, 40, 48, 64, 128, 256 px` variants. At sizes up to 32px, reduce glow and increase core stroke width. Write a valid multi-image ICO directory and PNG-compressed image entries to `Horizon.ico`.

- [ ] **Step 3: Validate generated assets**

The script must verify that every expected size exists in the ICO table, the PNG is exactly `1024 × 1024`, the ICO header uses type `1`, and all output files are non-empty. It prints file paths, dimensions, frame count, and byte sizes.

- [ ] **Step 4: Run the exporter**

Run: `& .\scripts\build-logo-assets.ps1`

Expected: `horizon-logo.png` and `Horizon.ico` are regenerated successfully with nine ICO frames.

### Task 3: Integrate the logo into WPF and the installer

**Files:**
- Modify: `src/Horizon.App/Horizon.App.csproj`
- Modify: `src/Horizon.App/MainWindow.xaml:623-646`
- Modify: `installer/Horizon.iss:18-46`

- [ ] **Step 1: Configure executable and resource icons**

Add `<ApplicationIcon>Assets\Horizon.ico</ApplicationIcon>` and a WPF `<Resource Include="Assets\horizon-logo.png" />` item. Confirm the generated EXE exposes the new icon.

- [ ] **Step 2: Add the logo to the panel header**

Replace the left header stack with a two-column grid: a `26 × 26` `Image` sourced from `/Assets/horizon-logo.png`, followed by the existing title/subtitle stack. Preserve the right-side pin button, top margin, and current header height.

- [ ] **Step 3: Configure the installer icon**

Add `SetupIconFile=..\src\Horizon.App\Assets\Horizon.ico` to `[Setup]`. Keep `UninstallDisplayIcon={app}\Horizon.App.exe`, so uninstall and shortcut icons come from the branded executable.

- [ ] **Step 4: Build and inspect**

Run: `dotnet build Horizon.sln -c Release`

Expected: zero warnings and zero errors. Launch the app and verify the panel header logo is not clipped, the tray icon uses the new glyph, and the taskbar remains empty.

### Task 4: Package and verify v0.2.1

**Files:**
- Verify: `artifacts/installer/Horizon-Setup-v0.2.1-x64.exe`

- [ ] **Step 1: Run automated tests**

Run: `dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj -c Release`

Expected: `Panel layout tests passed.`

- [ ] **Step 2: Build the self-contained installer**

Run: `& .\scripts\build-installer.ps1 -Version '0.2.1'`

Expected: `artifacts\installer\Horizon-Setup-v0.2.1-x64.exe` is created.

- [ ] **Step 3: Verify installation surfaces**

Install into a temporary verification directory. Confirm the installed EXE, installer, Start menu shortcut, optional desktop shortcut, tray icon, and panel header use the C1 logo. Confirm uninstall removes the startup value but preserves `%LocalAppData%\Horizon\data`.

- [ ] **Step 4: Record integrity**

Run `Get-FileHash artifacts\installer\Horizon-Setup-v0.2.1-x64.exe -Algorithm SHA256` and report the absolute path, size, SHA-256, supported OS/architecture, and unsigned SmartScreen caveat.

### Task 5: Increase the optical size of tray icon frames

**Files:**
- Modify: `scripts/build-logo-assets.ps1`
- Regenerate: `src/Horizon.App/Assets/Horizon.ico`
- Verify: `artifacts/installer/Horizon-Setup-v0.2.2-x64.exe`

- [ ] **Step 1: Add a small-frame geometry profile**

For sizes up to `32 px`, reduce plate margin from `7%` to `2.5%`, increase orbit radius from `27.5%` to `32.5%`, increase orbit stroke to `9%`, expand the check endpoints around the center, increase check stroke to `10.5%`, and move/enlarge the node to remain attached to the larger orbit. Keep glow disabled for these frames.

- [ ] **Step 2: Preserve larger artwork**

Keep the 1024px PNG and all frames of `40 px` or larger on the existing geometry profile, so the main-panel Logo and visual master do not change.

- [ ] **Step 3: Regenerate and inspect the ICO**

Run: `& .\scripts\build-logo-assets.ps1`

Expected: nine ICO frames are emitted. Extract the executable icon after publishing and verify the `16–32 px` glyph fills the tray box without touching its edges.

- [ ] **Step 4: Rebuild and verify the installer**

Run: `& .\scripts\build-installer.ps1 -Version '0.2.2'`

Expected: tests and Release publish pass, and `artifacts\installer\Horizon-Setup-v0.2.2-x64.exe` contains the enlarged tray icon.

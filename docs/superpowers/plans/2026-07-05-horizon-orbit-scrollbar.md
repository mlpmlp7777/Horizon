# Horizon Orbit Blue Scrollbar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the default WPF scrollbars throughout Horizon with the approved lightweight Orbit Blue capsule style without changing scrolling behavior.

**Architecture:** Add one implicit `ScrollBar` style and reusable paging-button, thumb, vertical-template, and horizontal-template resources to `MainWindow.xaml`. Protect the visual contract with lightweight source-markup assertions in the existing executable test project; WPF compilation then validates the templates and bindings.

**Tech Stack:** .NET 9, WPF XAML, C#, existing `Horizon.App.Tests` executable test harness

---

## File map

- Modify `src/Horizon.App/MainWindow.xaml`: define the shared Orbit Blue scrollbar resources and apply them implicitly to every scrollbar in the window.
- Modify `tests/Horizon.App.Tests/Program.cs`: assert that both orientations, capsule sizing, hover/drag states, and page-scroll commands remain present in the XAML contract.
- No ViewModel, model, persistence, code-behind, or `AGENTS.md` changes are required.

### Task 1: Add the scrollbar visual-contract test

**Files:**
- Modify: `tests/Horizon.App.Tests/Program.cs:503`

- [ ] **Step 1: Add source-markup assertions before the final success message**

Insert this block immediately before `Console.WriteLine("Panel layout tests passed.");`:

```csharp
var mainWindowXamlPath = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory,
    "..", "..", "..", "..", "..",
    "src", "Horizon.App", "MainWindow.xaml"));
var mainWindowXaml = File.ReadAllText(mainWindowXamlPath);

AssertContains("x:Key=\"OrbitScrollThumbStyle\"", mainWindowXaml,
    "scrollbar uses a reusable capsule thumb");
AssertContains("x:Key=\"OrbitVerticalScrollBarTemplate\"", mainWindowXaml,
    "vertical scrollbar template exists");
AssertContains("x:Key=\"OrbitHorizontalScrollBarTemplate\"", mainWindowXaml,
    "horizontal scrollbar template exists");
AssertContains("<Style TargetType=\"ScrollBar\">", mainWindowXaml,
    "scrollbar style applies implicitly throughout the window");
AssertContains("MinHeight=\"30\"", mainWindowXaml,
    "vertical thumb keeps a usable minimum length");
AssertContains("MinWidth=\"30\"", mainWindowXaml,
    "horizontal thumb keeps a usable minimum length");
AssertContains("Property=\"IsMouseOver\" Value=\"True\"", mainWindowXaml,
    "scrollbar has a hover state");
AssertContains("Property=\"IsDragging\" Value=\"True\"", mainWindowXaml,
    "scrollbar has a dragging state");
AssertContains("ScrollBar.PageUpCommand", mainWindowXaml,
    "vertical track preserves page-up behavior");
AssertContains("ScrollBar.PageDownCommand", mainWindowXaml,
    "vertical track preserves page-down behavior");
AssertContains("ScrollBar.PageLeftCommand", mainWindowXaml,
    "horizontal track preserves page-left behavior");
AssertContains("ScrollBar.PageRightCommand", mainWindowXaml,
    "horizontal track preserves page-right behavior");
```

Add this helper immediately before `AssertThrows<TException>`:

```csharp
static void AssertContains(string expected, string actual, string name)
{
    if (!actual.Contains(expected, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"{name}: missing {expected}");
    }
}
```

- [ ] **Step 2: Run the test and verify that the new contract fails**

Run:

```powershell
dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj
```

Expected: the process fails at `scrollbar uses a reusable capsule thumb` because `OrbitScrollThumbStyle` has not been added yet.

### Task 2: Implement the Orbit Blue scrollbar resources

**Files:**
- Modify: `src/Horizon.App/MainWindow.xaml:48`

- [ ] **Step 1: Add the transparent paging-button and capsule-thumb styles**

Insert these resources after the converters and before the implicit `TextBlock` style:

```xml
<Style x:Key="OrbitScrollPageButtonStyle" TargetType="RepeatButton">
    <Setter Property="Focusable" Value="False" />
    <Setter Property="IsTabStop" Value="False" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="RepeatButton">
                <Border Background="Transparent" />
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>

<Style x:Key="OrbitScrollThumbStyle" TargetType="Thumb">
    <Setter Property="Background" Value="{StaticResource OrbitPrimaryBrush}" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Thumb">
                <Border Background="{TemplateBinding Background}"
                        CornerRadius="4" />
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

- [ ] **Step 2: Add the complete vertical scrollbar template**

Place this template after `OrbitScrollThumbStyle`:

```xml
<ControlTemplate x:Key="OrbitVerticalScrollBarTemplate" TargetType="ScrollBar">
    <Grid Width="12" Background="Transparent">
        <Border x:Name="TrackChrome"
                Width="10"
                HorizontalAlignment="Center"
                Background="Transparent"
                CornerRadius="5" />
        <Track x:Name="PART_Track"
               IsDirectionReversed="True"
               Orientation="Vertical">
            <Track.DecreaseRepeatButton>
                <RepeatButton Command="{x:Static ScrollBar.PageUpCommand}"
                              Style="{StaticResource OrbitScrollPageButtonStyle}" />
            </Track.DecreaseRepeatButton>
            <Track.Thumb>
                <Thumb x:Name="ScrollThumb"
                       Width="6"
                       MinHeight="30"
                       HorizontalAlignment="Center"
                       Style="{StaticResource OrbitScrollThumbStyle}" />
            </Track.Thumb>
            <Track.IncreaseRepeatButton>
                <RepeatButton Command="{x:Static ScrollBar.PageDownCommand}"
                              Style="{StaticResource OrbitScrollPageButtonStyle}" />
            </Track.IncreaseRepeatButton>
        </Track>
    </Grid>
    <ControlTemplate.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter TargetName="TrackChrome"
                    Property="Background"
                    Value="{StaticResource OrbitCollapsedCardBrush}" />
            <Setter TargetName="ScrollThumb" Property="Width" Value="8" />
        </Trigger>
        <Trigger SourceName="ScrollThumb" Property="IsDragging" Value="True">
            <Setter TargetName="ScrollThumb"
                    Property="Background"
                    Value="{StaticResource OrbitAccentInkBrush}" />
        </Trigger>
        <Trigger Property="IsEnabled" Value="False">
            <Setter TargetName="ScrollThumb" Property="Opacity" Value="0.4" />
        </Trigger>
    </ControlTemplate.Triggers>
</ControlTemplate>
```

- [ ] **Step 3: Add the complete horizontal scrollbar template**

Place this template after `OrbitVerticalScrollBarTemplate`:

```xml
<ControlTemplate x:Key="OrbitHorizontalScrollBarTemplate" TargetType="ScrollBar">
    <Grid Height="12" Background="Transparent">
        <Border x:Name="TrackChrome"
                Height="10"
                VerticalAlignment="Center"
                Background="Transparent"
                CornerRadius="5" />
        <Track x:Name="PART_Track"
               IsDirectionReversed="False"
               Orientation="Horizontal">
            <Track.DecreaseRepeatButton>
                <RepeatButton Command="{x:Static ScrollBar.PageLeftCommand}"
                              Style="{StaticResource OrbitScrollPageButtonStyle}" />
            </Track.DecreaseRepeatButton>
            <Track.Thumb>
                <Thumb x:Name="ScrollThumb"
                       Height="6"
                       MinWidth="30"
                       VerticalAlignment="Center"
                       Style="{StaticResource OrbitScrollThumbStyle}" />
            </Track.Thumb>
            <Track.IncreaseRepeatButton>
                <RepeatButton Command="{x:Static ScrollBar.PageRightCommand}"
                              Style="{StaticResource OrbitScrollPageButtonStyle}" />
            </Track.IncreaseRepeatButton>
        </Track>
    </Grid>
    <ControlTemplate.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter TargetName="TrackChrome"
                    Property="Background"
                    Value="{StaticResource OrbitCollapsedCardBrush}" />
            <Setter TargetName="ScrollThumb" Property="Height" Value="8" />
        </Trigger>
        <Trigger SourceName="ScrollThumb" Property="IsDragging" Value="True">
            <Setter TargetName="ScrollThumb"
                    Property="Background"
                    Value="{StaticResource OrbitAccentInkBrush}" />
        </Trigger>
        <Trigger Property="IsEnabled" Value="False">
            <Setter TargetName="ScrollThumb" Property="Opacity" Value="0.4" />
        </Trigger>
    </ControlTemplate.Triggers>
</ControlTemplate>
```

- [ ] **Step 4: Add the implicit orientation-aware scrollbar style**

Place this style after both templates:

```xml
<Style TargetType="ScrollBar">
    <Setter Property="Width" Value="12" />
    <Setter Property="MinWidth" Value="12" />
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="Foreground" Value="{StaticResource OrbitPrimaryBrush}" />
    <Setter Property="Template" Value="{StaticResource OrbitVerticalScrollBarTemplate}" />
    <Style.Triggers>
        <Trigger Property="Orientation" Value="Horizontal">
            <Setter Property="Width" Value="Auto" />
            <Setter Property="MinWidth" Value="0" />
            <Setter Property="Height" Value="12" />
            <Setter Property="MinHeight" Value="12" />
            <Setter Property="Template" Value="{StaticResource OrbitHorizontalScrollBarTemplate}" />
        </Trigger>
    </Style.Triggers>
</Style>
```

- [ ] **Step 5: Run the test and verify the visual contract passes**

Run:

```powershell
dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj
```

Expected: exit code `0` and `Panel layout tests passed.`

- [ ] **Step 6: Build the WPF solution to validate templates and bindings**

Run:

```powershell
dotnet build Horizon.sln --disable-build-servers -nodeReuse:false -p:UseSharedCompilation=false
```

Expected: `Build succeeded.` with `0 Warning(s)` and `0 Error(s)`.

- [ ] **Step 7: Commit the test and implementation together**

```powershell
git add -- src/Horizon.App/MainWindow.xaml tests/Horizon.App.Tests/Program.cs
git commit -m "style: add Orbit Blue scrollbars"
```

### Task 3: Publish and perform interaction QA

**Files:**
- Verify: `artifacts/Horizon.App/Horizon.App.exe`

- [ ] **Step 1: Publish a runnable Release build**

Run:

```powershell
dotnet publish src\Horizon.App\Horizon.App.csproj -c Release -o artifacts\Horizon.App --disable-build-servers -nodeReuse:false -p:UseSharedCompilation=false
```

Expected: publish succeeds and `artifacts\Horizon.App\Horizon.App.exe` exists.

- [ ] **Step 2: Verify the main task panel manually**

Open `artifacts\Horizon.App\Horizon.App.exe`, add enough weekly and long-term tasks to overflow, then verify:

1. The track is transparent at rest and the thumb is a centered `6px` blue capsule.
2. Moving the pointer over the `12px` scrollbar area reveals a subtle pale-blue track and expands the thumb to `8px`.
3. Dragging changes the thumb to the darker Orbit Blue and releasing restores the hover state.
4. Mouse wheel, track click, thumb drag, touchpad, Page Up, Page Down, Home, and End still move content correctly.
5. Project expand/collapse controls and task buttons remain clickable and are not covered by the scrollbar.

- [ ] **Step 3: Verify secondary scroll regions manually**

Open history and settings/project-management views, create overflow where needed, and verify the same rest, hover, drag, and no-overlap behavior. Confirm that short lists do not reserve a visible empty rail when `VerticalScrollBarVisibility="Auto"` hides the scrollbar.

- [ ] **Step 4: Check the final diff scope**

Run:

```powershell
git status --short
git diff --check HEAD~1..HEAD
git show --stat --oneline HEAD
```

Expected: the feature commit contains only `src/Horizon.App/MainWindow.xaml` and `tests/Horizon.App.Tests/Program.cs`; unrelated untracked files, including `AGENTS.md`, remain untouched.

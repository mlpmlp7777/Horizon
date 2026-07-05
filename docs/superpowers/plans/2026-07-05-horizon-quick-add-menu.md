# Horizon Quick Add Menu Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the default vertical quick-add context menu with the approved Orbit Blue horizontal two-card selector.

**Architecture:** Add keyed WPF styles for the quick-add `ContextMenu` and its two `MenuItem` cards so no future menus are changed implicitly. Keep menu construction in `MainWindow.xaml.cs`, but apply the keyed resources, set deterministic placement below the button, and attach type glyphs plus accessibility text while preserving the existing task-editor callbacks.

**Tech Stack:** .NET 9, WPF XAML, C#, existing executable `Horizon.App.Tests` harness

---

## File map

- Modify `src/Horizon.App/MainWindow.xaml`: add `QuickAddContextMenuStyle` and `QuickAddMenuItemStyle` resources.
- Modify `src/Horizon.App/MainWindow.xaml.cs`: construct and position the styled menu and build its two accessible cards.
- Modify `tests/Horizon.App.Tests/Program.cs`: protect the resource, layout, placement, labels, glyphs, and callbacks with source-contract assertions.
- Do not modify `AGENTS.md`, ViewModels, models, persistence, empty-state buttons, or project-expansion code.

### Task 1: Add failing quick-add menu contract tests

**Files:**
- Modify: `tests/Horizon.App.Tests/Program.cs`

- [ ] **Step 1: Load `MainWindow.xaml.cs` next to the existing XAML source contract**

Insert after `var mainWindowXaml = File.ReadAllText(mainWindowXamlPath);`:

```csharp
var mainWindowCodeBehindPath = Path.ChangeExtension(mainWindowXamlPath, ".xaml.cs");
var mainWindowCodeBehind = File.ReadAllText(mainWindowCodeBehindPath);
```

- [ ] **Step 2: Add exact visual and wiring assertions**

Insert before the existing `XDocument` template checks:

```csharp
AssertContains("x:Key=\"QuickAddContextMenuStyle\"", mainWindowXaml,
    "quick-add menu has a dedicated Orbit Blue style");
AssertContains("x:Key=\"QuickAddMenuItemStyle\"", mainWindowXaml,
    "quick-add items have a dedicated card style");
AssertContains("<StackPanel Orientation=\"Horizontal\"", mainWindowXaml,
    "quick-add items are arranged in two columns");
AssertContains("CornerRadius=\"18\"", mainWindowXaml,
    "quick-add menu uses the approved outer radius");
AssertContains("Width=\"104\"", mainWindowXaml,
    "quick-add cards use the approved width");
AssertContains("Height=\"76\"", mainWindowXaml,
    "quick-add cards use the approved height");
AssertContains("Placement = PlacementMode.Bottom", mainWindowCodeBehind,
    "quick-add menu opens below its button");
AssertContains("PlacementTarget = QuickAddButton", mainWindowCodeBehind,
    "quick-add menu anchors to the quick-add button");
AssertContains("\"新建本周任务\", \"周\", \"添加到当前周", mainWindowCodeBehind,
    "weekly quick-add card keeps its full label and glyph");
AssertContains("\"新建长期任务\", \"∞\", \"设置起止时间", mainWindowCodeBehind,
    "long-term quick-add card keeps its full label and glyph");
AssertContains("_viewModel.OpenCreateWeeklyTask()", mainWindowCodeBehind,
    "weekly quick-add card keeps its editor callback");
AssertContains("_viewModel.OpenCreateLongTermTask()", mainWindowCodeBehind,
    "long-term quick-add card keeps its editor callback");
```

- [ ] **Step 3: Run the tests and verify the contract fails**

Run `dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj`.

Expected: failure reporting the missing `QuickAddContextMenuStyle` resource.

### Task 2: Implement the horizontal Orbit Blue menu

**Files:**
- Modify: `src/Horizon.App/MainWindow.xaml:190`
- Modify: `src/Horizon.App/MainWindow.xaml.cs:1-6,158-166,763-768`

- [ ] **Step 1: Add the keyed `ContextMenu` style before the implicit `TextBlock` style**

```xml
<Style x:Key="QuickAddContextMenuStyle" TargetType="ContextMenu">
    <Setter Property="Background" Value="{StaticResource OrbitExpandedCardBrush}" />
    <Setter Property="BorderBrush" Value="{StaticResource OrbitShellBorderBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="HasDropShadow" Value="False" />
    <Setter Property="ItemsPanel">
        <Setter.Value>
            <ItemsPanelTemplate>
                <StackPanel Orientation="Horizontal" />
            </ItemsPanelTemplate>
        </Setter.Value>
    </Setter>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="ContextMenu">
                <Border Padding="8"
                        Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="18">
                    <ItemsPresenter />
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

- [ ] **Step 2: Add the keyed two-card `MenuItem` style after the context-menu style**

```xml
<Style x:Key="QuickAddMenuItemStyle" TargetType="MenuItem">
    <Setter Property="Width" Value="104" />
    <Setter Property="Height" Value="76" />
    <Setter Property="Margin" Value="3" />
    <Setter Property="Background" Value="{StaticResource OrbitTaskBrush}" />
    <Setter Property="BorderBrush" Value="Transparent" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="Foreground" Value="{StaticResource OrbitInkBrush}" />
    <Setter Property="Cursor" Value="Hand" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="MenuItem">
                <Border x:Name="MenuCard"
                        Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="13">
                    <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                        <Border x:Name="IconCard" Width="30" Height="30"
                                HorizontalAlignment="Center"
                                Background="{StaticResource OrbitHeaderBrush}"
                                CornerRadius="11">
                            <TextBlock x:Name="IconText"
                                       HorizontalAlignment="Center" VerticalAlignment="Center"
                                       Text="{TemplateBinding Tag}" FontSize="13" FontWeight="Bold"
                                       Foreground="{StaticResource OrbitPrimaryBrush}" />
                        </Border>
                        <TextBlock x:Name="MenuLabel" Margin="4,7,4,0"
                                   HorizontalAlignment="Center"
                                   Text="{TemplateBinding Header}" TextAlignment="Center"
                                   FontSize="10.5" FontWeight="SemiBold"
                                   Foreground="{TemplateBinding Foreground}" />
                    </StackPanel>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsHighlighted" Value="True">
                        <Setter TargetName="MenuCard" Property="Background" Value="{StaticResource OrbitHeaderBrush}" />
                        <Setter TargetName="MenuCard" Property="BorderBrush" Value="{StaticResource OrbitExpandedBorderBrush}" />
                    </Trigger>
                    <Trigger Property="IsMouseCaptureWithin" Value="True">
                        <Setter TargetName="MenuCard" Property="Background" Value="{StaticResource OrbitPrimaryBrush}" />
                        <Setter TargetName="MenuCard" Property="BorderBrush" Value="{StaticResource OrbitPrimaryBrush}" />
                        <Setter TargetName="MenuLabel" Property="Foreground" Value="White" />
                    </Trigger>
                    <Trigger Property="IsEnabled" Value="False">
                        <Setter TargetName="MenuCard" Property="Opacity" Value="0.4" />
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

- [ ] **Step 3: Import the placement and automation namespaces in code-behind**

```csharp
using System.Windows.Automation;
using System.Windows.Controls.Primitives;
```

- [ ] **Step 4: Replace `QuickAddButton_OnClick` with deterministic styled-menu construction**

```csharp
private void QuickAddButton_OnClick(object sender, RoutedEventArgs e)
{
    var menu = new ContextMenu
    {
        Style = (Style)FindResource("QuickAddContextMenuStyle"),
        Placement = PlacementMode.Bottom,
        PlacementTarget = QuickAddButton,
        HorizontalOffset = 0,
        VerticalOffset = 6
    };
    menu.Items.Add(BuildQuickAddMenuItem(
        "新建本周任务", "周", "添加到当前周，并归入所选项目",
        (_, _) => _viewModel.OpenCreateWeeklyTask()));
    menu.Items.Add(BuildQuickAddMenuItem(
        "新建长期任务", "∞", "设置起止时间，持续跟踪长期进度",
        (_, _) => _viewModel.OpenCreateLongTermTask()));

    QuickAddButton.ContextMenu = menu;
    menu.IsOpen = true;
}
```

- [ ] **Step 5: Replace `BuildMenuItem` with the resource-aware accessible builder**

```csharp
private MenuItem BuildQuickAddMenuItem(
    string header,
    string glyph,
    string helpText,
    RoutedEventHandler onClick)
{
    var item = new MenuItem
    {
        Header = header,
        Tag = glyph,
        Style = (Style)FindResource("QuickAddMenuItemStyle")
    };
    AutomationProperties.SetName(item, header);
    AutomationProperties.SetHelpText(item, helpText);
    item.Click += onClick;
    return item;
}
```

- [ ] **Step 6: Run tests and compile the WPF templates**

Run:

```powershell
dotnet run --project tests\Horizon.App.Tests\Horizon.App.Tests.csproj
dotnet build Horizon.sln --disable-build-servers -nodeReuse:false -p:UseSharedCompilation=false
```

Expected: tests print `Panel layout tests passed.` and build finishes with zero errors.

- [ ] **Step 7: Commit the implementation**

```powershell
git add -- src/Horizon.App/MainWindow.xaml src/Horizon.App/MainWindow.xaml.cs tests/Horizon.App.Tests/Program.cs
git commit -m "style: redesign quick add menu"
```

### Task 3: Publish and interaction QA

**Files:**
- Verify: `artifacts/Horizon.App/Horizon.App.exe`

- [ ] **Step 1: Publish Release**

Run `dotnet publish src\Horizon.App\Horizon.App.csproj -c Release -o artifacts\Horizon.App --disable-build-servers -nodeReuse:false -p:UseSharedCompilation=false`.

Expected: `artifacts\Horizon.App\Horizon.App.exe` exists.

- [ ] **Step 2: Verify the visible flow**

Open the published executable and verify: the menu appears below “新增内容”; both cards are equal width; full labels and glyphs render; hover and click feedback use Orbit Blue; each card opens the correct editor; `Esc` and outside click close the menu; keyboard focus can select both cards.

- [ ] **Step 3: Verify scope**

Run `git diff --check HEAD~1..HEAD` and `git show --stat --oneline HEAD`. Confirm only `MainWindow.xaml`, `MainWindow.xaml.cs`, and `Program.cs` changed and that `AGENTS.md` remains untouched.

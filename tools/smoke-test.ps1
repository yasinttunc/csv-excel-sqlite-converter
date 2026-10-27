<#
.SYNOPSIS
  Starts the built desktop app twice through UI Automation: once with a CSV file, once with a SQLite database,
  checks what it shows and saves screenshots of the app window. Used by CI on windows-latest.
#>
param(
    [Parameter(Mandatory = $true)] [string] $Exe,
    [Parameter(Mandatory = $true)] [string] $Csv,
    [Parameter(Mandatory = $true)] [string] $Database,
    [string] $OutDir = "smoke"
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type -Namespace Smoke -Name Native -MemberDefinition @'
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct Rect { public int Left; public int Top; public int Right; public int Bottom; }
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern System.IntPtr GetForegroundWindow();
[System.Runtime.InteropServices.DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(System.IntPtr hwnd, int attribute, out Rect rect, int size);
'@
$AE = [System.Windows.Automation.AutomationElement]
$Scope = [System.Windows.Automation.TreeScope]
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$checks = @()

function Cond($Property, $Value) { New-Object System.Windows.Automation.PropertyCondition($Property, $Value) }

function Shot([string] $Name) {
    $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $r = New-Object Smoke.Native+Rect
    if ([Smoke.Native]::DwmGetWindowAttribute([Smoke.Native]::GetForegroundWindow(), 9, [ref] $r, 16) -eq 0) {
        $b = New-Object System.Drawing.Rectangle $r.Left, $r.Top, ($r.Right - $r.Left), ($r.Bottom - $r.Top)
    }
    $bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
    $bmp.Save((Join-Path $OutDir "$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
}

function Start-App([string] $Argument) {
    $p = Start-Process -FilePath $Exe -ArgumentList "`"$Argument`"" -PassThru
    $deadline = (Get-Date).AddSeconds(60)
    while ((Get-Date) -lt $deadline) {
        if ($p.HasExited) { throw "The app exited with code $($p.ExitCode)." }
        $error1 = $AE::RootElement.FindFirst($Scope::Children, (Cond $AE::NameProperty "Data Converter - error"))
        if ($error1) { Shot "error"; throw "The app showed an error dialog." }
        $w = $AE::RootElement.FindFirst($Scope::Children, (New-Object System.Windows.Automation.AndCondition(
            (Cond $AE::ProcessIdProperty $p.Id), (Cond $AE::NameProperty "Data Converter"))))
        if ($w) {
            $w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).SetWindowVisualState(
                [System.Windows.Automation.WindowVisualState]::Maximized)
            Start-Sleep -Seconds 3
            return @{ Process = $p; Window = $w }
        }
        Start-Sleep -Milliseconds 500
    }
    throw "The main window did not appear."
}

function Find($Parent, $Property, [string] $Value) {
    $e = $Parent.FindFirst($Scope::Descendants, (Cond $Property $Value))
    if (-not $e) { throw "'$Value' not found." }
    return $e
}

function Rows($Window, [string] $AutomationId) {
    (Find $Window $AE::AutomationIdProperty $AutomationId).GetCurrentPattern([System.Windows.Automation.GridPattern]::Pattern).Current.RowCount
}

function Text($Window, [string] $Name) {
    $e = Find $Window $AE::NameProperty $Name
    $vp = $null
    if ($e.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref] $vp)) { return $vp.Current.Value }
    $tp = $null
    if ($e.TryGetCurrentPattern([System.Windows.Automation.TextPattern]::Pattern, [ref] $tp)) { return $tp.DocumentRange.GetText(-1) }
    return $e.Current.Name
}

# 1. A Turkish Excel-style CSV (semicolons, Windows-1254) opened from the command line.
$app = Start-App (Resolve-Path $Csv).Path
try {
    $rows = Rows $app.Window "PreviewGrid"
    if ($rows -ne 48) { throw "Preview shows $rows rows, expected 48." }
    $checks += "CSV preview has 48 rows"
    Shot "01-convert"

    $split = Find $app.Window $AE::NameProperty "Split by column"
    $split.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
    Start-Sleep -Milliseconds 500
    (Find $split $AE::NameProperty "Şube").GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $split.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Collapse()
    Start-Sleep -Seconds 1
    $checks += "split-by list offers the CSV's columns"
    Shot "02-import-options"
}
finally { if (-not $app.Process.HasExited) { Stop-Process -Id $app.Process.Id -Force } }

# 2. A SQLite database made by the CLI, opened from the command line.
$app = Start-App (Resolve-Path $Database).Path
try {
    $tables = Find $app.Window $AE::NameProperty "Tables"
    $items = $tables.FindAll($Scope::Children, (Cond $AE::ControlTypeProperty ([System.Windows.Automation.ControlType]::ListItem)))
    if ($items.Count -ne 4) { throw "Browse shows $($items.Count) tables, expected 4." }
    $rows = Rows $app.Window "TableGrid"
    if ($rows -ne 12) { throw "Table grid shows $rows rows, expected 12." }
    $checks += "database browser lists 4 tables, first has 12 rows"
    Shot "03-browse"
}
finally { if (-not $app.Process.HasExited) { Stop-Process -Id $app.Process.Id -Force } }

Write-Host "::notice title=UI smoke test::$($checks -join '; ')."

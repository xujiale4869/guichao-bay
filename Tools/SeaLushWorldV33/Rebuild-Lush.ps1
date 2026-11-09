param(
    [string]$Blender = 'C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe',
    [string]$Unity = 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe',
    [switch]$BakeOnly
)

$ErrorActionPreference = 'Stop'
$project = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (Get-Process Unity,Guichao-SeaLushV33 -ErrorAction SilentlyContinue) {
    throw 'Close the Unity editor or v33 player before this build.'
}
if (!(Test-Path -LiteralPath $Blender) -or !(Test-Path -LiteralPath $Unity)) {
    throw 'Set the Blender and Unity executable paths.'
}

Push-Location -LiteralPath $project
try {
    [IO.Directory]::CreateDirectory((Join-Path $project 'Temp')) | Out-Null
    $stamp = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')
    $generator = Join-Path $PSScriptRoot 'generate_land_and_sea_life_v33.py'
    $blenderLog = Join-Path $project "Temp/lush-v33-blender-$stamp.log"
    & $Blender -b --python-exit-code 13 -P $generator -- --project $project *> $blenderLog
    if ($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath $blenderLog -SimpleMatch 'LUSH_V33_MESHES 10 ' -Quiet)) {
        throw ('Blender generation failed. See ' + $blenderLog)
    }
    $method = if ($BakeOnly) { 'Prepare' } else { 'PrepareAndBuild' }
    $unityLog = Join-Path $project "Temp/lush-v33-unity-$stamp.log"
    $arguments = @('-batchmode','-quit','-projectPath',('"' + $project + '"'),
        '-executeMethod',('Deepsea.Editor.SeaLushBuildV33.' + $method),'-logFile',('"' + $unityLog + '"'))
    $process = Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru
    $null = $process.Handle
    $process.WaitForExit()
    $process.Refresh()
    $marker = if ($BakeOnly) { 'LUSH_V33_BAKED models=10' } else { 'LUSH_V33_BUILD_OK' }
    if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $unityLog -SimpleMatch $marker -Quiet)) {
        throw ('Unity bake or build failed. See ' + $unityLog)
    }
    Write-Output ($marker + ' ' + $unityLog)
} finally { Pop-Location }

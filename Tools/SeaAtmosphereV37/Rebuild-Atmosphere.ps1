param([string]$Unity='C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
Import-Module 'C:/Windows/System32/WindowsPowerShell/v1.0/Modules/Microsoft.PowerShell.Utility/Microsoft.PowerShell.Utility.psd1'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Push-Location -LiteralPath $root
try {
 if(Get-Process Unity,Guichao-SeaAtmosphereV37 -ErrorAction SilentlyContinue){throw 'Unity or v37 player is active.'}
 $log=Join-Path $root ('Temp/sea-atmosphere-v37-unity-'+[DateTime]::Now.ToString('yyyyMMdd-HHmmss')+'.log')
 [IO.Directory]::CreateDirectory((Split-Path $log -Parent)) | Out-Null
 $args=@('-batchmode','-nographics','-quit','-projectPath',('"'+$root+'"'),'-buildTarget','Win64','-executeMethod','Deepsea.Editor.SeaVillageBuildV31.BuildAtmosphere','-logFile',('"'+$log+'"'))
 $p=Start-Process -FilePath $Unity -ArgumentList $args -WorkingDirectory $root -PassThru -Wait
 if($p.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern 'SEA_ATMOSPHERE_V37_BUILD_OK' -Quiet)){throw "Unity build failed ($($p.ExitCode)). $log"}
 Write-Output "SEA_ATMOSPHERE_V37_BUILD_OK $log"
}finally{Pop-Location}

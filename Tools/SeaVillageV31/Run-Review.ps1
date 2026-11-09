param([string]$Evidence='village-v31-review06',[switch]$Native,[switch]$CoralContrast,[switch]$Craft,[switch]$Lush,[switch]$CoralSurvey,[int]$TableSample=-1,[int]$AnemoneSample=-1)
$ErrorActionPreference='Stop'
Import-Module 'C:/Windows/System32/WindowsPowerShell/v1.0/Modules/Microsoft.PowerShell.Utility/Microsoft.PowerShell.Utility.psd1'
if($Evidence -notmatch '^village-v31-[a-z0-9-]+$'){throw 'Invalid evidence folder'}
$r=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Push-Location $r
try{
 if(Get-Process Unity,Guichao-SeaVillageV31,Guichao-SeaVillageV31.1,Guichao-SeaCraftV32,Guichao-SeaLushV33 -ErrorAction SilentlyContinue){throw 'Existing Unity or v31 game is active'}
 $folder=$r+'/Verification/'+$Evidence;$log=$r+'/Temp/'+$Evidence+'.log'
 if(Test-Path -LiteralPath ($folder+'/result.json')){throw 'Evidence result already exists; preserve it and select a new round'}
 $args=@('-village-v31','-village-v31-check','-seamless-v29-dir',('Verification/'+$Evidence),'-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+$log+'"'))
 if($Native){$args+='-seamless-v29-native'}else{$args+='-seamless-v29-720'}
 if($CoralSurvey){$args+='-coral-survey'}
 if($TableSample -ge 0){$args+=@('-coral-table-coral-sample',[string]$TableSample)}
 if($AnemoneSample -ge 0){$args+=@('-coral-anemone-sample',[string]$AnemoneSample)}

 $binary=if($Lush){'Builds/SeaLushV33/Guichao-SeaLushV33.exe'}elseif($Craft){'Builds/SeaCraftV32/Guichao-SeaCraftV32.exe'}elseif($CoralContrast){'Builds/SeaVillageV31.1/Guichao-SeaVillageV31.1.exe'}else{'Builds/SeaVillageV31/Guichao-SeaVillageV31.exe'}
 $p=Start-Process ($r+'/'+$binary) -ArgumentList $args -WorkingDirectory $r -PassThru
  $null=$p.Handle
 [IO.File]::WriteAllText($r+'/Temp/'+$Evidence+'-pid.txt',[string]$p.Id)
 $driver=$null
 if($Native){$driver=Start-Process 'C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe' -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$r+'/Tools/SeaUnifiedV30/Test-NativeUI.ps1"'),'-TargetPid',$p.Id,'-Evidence',('"'+$folder+'"')) -WorkingDirectory $r -RedirectStandardOutput ($r+'/Temp/'+$Evidence+'-driver.log') -RedirectStandardError ($r+'/Temp/'+$Evidence+'-driver-error.log') -PassThru;$null=$driver.Handle}
 $started=[DateTime]::UtcNow;$reason=''
 while(!$p.WaitForExit(500)){
  $age=([DateTime]::UtcNow-$started).TotalSeconds
  if($age -gt 300){$reason='External 300-second test deadline';break}
  if($age -gt 12 -and (Test-Path -LiteralPath $log)){
   $fs=[IO.File]::Open($log,'Open','Read','ReadWrite');$reader=New-Object IO.StreamReader($fs)
   try{$text=$reader.ReadToEnd()}finally{$reader.Dispose();$fs.Dispose()}
   if(!$text.Contains('SEAMLESS_V29_READY') -and $text -match '(?m)^\w*Exception:'){$reason='Exception before world READY; see player log';break}
  }
 }
 if($reason){if(!$p.HasExited){$p.Kill();$p.WaitForExit()};[IO.File]::WriteAllText($r+'/Temp/'+$Evidence+'-watchdog.txt',$reason);throw $reason}
 $p.Refresh();[IO.File]::WriteAllText($r+'/Temp/'+$Evidence+'-exit.txt',[string]$p.ExitCode)
 if($driver -ne $null){$driver.WaitForExit();$driver.Refresh();if($driver.ExitCode -ne 0){throw ('Native driver exit '+$driver.ExitCode)}}
 if($p.ExitCode -ne 0){throw ('Player review exit '+$p.ExitCode)}
 $report=Get-Content -LiteralPath ($folder+'/result.json') -Raw -Encoding UTF8|ConvertFrom-Json
 if(!$report.passed){throw 'Player report failed'}
 Write-Output ('V31_REVIEW_OK '+$Evidence+' checks='+$report.checksCount)
}finally{Pop-Location}

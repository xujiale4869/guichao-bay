param([int]$TargetPid,[string]$Evidence,[switch]$Gameplay)
$ErrorActionPreference='Stop'
Import-Module 'C:/Windows/System32/WindowsPowerShell/v1.0/Modules/Microsoft.PowerShell.Utility/Microsoft.PowerShell.Utility.psd1'
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class SeamlessNativeV29 {
 [StructLayout(LayoutKind.Sequential)] public struct Point { public int X,Y; }
 [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; }
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h,out Rect r);
 [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h,ref Point p);
 [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
 [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
 [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a,uint b,bool attach);
 [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int command);
 [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
 [DllImport("user32.dll")] public static extern void keybd_event(byte vk,byte scan,uint flags,UIntPtr extra);
 public static void ScrollWheel(int delta){mouse_event(0x0800,0,0,unchecked((uint)delta),UIntPtr.Zero);}
 [DllImport("user32.dll")] public static extern void mouse_event(uint flags,uint dx,uint dy,uint data,UIntPtr extra);
}
'@
$target=Get-Process -Id $TargetPid
$until=[DateTime]::UtcNow.AddSeconds(150)
function AwaitPhase([string]$phase){
 while([DateTime]::UtcNow -lt $until){
  $state=Join-Path $Evidence 'native-state.json'
  if(Test-Path -LiteralPath $state){try{$s=Get-Content -LiteralPath $state -Raw -Encoding UTF8|ConvertFrom-Json;if($s.phase -eq $phase){Write-Output ('NATIVE_PHASE '+$phase);return}}catch{}}
  $target.Refresh();if($target.HasExited){throw 'Target exited before native sequence completed'}
  Start-Sleep -Milliseconds 100
 }
 throw ('Timed out awaiting '+$phase)
}
function GuardFocus {$target.Refresh();[uint32]$foregroundPid=0;[SeamlessNativeV29]::GetWindowThreadProcessId([SeamlessNativeV29]::GetForegroundWindow(),[ref]$foregroundPid)|Out-Null;if($foregroundPid -ne $TargetPid){throw ('Focus changed; stop native input. Expected PID='+$TargetPid+' actual='+$foregroundPid)}}
function Key([byte]$vk,[int]$hold=90){GuardFocus;[SeamlessNativeV29]::keybd_event($vk,0,0,[UIntPtr]::Zero);try{Start-Sleep -Milliseconds $hold}finally{[SeamlessNativeV29]::keybd_event($vk,0,2,[UIntPtr]::Zero)}}
function Click([bool]$right){GuardFocus;$btn=if($right){8}else{2};[SeamlessNativeV29]::mouse_event($btn,0,0,0,[UIntPtr]::Zero);try{Start-Sleep -Milliseconds 80}finally{[SeamlessNativeV29]::mouse_event($btn*2,0,0,0,[UIntPtr]::Zero)}}
try{
 AwaitPhase 'await-enter'
 $activation=New-Object -ComObject WScript.Shell;if(!$activation.AppActivate($TargetPid)){throw 'Could not activate the targeted game window'}
 Start-Sleep -Milliseconds 250;$target.Refresh();$handle=$target.MainWindowHandle
 [uint32]$foregroundPid=0;$foregroundThread=[SeamlessNativeV29]::GetWindowThreadProcessId([SeamlessNativeV29]::GetForegroundWindow(),[ref]$foregroundPid);$ownThread=[SeamlessNativeV29]::GetCurrentThreadId()
 [SeamlessNativeV29]::AttachThreadInput($ownThread,$foregroundThread,$true)|Out-Null
 try{[SeamlessNativeV29]::ShowWindow($handle,9)|Out-Null;[SeamlessNativeV29]::BringWindowToTop($handle)|Out-Null;[SeamlessNativeV29]::SetForegroundWindow($handle)|Out-Null}finally{[SeamlessNativeV29]::AttachThreadInput($ownThread,$foregroundThread,$false)|Out-Null}
 Start-Sleep -Milliseconds 300
 Key 0x47
 AwaitPhase 'await-pan';Key 0x57 600
 AwaitPhase 'await-select';Key 0x33
 AwaitPhase 'await-rotate';Key 0x52
 AwaitPhase 'await-place'
 $m=Get-Content (Join-Path $Evidence 'mouse-target.json') -Raw -Encoding UTF8|ConvertFrom-Json
 [SeamlessNativeV29]::SetThreadDpiAwarenessContext([IntPtr]::new(-4))|Out-Null
 $rect=New-Object SeamlessNativeV29+Rect;[SeamlessNativeV29]::GetClientRect($target.MainWindowHandle,[ref]$rect)|Out-Null
 $point=New-Object SeamlessNativeV29+Point;$point.X=[int]($m.x*$rect.Right/1920);$point.Y=$rect.Bottom-[int]($m.y*$rect.Bottom/1080)
 [SeamlessNativeV29]::ClientToScreen($target.MainWindowHandle,[ref]$point)|Out-Null;GuardFocus;[SeamlessNativeV29]::SetCursorPos($point.X,$point.Y)|Out-Null;Start-Sleep -Milliseconds 300;Click $false
 AwaitPhase 'await-remove';Click $true
 AwaitPhase 'await-exit';Key 0x47
 AwaitPhase 'await-scroll';GuardFocus;[SeamlessNativeV29]::ScrollWheel(-360)
 AwaitPhase 'await-gallery-b';Key 0x42
 AwaitPhase 'await-gallery-v';Key 0x56
 AwaitPhase 'await-gallery-f';Key 0x46
 if($Gameplay){
  AwaitPhase 'await-v34-time';Key 0x54
  AwaitPhase 'await-v34-lamp';Key 0x4C
  AwaitPhase 'await-v34-workshop';Key 0x45
  AwaitPhase 'await-v34-motor'
  $m=Get-Content (Join-Path $Evidence 'v34-button.json') -Raw -Encoding UTF8|ConvertFrom-Json
  $point=New-Object SeamlessNativeV29+Point;$point.X=[int]($m.x*$rect.Right/1920);$point.Y=$rect.Bottom-[int]($m.y*$rect.Bottom/1080)
  [SeamlessNativeV29]::ClientToScreen($target.MainWindowHandle,[ref]$point)|Out-Null
  GuardFocus;[SeamlessNativeV29]::SetCursorPos($point.X,$point.Y)|Out-Null;Start-Sleep -Milliseconds 280;Click $false
  AwaitPhase 'await-v34-close';Key 0x1B
  AwaitPhase 'await-v34-hook-tool';Key 0x32
  AwaitPhase 'await-v34-tow';Key 0x45
 }
 AwaitPhase 'done';Write-Output 'SEAMLESS_NATIVE_SEQUENCE_OK'
}finally{foreach($key in @(0x47,0x57,0x33,0x52,0x42,0x56,0x46,0x54,0x4C,0x45,0x1B,0x32)){[SeamlessNativeV29]::keybd_event($key,0,2,[UIntPtr]::Zero)}}

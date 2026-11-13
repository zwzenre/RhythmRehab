$ErrorActionPreference = 'Stop'
$sdk = dotnet --list-sdks | Select-Object -Last 1
if ($sdk -notmatch '^(\S+) \[(.+)\]$') { throw 'Install the .NET SDK to build the bridge.' }
$compiler = Join-Path $Matches[2] "$($Matches[1])/Roslyn/bincore/csc.dll"
$references = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7.2'
$metadata = Get-ChildItem (Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/UnionMetadata') -Directory |
    Sort-Object { try { [version]$_.Name } catch { [version]'0.0' } } -Descending |
    ForEach-Object { Join-Path $_.FullName 'Windows.winmd' } | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
$runtime = Get-ChildItem (Join-Path $env:WINDIR 'Microsoft.NET/assembly/GAC_MSIL/System.Runtime.WindowsRuntime') -Recurse -Filter 'System.Runtime.WindowsRuntime.dll' | Select-Object -First 1
if (!$metadata -or !$runtime) { throw 'Windows SDK metadata and the .NET Framework Windows Runtime assembly are required.' }
$output = Join-Path (Split-Path -Parent $PSScriptRoot) 'Assets/StreamingAssets'
New-Item -ItemType Directory -Force $output | Out-Null
dotnet $compiler /nologo /nostdlib /target:exe /optimize+ "/out:$output/RhythmRehabBluetooth.exe" "/r:$references/mscorlib.dll" "/r:$references/System.dll" "/r:$references/System.Core.dll" "/r:$references/Facades/System.Runtime.dll" "/r:$references/Facades/System.Runtime.InteropServices.WindowsRuntime.dll" "/r:$($runtime.FullName)" "/r:$metadata" (Join-Path $PSScriptRoot 'BluetoothImuBridge.cs')
if ($LASTEXITCODE -ne 0) { throw 'Bluetooth bridge compilation failed.' }

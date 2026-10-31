# Runs a program with NO visible window (no console, no game window) and waits for it: Unity batch builds and the
# headless game tests must never pop up or steal focus on Manan's PC (Kufu, 1 Nov: they did, mid-game).
# usage (from bash: tools/env.sh `hidden`): powershell -File run-hidden.ps1 <exe> [args...]
param([Parameter(Mandatory = $true)][string]$Exe, [Parameter(ValueFromRemainingArguments = $true)][string[]]$Rest)
$quoted = @($Rest | ForEach-Object { if ($_ -match '[\s"]') { '"' + ($_ -replace '"', '\"') + '"' } else { $_ } })
$p = if ($quoted.Count -gt 0) { Start-Process -FilePath $Exe -ArgumentList $quoted -WindowStyle Hidden -Wait -PassThru }
     else { Start-Process -FilePath $Exe -WindowStyle Hidden -Wait -PassThru }
exit $p.ExitCode

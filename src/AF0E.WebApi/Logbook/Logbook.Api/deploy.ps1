$config = Get-Content -Path ".secrets.json" -Raw | ConvertFrom-Json
$serverName = $config.webServerMachineName
$appPoolName = $config.appPoolName
$srcPath = $config.srcPath
$dstNetworkPath = $config.dstNetworkPath
$dstLocalPath = $config.dstLocalPath

Remove-Item ($srcPath + "/appsettings.json")
Remove-Item ($srcPath + "/appsettings.Development.json")
Remove-Item ($srcPath + "/dxcluster.filters.json")
Remove-Item ($srcPath + "/web.config")

$session = New-PSSession -ComputerName $serverName

Write-Host "Stopping app pool: ${serverName}/${appPoolName}"

Invoke-Command -Session $session -ScriptBlock {
    param($appPoolName)
    Import-Module WebAdministration
    Stop-WebAppPool -Name $appPoolName
} -ArgumentList $appPoolName

$maxAttempts = 10
$attempt = 0
do {
    Start-Sleep -Seconds 2
    $status = Invoke-Command -Session $session -ScriptBlock {
        param($appPoolName)
        Import-Module WebAdministration
        (Get-WebAppPoolState -Name $appPoolName).Value
    } -ArgumentList $appPoolName

    $attempt++
} while ($status -ne "Stopped" -and $attempt -lt $maxAttempts)

if ($status -ne "Stopped") {
    Write-Host "Timeout reached. App pool ${serverName}/${appPoolName} is still not stopped."
    Remove-PSSession -Session $session
    exit 1
}

Write-Host "App pool ${serverName}/${appPoolName} successfully stopped"
Read-Host -Prompt "Press Enter to continue..."

Invoke-Command -Session $session -ScriptBlock {
    param($siteRoot)
    Get-ChildItem -Path $siteRoot -Recurse | Where-Object { $_.Name -notlike "appsettings*" -and $_.Name -ne "web.config" -and $_.Name -ne "dxcluster.filters.json"} | Remove-Item -Recurse -Force
} -ArgumentList $dstLocalPath

Copy-Item -Path ($srcPath + "/*") -Destination ("\\" + $serverName + "/" + $dstNetworkPath) -Recurse

Write-Host "Content deployed"
Read-Host -Prompt "Press Enter to continue..."

Write-Host "Starting app pool: ${serverName}/${appPoolName}"

Invoke-Command -Session $session -ScriptBlock {
    param($appPoolName)
    Import-Module WebAdministration
    Start-WebAppPool -Name $appPoolName
} -ArgumentList $appPoolName

Start-Sleep -Seconds 5

Remove-PSSession -Session $session

Write-Host "Deployment finished"

exit 0

$config = Get-Content -Path "secrets.json" -Raw | ConvertFrom-Json
$serverName = $config.webServerMachineName
$webSiteName = $config.webSiteName
$srcPath = $config.srcPath
$dstNetworkPath = $config.dstNetworkPath
$dstLocalPath = $config.dstLocalPath

Write-Host ("\\" + $serverName + "/" + $dstNetworkPath)

Remove-Item ($srcPath + "/appsettings.json")
Remove-Item ($srcPath + "/appsettings.Development.json")

$session = New-PSSession -ComputerName $serverName

Invoke-Command -Session $session -ScriptBlock {
    param($websiteName)
    Import-Module WebAdministration
    Stop-WebSite -Name $websiteName
} -ArgumentList $websiteName

Invoke-Command -Session $session -ScriptBlock {
    param($siteRoot)
    Get-ChildItem -Path $siteRoot -Recurse | Where-Object { $_.Name -notlike "appsettings*" } | Remove-Item -Recurse -Force
} -ArgumentList $dstLocalPath

Copy-Item -Path ($srcPath + "/*") -Destination ("\\" + $serverName + "/" + $dstNetworkPath) -Recurse

Invoke-Command -Session $session -ScriptBlock {
    param($websiteName)
    Import-Module WebAdministration
    Start-WebSite -Name $websiteName
} -ArgumentList $websiteName

Remove-PSSession -Session $session

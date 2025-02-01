$config = Get-Content -Path ".secrets.json" -Raw | ConvertFrom-Json
$serverName = $config.webServerMachineName
$webSiteName = $config.webSiteName
$srcPath = $config.srcPath
$dstNetworkPath = $config.dstNetworkPath
$dstLocalPath = $config.dstLocalPath

Remove-Item ($srcPath + "/appsettings.json")
Remove-Item ($srcPath + "/appsettings.Development.json")

$session = New-PSSession -ComputerName $serverName

Invoke-Command -Session $session -ScriptBlock {
    param($websiteName)
    Import-Module WebAdministration
    Stop-WebSite -Name $websiteName
} -ArgumentList $websiteName

Start-Sleep -Seconds 5

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

Start-Sleep -Seconds 5

Remove-PSSession -Session $session

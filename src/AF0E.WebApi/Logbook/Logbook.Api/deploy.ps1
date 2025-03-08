$config = Get-Content -Path ".secrets.json" -Raw | ConvertFrom-Json
$serverName = $config.webServerMachineName
$webSiteList = $config.webSiteList
$webSites = $webSiteList -split ","
$srcPath = $config.srcPath
$dstNetworkPath = $config.dstNetworkPath
$dstLocalPath = $config.dstLocalPath

Remove-Item ($srcPath + "/appsettings.json")
Remove-Item ($srcPath + "/appsettings.Development.json")

$session = New-PSSession -ComputerName $serverName

foreach ($siteName in $webSites)
{
    $siteName = $siteName.Trim()
    Write-Host "Stopping website: ${serverName}/${siteName}"

    # Stop the website remotely
    Invoke-Command -Session $session -ScriptBlock {
        param ($siteName)
        Import-Module WebAdministration
        Stop-Website -Name $siteName
    } -ArgumentList $siteName
}

Remove-PSSession -Session $session
Start-Sleep -Seconds 2
$session = New-PSSession -ComputerName $serverName

foreach ($siteName in $webSites)
{
    # Wait for the sites to be fully stopped
    $maxAttempts = 10  # Maximum retries
    $attempt = 0
    do {
        Start-Sleep -Seconds 2  # Wait 2 seconds before checking again
        $status = Invoke-Command -Session $session -ScriptBlock {
            param ($siteName)
            Import-Module WebAdministration
            (Get-Website -Name $siteName).State
        } -ArgumentList $siteName

        $attempt++
        # Write-Host "${attempt} of ${maxAttempts}: Website status = $status"
    } while ($status -ne "Stopped" -and $attempt -lt $maxAttempts)

    if ($status -eq "Stopped") {
        Write-Host "Website ${serverName}/${siteName} successfully stopped"
    } else {
        Write-Host "Timeout reached. Website ${serverName}/${siteName} is still not stopped."
        exit 1
    }
}

Invoke-Command -Session $session -ScriptBlock {
    param($siteRoot)
    Get-ChildItem -Path $siteRoot -Recurse | Where-Object { $_.Name -notlike "appsettings*" } | Remove-Item -Recurse -Force
} -ArgumentList $dstLocalPath

Copy-Item -Path ($srcPath + "/*") -Destination ("\\" + $serverName + "/" + $dstNetworkPath) -Recurse

foreach ($siteName in $webSites)
{
    $siteName = $siteName.Trim()
    Write-Host "Starting website: ${serverName}/${siteName}"

    Invoke-Command -Session $session -ScriptBlock {
        param($siteName)
        Import-Module WebAdministration
        Start-WebSite -Name $siteName
    } -ArgumentList $siteName
}

Start-Sleep -Seconds 5

Remove-PSSession -Session $session

exit 0

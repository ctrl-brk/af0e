# PotaParksImport

Console app that imports parks from a configurable POTA API endpoint into `[dbo].[PotaParksImport]`, then runs `[dbo].[ImportPotaParks-US]`.

## Configuration

Set values in `appsettings.json` (or `appsettings.development.json`):

- `AppSettings:ConnectionString`
- `AppSettings:ParksUrl` (default: `https://api.pota.app/program/parks/US`)
- `AppSettings:HttpTimeoutSeconds`
- `AppSettings:DryRun`

## Run

```powershell
dotnet run --project C:\Projects\AF0E\src\AF0E.App\PotaParksImport\PotaParksImport.csproj
```

## Dry run

Dry-run validates HTTP + JSON parsing and logs how many rows would be imported, but skips:

- `TRUNCATE TABLE [dbo].[PotaParksImport]`
- insert into `[dbo].[PotaParksImport]`
- `EXEC [dbo].[ImportPotaParks-US]`

Use either config (`AppSettings:DryRun=true`) or CLI switch:

```powershell
dotnet run --project C:\Projects\AF0E\src\AF0E.App\PotaParksImport\PotaParksImport.csproj -- --dry-run
```



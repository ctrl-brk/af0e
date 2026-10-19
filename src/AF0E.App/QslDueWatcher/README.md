# QslDueWatcher

A one-shot console application that emails a digest when assigned Ham Events are approaching their QSL deadline. It is designed to run once daily from Windows Task Scheduler.

## Reminder rules

An event is included when:

- `QslDueDate` is the configured local date.
- Overdue events are also included when `IncludeOverdue` is enabled.
- The event has at least one `HamEventContact` whose log entry has no QSL sent date and whose QSL status is empty, `N`, or `R`.

One digest is sent for all qualifying events. After SMTP succeeds, the event/deadline/recipient identity is written to `StateFile`; later runs do not resend it. Changing the deadline or recipient creates a new reminder identity. Keep the state file when upgrading or republishing the app.

## Configuration

Do not put credentials in committed settings files. Supply them with environment variables or a deployment-only settings file:

```powershell
$env:AppSettings__ConnectionString = 'Server=...;Database=...;...'
$env:AppSettings__Email__From = 'sender@example.com'
$env:AppSettings__Email__To = 'recipient@example.com'
$env:AppSettings__Email__Smtp__Server = 'smtp.example.com'
$env:AppSettings__Email__Smtp__Port = '587'
$env:AppSettings__Email__Smtp__User = 'sender@example.com'
$env:AppSettings__Email__Smtp__Password = 'secret'
```

`TimeZoneId` defaults to the Windows ID `Mountain Standard Time`. Set an appropriate system time-zone ID when running on another platform.

## Run

```powershell
dotnet run --project C:\Projects\AF0E\src\AF0E.App\QslDueWatcher\QslDueWatcher.csproj
```

A successful run exits after checking the database, whether or not an email was required. Failures are logged and return process exit code `1`.

## Scheduling

Publish the app and create a Windows Task Scheduler task that runs it once each morning. Configure the task to prevent overlapping instances and set **Start in** to the publish directory. The application resolves settings and state paths relative to its executable directory.



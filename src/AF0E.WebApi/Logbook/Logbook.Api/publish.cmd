@echo off
rmdir dist /s /q > nul
dotnet publish -p:PublishProfile=FolderProfile
if %errorlevel% neq 0 exit 1
cd ..\..\..\AF0E.UI\Site
rmdir dist /s /q > nul
call ng build
if %errorlevel% neq 0 exit 1
mkdir ..\..\AF0E.WebApi\Logbook\Logbook.Api\dist\wwwroot
xcopy /s /q dist\wwwroot\browser\*.* ..\..\AF0E.WebApi\Logbook\Logbook.Api\dist\wwwroot >nul

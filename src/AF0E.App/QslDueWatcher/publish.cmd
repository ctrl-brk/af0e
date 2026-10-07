@echo off
setlocal
pushd "%~dp0"

dotnet test "..\..\..\tests\AF0E.App.QslDueWatcher.Tests\AF0E.App.QslDueWatcher.Tests.csproj" -c Release
if errorlevel 1 goto :failed

rmdir dist /s /q > nul 2>&1
dotnet publish -p:PublishProfile=FolderProfile
if errorlevel 1 goto :failed

popd
exit /b 0

:failed
set "exitCode=%errorlevel%"
popd
exit /b %exitCode%

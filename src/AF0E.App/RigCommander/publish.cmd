@echo off
rmdir /S /Q dist
dotnet test ..\..\..\tests\AF0E.App.RigCommander.Tests\RigCommander.Tests.csproj
dotnet publish -p:PublishProfile=FolderProfile
del dist\appsettings.development.json

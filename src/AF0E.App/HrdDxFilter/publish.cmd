@echo off
rmdir /S /Q dist > nul
dotnet publish -p:PublishProfile=FolderProfile

@echo off
rmdir /S /Q dist
dotnet publish -p:PublishProfile=FolderProfile

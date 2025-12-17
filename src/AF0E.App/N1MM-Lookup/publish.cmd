@echo off
rmdir dist /s /q > nul
dotnet publish -p:PublishProfile=FolderProfile

@echo off

dotnet new tool-manifest --force
dotnet tool install inedo.extensionpackager

cd subversion\InedoExtension
dotnet inedoxpack pack . C:\LocalDev\BuildMaster\Extensions\subversion.upack --build=Debug -o
cd ..\..
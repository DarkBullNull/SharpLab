Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Write-Output 'git submodule update --recursive --init'
git submodule update --recursive --init

Write-Output 'dotnet build source'
dotnet build source -c Release /p:UnbreakablePolicyReportEnabled=false
if ($LastExitCode -ne 0) { throw "dotnet build exited with code $LastExitCode" }

Write-Output 'dotnet publish source/Server.AspNetCore/Server.AspNetCore.csproj ...'
dotnet publish source/Server.AspNetCore/Server.AspNetCore.csproj -c Release --no-build --no-restore
if ($LastExitCode -ne 0) { throw "dotnet publish exited with code $LastExitCode" }

$aspNetCorePublishRoot = 'source/Server.AspNetCore/bin/Release/netcoreapp3.0/publish'
Write-Output "Compress-Archive -Path $aspNetCorePublishRoot/* -DestinationPath $aspNetCorePublishRoot/Server.AspNetCore.zip"
Compress-Archive -Path "$aspNetCorePublishRoot/*" -DestinationPath "$aspNetCorePublishRoot/Server.AspNetCore.zip"

Write-Output 'dotnet msbuild source/WebApp/WebApp.csproj /t:Publish ...'
dotnet msbuild source/WebApp/WebApp.csproj /t:Publish /p:Configuration=Release /p:UnbreakablePolicyReportEnabled=false /verbosity:minimal
if ($LastExitCode -ne 0) { throw "dotnet msbuild exited with code $LastExitCode" }

$webAppPublishRoot = 'source/WebApp/bin/publish'
Write-Output "Compress-Archive -Path $webAppPublishRoot/* -DestinationPath $webAppPublishRoot/WebApp.zip"
Compress-Archive -Path "$webAppPublishRoot/*" -DestinationPath "$webAppPublishRoot/WebApp.zip"
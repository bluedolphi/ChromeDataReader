@echo off
setlocal enabledelayedexpansion

echo ========================================
echo Chrome Data Reader - Minimal Build
echo Framework-Dependent Single File
echo ========================================
echo.

:: Check if in correct directory
if not exist "ChromeDataReader.csproj" (
    echo Error: ChromeDataReader.csproj not found
    echo Please run this script in the project root directory
    pause
    exit /b 1
)

:: Check .NET 6.0 SDK
echo Checking .NET 6.0 SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo Error: .NET SDK not found
    echo Please install .NET 6.0 SDK: https://dotnet.microsoft.com/download/dotnet/6.0
    pause
    exit /b 1
)

:: Show .NET version
for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set "dotnet_version=%%i"
echo .NET SDK Version: !dotnet_version!
echo.

:: Clean previous build files
echo Cleaning previous build files...
if exist "bin\Release" rmdir /s /q "bin\Release"
if exist "obj\Release" rmdir /s /q "obj\Release"
if exist "publish-minimal" rmdir /s /q "publish-minimal"
if exist "publish-single-file" rmdir /s /q "publish-single-file"
echo Clean completed
echo.

:: Restore NuGet packages
echo Restoring NuGet packages...
dotnet restore ChromeDataReader.csproj
if errorlevel 1 (
    echo Error: NuGet restore failed
    pause
    exit /b 1
)
echo NuGet restore completed
echo.

:: Build project
echo Building project...
dotnet build ChromeDataReader.csproj --configuration Release --no-restore
if errorlevel 1 (
    echo Error: Project build failed
    pause
    exit /b 1
)
echo Project build completed
echo.

:: Publish minimal single file version (x64) - Framework Dependent
echo Publishing minimal single file version (x64)...
dotnet publish ChromeDataReader.csproj --configuration Release --runtime win-x64 --self-contained false --output "publish-minimal\x64" /p:PublishSingleFile=true
if errorlevel 1 (
    echo Error: x64 version publish failed
    pause
    exit /b 1
)
echo x64 minimal version publish completed

:: Publish minimal single file version (x86) - Framework Dependent
echo Publishing minimal single file version (x86)...
dotnet publish ChromeDataReader.csproj --configuration Release --runtime win-x86 --self-contained false --output "publish-minimal\x86" /p:PublishSingleFile=true
if errorlevel 1 (
    echo Error: x86 version publish failed
    pause
    exit /b 1
)
echo x86 minimal version publish completed
echo.

:: Rename files
echo Renaming output files...
if exist "publish-minimal\x64\ChromeDataReader.exe" (
    ren "publish-minimal\x64\ChromeDataReader.exe" "ChromeDataReader-Minimal-x64.exe"
    echo x64 minimal version renamed to: ChromeDataReader-Minimal-x64.exe
)

if exist "publish-minimal\x86\ChromeDataReader.exe" (
    ren "publish-minimal\x86\ChromeDataReader.exe" "ChromeDataReader-Minimal-x86.exe"
    echo x86 minimal version renamed to: ChromeDataReader-Minimal-x86.exe
)

:: Show file size information
echo.
echo ========== Build Results ==========
if exist "publish-minimal\x64\ChromeDataReader-Minimal-x64.exe" (
    for %%F in ("publish-minimal\x64\ChromeDataReader-Minimal-x64.exe") do (
        set "size=%%~zF"
        set /a "sizeMB=!size!/1024/1024"
        echo x64 minimal version: ChromeDataReader-Minimal-x64.exe (!sizeMB! MB)
    )
)

if exist "publish-minimal\x86\ChromeDataReader-Minimal-x86.exe" (
    for %%F in ("publish-minimal\x86\ChromeDataReader-Minimal-x86.exe") do (
        set "size=%%~zF"
        set /a "sizeMB=!size!/1024/1024"
        echo x86 minimal version: ChromeDataReader-Minimal-x86.exe (!sizeMB! MB)
    )
)

echo.
echo ========== Build Completed ==========
echo Output directory: %CD%\publish-minimal
echo x64 minimal version: publish-minimal\x64\ChromeDataReader-Minimal-x64.exe
echo x86 minimal version: publish-minimal\x86\ChromeDataReader-Minimal-x86.exe
echo.
echo NOTE: These are framework-dependent builds.
echo Users need to install .NET 6.0 Runtime first.
echo The application will guide users to install if missing.
echo.

:: Ask to open output directory
set /p "open_folder=Open output directory? (Y/N): "
if /i "!open_folder!"=="Y" (
    start "" "%CD%\publish-minimal"
)

echo.
echo Script completed!
pause

@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework\v2.0.50727\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework64\v2.0.50727\csc.exe
if not exist "%CSC%" (
  echo The .NET Framework 2.0 C# compiler was not found.
  exit /b 1
)
"%CSC%" /nologo /target:winexe /platform:x86 /optimize+ /win32icon:app-icon.ico /out:Confocal-Configurator.exe /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll AssemblyInfo.cs Program.cs SpectralProfiles.cs FpMeasuredProfiles.cs DyeReferenceProfiles.cs Visualization.cs
if errorlevel 1 exit /b %errorlevel%
echo.
echo Created: %CD%\Confocal-Configurator.exe

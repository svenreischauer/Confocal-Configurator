@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework\v2.0.50727\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework64\v2.0.50727\csc.exe
if not exist "%CSC%" (
  echo The .NET Framework 2.0 C# compiler was not found.
  exit /b 1
)
set TEST_DLL=%TEMP%\Confocal-Configurator.Tests.dll
"%CSC%" /nologo /target:library /platform:x86 /optimize+ /out:"%TEST_DLL%" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll AssemblyInfo.cs Program.cs SpectralProfiles.cs FpMeasuredProfiles.cs DyeReferenceProfiles.cs Visualization.cs RegressionTests.cs
if errorlevel 1 exit /b %errorlevel%
set POWERSHELL32=%WINDIR%\SysWOW64\WindowsPowerShell\v1.0\powershell.exe
if not exist "%POWERSHELL32%" set POWERSHELL32=%WINDIR%\System32\WindowsPowerShell\v1.0\powershell.exe
"%POWERSHELL32%" -NoProfile -ExecutionPolicy Bypass -File "%CD%\run_library_tests.ps1" -AssemblyPath "%TEST_DLL%"
exit /b %errorlevel%

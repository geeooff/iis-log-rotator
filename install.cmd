@echo off
IF EXIST %WINDIR%\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe (
	echo Calling 64-bit .NET Installer...
	%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /LogFile=install.log /ShowCallStack LogRotator.exe
) ELSE (
	echo Calling 32-bit .NET Installer...
	%WINDIR%\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe /LogFile=install.log /ShowCallStack LogRotator.exe
)
echo Done.
@pause
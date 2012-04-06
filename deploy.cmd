@echo off
FOR %%m IN (ROWEB102 ROWEB103 ROWEB104 ROWEB105 ROVMS502 ROVMS503 ROVMS504 VRSRV502 VRSRV503 VRSRV504) DO FOR %%f IN (LogRotator.exe LogRotator.exe.config LogRotator.pdb install.cmd uninstall.cmd) DO xcopy /Q /Y bin\Release\%%f \\%%m\c$\Scripts\LogRotator
pause
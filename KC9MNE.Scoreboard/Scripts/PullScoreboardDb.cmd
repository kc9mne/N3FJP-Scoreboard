@echo off
setlocal
REM -- Set this to the IP or name of your logging master computer in this example the PC is named SERVER --
set SRC=\\SERVER\log\Scoreboard.mdb
REM --- This is the Scoreboard folder on c: where you should have extracted the other files from the package Dont change or it will break stuff you will need to fix!---
set DSTDIR=C:\Scoreboard\Data
set TMP=%DSTDIR%\Scoreboard_tmp.mdb
set LIVE=%DSTDIR%\Scoreboard.mdb

copy /Y "%SRC%" "%TMP%" >nul && move /Y "%TMP%" "%LIVE%" >nul



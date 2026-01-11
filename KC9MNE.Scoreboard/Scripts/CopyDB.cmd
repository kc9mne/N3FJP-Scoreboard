@echo off
color 0a
setlocal enabledelayedexpansion

echo KC9MNE N3FJP DB Copy Script (Safe/Atomic)
echo -----------------------------------------------
echo KC9MNE N3FJP Sync Replicates live DB to a share for scoreboard use
echo -----------------------------------------------
echo.

REM ===== CONFIG =====
REM ---Set to your path for N3FJP's data file location---
set SRC_DIR=C:\Users\usr\Documents\Affirmatech\N3FJP Software\ARRL-Field-Day
set SRC_FILE=LogData.mdb

REM --This is the folder you are sharing for the scoreboard pc to access.--
set DST_DIR=C:\log

REM The name the scoreboard should read/pull
set DST_LIVE=%DST_DIR%\Scoreboard.mdb
set DST_TMP=%DST_DIR%\Scoreboard_tmp.mdb

:loop
echo Replicating %SRC_FILE% ...

REM Copy to temp first
copy /Y "%SRC_DIR%\%SRC_FILE%" "%DST_TMP%" >nul
if errorlevel 1 (
  echo Copy FAILED at %TIME%  (will retry)
) else (
  REM Atomic swap so scoreboard never reads a half-copied file
  move /Y "%DST_TMP%" "%DST_LIVE%" >nul
  if errorlevel 1 (
    echo Swap FAILED at %TIME% (will retry)
  ) else (
    echo File replicated at %TIME%. Next run in 30 seconds.
  )
)

timeout /t 30 /nobreak >nul
goto loop

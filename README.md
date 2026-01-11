# N3FJP-Scoreboard
A quick web based leader board for groups logging with N3FJP's software, designed to run on a TV at the event.

![image](https://github.com/user-attachments/assets/c40fc536-4911-46d6-820d-ca8596e6ff9e)

What this is

This scoreboard reads an N3FJP Log .mdb (Microsoft Access format) and serves a local web dashboard you can open from any device on your LAN (TVs, tablets, laptops).

It’s designed for Field Day / Winter Field Day where internet is unreliable.

Setup Overview

You will have:

Logging PC (runs N3FJP and is the “master” log)

Scoreboard PC/Laptop (runs this web dashboard and reads a copied .mdb)

We use a simple file copy loop so the scoreboard always reads a safe read-only copy of the database.

1) Logging PC (N3FJP machine)
Create a folder to publish the copied DB

Create folder:

C:\log

Share it as:

Share name: log

Permissions (simple mode):

Share permissions: Everyone = Read

NTFS permissions: Read for whoever will access it

Run the DB copy script

Put DBcopy.cmd on the desktop and run it. Minimize it.

This continuously copies the live N3FJP database into C:\log without locking the file.

2) Scoreboard PC/Laptop
A) Install Access Database Engine (ACE) (required)

You need the ACE OLEDB provider so the scoreboard can read the .mdb.

Important: match your Office bitness.

If you have Office 32-bit, install ACE 32-bit

If you have Office 64-bit, install ACE 64-bit

(If you don’t have Office installed, either is fine — just match the scoreboard build you downloaded.)

B) Install / extract the scoreboard

Create this folder structure:

C:\Scoreboard\
  KC9MNE.Scoreboard.exe
  appsettings.json
  Data\
    Scoreboard.mdb
  wwwroot\
    index.html
    mascot.png
    lib\...


Your appsettings.json should look like this (default is fine): 

appsettings

{
  "Scoreboard": {
    "MdbPath": "Data\\Scoreboard.mdb",
    "RefreshSeconds": 30,
    "Port": 8080
  }
}

C) Copy the database from the Logging PC

Option 1 (recommended): use a scheduled task + robocopy loop to pull from the share:

Example batch file PullDb.cmd on the scoreboard laptop:

@echo off
set SRC=\\LOGGING-PC-NAME\log
set DST=C:\Scoreboard\Data

:loop
robocopy "%SRC%" "%DST%" *.mdb /XO /FFT /R:1 /W:1 >nul
timeout /t 10 /nobreak >nul
goto loop


Run that minimized. Now your scoreboard always has:

C:\Scoreboard\Data\Scoreboard.mdb

D) Run the scoreboard

Double-click:

KC9MNE.Scoreboard.exe

Open:

http://localhost:8080

3) View from other devices on the LAN

From any device on your event Wi-Fi / LAN:

http://SCOREBOARD-LAPTOP-IP:8080

Example:

http://192.168.3.50:8080

Troubleshooting
“The 'Microsoft.ACE.OLEDB.12.0' provider is not registered…”

Install the Access Database Engine (ACE) that matches your Office bitness (32/64-bit).

Dashboard loads but no data

Confirm:

C:\Scoreboard\Data\Scoreboard.mdb exists

your pull/copy script is running and updating the file

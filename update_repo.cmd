@echo off
setlocal
cd /d "%~dp0"

rem ============================================================
rem  Pfade zu den beiden Paketen. Hier anpassen, wenn sie
rem  umziehen oder die Versionsnummer wechselt.
rem ============================================================
set "ZIP_GAME=C:\Program Files (x86)\Steam\steamapps\common\Sons Of The Forest\LivingSoldiers_v1.19.0.zip"
set "ZIP_SERVER=C:\Program Files (x86)\Steam\steamapps\common\Sons Of The Forest\LivingSoldiers_v1.19.0_Server.zip"

rem Kurze Kontrolle, bevor irgendetwas hochgeladen wird
if not exist "%ZIP_GAME%"   echo HINWEIS: nicht gefunden: %ZIP_GAME%
if not exist "%ZIP_SERVER%" echo HINWEIS: nicht gefunden: %ZIP_SERVER%

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0update_repo.ps1" -Message "%~1" -Tag "%~2" -Zips "%ZIP_GAME%","%ZIP_SERVER%"
echo.
pause

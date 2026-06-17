@echo off
setlocal
cd /d "%~dp0"
title Crear ejecutable de Concentrica

where dotnet >nul 2>nul
if not %errorlevel%==0 (
    echo No se encontro el SDK de .NET.
    echo Instala .NET 10 desde https://dotnet.microsoft.com/download/dotnet/10.0
    echo y vuelve a ejecutar este archivo.
    echo.
    pause
    exit /b 1
)

echo Compilando el ejecutable autonomo... esto puede tardar un par de minutos.
powershell -NoProfile -ExecutionPolicy Bypass -File "publish.ps1"
if not %errorlevel%==0 (
    echo.
    echo Hubo un error al compilar.
    pause
    exit /b 1
)

rem Copia el .exe a la carpeta principal para que sea facil de encontrar.
if exist "publish\Concentrica.exe" copy /Y "publish\Concentrica.exe" "Concentrica.exe" >nul

echo.
echo LISTO. Ya puedes abrir la app con "Ejecutar-Concentrica.bat"
echo o haciendo doble clic en "Concentrica.exe".
echo.
pause

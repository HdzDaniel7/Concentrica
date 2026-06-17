@echo off
setlocal
cd /d "%~dp0"
title Concentrica

rem 1) Si el ejecutable ya existe (en la carpeta principal o en publish), abrirlo.
if exist "Concentrica.exe" (
    start "" "Concentrica.exe"
    exit /b 0
)
if exist "publish\Concentrica.exe" (
    start "" "publish\Concentrica.exe"
    exit /b 0
)

rem 2) Si esta instalado el SDK de .NET, ejecutar desde el codigo.
where dotnet >nul 2>nul
if %errorlevel%==0 (
    echo No se encontro el ejecutable; iniciando desde el codigo con .NET...
    echo La primera vez puede tardar un poco mientras compila.
    dotnet run --project "Soldadura.App\Soldadura.App.csproj" -c Release
    exit /b %errorlevel%
)

rem 3) Nada disponible: explicar como obtener la app.
echo.
echo No se encontro "Concentrica.exe" ni el SDK de .NET.
echo.
echo Opcion A (recomendada): descarga "Concentrica.exe" desde la pagina de
echo  Releases del proyecto en GitHub y guardalo junto a este archivo.
echo.
echo Opcion B: ejecuta "Crear-ejecutable.bat" (requiere el SDK de .NET 10,
echo  https://dotnet.microsoft.com/download/dotnet/10.0).
echo.
pause

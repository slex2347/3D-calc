@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo ==============================================================
echo  PrintCalc3D: сборка автономного exe (~70-150 МБ)
echo  Работает на любом Windows 10/11 БЕЗ установки .NET.
echo  Для сборки нужен доступ к nuget.org (скачивается среда .NET).
echo ==============================================================
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ОШИБКА] dotnet не найден. Установите .NET 8 SDK:
  echo https://dotnet.microsoft.com/download/dotnet/8.0
  pause
  exit /b 1
)
dotnet publish PrintCalc3D.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o dist-selfcontained
if errorlevel 1 (
  echo.
  echo [ОШИБКА] Сборка не удалась. Проверьте интернет и прокси Windows.
  pause
  exit /b 1
)
echo.
echo Готово: %~dp0dist-selfcontained\PrintCalc3D.exe
pause

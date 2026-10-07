@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo ==============================================================
echo  PrintCalc3D: сборка ОДНОГО exe-файла (маленький, ~0.2 МБ)
echo  На ПК, где он будет запускаться, нужен .NET 8 Desktop Runtime
echo  (если Visual Studio установлена - он уже есть).
echo ==============================================================
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ОШИБКА] dotnet не найден. Установите .NET 8 SDK:
  echo https://dotnet.microsoft.com/download/dotnet/8.0
  pause
  exit /b 1
)
dotnet publish PrintCalc3D.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o dist
if errorlevel 1 (
  echo.
  echo [ОШИБКА] Сборка не удалась. Если в тексте выше есть "nuget" или "127.0.0.1:10809" -
  echo отключите прокси Windows: Параметры - Сеть и Интернет - Прокси.
  pause
  exit /b 1
)
echo.
echo Готово: %~dp0dist\PrintCalc3D.exe
pause

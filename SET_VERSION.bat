@echo off
setlocal enabledelayedexpansion
title Gerenciador de Versao - Letreiro Digital
cls

echo =======================================================
echo    GERENCIADOR DE VERSAO - LETREIRO DIGITAL
echo =======================================================
echo.

set NEW_VER=%1
set NEW_BUILD=%2

if "%NEW_VER%"=="" (
    :GET_VERSION
    set /p NEW_VER="Digite a nova versao (ex: 3.1.0): "
    if "!NEW_VER!"=="" goto GET_VERSION
)

if "%NEW_BUILD%"=="" (
    :GET_BUILD
    set /p NEW_BUILD="Digite o Build Number (ex: 2): "
    if "!NEW_BUILD!"=="" goto GET_BUILD
)

echo.
echo Processando alteracao para v%NEW_VER% (Build %NEW_BUILD%)...
echo.

:: 1. Update Services\UpdateService.cs
echo [1/3] Atualizando UpdateService.cs...
powershell -Command "& { $f = '_DEV_PROJETO\Services\UpdateService.cs'; $raw = [IO.File]::ReadAllBytes($f); $hasBOM = ($raw.Length -ge 3 -and $raw[0] -eq 0xEF -and $raw[1] -eq 0xBB -and $raw[2] -eq 0xBF); if ($hasBOM) { $enc = New-Object System.Text.UTF8Encoding($true) } else { $enc = New-Object System.Text.UTF8Encoding($false) }; $c = [IO.File]::ReadAllText($f, $enc); $c = $c -replace 'CurrentVersion = \"[^\"]*\"', 'CurrentVersion = \"%NEW_VER%\"'; $c = $c -replace 'CurrentBuildNumber = \d+', 'CurrentBuildNumber = %NEW_BUILD%'; [IO.File]::WriteAllText($f, $c, $enc) }"

:: 2. Update LetreiroDigital.csproj
echo [2/3] Atualizando LetreiroDigital.csproj...
powershell -Command "& { $f = '_DEV_PROJETO\LetreiroDigital.csproj'; $raw = [IO.File]::ReadAllBytes($f); $hasBOM = ($raw.Length -ge 3 -and $raw[0] -eq 0xEF -and $raw[1] -eq 0xBB -and $raw[2] -eq 0xBF); if ($hasBOM) { $enc = New-Object System.Text.UTF8Encoding($true) } else { $enc = New-Object System.Text.UTF8Encoding($false) }; $c = [IO.File]::ReadAllText($f, $enc); $c = $c -replace '<Version>[^<]*</Version>', '<Version>%NEW_VER%</Version>'; $c = $c -replace '<FileVersion>[^<]*</FileVersion>', '<FileVersion>%NEW_VER%</FileVersion>'; $c = $c -replace '<AssemblyVersion>[^<]*</AssemblyVersion>', '<AssemblyVersion>%NEW_VER%.%NEW_BUILD%</AssemblyVersion>'; [IO.File]::WriteAllText($f, $c, $enc) }"

:: 3. Update Views\ControlWindow.xaml (Visual tag SISTEMA VX.X)
echo [3/3] Atualizando ControlWindow.xaml...
for /f "tokens=1,2 delims=." %%a in ("%NEW_VER%") do set SHORT_VER=%%a.%%b
powershell -Command "& { $f = '_DEV_PROJETO\Views\ControlWindow.xaml'; $raw = [IO.File]::ReadAllBytes($f); $hasBOM = ($raw.Length -ge 3 -and $raw[0] -eq 0xEF -and $raw[1] -eq 0xBB -and $raw[2] -eq 0xBF); if ($hasBOM) { $enc = New-Object System.Text.UTF8Encoding($true) } else { $enc = New-Object System.Text.UTF8Encoding($false) }; $c = [IO.File]::ReadAllText($f, $enc); $c = $c -replace 'Text=\"SISTEMA V\d+(\.\d+)*\"', 'Text=\"SISTEMA V%SHORT_VER%\"'; [IO.File]::WriteAllText($f, $c, $enc) }"

echo.
echo =======================================================
echo    VERSAO ATUALIZADA COM SUCESSO!
echo    Novo Estado: v%NEW_VER% (Build %NEW_BUILD%)
echo =======================================================
echo.
if "%3"=="--no-pause" goto END
pause
:END

@echo off
setlocal enabledelayedexpansion
echo ==========================================
echo GERANDO VERSAO FINAL DO USUARIO (RELEASE)
echo ==========================================
cd _DEV_PROJETO

:: Extraindo a versao do arquivo csproj
for /f "tokens=3 delims=<>" %%a in ('findstr "<Version>" LetreiroDigital.csproj') do set APP_VERSION=%%a

if "%APP_VERSION%"=="" (
    set APP_VERSION=1.0.0
)

set OUTPUT_DIR=..\_VERSAO_FINAL_USUARIO\letreiro_V%APP_VERSION%_CodaStudios

echo Versao identificada: %APP_VERSION%
echo Compilando o arquivo: LetreiroDigital.csproj
dotnet publish "LetreiroDigital.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%OUTPUT_DIR%"

if %errorlevel% neq 0 (
    echo.
    echo ERRO NA COMPILACAO! Verifique o console acima para detalhes.
    pause
    exit /b
)

echo.
echo SUCESSO! A versao final foi gerada na pasta:
echo %OUTPUT_DIR%
pause

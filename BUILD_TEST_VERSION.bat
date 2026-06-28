@echo off
echo ==========================================
echo GERANDO VERSAO DE TESTE (COM LICENCA)
echo ==========================================
cd _DEV_PROJETO

echo Compilando o arquivo: LetreiroDigital.csproj
dotnet publish "LetreiroDigital.csproj" -c Release -r win-x64 --self-contained false -p:DefineConstants="TEST_VERSION" -o ..\_VERSAO_TESTE_COM_LICENCA

if %errorlevel% neq 0 (
    echo.
    echo ERRO NA COMPILACAO! Verifique o console acima para detalhes.
    pause
    exit /b
)

echo.
echo SUCESSO! A versao foi gerada na pasta _VERSAO_TESTE_COM_LICENCA
pause

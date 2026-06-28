$source = "c:\Users\adna\Documents\PROJETOS\03-APLICATIVOS-CSHARP\LETREIRO-DIGITAL-WPF"
$versao_teste = Join-Path $source "_VERSAO_TESTE"
$dev_projeto = Join-Path $source "_DEV_PROJETO"
$bin_debug = Join-Path $source "bin\Debug\net8.0-windows"

Write-Host "Creating Directories..."
New-Item -ItemType Directory -Force -Path $versao_teste
New-Item -ItemType Directory -Force -Path $dev_projeto

Write-Host "Copying Build Files from $bin_debug to $versao_teste..."
if (Test-Path $bin_debug) {
    Copy-Item -Path "$bin_debug\*" -Destination $versao_teste -Recurse -Force
    Write-Host "Copy Complete."
} else {
    Write-Host "ERROR: Bin Debug not found!"
}

Write-Host "Moving Source Files..."
$exclude = @('_VERSAO_TESTE', '_DEV_PROJETO', 'organize.ps1', 'organize_log_ps.txt')
Get-ChildItem -Path $source | Where-Object { $_.Name -notin $exclude } | Move-Item -Destination $dev_projeto -Force

Write-Host "Done Moving."

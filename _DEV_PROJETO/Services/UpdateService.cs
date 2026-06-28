using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LetreiroDigital.Models;

namespace LetreiroDigital.Services
{
    /// <summary>
    /// Servico de Auto-Update para o Letreiro Digital.
    /// 
    /// ARQUITETURA:
    /// App (C#) --GET /version.json--> Firebase/Nuvem (version.json, setup_v2.2.exe)
    /// App (C#) <--Resposta JSON------
    ///   |
    ///   | (Nova versao detectada)
    ///   v
    /// Download -> %TEMP%\LetreiroDigital\setup_vX.X.X.exe + SHA256
    ///   |
    ///   | (Validacao OK)
    ///   v
    /// 1. Inicia o setup.exe (modo silencioso)
    /// 2. Encerra todos os processos do app
    /// 3. Setup substitui os arquivos
    /// 4. Setup reinicia o app automaticamente
    /// </summary>
    public class UpdateService
    {
        // ===============================================================
        // CONFIGURACAO - Altere estes valores para apontar a nuvem
        // ===============================================================

        /// <summary>
        /// URL publica do arquivo version.json na nuvem.
        /// Pode ser Firebase Hosting, Firebase Storage, AWS S3, GitHub Releases, etc.
        /// </summary>
        private const string VersionJsonUrl =
            "https://letreirodigital-88f8e-default-rtdb.firebaseio.com/app_update.json";

        /// <summary>
        /// Versao atual do aplicativo (deve bater com o assembly).
        /// Atualize manualmente a cada release, ou leia do Assembly.
        /// </summary>
        public static readonly string CurrentVersion = "4.6.0";
        public static readonly int CurrentBuildNumber = 1;

        // ===============================================================

        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        private readonly string _updateDir;
        private CancellationTokenSource? _downloadCts;

        public event Action<UpdateState>? StateChanged;

        private UpdateState _state = new();
        public UpdateState State => _state;

        public UpdateService()
        {
            _updateDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LetreiroDigital", "Updates");

            if (!Directory.Exists(_updateDir))
                Directory.CreateDirectory(_updateDir);
        }

        // ===============================================================
        // 1. VERIFICAR ATUALIZACAO
        // ===============================================================

        /// <summary>
        /// Consulta o version.json na nuvem e compara com a versao atual.
        /// </summary>
        public async Task<UpdateInfo?> CheckForUpdateAsync()
        {
            _state = new UpdateState { IsChecking = true, StatusMessage = "Verificando atualizações..." };
            NotifyStateChanged();

            try
            {
                var response = await _http.GetStringAsync(VersionJsonUrl);
                var info = JsonSerializer.Deserialize<UpdateInfo>(response);

                if (info == null)
                {
                    SetError("Resposta inválida do servidor.");
                    return null;
                }

                bool hasUpdate = CompareVersions(info.Version, CurrentVersion) > 0;

                _state = new UpdateState
                {
                    HasUpdate = hasUpdate,
                    AvailableUpdate = hasUpdate ? info : null,
                    StatusMessage = hasUpdate
                        ? $"Nova versão disponível: v{info.Version}"
                        : "Você está na versão mais recente! ✅"
                };
                NotifyStateChanged();

                return hasUpdate ? info : null;
            }
            catch (HttpRequestException ex)
            {
                SetError($"Sem conexão com o servidor: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException)
            {
                SetError("Tempo esgotado ao conectar ao servidor.");
                return null;
            }
            catch (Exception ex)
            {
                SetError($"Erro ao verificar: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Verificacao silenciosa (para uso no startup, sem erros visiveis).
        /// Retorna null se nao houver atualizacao ou se houver erro de rede.
        /// </summary>
        public async Task<UpdateInfo?> CheckSilentlyAsync()
        {
            try
            {
                var response = await _http.GetStringAsync(VersionJsonUrl);
                var info = JsonSerializer.Deserialize<UpdateInfo>(response);

                if (info != null && CompareVersions(info.Version, CurrentVersion) > 0)
                    return info;
            }
            catch { /* Silencioso - sem rede ou erro, ignora */ }

            return null;
        }

        // ===============================================================
        // 2. DOWNLOAD DA ATUALIZACAO
        // ===============================================================

        /// <summary>
        /// Baixa o instalador da atualizacao com barra de progresso.
        /// </summary>
        public async Task<bool> DownloadUpdateAsync(UpdateInfo updateInfo)
        {
            if (string.IsNullOrEmpty(updateInfo.DownloadUrl))
            {
                SetError("URL de download não disponível.");
                return false;
            }

            _downloadCts = new CancellationTokenSource();
            var token = _downloadCts.Token;

            _state = new UpdateState
            {
                IsDownloading = true,
                AvailableUpdate = updateInfo,
                StatusMessage = "Iniciando download..."
            };
            NotifyStateChanged();

            string filePath = "";

            try
            {
                string downloadUrl = updateInfo.DownloadUrl;

                // -- GOOGLE DRIVE: Tratamento de confirmacao de virus --
                // O Google Drive bloqueia downloads diretos de arquivos grandes
                // e retorna uma pagina HTML pedindo confirmacao.
                if (downloadUrl.Contains("drive.google.com"))
                {
                    _state.StatusMessage = "Conectando ao Google Drive...";
                    NotifyStateChanged();

                    // Extrai o ID do arquivo do link
                    string? fileId = null;
                    var idMatch = System.Text.RegularExpressions.Regex.Match(downloadUrl, @"[?&]id=([a-zA-Z0-9_-]+)");
                    if (idMatch.Success) fileId = idMatch.Groups[1].Value;
                    else
                    {
                        var pathMatch = System.Text.RegularExpressions.Regex.Match(downloadUrl, @"/d/([a-zA-Z0-9_-]+)");
                        if (pathMatch.Success) fileId = pathMatch.Groups[1].Value;
                    }

                    if (fileId != null)
                    {
                        // Usa o formato com confirm=t que ignora a verificacao de virus
                        downloadUrl = $"https://drive.google.com/uc?export=download&confirm=t&id={fileId}";
                    }
                }

                using var response = await _http.GetAsync(downloadUrl,
                    HttpCompletionOption.ResponseHeadersRead, token);

                response.EnsureSuccessStatusCode();

                // Segunda verificacao: Se ainda receber HTML, rejeita
                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType != null && contentType.Contains("text/html"))
                {
                    SetError("O Google Drive bloqueou o download. O arquivo pode ser muito grande ou não estar compartilhado publicamente.\n\nSolução: Hospede o arquivo no Firebase Storage.");
                    return false;
                }

                // -- DETECCAO DE EXTENSAO ROBUSTA --
                // Google Drive nao retorna extensao no Content-Disposition nem na URL.
                // Por isso, SEMPRE salvamos como .zip (nosso padrao de atualizacao).
                // Apos o download, verificamos os bytes magicos do arquivo para confirmar.
                string extension = ".zip"; // Padrao: sempre ZIP para atualizacoes
                
                // Tenta detectar do Content-Disposition (caso hospede fora do Drive)
                var contentDisposition = response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
                if (!string.IsNullOrEmpty(contentDisposition))
                {
                    string detectedExt = Path.GetExtension(contentDisposition);
                    if (!string.IsNullOrEmpty(detectedExt))
                        extension = detectedExt;
                }

                string fileName = $"LetreiroDigital_Update_v{updateInfo.Version}{extension}";
                filePath = Path.Combine(_updateDir, fileName);

                long totalBytes = response.Content.Headers.ContentLength ?? -1;
                _state.TotalBytes = totalBytes;

                using var contentStream = await response.Content.ReadAsStreamAsync(token);
                using var fileStream = new FileStream(filePath, FileMode.Create,
                    FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;
                var lastProgressUpdate = DateTime.MinValue;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, token);
                    totalRead += bytesRead;

                    // Atualiza progresso a cada 100ms para nao travar a UI
                    if ((DateTime.Now - lastProgressUpdate).TotalMilliseconds > 100)
                    {
                        _state.DownloadedBytes = totalRead;
                        _state.DownloadProgress = totalBytes > 0
                            ? (double)totalRead / totalBytes
                            : 0;
                        _state.StatusMessage = totalBytes > 0
                            ? $"Baixando... {totalRead / 1048576.0:F1} MB / {totalBytes / 1048576.0:F1} MB"
                            : $"Baixando... {totalRead / 1048576.0:F1} MB";
                        NotifyStateChanged();

                        lastProgressUpdate = DateTime.Now;
                    }
                }

                // -- VERIFICACAO DE BYTES MAGICOS (ZIP = PK = 0x50 0x4B) --
                // Se o arquivo nao comeca com "PK", nao e um ZIP valido.
                // Isso pega casos onde o Drive retorna lixo ou o link esta errado.
                try
                {
                    using var checkStream = File.OpenRead(filePath);
                    byte[] magic = new byte[2];
                    checkStream.Read(magic, 0, 2);
                    if (magic[0] != 0x50 || magic[1] != 0x4B) // "PK"
                    {
                        checkStream.Close();
                        // Nao e ZIP - pode ser HTML ou arquivo corrompido
                        try { File.Delete(filePath); } catch { }
                        SetError("O arquivo baixado não é um ZIP válido. Verifique se o link de download está correto e compartilhado publicamente.");
                        return false;
                    }
                }
                catch { /* Se nao conseguir ler, tenta continuar */ }

                // Garante que a extensao seja .zip se o conteudo for ZIP
                if (!filePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    string correctedPath = Path.ChangeExtension(filePath, ".zip");
                    try
                    {
                        if (File.Exists(correctedPath)) File.Delete(correctedPath);
                        File.Move(filePath, correctedPath);
                        filePath = correctedPath;
                    }
                    catch { /* Mantem o path original se nao conseguir renomear */ }
                }

                // Validacao SHA256 (se disponivel)
                if (!string.IsNullOrEmpty(updateInfo.Sha256))
                {
                    _state.StatusMessage = "Validando integridade do arquivo...";
                    NotifyStateChanged();

                    string fileHash = ComputeSha256(filePath);
                    if (!string.Equals(fileHash, updateInfo.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Delete(filePath); } catch { }
                        SetError("Arquivo corrompido! O hash SHA256 não confere. Tente novamente.");
                        return false;
                    }
                }

                // Sucesso!
                _state = new UpdateState
                {
                    IsReady = true,
                    HasUpdate = true,
                    AvailableUpdate = updateInfo,
                    DownloadedFilePath = filePath,
                    DownloadProgress = 1.0,
                    StatusMessage = "Download concluído! Pronto para instalar. 🚀"
                };
                NotifyStateChanged();

                return true;
            }
            catch (OperationCanceledException)
            {
                try { File.Delete(filePath); } catch { }
                _state = new UpdateState
                {
                    StatusMessage = "Download cancelado.",
                    AvailableUpdate = updateInfo,
                    HasUpdate = true
                };
                NotifyStateChanged();
                return false;
            }
            catch (Exception ex)
            {
                try { File.Delete(filePath); } catch { }
                SetError($"Erro no download: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cancela o download em andamento.
        /// </summary>
        public void CancelDownload()
        {
            _downloadCts?.Cancel();
        }

        // ===============================================================
        // 3. INSTALAR ATUALIZACAO - EXTRACAO NATIVA + SUBSTITUICAO
        // ===============================================================

        /// <summary>
        /// Extrai o ZIP nativamente com System.IO.Compression e cria um
        /// script .bat para copiar os arquivos apos o app fechar.
        /// 
        /// FLUXO:
        /// 1. Extrai ZIP para pasta temporaria (Work) usando .NET nativo
        /// 2. Cria update_launcher.bat com xcopy
        /// 3. App se encerra
        /// 4. Bat aguarda, copia arquivos, limpa temp, reinicia app
        /// </summary>
        public bool InstallUpdate(string installerPath)
        {
            if (!File.Exists(installerPath))
            {
                SetError("Arquivo de atualização não encontrado.");
                return false;
            }

            try
            {
                _state.StatusMessage = "Preparando extração...";
                NotifyStateChanged();

                string updateWorkDir = Path.Combine(_updateDir, "update_temp");
                if (Directory.Exists(updateWorkDir)) Directory.Delete(updateWorkDir, true);
                Directory.CreateDirectory(updateWorkDir);

                string appExePath = Process.GetCurrentProcess().MainModule?.FileName ?? "LetreiroDigital.exe";
                string appDir = Path.GetDirectoryName(appExePath) ?? AppDomain.CurrentDomain.BaseDirectory;
                string appProcessName = Path.GetFileNameWithoutExtension(appExePath);

                // -- EXTRACAO NATIVA COM System.IO.Compression --
                // Usa ZipFile.ExtractToDirectory do .NET - NAO depende de WinRAR/7-Zip
                _state.StatusMessage = "Extraindo arquivos da atualização...";
                NotifyStateChanged();

                try
                {
                    ZipFile.ExtractToDirectory(installerPath, updateWorkDir, true);
                }
                catch (InvalidDataException)
                {
                    // Arquivo nao e um ZIP valido
                    try { Directory.Delete(updateWorkDir, true); } catch { }
                    SetError("O arquivo baixado não é um ZIP válido. O download pode ter falhado.");
                    return false;
                }
                catch (Exception ex)
                {
                    try { Directory.Delete(updateWorkDir, true); } catch { }
                    SetError($"Erro ao extrair ZIP: {ex.Message}");
                    return false;
                }

                // Verifica se a extracao produziu arquivos
                var extractedFiles = Directory.GetFiles(updateWorkDir, "*", SearchOption.AllDirectories);
                if (extractedFiles.Length == 0)
                {
                    try { Directory.Delete(updateWorkDir, true); } catch { }
                    SetError("O ZIP está vazio ou não contém arquivos válidos.");
                    return false;
                }

                _state.StatusMessage = $"Extração concluída ({extractedFiles.Length} arquivos). Preparando substituição...";
                NotifyStateChanged();

                // -- GERA O BATCH DE SUBSTITUICAO --
                string batchPath = Path.Combine(_updateDir, "update_launcher.bat");
                string batchContent = $@"@echo off
chcp 65001 >NUL
title Letreiro Digital - Atualizando...
echo.
echo ================================================
echo    LETREIRO DIGITAL - ATUALIZACAO EM ANDAMENTO
echo ================================================
echo.
echo Aguardando o aplicativo encerrar...

timeout /t 3 /nobreak >NUL

:WAIT_LOOP
tasklist /FI ""IMAGENAME eq {appProcessName}.exe"" 2>NUL | find /I /N ""{appProcessName}.exe"" >NUL
if ""%ERRORLEVEL%""==""0"" (
    echo Ainda aguardando...
    timeout /t 2 /nobreak >NUL
    goto WAIT_LOOP
)

echo.
echo Aplicativo encerrado. Copiando novos arquivos...
echo Fonte: {updateWorkDir}
echo Destino: {appDir}
echo.

:: Copia recursiva de todos os arquivos extraidos para a pasta do app
:: /Y = sobrescreve sem perguntar, /E = pastas e subpastas
:: /H = arquivos ocultos, /I = assume pasta, /C = continua em erro
xcopy ""{updateWorkDir}\*"" ""{appDir}\"" /Y /E /H /I /C

echo.
echo ================================================
echo    ATUALIZACAO CONCLUIDA COM SUCESSO!
echo ================================================
echo.
echo Limpando arquivos temporarios...
rd /s /q ""{updateWorkDir}"" 2>NUL
del /f /q ""{installerPath}"" 2>NUL

echo Iniciando o aplicativo...
timeout /t 2 /nobreak >NUL
start """" ""{appExePath}""

:: Remove o proprio batch
del ""%~f0""
";

                File.WriteAllText(batchPath, batchContent, System.Text.Encoding.UTF8);

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"\"{batchPath}\"\"",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal,
                    CreateNoWindow = false
                };

                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                SetError($"Erro ao processar atualização: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Limpa arquivos de atualizacao antigos da pasta temporaria.
        /// </summary>
        public void CleanupOldUpdates()
        {
            try
            {
                if (Directory.Exists(_updateDir))
                {
                    foreach (var file in Directory.GetFiles(_updateDir, "*.exe"))
                    {
                        try { File.Delete(file); } catch { }
                    }
                    foreach (var file in Directory.GetFiles(_updateDir, "*.zip"))
                    {
                        try { File.Delete(file); } catch { }
                    }
                    foreach (var file in Directory.GetFiles(_updateDir, "*.bat"))
                    {
                        try { File.Delete(file); } catch { }
                    }
                    // Limpa pasta temporaria de extracao
                    string tempDir = Path.Combine(_updateDir, "update_temp");
                    if (Directory.Exists(tempDir))
                    {
                        try { Directory.Delete(tempDir, true); } catch { }
                    }
                }
            }
            catch { }
        }

        // ===============================================================
        // UTILITARIOS
        // ===============================================================

        /// <summary>
        /// Compara duas versoes semanticas (ex: "2.1.0" vs "2.2.0").
        /// Retorna:  >0 se a > b,  0 se iguais,  &lt;0 se a &lt; b
        /// </summary>
        public static int CompareVersions(string a, string b)
        {
            try
            {
                var vA = new Version(a);
                var vB = new Version(b);
                return vA.CompareTo(vB);
            }
            catch
            {
                return string.Compare(a, b, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Calcula o hash SHA256 de um arquivo (para validacao de integridade).
        /// </summary>
        private static string ComputeSha256(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private void SetError(string message)
        {
            _state = new UpdateState
            {
                HasError = true,
                ErrorMessage = message,
                StatusMessage = message,
                AvailableUpdate = _state.AvailableUpdate,
                HasUpdate = _state.HasUpdate
            };
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(_state);
        }
    }
}

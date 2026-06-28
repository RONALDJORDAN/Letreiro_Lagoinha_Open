using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LetreiroDigital.Models
{
    /// <summary>
    /// Modelo para o JSON de versionamento hospedado na nuvem.
    /// Estrutura esperada no Firebase/servidor:
    /// {
    ///   "version": "2.2.0",
    ///   "build_number": 220,
    ///   "release_date": "2026-02-22",
    ///   "download_url": "https://firebasestorage.googleapis.com/.../LetreiroDigital_Setup_v2.2.0.exe",
    ///   "file_size_mb": 45.2,
    ///   "sha256": "abc123...",
    ///   "required": false,
    ///   "min_version": "2.0.0",
    ///   "changelog": [
    ///     "Novo sistema de temas",
    ///     "Correções de performance"
    ///   ],
    ///   "changelog_url": "https://...",
    ///   "severity": "recommended"
    /// }
    /// </summary>
    public class UpdateInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "";

        [JsonPropertyName("build_number")]
        public int BuildNumber { get; set; }

        [JsonPropertyName("release_date")]
        public string ReleaseDate { get; set; } = "";

        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; } = "";

        [JsonPropertyName("file_size_mb")]
        public double FileSizeMb { get; set; }

        [JsonPropertyName("sha256")]
        public string Sha256 { get; set; } = "";

        /// <summary>
        /// Se true, o app FORÇA a atualização (impede uso até atualizar).
        /// </summary>
        [JsonPropertyName("required")]
        public bool Required { get; set; }

        /// <summary>
        /// Versão mínima que pode rodar sem atualizar.
        /// Se a versão atual for menor que isso, a atualização é obrigatória.
        /// </summary>
        [JsonPropertyName("min_version")]
        public string MinVersion { get; set; } = "";

        [JsonPropertyName("changelog")]
        public List<string> Changelog { get; set; } = new();

        [JsonPropertyName("changelog_url")]
        public string ChangelogUrl { get; set; } = "";

        /// <summary>
        /// "critical", "recommended", "optional"
        /// </summary>
        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "optional";
    }

    /// <summary>
    /// Estado local do progresso de atualização.
    /// </summary>
    public class UpdateState
    {
        public bool IsChecking { get; set; }
        public bool IsDownloading { get; set; }
        public bool IsReady { get; set; }
        public bool HasUpdate { get; set; }
        public bool HasError { get; set; }

        public double DownloadProgress { get; set; } // 0.0 → 1.0
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }

        public string StatusMessage { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public UpdateInfo? AvailableUpdate { get; set; }
        public string? DownloadedFilePath { get; set; }
    }
}

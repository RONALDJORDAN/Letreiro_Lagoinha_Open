using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LetreiroDigital.Models;

namespace LetreiroDigital.Services
{
    /// <summary>
    /// Serviço para gerenciamento de temas (salvar, carregar, aplicar)
    /// </summary>
    public class ThemeManager
    {
        private readonly string _themesDirectory;
        private readonly string _defaultThemePath;
        private List<Theme> _availableThemes;

        public ThemeManager()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _themesDirectory = Path.Combine(appDataPath, "LetreiroDigital", "Themes");
            _defaultThemePath = Path.Combine(_themesDirectory, "default.json");
            
            Directory.CreateDirectory(_themesDirectory);
            _availableThemes = new List<Theme>();
            
            LoadAllThemes();
            EnsureDefaultTheme();
        }

        // ==================== TEMAS PADRÃO ====================
        private void EnsureDefaultTheme()
        {
            if (!_availableThemes.Any(t => t.Name == "Padrão"))
            {
                var defaultTheme = new Theme
                {
                    Name = "Padrão",
                    TitleColor = "#FFFFFF",
                    BodyColor = "#E0E0E0",
                    BackgroundColor = "#1A1A1A"
                };
                SaveTheme(defaultTheme);
            }

            // Criar alguns temas pré-definidos
            CreateSampleThemesIfNeeded();
        }

        private void CreateSampleThemesIfNeeded()
        {
            var sampleThemes = new List<Theme>
            {
                new Theme
                {
                    Name = "Culto Jovens",
                    TitleColor = "#00FF88",
                    BodyColor = "#FFFFFF",
                    BackgroundType = "Gradient",
                    BackgroundGradientStart = "#1A0033",
                    BackgroundGradientEnd = "#330066",
                    TitleFontSize = 84,
                    TitleShadowBlur = 12,
                    BorderEnabled = true,
                    BorderColor = "#00FF88"
                },
                new Theme
                {
                    Name = "Domingo Manhã",
                    TitleColor = "#FFD700",
                    BodyColor = "#F0F0F0",
                    BackgroundType = "Gradient",
                    BackgroundGradientStart = "#001A33",
                    BackgroundGradientEnd = "#003366",
                    TitleFontSize = 78,
                    TitleFontFamily = "Georgia",
                    TitleShadowBlur = 10
                },
                new Theme
                {
                    Name = "Celebração",
                    TitleColor = "#FF6B6B",
                    BodyColor = "#FFE5E5",
                    BackgroundType = "Gradient",
                    BackgroundGradientStart = "#2C003E",
                    BackgroundGradientEnd = "#5A007A",
                    TitleFontSize = 90,
                    TitleShadowBlur = 15,
                    BorderEnabled = true,
                    BorderColor = "#FF6B6B",
                    BorderCornerRadius = 20
                },
                new Theme
                {
                    Name = "Minimalista",
                    TitleColor = "#FFFFFF",
                    BodyColor = "#CCCCCC",
                    BackgroundColor = "#000000",
                    TitleFontFamily = "Segoe UI Light",
                    TitleFontSize = 72,
                    TitleShadowEnabled = false,
                    BodyShadowEnabled = false,
                    BorderEnabled = false
                }
            };

            foreach (var theme in sampleThemes)
            {
                if (!_availableThemes.Any(t => t.Name == theme.Name))
                {
                    SaveTheme(theme);
                }
            }
        }

        // ==================== SALVAR/CARREGAR ====================
        public void SaveTheme(Theme theme)
        {
            theme.ModifiedAt = DateTime.Now;
            var json = JsonSerializer.Serialize(theme, new JsonSerializerOptions { WriteIndented = true });
            var filePath = Path.Combine(_themesDirectory, $"{SanitizeFileName(theme.Name)}.json");
            
            File.WriteAllText(filePath, json);
            
            // Atualizar lista
            var existing = _availableThemes.FirstOrDefault(t => t.Id == theme.Id);
            if (existing != null)
                _availableThemes.Remove(existing);
            _availableThemes.Add(theme);
        }

        public Theme LoadTheme(string themeName)
        {
            var filePath = Path.Combine(_themesDirectory, $"{SanitizeFileName(themeName)}.json");
            if (!File.Exists(filePath))
                return _availableThemes.FirstOrDefault() ?? new Theme();

            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<Theme>(json) ?? new Theme();
            }
            catch
            {
                return new Theme();
            }
        }

        public void LoadAllThemes()
        {
            _availableThemes.Clear();
            
            if (!Directory.Exists(_themesDirectory))
                return;

            foreach (var file in Directory.GetFiles(_themesDirectory, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var theme = JsonSerializer.Deserialize<Theme>(json);
                    if (theme != null)
                        _availableThemes.Add(theme);
                }
                catch
                {
                    // Ignorar arquivos corrompidos
                }
            }
        }

        public void DeleteTheme(string themeName)
        {
            if (themeName == "Padrão")
                return; // Não pode deletar o padrão

            var filePath = Path.Combine(_themesDirectory, $"{SanitizeFileName(themeName)}.json");
            if (File.Exists(filePath))
                File.Delete(filePath);

            _availableThemes.RemoveAll(t => t.Name == themeName);
        }

        public List<Theme> GetAllThemes()
        {
            return new List<Theme>(_availableThemes);
        }

        public Theme GetThemeByName(string name)
        {
            return _availableThemes.FirstOrDefault(t => t.Name == name) ?? new Theme();
        }

        // ==================== UTILITÁRIOS ====================
        private string SanitizeFileName(string fileName)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }

        public bool ThemeExists(string name)
        {
            return _availableThemes.Any(t => t.Name == name);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LetreiroDigital.Models;

namespace LetreiroDigital.Services
{
    public class DataService
    {
        private readonly string _dataDir;
        private readonly string _scheduleFile;
        private readonly string _configFile;
        private readonly string _presetsFile;

        public static readonly string[] DaysOfWeek = { "Culto Fé", "Culto de Celebração" };

        public static readonly List<ScheduleItem> DefaultSchedule = new()
        {
            new() { Time = "18:20 as 18:25", Sigla = "Videos", Content = "Vídeo de Abertura", Duration = "5min", Lead = "Vídeo", Color = "#808080" },
            new() { Time = "18:25 as 18:30", Sigla = "ORA", Content = "Oração Inicial", Duration = "5min", Lead = "", Color = "#FFC107" },
            new() { Time = "18:30 as 18:55", Sigla = "MSC", Content = "Lagoinha Music", Duration = "25min", Lead = "Lagoinha Music", Color = "#FFEB3B" },
            new() { Time = "18:55 as 19:00", Sigla = "ORA", Content = "ORAÇÃO INTERCESSÃO", Duration = "5min", Lead = "", Color = "#D2691E" },
            new() { Time = "19:00 as 19:05", Sigla = "PLV", Content = "LAGOINHA NEWS", Duration = "5min", Lead = "", Color = "#FF8C00" },
            new() { Time = "19:05 as 19:09", Sigla = "PLV", Content = "PALAVRA DE OFERTA", Duration = "4min", Lead = "", Color = "#E0E0E0" },
            new() { Time = "19:09 as 19:14", Sigla = "MSC", Content = "Lagoinha AMusic", Duration = "5min", Lead = "Lagoinha Music", Color = "#FFEB3B" },
            new() { Time = "19:14 as 19:16", Sigla = "ORA", Content = "ORAÇÃO PELA OFERTA", Duration = "2min", Lead = "", Color = "#90EE90" },
            new() { Time = "19:16 as 19:56", Sigla = "PLV", Content = "PALAVRA DO CULTO", Duration = "40min", Lead = "", Color = "#4682B4" },
            new() { Time = "19:56 as 19:58", Sigla = "ORA", Content = "APELO FINAL", Duration = "2min", Lead = "", Color = "#E0E0E0" },
            new() { Time = "19:58 as 20:00", Sigla = "FIM", Content = "FINALIZAÇÃO", Duration = "2min", Lead = "", Color = "#FF0000" },
        };

        public static readonly Dictionary<string, ModeConfig> DefaultModeConfigs = new()
        {
            ["PROJECTION"] = new ModeConfig { ClockSize = 30, ClockScale = 1.0, BannerHeight = 80, ScheduleSize = 12, ScheduleWidth = 300, TextMode = "full", BgColor = "#CC0000" },
            ["TV_MODE"] = new ModeConfig { ClockSize = 30, ClockScale = 1.0, BannerHeight = 90, ScheduleSize = 12, ScheduleWidth = 300, TextMode = "full", BgColor = "#002366" },
        };

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public DataService()
        {
            _dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(_dataDir)) Directory.CreateDirectory(_dataDir);

            _scheduleFile = Path.Combine(_dataDir, "schedule_data.json");
            _configFile = Path.Combine(_dataDir, "app_config.json");
            _presetsFile = Path.Combine(_dataDir, "presets.json");
        }

        // ==================== SCHEDULE ====================
        public Dictionary<string, List<ScheduleItem>> LoadSchedule()
        {
            if (File.Exists(_scheduleFile))
            {
                try
                {
                    var json = File.ReadAllText(_scheduleFile);
                    var data = JsonSerializer.Deserialize<Dictionary<string, List<ScheduleItem>>>(json, _jsonOpts);
                    if (data != null)
                    {
                        // MIGRATION: Rename Keys
                        if (data.ContainsKey("Quarta") && !data.ContainsKey("Culto Fé"))
                        {
                            data["Culto Fé"] = data["Quarta"];
                            data.Remove("Quarta");
                        }
                        if (data.ContainsKey("Domingo") && !data.ContainsKey("Culto de Celebração"))
                        {
                            data["Culto de Celebração"] = data["Domingo"];
                            data.Remove("Domingo");
                        }

                        // Ensure required keys exist
                        foreach (var day in DaysOfWeek)
                        {
                            if (!data.ContainsKey(day))
                                data[day] = GetDefaultScheduleForDay(day);
                        }
                        
                        // FILTER: Remove keys that are not in DaysOfWeek (User Request: "Remove others")
                        // But we only do this if we have our main keys to avoid wiping everything on error
                        var keysToRemove = data.Keys.Where(k => !DaysOfWeek.Contains(k)).ToList();
                        foreach(var k in keysToRemove) data.Remove(k);

                        return data;
                    }
                }
                catch { /* ignore, return defaults */ }
            }

            var result = new Dictionary<string, List<ScheduleItem>>();
            foreach (var day in DaysOfWeek)
                result[day] = GetDefaultScheduleForDay(day);
            return result;
        }

        public void SaveSchedule(Dictionary<string, List<ScheduleItem>> schedule)
        {
            try
            {
                var json = JsonSerializer.Serialize(schedule, _jsonOpts);
                File.WriteAllText(_scheduleFile, json);
            }
            catch { /* ignore */ }
        }

        public void ExportSchedule(string filePath, Dictionary<string, List<ScheduleItem>> schedule)
        {
            try
            {
                var json = JsonSerializer.Serialize(schedule, _jsonOpts);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao exportar cronograma: {ex.Message}");
            }
        }

        public Dictionary<string, List<ScheduleItem>>? ImportSchedule(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<Dictionary<string, List<ScheduleItem>>>(json, _jsonOpts);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao importar cronograma: {ex.Message}");
            }
        }

        // ==================== EXCEL (CSV) ====================
        public void ExportScheduleToCsv(string filePath, Dictionary<string, List<ScheduleItem>> schedule)
        {
            try
            {
                var lines = new List<string>();
                // Header explicativo (Renomeado DIA para EVENTO)
                lines.Add("EVENTO;HORARIO;SIGLA;TITULO PRINCIPAL;SUBTITULO (LEAD);DURACAO;COR (HEX);DESCRIÇÃO E DICAS DE PREENCHIMENTO");

                foreach (var day in schedule)
                {
                    foreach (var item in day.Value)
                    {
                        // Escapa ponto-e-vírgula se existir no conteúdo para não quebrar colunas
                        string content = item.Content?.Replace(";", ",") ?? "";
                        string lead = item.Lead?.Replace(";", ",") ?? "";
                        string row = $"{day.Key};{item.Time};{item.Sigla};{content};{lead};{item.Duration};{item.Color};DICA: Horário no padrão '00:00 as 00:00'. Duração ex: '5min'. Cor ex: '#FF0000'.";
                        lines.Add(row);
                    }
                }
                
                // Salva com BOM (Byte Order Mark) para o Excel reconhecer acentos e formato UTF-8 automaticamente
                File.WriteAllLines(filePath, lines, new System.Text.UTF8Encoding(true));
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao exportar Excel (CSV): {ex.Message}");
            }
        }

        public Dictionary<string, List<ScheduleItem>>? ImportScheduleFromCsv(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
                if (lines.Length <= 1) return null;

                var result = new Dictionary<string, List<ScheduleItem>>();

                // Pula o cabeçalho (i=0)
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Trata as aspas criadas pelo Excel, caso hajam
                    var parts = line.Split(';').Select(p => p.Trim('"', ' ', '\t')).ToArray();
                    if (parts.Length < 7) continue; // Garante que temos as 7 colunas mínimas (Evento até Cor)

                    string day = parts[0];
                    var item = new ScheduleItem
                    {
                        Time = parts[1],
                        Sigla = parts[2],
                        Content = parts[3],
                        Lead = parts[4],
                        Duration = parts[5],
                        Color = parts[6]
                    };

                    if (!result.ContainsKey(day)) result[day] = new List<ScheduleItem>();
                    result[day].Add(item);
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao importar Excel (CSV): {ex.Message}");
            }
        }

        public List<ScheduleItem> GetDefaultScheduleForDay(string day)
        {
            // Culto Fe = Quarta (+60 min offset from 18:20 base? Or use Quarta logic)
            // Assuming Culto Fé is approximately at 19:20-ish like Quarta was.
            // "Culto de Celebração" = delta 0   → horários base (ex: 18:20-20:00)
            // "Culto Fé"            = delta +60  → 1h depois     (ex: 19:20-21:00)
            int delta = (day == "Culto Fé" || day == "Quarta") ? 60 : 0;
            return DefaultSchedule.Select(item =>
            {
                var clone = item.Clone();
                if (delta != 0) clone.Time = AdjustTimeString(item.Time, delta);
                return clone;
            }).ToList();
        }

        // ==================== CONFIG ====================
        public AppConfig LoadConfig()
        {
            if (File.Exists(_configFile))
            {
                try
                {
                    var json = File.ReadAllText(_configFile);
                    var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOpts);
                    if (config != null) return config;
                }
                catch { /* ignore */ }
            }
            return new AppConfig
            {
                CurrentMode = "PROJECTION",
                ModeConfigs = DefaultModeConfigs.ToDictionary(kv => kv.Key, kv => kv.Value.Clone())
            };
        }

        public void SaveConfig(AppConfig config)
        {
            try
            {
                var json = JsonSerializer.Serialize(config, _jsonOpts);
                File.WriteAllText(_configFile, json);
            }
            catch { /* ignore */ }
        }

        // ==================== PRESETS ====================
        public PresetData? LoadPreset(int slotId)
        {
            if (!File.Exists(_presetsFile)) return null;
            try
            {
                var json = File.ReadAllText(_presetsFile);
                var presets = JsonSerializer.Deserialize<Dictionary<string, PresetData>>(json, _jsonOpts);
                if (presets != null && presets.TryGetValue(slotId.ToString(), out var data))
                    return data;
            }
            catch { /* ignore */ }
            return null;
        }

        public void SavePreset(int slotId, PresetData data)
        {
            Dictionary<string, PresetData> presets = new();
            if (File.Exists(_presetsFile))
            {
                try
                {
                    var json = File.ReadAllText(_presetsFile);
                    presets = JsonSerializer.Deserialize<Dictionary<string, PresetData>>(json, _jsonOpts) ?? new();
                }
                catch { presets = new(); }
            }
            presets[slotId.ToString()] = data;
            File.WriteAllText(_presetsFile, JsonSerializer.Serialize(presets, _jsonOpts));
        }

        public void ResetPresets()
        {
            if (File.Exists(_presetsFile)) File.Delete(_presetsFile);
        }

        // ==================== UTILITY ====================
        public static int? ParseDuration(string? durationStr)
        {
            if (string.IsNullOrWhiteSpace(durationStr) || durationStr == "-------") return null;
            var cleaned = durationStr.ToLower().Replace("min", "").Trim();
            if (int.TryParse(cleaned, out int mins))
                return mins * 60;
            return null;
        }

        public static string FormatTimer(int seconds)
        {
            int mins = seconds / 60;
            int secs = seconds % 60;
            return $"{mins:D2}:{secs:D2}";
        }

        public static string AdjustTimeString(string timeStr, int minutesDelta)
        {
            if (string.IsNullOrEmpty(timeStr) || !timeStr.Contains(':')) return timeStr;
            try
            {
                var parts = timeStr.Split(" as ");
                var newParts = parts.Select(part =>
                {
                    var hm = part.Trim().Split(':');
                    int h = int.Parse(hm[0]);
                    int m = int.Parse(hm[1]);
                    int totalMin = h * 60 + m + minutesDelta;
                    totalMin = ((totalMin % 1440) + 1440) % 1440;
                    int newH = totalMin / 60;
                    int newM = totalMin % 60;
                    return $"{newH:D2}:{newM:D2}";
                });
                return string.Join(" as ", newParts);
            }
            catch { return timeStr; }
        }
    }
}

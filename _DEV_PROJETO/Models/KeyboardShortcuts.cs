using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace LetreiroDigital.Models
{
    /// <summary>
    /// Configurações de atalhos de teclado personalizáveis
    /// </summary>
    public class KeyboardShortcuts
    {
        // ==================== NAVEGAÇÃO ====================
        public Key NextItem { get; set; } = Key.Down;
        public Key PreviousItem { get; set; } = Key.Up;
        public Key FirstItem { get; set; } = Key.Home;
        public Key LastItem { get; set; } = Key.End;

        // ==================== CONTROLE DE APRESENTAÇÃO ====================
        public Key StartPresentation { get; set; } = Key.F5;
        public Key StopPresentation { get; set; } = Key.Escape;
        public Key ToggleBanner { get; set; } = Key.F11;
        public Key ToggleClock { get; set; } = Key.F9;
        public Key ToggleTimer { get; set; } = Key.F10;

        // ==================== TIMER ====================
        public Key StartTimer { get; set; } = Key.Space;
        public Key PauseTimer { get; set; } = Key.P;
        public Key ResetTimer { get; set; } = Key.R;
        public Key AddMinute { get; set; } = Key.Add;
        public Key SubtractMinute { get; set; } = Key.Subtract;

        // ==================== MODOS ====================
        public Key SwitchToProjection { get; set; } = Key.F1;
        public Key SwitchToTvMode { get; set; } = Key.F2;

        // ==================== EDIÇÃO ====================
        public Key OpenEditor { get; set; } = Key.E;
        public Key DeleteItem { get; set; } = Key.Delete;
        public Key DuplicateItem { get; set; } = Key.D;

        // ==================== SALVAMENTO ====================
        public Key QuickSave { get; set; } = Key.S; // Ctrl+S
        public Key SaveAs { get; set; } = Key.None;

        // Método para verificar se uma tecla está em uso
        public bool IsKeyInUse(Key key)
        {
            var properties = GetType().GetProperties();
            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(Key))
                {
                    if (prop.GetValue(this) is Key value && value == key && key != Key.None)
                        return true;
                }
            }
            return false;
        }

        // Resetar para padrões
        public void ResetToDefaults()
        {
            NextItem = Key.Down;
            PreviousItem = Key.Up;
            FirstItem = Key.Home;
            LastItem = Key.End;
            StartPresentation = Key.F5;
            StopPresentation = Key.Escape;
            ToggleBanner = Key.F11;
            ToggleClock = Key.F9;
            ToggleTimer = Key.F10;
            StartTimer = Key.Space;
            PauseTimer = Key.P;
            ResetTimer = Key.R;
            AddMinute = Key.Add;
            SubtractMinute = Key.Subtract;
            SwitchToProjection = Key.F1;
            SwitchToTvMode = Key.F2;
            OpenEditor = Key.E;
            DeleteItem = Key.Delete;
            DuplicateItem = Key.D;
            QuickSave = Key.S;
        }
    }
}

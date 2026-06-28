using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LetreiroDigital.Models
{
    /// <summary>
    /// Representa um tema visual completo para apresentações
    /// </summary>
    public class Theme
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Novo Tema";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;

        // ==================== FONTES ====================
        public string TitleFontFamily { get; set; } = "Oswald";
        public int TitleFontSize { get; set; } = 72;
        public string TitleFontWeight { get; set; } = "Black";
        public string TitleFontStyle { get; set; } = "Normal";
        public string TitleColor { get; set; } = "#FFFFFF";

        public string BodyFontFamily { get; set; } = "Segoe UI";
        public int BodyFontSize { get; set; } = 24;
        public string BodyFontWeight { get; set; } = "Normal";
        public string BodyColor { get; set; } = "#E0E0E0";
        
        // ==================== NOVAS PROPRIEDADES DE COR ====================
        // Labels Principais
        public string EventLabelColor { get; set; } = "#FF3333"; // "EVENTO ATUAL"
        public string ScheduleHeaderColor { get; set; } = "#FF3333"; // "CRONOGRAMA"
        public string SimpleLabelColor { get; set; } = "#FFFFFF"; // "MINUTOS", "RELÓGIO"
        
        // Cronograma
        public string ScheduleItemActiveColor { get; set; } = "#FFFFFF";
        public string ScheduleItemInactiveColor { get; set; } = "#CCCCCC";
        
        // Valores Digitais (LED)
        public string DigitalTimerColor { get; set; } = "#FF0000";
        public string DigitalClockColor { get; set; } = "#FF0000";
        
        // Rodapé (Footer)
        public string FooterBgColor { get; set; } = "#050508";
        public string FooterTextColor { get; set; } = "#FF4411";
        
        // Painéis de Fundo
        public string PanelPrimaryBgColor { get; set; } = "#11141B"; // Área Principal
        public string PanelSecondaryBgColor { get; set; } = "#1A1D2E"; // Lateral Direita
        
        // Indicador Ao Vivo
        public string LiveIndicatorColor { get; set; } = "#00FF00";

        // Fontes específicas do Modo TV
        public string DigitalFontFamily { get; set; } = "Digital-7";
        public string FooterFontFamily { get; set; } = "Led Board-7";

        // Tamanhos específicos do Modo TV
        public int TvTimerSize { get; set; } = 96;
        public int TvClockSize { get; set; } = 88;
        public int TvFooterSize { get; set; } = 38;

        // ==================== ALINHAMENTOS ====================
        public string TitleAlignment { get; set; } = "Center"; // Left, Center, Right
        public string BodyAlignment { get; set; } = "Center";
        public string VerticalAlignment { get; set; } = "Center"; // Top, Center, Bottom

        // ==================== SOMBRAS ====================
        public bool TitleShadowEnabled { get; set; } = true;
        public string TitleShadowColor { get; set; } = "#000000";
        public double TitleShadowBlur { get; set; } = 8.0;
        public double TitleShadowDepth { get; set; } = 3.0;
        public double TitleShadowOpacity { get; set; } = 0.8;

        public bool BodyShadowEnabled { get; set; } = true;
        public string BodyShadowColor { get; set; } = "#000000";
        public double BodyShadowBlur { get; set; } = 6.0;
        public double BodyShadowDepth { get; set; } = 2.0;
        public double BodyShadowOpacity { get; set; } = 0.6;

        // ==================== BORDAS ====================
        public bool BorderEnabled { get; set; } = false;
        public string BorderColor { get; set; } = "#FFD700";
        public double BorderThickness { get; set; } = 2.0;
        public double BorderCornerRadius { get; set; } = 10.0;

        // ==================== FUNDO ====================
        public string BackgroundType { get; set; } = "Solid"; // Solid, Gradient, Image
        public string BackgroundColor { get; set; } = "#1A1A1A";
        public string BackgroundGradientStart { get; set; } = "#1A1A1A";
        public string BackgroundGradientEnd { get; set; } = "#2D2D2D";
        public string BackgroundImagePath { get; set; } = "";
        public double BackgroundOpacity { get; set; } = 1.0;

        // ==================== MARGENS E PADDING ====================
        public double MarginTop { get; set; } = 40;
        public double MarginBottom { get; set; } = 40;
        public double MarginLeft { get; set; } = 60;
        public double MarginRight { get; set; } = 60;
        public double PaddingBetweenElements { get; set; } = 20;

        // ==================== TRANSIÇÕES ====================
        public double TransitionDuration { get; set; } = 0.5; // seconds
        public string TransitionType { get; set; } = "Fade"; // Fade, Slide, None

        // Clone method for editing
        public Theme Clone()
        {
            return new Theme
            {
                Id = Guid.NewGuid().ToString(), // New ID for clone
                Name = this.Name + " (Cópia)",
                TitleFontFamily = this.TitleFontFamily,
                TitleFontSize = this.TitleFontSize,
                TitleFontWeight = this.TitleFontWeight,
                TitleFontStyle = this.TitleFontStyle,
                TitleColor = this.TitleColor,
                BodyFontFamily = this.BodyFontFamily,
                BodyFontSize = this.BodyFontSize,
                BodyFontWeight = this.BodyFontWeight,
                BodyColor = this.BodyColor,
                EventLabelColor = this.EventLabelColor,
                ScheduleHeaderColor = this.ScheduleHeaderColor,
                SimpleLabelColor = this.SimpleLabelColor,
                ScheduleItemActiveColor = this.ScheduleItemActiveColor,
                ScheduleItemInactiveColor = this.ScheduleItemInactiveColor,
                DigitalTimerColor = this.DigitalTimerColor,
                DigitalClockColor = this.DigitalClockColor,
                FooterBgColor = this.FooterBgColor,
                FooterTextColor = this.FooterTextColor,
                PanelPrimaryBgColor = this.PanelPrimaryBgColor,
                PanelSecondaryBgColor = this.PanelSecondaryBgColor,
                LiveIndicatorColor = this.LiveIndicatorColor,
                DigitalFontFamily = this.DigitalFontFamily,
                FooterFontFamily = this.FooterFontFamily,
                TvTimerSize = this.TvTimerSize,
                TvClockSize = this.TvClockSize,
                TvFooterSize = this.TvFooterSize,
                TitleAlignment = this.TitleAlignment,
                BodyAlignment = this.BodyAlignment,
                VerticalAlignment = this.VerticalAlignment,
                TitleShadowEnabled = this.TitleShadowEnabled,
                TitleShadowColor = this.TitleShadowColor,
                TitleShadowBlur = this.TitleShadowBlur,
                TitleShadowDepth = this.TitleShadowDepth,
                TitleShadowOpacity = this.TitleShadowOpacity,
                BodyShadowEnabled = this.BodyShadowEnabled,
                BodyShadowColor = this.BodyShadowColor,
                BodyShadowBlur = this.BodyShadowBlur,
                BodyShadowDepth = this.BodyShadowDepth,
                BodyShadowOpacity = this.BodyShadowOpacity,
                BorderEnabled = this.BorderEnabled,
                BorderColor = this.BorderColor,
                BorderThickness = this.BorderThickness,
                BorderCornerRadius = this.BorderCornerRadius,
                BackgroundType = this.BackgroundType,
                BackgroundColor = this.BackgroundColor,
                BackgroundGradientStart = this.BackgroundGradientStart,
                BackgroundGradientEnd = this.BackgroundGradientEnd,
                BackgroundImagePath = this.BackgroundImagePath,
                BackgroundOpacity = this.BackgroundOpacity,
                MarginTop = this.MarginTop,
                MarginBottom = this.MarginBottom,
                MarginLeft = this.MarginLeft,
                MarginRight = this.MarginRight,
                PaddingBetweenElements = this.PaddingBetweenElements,
                TransitionDuration = this.TransitionDuration,
                TransitionType = this.TransitionType
            };
        }
    }
}

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using LetreiroDigital.Models;
using LetreiroDigital.Services;
using LetreiroDigital.ViewModels;

namespace LetreiroDigital.Views
{
    public class FontItem
    {
        public string Name { get; set; } = string.Empty;
        public FontFamily? Family { get; set; }
    }

    public partial class ThemeEditorWindow : Window
    {
        private readonly ThemeManager _themeManager;
        private Theme _currentTheme;
        private readonly AppViewModel _previewVm;
        public Theme? SelectedTheme { get; private set; }
        private bool _isLoading = false; // Prevent preview updates during load
        private bool _isDirty = false;   // Track unsaved edits

        public ThemeEditorWindow(Theme? initialTheme = null)
        {
            InitializeComponent();
            _themeManager = new ThemeManager();
            _currentTheme = initialTheme?.Clone() ?? new Theme();
            
            // Initialize Preview VM
            _previewVm = new AppViewModel();
            _previewVm.Initialize();
            
            _previewVm.CurrentItem = new ScheduleItem 
            { 
                Content = "EXEMPLO DE TÍTULO", 
                Time = "18:00 as 20:00",
                Sigla = "CULTO"
            };
            
            previewControl.SetViewModel(_previewVm);

            InitializeFontComboBoxes();
            LoadThemesList();
            LoadThemeToUI(_currentTheme);

            // Auto-select the active theme in the list and update window title
            if (initialTheme != null)
            {
                Title = $"Theme Studio — Editando: {initialTheme.Name}";
                var match = lstThemes.Items.Cast<object>().FirstOrDefault(i => i?.ToString() == initialTheme.Name);
                if (match != null)
                    lstThemes.SelectedItem = match;
            }
        }

        // ==================== INICIALIZAÇÃO ====================
        private void InitializeFontComboBoxes()
        {
            var fonts = Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(f => f).ToList();
            
            // Add embedded fonts to the top
            fonts.Insert(0, "Led Board-7");
            fonts.Insert(0, "Digital-7 Mono");
            fonts.Insert(0, "Digital-7");
            fonts.Insert(0, "Oswald");

            var fontItems = fonts.Select(name =>
            {
                var path = AppViewModel.ResolveFontPath(name);
                return new FontItem { Name = name, Family = new FontFamily(path != name ? $"{path}, {name}" : name) };
            }).ToList();

            cboTitleFont.SelectedValuePath = "Name";
            cboBodyFont.SelectedValuePath = "Name";
            cboDigitalFont.SelectedValuePath = "Name";
            cboFooterFont.SelectedValuePath = "Name";

            cboTitleFont.ItemsSource = fontItems;
            cboBodyFont.ItemsSource = fontItems;
            cboDigitalFont.ItemsSource = fontItems;
            cboFooterFont.ItemsSource = fontItems;
            
            cboTitleFont.SelectedValue = "Oswald";
            cboBodyFont.SelectedValue = "Segoe UI";
            cboDigitalFont.SelectedValue = "Digital-7";
            cboFooterFont.SelectedValue = "Led Board-7";
        }

        private void LoadThemesList()
        {
            var themes = _themeManager.GetAllThemes();
            lstThemes.ItemsSource = themes.Select(t => t.Name).ToList();
        }

        // ==================== CARREGAR/SALVAR TEMA ====================
        private void LoadThemeToUI(Theme theme)
        {
            _isLoading = true;
            try
            {
                _currentTheme = theme;
                txtThemeName.Text = theme.Name;
                
                // Fontes Título
                cboTitleFont.SelectedValue = theme.TitleFontFamily;
                sliderTitleSize.Value = theme.TitleFontSize;
                txtTitleColor.Text = theme.TitleColor;
                SetComboByContent(cboTitleWeight, theme.TitleFontWeight);
                SetComboByContent(cboTitleStyle, theme.TitleFontStyle);
                
                // Fontes Corpo
                cboBodyFont.SelectedValue = theme.BodyFontFamily;
                sliderBodySize.Value = theme.BodyFontSize;
                txtBodyColor.Text = theme.BodyColor;

                // Fontes TV
                cboDigitalFont.SelectedValue = theme.DigitalFontFamily;
                cboFooterFont.SelectedValue = theme.FooterFontFamily;
                sliderTimerSize.Value = theme.TvTimerSize;
                sliderClockSize.Value = theme.TvClockSize;
                sliderFooterSize.Value = theme.TvFooterSize;

                // Cores da Tela
                txtEventLabelColor.Text = theme.EventLabelColor;
                txtScheduleHeaderColor.Text = theme.ScheduleHeaderColor;
                txtSimpleLabelColor.Text = theme.SimpleLabelColor;
                txtScheduleActiveColor.Text = theme.ScheduleItemActiveColor;
                txtScheduleInactiveColor.Text = theme.ScheduleItemInactiveColor;
                txtTimerColor.Text = theme.DigitalTimerColor;
                txtClockColor.Text = theme.DigitalClockColor;
                txtFooterBgColor.Text = theme.FooterBgColor;
                txtFooterTextColor.Text = theme.FooterTextColor;
                txtPanelPrimaryColor.Text = theme.PanelPrimaryBgColor;
                txtPanelSecondaryColor.Text = theme.PanelSecondaryBgColor;
                txtLiveColor.Text = theme.LiveIndicatorColor;
                
                // Alinhamento
                switch (theme.TitleAlignment)
                {
                    case "Left": cboAlignment.SelectedIndex = 0; break;
                    case "Center": cboAlignment.SelectedIndex = 1; break;
                    case "Right": cboAlignment.SelectedIndex = 2; break;
                }
                switch (theme.VerticalAlignment)
                {
                    case "Top": cboVerticalAlign.SelectedIndex = 0; break;
                    case "Center": cboVerticalAlign.SelectedIndex = 1; break;
                    case "Bottom": cboVerticalAlign.SelectedIndex = 2; break;
                }
                
                // Sombras Título
                chkTitleShadow.IsChecked = theme.TitleShadowEnabled;
                txtTitleShadowColor.Text = theme.TitleShadowColor;
                sliderShadowBlur.Value = theme.TitleShadowBlur;
                sliderShadowDepth.Value = theme.TitleShadowDepth;
                sliderTitleShadowOpacity.Value = theme.TitleShadowOpacity;

                // Sombras Corpo
                chkBodyShadow.IsChecked = theme.BodyShadowEnabled;
                txtBodyShadowColor.Text = theme.BodyShadowColor;
                sliderBodyShadowBlur.Value = theme.BodyShadowBlur;
                sliderBodyShadowOpacity.Value = theme.BodyShadowOpacity;
                
                // Bordas
                chkBorder.IsChecked = theme.BorderEnabled;
                txtBorderColor.Text = theme.BorderColor;
                sliderBorderThickness.Value = theme.BorderThickness;
                sliderBorderRadius.Value = theme.BorderCornerRadius;
                
                // Fundo
                cboBackgroundType.SelectedIndex = theme.BackgroundType == "Solid" ? 0 : 1;
                txtBackgroundColor.Text = theme.BackgroundColor;
                txtGradientEnd.Text = theme.BackgroundGradientEnd;
                sliderBgOpacity.Value = theme.BackgroundOpacity;

                // Margens
                sliderMarginTop.Value = theme.MarginTop;
                sliderMarginBottom.Value = theme.MarginBottom;
                sliderMarginLeft.Value = theme.MarginLeft;
                sliderMarginRight.Value = theme.MarginRight;
                sliderPadding.Value = theme.PaddingBetweenElements;
                
                // Transições
                SetComboByContent(cboTransitionType, theme.TransitionType == "None" ? "Nenhuma" : theme.TransitionType);
                sliderTransitionDuration.Value = theme.TransitionDuration;
            }
            finally
            {
                _isLoading = false;
                _isDirty = false; // Loading a theme is not a user edit
            }
            
            UpdatePreview();
            UpdateAllValueLabels();
        }

        private void SetComboByContent(ComboBox combo, string content)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is ComboBoxItem item && item.Content?.ToString() == content)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            combo.SelectedIndex = 0;
        }

        private Theme GetThemeFromUI()
        {
            _currentTheme.Name = txtThemeName.Text;
            
            // Fontes Título
            _currentTheme.TitleFontFamily = cboTitleFont.SelectedValue?.ToString() ?? "Oswald";
            _currentTheme.TitleFontSize = (int)sliderTitleSize.Value;
            _currentTheme.TitleColor = txtTitleColor.Text;
            _currentTheme.TitleFontWeight = (cboTitleWeight.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Black";
            _currentTheme.TitleFontStyle = (cboTitleStyle.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Normal";
            
            // Fontes Corpo
            _currentTheme.BodyFontFamily = cboBodyFont.SelectedValue?.ToString() ?? "Segoe UI";
            _currentTheme.BodyFontSize = (int)sliderBodySize.Value;
            _currentTheme.BodyColor = txtBodyColor.Text;

            // Fontes TV
            _currentTheme.DigitalFontFamily = cboDigitalFont.SelectedValue?.ToString() ?? "Digital-7";
            _currentTheme.FooterFontFamily = cboFooterFont.SelectedValue?.ToString() ?? "Led Board-7";
            _currentTheme.TvTimerSize = (int)sliderTimerSize.Value;
            _currentTheme.TvClockSize = (int)sliderClockSize.Value;
            _currentTheme.TvFooterSize = (int)sliderFooterSize.Value;

            // Cores da Tela
            _currentTheme.EventLabelColor = txtEventLabelColor.Text;
            _currentTheme.ScheduleHeaderColor = txtScheduleHeaderColor.Text;
            _currentTheme.SimpleLabelColor = txtSimpleLabelColor.Text;
            _currentTheme.ScheduleItemActiveColor = txtScheduleActiveColor.Text;
            _currentTheme.ScheduleItemInactiveColor = txtScheduleInactiveColor.Text;
            _currentTheme.DigitalTimerColor = txtTimerColor.Text;
            _currentTheme.DigitalClockColor = txtClockColor.Text;
            _currentTheme.FooterBgColor = txtFooterBgColor.Text;
            _currentTheme.FooterTextColor = txtFooterTextColor.Text;
            _currentTheme.PanelPrimaryBgColor = txtPanelPrimaryColor.Text;
            _currentTheme.PanelSecondaryBgColor = txtPanelSecondaryColor.Text;
            _currentTheme.LiveIndicatorColor = txtLiveColor.Text;
            
            // Alinhamento
            _currentTheme.TitleAlignment = cboAlignment.SelectedIndex switch
            {
                0 => "Left",
                2 => "Right",
                _ => "Center"
            };
            _currentTheme.BodyAlignment = _currentTheme.TitleAlignment;
            _currentTheme.VerticalAlignment = cboVerticalAlign.SelectedIndex switch
            {
                0 => "Top",
                2 => "Bottom",
                _ => "Center"
            };
            
            // Sombras Título
            _currentTheme.TitleShadowEnabled = chkTitleShadow.IsChecked == true;
            _currentTheme.TitleShadowColor = txtTitleShadowColor.Text;
            _currentTheme.TitleShadowBlur = sliderShadowBlur.Value;
            _currentTheme.TitleShadowDepth = sliderShadowDepth.Value;
            _currentTheme.TitleShadowOpacity = sliderTitleShadowOpacity.Value;

            // Sombras Corpo
            _currentTheme.BodyShadowEnabled = chkBodyShadow.IsChecked == true;
            _currentTheme.BodyShadowColor = txtBodyShadowColor.Text;
            _currentTheme.BodyShadowBlur = sliderBodyShadowBlur.Value;
            _currentTheme.BodyShadowDepth = 2.0;
            _currentTheme.BodyShadowOpacity = sliderBodyShadowOpacity.Value;
            
            // Bordas
            _currentTheme.BorderEnabled = chkBorder.IsChecked == true;
            _currentTheme.BorderColor = txtBorderColor.Text;
            _currentTheme.BorderThickness = sliderBorderThickness.Value;
            _currentTheme.BorderCornerRadius = sliderBorderRadius.Value;
            
            // Fundo
            _currentTheme.BackgroundType = cboBackgroundType.SelectedIndex == 0 ? "Solid" : "Gradient";
            _currentTheme.BackgroundColor = txtBackgroundColor.Text;
            _currentTheme.BackgroundGradientStart = txtBackgroundColor.Text;
            _currentTheme.BackgroundGradientEnd = txtGradientEnd.Text;
            _currentTheme.BackgroundOpacity = sliderBgOpacity.Value;

            // Margens
            _currentTheme.MarginTop = sliderMarginTop.Value;
            _currentTheme.MarginBottom = sliderMarginBottom.Value;
            _currentTheme.MarginLeft = sliderMarginLeft.Value;
            _currentTheme.MarginRight = sliderMarginRight.Value;
            _currentTheme.PaddingBetweenElements = sliderPadding.Value;
            
            // Transições
            var transType = (cboTransitionType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Fade";
            _currentTheme.TransitionType = transType == "Nenhuma" ? "None" : transType;
            _currentTheme.TransitionDuration = sliderTransitionDuration.Value;
            
            return _currentTheme;
        }

        // ==================== PREVIEW EM TEMPO REAL ====================
        private void UpdatePreview()
        {
            if (_isLoading) return;
            try
            {
                var theme = GetThemeFromUI();
                ApplyThemeToViewModel(theme);
                UpdateAllValueLabels();
            }
            catch
            {
                // Ignorar erros durante digitação
            }
        }

        private void UpdateAllValueLabels()
        {
            try
            {
                lblTitleSizeVal.Text = $"{(int)sliderTitleSize.Value}";
                lblBodySizeVal.Text = $"{(int)sliderBodySize.Value}";
                lblTimerSizeVal.Text = $"{(int)sliderTimerSize.Value}";
                lblClockSizeVal.Text = $"{(int)sliderClockSize.Value}";
                lblFooterSizeVal.Text = $"{(int)sliderFooterSize.Value}";
                lblShadowBlurVal.Text = $"{sliderShadowBlur.Value:F0}";
                lblShadowDepthVal.Text = $"{sliderShadowDepth.Value:F0}";
                lblTitleShadowOpacityVal.Text = $"{sliderTitleShadowOpacity.Value:F2}";
                lblBodyShadowBlurVal.Text = $"{sliderBodyShadowBlur.Value:F0}";
                lblBodyShadowOpacityVal.Text = $"{sliderBodyShadowOpacity.Value:F2}";
                lblBorderThicknessVal.Text = $"{sliderBorderThickness.Value:F0}";
                lblBorderRadiusVal.Text = $"{sliderBorderRadius.Value:F0}";
                lblBgOpacityVal.Text = $"{sliderBgOpacity.Value:F2}";
                lblMarginTopVal.Text = $"{(int)sliderMarginTop.Value}";
                lblMarginBottomVal.Text = $"{(int)sliderMarginBottom.Value}";
                lblMarginLeftVal.Text = $"{(int)sliderMarginLeft.Value}";
                lblMarginRightVal.Text = $"{(int)sliderMarginRight.Value}";
                lblPaddingVal.Text = $"{(int)sliderPadding.Value}";
                lblTransitionValue.Text = $"{sliderTransitionDuration.Value:F1}s";
            }
            catch { }
        }

        private void ApplyThemeToViewModel(Theme theme)
        {
            _previewVm.TitleFontFamily = theme.TitleFontFamily;
            _previewVm.BodyFontFamily = theme.BodyFontFamily;
            _previewVm.DigitalFontFamily = theme.DigitalFontFamily;
            _previewVm.FooterFontFamily = theme.FooterFontFamily;
            
            _previewVm.TitleColor = theme.TitleColor;
            _previewVm.BodyColor = theme.BodyColor;
            
            _previewVm.EventLabelColor = theme.EventLabelColor;
            _previewVm.ScheduleHeaderColor = theme.ScheduleHeaderColor;
            _previewVm.SimpleLabelColor = theme.SimpleLabelColor;
            _previewVm.ScheduleItemActiveColor = theme.ScheduleItemActiveColor;
            _previewVm.ScheduleItemInactiveColor = theme.ScheduleItemInactiveColor;
            _previewVm.DigitalTimerColor = theme.DigitalTimerColor;
            _previewVm.DigitalClockColor = theme.DigitalClockColor;
            _previewVm.FooterBgColor = theme.FooterBgColor;
            _previewVm.FooterTextColor = theme.FooterTextColor;
            _previewVm.PanelPrimaryBgColor = theme.PanelPrimaryBgColor;
            _previewVm.PanelSecondaryBgColor = theme.PanelSecondaryBgColor;
            _previewVm.LiveIndicatorColor = theme.LiveIndicatorColor;

            _previewVm.TvEventSize = theme.TitleFontSize;
            _previewVm.TvListSize = theme.BodyFontSize;
            _previewVm.TvTimerSize = theme.TvTimerSize;
            _previewVm.TvClockSize = theme.TvClockSize;
            _previewVm.TvFooterSize = theme.TvFooterSize;
        }

        // ==================== EVENTOS ====================
        private void OnPropertyChanged(object sender, RoutedEventArgs e)
        {
            _isDirty = true;
            UpdatePreview();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_isDirty && DialogResult != true)
            {
                var result = MessageBox.Show(
                    "Você fez alterações não aplicadas.\r\nDeseja aplicar e salvar antes de fechar?",
                    "Alterações Pendentes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (result == MessageBoxResult.Yes)
                {
                    SelectedTheme = GetThemeFromUI();
                    _currentTheme = SelectedTheme;
                    _themeManager.SaveTheme(_currentTheme);
                    DialogResult = true;
                }
            }
            base.OnClosing(e);
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            _currentTheme = new Theme { Name = "Novo Tema" };
            LoadThemeToUI(_currentTheme);
            _isDirty = false;
            Title = "Theme Studio — Novo Tema";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtThemeName.Text))
            {
                MessageBox.Show("Digite um nome para o tema!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var theme = GetThemeFromUI();
            _themeManager.SaveTheme(theme);
            LoadThemesList();
            
            MessageBox.Show($"Tema '{theme.Name}' salvo com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (lstThemes.SelectedItem == null)
            {
                MessageBox.Show("Selecione um tema para excluir!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var themeName = lstThemes.SelectedItem.ToString() ?? "";
            
            if (MessageBox.Show($"Deseja realmente excluir o tema '{themeName}'?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _themeManager.DeleteTheme(themeName);
                LoadThemesList();
                MessageBox.Show("Tema excluído!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LstThemes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstThemes.SelectedItem != null)
            {
                var themeName = lstThemes.SelectedItem.ToString() ?? "";
                var theme = _themeManager.LoadTheme(themeName);
                LoadThemeToUI(theme);
            }
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            SelectedTheme = GetThemeFromUI();

            // Ask to save permanently
            var result = MessageBox.Show("Deseja salvar permanentemente as alterações feitas neste tema (sobrescrever arquivo)?\r\n\r\n'Sim' para salvar e aplicar.\r\n'Não' para apenas aplicar temporariamente.", 
                                       "Salvar Alterações", 
                                       MessageBoxButton.YesNoCancel, 
                                       MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Cancel) return;

            if (result == MessageBoxResult.Yes)
            {
                _currentTheme = SelectedTheme;
                _themeManager.SaveTheme(_currentTheme);
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnPickColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                TextBox? targetBox = tag switch
                {
                    "Title" => txtTitleColor,
                    "Body" => txtBodyColor,
                    "Border" => txtBorderColor,
                    "Background" => txtBackgroundColor,
                    "GradientEnd" => txtGradientEnd,
                    "TitleShadow" => txtTitleShadowColor,
                    "BodyShadow" => txtBodyShadowColor,
                    "EventLabel" => txtEventLabelColor,
                    "ScheduleHeader" => txtScheduleHeaderColor,
                    "SimpleLabel" => txtSimpleLabelColor,
                    "ScheduleActive" => txtScheduleActiveColor,
                    "ScheduleInactive" => txtScheduleInactiveColor,
                    "DigitalTimer" => txtTimerColor,
                    "DigitalClock" => txtClockColor,
                    "FooterBg" => txtFooterBgColor,
                    "FooterText" => txtFooterTextColor,
                    "PanelPrimary" => txtPanelPrimaryColor,
                    "PanelSecondary" => txtPanelSecondaryColor,
                    "LiveIndicator" => txtLiveColor,
                    _ => null
                };

                if (targetBox != null)
                {
                    var picker = new ColorPickerWindow(targetBox.Text);
                    picker.Owner = this;
                    if (picker.ShowDialog() == true)
                    {
                        targetBox.Text = picker.SelectedColor;
                    }
                }
            }
        }

    }
}

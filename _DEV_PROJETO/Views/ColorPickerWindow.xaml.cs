using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LetreiroDigital.Views
{
    public partial class ColorPickerWindow : Window
    {
        public string SelectedColor { get; private set; } = "#FFFFFF";

        public class HexItem
        {
            public SolidColorBrush ColorBrush { get; set; } = Brushes.White;
            public string ColorHex { get; set; } = "#FFFFFF";
            public double X { get; set; }
            public double Y { get; set; }
        }

        public ColorPickerWindow(string initialColor = "")
        {
            InitializeComponent();
            InitializeHexGrid();
            
            if (!string.IsNullOrEmpty(initialColor))
            {
                txtColorCode.Text = initialColor;
            }
        }

        private void InitializeHexGrid()
        {
            var items = new List<HexItem>();
            
            // Definição de cores para o grid hexagonal (aproximação da imagem)
            // Centro
            items.Add(CreateHexItem("#FFFFFF", 0, 0)); // Branco
            
            // Camadas ao redor
            string[] ring1 = { "#FFFF00", "#00FF00", "#00FFFF", "#0000FF", "#FF00FF", "#FF0000" };
            
            // Expandir para gerar um gradiente hexagonal
            // Simplificação: gerando um grid de cores padrão
            

            double hexWidth = 26; // 24 + 2 margin
            double hexHeight = 22; // approx vertical spacing
            
            // Lista de cores básicas para popular o grid
            var baseColors = new List<string>
            {
                // Grayscale center
                "#FFFFFF", "#E0E0E0", "#C0C0C0", "#A0A0A0", "#808080", "#606060", "#404040", "#202020", "#000000",
                // Reds
                "#FFCCCC", "#FF9999", "#FF6666", "#FF3333", "#FF0000", "#CC0000", "#990000", "#660000", 
                // Oranges
                "#FFE5CC", "#FFCC99", "#FFB266", "#FF9933", "#FF8000", "#CC6600", "#994C00", "#663300",
                // Yellows
                "#FFFFCC", "#FFFF99", "#FFFF66", "#FFFF33", "#FFFF00", "#CCCC00", "#999900", "#666600",
                // Greens
                "#CCFFCC", "#99FF99", "#66FF66", "#33FF33", "#00FF00", "#00CC00", "#009900", "#006600",
                // Cyans
                "#CCFFFF", "#99FFFF", "#66FFFF", "#33FFFF", "#00FFFF", "#00CCCC", "#009999", "#006666",
                // Blues
                "#CCCCFF", "#9999FF", "#6666FF", "#3333FF", "#0000FF", "#0000CC", "#000099", "#000066",
                // Purples
                "#E5CCFF", "#CC99FF", "#B266FF", "#9933FF", "#8000FF", "#6600CC", "#4C0099", "#330066",
                // Magentas
                "#FFCCFF", "#FF99FF", "#FF66FF", "#FF33FF", "#FF00FF", "#CC00CC", "#990099", "#660099"
            };

            // Layout em espiral ou grid
            // Vamos usar um layout simples de linhas offset para simular hex grid
            int cols = 9;
            int rows = 8;
            
            int colorIndex = 0;
            
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (colorIndex >= baseColors.Count) break;
                    
                    double xPos = c * hexWidth + (r % 2 == 1 ? hexWidth / 2 : 0);
                    double yPos = r * hexHeight;
                    
                    // Centralizar no canvas
                    xPos += 20; 
                    yPos += 20;

                    items.Add(CreateHexItem(baseColors[colorIndex], xPos, yPos));
                    colorIndex++;
                }
            }

            HexGridItemsControl.ItemsSource = items;
        }

        private HexItem CreateHexItem(string hex, double x, double y)
        {
            try 
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                return new HexItem 
                { 
                    ColorBrush = new SolidColorBrush(color),
                    ColorHex = hex,
                    X = x,
                    Y = y
                };
            }
            catch
            {
                return new HexItem { X = x, Y = y };
            }
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is HexItem item)
            {
                txtColorCode.Text = item.ColorHex;
            }
        }

        private void TxtColorCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string text = txtColorCode.Text;
                if (!text.StartsWith("#") && (text.Length == 3 || text.Length == 6))
                {
                    text = "#" + text;
                }
                
                var color = (Color)ColorConverter.ConvertFromString(text);
                previewColorBox.Background = new SolidColorBrush(color);
                SelectedColor = text;
            }
            catch
            {
                // Invalid color, ignore
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

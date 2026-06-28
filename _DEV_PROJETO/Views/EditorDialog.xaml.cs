using System.Windows;
using LetreiroDigital.Models;

namespace LetreiroDigital.Views
{
    public partial class EditorDialog : Window
    {
        public ScheduleItem ResultItem { get; private set; } = new();

        public EditorDialog(ScheduleItem? existing = null)
        {
            InitializeComponent();
            if (existing != null)
            {
                txtTime.Text = existing.Time;
                txtSigla.Text = existing.Sigla;
                txtContent.Text = existing.Content;
                txtDuration.Text = existing.Duration;
                txtLead.Text = existing.Lead;
                txtColor.Text = existing.Color;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            ResultItem = new ScheduleItem
            {
                Time = txtTime.Text.Trim(),
                Sigla = txtSigla.Text.Trim(),
                Content = txtContent.Text.Trim(),
                Duration = txtDuration.Text.Trim(),
                Lead = txtLead.Text.Trim(),
                Color = string.IsNullOrWhiteSpace(txtColor.Text) ? "#FFEB3B" : txtColor.Text.Trim(),
            };
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

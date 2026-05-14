using Automate_Whatsapp.Logic;

namespace Automate_Whatsapp
{
    internal sealed class ConfigurationDialog : Form
    {
        private readonly NumericUpDown nudDelayBetweenMessages;

        public ConfigurationDialog(AutoWhatsAppSettings settings, Icon? applicationIcon)
        {
            Settings = settings.Normalize();

            Text = "Configuración de AutoWhatsApp";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(500, 250);

            if (applicationIcon != null)
            {
                Icon = (Icon)applicationIcon.Clone();
            }

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(18)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

            var iconBox = new PictureBox
            {
                Dock = DockStyle.Top,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Image = applicationIcon?.ToBitmap()
            };
            layout.Controls.Add(iconBox, 0, 0);
            layout.SetRowSpan(iconBox, 4);

            layout.Controls.Add(new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Text = "Configuración de envío",
                TextAlign = ContentAlignment.MiddleLeft
            }, 1, 0);

            layout.Controls.Add(new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = "Define cuánto esperar entre un mensaje y el siguiente.",
                TextAlign = ContentAlignment.MiddleLeft
            }, 1, 1);

            var delayLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            delayLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            delayLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));

            delayLayout.Controls.Add(new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = "Espera entre mensajes (minutos)",
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            nudDelayBetweenMessages = new NumericUpDown
            {
                Dock = DockStyle.Left,
                Minimum = AutoWhatsAppSettings.MinDelayBetweenMessagesMinutes,
                Maximum = AutoWhatsAppSettings.MaxDelayBetweenMessagesMinutes,
                Increment = 1,
                Size = new Size(86, 23),
                Value = Settings.DelayBetweenMessagesMinutes
            };
            delayLayout.Controls.Add(nudDelayBetweenMessages, 1, 0);
            layout.Controls.Add(delayLayout, 1, 2);

            layout.Controls.Add(new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(75, 85, 99),
                Text = "0 = enviar el siguiente mensaje sin espera adicional.",
                TextAlign = ContentAlignment.TopLeft
            }, 1, 3);

            var saveButton = new Button
            {
                Text = "Guardar",
                Size = new Size(96, 32)
            };
            saveButton.Click += saveButton_Click;

            var cancelButton = new Button
            {
                DialogResult = DialogResult.Cancel,
                Text = "Cancelar",
                Size = new Size(96, 32)
            };

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            actions.Controls.Add(cancelButton);
            actions.Controls.Add(saveButton);
            layout.Controls.Add(actions, 0, 4);
            layout.SetColumnSpan(actions, 2);

            AcceptButton = saveButton;
            CancelButton = cancelButton;
            Controls.Add(layout);
        }

        public AutoWhatsAppSettings Settings { get; private set; }

        private void saveButton_Click(object? sender, EventArgs e)
        {
            Settings = Settings with
            {
                DelayBetweenMessagesMinutes = (int)nudDelayBetweenMessages.Value
            };

            AutoWhatsAppSettingsStore.Save(Settings);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

using Automate_Whatsapp.Logic;

namespace Automate_Whatsapp
{
    internal sealed class ConfigurationDialog : Form
    {
        private NumericUpDown nudDelayBetweenMessages = null!;
        private ComboBox cmbWhatsAppLine = null!;
        private CheckBox chkAutoFallback = null!;
        private CheckedListBox checkedRunLines = null!;
        private Label lblRunLinesSummary = null!;
        private Label lblLinePreparationStatus = null!;
        private TextBox txtElevenLabsApiKey = null!;
        private TextBox txtElevenLabsVoiceId = null!;
        private TextBox txtElevenLabsModelId = null!;
        private TextBox txtElevenLabsOutputFormat = null!;
        private NumericUpDown nudElevenLabsStability = null!;
        private NumericUpDown nudElevenLabsSimilarityBoost = null!;
        private Label lblElevenLabsStatus = null!;
        private Button btnPrepareLine = null!;
        private Button btnPrepareAllLines = null!;
        private Button btnSaveElevenLabsSettings = null!;
        private Button btnTestElevenLabsSettings = null!;
        private readonly TabControl tabControl;
        private readonly TabPage whatsAppTab;
        private readonly ToolTip uiToolTip = new();
        private readonly ConfigurationDialogActions dialogActions;
        private readonly bool lineControlsEnabled;
        private readonly bool runLineSelectionEnabled;
        private readonly bool configureLinesEnabled;
        private readonly bool prepareLineEnabled;
        private readonly bool prepareAllLinesEnabled;
        private readonly bool elevenLabsControlsEnabled;

        private List<WhatsAppLine> whatsAppLines = new();
        private readonly HashSet<string> selectedLineIdsForRun = new(StringComparer.OrdinalIgnoreCase);
        private string? selectedWhatsAppLineId;
        private string elevenLabsStatusText;
        private bool suppressWhatsAppLineChanged;
        private bool suppressRunLineChecks;

        public ConfigurationDialog(
            AutoWhatsAppSettings settings,
            AppConfigurationState configurationState,
            IReadOnlyList<WhatsAppLine> whatsAppLines,
            Icon? applicationIcon,
            bool lineControlsEnabled,
            bool runLineSelectionEnabled,
            bool configureLinesEnabled,
            bool prepareLineEnabled,
            bool prepareAllLinesEnabled,
            bool elevenLabsControlsEnabled,
            ElevenLabsDialogState elevenLabsState,
            ConfigurationDialogActions dialogActions)
        {
            Settings = settings.Normalize();
            ConfigurationState = configurationState;
            selectedWhatsAppLineId = configurationState.SelectedWhatsAppLineId;
            elevenLabsStatusText = configurationState.ElevenLabsStatusText;
            selectedLineIdsForRun = new HashSet<string>(
                configurationState.SelectedLineIdsForRun,
                StringComparer.OrdinalIgnoreCase);
            this.dialogActions = dialogActions;
            this.lineControlsEnabled = lineControlsEnabled;
            this.runLineSelectionEnabled = runLineSelectionEnabled;
            this.configureLinesEnabled = configureLinesEnabled;
            this.prepareLineEnabled = prepareLineEnabled;
            this.prepareAllLinesEnabled = prepareAllLinesEnabled;
            this.elevenLabsControlsEnabled = elevenLabsControlsEnabled;

            Text = "Configuración de AutoWhatsApp";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(720, 520);

            if (applicationIcon != null)
            {
                Icon = (Icon)applicationIcon.Clone();
            }

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(18)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

            var iconBox = new PictureBox
            {
                Dock = DockStyle.Top,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Image = applicationIcon?.ToBitmap()
            };
            layout.Controls.Add(iconBox, 0, 0);
            layout.SetRowSpan(iconBox, 2);

            layout.Controls.Add(new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Text = "Configuración general",
                TextAlign = ContentAlignment.MiddleLeft
            }, 1, 0);

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };
            tabControl.TabPages.Add(CreateSendTab());
            whatsAppTab = CreateWhatsAppTab();
            tabControl.TabPages.Add(whatsAppTab);
            tabControl.TabPages.Add(CreateElevenLabsTab());
            layout.Controls.Add(tabControl, 1, 1);

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
            layout.Controls.Add(actions, 0, 2);
            layout.SetColumnSpan(actions, 2);

            AcceptButton = saveButton;
            CancelButton = cancelButton;
            Controls.Add(layout);

            chkAutoFallback.Checked = configurationState.AutoFallbackEnabled;
            nudDelayBetweenMessages.Value = Settings.DelayBetweenMessagesMinutes;
            SetWhatsAppLines(whatsAppLines, selectedWhatsAppLineId, includeSelectedLine: true);
            ApplyElevenLabsState(elevenLabsState);
            SetElevenLabsControlsEnabled(elevenLabsControlsEnabled);
            UpdateActionButtons();
        }

        public AutoWhatsAppSettings Settings { get; private set; }

        public AppConfigurationState ConfigurationState { get; private set; }

        private TabPage CreateSendTab()
        {
            var tab = new TabPage("Envío");
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

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
                Size = new Size(86, 23)
            };
            delayLayout.Controls.Add(nudDelayBetweenMessages, 1, 0);

            layout.Controls.Add(delayLayout, 0, 0);
            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(75, 85, 99),
                Text = "0 = enviar sin espera adicional",
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 1);
            tab.Controls.Add(layout);
            return tab;
        }

        private TabPage CreateWhatsAppTab()
        {
            var tab = new TabPage("WhatsApp");
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 98F));

            layout.Controls.Add(CreateLineSelectionSection(), 0, 0);
            layout.Controls.Add(CreateRunLinesSection(), 0, 1);
            layout.Controls.Add(CreatePreparationSection(), 0, 2);

            tab.Controls.Add(layout);
            return tab;
        }

        private Control CreateLineSelectionSection()
        {
            var group = new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = "Línea WhatsApp",
                Padding = new Padding(10, 8, 10, 10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));

            cmbWhatsAppLine = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = lineControlsEnabled
            };
            cmbWhatsAppLine.SelectedIndexChanged += cmbWhatsAppLine_SelectedIndexChanged;

            chkAutoFallback = new CheckBox
            {
                Anchor = AnchorStyles.Left,
                AutoSize = true,
                Enabled = lineControlsEnabled,
                Text = "Cambiar automáticamente si falla"
            };

            layout.Controls.Add(cmbWhatsAppLine, 0, 0);
            layout.Controls.Add(chkAutoFallback, 1, 0);
            group.Controls.Add(layout);
            return group;
        }

        private Control CreateRunLinesSection()
        {
            var group = new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = "Líneas de corrida",
                Padding = new Padding(10, 8, 10, 10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

            checkedRunLines = new CheckedListBox
            {
                CheckOnClick = true,
                Dock = DockStyle.Fill,
                Enabled = runLineSelectionEnabled,
                FormattingEnabled = true,
                IntegralHeight = false
            };
            checkedRunLines.ItemCheck += checkedRunLines_ItemCheck;

            lblRunLinesSummary = new Label
            {
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(75, 85, 99),
                TextAlign = ContentAlignment.MiddleLeft
            };

            layout.Controls.Add(checkedRunLines, 0, 0);
            layout.Controls.Add(lblRunLinesSummary, 0, 1);
            group.Controls.Add(layout);
            return group;
        }

        private Control CreatePreparationSection()
        {
            var group = new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = "Preparación",
                Padding = new Padding(10, 8, 10, 10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 376F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false
            };

            var btnConfigureLines = new Button
            {
                Enabled = configureLinesEnabled,
                Text = "Configurar líneas",
                Size = new Size(122, 30)
            };
            btnConfigureLines.Click += btnConfigureLines_Click;

            btnPrepareLine = new Button
            {
                Text = "Preparar línea",
                Size = new Size(112, 30)
            };
            btnPrepareLine.Click += btnPrepareLine_Click;

            btnPrepareAllLines = new Button
            {
                Text = "Preparar todas",
                Size = new Size(112, 30)
            };
            btnPrepareAllLines.Click += btnPrepareAllLines_Click;

            actions.Controls.Add(btnConfigureLines);
            actions.Controls.Add(btnPrepareLine);
            actions.Controls.Add(btnPrepareAllLines);

            lblLinePreparationStatus = new Label
            {
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(75, 85, 99),
                TextAlign = ContentAlignment.MiddleLeft
            };

            layout.Controls.Add(actions, 0, 0);
            layout.Controls.Add(lblLinePreparationStatus, 1, 0);
            group.Controls.Add(layout);
            return group;
        }

        private TabPage CreateElevenLabsTab()
        {
            var tab = new TabPage("ElevenLabs");
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 5,
                Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            txtElevenLabsApiKey = CreateTextBox(usePasswordChar: true);
            txtElevenLabsVoiceId = CreateTextBox();
            txtElevenLabsModelId = CreateTextBox();
            txtElevenLabsOutputFormat = CreateTextBox();
            nudElevenLabsStability = CreateRatioNumericUpDown();
            nudElevenLabsSimilarityBoost = CreateRatioNumericUpDown();

            AddLabel(layout, "API Key:", 0, 0);
            layout.Controls.Add(txtElevenLabsApiKey, 1, 0);
            AddLabel(layout, "Voice ID:", 2, 0);
            layout.Controls.Add(txtElevenLabsVoiceId, 3, 0);
            AddLabel(layout, "Modelo:", 0, 1);
            layout.Controls.Add(txtElevenLabsModelId, 1, 1);
            AddLabel(layout, "Output format:", 2, 1);
            layout.Controls.Add(txtElevenLabsOutputFormat, 3, 1);
            AddLabel(layout, "Stability:", 0, 2);
            layout.Controls.Add(nudElevenLabsStability, 1, 2);
            AddLabel(layout, "Similarity Boost:", 2, 2);
            layout.Controls.Add(nudElevenLabsSimilarityBoost, 3, 2);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false
            };
            btnSaveElevenLabsSettings = new Button
            {
                Text = "Guardar configuración",
                Size = new Size(140, 30)
            };
            btnSaveElevenLabsSettings.Click += btnSaveElevenLabsSettings_Click;

            btnTestElevenLabsSettings = new Button
            {
                Text = "Probar configuración",
                Size = new Size(130, 30)
            };
            btnTestElevenLabsSettings.Click += btnTestElevenLabsSettings_Click;

            lblElevenLabsStatus = new Label
            {
                AutoEllipsis = true,
                ForeColor = Color.FromArgb(75, 85, 99),
                Size = new Size(250, 40),
                Text = elevenLabsStatusText,
                TextAlign = ContentAlignment.MiddleLeft
            };

            actions.Controls.Add(btnSaveElevenLabsSettings);
            actions.Controls.Add(btnTestElevenLabsSettings);
            actions.Controls.Add(lblElevenLabsStatus);
            layout.Controls.Add(actions, 0, 3);
            layout.SetColumnSpan(actions, 4);

            tab.Controls.Add(layout);
            return tab;
        }

        private static TextBox CreateTextBox(bool usePasswordChar = false)
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 5, 12, 3),
                UseSystemPasswordChar = usePasswordChar
            };
        }

        private static NumericUpDown CreateRatioNumericUpDown()
        {
            return new NumericUpDown
            {
                DecimalPlaces = 2,
                Dock = DockStyle.Left,
                Increment = 0.05M,
                Maximum = 1M,
                Size = new Size(86, 23),
                Value = 0.75M
            };
        }

        private static void AddLabel(TableLayoutPanel layout, string text, int column, int row)
        {
            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            }, column, row);
        }

        private void SetWhatsAppLines(
            IReadOnlyList<WhatsAppLine> lines,
            string? selectedLineId,
            bool includeSelectedLine)
        {
            whatsAppLines = lines
                .Where(line => line.Enabled)
                .OrderBy(line => line.Priority)
                .ThenBy(line => line.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var enabledLineIds = whatsAppLines
                .Select(line => line.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            selectedLineIdsForRun.RemoveWhere(lineId => !enabledLineIds.Contains(lineId));

            var matchingLine = !string.IsNullOrWhiteSpace(selectedLineId)
                ? whatsAppLines.FirstOrDefault(line =>
                    string.Equals(line.Id, selectedLineId, StringComparison.OrdinalIgnoreCase))
                : null;

            matchingLine ??= whatsAppLines.FirstOrDefault();
            selectedWhatsAppLineId = matchingLine?.Id;
            if (includeSelectedLine && matchingLine != null)
            {
                selectedLineIdsForRun.Add(matchingLine.Id);
            }

            suppressWhatsAppLineChanged = true;
            try
            {
                cmbWhatsAppLine.DataSource = null;
                cmbWhatsAppLine.DisplayMember = nameof(WhatsAppLine.DisplayNameWithState);
                cmbWhatsAppLine.ValueMember = nameof(WhatsAppLine.Id);
                cmbWhatsAppLine.DataSource = whatsAppLines;
                if (matchingLine != null)
                {
                    cmbWhatsAppLine.SelectedItem = matchingLine;
                }
            }
            finally
            {
                suppressWhatsAppLineChanged = false;
            }

            RebuildRunLinesList();
            UpdateLinePreparationStatus();
            UpdateActionButtons();
        }

        private void RebuildRunLinesList()
        {
            suppressRunLineChecks = true;
            try
            {
                checkedRunLines.Items.Clear();
                checkedRunLines.DisplayMember = nameof(WhatsAppLine.DisplayNameWithState);
                foreach (var line in whatsAppLines)
                {
                    checkedRunLines.Items.Add(line, selectedLineIdsForRun.Contains(line.Id));
                }
            }
            finally
            {
                suppressRunLineChecks = false;
            }

            UpdateRunLinesSummaryFromState();
        }

        private void SyncSelectedRunLineIdsFromCheckedList()
        {
            selectedLineIdsForRun.Clear();
            foreach (var line in checkedRunLines.CheckedItems.Cast<WhatsAppLine>())
            {
                selectedLineIdsForRun.Add(line.Id);
            }
        }

        private void EnsureSelectedLineChecked()
        {
            var selectedLine = SelectedWhatsAppLine;
            if (selectedLine == null)
            {
                return;
            }

            SyncSelectedRunLineIdsFromCheckedList();
            selectedLineIdsForRun.Add(selectedLine.Id);

            suppressRunLineChecks = true;
            try
            {
                for (int i = 0; i < checkedRunLines.Items.Count; i++)
                {
                    if (checkedRunLines.Items[i] is WhatsAppLine line
                        && string.Equals(line.Id, selectedLine.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        checkedRunLines.SetItemChecked(i, true);
                        break;
                    }
                }
            }
            finally
            {
                suppressRunLineChecks = false;
            }

            UpdateRunLinesSummaryFromState();
        }

        private void UpdateRunLinesSummaryFromState()
        {
            var selectedLines = whatsAppLines
                .Where(line => selectedLineIdsForRun.Contains(line.Id))
                .OrderBy(line => line.Priority)
                .ThenBy(line => line.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            lblRunLinesSummary.Text = selectedLines.Count == 0
                ? "Líneas seleccionadas: ninguna"
                : $"Líneas seleccionadas: {string.Join(", ", selectedLines.Select(line => line.DisplayName))}";
            lblRunLinesSummary.ForeColor = selectedLines.Count == 0 && whatsAppLines.Count > 0
                ? Color.FromArgb(185, 28, 28)
                : Color.FromArgb(75, 85, 99);
        }

        private WhatsAppLine? SelectedWhatsAppLine => cmbWhatsAppLine.SelectedItem as WhatsAppLine;

        private void UpdateLinePreparationStatus()
        {
            var selectedLine = SelectedWhatsAppLine;
            if (selectedLine == null)
            {
                lblLinePreparationStatus.Text = "Estado línea: sin línea";
                lblLinePreparationStatus.ForeColor = Color.FromArgb(75, 85, 99);
                return;
            }

            lblLinePreparationStatus.Text = $"Estado línea: {selectedLine.OperationalStateText}";
            lblLinePreparationStatus.ForeColor = selectedLine.OperationalState switch
            {
                WhatsAppLineOperationalState.Ready => Color.FromArgb(21, 128, 61),
                WhatsAppLineOperationalState.RequiresManualAuth => Color.FromArgb(180, 83, 9),
                WhatsAppLineOperationalState.NotAvailable => Color.FromArgb(185, 28, 28),
                _ => Color.FromArgb(75, 85, 99)
            };
        }

        private void UpdateActionButtons()
        {
            btnPrepareLine.Enabled = prepareLineEnabled && SelectedWhatsAppLine != null;
            btnPrepareAllLines.Enabled = prepareAllLinesEnabled && whatsAppLines.Count > 0;
        }

        private void cmbWhatsAppLine_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (suppressWhatsAppLineChanged)
            {
                return;
            }

            selectedWhatsAppLineId = SelectedWhatsAppLine?.Id;
            EnsureSelectedLineChecked();
            UpdateLinePreparationStatus();
            UpdateActionButtons();
        }

        private void checkedRunLines_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (suppressRunLineChecks)
            {
                return;
            }

            BeginInvoke(new Action(() =>
            {
                SyncSelectedRunLineIdsFromCheckedList();
                UpdateRunLinesSummaryFromState();
            }));
        }

        private void btnConfigureLines_Click(object? sender, EventArgs e)
        {
            SyncSelectedRunLineIdsFromCheckedList();
            var updatedLines = dialogActions.ConfigureLines(this, selectedWhatsAppLineId);
            if (updatedLines == null)
            {
                return;
            }

            SetWhatsAppLines(updatedLines, selectedWhatsAppLineId, includeSelectedLine: true);
        }

        private void btnPrepareLine_Click(object? sender, EventArgs e)
        {
            var selectedLine = SelectedWhatsAppLine;
            if (selectedLine == null)
            {
                MessageBox.Show("Selecciona una línea para preparar.", "Preparar línea", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SyncSelectedRunLineIdsFromCheckedList();
            var updatedLines = dialogActions.PrepareLine(selectedLine.Id);
            SetWhatsAppLines(updatedLines, selectedLine.Id, includeSelectedLine: false);
        }

        private void btnPrepareAllLines_Click(object? sender, EventArgs e)
        {
            SyncSelectedRunLineIdsFromCheckedList();
            var updatedLines = dialogActions.PrepareAllLines();
            SetWhatsAppLines(updatedLines, selectedWhatsAppLineId, includeSelectedLine: false);
        }

        private void btnSaveElevenLabsSettings_Click(object? sender, EventArgs e)
        {
            ApplyElevenLabsState(dialogActions.SaveElevenLabsSettings(BuildElevenLabsSettingsFromUi()));
        }

        private void btnTestElevenLabsSettings_Click(object? sender, EventArgs e)
        {
            ApplyElevenLabsState(dialogActions.TestElevenLabsSettings(BuildElevenLabsSettingsFromUi()));
        }

        private ElevenLabsSettings BuildElevenLabsSettingsFromUi()
        {
            return new ElevenLabsSettings(
                txtElevenLabsApiKey.Text,
                txtElevenLabsVoiceId.Text,
                txtElevenLabsModelId.Text,
                txtElevenLabsOutputFormat.Text,
                (double)nudElevenLabsStability.Value,
                (double)nudElevenLabsSimilarityBoost.Value);
        }

        private void ApplyElevenLabsState(ElevenLabsDialogState state)
        {
            txtElevenLabsApiKey.Text = state.Settings.ApiKey;
            txtElevenLabsVoiceId.Text = state.Settings.VoiceId;
            txtElevenLabsModelId.Text = state.Settings.ModelId;
            txtElevenLabsOutputFormat.Text = state.Settings.OutputFormat;
            nudElevenLabsStability.Value = ToNumericRatio(state.Settings.Stability, ElevenLabsSettings.DefaultStability);
            nudElevenLabsSimilarityBoost.Value = ToNumericRatio(state.Settings.SimilarityBoost, ElevenLabsSettings.DefaultSimilarityBoost);
            elevenLabsStatusText = state.StatusText;
            lblElevenLabsStatus.Text = state.StatusText;
            lblElevenLabsStatus.ForeColor = state.StatusColor;
            uiToolTip.SetToolTip(lblElevenLabsStatus, state.ToolTipText);
            uiToolTip.SetToolTip(txtElevenLabsApiKey, "La API key se muestra enmascarada y se guarda cifrada para el usuario actual.");
        }

        private static decimal ToNumericRatio(double value, double defaultValue)
        {
            if (double.IsNaN(value) || value < 0 || value > 1)
            {
                value = defaultValue;
            }

            return Math.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
        }

        private void SetElevenLabsControlsEnabled(bool enabled)
        {
            txtElevenLabsApiKey.Enabled = enabled;
            txtElevenLabsVoiceId.Enabled = enabled;
            txtElevenLabsModelId.Enabled = enabled;
            txtElevenLabsOutputFormat.Enabled = enabled;
            nudElevenLabsStability.Enabled = enabled;
            nudElevenLabsSimilarityBoost.Enabled = enabled;
            btnSaveElevenLabsSettings.Enabled = enabled;
            btnTestElevenLabsSettings.Enabled = enabled;
        }

        private void saveButton_Click(object? sender, EventArgs e)
        {
            SyncSelectedRunLineIdsFromCheckedList();
            if (whatsAppLines.Count > 0 && selectedLineIdsForRun.Count == 0)
            {
                tabControl.SelectedTab = whatsAppTab;
                MessageBox.Show(
                    "Selecciona al menos una línea para esta corrida.",
                    "Líneas de corrida",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            selectedWhatsAppLineId = SelectedWhatsAppLine?.Id;
            Settings = Settings with
            {
                DelayBetweenMessagesMinutes = (int)nudDelayBetweenMessages.Value
            };
            Settings = Settings.Normalize();

            ConfigurationState = new AppConfigurationState(
                selectedWhatsAppLineId,
                chkAutoFallback.Checked,
                selectedLineIdsForRun.ToList(),
                Settings.DelayBetweenMessagesMinutes,
                elevenLabsStatusText);

            AutoWhatsAppSettingsStore.Save(Settings);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

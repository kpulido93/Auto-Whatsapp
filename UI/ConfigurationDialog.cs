using Automate_Whatsapp.Logic;

namespace Automate_Whatsapp
{
    internal sealed class ConfigurationDialog : Form
    {
        private const int TemplateNumberMaximum = 9999;
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
        private ListBox lstMessageTemplates = null!;
        private NumericUpDown nudTemplateNumber = null!;
        private TextBox txtTemplateName = null!;
        private CheckBox chkTemplateEnabled = null!;
        private TextBox txtTemplateBody = null!;
        private Button btnAddTemplate = null!;
        private Button btnPrepareLine = null!;
        private Button btnPrepareAllLines = null!;
        private Button btnSaveElevenLabsSettings = null!;
        private Button btnTestElevenLabsSettings = null!;
        private readonly TabControl tabControl;
        private readonly TabPage whatsAppTab;
        private readonly TabPage templatesTab;
        private readonly ToolTip uiToolTip = new();
        private readonly ConfigurationDialogActions dialogActions;
        private readonly bool lineControlsEnabled;
        private readonly bool runLineSelectionEnabled;
        private readonly bool configureLinesEnabled;
        private readonly bool prepareLineEnabled;
        private readonly bool prepareAllLinesEnabled;
        private readonly bool elevenLabsControlsEnabled;

        private List<WhatsAppLine> whatsAppLines = new();
        private readonly List<EditableMessageTemplate> editableMessageTemplates = new();
        private readonly HashSet<string> selectedLineIdsForRun = new(StringComparer.OrdinalIgnoreCase);
        private string? selectedWhatsAppLineId;
        private string elevenLabsStatusText;
        private bool suppressWhatsAppLineChanged;
        private bool suppressRunLineChecks;
        private bool suppressTemplateEditorEvents;

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
            ClientSize = new Size(780, 560);

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
            templatesTab = CreateTemplatesTab();
            tabControl.TabPages.Add(templatesTab);
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
            LoadMessageTemplates();
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
                Text = "La espera se aplica entre mensajes.",
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

        private TabPage CreateTemplatesTab()
        {
            var tab = new TabPage("Plantillas");
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            layout.Controls.Add(CreateTemplatesListSection(), 0, 0);
            layout.Controls.Add(CreateTemplateEditorSection(), 1, 0);

            var footerLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(75, 85, 99),
                Text = "Placeholders disponibles: {Banco}, {NombreDeudor}. Las plantillas se guardan localmente al pulsar Guardar.",
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(footerLabel, 0, 1);
            layout.SetColumnSpan(footerLabel, 2);

            tab.Controls.Add(layout);
            return tab;
        }

        private Control CreateTemplatesListSection()
        {
            var group = new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = "Plantillas",
                Padding = new Padding(10, 8, 10, 10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            lstMessageTemplates = new ListBox
            {
                Dock = DockStyle.Fill,
                IntegralHeight = false
            };
            lstMessageTemplates.SelectedIndexChanged += lstMessageTemplates_SelectedIndexChanged;

            btnAddTemplate = new Button
            {
                Anchor = AnchorStyles.Left,
                AutoSize = true,
                Text = "Nueva plantilla"
            };
            btnAddTemplate.Click += btnAddTemplate_Click;

            layout.Controls.Add(lstMessageTemplates, 0, 0);
            layout.Controls.Add(btnAddTemplate, 0, 1);
            group.Controls.Add(layout);
            return group;
        }

        private Control CreateTemplateEditorSection()
        {
            var group = new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = "Edición",
                Padding = new Padding(10, 8, 10, 10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            nudTemplateNumber = new NumericUpDown
            {
                Dock = DockStyle.Left,
                Minimum = 0,
                Maximum = TemplateNumberMaximum,
                Size = new Size(90, 23)
            };
            nudTemplateNumber.ValueChanged += nudTemplateNumber_ValueChanged;

            txtTemplateName = CreateTextBox();
            txtTemplateName.TextChanged += txtTemplateName_TextChanged;
            txtTemplateName.Leave += txtTemplateName_Leave;

            chkTemplateEnabled = new CheckBox
            {
                Anchor = AnchorStyles.Left,
                AutoSize = true,
                Text = "Habilitada"
            };
            chkTemplateEnabled.CheckedChanged += chkTemplateEnabled_CheckedChanged;

            txtTemplateBody = new TextBox
            {
                AcceptsReturn = true,
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            txtTemplateBody.TextChanged += txtTemplateBody_TextChanged;

            var placeholdersLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(75, 85, 99),
                Text = "Puedes usar {Banco} y {NombreDeudor} dentro del cuerpo.",
                TextAlign = ContentAlignment.MiddleLeft
            };

            AddLabel(layout, "Número:", 0, 0);
            layout.Controls.Add(nudTemplateNumber, 1, 0);
            AddLabel(layout, "Nombre:", 0, 1);
            layout.Controls.Add(txtTemplateName, 1, 1);
            AddLabel(layout, "Estado:", 0, 2);
            layout.Controls.Add(chkTemplateEnabled, 1, 2);
            AddLabel(layout, "Cuerpo:", 0, 3);
            layout.Controls.Add(txtTemplateBody, 1, 3);
            layout.Controls.Add(placeholdersLabel, 1, 4);

            group.Controls.Add(layout);
            return group;
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

        private void LoadMessageTemplates()
        {
            editableMessageTemplates.Clear();

            foreach (MessageTemplate template in MessageTemplateStore.LoadOrDefault().OrderBy(template => template.Number))
            {
                editableMessageTemplates.Add(new EditableMessageTemplate(template));
            }

            EnsureMinimumTemplateCount();
            RebuildMessageTemplateList(editableMessageTemplates.FirstOrDefault());
        }

        private void EnsureMinimumTemplateCount()
        {
            while (editableMessageTemplates.Count < MessageTemplateValidator.MinimumTemplateCount)
            {
                int nextNumber = GetNextAvailableTemplateNumber();
                editableMessageTemplates.Add(new EditableMessageTemplate(
                    new MessageTemplate(nextNumber, $"Plantilla {nextNumber}", "", false)));
            }
        }

        private int GetNextAvailableTemplateNumber()
        {
            HashSet<int> usedNumbers = editableMessageTemplates
                .Select(template => template.Number)
                .Where(number => number > 0)
                .ToHashSet();

            int nextNumber = 1;
            while (usedNumbers.Contains(nextNumber))
            {
                nextNumber++;
            }

            return nextNumber;
        }

        private EditableMessageTemplate? SelectedEditableMessageTemplate
            => lstMessageTemplates.SelectedItem as EditableMessageTemplate;

        private void RebuildMessageTemplateList(EditableMessageTemplate? templateToSelect = null)
        {
            templateToSelect ??= SelectedEditableMessageTemplate ?? editableMessageTemplates.FirstOrDefault();

            editableMessageTemplates.Sort(static (left, right) =>
            {
                int numberComparison = left.Number.CompareTo(right.Number);
                return numberComparison != 0
                    ? numberComparison
                    : StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name);
            });

            lstMessageTemplates.BeginUpdate();
            try
            {
                lstMessageTemplates.Items.Clear();
                foreach (EditableMessageTemplate template in editableMessageTemplates)
                {
                    lstMessageTemplates.Items.Add(template);
                }
            }
            finally
            {
                lstMessageTemplates.EndUpdate();
            }

            if (templateToSelect != null)
            {
                int selectedIndex = lstMessageTemplates.Items.IndexOf(templateToSelect);
                lstMessageTemplates.SelectedIndex = selectedIndex >= 0
                    ? selectedIndex
                    : (lstMessageTemplates.Items.Count > 0 ? 0 : -1);
            }
            else if (lstMessageTemplates.Items.Count > 0)
            {
                lstMessageTemplates.SelectedIndex = 0;
            }
            else
            {
                BindSelectedTemplateEditor();
            }
        }

        private void BindSelectedTemplateEditor()
        {
            EditableMessageTemplate? selectedTemplate = SelectedEditableMessageTemplate;

            suppressTemplateEditorEvents = true;
            try
            {
                if (selectedTemplate == null)
                {
                    nudTemplateNumber.Value = 0;
                    txtTemplateName.Text = string.Empty;
                    chkTemplateEnabled.Checked = false;
                    txtTemplateBody.Text = string.Empty;
                    SetTemplateEditorEnabled(false);
                    return;
                }

                SetTemplateEditorEnabled(true);
                nudTemplateNumber.Value = Math.Clamp(selectedTemplate.Number, 0, TemplateNumberMaximum);
                txtTemplateName.Text = selectedTemplate.Name;
                chkTemplateEnabled.Checked = selectedTemplate.Enabled;
                txtTemplateBody.Text = selectedTemplate.Body;
            }
            finally
            {
                suppressTemplateEditorEvents = false;
            }
        }

        private void SetTemplateEditorEnabled(bool enabled)
        {
            nudTemplateNumber.Enabled = enabled;
            txtTemplateName.Enabled = enabled;
            chkTemplateEnabled.Enabled = enabled;
            txtTemplateBody.Enabled = enabled;
        }

        private void lstMessageTemplates_SelectedIndexChanged(object? sender, EventArgs e)
        {
            BindSelectedTemplateEditor();
        }

        private void btnAddTemplate_Click(object? sender, EventArgs e)
        {
            int nextNumber = GetNextAvailableTemplateNumber();
            var newTemplate = new EditableMessageTemplate(
                new MessageTemplate(nextNumber, $"Plantilla {nextNumber}", "", false));

            editableMessageTemplates.Add(newTemplate);
            RebuildMessageTemplateList(newTemplate);
            txtTemplateBody.Focus();
        }

        private void nudTemplateNumber_ValueChanged(object? sender, EventArgs e)
        {
            if (suppressTemplateEditorEvents || SelectedEditableMessageTemplate == null)
            {
                return;
            }

            SelectedEditableMessageTemplate.Number = (int)nudTemplateNumber.Value;
            RebuildMessageTemplateList(SelectedEditableMessageTemplate);
        }

        private void txtTemplateName_TextChanged(object? sender, EventArgs e)
        {
            if (suppressTemplateEditorEvents || SelectedEditableMessageTemplate == null)
            {
                return;
            }

            SelectedEditableMessageTemplate.Name = txtTemplateName.Text;
        }

        private void txtTemplateName_Leave(object? sender, EventArgs e)
        {
            if (SelectedEditableMessageTemplate == null)
            {
                return;
            }

            RebuildMessageTemplateList(SelectedEditableMessageTemplate);
        }

        private void chkTemplateEnabled_CheckedChanged(object? sender, EventArgs e)
        {
            if (suppressTemplateEditorEvents || SelectedEditableMessageTemplate == null)
            {
                return;
            }

            SelectedEditableMessageTemplate.Enabled = chkTemplateEnabled.Checked;
            RebuildMessageTemplateList(SelectedEditableMessageTemplate);
        }

        private void txtTemplateBody_TextChanged(object? sender, EventArgs e)
        {
            if (suppressTemplateEditorEvents || SelectedEditableMessageTemplate == null)
            {
                return;
            }

            SelectedEditableMessageTemplate.Body = txtTemplateBody.Text;
        }

        private bool TrySaveMessageTemplates()
        {
            RebuildMessageTemplateList(SelectedEditableMessageTemplate);

            MessageTemplate[] templatesToSave = editableMessageTemplates
                .Select(template => template.ToMessageTemplate())
                .OrderBy(template => template.Number)
                .ToArray();

            MessageTemplateValidationResult validationResult = MessageTemplateValidator.Validate(templatesToSave);
            if (!validationResult.IsValid)
            {
                tabControl.SelectedTab = templatesTab;
                MessageBox.Show(
                    string.Join(Environment.NewLine, validationResult.Errors),
                    "Plantillas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            MessageTemplateStore.Save(templatesToSave);
            if (!MessageTemplateStore.TryLoad(out var persistedTemplates)
                || !persistedTemplates.SequenceEqual(templatesToSave))
            {
                tabControl.SelectedTab = templatesTab;
                MessageBox.Show(
                    "No se pudo guardar la configuración local de plantillas.",
                    "Plantillas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            return true;
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

            if (!TrySaveMessageTemplates())
            {
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

        private sealed class EditableMessageTemplate
        {
            public EditableMessageTemplate(MessageTemplate template)
            {
                Number = template.Number;
                Name = template.Name;
                Enabled = template.Enabled;
                Body = template.Body;
            }

            public int Number { get; set; }

            public string Name { get; set; }

            public bool Enabled { get; set; }

            public string Body { get; set; }

            public MessageTemplate ToMessageTemplate()
            {
                return new MessageTemplate(Number, Name, Body, Enabled);
            }

            public override string ToString()
            {
                string resolvedName = string.IsNullOrWhiteSpace(Name)
                    ? "Sin nombre"
                    : Name.Trim();

                return Enabled
                    ? $"{Number}. {resolvedName}"
                    : $"{Number}. {resolvedName} (deshabilitada)";
            }
        }
    }
}

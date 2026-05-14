using Automate_Whatsapp.Logic;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Automate_Whatsapp
{
    public partial class Form1 : Form
    {
        private List<WhatsAppLine> whatsAppLines = new();
        private HashSet<string> selectedLineIdsForRun = new(StringComparer.OrdinalIgnoreCase);
        private WhatsAppSendOrchestrator sendOrchestrator = null!;
        private AutoWhatsAppSettings appSettings = AutoWhatsAppSettings.Default;
        private bool suppressLineSelectionChanged = false;

        private string excelPath = "";
        private List<ExcelMessagePreviewRow> excelPreviewRows = new();

        private System.Windows.Forms.Timer schedulerTimer = null!;
        private DateTime scheduledTime;
        private bool isPaused = false;
        private bool isScheduled = false;
        private bool isSending = false;
        private bool cancellationRequested = false;
        private bool isPreparingLines = false;
        private bool isLoadingExcelPreview = false;
        private bool isConfigurationExpanded = false;
        private int excelPreviewLoadVersion = 0;

        private const string StatusNoFile = "Sin archivo";
        private const string StatusReady = "Listo";
        private const string StatusScheduled = "Programado";
        private const string StatusSending = "Enviando";
        private const string StatusPaused = "Pausado";
        private const string StatusCancelled = "Cancelado";
        private const string StatusFinished = "Finalizado";
        private const int ConfigurationRowIndex = 0;
        private const int ExcelPreviewRowIndex = 2;
        private const int ResponsiveLayoutPadding = 8;
        private const int ResponsiveLayoutMinimumWidth = 700;
        private const int ResponsiveLayoutMinimumHeightWithoutPreview = 560;
        private const int ResponsiveLayoutMinimumHeightWithPreview = 740;
        private const float ConfigurationCollapsedHeight = 0F;
        private const float ConfigurationExpandedHeight = 420F;
        private const float ExcelPreviewCollapsedHeight = 0F;
        private const float ExcelPreviewExpandedHeight = 184F;
        private const string ApplicationIconResourceName = "Resources.icon.ico";
        private const string ElevenLabsSavedSettingsTooltip = "La configuración usada en envíos es la guardada localmente.";

        private enum SendStartTrigger
        {
            ImmediateClick,
            ScheduledTimer
        }

        public Form1()
        {
            InitializeComponent();
            ApplyApplicationIcon();
            LoadAppSettings();
            LoadElevenLabsSettingsIntoUi();
            ApplyConfigurationVisibility();
            ApplyExcelPreviewVisibility();
            AdjustResponsiveLayout();
            InitializeScheduleControls();
            SetSendFeedback(StatusNoFile, 0, 0, 0, 0, 0);
            UpdateActionButtons();
            LoadWhatsAppLines();
            InitializeSendOrchestrator();
            UpdateMenuState();
        }

        private void ApplyApplicationIcon()
        {
            using Stream? iconStream = typeof(Form1).Assembly.GetManifestResourceStream(ApplicationIconResourceName);

            if (iconStream != null)
            {
                using var embeddedIcon = new Icon(iconStream);
                Icon = (Icon)embeddedIcon.Clone();
                return;
            }

            string iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "icon.ico");
            if (File.Exists(iconPath))
            {
                Icon = new Icon(iconPath);
            }
        }

        private void LoadAppSettings()
        {
            appSettings = AutoWhatsAppSettingsStore.LoadOrDefault();
        }

        private void chkShowExcelPreview_CheckedChanged(object sender, EventArgs e)
        {
            mostrarVistaPreviaExcelToolStripMenuItem.Checked = chkShowExcelPreview.Checked;
            ApplyExcelPreviewVisibility();
        }

        private void btnToggleConfiguration_Click(object sender, EventArgs e)
        {
            isConfigurationExpanded = !isConfigurationExpanded;
            ApplyConfigurationVisibility();
        }

        private void chkAutoLineFallback_CheckedChanged(object sender, EventArgs e)
        {
            cambiarAutomaticamenteSiFallaToolStripMenuItem.Checked = chkAutoLineFallback.Checked;
            UpdateConfigurationSummary();
            UpdateMenuState();
        }

        private void ApplyConfigurationVisibility()
        {
            mainLayout.SuspendLayout();
            try
            {
                configurationContentLayout.Visible = isConfigurationExpanded;
                grpConfiguration.Visible = isConfigurationExpanded;
                mainLayout.RowStyles[ConfigurationRowIndex].Height = isConfigurationExpanded
                    ? ConfigurationExpandedHeight
                    : ConfigurationCollapsedHeight;
                btnToggleConfiguration.Text = isConfigurationExpanded ? "Ocultar" : "Mostrar";
            }
            finally
            {
                mainLayout.ResumeLayout(true);
            }

            UpdateConfigurationSummary();
            AdjustResponsiveLayout();
        }

        private void ApplyExcelPreviewVisibility()
        {
            bool showPreview = chkShowExcelPreview.Checked;

            mainLayout.SuspendLayout();
            try
            {
                grpExcelPreview.Visible = showPreview;
                dgvExcelPreview.Visible = showPreview;
                mainLayout.RowStyles[ExcelPreviewRowIndex].Height = showPreview
                    ? ExcelPreviewExpandedHeight
                    : ExcelPreviewCollapsedHeight;

                if (showPreview)
                {
                    BindExcelPreviewGrid();
                }
                else
                {
                    ClearExcelPreviewGrid();
                }
            }
            finally
            {
                mainLayout.ResumeLayout(true);
            }

            AdjustResponsiveLayout();
        }

        private async void seleccionarExcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await SelectExcelFileAsync();
        }

        private void descargarPlantillaExcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DownloadExcelTemplate();
        }

        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void configuracionToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            UpdateMenuState();
            RefreshWhatsAppLineMenu();
        }

        private void lineaWhatsAppToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            RefreshWhatsAppLineMenu();
        }

        private void configuracionGeneralToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var configurationDialog = new ConfigurationDialog(appSettings, Icon);
            if (configurationDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            appSettings = configurationDialog.Settings.Normalize();
            UpdateConfigurationSummary();
            Log($"Configuración general guardada. Espera entre mensajes: {appSettings.DelayBetweenMessagesMinutes} min.");
        }

        private void cambiarAutomaticamenteSiFallaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (chkAutoLineFallback.Checked != cambiarAutomaticamenteSiFallaToolStripMenuItem.Checked)
            {
                chkAutoLineFallback.Checked = cambiarAutomaticamenteSiFallaToolStripMenuItem.Checked;
            }
        }

        private void seleccionarLineasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnSelectRunLines_Click(sender, e);
        }

        private void elevenLabsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowElevenLabsConfiguration();
        }

        private void mostrarVistaPreviaExcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (chkShowExcelPreview.Checked != mostrarVistaPreviaExcelToolStripMenuItem.Checked)
            {
                chkShowExcelPreview.Checked = mostrarVistaPreviaExcelToolStripMenuItem.Checked;
            }
        }

        private void acercaDeAutoWhatsAppToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var aboutDialog = new AboutDialog(Icon);
            aboutDialog.ShowDialog(this);
        }

        private void UpdateMenuState()
        {
            seleccionarExcelToolStripMenuItem.Enabled = btnSelectFile.Enabled;
            descargarPlantillaExcelToolStripMenuItem.Enabled = btnDownloadTemplate.Enabled;
            configuracionGeneralToolStripMenuItem.Enabled = true;
            lineaWhatsAppToolStripMenuItem.Enabled = true;
            cambiarAutomaticamenteSiFallaToolStripMenuItem.Enabled = chkAutoLineFallback.Enabled;
            cambiarAutomaticamenteSiFallaToolStripMenuItem.Checked = chkAutoLineFallback.Checked;
            seleccionarLineasToolStripMenuItem.Enabled = btnSelectRunLines.Enabled;
            elevenLabsToolStripMenuItem.Enabled = btnTestElevenLabsSettings.Enabled;
            mostrarVistaPreviaExcelToolStripMenuItem.Enabled = chkShowExcelPreview.Enabled;
            mostrarVistaPreviaExcelToolStripMenuItem.Checked = chkShowExcelPreview.Checked;
        }

        private void RefreshWhatsAppLineMenu()
        {
            while (lineaWhatsAppToolStripMenuItem.DropDownItems.Count > 0)
            {
                var item = lineaWhatsAppToolStripMenuItem.DropDownItems[0];
                lineaWhatsAppToolStripMenuItem.DropDownItems.RemoveAt(0);
                item.Dispose();
            }

            var configureLinesItem = new ToolStripMenuItem("Configurar líneas...")
            {
                Enabled = btnConfigureLines.Enabled
            };
            configureLinesItem.Click += (_, args) => btnConfigureLines_Click(configureLinesItem, args);
            lineaWhatsAppToolStripMenuItem.DropDownItems.Add(configureLinesItem);
            lineaWhatsAppToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());

            if (whatsAppLines.Count == 0)
            {
                lineaWhatsAppToolStripMenuItem.DropDownItems.Add(new ToolStripMenuItem("Sin verificar")
                {
                    Enabled = false
                });
                return;
            }

            string? selectedLineId = SelectedWhatsAppLine?.Id;
            foreach (var line in whatsAppLines)
            {
                var lineMenuItem = new ToolStripMenuItem(line.DisplayNameWithState)
                {
                    Checked = string.Equals(line.Id, selectedLineId, StringComparison.OrdinalIgnoreCase),
                    Enabled = cmbWhatsAppLine.Enabled,
                    Tag = line
                };

                lineMenuItem.Click += whatsappLineToolStripMenuItem_Click;
                lineaWhatsAppToolStripMenuItem.DropDownItems.Add(lineMenuItem);
            }

            lineaWhatsAppToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            var prepareLineItem = new ToolStripMenuItem("Preparar línea seleccionada")
            {
                Enabled = btnPrepareLine.Enabled
            };
            prepareLineItem.Click += (_, args) => btnPrepareLine_Click(prepareLineItem, args);
            lineaWhatsAppToolStripMenuItem.DropDownItems.Add(prepareLineItem);

            var prepareAllLinesItem = new ToolStripMenuItem("Preparar todas las líneas")
            {
                Enabled = btnPrepareAllLines.Enabled
            };
            prepareAllLinesItem.Click += (_, args) => btnPrepareAllLines_Click(prepareAllLinesItem, args);
            lineaWhatsAppToolStripMenuItem.DropDownItems.Add(prepareAllLinesItem);
        }

        private void whatsappLineToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem { Tag: WhatsAppLine line } || !cmbWhatsAppLine.Enabled)
            {
                return;
            }

            var matchingLine = whatsAppLines.FirstOrDefault(item =>
                string.Equals(item.Id, line.Id, StringComparison.OrdinalIgnoreCase));

            if (matchingLine == null)
            {
                return;
            }

            cmbWhatsAppLine.SelectedItem = matchingLine;
            RefreshWhatsAppLineMenu();
            UpdateMenuState();
        }

        private void LoadWhatsAppLines(string? selectedLineId = null)
        {
            string? selectionToRestore = selectedLineId ?? SelectedWhatsAppLine?.Id;

            try
            {
                whatsAppLines = WhatsAppLineManager.GetEnabledLines().ToList();
            }
            catch (Exception ex)
            {
                whatsAppLines = new List<WhatsAppLine> { WhatsAppLineManager.CreateDefaultLine() };
                Log($"❌ No se pudo cargar la configuración local de líneas. Se usará la línea Principal. Detalle: {ex.Message}");
                MessageBox.Show(
                    "No se pudo cargar la configuración local de líneas. Se usará la línea Principal.",
                    "Configuración de líneas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            suppressLineSelectionChanged = true;
            try
            {
                cmbWhatsAppLine.DisplayMember = nameof(WhatsAppLine.DisplayNameWithState);
                cmbWhatsAppLine.ValueMember = nameof(WhatsAppLine.Id);
                cmbWhatsAppLine.DataSource = whatsAppLines;

                var matchingLine = !string.IsNullOrWhiteSpace(selectionToRestore)
                    ? whatsAppLines.FirstOrDefault(line =>
                        string.Equals(line.Id, selectionToRestore, StringComparison.OrdinalIgnoreCase))
                    : null;

                if (matchingLine != null)
                {
                    cmbWhatsAppLine.SelectedItem = matchingLine;
                }
                else if (whatsAppLines.Count > 0)
                {
                    cmbWhatsAppLine.SelectedIndex = 0;
                }
            }
            finally
            {
                suppressLineSelectionChanged = false;
            }

            if (sendOrchestrator != null)
            {
                sendOrchestrator.UpdateLines(whatsAppLines);
                sendOrchestrator.SetSelectedLine(SelectedWhatsAppLine);
            }

            ReconcileSelectedLinesForRun(includePrimary: true);
            UpdateSelectedLineStatus();
            RefreshWhatsAppLineMenu();
            UpdateActionButtons();
            Log($"Líneas habilitadas cargadas: {whatsAppLines.Count}");
        }

        private WhatsAppLine? SelectedWhatsAppLine => cmbWhatsAppLine.SelectedItem as WhatsAppLine;

        private void RefreshWhatsAppLineSelector(string? selectedLineId = null)
        {
            string? selectedId = selectedLineId ?? SelectedWhatsAppLine?.Id;

            suppressLineSelectionChanged = true;
            try
            {
                cmbWhatsAppLine.DataSource = null;
                cmbWhatsAppLine.DisplayMember = nameof(WhatsAppLine.DisplayNameWithState);
                cmbWhatsAppLine.ValueMember = nameof(WhatsAppLine.Id);
                cmbWhatsAppLine.DataSource = whatsAppLines;

                if (!string.IsNullOrWhiteSpace(selectedId))
                {
                    var matchingLine = whatsAppLines.FirstOrDefault(line =>
                        string.Equals(line.Id, selectedId, StringComparison.OrdinalIgnoreCase));

                    if (matchingLine != null)
                    {
                        cmbWhatsAppLine.SelectedItem = matchingLine;
                    }
                }
                else if (whatsAppLines.Count > 0)
                {
                    cmbWhatsAppLine.SelectedIndex = 0;
                }
            }
            finally
            {
                suppressLineSelectionChanged = false;
            }

            UpdateSelectedLineStatus();
            RefreshWhatsAppLineMenu();
            UpdateMenuState();
        }

        private WhatsAppLine UpdateWhatsAppLineState(WhatsAppLine line, WhatsAppLineOperationalState state)
        {
            int lineIndex = whatsAppLines.FindIndex(item =>
                string.Equals(item.Id, line.Id, StringComparison.OrdinalIgnoreCase));

            if (lineIndex < 0)
            {
                return line;
            }

            var updatedLine = whatsAppLines[lineIndex] with { OperationalState = state };
            whatsAppLines[lineIndex] = updatedLine;
            sendOrchestrator.UpdateLines(whatsAppLines);
            RefreshWhatsAppLineSelector(updatedLine.Id);

            return updatedLine;
        }

        private void UpdateSelectedLineStatus()
        {
            var selectedLine = SelectedWhatsAppLine;
            if (selectedLine == null)
            {
                lblLinePreparationStatus.Text = "Estado línea: sin línea";
                lblLinePreparationStatus.ForeColor = Color.FromArgb(75, 85, 99);
                UpdateConfigurationSummary();
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
            UpdateConfigurationSummary();
        }

        private void ReconcileSelectedLinesForRun(bool includePrimary)
        {
            var enabledLineIds = whatsAppLines
                .Where(line => line.Enabled)
                .Select(line => line.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            selectedLineIdsForRun.RemoveWhere(lineId => !enabledLineIds.Contains(lineId));

            if (includePrimary && SelectedWhatsAppLine != null)
            {
                selectedLineIdsForRun.Add(SelectedWhatsAppLine.Id);
            }

            UpdateRunLinesSummary();
        }

        private void UpdateRunLinesSummary()
        {
            var selectedLines = GetSelectedLinesForRun();

            if (selectedLines.Count == 0)
            {
                lblRunLinesSummary.Text = "Líneas seleccionadas: ninguna";
                lblRunLinesSummary.ForeColor = Color.FromArgb(185, 28, 28);
                UpdateConfigurationSummary();
                return;
            }

            lblRunLinesSummary.Text = $"Líneas seleccionadas: {string.Join(", ", selectedLines.Select(line => line.DisplayName))}";
            lblRunLinesSummary.ForeColor = Color.FromArgb(75, 85, 99);
            UpdateConfigurationSummary();
        }

        private void LoadElevenLabsSettingsIntoUi()
        {
            try
            {
                bool loadedLocalSettings = ElevenLabsSettingsStore.TryLoad(out var settings);
                ApplyElevenLabsSettingsToUi(settings);
                uiToolTip.SetToolTip(txtElevenLabsApiKey, "La API key se muestra enmascarada y se guarda cifrada para el usuario actual.");

                if (settings.Validate().Count == 0)
                {
                    if (loadedLocalSettings)
                    {
                        MarkElevenLabsSavedLocally();
                    }
                    else
                    {
                        lblElevenLabsStatus.Text = "Configurado por entorno";
                        uiToolTip.SetToolTip(
                            lblElevenLabsStatus,
                            "Los envíos de audio usan variables de entorno porque no hay configuración local guardada."
                            + Environment.NewLine
                            + "Pulsa Guardar configuración o Probar configuración para guardar estos valores localmente."
                            + Environment.NewLine
                            + $"Ruta local: {ElevenLabsSettingsStore.GetConfigPath()}");
                        UpdateConfigurationSummary();
                    }
                }
            }
            catch (Exception ex)
            {
                ApplyElevenLabsSettingsToUi(ElevenLabsSettings.FromEnvironment());
                lblElevenLabsStatus.Text = "Error de validación";
                lblElevenLabsStatus.ForeColor = Color.FromArgb(185, 28, 28);
                uiToolTip.SetToolTip(lblElevenLabsStatus, ex.Message);
                Log($"No se pudo cargar la configuración local de ElevenLabs. Se usarán variables de entorno. Detalle: {ex.Message}");
            }
        }

        private void ShowElevenLabsConfiguration()
        {
            if (!isConfigurationExpanded)
            {
                isConfigurationExpanded = true;
                ApplyConfigurationVisibility();
            }

            BeginInvoke(new Action(() =>
            {
                mainScrollPanel.ScrollControlIntoView(grpElevenLabs);
                txtElevenLabsApiKey.Focus();
            }));
        }

        private void ApplyElevenLabsSettingsToUi(ElevenLabsSettings settings)
        {
            txtElevenLabsApiKey.Text = settings.ApiKey;
            txtElevenLabsVoiceId.Text = settings.VoiceId;
            txtElevenLabsModelId.Text = settings.ModelId;
            txtElevenLabsOutputFormat.Text = settings.OutputFormat;
            nudElevenLabsStability.Value = ToNumericRatio(settings.Stability, ElevenLabsSettings.DefaultStability);
            nudElevenLabsSimilarityBoost.Value = ToNumericRatio(settings.SimilarityBoost, ElevenLabsSettings.DefaultSimilarityBoost);
            UpdateElevenLabsStatus(settings);
        }

        private void btnSaveElevenLabsSettings_Click(object sender, EventArgs e)
        {
            if (!TryBuildElevenLabsSettingsFromUi(out var settings, out var validationErrors))
            {
                UpdateElevenLabsStatus(settings, validationErrors);
                Log("Configuración ElevenLabs no guardada: " + string.Join(" ", validationErrors));
                MessageBox.Show(
                    "La configuración de ElevenLabs está incompleta o no es válida. Revisa los campos obligatorios.",
                    "ElevenLabs",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                SaveElevenLabsSettings(settings, "Configuración ElevenLabs guardada localmente. Los envíos de audio usarán estos valores.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException)
            {
                ReportElevenLabsSettingsSaveError(ex);
            }
        }

        private void btnTestElevenLabsSettings_Click(object sender, EventArgs e)
        {
            if (!TryBuildElevenLabsSettingsFromUi(out var settings, out var validationErrors))
            {
                UpdateElevenLabsStatus(settings, validationErrors);
                Log("Prueba ElevenLabs fallida: " + string.Join(" ", validationErrors));
                return;
            }

            try
            {
                SaveElevenLabsSettings(settings, "Prueba ElevenLabs correcta: configuración mínima presente y guardada localmente. No se llamó a la API.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException)
            {
                ReportElevenLabsSettingsSaveError(ex);
            }
        }

        private void SaveElevenLabsSettings(ElevenLabsSettings settings, string successLogMessage)
        {
            ElevenLabsSettingsStore.Save(settings);
            MarkElevenLabsSavedLocally();
            Log(successLogMessage);
        }

        private void MarkElevenLabsSavedLocally()
        {
            lblElevenLabsStatus.Text = "Configurado y guardado";
            lblElevenLabsStatus.ForeColor = Color.FromArgb(21, 128, 61);
            uiToolTip.SetToolTip(
                lblElevenLabsStatus,
                ElevenLabsSavedSettingsTooltip
                + Environment.NewLine
                + $"Ruta local: {ElevenLabsSettingsStore.GetConfigPath()}");
            UpdateConfigurationSummary();
        }

        private void ReportElevenLabsSettingsSaveError(Exception ex)
        {
            lblElevenLabsStatus.Text = "Error al guardar";
            lblElevenLabsStatus.ForeColor = Color.FromArgb(185, 28, 28);
            uiToolTip.SetToolTip(lblElevenLabsStatus, ex.Message);
            Log($"No se pudo guardar la configuración ElevenLabs: {ex.Message}");
            MessageBox.Show(
                "No se pudo guardar la configuración de ElevenLabs.",
                "ElevenLabs",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private bool TryBuildElevenLabsSettingsFromUi(
            out ElevenLabsSettings settings,
            out IReadOnlyList<string> validationErrors)
        {
            settings = new ElevenLabsSettings(
                txtElevenLabsApiKey.Text,
                txtElevenLabsVoiceId.Text,
                txtElevenLabsModelId.Text,
                txtElevenLabsOutputFormat.Text,
                (double)nudElevenLabsStability.Value,
                (double)nudElevenLabsSimilarityBoost.Value);

            validationErrors = settings.Validate();
            return validationErrors.Count == 0;
        }

        private void UpdateElevenLabsStatus(
            ElevenLabsSettings settings,
            IReadOnlyList<string>? validationErrors = null)
        {
            validationErrors ??= settings.Validate();

            if (validationErrors.Count == 0)
            {
                lblElevenLabsStatus.Text = "Configurado";
                lblElevenLabsStatus.ForeColor = Color.FromArgb(21, 128, 61);
                uiToolTip.SetToolTip(
                    lblElevenLabsStatus,
                    "Configuración ElevenLabs válida. Pulsa Guardar configuración o Probar configuración para guardarla localmente antes de enviar audios."
                    + Environment.NewLine
                    + $"Ruta local: {ElevenLabsSettingsStore.GetConfigPath()}");
            }
            else if (string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.VoiceId))
            {
                lblElevenLabsStatus.Text = "No configurado";
                lblElevenLabsStatus.ForeColor = Color.FromArgb(180, 83, 9);
                uiToolTip.SetToolTip(lblElevenLabsStatus, string.Join(Environment.NewLine, validationErrors));
            }
            else
            {
                lblElevenLabsStatus.Text = "Error de validación";
                lblElevenLabsStatus.ForeColor = Color.FromArgb(185, 28, 28);
                uiToolTip.SetToolTip(lblElevenLabsStatus, string.Join(Environment.NewLine, validationErrors));
            }

            UpdateConfigurationSummary();
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

        private static decimal ToNumericRatio(double value, double defaultValue)
        {
            if (double.IsNaN(value) || value < 0 || value > 1)
            {
                value = defaultValue;
            }

            return Math.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
        }

        private void UpdateConfigurationSummary()
        {
            if (lblConfigurationSummary == null || chkAutoLineFallback == null)
            {
                return;
            }

            string selectedLineName = SelectedWhatsAppLine?.DisplayName ?? "sin línea";
            string fallbackText = chkAutoLineFallback.Checked ? "Sí" : "No";
            var selectedLines = GetSelectedLinesForRun();
            string selectedLineNames = selectedLines.Count == 0
                ? "ninguna"
                : string.Join(", ", selectedLines.Select(line => line.DisplayName));
            string elevenLabsStatus = lblElevenLabsStatus?.Text ?? "No configurado";
            string delayText = $"{appSettings.DelayBetweenMessagesMinutes} min";

            lblConfigurationSummary.Text =
                $"Configuración: {selectedLineName} · Auto-fallback: {fallbackText} · Seleccionadas: {selectedLineNames} · Espera: {delayText} · ElevenLabs: {elevenLabsStatus}";
        }

        private List<WhatsAppLine> GetSelectedLinesForRun()
        {
            return whatsAppLines
                .Where(line => line.Enabled && selectedLineIdsForRun.Contains(line.Id))
                .OrderBy(line => line.Priority)
                .ThenBy(line => line.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void InitializeSendOrchestrator()
        {
            sendOrchestrator = new WhatsAppSendOrchestrator(whatsAppLines);
            sendOrchestrator.Log += message => Ui(() => Log(message));
            sendOrchestrator.ProgressChanged += progress => Ui(() =>
            {
                SetProgressFeedback(progress.Processed, progress.Total);
                SetSendCounters(progress.Successes, progress.Errors, progress.Skipped);
            });
            sendOrchestrator.StateChanged += state => Ui(() => ApplyOrchestratorState(state));
            sendOrchestrator.LineChanged += line => Ui(() => SelectWhatsAppLine(line));
            sendOrchestrator.QrInstructionRequired += line => Ui(() =>
            {
                MessageBox.Show(
                    "Escanea el QR en la ventana de Chrome y luego continúa.",
                    $"WhatsApp Web - {line.DisplayName}",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            });
            sendOrchestrator.SenderOpenFailed += (line, message) => Ui(() =>
            {
                MessageBox.Show(
                    $"No se pudo abrir Chrome para la línea {line.DisplayName}.\n\n{message}",
                    "Error abriendo WhatsApp Web",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            });
        }

        private void Ui(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action);
                return;
            }

            action();
        }

        private void mainScrollPanel_Resize(object sender, EventArgs e)
        {
            AdjustResponsiveLayout();
        }

        private void AdjustResponsiveLayout()
        {
            if (mainScrollPanel == null || mainLayout == null)
            {
                return;
            }

            int availableWidth = mainScrollPanel.ClientSize.Width
                - (ResponsiveLayoutPadding * 2)
                - SystemInformation.VerticalScrollBarWidth;
            int availableHeight = mainScrollPanel.ClientSize.Height
                - (ResponsiveLayoutPadding * 2);
            int minimumHeight = chkShowExcelPreview?.Checked == true
                ? ResponsiveLayoutMinimumHeightWithPreview
                : ResponsiveLayoutMinimumHeightWithoutPreview;
            if (isConfigurationExpanded)
            {
                minimumHeight += (int)(ConfigurationExpandedHeight - ConfigurationCollapsedHeight);
            }

            mainLayout.Location = new Point(ResponsiveLayoutPadding, ResponsiveLayoutPadding);
            mainLayout.Size = new Size(
                Math.Max(ResponsiveLayoutMinimumWidth, availableWidth),
                Math.Max(minimumHeight, availableHeight));
        }

        private void InitializeScheduleControls()
        {
            var defaultTime = DateTime.Now.AddMinutes(5);
            scheduledTime = SinSegundos(defaultTime);
            UpdateScheduleSummary();
        }

        private DateTime GetSelectedScheduleDateTime()
        {
            return scheduledTime;
        }

        private void UpdateScheduleSummary()
        {
            lblSelectedScheduleTime.Text = $"{GetSelectedScheduleDateTime():dd/MM/yyyy HH:mm}";
            lblScheduleSummary.Text = $"Programado para: {GetSelectedScheduleDateTime():dd/MM/yyyy HH:mm}";
        }

        private void btnChangeScheduleTime_Click(object sender, EventArgs e)
        {
            if (isScheduled || isSending || isPreparingLines)
            {
                return;
            }

            using var picker = new SchedulePickerForm(GetSelectedScheduleDateTime());
            if (picker.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            scheduledTime = SinSegundos(picker.SelectedDateTime);
            UpdateScheduleSummary();
        }

        private void ApplyOrchestratorState(WhatsAppSendOrchestrationState state)
        {
            switch (state)
            {
                case WhatsAppSendOrchestrationState.Sending:
                    isSending = true;
                    isScheduled = false;
                    schedulerTimer.Stop();
                    isPaused = false;
                    cancellationRequested = false;
                    cmbWhatsAppLine.Enabled = false;
                    chkAutoLineFallback.Enabled = false;
                    btnPauseResume.Text = "Pausar";
                    SetGeneralStatus(StatusSending);
                    break;
                case WhatsAppSendOrchestrationState.Paused:
                    isSending = true;
                    isPaused = true;
                    btnPauseResume.Text = "Reanudar";
                    SetGeneralStatus(StatusPaused);
                    break;
                case WhatsAppSendOrchestrationState.Cancelled:
                    bool cancellationStillStopping = sendOrchestrator.IsRunning;
                    isSending = cancellationStillStopping;
                    isScheduled = false;
                    isPaused = false;
                    cancellationRequested = cancellationStillStopping;
                    cmbWhatsAppLine.Enabled = !cancellationStillStopping;
                    chkAutoLineFallback.Enabled = !cancellationStillStopping;
                    btnPauseResume.Text = "Pausar";
                    SetGeneralStatus(StatusCancelled);
                    break;
                case WhatsAppSendOrchestrationState.Finished:
                    isSending = false;
                    isScheduled = false;
                    isPaused = false;
                    cancellationRequested = false;
                    cmbWhatsAppLine.Enabled = true;
                    chkAutoLineFallback.Enabled = true;
                    btnPauseResume.Text = "Pausar";
                    SetGeneralStatus(StatusFinished);
                    break;
            }

            UpdateActionButtons();
        }

        private void SelectWhatsAppLine(WhatsAppLine line)
        {
            var matchingLine = whatsAppLines.FirstOrDefault(item =>
                string.Equals(item.Id, line.Id, StringComparison.OrdinalIgnoreCase));

            if (matchingLine == null || ReferenceEquals(cmbWhatsAppLine.SelectedItem, matchingLine))
            {
                return;
            }

            suppressLineSelectionChanged = true;
            try
            {
                cmbWhatsAppLine.SelectedItem = matchingLine;
            }
            finally
            {
                suppressLineSelectionChanged = false;
            }

            UpdateSelectedLineStatus();
            RefreshWhatsAppLineMenu();
            UpdateMenuState();
        }

        private void cmbWhatsAppLine_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressLineSelectionChanged)
            {
                return;
            }

            var selectedLine = SelectedWhatsAppLine;
            if (selectedLine == null)
            {
                return;
            }

            sendOrchestrator.SetSelectedLine(selectedLine);
            ReconcileSelectedLinesForRun(includePrimary: true);
            UpdateSelectedLineStatus();
            RefreshWhatsAppLineMenu();
            UpdateMenuState();
        }

        private void btnPrepareLine_Click(object sender, EventArgs e)
        {
            var selectedLine = SelectedWhatsAppLine;
            if (selectedLine == null)
            {
                MessageBox.Show("Selecciona una línea para preparar.", "Preparar línea", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isPreparingLines = true;
            UpdateActionButtons();

            try
            {
                var state = PrepareLineWithManualRetry(selectedLine);
                if (state == WhatsAppLineOperationalState.Ready)
                {
                    MessageBox.Show(
                        $"La línea {selectedLine.DisplayName} quedó lista.",
                        "Preparar línea",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            finally
            {
                isPreparingLines = false;
                UpdateSelectedLineStatus();
                UpdateActionButtons();
            }
        }

        private void btnPrepareAllLines_Click(object sender, EventArgs e)
        {
            var enabledLines = whatsAppLines
                .Where(line => line.Enabled)
                .OrderBy(line => line.Priority)
                .ThenBy(line => line.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (enabledLines.Count == 0)
            {
                MessageBox.Show("No hay líneas habilitadas para preparar.", "Preparar todas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isPreparingLines = true;
            UpdateActionButtons();

            try
            {
                foreach (var line in enabledLines)
                {
                    var state = PrepareLineWithManualRetry(line);
                    if (state == WhatsAppLineOperationalState.Ready)
                    {
                        continue;
                    }

                    var continueResult = MessageBox.Show(
                        $"La línea {line.DisplayName} no quedó lista.\n\n¿Quieres continuar con las demás líneas?",
                        "Preparar todas",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (continueResult != DialogResult.Yes)
                    {
                        Log("Preparación de líneas detenida por el usuario.");
                        break;
                    }
                }

                ShowLinePreparationSummary();
            }
            finally
            {
                isPreparingLines = false;
                UpdateSelectedLineStatus();
                UpdateActionButtons();
            }
        }

        private WhatsAppLineOperationalState PrepareLineWithManualRetry(WhatsAppLine line)
        {
            while (true)
            {
                var result = sendOrchestrator.PrepareLine(line, TimeSpan.FromSeconds(12));
                line = UpdateWhatsAppLineState(line, result.State);

                if (result.State == WhatsAppLineOperationalState.Ready)
                {
                    return result.State;
                }

                if (result.State == WhatsAppLineOperationalState.RequiresManualAuth)
                {
                    var retryResult = MessageBox.Show(
                        "Escanea el QR en la ventana de Chrome y luego presiona Reintentar.",
                        $"Preparar línea - {line.DisplayName}",
                        MessageBoxButtons.RetryCancel,
                        MessageBoxIcon.Information);

                    if (retryResult == DialogResult.Retry)
                    {
                        Log($"Reintentando verificación de la línea {line.DisplayName} después de intervención manual.");
                        continue;
                    }

                    Log($"Preparación manual cancelada para la línea {line.DisplayName}.");
                    line = UpdateWhatsAppLineState(line, WhatsAppLineOperationalState.NotAvailable);
                    return line.OperationalState;
                }

                return result.State;
            }
        }

        private void ShowLinePreparationSummary()
        {
            var enabledLines = whatsAppLines.Where(line => line.Enabled).ToList();
            int ready = enabledLines.Count(line => line.OperationalState == WhatsAppLineOperationalState.Ready);
            int pending = enabledLines.Count(line =>
                line.OperationalState == WhatsAppLineOperationalState.Unknown
                || line.OperationalState == WhatsAppLineOperationalState.RequiresManualAuth);
            int unavailable = enabledLines.Count(line => line.OperationalState == WhatsAppLineOperationalState.NotAvailable);

            string summary = $"Listas: {ready}\nPendientes de preparación: {pending}\nNo disponibles: {unavailable}";
            Log($"Resumen preparación de líneas. Listas: {ready}. Pendientes: {pending}. No disponibles: {unavailable}.");
            MessageBox.Show(summary, "Resumen de preparación", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void btnSelectFile_Click(object sender, EventArgs e)
        {
            await SelectExcelFileAsync();
        }

        private async Task SelectExcelFileAsync()
        {
            openFileDialog1.Filter = "Archivos Excel|*.xlsx;*.xls";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                await SetSelectedExcelFileAsync(openFileDialog1.FileName);
            }
        }

        private void btnDownloadTemplate_Click(object sender, EventArgs e)
        {
            DownloadExcelTemplate();
        }

        private void DownloadExcelTemplate()
        {
            using var saveFileDialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "xlsx",
                FileName = ExcelTemplateService.DefaultFileName,
                Filter = "Libro de Excel (*.xlsx)|*.xlsx",
                OverwritePrompt = true,
                Title = "Guardar plantilla Excel"
            };

            if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                ExcelTemplateService.CreateTemplate(saveFileDialog.FileName);
                Log($"Plantilla Excel creada: {saveFileDialog.FileName}");
                MessageBox.Show(
                    "La plantilla Excel se creó correctamente.",
                    "Descargar plantilla",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log($"No se pudo crear la plantilla Excel: {ex.Message}");
                MessageBox.Show(
                    $"No se pudo crear la plantilla Excel.\n\n{ex.Message}",
                    "Descargar plantilla",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async Task SetSelectedExcelFileAsync(string selectedPath)
        {
            excelPath = selectedPath;
            lblFilePath.Text = $"Archivo: {Path.GetFileName(excelPath)}";
            uiToolTip.SetToolTip(lblFilePath, excelPath);
            await LoadExcelPreviewAsync(selectedPath, ++excelPreviewLoadVersion);
        }

        private async Task LoadExcelPreviewAsync(string selectedPath, int loadVersion)
        {
            isLoadingExcelPreview = true;
            excelPreviewRows = new List<ExcelMessagePreviewRow>();
            ClearExcelPreviewGrid();
            lblPreviewSummary.Text = "Total filas: 0 | Válidas: 0 | Inválidas: 0 | Audios: 0";
            lblPreviewStatus.Text = "Analizando archivo Excel...";
            lblPreviewStatus.ForeColor = Color.FromArgb(75, 85, 99);
            lblExcelCompactSummary.Text = $"{Path.GetFileName(selectedPath)} - analizando Excel...";
            lblExcelCompactSummary.ForeColor = Color.FromArgb(75, 85, 99);
            SetSendFeedback(StatusNoFile, 0, 0, 0, 0, 0);
            UpdateActionButtons();

            try
            {
                var loadedRows = await Task.Run(() => ExcelReader.ReadPreview(selectedPath));
                if (loadVersion != excelPreviewLoadVersion)
                {
                    return;
                }

                excelPreviewRows = loadedRows;
                BindExcelPreview();
                Log("Archivo Excel seleccionado correctamente.");
                Log($"Excel analizado: {excelPreviewRows.Count} filas.");
            }
            catch (Exception ex)
            {
                if (loadVersion != excelPreviewLoadVersion)
                {
                    return;
                }

                ClearSelectedExcelAfterReadError(ex);
            }
            finally
            {
                if (loadVersion == excelPreviewLoadVersion)
                {
                    isLoadingExcelPreview = false;
                    UpdateActionButtons();
                }
            }
        }

        private void LoadExcelPreview()
        {
            try
            {
                excelPreviewRows = ExcelReader.ReadPreview(excelPath);
                BindExcelPreview();
                Log("Archivo Excel seleccionado correctamente.");
                Log($"Excel analizado: {excelPreviewRows.Count} filas.");
            }
            catch (Exception ex)
            {
                ClearSelectedExcelAfterReadError(ex);
            }
        }

        private void ClearSelectedExcelAfterReadError(Exception ex)
        {
            excelPath = "";
            excelPreviewRows = new List<ExcelMessagePreviewRow>();
            lblFilePath.Text = "Archivo seleccionado: ninguno";
            uiToolTip.SetToolTip(lblFilePath, lblFilePath.Text);
            BindExcelPreview();
            lblPreviewStatus.Text = $"No se pudo leer el Excel: {ex.Message}";
            lblPreviewStatus.ForeColor = Color.FromArgb(185, 28, 28);
            lblExcelCompactSummary.Text = "Resumen: no se pudo leer el Excel seleccionado.";
            lblExcelCompactSummary.ForeColor = Color.FromArgb(185, 28, 28);
            SetSendFeedback(StatusNoFile, 0, 0, 0, 0, 0);
            UpdateActionButtons();
            Log($"No se pudo leer el Excel seleccionado: {ex.Message}");
        }

        private void BindExcelPreview()
        {
            UpdateExcelPreviewSummary();

            if (chkShowExcelPreview.Checked)
            {
                BindExcelPreviewGrid();
                return;
            }

            ClearExcelPreviewGrid();
        }

        private void BindExcelPreviewGrid()
        {
            if (!chkShowExcelPreview.Checked)
            {
                return;
            }

            dgvExcelPreview.SuspendLayout();
            try
            {
                dgvExcelPreview.DataSource = null;
                dgvExcelPreview.DataSource = excelPreviewRows;
                ApplyExcelPreviewRowStyles();
            }
            finally
            {
                dgvExcelPreview.ResumeLayout();
            }
        }

        private void ClearExcelPreviewGrid()
        {
            if (dgvExcelPreview.DataSource != null)
            {
                dgvExcelPreview.DataSource = null;
            }
        }

        private void UpdateExcelPreviewSummary()
        {
            int totalRows = excelPreviewRows.Count;
            int validRows = excelPreviewRows.Count(row => row.IsValid);
            int invalidRows = totalRows - validRows;
            int audioRows = excelPreviewRows.Count(row => row.IsValid && row.ToAudio);

            string totalsText = $"Total filas: {totalRows} | Válidas: {validRows} | Inválidas: {invalidRows} | Audios: {audioRows}";
            lblPreviewSummary.Text = totalsText;

            if (string.IsNullOrWhiteSpace(excelPath))
            {
                lblExcelCompactSummary.Text = "Resumen: sin Excel seleccionado.";
                lblExcelCompactSummary.ForeColor = Color.FromArgb(75, 85, 99);
                lblPreviewStatus.Text = "Selecciona un archivo Excel para revisar las filas antes de programar.";
                lblPreviewStatus.ForeColor = Color.FromArgb(75, 85, 99);
            }
            else
            {
                lblExcelCompactSummary.Text = $"{Path.GetFileName(excelPath)} - {totalsText}";
                lblExcelCompactSummary.ForeColor = validRows == 0
                    ? Color.FromArgb(185, 28, 28)
                    : invalidRows > 0
                        ? Color.FromArgb(180, 83, 9)
                        : Color.FromArgb(21, 128, 61);
            }

            if (!isScheduled && !isSending)
            {
                string status = validRows > 0 ? StatusReady : StatusNoFile;
                SetSendFeedback(status, 0, validRows, 0, 0, invalidRows);
            }

            UpdateActionButtons();

            if (string.IsNullOrWhiteSpace(excelPath))
            {
                return;
            }

            if (totalRows == 0)
            {
                lblPreviewStatus.Text = "El Excel no tiene filas de datos para enviar.";
                lblPreviewStatus.ForeColor = Color.FromArgb(185, 28, 28);
                return;
            }

            if (validRows == 0)
            {
                lblPreviewStatus.Text = "Todas las filas son inválidas. Corrige teléfono y mensaje antes de programar.";
                lblPreviewStatus.ForeColor = Color.FromArgb(185, 28, 28);
                return;
            }

            if (invalidRows > 0)
            {
                lblPreviewStatus.Text = $"Hay {invalidRows} fila(s) inválida(s). No se enviarán; solo se programarán {validRows} fila(s) válida(s).";
                lblPreviewStatus.ForeColor = Color.FromArgb(180, 83, 9);
                return;
            }

            lblPreviewStatus.Text = "Todas las filas tienen teléfono y mensaje. Puedes programar el envío.";
            lblPreviewStatus.ForeColor = Color.FromArgb(21, 128, 61);
        }

        private void ApplyExcelPreviewRowStyles()
        {
            foreach (DataGridViewRow gridRow in dgvExcelPreview.Rows)
            {
                if (gridRow.DataBoundItem is not ExcelMessagePreviewRow previewRow)
                {
                    continue;
                }

                if (!previewRow.IsValid)
                {
                    gridRow.DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242);
                    gridRow.DefaultCellStyle.ForeColor = Color.FromArgb(127, 29, 29);
                    gridRow.DefaultCellStyle.SelectionBackColor = Color.FromArgb(252, 165, 165);
                    gridRow.DefaultCellStyle.SelectionForeColor = Color.FromArgb(69, 10, 10);
                }
                else
                {
                    gridRow.DefaultCellStyle.BackColor = Color.White;
                    gridRow.DefaultCellStyle.ForeColor = SystemColors.ControlText;
                    gridRow.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
                    gridRow.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
                }
            }
        }

        private List<OutboundMessage> GetValidPreviewMessages()
        {
            return excelPreviewRows
                .Where(row => row.IsValid)
                .Select(row => new OutboundMessage(row.CountryCode, row.Phone, row.Message, row.ToAudio))
                .ToList();
        }

        private int ValidPreviewRows => excelPreviewRows.Count(row => row.IsValid);

        private int InvalidPreviewRows => excelPreviewRows.Count(row => !row.IsValid);

        private void SetSendFeedback(string status, int processed, int total, int successes, int errors, int skipped)
        {
            SetGeneralStatus(status);
            SetProgressFeedback(processed, total);
            SetSendCounters(successes, errors, skipped);
        }

        private void SetGeneralStatus(string status)
        {
            lblGeneralStatus.Text = $"Estado general: {status}";
            lblGeneralStatus.ForeColor = status switch
            {
                StatusReady => Color.FromArgb(21, 128, 61),
                StatusScheduled => Color.FromArgb(37, 99, 235),
                StatusSending => Color.FromArgb(22, 101, 52),
                StatusPaused => Color.FromArgb(180, 83, 9),
                StatusCancelled => Color.FromArgb(185, 28, 28),
                StatusFinished => Color.FromArgb(21, 128, 61),
                _ => Color.FromArgb(75, 85, 99)
            };
        }

        private void SetProgressFeedback(int processed, int total)
        {
            int safeTotal = Math.Max(total, 0);
            int safeProcessed = Math.Min(Math.Max(processed, 0), safeTotal);

            lblProgressText.Text = $"Procesados: {safeProcessed} / {safeTotal}";
            progressBar1.Minimum = 0;
            progressBar1.Maximum = Math.Max(safeTotal, 1);
            progressBar1.Value = safeTotal == 0 ? 0 : safeProcessed;
        }

        private void SetSendCounters(int successes, int errors, int skipped)
        {
            lblSendCounters.Text = $"Éxitos: {Math.Max(successes, 0)} | Errores: {Math.Max(errors, 0)} | Omitidos: {Math.Max(skipped, 0)}";
        }

        private void UpdateActionButtons()
        {
            bool operationActive = (isScheduled || isSending) && !cancellationRequested;
            bool operationBlockingSetup = isScheduled || isSending || isPreparingLines;
            bool canRequestSend = SendActionPolicy.CanRequestSend(
                new SendActionButtonState(isSending, isScheduled, isPreparingLines));

            btnPauseResume.Enabled = operationActive;
            btnCancel.Enabled = operationActive;
            btnPrepareLine.Enabled = SelectedWhatsAppLine != null && !operationBlockingSetup;
            btnPrepareAllLines.Enabled = whatsAppLines.Count > 0 && !operationBlockingSetup;
            btnSelectRunLines.Enabled = whatsAppLines.Count > 0 && !operationBlockingSetup;
            btnConfigureLines.Enabled = !operationBlockingSetup;
            // Los botones de envío quedan disponibles aunque falten precondiciones;
            // las validaciones centralizadas explican el bloqueo con log y MessageBox.
            btnSend.Enabled = canRequestSend;
            btnSendNow.Enabled = canRequestSend;
            btnChangeScheduleTime.Enabled = !operationBlockingSetup;
            cmbWhatsAppLine.Enabled = !operationBlockingSetup;
            chkAutoLineFallback.Enabled = !operationBlockingSetup;
            SetElevenLabsControlsEnabled(!operationBlockingSetup);
            UpdateMenuState();
        }

        private void dgvExcelPreview_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            ApplyExcelPreviewRowStyles();
            dgvExcelPreview.ClearSelection();
        }

        private void BtnPauseResume_Click(object sender, EventArgs e)
        {
            if (!isScheduled && !isSending)
                return;

            if (isSending)
            {
                if (sendOrchestrator.IsPaused)
                {
                    sendOrchestrator.Resume();
                }
                else
                {
                    sendOrchestrator.Pause();
                }

                return;
            }

            isPaused = !isPaused;
            btnPauseResume.Text = isPaused ? "Reanudar" : "Pausar";
            SetGeneralStatus(isPaused ? StatusPaused : isSending ? StatusSending : StatusScheduled);
            UpdateActionButtons();
            Log(isPaused ? "Temporizador pausado." : "Temporizador reanudado.");
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            if (!isScheduled && !isSending)
            {
                return;
            }

            bool wasSending = isSending;
            if (wasSending)
            {
                cancellationRequested = true;
                sendOrchestrator.Cancel();
                UpdateActionButtons();
                return;
            }

            schedulerTimer.Stop();
            cancellationRequested = true;
            isScheduled = false;
            isPaused = false;
            if (!isSending)
            {
                cancellationRequested = false;
            }
            btnPauseResume.Text = "Pausar";
            cmbWhatsAppLine.Enabled = true;
            chkAutoLineFallback.Enabled = true;
            SetGeneralStatus(StatusCancelled);
            UpdateActionButtons();
            Log(wasSending ? "Envío cancelado." : "Programación cancelada.");
        }

        private static DateTime SinSegundos(DateTime dt)
            => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);

        private async void SchedulerTimer_Tick(object sender, EventArgs e)
        {
            if (!isScheduled)
            {
                // El Tick puede llegar justo después de cancelar o iniciar envío; no hay acción de usuario que reportar.
                schedulerTimer.Stop();
                return;
            }

            if (isPaused)
            {
                // En pausa el timer sigue vivo para poder reanudarse sin reprogramar; no se loguea cada segundo.
                return;
            }

            var target = SinSegundos(scheduledTime);

            if (DateTime.Now >= target)
            {
                await StartSendAsync(SendStartTrigger.ScheduledTimer);
            }
        }


        private void btnSend_Click(object sender, EventArgs e)
        {
            Log("Programación de envío solicitada.");

            if (TryStartSchedule())
            {
                MessageBox.Show(
                    $"Envío programado para {scheduledTime:dd/MM/yyyy HH:mm}",
                    "Programación creada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private async void btnSendNow_Click(object sender, EventArgs e)
        {
            await StartSendAsync(SendStartTrigger.ImmediateClick);
        }

        private void btnConfigureLines_Click(object sender, EventArgs e)
        {
            if (isScheduled || isSending || isPreparingLines)
            {
                MessageBox.Show(
                    "No se pueden modificar las líneas mientras hay una programación, envío o preparación en curso.",
                    "Configurar líneas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string? selectedLineId = SelectedWhatsAppLine?.Id;
            using var settingsForm = new LineSettingsForm();

            if (settingsForm.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            LoadWhatsAppLines(selectedLineId);
        }

        private void btnSelectRunLines_Click(object sender, EventArgs e)
        {
            var enabledLines = whatsAppLines
                .Where(line => line.Enabled)
                .OrderBy(line => line.Priority)
                .ThenBy(line => line.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (enabledLines.Count == 0)
            {
                MessageBox.Show(
                    "No hay líneas habilitadas para seleccionar.",
                    "Seleccionar líneas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var selectedIds = ShowRunLineSelectionDialog(enabledLines);
            if (selectedIds == null)
            {
                return;
            }

            selectedLineIdsForRun = new HashSet<string>(selectedIds, StringComparer.OrdinalIgnoreCase);
            UpdateRunLinesSummary();
            LogSelectedLinesForRun();

            if (!chkAutoLineFallback.Checked && selectedLineIdsForRun.Count > 1)
            {
                MessageBox.Show(
                    "El cambio automático está desactivado. Para esta corrida solo se usará la línea principal seleccionada en el ComboBox.",
                    "Línea alternativa desactivada",
                    MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            }
        }

        private IReadOnlyList<string>? ShowRunLineSelectionDialog(IReadOnlyList<WhatsAppLine> enabledLines)
        {
            using var dialog = new Form
            {
                Text = "Seleccionar líneas",
                FormBorderStyle = FormBorderStyle.Sizable,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                MinimumSize = new Size(420, 360),
                ClientSize = new Size(520, 420)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            var instructions = new Label
            {
                Dock = DockStyle.Fill,
                Text = chkAutoLineFallback.Checked
                    ? "Marca las líneas habilitadas que podrán participar en esta corrida."
                    : "El cambio automático está desactivado. Puedes marcar líneas, pero solo se enviará desde la principal.",
                TextAlign = ContentAlignment.MiddleLeft
            };

            var checkedLines = new CheckedListBox
            {
                CheckOnClick = true,
                Dock = DockStyle.Fill,
                FormattingEnabled = true
            };

            var primaryLineId = SelectedWhatsAppLine?.Id;
            foreach (var line in enabledLines)
            {
                bool isChecked = selectedLineIdsForRun.Contains(line.Id)
                    || string.Equals(line.Id, primaryLineId, StringComparison.OrdinalIgnoreCase);
                checkedLines.Items.Add(line, isChecked);
            }

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            var okButton = new Button
            {
                Text = "Aceptar",
                Size = new Size(96, 32)
            };

            var cancelButton = new Button
            {
                Text = "Cancelar",
                Size = new Size(96, 32),
                DialogResult = DialogResult.Cancel
            };

            IReadOnlyList<string>? result = null;
            okButton.Click += (_, _) =>
            {
                result = checkedLines.CheckedItems
                    .Cast<WhatsAppLine>()
                    .Select(line => line.Id)
                    .ToList();

                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            };

            actions.Controls.Add(okButton);
            actions.Controls.Add(cancelButton);
            layout.Controls.Add(instructions, 0, 0);
            layout.Controls.Add(checkedLines, 0, 1);
            layout.Controls.Add(actions, 0, 2);
            dialog.Controls.Add(layout);
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;

            return dialog.ShowDialog(this) == DialogResult.OK ? result : null;
        }

        private bool TryStartSchedule()
        {
            scheduledTime = GetSelectedScheduleDateTime();
            UpdateScheduleSummary();

            if (!TryBuildMessagesForSend(
                out var validMessages,
                SendActionKind.Schedule,
                showFallbackWarning: true,
                failureContext: "No se programó el envío",
                confirmInvalidRows: true))
            {
                return false;
            }

            int invalidRows = excelPreviewRows.Count(row => !row.IsValid);
            isScheduled = true;
            isPaused = false;
            isSending = false;
            cancellationRequested = false;
            btnPauseResume.Text = "Pausar";
            SetSendFeedback(StatusScheduled, 0, validMessages.Count, 0, 0, invalidRows);
            LogSelectedLinesForRun();
            LogReadyFallbackCandidates();
            UpdateActionButtons();
            schedulerTimer.Start();
            Log($"Envío programado para {scheduledTime:dd/MM/yyyy HH:mm}.");
            return true;
        }

        private async Task StartSendAsync(SendStartTrigger trigger)
        {
            bool fromSchedule = trigger == SendStartTrigger.ScheduledTimer;
            string failureContext = fromSchedule
                ? "No se inició el envío programado"
                : "No se inició el envío inmediato";

            Log(fromSchedule
                ? $"Hora programada alcanzada ({scheduledTime:dd/MM/yyyy HH:mm}). Iniciando envío programado..."
                : "Iniciando envío inmediato...");

            if (fromSchedule)
            {
                schedulerTimer.Stop();
                isScheduled = false;
                isPaused = false;
            }

            if (!TryBuildMessagesForSend(
                out var mensajes,
                SendActionKind.SendNow,
                showFallbackWarning: !fromSchedule,
                failureContext: failureContext,
                confirmInvalidRows: !fromSchedule))
            {
                isSending = false;
                cancellationRequested = false;
                if (!isScheduled)
                {
                    SetGeneralStatus(StatusCancelled);
                }

                UpdateActionButtons();
                return;
            }

            schedulerTimer.Stop();
            isScheduled = false;
            isPaused = false;
            isSending = true;
            cancellationRequested = false;
            btnPauseResume.Text = "Pausar";
            SetSendFeedback(StatusSending, 0, mensajes.Count, 0, 0, InvalidPreviewRows);
            LogSelectedLinesForRun();
            LogReadyFallbackCandidates();
            UpdateActionButtons();

            await RunSendAsync(mensajes);
        }

        private bool TryBuildMessagesForSend(
            out List<OutboundMessage> validMessages,
            SendActionKind action,
            bool showFallbackWarning,
            string failureContext,
            bool confirmInvalidRows)
        {
            validMessages = new List<OutboundMessage>();

            bool hadExcelBeforeLoad = !string.IsNullOrEmpty(excelPath);
            if (hadExcelBeforeLoad && !isLoadingExcelPreview && excelPreviewRows.Count == 0)
            {
                LoadExcelPreview();
            }

            if (hadExcelBeforeLoad && string.IsNullOrEmpty(excelPath))
            {
                ShowSendBlocked(
                    $"{failureContext}: no se pudo leer el Excel seleccionado.",
                    "No se pudo leer el Excel seleccionado. Vuelve a seleccionar un archivo Excel válido.",
                    "Excel no válido",
                    MessageBoxIcon.Warning);
                return false;
            }

            if (selectedLineIdsForRun.Count > 0
                && SelectedWhatsAppLine is { } selectedLine
                && !selectedLineIdsForRun.Contains(selectedLine.Id))
            {
                selectedLineIdsForRun.Add(selectedLine.Id);
                UpdateRunLinesSummary();
                Log($"La línea principal {selectedLine.DisplayName} se agregó automáticamente a la selección de esta corrida.");
            }

            var readiness = SendActionPolicy.Evaluate(CreateSendActionPolicyInput(action, failureContext));
            if (!readiness.CanProceed)
            {
                ShowSendBlocked(readiness);
                return false;
            }

            ShowFallbackWarningIfNeeded(showFallbackWarning);
            validMessages = GetValidPreviewMessages();
            int invalidRows = excelPreviewRows.Count(row => !row.IsValid);
            if (invalidRows > 0 && confirmInvalidRows)
            {
                var result = MessageBox.Show(
                    $"El Excel tiene {invalidRows} fila(s) inválida(s). No se enviarán.\n\n¿Quieres continuar únicamente con las {validMessages.Count} fila(s) válida(s)?",
                    "Filas inválidas detectadas",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                {
                    Log($"{failureContext}: el usuario canceló al detectar {invalidRows} fila(s) inválida(s).");
                    return false;
                }
            }

            return true;
        }

        private void ShowFallbackWarningIfNeeded(bool showFallbackWarning)
        {
            int enabledSelectedCount = GetSelectedLinesForRun().Count;
            if (showFallbackWarning && chkAutoLineFallback.Checked && enabledSelectedCount == 1)
            {
                MessageBox.Show(
                    "El cambio automático está activado, pero solo hay una línea seleccionada para esta corrida. El envío continuará sin alternativas disponibles.",
                    "Sin líneas alternativas seleccionadas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                Log("Fallback automático activado con una sola línea seleccionada; no habrá alternativas para esta corrida.");
            }
        }

        private SendActionPolicyInput CreateSendActionPolicyInput(SendActionKind action, string failureContext)
        {
            var selectedLine = SelectedWhatsAppLine;
            return new SendActionPolicyInput
            {
                Action = action,
                FailureContext = failureContext,
                HasExcel = !string.IsNullOrEmpty(excelPath),
                ValidRowCount = ValidPreviewRows,
                AvailableLineCount = whatsAppLines.Count,
                HasSelectedLine = selectedLine != null,
                SelectedRunLineCount = selectedLineIdsForRun.Count,
                EnabledSelectedRunLineCount = GetSelectedLinesForRun().Count,
                SelectedLineState = selectedLine?.OperationalState ?? WhatsAppLineOperationalState.Unknown,
                SelectedLineName = selectedLine?.DisplayName ?? "",
                IsSending = isSending,
                IsScheduled = isScheduled,
                IsPreparingLines = isPreparingLines,
                IsLoadingExcelPreview = isLoadingExcelPreview,
                ScheduledTime = action == SendActionKind.Schedule ? scheduledTime : null,
                Now = DateTime.Now
            };
        }

        private void ShowSendBlocked(SendActionPolicyResult result)
        {
            Log(result.LogMessage);
            MessageBox.Show(
                result.UserMessage,
                result.Title,
                MessageBoxButtons.OK,
                result.Severity == SendActionBlockSeverity.Information
                    ? MessageBoxIcon.Information
                    : MessageBoxIcon.Warning);
        }

        private void ShowSendBlocked(
            string logMessage,
            string userMessage,
            string title,
            MessageBoxIcon icon)
        {
            Log(logMessage);
            MessageBox.Show(userMessage, title, MessageBoxButtons.OK, icon);
        }

        private void LogReadyFallbackCandidates()
        {
            if (!chkAutoLineFallback.Checked || SelectedWhatsAppLine == null)
            {
                return;
            }

            int readyAlternatives = whatsAppLines.Count(line =>
                line.Enabled
                && line.OperationalState == WhatsAppLineOperationalState.Ready
                && selectedLineIdsForRun.Contains(line.Id)
                && !string.Equals(line.Id, SelectedWhatsAppLine.Id, StringComparison.OrdinalIgnoreCase));

            Log($"Fallback automático habilitado. Líneas alternativas listas: {readyAlternatives}.");
        }

        private void LogSelectedLinesForRun()
        {
            var selectedLines = GetSelectedLinesForRun();
            string selectedNames = selectedLines.Count == 0
                ? "ninguna"
                : string.Join(", ", selectedLines.Select(line => line.DisplayName));

            Log($"Líneas seleccionadas para esta corrida: {selectedNames}");
        }

        private void Log(string message)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        private static string FormatDelayBetweenMessagesLog(int delayMinutes)
        {
            return delayMinutes <= 0
                ? "Tiempo de espera entre mensajes: sin espera adicional."
                : $"Tiempo de espera entre mensajes: {delayMinutes} minuto(s).";
        }

        private async Task RunSendAsync(List<OutboundMessage> mensajes)
        {
            Log(FormatDelayBetweenMessagesLog(appSettings.DelayBetweenMessagesMinutes));

            var summary = await sendOrchestrator.SendAsync(
                mensajes,
                new WhatsAppSendOptions(
                    SelectedWhatsAppLine,
                    chkAutoLineFallback.Checked,
                    InvalidPreviewRows,
                    SelectedLineIdsForRun: selectedLineIdsForRun.ToList(),
                    DelayBetweenMessagesMinutes: appSettings.DelayBetweenMessagesMinutes));

            if (summary.StoppedByGlobalFailure)
            {
                MessageBox.Show(
                    $"WhatsApp Web ya no permite continuar con el envío.\n\nMensajes procesados: {summary.Processed}\nMensajes pendientes: {summary.Pending}\nMotivo: {summary.GlobalFailureReason}",
                    "Envío detenido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else if (summary.CompletedSuccessfully)
            {
                MessageBox.Show(
                    $"Todos los mensajes fueron procesados.\n\n{summary.DisplaySummary}",
                    "Envío finalizado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            var result = MessageBox.Show("¿Estás seguro de que deseas salir?", "Confirmar salida", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No)
            {
                e.Cancel = true; // Cancela el cierre
            }
            else
            {
                sendOrchestrator.CloseCurrentSender();
            }
        }
    }
}


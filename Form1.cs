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
        private readonly List<WhatsAppMessageResult> currentSendResults = new();
        private string? selectedWhatsAppLineId;
        private bool autoFallbackEnabled;
        private HashSet<string> selectedLineIdsForRun = new(StringComparer.OrdinalIgnoreCase);
        private WhatsAppSendOrchestrator sendOrchestrator = null!;
        private ToolStripMenuItem exportReportToolStripMenuItem = null!;
        private AutoWhatsAppSettings appSettings = AutoWhatsAppSettings.Default;
        private ElevenLabsSettings elevenLabsSettings = ElevenLabsSettings.FromEnvironment();
        private string elevenLabsStatusText = "No configurado";
        private Color elevenLabsStatusColor = Color.FromArgb(75, 85, 99);
        private string elevenLabsStatusToolTip = "";

        private string excelPath = "";
        private List<ExcelMessagePreviewRow> excelPreviewRows = new();
        private string excelConsentWarning = "";
        private Button btnDryRun = null!;
        private string approvedDryRunToken = "";
        private DateTime? approvedDryRunAt;

        private System.Windows.Forms.Timer schedulerTimer = null!;
        private DateTime scheduledTime;
        private bool isPaused = false;
        private bool isScheduled = false;
        private bool isSending = false;
        private bool cancellationRequested = false;
        private bool isPreparingLines = false;
        private bool isLoadingExcelPreview = false;
        private int excelPreviewLoadVersion = 0;

        private const string StatusNoFile = "Sin archivo";
        private const string StatusReady = "Listo";
        private const string StatusDryRunRequired = "Dry-run requerido";
        private const string StatusReviewRequired = "Revisar Excel";
        private const string StatusScheduled = "Programado";
        private const string StatusSending = "Enviando";
        private const string StatusPaused = "Pausado";
        private const string StatusCancelled = "Cancelado";
        private const string StatusFinished = "Finalizado";
        private const int ExcelPreviewRowIndex = 2;
        private const int ResponsiveLayoutPadding = 8;
        private const int ResponsiveLayoutMinimumWidth = 700;
        private const int ResponsiveLayoutMinimumHeightWithoutPreview = 560;
        private const int ResponsiveLayoutMinimumHeightWithPreview = 740;
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
            InitializeReportExportMenu();
            InitializeConfigurationStateFromUi();
            ApplyApplicationIcon();
            LoadAppSettings();
            LoadElevenLabsSettingsIntoUi();
            ApplyExcelPreviewVisibility();
            AdjustResponsiveLayout();
            InitializeScheduleControls();
            InitializeDryRunControls();
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

        private void InitializeConfigurationStateFromUi()
        {
            autoFallbackEnabled = false;
        }

        private void InitializeDryRunControls()
        {
            btnDryRun = new Button
            {
                Name = "btnDryRun",
                Text = "Ejecutar dry-run",
                AutoSize = false,
                Size = new Size(142, 34),
                Enabled = false,
                UseVisualStyleBackColor = true,
                Anchor = AnchorStyles.Left
            };
            btnDryRun.Click += btnDryRun_Click;

            fileLayout.SuspendLayout();
            try
            {
                if (fileLayout.ColumnCount < 5)
                {
                    fileLayout.ColumnCount = 5;
                    fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148F));
                    fileLayout.Controls.Add(btnDryRun, 4, 0);
                    fileLayout.SetColumnSpan(lblExcelCompactSummary, 5);
                }
            }
            finally
            {
                fileLayout.ResumeLayout(true);
            }

            uiToolTip.SetToolTip(
                btnDryRun,
                "Genera un reporte local sin enviar mensajes y habilita el envío real solo después de confirmarlo.");
        }

        private void chkShowExcelPreview_CheckedChanged(object sender, EventArgs e)
        {
            mostrarVistaPreviaExcelToolStripMenuItem.Checked = chkShowExcelPreview.Checked;
            ApplyExcelPreviewVisibility();
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
        }

        private void InitializeReportExportMenu()
        {
            exportReportToolStripMenuItem = new ToolStripMenuItem
            {
                Name = "exportReportToolStripMenuItem",
                Size = new Size(216, 22),
                Text = "Exportar reporte...",
                Enabled = false
            };
            exportReportToolStripMenuItem.Click += exportReportToolStripMenuItem_Click;

            int separatorIndex = archivoToolStripMenuItem.DropDownItems.IndexOf(archivoToolStripSeparator);
            int insertIndex = separatorIndex >= 0
                ? separatorIndex
                : archivoToolStripMenuItem.DropDownItems.Count;

            archivoToolStripMenuItem.DropDownItems.Insert(insertIndex, exportReportToolStripMenuItem);
        }

        private void configuracionGeneralToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var configurationDialog = new ConfigurationDialog(
                appSettings,
                CreateConfigurationState(),
                whatsAppLines,
                Icon,
                CanEditConfiguration,
                CanSelectRunLines,
                CanConfigureLines,
                CanPrepareSelectedLine,
                CanPrepareAnyLine,
                CanEditConfiguration,
                CreateElevenLabsDialogState(),
                new ConfigurationDialogActions(
                    ConfigureLinesFromConfigurationDialog,
                    PrepareLineFromConfigurationDialog,
                    PrepareAllLinesFromConfigurationDialog,
                    SaveElevenLabsSettingsFromConfigurationDialog,
                    TestElevenLabsSettingsFromConfigurationDialog));

            if (configurationDialog.ShowDialog(this) != DialogResult.OK)
            {
                LoadElevenLabsSettingsIntoUi();
                return;
            }

            appSettings = configurationDialog.Settings.Normalize();
            ApplyConfigurationState(configurationDialog.ConfigurationState);
            LoadElevenLabsSettingsIntoUi();
            UpdateConfigurationSummary();
            Log($"Configuración general guardada. Espera entre mensajes: {appSettings.DelayBetweenMessagesMinutes} min.");
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
            if (exportReportToolStripMenuItem != null)
            {
                exportReportToolStripMenuItem.Enabled = currentSendResults.Count > 0 && !isSending;
            }

            configuracionGeneralToolStripMenuItem.Enabled = true;
            mostrarVistaPreviaExcelToolStripMenuItem.Enabled = chkShowExcelPreview.Enabled;
            mostrarVistaPreviaExcelToolStripMenuItem.Checked = chkShowExcelPreview.Checked;
        }

        private void LoadWhatsAppLines(string? selectedLineId = null)
        {
            string? selectionToRestore = selectedLineId ?? selectedWhatsAppLineId ?? SelectedWhatsAppLine?.Id;

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

            selectedWhatsAppLineId = ResolveWhatsAppLineById(selectionToRestore)?.Id
                ?? whatsAppLines.FirstOrDefault()?.Id;

            if (sendOrchestrator != null)
            {
                sendOrchestrator.UpdateLines(whatsAppLines);
                sendOrchestrator.SetSelectedLine(SelectedWhatsAppLine);
            }

            ReconcileSelectedLinesForRun(includePrimary: true);
            UpdateSelectedLineStatus();
            UpdateActionButtons();
            Log($"Líneas habilitadas cargadas: {whatsAppLines.Count}");
        }

        private WhatsAppLine? SelectedWhatsAppLine =>
            ResolveWhatsAppLineById(selectedWhatsAppLineId);

        private WhatsAppLine? ResolveWhatsAppLineById(string? lineId)
        {
            if (string.IsNullOrWhiteSpace(lineId))
            {
                return null;
            }

            return whatsAppLines.FirstOrDefault(line =>
                string.Equals(line.Id, lineId, StringComparison.OrdinalIgnoreCase));
        }

        private void SetAutoFallbackEnabled(bool enabled)
        {
            autoFallbackEnabled = enabled;
            UpdateConfigurationSummary();
            UpdateActionButtons();
            UpdateMenuState();
        }

        private void RefreshWhatsAppLineSelector(string? selectedLineId = null)
        {
            string? selectedId = selectedLineId ?? selectedWhatsAppLineId ?? SelectedWhatsAppLine?.Id;
            selectedWhatsAppLineId = ResolveWhatsAppLineById(selectedId)?.Id
                ?? whatsAppLines.FirstOrDefault()?.Id;
            UpdateSelectedLineStatus();
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
            UpdateConfigurationSummary();
            UpdateActionButtons();
        }

        private void ReconcileSelectedLinesForRun(bool includePrimary)
        {
            var enabledLineIds = whatsAppLines
                .Where(line => line.Enabled)
                .Select(line => line.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            selectedLineIdsForRun.RemoveWhere(lineId => !enabledLineIds.Contains(lineId));

            var selectedLine = SelectedWhatsAppLine;
            if (includePrimary && selectedLine != null)
            {
                selectedLineIdsForRun.Add(selectedLine.Id);
            }

            UpdateRunLinesSummary();
        }

        private void UpdateRunLinesSummary()
        {
            UpdateConfigurationSummary();
            UpdateActionButtons();
        }

        private void LoadElevenLabsSettingsIntoUi()
        {
            try
            {
                bool loadedLocalSettings = ElevenLabsSettingsStore.TryLoad(out var settings);
                ApplyElevenLabsSettingsToUi(settings);

                if (settings.Validate().Count == 0)
                {
                    if (loadedLocalSettings)
                    {
                        MarkElevenLabsSavedLocally();
                    }
                    else
                    {
                        SetElevenLabsStatus(
                            "Configurado por entorno",
                            Color.FromArgb(21, 128, 61),
                            "Los envíos de audio usan variables de entorno porque no hay configuración local guardada."
                            + Environment.NewLine
                            + "Pulsa Guardar configuración o Probar configuración para guardar estos valores localmente."
                            + Environment.NewLine
                            + $"Ruta local: {ElevenLabsSettingsStore.GetConfigPath()}");
                    }
                }
            }
            catch (Exception ex)
            {
                ApplyElevenLabsSettingsToUi(ElevenLabsSettings.FromEnvironment());
                SetElevenLabsStatus(
                    "Error de validación",
                    Color.FromArgb(185, 28, 28),
                    ex.Message);
                Log($"No se pudo cargar la configuración local de ElevenLabs. Se usarán variables de entorno. Detalle: {ex.Message}");
            }
        }

        private void ApplyElevenLabsSettingsToUi(ElevenLabsSettings settings)
        {
            elevenLabsSettings = settings;
            UpdateElevenLabsStatus(settings);
        }

        private void SaveElevenLabsSettings(ElevenLabsSettings settings, string successLogMessage)
        {
            ElevenLabsSettingsStore.Save(settings);
            elevenLabsSettings = settings;
            MarkElevenLabsSavedLocally();
            Log(successLogMessage);
        }

        private void MarkElevenLabsSavedLocally()
        {
            SetElevenLabsStatus(
                "Configurado y guardado",
                Color.FromArgb(21, 128, 61),
                ElevenLabsSavedSettingsTooltip
                + Environment.NewLine
                + $"Ruta local: {ElevenLabsSettingsStore.GetConfigPath()}");
        }

        private void ReportElevenLabsSettingsSaveError(Exception ex)
        {
            SetElevenLabsStatus(
                "Error al guardar",
                Color.FromArgb(185, 28, 28),
                ex.Message);
            Log($"No se pudo guardar la configuración ElevenLabs: {ex.Message}");
            MessageBox.Show(
                "No se pudo guardar la configuración de ElevenLabs.",
                "ElevenLabs",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private ElevenLabsSettings BuildElevenLabsSettingsFromUi()
        {
            return elevenLabsSettings;
        }

        private static bool TryValidateElevenLabsSettings(
            ElevenLabsSettings settings,
            out IReadOnlyList<string> validationErrors)
        {
            validationErrors = settings.Validate();
            return validationErrors.Count == 0;
        }

        private ElevenLabsDialogState SaveElevenLabsSettingsFromConfigurationDialog(ElevenLabsSettings settings)
        {
            SaveElevenLabsSettingsFromInput(settings);
            return CreateElevenLabsDialogState(settings);
        }

        private ElevenLabsDialogState TestElevenLabsSettingsFromConfigurationDialog(ElevenLabsSettings settings)
        {
            TestElevenLabsSettingsFromInput(settings);
            return CreateElevenLabsDialogState(settings);
        }

        private void SaveElevenLabsSettingsFromInput(ElevenLabsSettings settings)
        {
            if (!TryValidateElevenLabsSettings(settings, out var validationErrors))
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

            TrySaveElevenLabsSettings(
                settings,
                "Configuración ElevenLabs guardada localmente. Los envíos de audio usarán estos valores.");
        }

        private void TestElevenLabsSettingsFromInput(ElevenLabsSettings settings)
        {
            if (!TryValidateElevenLabsSettings(settings, out var validationErrors))
            {
                UpdateElevenLabsStatus(settings, validationErrors);
                Log("Prueba ElevenLabs fallida: " + string.Join(" ", validationErrors));
                return;
            }

            TrySaveElevenLabsSettings(
                settings,
                "Prueba ElevenLabs correcta: configuración mínima presente y guardada localmente. No se llamó a la API.");
        }

        private void TrySaveElevenLabsSettings(ElevenLabsSettings settings, string successLogMessage)
        {
            try
            {
                SaveElevenLabsSettings(settings, successLogMessage);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException)
            {
                ReportElevenLabsSettingsSaveError(ex);
            }
        }

        private void UpdateElevenLabsStatus(
            ElevenLabsSettings settings,
            IReadOnlyList<string>? validationErrors = null)
        {
            validationErrors ??= settings.Validate();

            if (validationErrors.Count == 0)
            {
                SetElevenLabsStatus(
                    "Configurado",
                    Color.FromArgb(21, 128, 61),
                    "Configuración ElevenLabs válida. Pulsa Guardar configuración o Probar configuración para guardarla localmente antes de enviar audios."
                    + Environment.NewLine
                    + $"Ruta local: {ElevenLabsSettingsStore.GetConfigPath()}");
            }
            else if (string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.VoiceId))
            {
                SetElevenLabsStatus(
                    "No configurado",
                    Color.FromArgb(180, 83, 9),
                    string.Join(Environment.NewLine, validationErrors));
            }
            else
            {
                SetElevenLabsStatus(
                    "Error de validación",
                    Color.FromArgb(185, 28, 28),
                    string.Join(Environment.NewLine, validationErrors));
            }
        }

        private void SetElevenLabsStatus(string statusText, Color foreColor, string toolTipText)
        {
            elevenLabsStatusText = statusText;
            elevenLabsStatusColor = foreColor;
            elevenLabsStatusToolTip = toolTipText;
            UpdateConfigurationSummary();
        }

        private ElevenLabsDialogState CreateElevenLabsDialogState(ElevenLabsSettings? settings = null)
        {
            return new ElevenLabsDialogState(
                settings ?? BuildElevenLabsSettingsFromUi(),
                elevenLabsStatusText,
                elevenLabsStatusColor,
                elevenLabsStatusToolTip);
        }

        private void UpdateConfigurationSummary()
        {
            if (lblConfigurationSummary == null)
            {
                return;
            }

            var configurationState = CreateConfigurationState();
            string selectedLineName = ResolveWhatsAppLineById(configurationState.SelectedWhatsAppLineId)?.DisplayName ?? "sin línea";
            string delayText = $"{configurationState.DelayBetweenMessagesMinutes} min";

            lblConfigurationSummary.Text =
                $"Configuración: {selectedLineName} · Espera: {delayText} · ElevenLabs: {configurationState.ElevenLabsStatusText}";
        }

        private List<WhatsAppLine> GetSelectedLinesForRun()
            => GetSelectedLinesForRun(selectedLineIdsForRun);

        private List<WhatsAppLine> GetSelectedLinesForRun(IEnumerable<string> selectedLineIds)
        {
            var selectedIds = selectedLineIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

            return whatsAppLines
                .Where(line => line.Enabled && selectedIds.Contains(line.Id))
                .OrderBy(line => line.Priority)
                .ThenBy(line => line.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private bool OperationBlockingSetup => isScheduled || isSending || isPreparingLines;

        private bool CanEditConfiguration => !OperationBlockingSetup;

        private bool CanSelectRunLines => whatsAppLines.Count > 0 && !OperationBlockingSetup;

        private bool CanConfigureLines => !OperationBlockingSetup;

        private bool CanPrepareSelectedLine => SelectedWhatsAppLine != null && !OperationBlockingSetup;

        private bool CanPrepareAnyLine => whatsAppLines.Count > 0 && !OperationBlockingSetup;

        private AppConfigurationState CreateConfigurationState()
            => new(
                selectedWhatsAppLineId,
                autoFallbackEnabled,
                selectedLineIdsForRun.ToList(),
                appSettings.DelayBetweenMessagesMinutes,
                elevenLabsStatusText);

        private void ApplyConfigurationState(AppConfigurationState configurationState)
        {
            selectedWhatsAppLineId = configurationState.SelectedWhatsAppLineId;
            selectedLineIdsForRun = new HashSet<string>(
                configurationState.SelectedLineIdsForRun,
                StringComparer.OrdinalIgnoreCase);
            elevenLabsStatusText = configurationState.ElevenLabsStatusText;

            SetAutoFallbackEnabled(configurationState.AutoFallbackEnabled);
            RefreshWhatsAppLineSelector(selectedWhatsAppLineId);
            sendOrchestrator.SetSelectedLine(SelectedWhatsAppLine);
            ReconcileSelectedLinesForRun(includePrimary: true);
            UpdateSelectedLineStatus();
            UpdateRunLinesSummary();
            UpdateActionButtons();
            UpdateMenuState();
        }

        private bool ConfigureLinesWithFeedback(IWin32Window owner, string? selectedLineId)
        {
            if (isScheduled || isSending || isPreparingLines)
            {
                MessageBox.Show(
                    "No se pueden modificar las líneas mientras hay una programación, envío o preparación en curso.",
                    "Configurar líneas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }

            using var settingsForm = new LineSettingsForm();

            if (settingsForm.ShowDialog(owner) != DialogResult.OK)
            {
                return false;
            }

            LoadWhatsAppLines(selectedLineId);
            return true;
        }

        private IReadOnlyList<WhatsAppLine>? ConfigureLinesFromConfigurationDialog(
            IWin32Window owner,
            string? dialogSelectedLineId)
        {
            string? currentSelectedLineId = selectedWhatsAppLineId;
            var currentSelectedRunLineIds = selectedLineIdsForRun.ToList();

            if (!ConfigureLinesWithFeedback(owner, currentSelectedLineId))
            {
                return null;
            }

            selectedLineIdsForRun = new HashSet<string>(currentSelectedRunLineIds, StringComparer.OrdinalIgnoreCase);
            ReconcileSelectedLinesForRun(includePrimary: true);
            return whatsAppLines;
        }

        private IReadOnlyList<WhatsAppLine> PrepareLineFromConfigurationDialog(string selectedLineId)
        {
            string? currentSelectedLineId = selectedWhatsAppLineId;
            var currentSelectedRunLineIds = selectedLineIdsForRun.ToList();
            var selectedLine = ResolveWhatsAppLineById(selectedLineId);
            if (selectedLine == null)
            {
                MessageBox.Show("Selecciona una línea para preparar.", "Preparar línea", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return whatsAppLines;
            }

            PrepareSingleLineWithFeedback(selectedLine);
            RestoreConfigurationSelection(currentSelectedLineId, currentSelectedRunLineIds);
            return whatsAppLines;
        }

        private IReadOnlyList<WhatsAppLine> PrepareAllLinesFromConfigurationDialog()
        {
            string? currentSelectedLineId = selectedWhatsAppLineId;
            var currentSelectedRunLineIds = selectedLineIdsForRun.ToList();
            PrepareAllEnabledLinesWithFeedback();
            RestoreConfigurationSelection(currentSelectedLineId, currentSelectedRunLineIds);
            return whatsAppLines;
        }

        private void RestoreConfigurationSelection(string? selectedLineId, IReadOnlyCollection<string> runLineIds)
        {
            selectedWhatsAppLineId = selectedLineId;
            selectedLineIdsForRun = new HashSet<string>(runLineIds, StringComparer.OrdinalIgnoreCase);
            RefreshWhatsAppLineSelector(selectedWhatsAppLineId);
            sendOrchestrator.SetSelectedLine(SelectedWhatsAppLine);
            ReconcileSelectedLinesForRun(includePrimary: true);
            UpdateSelectedLineStatus();
            UpdateRunLinesSummary();
            UpdateMenuState();
        }

        private void InitializeSendOrchestrator()
        {
            sendOrchestrator = new WhatsAppSendOrchestrator(whatsAppLines);
            sendOrchestrator.Log += message => Ui(() => Log(message));
            sendOrchestrator.MessageResult += result => Ui(() => RegisterSendMessageResult(result));
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

        private void RegisterSendMessageResult(WhatsAppMessageResult result)
        {
            currentSendResults.Add(result);
            UpdateMenuState();
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
                    btnPauseResume.Text = "Pausar";
                    SetGeneralStatus(StatusCancelled);
                    break;
                case WhatsAppSendOrchestrationState.Finished:
                    isSending = false;
                    isScheduled = false;
                    isPaused = false;
                    cancellationRequested = false;
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

            if (matchingLine == null)
            {
                return;
            }

            selectedWhatsAppLineId = matchingLine.Id;

            UpdateSelectedLineStatus();
            UpdateMenuState();
        }

        private void PrepareSingleLineWithFeedback(WhatsAppLine selectedLine)
        {
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

        private void PrepareAllEnabledLinesWithFeedback()
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

        private void exportReportToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            ExportSendReport();
        }

        private void ExportSendReport()
        {
            if (currentSendResults.Count == 0)
            {
                MessageBox.Show(
                    "No hay resultados para exportar.",
                    "Exportar reporte",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var saveFileDialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "xlsx",
                FileName = $"reporte_envio_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Filter = "Libro de Excel (*.xlsx)|*.xlsx",
                OverwritePrompt = true,
                Title = "Guardar reporte de envío"
            };

            if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                SendReportExporter.ExportToExcel(saveFileDialog.FileName, currentSendResults.ToList());
                Log($"Reporte de envío exportado: {saveFileDialog.FileName}");
                MessageBox.Show(
                    "El reporte de envío se exportó correctamente.",
                    "Exportar reporte",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log($"❌ No se pudo exportar el reporte de envío: {ex.Message}");
                MessageBox.Show(
                    "No se pudo exportar el reporte de envío.",
                    "Exportar reporte",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async Task SetSelectedExcelFileAsync(string selectedPath)
        {
            ResetDryRunApproval();
            excelPath = selectedPath;
            lblFilePath.Text = $"Archivo: {Path.GetFileName(excelPath)}";
            uiToolTip.SetToolTip(lblFilePath, excelPath);
            await LoadExcelPreviewAsync(selectedPath, ++excelPreviewLoadVersion);
        }

        private async Task LoadExcelPreviewAsync(string selectedPath, int loadVersion)
        {
            isLoadingExcelPreview = true;
            excelPreviewRows = new List<ExcelMessagePreviewRow>();
            excelConsentWarning = "";
            ClearExcelPreviewGrid();
            lblPreviewSummary.Text = "Total filas: 0 | Enviables: 0 | Bloqueados DNC: 0 | Sin opt-in: 0 | Inválidas: 0 | Audios: 0";
            lblPreviewStatus.Text = "Analizando archivo Excel...";
            lblPreviewStatus.ForeColor = Color.FromArgb(75, 85, 99);
            lblExcelCompactSummary.Text = $"{Path.GetFileName(selectedPath)} - analizando Excel...";
            lblExcelCompactSummary.ForeColor = Color.FromArgb(75, 85, 99);
            SetSendFeedback(StatusNoFile, 0, 0, 0, 0, 0);
            UpdateActionButtons();

            try
            {
                var doNotContactEntries = await Task.Run(LoadDoNotContactEntriesForValidation);
                var previewResult = await Task.Run(() => ExcelReader.ReadPreviewResult(selectedPath, doNotContactEntries));
                if (loadVersion != excelPreviewLoadVersion)
                {
                    return;
                }

                excelPreviewRows = previewResult.Rows;
                excelConsentWarning = previewResult.ConsentWarning;
                BindExcelPreview();
                Log("Archivo Excel seleccionado correctamente.");
                Log($"Excel analizado: {excelPreviewRows.Count} filas.");
                Log($"Lista local de no contactar cargada: {doNotContactEntries.Count} registro(s).");
                if (!string.IsNullOrWhiteSpace(excelConsentWarning))
                {
                    Log(excelConsentWarning);
                }
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
                var doNotContactEntries = LoadDoNotContactEntriesForValidation();
                var previewResult = ExcelReader.ReadPreviewResult(excelPath, doNotContactEntries);
                excelPreviewRows = previewResult.Rows;
                excelConsentWarning = previewResult.ConsentWarning;
                BindExcelPreview();
                Log("Archivo Excel seleccionado correctamente.");
                Log($"Excel analizado: {excelPreviewRows.Count} filas.");
                Log($"Lista local de no contactar cargada: {doNotContactEntries.Count} registro(s).");
                if (!string.IsNullOrWhiteSpace(excelConsentWarning))
                {
                    Log(excelConsentWarning);
                }
            }
            catch (Exception ex)
            {
                ClearSelectedExcelAfterReadError(ex);
            }
        }

        private static IReadOnlyList<DoNotContactEntry> LoadDoNotContactEntriesForValidation()
        {
            string configPath = DoNotContactStore.GetConfigPath();
            if (DoNotContactStore.TryLoad(out var entries, configPath))
            {
                return entries;
            }

            if (File.Exists(configPath))
            {
                throw new InvalidOperationException(
                    $"No se pudo cargar la lista local de no contactar en {configPath}. Corrige el archivo JSON antes de enviar.");
            }

            return Array.Empty<DoNotContactEntry>();
        }

        private void ClearSelectedExcelAfterReadError(Exception ex)
        {
            ResetDryRunApproval();
            excelPath = "";
            excelPreviewRows = new List<ExcelMessagePreviewRow>();
            excelConsentWarning = "";
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
            int sendableRows = SendablePreviewRows;
            int blockedRows = PreviewRowsBlockedByDoNotContact;
            int withoutOptInRows = PreviewRowsWithoutOptIn;
            int invalidRows = InvalidPreviewRows;
            int unsendableRows = UnsendablePreviewRows;
            int audioRows = AudioPreviewRows;

            string totalsText = $"Total filas: {totalRows} | Enviables: {sendableRows} | Bloqueados DNC: {blockedRows} | Sin opt-in: {withoutOptInRows} | Inválidas: {invalidRows} | Audios: {audioRows}";
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
                lblExcelCompactSummary.ForeColor = sendableRows == 0
                    ? Color.FromArgb(185, 28, 28)
                    : unsendableRows > 0 || !string.IsNullOrWhiteSpace(excelConsentWarning)
                        ? Color.FromArgb(180, 83, 9)
                        : Color.FromArgb(21, 128, 61);
            }

            if (!isScheduled && !isSending)
            {
                string status = string.IsNullOrWhiteSpace(excelPath)
                    ? StatusNoFile
                    : sendableRows > 0
                        ? HasApprovedDryRun
                            ? StatusReady
                            : StatusDryRunRequired
                        : StatusReviewRequired;
                SetSendFeedback(status, 0, sendableRows, 0, 0, unsendableRows);
            }

            UpdateActionButtons();

            if (string.IsNullOrWhiteSpace(excelPath))
            {
                return;
            }

            if (totalRows == 0)
            {
                lblPreviewStatus.Text = AppendConsentWarning("El Excel no tiene filas de datos para enviar.");
                lblPreviewStatus.ForeColor = Color.FromArgb(185, 28, 28);
                return;
            }

            if (sendableRows == 0)
            {
                lblPreviewStatus.Text = AppendConsentWarning(
                    blockedRows > 0 && withoutOptInRows == 0 && invalidRows == 0
                        ? "Todas las filas están bloqueadas por la lista no contactar. No se enviará ningún mensaje."
                    : withoutOptInRows > 0 && blockedRows == 0 && invalidRows == 0
                        ? "Todas las filas carecen de opt-in explícito. No se enviará ningún mensaje hasta registrar el consentimiento."
                        : "No hay filas enviables. Revisa bloqueos por no contactar, consentimiento y datos del Excel.");
                lblPreviewStatus.ForeColor = Color.FromArgb(185, 28, 28);
                return;
            }

            if (unsendableRows > 0 || !string.IsNullOrWhiteSpace(excelConsentWarning))
            {
                lblPreviewStatus.Text = AppendConsentWarning(
                    HasApprovedDryRun
                        ? $"Dry-run aprobado. Hay {sendableRows} fila(s) enviable(s), {blockedRows} bloqueada(s) por no contactar, {withoutOptInRows} sin opt-in y {invalidRows} inválida(s). Solo se enviarán las enviables."
                        : $"Hay {sendableRows} fila(s) enviable(s), {blockedRows} bloqueada(s) por no contactar, {withoutOptInRows} sin opt-in y {invalidRows} inválida(s). Ejecuta el dry-run para revisar riesgos y habilitar el envío real.");
                lblPreviewStatus.ForeColor = Color.FromArgb(180, 83, 9);
                return;
            }

            if (!HasApprovedDryRun)
            {
                lblPreviewStatus.Text = "Todas las filas revisadas son enviables. Ejecuta el dry-run para revisar riesgos operativos y habilitar el envío real.";
                lblPreviewStatus.ForeColor = Color.FromArgb(37, 99, 235);
                return;
            }

            string approvedAtText = approvedDryRunAt.HasValue
                ? approvedDryRunAt.Value.ToString("HH:mm")
                : "hace unos instantes";
            lblPreviewStatus.Text = $"Dry-run aprobado a las {approvedAtText}. Puedes programar o enviar únicamente las filas enviables.";
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

                if (previewRow.HasInvalidData)
                {
                    gridRow.DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242);
                    gridRow.DefaultCellStyle.ForeColor = Color.FromArgb(127, 29, 29);
                    gridRow.DefaultCellStyle.SelectionBackColor = Color.FromArgb(252, 165, 165);
                    gridRow.DefaultCellStyle.SelectionForeColor = Color.FromArgb(69, 10, 10);
                }
                else if (previewRow.IsBlockedByDoNotContact)
                {
                    gridRow.DefaultCellStyle.BackColor = Color.FromArgb(243, 244, 246);
                    gridRow.DefaultCellStyle.ForeColor = Color.FromArgb(55, 65, 81);
                    gridRow.DefaultCellStyle.SelectionBackColor = Color.FromArgb(209, 213, 219);
                    gridRow.DefaultCellStyle.SelectionForeColor = Color.FromArgb(31, 41, 55);
                }
                else if (previewRow.LacksExplicitOptIn)
                {
                    gridRow.DefaultCellStyle.BackColor = Color.FromArgb(255, 251, 235);
                    gridRow.DefaultCellStyle.ForeColor = Color.FromArgb(146, 64, 14);
                    gridRow.DefaultCellStyle.SelectionBackColor = Color.FromArgb(253, 230, 138);
                    gridRow.DefaultCellStyle.SelectionForeColor = Color.FromArgb(120, 53, 15);
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

        private List<OutboundMessage> GetSendablePreviewMessages()
        {
            return excelPreviewRows
                .Where(row => row.IsSendable)
                .Select(row => new OutboundMessage(
                    row.CountryCode,
                    row.Phone,
                    row.Message,
                    row.ToAudio,
                    true,
                    row.OptInSource,
                    row.OptInAt,
                    row.DebtorName,
                    row.TemplateNumber,
                    row.Bank))
                .ToList();
        }

        private int ValidPreviewRows => SendablePreviewRows;

        private int SendablePreviewRows => excelPreviewRows.Count(row => row.IsSendable);

        private int PreviewRowsBlockedByDoNotContact => excelPreviewRows.Count(row => row.IsBlockedByDoNotContact);

        private int PreviewRowsWithoutOptIn => excelPreviewRows.Count(row => row.LacksExplicitOptIn);

        private int InvalidPreviewRows => excelPreviewRows.Count(row => row.HasInvalidData);

        private int UnsendablePreviewRows => excelPreviewRows.Count - SendablePreviewRows;

        private int AudioPreviewRows => excelPreviewRows.Count(row => row.IsSendable && row.ToAudio);

        private bool HasApprovedDryRun =>
            !string.IsNullOrWhiteSpace(approvedDryRunToken)
            && string.Equals(approvedDryRunToken, BuildDryRunToken(), StringComparison.Ordinal);

        private string AppendConsentWarning(string baseText)
        {
            return string.IsNullOrWhiteSpace(excelConsentWarning)
                ? baseText
                : $"{baseText} {excelConsentWarning}";
        }

        private string BuildDryRunToken()
        {
            string selectedRunIds = string.Join(
                ",",
                selectedLineIdsForRun
                    .OrderBy(lineId => lineId, StringComparer.OrdinalIgnoreCase));

            return string.Join(
                "|",
                excelPath.Trim(),
                excelPreviewRows.Count,
                SendablePreviewRows,
                PreviewRowsBlockedByDoNotContact,
                PreviewRowsWithoutOptIn,
                InvalidPreviewRows,
                AudioPreviewRows,
                selectedWhatsAppLineId ?? "",
                (int)(SelectedWhatsAppLine?.OperationalState ?? WhatsAppLineOperationalState.Unknown),
                autoFallbackEnabled ? "1" : "0",
                selectedRunIds,
                GetSelectedLinesForRun().Count);
        }

        private void ResetDryRunApproval()
        {
            approvedDryRunToken = "";
            approvedDryRunAt = null;
        }

        private static string BuildDryRunSummary(SendDryRunReport report)
        {
            var summaryLines = new List<string>
            {
                "Dry-run completado. Esto no enviará mensajes.",
                "",
                "Resumen:",
                $"- Total filas: {report.TotalRowCount}",
                $"- Enviables: {report.SendableRowCount}",
                $"- Bloqueadas por no contactar: {report.BlockedByDoNotContactCount}",
                $"- Sin opt-in: {report.RowsWithoutOptInCount}",
                $"- Inválidas: {report.InvalidRowCount}",
                $"- Audios: {report.AudioRowCount}",
                $"- Línea principal: {report.SelectedLineName} ({report.SelectedLineStateText})",
                $"- Líneas seleccionadas: {report.EnabledSelectedRunLineCount}",
                $"- Cambio automático: {(report.AutoFallbackEnabled ? "Activado" : "Desactivado")}"
            };

            if (report.RiskMessages.Count == 0)
            {
                summaryLines.Add("");
                summaryLines.Add("Sin riesgos bloqueantes detectados.");
            }
            else
            {
                summaryLines.Add("");
                summaryLines.Add("Riesgos y exclusiones:");
                foreach (string riskMessage in report.RiskMessages)
                {
                    summaryLines.Add($"- {riskMessage}");
                }
            }

            return string.Join(Environment.NewLine, summaryLines);
        }

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
                StatusDryRunRequired => Color.FromArgb(37, 99, 235),
                StatusReviewRequired => Color.FromArgb(180, 83, 9),
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
            bool hasApprovedDryRun = HasApprovedDryRun;
            bool canRequestSend = SendActionPolicy.CanRequestSend(
                new SendActionButtonState(isSending, isScheduled, isPreparingLines, hasApprovedDryRun));
            bool canRunDryRun = !OperationBlockingSetup
                && !isLoadingExcelPreview
                && !string.IsNullOrWhiteSpace(excelPath);

            btnPauseResume.Enabled = operationActive;
            btnCancel.Enabled = operationActive;
            btnSend.Enabled = canRequestSend;
            btnSendNow.Enabled = canRequestSend;
            btnDryRun.Enabled = canRunDryRun;
            btnDryRun.Text = hasApprovedDryRun ? "Revisar dry-run" : "Ejecutar dry-run";
            uiToolTip.SetToolTip(
                btnDryRun,
                hasApprovedDryRun
                    ? "Vuelve a revisar el reporte local antes de enviar si quieres confirmar el estado actual."
                    : "Genera un reporte local sin enviar mensajes y habilita el envío real solo después de confirmarlo.");
            btnChangeScheduleTime.Enabled = !OperationBlockingSetup;
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

        private void btnDryRun_Click(object? sender, EventArgs e)
        {
            RunDryRunReview();
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

        private void RunDryRunReview()
        {
            bool hadExcelBeforeLoad = !string.IsNullOrEmpty(excelPath);
            if (hadExcelBeforeLoad && !isLoadingExcelPreview && excelPreviewRows.Count == 0)
            {
                LoadExcelPreview();
            }

            var dryRunReport = SendActionPolicy.CreateDryRunReport(
                CreateSendActionPolicyInput(
                    SendActionKind.DryRun,
                    "No se completó el dry-run"));

            string reviewSummary = BuildDryRunSummary(dryRunReport);
            Log("Dry-run ejecutado. No se enviaron mensajes.");
            Log(reviewSummary.Replace(Environment.NewLine, " "));

            if (!dryRunReport.CanApproveForSend)
            {
                ResetDryRunApproval();
                UpdateExcelPreviewSummary();
                UpdateActionButtons();
                MessageBox.Show(
                    reviewSummary + Environment.NewLine + Environment.NewLine
                    + "Corrige los puntos marcados y vuelve a ejecutar el dry-run antes de enviar.",
                    "Dry-run",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var confirmationResult = MessageBox.Show(
                reviewSummary + Environment.NewLine + Environment.NewLine
                + $"¿Confirmas que revisaste este dry-run y quieres habilitar el envío real únicamente para {dryRunReport.SendableRowCount} fila(s) enviable(s)?",
                "Confirmar dry-run",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (confirmationResult != DialogResult.Yes)
            {
                ResetDryRunApproval();
                UpdateExcelPreviewSummary();
                UpdateActionButtons();
                Log("Dry-run revisado sin aprobación explícita. El envío real permanece deshabilitado.");
                return;
            }

            approvedDryRunToken = BuildDryRunToken();
            approvedDryRunAt = DateTime.Now;
            UpdateExcelPreviewSummary();
            UpdateActionButtons();
            Log($"Dry-run aprobado. Envío real habilitado para {dryRunReport.SendableRowCount} fila(s) enviable(s).");
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

            int unsendableRows = UnsendablePreviewRows;
            isScheduled = true;
            isPaused = false;
            isSending = false;
            cancellationRequested = false;
            btnPauseResume.Text = "Pausar";
            SetSendFeedback(StatusScheduled, 0, validMessages.Count, 0, 0, unsendableRows);
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
            SetSendFeedback(StatusSending, 0, mensajes.Count, 0, 0, UnsendablePreviewRows);
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
            validMessages = GetSendablePreviewMessages();
            int unsendableRows = UnsendablePreviewRows;
            if (unsendableRows > 0 && confirmInvalidRows && !HasApprovedDryRun)
            {
                var result = MessageBox.Show(
                    $"El Excel tiene {unsendableRows} fila(s) no enviable(s): {PreviewRowsBlockedByDoNotContact} bloqueada(s) por no contactar, {PreviewRowsWithoutOptIn} sin opt-in y {InvalidPreviewRows} inválida(s). No se enviarán.\n\n¿Quieres continuar únicamente con las {validMessages.Count} fila(s) enviable(s)?",
                    "Filas no enviables detectadas",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                {
                    Log($"{failureContext}: el usuario canceló al detectar {unsendableRows} fila(s) no enviable(s).");
                    return false;
                }
            }

            return true;
        }

        private void ShowFallbackWarningIfNeeded(bool showFallbackWarning)
        {
            int enabledSelectedCount = GetSelectedLinesForRun().Count;
            if (showFallbackWarning && autoFallbackEnabled && enabledSelectedCount == 1)
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
                TotalRowCount = excelPreviewRows.Count,
                ValidRowCount = ValidPreviewRows,
                BlockedByDoNotContactCount = PreviewRowsBlockedByDoNotContact,
                InvalidRowCount = InvalidPreviewRows,
                RowsWithoutOptInCount = PreviewRowsWithoutOptIn,
                AudioRowCount = AudioPreviewRows,
                AvailableLineCount = whatsAppLines.Count,
                HasSelectedLine = selectedLine != null,
                SelectedRunLineCount = selectedLineIdsForRun.Count,
                EnabledSelectedRunLineCount = GetSelectedLinesForRun().Count,
                SelectedLineState = selectedLine?.OperationalState ?? WhatsAppLineOperationalState.Unknown,
                SelectedLineName = selectedLine?.DisplayName ?? "",
                AutoFallbackEnabled = autoFallbackEnabled,
                HasApprovedDryRun = HasApprovedDryRun,
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
            var selectedLine = SelectedWhatsAppLine;
            if (!autoFallbackEnabled || selectedLine == null)
            {
                return;
            }

            int readyAlternatives = whatsAppLines.Count(line =>
                line.Enabled
                && line.OperationalState == WhatsAppLineOperationalState.Ready
                && selectedLineIdsForRun.Contains(line.Id)
                && !string.Equals(line.Id, selectedLine.Id, StringComparison.OrdinalIgnoreCase));

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
            var configurationState = CreateConfigurationState();
            var selectedLine = ResolveWhatsAppLineById(configurationState.SelectedWhatsAppLineId);

            currentSendResults.Clear();
            UpdateMenuState();
            Log(FormatDelayBetweenMessagesLog(configurationState.DelayBetweenMessagesMinutes));

            var summary = await sendOrchestrator.SendAsync(
                mensajes,
                new WhatsAppSendOptions(
                    selectedLine,
                    configurationState.AutoFallbackEnabled,
                    UnsendablePreviewRows,
                    SelectedLineIdsForRun: configurationState.SelectedLineIdsForRun.ToList(),
                    DelayBetweenMessagesMinutes: configurationState.DelayBetweenMessagesMinutes));

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

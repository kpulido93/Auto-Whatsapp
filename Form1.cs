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

        private const string StatusNoFile = "Sin archivo";
        private const string StatusReady = "Listo";
        private const string StatusScheduled = "Programado";
        private const string StatusSending = "Enviando";
        private const string StatusPaused = "Pausado";
        private const string StatusCancelled = "Cancelado";
        private const string StatusFinished = "Finalizado";
        private const int ResponsiveLayoutPadding = 16;
        private const int ResponsiveLayoutMinimumWidth = 700;
        private const int ResponsiveLayoutMinimumHeight = 902;

        public Form1()
        {
            InitializeComponent();
            AdjustResponsiveLayout();
            InitializeScheduleControls();
            SetSendFeedback(StatusNoFile, 0, 0, 0, 0, 0);
            UpdateActionButtons();
            LoadWhatsAppLines();
            InitializeSendOrchestrator();
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
                return;
            }

            lblRunLinesSummary.Text = $"Líneas seleccionadas: {string.Join(", ", selectedLines.Select(line => line.DisplayName))}";
            lblRunLinesSummary.ForeColor = Color.FromArgb(75, 85, 99);
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

            mainLayout.Location = new Point(ResponsiveLayoutPadding, ResponsiveLayoutPadding);
            mainLayout.Size = new Size(
                Math.Max(ResponsiveLayoutMinimumWidth, availableWidth),
                Math.Max(ResponsiveLayoutMinimumHeight, availableHeight));
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
                    isScheduled = true;
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

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Archivos Excel|*.xlsx;*.xls";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                SetSelectedExcelFile(openFileDialog1.FileName);
            }
        }

        private void SetSelectedExcelFile(string selectedPath)
        {
            excelPath = selectedPath;
            lblFilePath.Text = $"Archivo: {Path.GetFileName(excelPath)}";
            uiToolTip.SetToolTip(lblFilePath, excelPath);
            LoadExcelPreview();
        }

        private void LoadExcelPreview()
        {
            try
            {
                excelPreviewRows = ExcelReader.ReadPreview(excelPath);
                BindExcelPreview();
                Log("Archivo Excel seleccionado correctamente.");
                Log($"Vista previa cargada: {excelPreviewRows.Count} filas.");
            }
            catch (Exception ex)
            {
                excelPath = "";
                excelPreviewRows = new List<ExcelMessagePreviewRow>();
                lblFilePath.Text = "Archivo seleccionado: ninguno";
                uiToolTip.SetToolTip(lblFilePath, lblFilePath.Text);
                BindExcelPreview();
                lblPreviewStatus.Text = $"No se pudo leer el Excel: {ex.Message}";
                lblPreviewStatus.ForeColor = Color.FromArgb(185, 28, 28);
                SetSendFeedback(StatusNoFile, 0, 0, 0, 0, 0);
                UpdateActionButtons();
                Log($"No se pudo leer el Excel seleccionado: {ex.Message}");
            }
        }

        private void BindExcelPreview()
        {
            dgvExcelPreview.DataSource = null;
            dgvExcelPreview.DataSource = excelPreviewRows;
            UpdateExcelPreviewSummary();
            ApplyExcelPreviewRowStyles();
        }

        private void UpdateExcelPreviewSummary()
        {
            int totalRows = excelPreviewRows.Count;
            int validRows = excelPreviewRows.Count(row => row.IsValid);
            int invalidRows = totalRows - validRows;
            int audioRows = excelPreviewRows.Count(row => row.IsValid && row.ToAudio);

            lblPreviewSummary.Text = $"Total filas: {totalRows} | Válidas: {validRows} | Inválidas: {invalidRows} | Audios: {audioRows}";

            if (!isScheduled && !isSending)
            {
                string status = validRows > 0 ? StatusReady : StatusNoFile;
                SetSendFeedback(status, 0, validRows, 0, 0, invalidRows);
            }

            UpdateActionButtons();

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
            bool selectedLineReady = SelectedWhatsAppLine?.OperationalState == WhatsAppLineOperationalState.Ready;

            btnPauseResume.Enabled = operationActive;
            btnCancel.Enabled = operationActive;
            btnPrepareLine.Enabled = SelectedWhatsAppLine != null && !operationBlockingSetup;
            btnPrepareAllLines.Enabled = whatsAppLines.Count > 0 && !operationBlockingSetup;
            btnSelectRunLines.Enabled = whatsAppLines.Count > 0 && !operationBlockingSetup;
            btnConfigureLines.Enabled = !operationBlockingSetup;
            btnSend.Enabled = ValidPreviewRows > 0 && selectedLineReady && !operationBlockingSetup;
            btnSendNow.Enabled = ValidPreviewRows > 0 && selectedLineReady && !operationBlockingSetup;
            btnChangeScheduleTime.Enabled = !operationBlockingSetup;
            cmbWhatsAppLine.Enabled = !operationBlockingSetup;
            chkAutoLineFallback.Enabled = !operationBlockingSetup;
        }

        private void dgvExcelPreview_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            ApplyExcelPreviewRowStyles();
            dgvExcelPreview.ClearSelection();
        }

        private void BtnSchedule_Click(object sender, EventArgs e)
        {
            scheduledTime = GetSelectedScheduleDateTime();
            schedulerTimer.Start();
            isScheduled = true;
            isPaused = false;
            cancellationRequested = false;
            btnPauseResume.Text = "Pausar";
            SetGeneralStatus(StatusScheduled);
            UpdateActionButtons();
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
            var target = SinSegundos(scheduledTime);

            if (isScheduled && !isPaused && DateTime.Now >= target)
            {
                schedulerTimer.Stop();

                isScheduled = true;

                if (excelPreviewRows.Count == 0 && !string.IsNullOrEmpty(excelPath))
                {
                    LoadExcelPreview();
                }

                var mensajes = GetValidPreviewMessages();
                if (mensajes.Count == 0)
                {
                    Log("No se encontraron mensajes válidos en el Excel.");
                    lblPreviewStatus.Text = "No hay filas válidas para enviar. Corrige el Excel antes de programar.";
                    lblPreviewStatus.ForeColor = Color.FromArgb(185, 28, 28);
                    isScheduled = false;
                    cmbWhatsAppLine.Enabled = true;
                    chkAutoLineFallback.Enabled = true;
                    SetSendFeedback(StatusNoFile, 0, 0, 0, 0, InvalidPreviewRows);
                    UpdateActionButtons();
                    return;
                }

                if (!ValidateSelectedLinesForRun(showFallbackWarning: false))
                {
                    isScheduled = false;
                    cmbWhatsAppLine.Enabled = true;
                    chkAutoLineFallback.Enabled = true;
                    SetGeneralStatus(StatusCancelled);
                    UpdateActionButtons();
                    return;
                }

                if (!ValidateSelectedLineReady())
                {
                    isScheduled = false;
                    cmbWhatsAppLine.Enabled = true;
                    chkAutoLineFallback.Enabled = true;
                    SetGeneralStatus(StatusCancelled);
                    UpdateActionButtons();
                    return;
                }

                await RunSendAsync(mensajes);
            }
        }


        private void btnSend_Click(object sender, EventArgs e)
        {
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
            if (isScheduled || isSending || isPreparingLines)
            {
                return;
            }

            if (!TryBuildMessagesForSend(out var mensajes, showFallbackWarning: true))
            {
                return;
            }

            schedulerTimer.Stop();
            isScheduled = false;
            isPaused = false;
            isSending = true;
            cancellationRequested = false;
            btnPauseResume.Text = "Pausar";
            SetSendFeedback(StatusSending, 0, mensajes.Count, 0, 0, InvalidPreviewRows);
            Log("Envío inmediato solicitado.");
            LogSelectedLinesForRun();
            LogReadyFallbackCandidates();
            UpdateActionButtons();

            await RunSendAsync(mensajes);
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

            if (scheduledTime <= DateTime.Now)
            {
                MessageBox.Show(
                    "Selecciona una fecha y hora futura para programar el envío.",
                    "Hora no válida",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            if (!TryBuildMessagesForSend(out var validMessages, showFallbackWarning: true))
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
            return true;
        }

        private bool TryBuildMessagesForSend(out List<OutboundMessage> validMessages, bool showFallbackWarning)
        {
            validMessages = new List<OutboundMessage>();

            if (string.IsNullOrEmpty(excelPath))
            {
                MessageBox.Show("Seleccione un archivo Excel primero.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (excelPreviewRows.Count == 0)
            {
                LoadExcelPreview();
            }

            validMessages = GetValidPreviewMessages();
            if (validMessages.Count == 0)
            {
                MessageBox.Show(
                    "No hay filas válidas para enviar. Corrige teléfono y mensaje en el Excel.",
                    "Excel sin filas válidas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            if (!ValidateSelectedLinesForRun(showFallbackWarning))
            {
                return false;
            }

            if (!ValidateSelectedLineReady())
            {
                return false;
            }

            int invalidRows = excelPreviewRows.Count(row => !row.IsValid);
            if (invalidRows > 0)
            {
                var result = MessageBox.Show(
                    $"El Excel tiene {invalidRows} fila(s) inválida(s). No se enviarán.\n\n¿Quieres continuar únicamente con las {validMessages.Count} fila(s) válida(s)?",
                    "Filas inválidas detectadas",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ValidateSelectedLinesForRun(bool showFallbackWarning)
        {
            if (selectedLineIdsForRun.Count == 0)
            {
                MessageBox.Show(
                    "Selecciona al menos una línea para esta corrida antes de programar el envío.",
                    "Líneas de corrida requeridas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Log("No se programó el envío porque no hay líneas seleccionadas para esta corrida.");
                return false;
            }

            var selectedLine = SelectedWhatsAppLine;
            if (selectedLine == null)
            {
                MessageBox.Show("Selecciona una línea de WhatsApp antes de programar.", "Línea requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!selectedLineIdsForRun.Contains(selectedLine.Id))
            {
                selectedLineIdsForRun.Add(selectedLine.Id);
                UpdateRunLinesSummary();
                Log($"La línea principal {selectedLine.DisplayName} se agregó automáticamente a la selección de esta corrida.");
            }

            int enabledSelectedCount = GetSelectedLinesForRun().Count;
            if (enabledSelectedCount == 0)
            {
                MessageBox.Show(
                    "Las líneas seleccionadas ya no están habilitadas. Selecciona al menos una línea habilitada para esta corrida.",
                    "Líneas no disponibles",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Log("No se programó el envío porque las líneas seleccionadas no están habilitadas.");
                return false;
            }

            if (showFallbackWarning && chkAutoLineFallback.Checked && enabledSelectedCount == 1)
            {
                MessageBox.Show(
                    "El cambio automático está activado, pero solo hay una línea seleccionada para esta corrida. El envío continuará sin alternativas disponibles.",
                    "Sin líneas alternativas seleccionadas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                Log("Fallback automático activado con una sola línea seleccionada; no habrá alternativas para esta corrida.");
            }

            return true;
        }

        private bool ValidateSelectedLineReady()
        {
            var selectedLine = SelectedWhatsAppLine;
            if (selectedLine == null)
            {
                MessageBox.Show("Selecciona una línea de WhatsApp antes de programar.", "Línea requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (selectedLine.OperationalState == WhatsAppLineOperationalState.Ready)
            {
                return true;
            }

            MessageBox.Show(
                $"Prepara la línea {selectedLine.DisplayName} antes de programar el envío.\n\nEstado actual: {selectedLine.OperationalStateText}.",
                "Línea no preparada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            Log($"No se programó el envío porque la línea {selectedLine.DisplayName} no está lista. Estado: {selectedLine.OperationalStateText}.");
            return false;
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

        private async Task RunSendAsync(List<OutboundMessage> mensajes)
        {
            var summary = await sendOrchestrator.SendAsync(
                mensajes,
                new WhatsAppSendOptions(
                    SelectedWhatsAppLine,
                    chkAutoLineFallback.Checked,
                    InvalidPreviewRows,
                    selectedLineIdsForRun.ToList()));

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


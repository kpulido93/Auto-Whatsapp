namespace Automate_Whatsapp
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén utilizando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben eliminar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            mainScrollPanel = new Panel();
            mainLayout = new TableLayoutPanel();
            grpExcelFile = new GroupBox();
            fileLayout = new TableLayoutPanel();
            btnSelectFile = new Button();
            lblFilePath = new Label();
            uiToolTip = new ToolTip(components);
            grpExcelPreview = new GroupBox();
            previewLayout = new TableLayoutPanel();
            lblPreviewSummary = new Label();
            dgvExcelPreview = new DataGridView();
            colCountryCode = new DataGridViewTextBoxColumn();
            colPhone = new DataGridViewTextBoxColumn();
            colMessage = new DataGridViewTextBoxColumn();
            colToAudio = new DataGridViewCheckBoxColumn();
            colValidationStatus = new DataGridViewTextBoxColumn();
            lblPreviewStatus = new Label();
            grpSchedule = new GroupBox();
            scheduleLayout = new TableLayoutPanel();
            lblScheduleTime = new Label();
            schedulePickerLayout = new TableLayoutPanel();
            lblSelectedScheduleTime = new Label();
            btnChangeScheduleTime = new Button();
            lblScheduleSummary = new Label();
            lblWhatsAppLine = new Label();
            lineOptionsLayout = new FlowLayoutPanel();
            cmbWhatsAppLine = new ComboBox();
            chkAutoLineFallback = new CheckBox();
            lblRunLines = new Label();
            runLinesLayout = new FlowLayoutPanel();
            btnSelectRunLines = new Button();
            lblRunLinesSummary = new Label();
            lblLinePreparation = new Label();
            linePreparationLayout = new FlowLayoutPanel();
            btnPrepareLine = new Button();
            btnPrepareAllLines = new Button();
            lblLinePreparationStatus = new Label();
            scheduleActionsLayout = new FlowLayoutPanel();
            btnSend = new Button();
            btnSendNow = new Button();
            btnConfigureLines = new Button();
            grpSendStatus = new GroupBox();
            statusLayout = new TableLayoutPanel();
            lblGeneralStatus = new Label();
            lblProgress = new Label();
            lblProgressText = new Label();
            lblSendCounters = new Label();
            progressBar1 = new ProgressBar();
            statusActionsLayout = new FlowLayoutPanel();
            btnCancel = new Button();
            btnPauseResume = new Button();
            grpActivity = new GroupBox();
            logLayout = new TableLayoutPanel();
            txtLog = new TextBox();
            openFileDialog1 = new OpenFileDialog();
            schedulerTimer = new System.Windows.Forms.Timer(components);
            mainScrollPanel.SuspendLayout();
            mainLayout.SuspendLayout();
            grpExcelFile.SuspendLayout();
            fileLayout.SuspendLayout();
            grpExcelPreview.SuspendLayout();
            previewLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvExcelPreview).BeginInit();
            grpSchedule.SuspendLayout();
            scheduleLayout.SuspendLayout();
            schedulePickerLayout.SuspendLayout();
            lineOptionsLayout.SuspendLayout();
            runLinesLayout.SuspendLayout();
            linePreparationLayout.SuspendLayout();
            scheduleActionsLayout.SuspendLayout();
            grpSendStatus.SuspendLayout();
            statusLayout.SuspendLayout();
            statusActionsLayout.SuspendLayout();
            grpActivity.SuspendLayout();
            logLayout.SuspendLayout();
            SuspendLayout();
            //
            // mainScrollPanel
            //
            mainScrollPanel.AutoScroll = true;
            mainScrollPanel.Controls.Add(mainLayout);
            mainScrollPanel.Dock = DockStyle.Fill;
            mainScrollPanel.Location = new Point(0, 0);
            mainScrollPanel.Name = "mainScrollPanel";
            mainScrollPanel.Size = new Size(920, 880);
            mainScrollPanel.TabIndex = 0;
            mainScrollPanel.Resize += mainScrollPanel_Resize;
            //
            // mainLayout
            //
            mainLayout.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            mainLayout.ColumnCount = 1;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.Controls.Add(grpExcelFile, 0, 0);
            mainLayout.Controls.Add(grpExcelPreview, 0, 1);
            mainLayout.Controls.Add(grpSchedule, 0, 2);
            mainLayout.Controls.Add(grpSendStatus, 0, 3);
            mainLayout.Controls.Add(grpActivity, 0, 4);
            mainLayout.Location = new Point(16, 16);
            mainLayout.Name = "mainLayout";
            mainLayout.RowCount = 5;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 210F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 286F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 152F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.Size = new Size(869, 902);
            mainLayout.TabIndex = 0;
            //
            // grpExcelFile
            //
            grpExcelFile.Controls.Add(fileLayout);
            grpExcelFile.Dock = DockStyle.Fill;
            grpExcelFile.Location = new Point(0, 0);
            grpExcelFile.Margin = new Padding(0, 0, 0, 12);
            grpExcelFile.Name = "grpExcelFile";
            grpExcelFile.Padding = new Padding(12, 10, 12, 12);
            grpExcelFile.Size = new Size(869, 84);
            grpExcelFile.TabIndex = 0;
            grpExcelFile.TabStop = false;
            grpExcelFile.Text = "1. Archivo Excel";
            //
            // fileLayout
            //
            fileLayout.ColumnCount = 2;
            fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230F));
            fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            fileLayout.Controls.Add(btnSelectFile, 0, 0);
            fileLayout.Controls.Add(lblFilePath, 1, 0);
            fileLayout.Dock = DockStyle.Fill;
            fileLayout.Location = new Point(12, 26);
            fileLayout.Name = "fileLayout";
            fileLayout.RowCount = 1;
            fileLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            fileLayout.Size = new Size(845, 46);
            fileLayout.TabIndex = 0;
            //
            // btnSelectFile
            //
            btnSelectFile.Anchor = AnchorStyles.Left;
            btnSelectFile.Location = new Point(3, 5);
            btnSelectFile.Name = "btnSelectFile";
            btnSelectFile.Size = new Size(210, 36);
            btnSelectFile.TabIndex = 0;
            btnSelectFile.Text = "Seleccionar archivo Excel";
            btnSelectFile.UseVisualStyleBackColor = true;
            btnSelectFile.Click += btnSelectFile_Click;
            //
            // lblFilePath
            //
            lblFilePath.AutoEllipsis = true;
            lblFilePath.Dock = DockStyle.Fill;
            lblFilePath.Location = new Point(233, 0);
            lblFilePath.Name = "lblFilePath";
            lblFilePath.Padding = new Padding(8, 0, 0, 0);
            lblFilePath.Size = new Size(628, 46);
            lblFilePath.TabIndex = 1;
            lblFilePath.Text = "Archivo seleccionado: ninguno";
            lblFilePath.TextAlign = ContentAlignment.MiddleLeft;
            uiToolTip.SetToolTip(lblFilePath, "Archivo seleccionado: ninguno");
            //
            // grpExcelPreview
            //
            grpExcelPreview.Controls.Add(previewLayout);
            grpExcelPreview.Dock = DockStyle.Fill;
            grpExcelPreview.Location = new Point(0, 96);
            grpExcelPreview.Margin = new Padding(0, 0, 0, 12);
            grpExcelPreview.Name = "grpExcelPreview";
            grpExcelPreview.Padding = new Padding(12, 10, 12, 12);
            grpExcelPreview.Size = new Size(869, 224);
            grpExcelPreview.TabIndex = 1;
            grpExcelPreview.TabStop = false;
            grpExcelPreview.Text = "2. Vista previa del Excel";
            //
            // previewLayout
            //
            previewLayout.ColumnCount = 1;
            previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            previewLayout.Controls.Add(lblPreviewSummary, 0, 0);
            previewLayout.Controls.Add(dgvExcelPreview, 0, 1);
            previewLayout.Controls.Add(lblPreviewStatus, 0, 2);
            previewLayout.Dock = DockStyle.Fill;
            previewLayout.Location = new Point(12, 26);
            previewLayout.Name = "previewLayout";
            previewLayout.RowCount = 3;
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            previewLayout.Size = new Size(864, 186);
            previewLayout.TabIndex = 0;
            //
            // lblPreviewSummary
            //
            lblPreviewSummary.Dock = DockStyle.Fill;
            lblPreviewSummary.AutoEllipsis = true;
            lblPreviewSummary.Location = new Point(3, 0);
            lblPreviewSummary.Name = "lblPreviewSummary";
            lblPreviewSummary.Size = new Size(858, 28);
            lblPreviewSummary.TabIndex = 0;
            lblPreviewSummary.Text = "Total filas: 0 | Válidas: 0 | Inválidas: 0 | Audios: 0";
            lblPreviewSummary.TextAlign = ContentAlignment.MiddleLeft;
            //
            // dgvExcelPreview
            //
            dgvExcelPreview.AllowUserToAddRows = false;
            dgvExcelPreview.AllowUserToDeleteRows = false;
            dgvExcelPreview.AutoGenerateColumns = false;
            dgvExcelPreview.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
            dgvExcelPreview.BackgroundColor = SystemColors.Window;
            dgvExcelPreview.BorderStyle = BorderStyle.Fixed3D;
            dgvExcelPreview.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvExcelPreview.Columns.AddRange(new DataGridViewColumn[] { colCountryCode, colPhone, colMessage, colToAudio, colValidationStatus });
            dgvExcelPreview.Dock = DockStyle.Fill;
            dgvExcelPreview.Location = new Point(3, 31);
            dgvExcelPreview.MultiSelect = false;
            dgvExcelPreview.Name = "dgvExcelPreview";
            dgvExcelPreview.ReadOnly = true;
            dgvExcelPreview.RowHeadersVisible = false;
            dgvExcelPreview.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvExcelPreview.Size = new Size(858, 120);
            dgvExcelPreview.TabIndex = 1;
            dgvExcelPreview.DataBindingComplete += dgvExcelPreview_DataBindingComplete;
            //
            // colCountryCode
            //
            colCountryCode.DataPropertyName = "CountryCode";
            colCountryCode.HeaderText = "Código país";
            colCountryCode.MinimumWidth = 90;
            colCountryCode.Name = "colCountryCode";
            colCountryCode.ReadOnly = true;
            colCountryCode.Width = 110;
            //
            // colPhone
            //
            colPhone.DataPropertyName = "Phone";
            colPhone.HeaderText = "Teléfono";
            colPhone.MinimumWidth = 120;
            colPhone.Name = "colPhone";
            colPhone.ReadOnly = true;
            colPhone.Width = 140;
            //
            // colMessage
            //
            colMessage.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colMessage.DataPropertyName = "Message";
            colMessage.HeaderText = "Mensaje";
            colMessage.MinimumWidth = 220;
            colMessage.Name = "colMessage";
            colMessage.ReadOnly = true;
            //
            // colToAudio
            //
            colToAudio.DataPropertyName = "ToAudio";
            colToAudio.HeaderText = "toAudio";
            colToAudio.MinimumWidth = 70;
            colToAudio.Name = "colToAudio";
            colToAudio.ReadOnly = true;
            colToAudio.Width = 80;
            //
            // colValidationStatus
            //
            colValidationStatus.DataPropertyName = "ValidationStatus";
            colValidationStatus.HeaderText = "Estado de validación";
            colValidationStatus.MinimumWidth = 150;
            colValidationStatus.Name = "colValidationStatus";
            colValidationStatus.ReadOnly = true;
            colValidationStatus.Width = 180;
            //
            // lblPreviewStatus
            //
            lblPreviewStatus.Dock = DockStyle.Fill;
            lblPreviewStatus.AutoEllipsis = true;
            lblPreviewStatus.ForeColor = Color.FromArgb(75, 85, 99);
            lblPreviewStatus.Location = new Point(3, 154);
            lblPreviewStatus.Name = "lblPreviewStatus";
            lblPreviewStatus.Size = new Size(858, 32);
            lblPreviewStatus.TabIndex = 2;
            lblPreviewStatus.Text = "Selecciona un archivo Excel para revisar las filas antes de programar.";
            lblPreviewStatus.TextAlign = ContentAlignment.MiddleLeft;
            //
            // grpSchedule
            //
            grpSchedule.Controls.Add(scheduleLayout);
            grpSchedule.Dock = DockStyle.Fill;
            grpSchedule.Location = new Point(0, 306);
            grpSchedule.Margin = new Padding(0, 0, 0, 12);
            grpSchedule.Name = "grpSchedule";
            grpSchedule.Padding = new Padding(12, 10, 12, 12);
            grpSchedule.Size = new Size(869, 274);
            grpSchedule.TabIndex = 2;
            grpSchedule.TabStop = false;
            grpSchedule.Text = "3. Programación";
            //
            // scheduleLayout
            //
            scheduleLayout.ColumnCount = 2;
            scheduleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            scheduleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            scheduleLayout.Controls.Add(lblScheduleTime, 0, 0);
            scheduleLayout.Controls.Add(schedulePickerLayout, 1, 0);
            scheduleLayout.Controls.Add(lblWhatsAppLine, 0, 1);
            scheduleLayout.Controls.Add(lineOptionsLayout, 1, 1);
            scheduleLayout.Controls.Add(lblRunLines, 0, 2);
            scheduleLayout.Controls.Add(runLinesLayout, 1, 2);
            scheduleLayout.Controls.Add(lblLinePreparation, 0, 3);
            scheduleLayout.Controls.Add(linePreparationLayout, 1, 3);
            scheduleLayout.Controls.Add(scheduleActionsLayout, 1, 4);
            scheduleLayout.Dock = DockStyle.Fill;
            scheduleLayout.Location = new Point(12, 26);
            scheduleLayout.Name = "scheduleLayout";
            scheduleLayout.RowCount = 5;
            scheduleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
            scheduleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            scheduleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            scheduleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            scheduleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            scheduleLayout.Size = new Size(864, 236);
            scheduleLayout.TabIndex = 0;
            //
            // lblScheduleTime
            //
            lblScheduleTime.Dock = DockStyle.Fill;
            lblScheduleTime.Location = new Point(3, 0);
            lblScheduleTime.Name = "lblScheduleTime";
            lblScheduleTime.Size = new Size(124, 74);
            lblScheduleTime.TabIndex = 0;
            lblScheduleTime.Text = "Fecha y hora:";
            lblScheduleTime.TextAlign = ContentAlignment.MiddleLeft;
            //
            // schedulePickerLayout
            //
            schedulePickerLayout.ColumnCount = 2;
            schedulePickerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            schedulePickerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
            schedulePickerLayout.Controls.Add(lblSelectedScheduleTime, 0, 0);
            schedulePickerLayout.Controls.Add(btnChangeScheduleTime, 1, 0);
            schedulePickerLayout.Controls.Add(lblScheduleSummary, 0, 1);
            schedulePickerLayout.Dock = DockStyle.Fill;
            schedulePickerLayout.Location = new Point(130, 0);
            schedulePickerLayout.Margin = new Padding(0);
            schedulePickerLayout.Name = "schedulePickerLayout";
            schedulePickerLayout.RowCount = 2;
            schedulePickerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            schedulePickerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            schedulePickerLayout.Size = new Size(734, 74);
            schedulePickerLayout.TabIndex = 1;
            //
            // lblSelectedScheduleTime
            //
            lblSelectedScheduleTime.AutoEllipsis = true;
            lblSelectedScheduleTime.BackColor = SystemColors.Window;
            lblSelectedScheduleTime.BorderStyle = BorderStyle.FixedSingle;
            lblSelectedScheduleTime.Dock = DockStyle.Fill;
            lblSelectedScheduleTime.Location = new Point(3, 3);
            lblSelectedScheduleTime.Margin = new Padding(3, 3, 8, 3);
            lblSelectedScheduleTime.Name = "lblSelectedScheduleTime";
            lblSelectedScheduleTime.Padding = new Padding(8, 0, 8, 0);
            lblSelectedScheduleTime.Size = new Size(611, 30);
            lblSelectedScheduleTime.TabIndex = 0;
            lblSelectedScheduleTime.Text = "dd/MM/yyyy HH:mm";
            lblSelectedScheduleTime.TextAlign = ContentAlignment.MiddleLeft;
            //
            // btnChangeScheduleTime
            //
            btnChangeScheduleTime.Dock = DockStyle.Fill;
            btnChangeScheduleTime.Location = new Point(625, 3);
            btnChangeScheduleTime.Margin = new Padding(3);
            btnChangeScheduleTime.Name = "btnChangeScheduleTime";
            btnChangeScheduleTime.Size = new Size(106, 30);
            btnChangeScheduleTime.TabIndex = 1;
            btnChangeScheduleTime.Text = "Cambiar...";
            btnChangeScheduleTime.UseVisualStyleBackColor = true;
            btnChangeScheduleTime.Click += btnChangeScheduleTime_Click;
            //
            // lblScheduleSummary
            //
            lblScheduleSummary.AutoEllipsis = true;
            schedulePickerLayout.SetColumnSpan(lblScheduleSummary, 2);
            lblScheduleSummary.Dock = DockStyle.Fill;
            lblScheduleSummary.ForeColor = Color.FromArgb(37, 99, 235);
            lblScheduleSummary.Location = new Point(3, 36);
            lblScheduleSummary.Name = "lblScheduleSummary";
            lblScheduleSummary.Size = new Size(728, 38);
            lblScheduleSummary.TabIndex = 2;
            lblScheduleSummary.Text = "Programado para:";
            lblScheduleSummary.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblWhatsAppLine
            //
            lblWhatsAppLine.Dock = DockStyle.Fill;
            lblWhatsAppLine.Location = new Point(3, 74);
            lblWhatsAppLine.Name = "lblWhatsAppLine";
            lblWhatsAppLine.Size = new Size(124, 34);
            lblWhatsAppLine.TabIndex = 2;
            lblWhatsAppLine.Text = "Línea WhatsApp:";
            lblWhatsAppLine.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lineOptionsLayout
            //
            lineOptionsLayout.Controls.Add(cmbWhatsAppLine);
            lineOptionsLayout.Controls.Add(chkAutoLineFallback);
            lineOptionsLayout.Dock = DockStyle.Fill;
            lineOptionsLayout.Location = new Point(130, 74);
            lineOptionsLayout.Margin = new Padding(0);
            lineOptionsLayout.Name = "lineOptionsLayout";
            lineOptionsLayout.Size = new Size(734, 34);
            lineOptionsLayout.TabIndex = 3;
            lineOptionsLayout.WrapContents = false;
            //
            // cmbWhatsAppLine
            //
            cmbWhatsAppLine.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbWhatsAppLine.FormattingEnabled = true;
            cmbWhatsAppLine.Location = new Point(3, 5);
            cmbWhatsAppLine.Margin = new Padding(3, 5, 16, 3);
            cmbWhatsAppLine.Name = "cmbWhatsAppLine";
            cmbWhatsAppLine.Size = new Size(230, 23);
            cmbWhatsAppLine.TabIndex = 0;
            cmbWhatsAppLine.SelectedIndexChanged += cmbWhatsAppLine_SelectedIndexChanged;
            //
            // chkAutoLineFallback
            //
            chkAutoLineFallback.Anchor = AnchorStyles.Left;
            chkAutoLineFallback.AutoSize = true;
            chkAutoLineFallback.Checked = false;
            chkAutoLineFallback.Location = new Point(252, 7);
            chkAutoLineFallback.Margin = new Padding(3, 7, 3, 3);
            chkAutoLineFallback.Name = "chkAutoLineFallback";
            chkAutoLineFallback.Size = new Size(225, 19);
            chkAutoLineFallback.TabIndex = 1;
            chkAutoLineFallback.Text = "Cambiar automáticamente si falla";
            chkAutoLineFallback.UseVisualStyleBackColor = true;
            //
            // lblRunLines
            //
            lblRunLines.Dock = DockStyle.Fill;
            lblRunLines.Location = new Point(3, 108);
            lblRunLines.Name = "lblRunLines";
            lblRunLines.Size = new Size(124, 40);
            lblRunLines.TabIndex = 4;
            lblRunLines.Text = "Líneas corrida:";
            lblRunLines.TextAlign = ContentAlignment.MiddleLeft;
            //
            // runLinesLayout
            //
            runLinesLayout.Controls.Add(btnSelectRunLines);
            runLinesLayout.Controls.Add(lblRunLinesSummary);
            runLinesLayout.Dock = DockStyle.Fill;
            runLinesLayout.Location = new Point(130, 108);
            runLinesLayout.Margin = new Padding(0);
            runLinesLayout.Name = "runLinesLayout";
            runLinesLayout.Size = new Size(715, 40);
            runLinesLayout.TabIndex = 5;
            runLinesLayout.WrapContents = false;
            //
            // btnSelectRunLines
            //
            btnSelectRunLines.Location = new Point(3, 5);
            btnSelectRunLines.Margin = new Padding(3, 5, 10, 3);
            btnSelectRunLines.Name = "btnSelectRunLines";
            btnSelectRunLines.Size = new Size(170, 30);
            btnSelectRunLines.TabIndex = 0;
            btnSelectRunLines.Text = "Seleccionar líneas";
            btnSelectRunLines.UseVisualStyleBackColor = true;
            btnSelectRunLines.Click += btnSelectRunLines_Click;
            //
            // lblRunLinesSummary
            //
            lblRunLinesSummary.AutoEllipsis = true;
            lblRunLinesSummary.ForeColor = Color.FromArgb(75, 85, 99);
            lblRunLinesSummary.Location = new Point(186, 0);
            lblRunLinesSummary.Name = "lblRunLinesSummary";
            lblRunLinesSummary.Size = new Size(420, 40);
            lblRunLinesSummary.TabIndex = 1;
            lblRunLinesSummary.Text = "Líneas seleccionadas: Principal";
            lblRunLinesSummary.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblLinePreparation
            //
            lblLinePreparation.Dock = DockStyle.Fill;
            lblLinePreparation.Location = new Point(3, 148);
            lblLinePreparation.Name = "lblLinePreparation";
            lblLinePreparation.Size = new Size(124, 42);
            lblLinePreparation.TabIndex = 6;
            lblLinePreparation.Text = "Preparación:";
            lblLinePreparation.TextAlign = ContentAlignment.MiddleLeft;
            //
            // linePreparationLayout
            //
            linePreparationLayout.Controls.Add(btnPrepareLine);
            linePreparationLayout.Controls.Add(btnPrepareAllLines);
            linePreparationLayout.Controls.Add(lblLinePreparationStatus);
            linePreparationLayout.Dock = DockStyle.Fill;
            linePreparationLayout.Location = new Point(130, 148);
            linePreparationLayout.Margin = new Padding(0);
            linePreparationLayout.Name = "linePreparationLayout";
            linePreparationLayout.Size = new Size(715, 42);
            linePreparationLayout.TabIndex = 7;
            linePreparationLayout.WrapContents = false;
            //
            // btnPrepareLine
            //
            btnPrepareLine.Location = new Point(3, 5);
            btnPrepareLine.Margin = new Padding(3, 5, 6, 3);
            btnPrepareLine.Name = "btnPrepareLine";
            btnPrepareLine.Size = new Size(120, 30);
            btnPrepareLine.TabIndex = 0;
            btnPrepareLine.Text = "Preparar línea";
            btnPrepareLine.UseVisualStyleBackColor = true;
            btnPrepareLine.Click += btnPrepareLine_Click;
            //
            // btnPrepareAllLines
            //
            btnPrepareAllLines.Location = new Point(132, 5);
            btnPrepareAllLines.Margin = new Padding(3, 5, 12, 3);
            btnPrepareAllLines.Name = "btnPrepareAllLines";
            btnPrepareAllLines.Size = new Size(120, 30);
            btnPrepareAllLines.TabIndex = 1;
            btnPrepareAllLines.Text = "Preparar todas";
            btnPrepareAllLines.UseVisualStyleBackColor = true;
            btnPrepareAllLines.Click += btnPrepareAllLines_Click;
            //
            // lblLinePreparationStatus
            //
            lblLinePreparationStatus.AutoEllipsis = true;
            lblLinePreparationStatus.ForeColor = Color.FromArgb(75, 85, 99);
            lblLinePreparationStatus.Location = new Point(267, 0);
            lblLinePreparationStatus.Name = "lblLinePreparationStatus";
            lblLinePreparationStatus.Size = new Size(280, 40);
            lblLinePreparationStatus.TabIndex = 2;
            lblLinePreparationStatus.Text = "Estado línea: Sin verificar";
            lblLinePreparationStatus.TextAlign = ContentAlignment.MiddleLeft;
            //
            // scheduleActionsLayout
            //
            scheduleActionsLayout.Controls.Add(btnSend);
            scheduleActionsLayout.Controls.Add(btnSendNow);
            scheduleActionsLayout.Controls.Add(btnConfigureLines);
            scheduleActionsLayout.Dock = DockStyle.Fill;
            scheduleActionsLayout.FlowDirection = FlowDirection.RightToLeft;
            scheduleActionsLayout.Location = new Point(130, 190);
            scheduleActionsLayout.Margin = new Padding(0);
            scheduleActionsLayout.Name = "scheduleActionsLayout";
            scheduleActionsLayout.Size = new Size(715, 46);
            scheduleActionsLayout.TabIndex = 8;
            scheduleActionsLayout.WrapContents = false;
            //
            // btnSend
            //
            btnSend.BackColor = Color.FromArgb(22, 163, 74);
            btnSend.Enabled = false;
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.FlatStyle = FlatStyle.Flat;
            btnSend.ForeColor = Color.White;
            btnSend.Location = new Point(555, 2);
            btnSend.Margin = new Padding(3, 2, 0, 0);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(160, 32);
            btnSend.TabIndex = 0;
            btnSend.Text = "Programar envío";
            btnSend.UseVisualStyleBackColor = false;
            btnSend.Click += btnSend_Click;
            //
            // btnSendNow
            //
            btnSendNow.BackColor = Color.FromArgb(37, 99, 235);
            btnSendNow.Enabled = false;
            btnSendNow.FlatAppearance.BorderSize = 0;
            btnSendNow.FlatStyle = FlatStyle.Flat;
            btnSendNow.ForeColor = Color.White;
            btnSendNow.Location = new Point(422, 2);
            btnSendNow.Margin = new Padding(3, 2, 6, 0);
            btnSendNow.Name = "btnSendNow";
            btnSendNow.Size = new Size(124, 32);
            btnSendNow.TabIndex = 1;
            btnSendNow.Text = "Enviar ahora";
            btnSendNow.UseVisualStyleBackColor = false;
            btnSendNow.Click += btnSendNow_Click;
            //
            // btnConfigureLines
            //
            btnConfigureLines.Location = new Point(275, 2);
            btnConfigureLines.Margin = new Padding(3, 2, 6, 0);
            btnConfigureLines.Name = "btnConfigureLines";
            btnConfigureLines.Size = new Size(138, 32);
            btnConfigureLines.TabIndex = 2;
            btnConfigureLines.Text = "Configurar líneas";
            btnConfigureLines.UseVisualStyleBackColor = true;
            btnConfigureLines.Click += btnConfigureLines_Click;
            //
            // grpSendStatus
            //
            grpSendStatus.Controls.Add(statusLayout);
            grpSendStatus.Dock = DockStyle.Fill;
            grpSendStatus.Location = new Point(0, 592);
            grpSendStatus.Margin = new Padding(0, 0, 0, 12);
            grpSendStatus.Name = "grpSendStatus";
            grpSendStatus.Padding = new Padding(12, 10, 12, 12);
            grpSendStatus.Size = new Size(869, 140);
            grpSendStatus.TabIndex = 3;
            grpSendStatus.TabStop = false;
            grpSendStatus.Text = "4. Estado del envío";
            //
            // statusLayout
            //
            statusLayout.ColumnCount = 2;
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 232F));
            statusLayout.Controls.Add(lblGeneralStatus, 0, 0);
            statusLayout.Controls.Add(lblProgress, 0, 1);
            statusLayout.Controls.Add(lblProgressText, 0, 2);
            statusLayout.Controls.Add(lblSendCounters, 0, 3);
            statusLayout.Controls.Add(progressBar1, 0, 4);
            statusLayout.Controls.Add(statusActionsLayout, 1, 0);
            statusLayout.Dock = DockStyle.Fill;
            statusLayout.Location = new Point(12, 26);
            statusLayout.Name = "statusLayout";
            statusLayout.RowCount = 5;
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            statusLayout.Size = new Size(845, 102);
            statusLayout.TabIndex = 0;
            //
            // lblGeneralStatus
            //
            lblGeneralStatus.Dock = DockStyle.Fill;
            lblGeneralStatus.AutoEllipsis = true;
            lblGeneralStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblGeneralStatus.Location = new Point(3, 0);
            lblGeneralStatus.Name = "lblGeneralStatus";
            lblGeneralStatus.Size = new Size(638, 22);
            lblGeneralStatus.TabIndex = 0;
            lblGeneralStatus.Text = "Estado general: Sin archivo";
            lblGeneralStatus.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblProgress
            //
            lblProgress.Dock = DockStyle.Fill;
            lblProgress.AutoEllipsis = true;
            lblProgress.ForeColor = Color.FromArgb(75, 85, 99);
            lblProgress.Location = new Point(3, 22);
            lblProgress.Name = "lblProgress";
            lblProgress.Size = new Size(638, 20);
            lblProgress.TabIndex = 1;
            lblProgress.Text = "Progreso";
            lblProgress.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblProgressText
            //
            lblProgressText.Dock = DockStyle.Fill;
            lblProgressText.AutoEllipsis = true;
            lblProgressText.Location = new Point(3, 42);
            lblProgressText.Name = "lblProgressText";
            lblProgressText.Size = new Size(638, 20);
            lblProgressText.TabIndex = 2;
            lblProgressText.Text = "Procesados: 0 / 0";
            lblProgressText.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblSendCounters
            //
            lblSendCounters.Dock = DockStyle.Fill;
            lblSendCounters.AutoEllipsis = true;
            lblSendCounters.Location = new Point(3, 62);
            lblSendCounters.Name = "lblSendCounters";
            lblSendCounters.Size = new Size(638, 20);
            lblSendCounters.TabIndex = 3;
            lblSendCounters.Text = "Éxitos: 0 | Errores: 0 | Omitidos: 0";
            lblSendCounters.TextAlign = ContentAlignment.MiddleLeft;
            //
            // progressBar1
            //
            progressBar1.Dock = DockStyle.Fill;
            progressBar1.Location = new Point(3, 85);
            progressBar1.MinimumSize = new Size(120, 14);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(607, 14);
            progressBar1.TabIndex = 4;
            //
            // statusActionsLayout
            //
            statusActionsLayout.Controls.Add(btnCancel);
            statusActionsLayout.Controls.Add(btnPauseResume);
            statusActionsLayout.Dock = DockStyle.Fill;
            statusActionsLayout.FlowDirection = FlowDirection.RightToLeft;
            statusActionsLayout.Location = new Point(613, 0);
            statusActionsLayout.Margin = new Padding(0);
            statusActionsLayout.Name = "statusActionsLayout";
            statusLayout.SetRowSpan(statusActionsLayout, 5);
            statusActionsLayout.Size = new Size(232, 102);
            statusActionsLayout.TabIndex = 5;
            statusActionsLayout.WrapContents = false;
            //
            // btnCancel
            //
            btnCancel.Anchor = AnchorStyles.Right;
            btnCancel.Enabled = false;
            btnCancel.Location = new Point(128, 11);
            btnCancel.Margin = new Padding(8, 11, 0, 0);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(104, 32);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancelar";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += BtnCancel_Click;
            //
            // btnPauseResume
            //
            btnPauseResume.Anchor = AnchorStyles.Right;
            btnPauseResume.Enabled = false;
            btnPauseResume.Location = new Point(16, 11);
            btnPauseResume.Margin = new Padding(0, 11, 0, 0);
            btnPauseResume.Name = "btnPauseResume";
            btnPauseResume.Size = new Size(104, 32);
            btnPauseResume.TabIndex = 0;
            btnPauseResume.Text = "Pausar";
            btnPauseResume.UseVisualStyleBackColor = true;
            btnPauseResume.Click += BtnPauseResume_Click;
            //
            // grpActivity
            //
            grpActivity.Controls.Add(logLayout);
            grpActivity.Dock = DockStyle.Fill;
            grpActivity.Location = new Point(0, 744);
            grpActivity.Margin = new Padding(0);
            grpActivity.Name = "grpActivity";
            grpActivity.Padding = new Padding(12, 10, 12, 12);
            grpActivity.Size = new Size(869, 158);
            grpActivity.TabIndex = 4;
            grpActivity.TabStop = false;
            grpActivity.Text = "5. Actividad / logs";
            //
            // logLayout
            //
            logLayout.ColumnCount = 1;
            logLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            logLayout.Controls.Add(txtLog, 0, 0);
            logLayout.Dock = DockStyle.Fill;
            logLayout.Location = new Point(12, 26);
            logLayout.Name = "logLayout";
            logLayout.RowCount = 1;
            logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            logLayout.Size = new Size(864, 126);
            logLayout.TabIndex = 0;
            //
            // txtLog
            //
            txtLog.Dock = DockStyle.Fill;
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.Location = new Point(3, 3);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(858, 120);
            txtLog.TabIndex = 0;
            //
            // openFileDialog1
            //
            openFileDialog1.FileName = "openFileDialog1";
            //
            // schedulerTimer
            //
            schedulerTimer.Interval = 1000;
            schedulerTimer.Tick += SchedulerTimer_Tick;
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(920, 880);
            AutoScroll = true;
            Controls.Add(mainScrollPanel);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(760, 560);
            Name = "Form1";
            Padding = new Padding(16);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "AutoWhatsApp";
            FormClosing += Form1_FormClosing;
            mainLayout.ResumeLayout(false);
            mainScrollPanel.ResumeLayout(false);
            grpExcelFile.ResumeLayout(false);
            fileLayout.ResumeLayout(false);
            grpExcelPreview.ResumeLayout(false);
            previewLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvExcelPreview).EndInit();
            grpSchedule.ResumeLayout(false);
            scheduleLayout.ResumeLayout(false);
            schedulePickerLayout.ResumeLayout(false);
            lineOptionsLayout.ResumeLayout(false);
            lineOptionsLayout.PerformLayout();
            runLinesLayout.ResumeLayout(false);
            linePreparationLayout.ResumeLayout(false);
            scheduleActionsLayout.ResumeLayout(false);
            grpSendStatus.ResumeLayout(false);
            statusLayout.ResumeLayout(false);
            statusActionsLayout.ResumeLayout(false);
            grpActivity.ResumeLayout(false);
            logLayout.ResumeLayout(false);
            logLayout.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Panel mainScrollPanel;
        private System.Windows.Forms.GroupBox grpExcelFile;
        private System.Windows.Forms.TableLayoutPanel fileLayout;
        private System.Windows.Forms.GroupBox grpExcelPreview;
        private System.Windows.Forms.TableLayoutPanel previewLayout;
        private System.Windows.Forms.Label lblPreviewSummary;
        private System.Windows.Forms.DataGridView dgvExcelPreview;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCountryCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPhone;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMessage;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colToAudio;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValidationStatus;
        private System.Windows.Forms.Label lblPreviewStatus;
        private System.Windows.Forms.GroupBox grpSchedule;
        private System.Windows.Forms.TableLayoutPanel scheduleLayout;
        private System.Windows.Forms.Label lblScheduleTime;
        private System.Windows.Forms.TableLayoutPanel schedulePickerLayout;
        private System.Windows.Forms.Label lblSelectedScheduleTime;
        private System.Windows.Forms.Button btnChangeScheduleTime;
        private System.Windows.Forms.Label lblScheduleSummary;
        private System.Windows.Forms.FlowLayoutPanel lineOptionsLayout;
        private System.Windows.Forms.FlowLayoutPanel scheduleActionsLayout;
        private System.Windows.Forms.GroupBox grpSendStatus;
        private System.Windows.Forms.TableLayoutPanel statusLayout;
        private System.Windows.Forms.Label lblGeneralStatus;
        private System.Windows.Forms.Label lblProgress;
        private System.Windows.Forms.Label lblProgressText;
        private System.Windows.Forms.Label lblSendCounters;
        private System.Windows.Forms.FlowLayoutPanel statusActionsLayout;
        private System.Windows.Forms.GroupBox grpActivity;
        private System.Windows.Forms.TableLayoutPanel logLayout;
        private System.Windows.Forms.Button btnSelectFile;
        private System.Windows.Forms.Label lblFilePath;
        private System.Windows.Forms.ToolTip uiToolTip;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Button btnConfigureLines;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button btnPauseResume;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblWhatsAppLine;
        private System.Windows.Forms.ComboBox cmbWhatsAppLine;
        private System.Windows.Forms.CheckBox chkAutoLineFallback;
        private System.Windows.Forms.Label lblRunLines;
        private System.Windows.Forms.FlowLayoutPanel runLinesLayout;
        private System.Windows.Forms.Button btnSelectRunLines;
        private System.Windows.Forms.Label lblRunLinesSummary;
        private System.Windows.Forms.Label lblLinePreparation;
        private System.Windows.Forms.FlowLayoutPanel linePreparationLayout;
        private System.Windows.Forms.Button btnPrepareLine;
        private System.Windows.Forms.Button btnPrepareAllLines;
        private System.Windows.Forms.Label lblLinePreparationStatus;
        private System.Windows.Forms.Button btnSendNow;
    }
}

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
            mainMenuStrip = new MenuStrip();
            archivoToolStripMenuItem = new ToolStripMenuItem();
            seleccionarExcelToolStripMenuItem = new ToolStripMenuItem();
            descargarPlantillaExcelToolStripMenuItem = new ToolStripMenuItem();
            archivoToolStripSeparator = new ToolStripSeparator();
            salirToolStripMenuItem = new ToolStripMenuItem();
            configuracionToolStripMenuItem = new ToolStripMenuItem();
            configuracionGeneralToolStripMenuItem = new ToolStripMenuItem();
            configuracionToolStripSeparator = new ToolStripSeparator();
            mostrarVistaPreviaExcelToolStripMenuItem = new ToolStripMenuItem();
            ayudaToolStripMenuItem = new ToolStripMenuItem();
            acercaDeAutoWhatsAppToolStripMenuItem = new ToolStripMenuItem();
            mainScrollPanel = new Panel();
            mainLayout = new TableLayoutPanel();
            lblConfigurationSummary = new Label();
            grpExcelFile = new GroupBox();
            fileLayout = new TableLayoutPanel();
            btnSelectFile = new Button();
            btnDownloadTemplate = new Button();
            chkShowExcelPreview = new CheckBox();
            lblFilePath = new Label();
            lblExcelCompactSummary = new Label();
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
            scheduleActionsLayout = new FlowLayoutPanel();
            btnSend = new Button();
            btnSendNow = new Button();
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
            mainMenuStrip.SuspendLayout();
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
            scheduleActionsLayout.SuspendLayout();
            grpSendStatus.SuspendLayout();
            statusLayout.SuspendLayout();
            statusActionsLayout.SuspendLayout();
            grpActivity.SuspendLayout();
            logLayout.SuspendLayout();
            SuspendLayout();
            //
            // mainMenuStrip
            //
            mainMenuStrip.Items.AddRange(new ToolStripItem[] { archivoToolStripMenuItem, configuracionToolStripMenuItem, ayudaToolStripMenuItem });
            mainMenuStrip.Location = new Point(0, 0);
            mainMenuStrip.Name = "mainMenuStrip";
            mainMenuStrip.Size = new Size(920, 24);
            mainMenuStrip.TabIndex = 0;
            mainMenuStrip.Text = "mainMenuStrip";
            //
            // archivoToolStripMenuItem
            //
            archivoToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { seleccionarExcelToolStripMenuItem, descargarPlantillaExcelToolStripMenuItem, archivoToolStripSeparator, salirToolStripMenuItem });
            archivoToolStripMenuItem.Name = "archivoToolStripMenuItem";
            archivoToolStripMenuItem.Size = new Size(60, 20);
            archivoToolStripMenuItem.Text = "&Archivo";
            //
            // seleccionarExcelToolStripMenuItem
            //
            seleccionarExcelToolStripMenuItem.Name = "seleccionarExcelToolStripMenuItem";
            seleccionarExcelToolStripMenuItem.Size = new Size(216, 22);
            seleccionarExcelToolStripMenuItem.Text = "Seleccionar Excel...";
            seleccionarExcelToolStripMenuItem.Click += seleccionarExcelToolStripMenuItem_Click;
            //
            // descargarPlantillaExcelToolStripMenuItem
            //
            descargarPlantillaExcelToolStripMenuItem.Name = "descargarPlantillaExcelToolStripMenuItem";
            descargarPlantillaExcelToolStripMenuItem.Size = new Size(216, 22);
            descargarPlantillaExcelToolStripMenuItem.Text = "Descargar plantilla Excel...";
            descargarPlantillaExcelToolStripMenuItem.Click += descargarPlantillaExcelToolStripMenuItem_Click;
            //
            // archivoToolStripSeparator
            //
            archivoToolStripSeparator.Name = "archivoToolStripSeparator";
            archivoToolStripSeparator.Size = new Size(213, 6);
            //
            // salirToolStripMenuItem
            //
            salirToolStripMenuItem.Name = "salirToolStripMenuItem";
            salirToolStripMenuItem.Size = new Size(216, 22);
            salirToolStripMenuItem.Text = "Salir";
            salirToolStripMenuItem.Click += salirToolStripMenuItem_Click;
            //
            // configuracionToolStripMenuItem
            //
            configuracionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { configuracionGeneralToolStripMenuItem, configuracionToolStripSeparator, mostrarVistaPreviaExcelToolStripMenuItem });
            configuracionToolStripMenuItem.Name = "configuracionToolStripMenuItem";
            configuracionToolStripMenuItem.Size = new Size(95, 20);
            configuracionToolStripMenuItem.Text = "&Configuración";
            configuracionToolStripMenuItem.DropDownOpening += configuracionToolStripMenuItem_DropDownOpening;
            //
            // configuracionGeneralToolStripMenuItem
            //
            configuracionGeneralToolStripMenuItem.Name = "configuracionGeneralToolStripMenuItem";
            configuracionGeneralToolStripMenuItem.Size = new Size(226, 22);
            configuracionGeneralToolStripMenuItem.Text = "Configuración general...";
            configuracionGeneralToolStripMenuItem.Click += configuracionGeneralToolStripMenuItem_Click;
            //
            // configuracionToolStripSeparator
            //
            configuracionToolStripSeparator.Name = "configuracionToolStripSeparator";
            configuracionToolStripSeparator.Size = new Size(223, 6);
            //
            // mostrarVistaPreviaExcelToolStripMenuItem
            //
            mostrarVistaPreviaExcelToolStripMenuItem.CheckOnClick = true;
            mostrarVistaPreviaExcelToolStripMenuItem.Name = "mostrarVistaPreviaExcelToolStripMenuItem";
            mostrarVistaPreviaExcelToolStripMenuItem.Size = new Size(226, 22);
            mostrarVistaPreviaExcelToolStripMenuItem.Text = "Mostrar vista previa del Excel";
            mostrarVistaPreviaExcelToolStripMenuItem.Click += mostrarVistaPreviaExcelToolStripMenuItem_Click;
            //
            // ayudaToolStripMenuItem
            //
            ayudaToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { acercaDeAutoWhatsAppToolStripMenuItem });
            ayudaToolStripMenuItem.Name = "ayudaToolStripMenuItem";
            ayudaToolStripMenuItem.Size = new Size(53, 20);
            ayudaToolStripMenuItem.Text = "Ay&uda";
            //
            // acercaDeAutoWhatsAppToolStripMenuItem
            //
            acercaDeAutoWhatsAppToolStripMenuItem.Name = "acercaDeAutoWhatsAppToolStripMenuItem";
            acercaDeAutoWhatsAppToolStripMenuItem.Size = new Size(211, 22);
            acercaDeAutoWhatsAppToolStripMenuItem.Text = "Acerca de AutoWhatsApp";
            acercaDeAutoWhatsAppToolStripMenuItem.Click += acercaDeAutoWhatsAppToolStripMenuItem_Click;
            //
            // mainScrollPanel
            //
            mainScrollPanel.AutoScroll = true;
            mainScrollPanel.Controls.Add(mainLayout);
            mainScrollPanel.Dock = DockStyle.Fill;
            mainScrollPanel.Location = new Point(0, 24);
            mainScrollPanel.Name = "mainScrollPanel";
            mainScrollPanel.Size = new Size(920, 736);
            mainScrollPanel.TabIndex = 1;
            mainScrollPanel.Resize += mainScrollPanel_Resize;
            //
            // mainLayout
            //
            mainLayout.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            mainLayout.ColumnCount = 1;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.Controls.Add(lblConfigurationSummary, 0, 0);
            mainLayout.Controls.Add(grpExcelFile, 0, 1);
            mainLayout.Controls.Add(grpExcelPreview, 0, 2);
            mainLayout.Controls.Add(grpSchedule, 0, 3);
            mainLayout.Controls.Add(grpSendStatus, 0, 4);
            mainLayout.Controls.Add(grpActivity, 0, 5);
            mainLayout.Location = new Point(8, 8);
            mainLayout.Name = "mainLayout";
            mainLayout.RowCount = 6;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 104F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 156F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.Size = new Size(895, 720);
            mainLayout.TabIndex = 0;
            //
            // lblConfigurationSummary
            //
            lblConfigurationSummary.AutoEllipsis = true;
            lblConfigurationSummary.Dock = DockStyle.Fill;
            lblConfigurationSummary.ForeColor = Color.FromArgb(75, 85, 99);
            lblConfigurationSummary.Location = new Point(3, 0);
            lblConfigurationSummary.Margin = new Padding(3, 0, 3, 6);
            lblConfigurationSummary.Name = "lblConfigurationSummary";
            lblConfigurationSummary.Size = new Size(889, 28);
            lblConfigurationSummary.TabIndex = 1;
            lblConfigurationSummary.Text = "Configuración: sin línea · Espera: 0 min · ElevenLabs: No configurado";
            lblConfigurationSummary.TextAlign = ContentAlignment.MiddleLeft;
            //
            // grpExcelFile
            //
            grpExcelFile.Controls.Add(fileLayout);
            grpExcelFile.Dock = DockStyle.Fill;
            grpExcelFile.Location = new Point(0, 0);
            grpExcelFile.Margin = new Padding(0, 0, 0, 6);
            grpExcelFile.Name = "grpExcelFile";
            grpExcelFile.Padding = new Padding(12, 10, 12, 12);
            grpExcelFile.Size = new Size(895, 106);
            grpExcelFile.TabIndex = 1;
            grpExcelFile.TabStop = false;
            grpExcelFile.Text = "1. Archivo Excel";
            //
            // fileLayout
            //
            fileLayout.ColumnCount = 4;
            fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 206F));
            fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 154F));
            fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210F));
            fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            fileLayout.Controls.Add(btnSelectFile, 0, 0);
            fileLayout.Controls.Add(btnDownloadTemplate, 1, 0);
            fileLayout.Controls.Add(chkShowExcelPreview, 2, 0);
            fileLayout.Controls.Add(lblFilePath, 3, 0);
            fileLayout.Controls.Add(lblExcelCompactSummary, 0, 1);
            fileLayout.Dock = DockStyle.Fill;
            fileLayout.Location = new Point(12, 26);
            fileLayout.Name = "fileLayout";
            fileLayout.RowCount = 2;
            fileLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fileLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            fileLayout.Size = new Size(871, 68);
            fileLayout.TabIndex = 0;
            //
            // btnSelectFile
            //
            btnSelectFile.Anchor = AnchorStyles.Left;
            btnSelectFile.Location = new Point(3, 5);
            btnSelectFile.Name = "btnSelectFile";
            btnSelectFile.Size = new Size(194, 34);
            btnSelectFile.TabIndex = 0;
            btnSelectFile.Text = "Seleccionar archivo Excel";
            btnSelectFile.UseVisualStyleBackColor = true;
            btnSelectFile.Click += btnSelectFile_Click;
            //
            // btnDownloadTemplate
            //
            btnDownloadTemplate.Anchor = AnchorStyles.Left;
            btnDownloadTemplate.Location = new Point(209, 5);
            btnDownloadTemplate.Name = "btnDownloadTemplate";
            btnDownloadTemplate.Size = new Size(142, 34);
            btnDownloadTemplate.TabIndex = 1;
            btnDownloadTemplate.Text = "Descargar plantilla";
            btnDownloadTemplate.UseVisualStyleBackColor = true;
            btnDownloadTemplate.Click += btnDownloadTemplate_Click;
            //
            // chkShowExcelPreview
            //
            chkShowExcelPreview.Anchor = AnchorStyles.Left;
            chkShowExcelPreview.AutoSize = true;
            chkShowExcelPreview.Location = new Point(363, 11);
            chkShowExcelPreview.Name = "chkShowExcelPreview";
            chkShowExcelPreview.Size = new Size(186, 19);
            chkShowExcelPreview.TabIndex = 2;
            chkShowExcelPreview.Text = "Mostrar vista previa del Excel";
            chkShowExcelPreview.UseVisualStyleBackColor = true;
            chkShowExcelPreview.CheckedChanged += chkShowExcelPreview_CheckedChanged;
            //
            // lblFilePath
            //
            lblFilePath.AutoEllipsis = true;
            lblFilePath.Dock = DockStyle.Fill;
            lblFilePath.Location = new Point(573, 0);
            lblFilePath.Name = "lblFilePath";
            lblFilePath.Padding = new Padding(8, 0, 0, 0);
            lblFilePath.Size = new Size(295, 42);
            lblFilePath.TabIndex = 3;
            lblFilePath.Text = "Archivo seleccionado: ninguno";
            lblFilePath.TextAlign = ContentAlignment.MiddleLeft;
            uiToolTip.SetToolTip(lblFilePath, "Archivo seleccionado: ninguno");
            //
            // lblExcelCompactSummary
            //
            lblExcelCompactSummary.AutoEllipsis = true;
            fileLayout.SetColumnSpan(lblExcelCompactSummary, 4);
            lblExcelCompactSummary.Dock = DockStyle.Fill;
            lblExcelCompactSummary.ForeColor = Color.FromArgb(75, 85, 99);
            lblExcelCompactSummary.Location = new Point(3, 42);
            lblExcelCompactSummary.Name = "lblExcelCompactSummary";
            lblExcelCompactSummary.Size = new Size(865, 26);
            lblExcelCompactSummary.TabIndex = 4;
            lblExcelCompactSummary.Text = "Resumen: sin Excel seleccionado.";
            lblExcelCompactSummary.TextAlign = ContentAlignment.MiddleLeft;
            //
            // grpExcelPreview
            //
            grpExcelPreview.Controls.Add(previewLayout);
            grpExcelPreview.Dock = DockStyle.Fill;
            grpExcelPreview.Location = new Point(0, 112);
            grpExcelPreview.Margin = new Padding(0, 0, 0, 6);
            grpExcelPreview.Name = "grpExcelPreview";
            grpExcelPreview.Padding = new Padding(12, 10, 12, 12);
            grpExcelPreview.Size = new Size(895, 0);
            grpExcelPreview.TabIndex = 1;
            grpExcelPreview.TabStop = false;
            grpExcelPreview.Text = "2. Vista previa del Excel";
            grpExcelPreview.Visible = false;
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
            dgvExcelPreview.Visible = false;
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
            grpSchedule.Location = new Point(0, 112);
            grpSchedule.Margin = new Padding(0, 0, 0, 6);
            grpSchedule.Name = "grpSchedule";
            grpSchedule.Padding = new Padding(12, 10, 12, 12);
            grpSchedule.Size = new Size(895, 98);
            grpSchedule.TabIndex = 3;
            grpSchedule.TabStop = false;
            grpSchedule.Text = "3. Programación";
            //
            // scheduleLayout
            //
            scheduleLayout.ColumnCount = 3;
            scheduleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            scheduleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            scheduleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 306F));
            scheduleLayout.Controls.Add(lblScheduleTime, 0, 0);
            scheduleLayout.Controls.Add(schedulePickerLayout, 1, 0);
            scheduleLayout.Controls.Add(scheduleActionsLayout, 2, 0);
            scheduleLayout.Dock = DockStyle.Fill;
            scheduleLayout.Location = new Point(12, 26);
            scheduleLayout.Name = "scheduleLayout";
            scheduleLayout.RowCount = 1;
            scheduleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            scheduleLayout.Size = new Size(871, 60);
            scheduleLayout.TabIndex = 0;
            //
            // lblScheduleTime
            //
            lblScheduleTime.Dock = DockStyle.Fill;
            lblScheduleTime.Location = new Point(3, 0);
            lblScheduleTime.Name = "lblScheduleTime";
            lblScheduleTime.Size = new Size(124, 60);
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
            schedulePickerLayout.Size = new Size(435, 60);
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
            lblSelectedScheduleTime.Size = new Size(312, 30);
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
            lblScheduleSummary.Size = new Size(429, 24);
            lblScheduleSummary.TabIndex = 2;
            lblScheduleSummary.Text = "Programado para:";
            lblScheduleSummary.TextAlign = ContentAlignment.MiddleLeft;
            //
            // scheduleActionsLayout
            //
            scheduleActionsLayout.Controls.Add(btnSend);
            scheduleActionsLayout.Controls.Add(btnSendNow);
            scheduleActionsLayout.Dock = DockStyle.Fill;
            scheduleActionsLayout.FlowDirection = FlowDirection.RightToLeft;
            scheduleActionsLayout.Location = new Point(565, 0);
            scheduleActionsLayout.Margin = new Padding(0);
            scheduleActionsLayout.Name = "scheduleActionsLayout";
            scheduleActionsLayout.Size = new Size(306, 60);
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
            // grpSendStatus
            //
            grpSendStatus.Controls.Add(statusLayout);
            grpSendStatus.Dock = DockStyle.Fill;
            grpSendStatus.Location = new Point(0, 216);
            grpSendStatus.Margin = new Padding(0, 0, 0, 6);
            grpSendStatus.Name = "grpSendStatus";
            grpSendStatus.Padding = new Padding(12, 10, 12, 12);
            grpSendStatus.Size = new Size(895, 150);
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
            statusLayout.Controls.Add(progressBar1, 0, 2);
            statusLayout.Controls.Add(lblProgressText, 0, 3);
            statusLayout.Controls.Add(lblSendCounters, 0, 4);
            statusLayout.Controls.Add(statusActionsLayout, 1, 0);
            statusLayout.Dock = DockStyle.Fill;
            statusLayout.Location = new Point(12, 26);
            statusLayout.Name = "statusLayout";
            statusLayout.RowCount = 5;
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
            statusLayout.Size = new Size(871, 112);
            statusLayout.TabIndex = 0;
            //
            // lblGeneralStatus
            //
            lblGeneralStatus.Dock = DockStyle.Fill;
            lblGeneralStatus.AutoEllipsis = true;
            lblGeneralStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblGeneralStatus.Location = new Point(3, 0);
            lblGeneralStatus.Name = "lblGeneralStatus";
            lblGeneralStatus.Size = new Size(633, 32);
            lblGeneralStatus.TabIndex = 0;
            lblGeneralStatus.Text = "Estado general: Sin archivo";
            lblGeneralStatus.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblProgress
            //
            lblProgress.Dock = DockStyle.Fill;
            lblProgress.AutoEllipsis = true;
            lblProgress.ForeColor = Color.FromArgb(75, 85, 99);
            lblProgress.Location = new Point(3, 32);
            lblProgress.Name = "lblProgress";
            lblProgress.Size = new Size(865, 18);
            lblProgress.TabIndex = 1;
            lblProgress.Text = "Progreso";
            lblProgress.TextAlign = ContentAlignment.MiddleLeft;
            statusLayout.SetColumnSpan(lblProgress, 2);
            //
            // lblProgressText
            //
            lblProgressText.Dock = DockStyle.Fill;
            lblProgressText.AutoEllipsis = true;
            lblProgressText.Location = new Point(3, 76);
            lblProgressText.Name = "lblProgressText";
            lblProgressText.Size = new Size(865, 18);
            lblProgressText.TabIndex = 2;
            lblProgressText.Text = "Procesados: 0 / 0";
            lblProgressText.TextAlign = ContentAlignment.MiddleLeft;
            statusLayout.SetColumnSpan(lblProgressText, 2);
            //
            // lblSendCounters
            //
            lblSendCounters.Dock = DockStyle.Fill;
            lblSendCounters.AutoEllipsis = true;
            lblSendCounters.Location = new Point(3, 94);
            lblSendCounters.Name = "lblSendCounters";
            lblSendCounters.Size = new Size(865, 18);
            lblSendCounters.TabIndex = 3;
            lblSendCounters.Text = "Éxitos: 0 | Errores: 0 | Omitidos: 0";
            lblSendCounters.TextAlign = ContentAlignment.MiddleLeft;
            statusLayout.SetColumnSpan(lblSendCounters, 2);
            //
            // progressBar1
            //
            progressBar1.Dock = DockStyle.Fill;
            progressBar1.Location = new Point(3, 53);
            progressBar1.Margin = new Padding(3, 3, 3, 5);
            progressBar1.MinimumSize = new Size(120, 16);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(865, 18);
            progressBar1.TabIndex = 4;
            statusLayout.SetColumnSpan(progressBar1, 2);
            //
            // statusActionsLayout
            //
            statusActionsLayout.Controls.Add(btnCancel);
            statusActionsLayout.Controls.Add(btnPauseResume);
            statusActionsLayout.Dock = DockStyle.Fill;
            statusActionsLayout.FlowDirection = FlowDirection.RightToLeft;
            statusActionsLayout.Location = new Point(639, 0);
            statusActionsLayout.Margin = new Padding(0);
            statusActionsLayout.Name = "statusActionsLayout";
            statusActionsLayout.Size = new Size(232, 32);
            statusActionsLayout.TabIndex = 5;
            statusActionsLayout.WrapContents = false;
            //
            // btnCancel
            //
            btnCancel.Anchor = AnchorStyles.Right;
            btnCancel.Enabled = false;
            btnCancel.Location = new Point(128, 0);
            btnCancel.Margin = new Padding(8, 0, 0, 0);
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
            btnPauseResume.Location = new Point(16, 0);
            btnPauseResume.Margin = new Padding(0);
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
            grpActivity.Location = new Point(0, 372);
            grpActivity.Margin = new Padding(0);
            grpActivity.Name = "grpActivity";
            grpActivity.Padding = new Padding(12, 10, 12, 12);
            grpActivity.Size = new Size(895, 348);
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
            logLayout.Size = new Size(871, 310);
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
            txtLog.Size = new Size(865, 304);
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
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(920, 760);
            AutoScroll = false;
            Controls.Add(mainScrollPanel);
            Controls.Add(mainMenuStrip);
            FormBorderStyle = FormBorderStyle.Sizable;
            MainMenuStrip = mainMenuStrip;
            MinimumSize = new Size(800, 600);
            Name = "Form1";
            Padding = new Padding(0);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "AutoWhatsApp";
            FormClosing += Form1_FormClosing;
            mainLayout.ResumeLayout(false);
            mainLayout.PerformLayout();
            mainScrollPanel.ResumeLayout(false);
            grpExcelFile.ResumeLayout(false);
            fileLayout.ResumeLayout(false);
            fileLayout.PerformLayout();
            grpExcelPreview.ResumeLayout(false);
            previewLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvExcelPreview).EndInit();
            grpSchedule.ResumeLayout(false);
            scheduleLayout.ResumeLayout(false);
            schedulePickerLayout.ResumeLayout(false);
            scheduleActionsLayout.ResumeLayout(false);
            grpSendStatus.ResumeLayout(false);
            statusLayout.ResumeLayout(false);
            statusActionsLayout.ResumeLayout(false);
            grpActivity.ResumeLayout(false);
            logLayout.ResumeLayout(false);
            logLayout.PerformLayout();
            mainMenuStrip.ResumeLayout(false);
            mainMenuStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.MenuStrip mainMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem archivoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem seleccionarExcelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem descargarPlantillaExcelToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator archivoToolStripSeparator;
        private System.Windows.Forms.ToolStripMenuItem salirToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configuracionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configuracionGeneralToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator configuracionToolStripSeparator;
        private System.Windows.Forms.ToolStripMenuItem mostrarVistaPreviaExcelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ayudaToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem acercaDeAutoWhatsAppToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Panel mainScrollPanel;
        private System.Windows.Forms.Label lblConfigurationSummary;
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
        private System.Windows.Forms.Button btnDownloadTemplate;
        private System.Windows.Forms.CheckBox chkShowExcelPreview;
        private System.Windows.Forms.Label lblFilePath;
        private System.Windows.Forms.Label lblExcelCompactSummary;
        private System.Windows.Forms.ToolTip uiToolTip;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button btnPauseResume;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnSendNow;
    }
}

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
            dateTimePicker = new DateTimePicker();
            btnPauseResume = new Button();
            btnCancel = new Button();
            btnSelectFile = new Button();
            lblFilePath = new Label();
            btnSend = new Button();
            progressBar1 = new ProgressBar();
            txtLog = new TextBox();
            openFileDialog1 = new OpenFileDialog();
            SuspendLayout();
            // 
            // dateTimePicker
            // 
            dateTimePicker.CustomFormat = "dd/MM/yyyy hh:mm tt";
            dateTimePicker.Format = DateTimePickerFormat.Custom;
            dateTimePicker.Location = new Point(330, 37);
            dateTimePicker.Name = "dateTimePicker";
            dateTimePicker.ShowUpDown = true;
            dateTimePicker.Size = new Size(200, 23);
            dateTimePicker.TabIndex = 0;

            schedulerTimer = new System.Windows.Forms.Timer();
            schedulerTimer.Interval = 1000;
            schedulerTimer.Tick += SchedulerTimer_Tick;

            // 
            // btnPauseResume
            // 
            btnPauseResume.Location = new Point(330, 105);
            btnPauseResume.Name = "btnPauseResume";
            btnPauseResume.Size = new Size(87, 30);
            btnPauseResume.TabIndex = 1;
            btnPauseResume.Text = "Pausar";
            btnPauseResume.Click += BtnPauseResume_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(436, 105);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(94, 30);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancelar";
            btnCancel.Click += BtnCancel_Click;
            // 
            // btnSelectFile
            // 
            btnSelectFile.Location = new Point(30, 30);
            btnSelectFile.Name = "btnSelectFile";
            btnSelectFile.Size = new Size(200, 30);
            btnSelectFile.TabIndex = 0;
            btnSelectFile.Text = "Seleccionar archivo Excel";
            btnSelectFile.UseVisualStyleBackColor = true;
            btnSelectFile.Click += btnSelectFile_Click;
            // 
            // lblFilePath
            // 
            lblFilePath.AutoSize = true;
            lblFilePath.Location = new Point(30, 75);
            lblFilePath.Name = "lblFilePath";
            lblFilePath.Size = new Size(126, 15);
            lblFilePath.TabIndex = 1;
            lblFilePath.Text = "Archivo seleccionado: ";
            // 
            // btnSend
            // 
            btnSend.Location = new Point(30, 105);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(200, 30);
            btnSend.TabIndex = 2;
            btnSend.Text = "Enviar Mensajes";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(30, 150);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(500, 23);
            progressBar1.TabIndex = 3;
            // 
            // txtLog
            // 
            txtLog.Location = new Point(30, 190);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(500, 200);
            txtLog.TabIndex = 4;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // Form1
            // 
            ClientSize = new Size(584, 421);
            Controls.Add(dateTimePicker);
            Controls.Add(btnPauseResume);
            Controls.Add(btnCancel);
            Controls.Add(txtLog);
            Controls.Add(progressBar1);
            Controls.Add(btnSend);
            Controls.Add(lblFilePath);
            Controls.Add(btnSelectFile);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Name = "Form1";
            Text = "WhatsApp Messenger App";
            FormClosing += Form1_FormClosing;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSelectFile;
        private System.Windows.Forms.Label lblFilePath;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.DateTimePicker dateTimePicker;
        private System.Windows.Forms.Button btnPauseResume;
        private System.Windows.Forms.Button btnCancel;

    }
}

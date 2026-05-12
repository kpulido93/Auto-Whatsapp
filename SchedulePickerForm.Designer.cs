namespace Automate_Whatsapp
{
    partial class SchedulePickerForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            mainLayout = new TableLayoutPanel();
            monthCalendarSchedule = new MonthCalendar();
            timeLayout = new TableLayoutPanel();
            lblHour = new Label();
            numScheduleHour = new NumericUpDown();
            lblMinute = new Label();
            numScheduleMinute = new NumericUpDown();
            lblSummary = new Label();
            actionsLayout = new FlowLayoutPanel();
            btnAccept = new Button();
            btnCancel = new Button();
            mainLayout.SuspendLayout();
            timeLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numScheduleHour).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numScheduleMinute).BeginInit();
            actionsLayout.SuspendLayout();
            SuspendLayout();
            //
            // mainLayout
            //
            mainLayout.ColumnCount = 1;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.Controls.Add(monthCalendarSchedule, 0, 0);
            mainLayout.Controls.Add(timeLayout, 0, 1);
            mainLayout.Controls.Add(lblSummary, 0, 2);
            mainLayout.Controls.Add(actionsLayout, 0, 3);
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Location = new Point(12, 12);
            mainLayout.Name = "mainLayout";
            mainLayout.RowCount = 4;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 176F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.Size = new Size(340, 286);
            mainLayout.TabIndex = 0;
            //
            // monthCalendarSchedule
            //
            monthCalendarSchedule.Anchor = AnchorStyles.None;
            monthCalendarSchedule.Location = new Point(56, 7);
            monthCalendarSchedule.MaxSelectionCount = 1;
            monthCalendarSchedule.Name = "monthCalendarSchedule";
            monthCalendarSchedule.TabIndex = 0;
            monthCalendarSchedule.DateChanged += monthCalendarSchedule_DateChanged;
            //
            // timeLayout
            //
            timeLayout.ColumnCount = 4;
            timeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            timeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72F));
            timeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56F));
            timeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            timeLayout.Controls.Add(lblHour, 0, 0);
            timeLayout.Controls.Add(numScheduleHour, 1, 0);
            timeLayout.Controls.Add(lblMinute, 2, 0);
            timeLayout.Controls.Add(numScheduleMinute, 3, 0);
            timeLayout.Dock = DockStyle.Fill;
            timeLayout.Location = new Point(0, 176);
            timeLayout.Margin = new Padding(0);
            timeLayout.Name = "timeLayout";
            timeLayout.RowCount = 1;
            timeLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            timeLayout.Size = new Size(340, 42);
            timeLayout.TabIndex = 1;
            //
            // lblHour
            //
            lblHour.Dock = DockStyle.Fill;
            lblHour.Location = new Point(3, 0);
            lblHour.Name = "lblHour";
            lblHour.Size = new Size(100, 42);
            lblHour.TabIndex = 0;
            lblHour.Text = "Hora:";
            lblHour.TextAlign = ContentAlignment.MiddleRight;
            //
            // numScheduleHour
            //
            numScheduleHour.Anchor = AnchorStyles.Left;
            numScheduleHour.Location = new Point(109, 9);
            numScheduleHour.Maximum = new decimal(new int[] { 23, 0, 0, 0 });
            numScheduleHour.Name = "numScheduleHour";
            numScheduleHour.Size = new Size(58, 23);
            numScheduleHour.TabIndex = 1;
            numScheduleHour.ValueChanged += scheduleTime_ValueChanged;
            //
            // lblMinute
            //
            lblMinute.Dock = DockStyle.Fill;
            lblMinute.Location = new Point(181, 0);
            lblMinute.Name = "lblMinute";
            lblMinute.Size = new Size(50, 42);
            lblMinute.TabIndex = 2;
            lblMinute.Text = "Min:";
            lblMinute.TextAlign = ContentAlignment.MiddleRight;
            //
            // numScheduleMinute
            //
            numScheduleMinute.Anchor = AnchorStyles.Left;
            numScheduleMinute.Location = new Point(237, 9);
            numScheduleMinute.Maximum = new decimal(new int[] { 59, 0, 0, 0 });
            numScheduleMinute.Name = "numScheduleMinute";
            numScheduleMinute.Size = new Size(58, 23);
            numScheduleMinute.TabIndex = 3;
            numScheduleMinute.ValueChanged += scheduleTime_ValueChanged;
            //
            // lblSummary
            //
            lblSummary.AutoEllipsis = true;
            lblSummary.Dock = DockStyle.Fill;
            lblSummary.ForeColor = Color.FromArgb(37, 99, 235);
            lblSummary.Location = new Point(3, 218);
            lblSummary.Name = "lblSummary";
            lblSummary.Size = new Size(334, 34);
            lblSummary.TabIndex = 2;
            lblSummary.Text = "Seleccionado:";
            lblSummary.TextAlign = ContentAlignment.MiddleLeft;
            //
            // actionsLayout
            //
            actionsLayout.Controls.Add(btnAccept);
            actionsLayout.Controls.Add(btnCancel);
            actionsLayout.Dock = DockStyle.Fill;
            actionsLayout.FlowDirection = FlowDirection.RightToLeft;
            actionsLayout.Location = new Point(0, 252);
            actionsLayout.Margin = new Padding(0);
            actionsLayout.Name = "actionsLayout";
            actionsLayout.Size = new Size(340, 34);
            actionsLayout.TabIndex = 3;
            actionsLayout.WrapContents = false;
            //
            // btnAccept
            //
            btnAccept.BackColor = Color.FromArgb(22, 163, 74);
            btnAccept.FlatAppearance.BorderSize = 0;
            btnAccept.FlatStyle = FlatStyle.Flat;
            btnAccept.ForeColor = Color.White;
            btnAccept.Location = new Point(244, 2);
            btnAccept.Margin = new Padding(6, 2, 0, 0);
            btnAccept.Name = "btnAccept";
            btnAccept.Size = new Size(96, 30);
            btnAccept.TabIndex = 0;
            btnAccept.Text = "Aceptar";
            btnAccept.UseVisualStyleBackColor = false;
            btnAccept.Click += btnAccept_Click;
            //
            // btnCancel
            //
            btnCancel.Location = new Point(142, 2);
            btnCancel.Margin = new Padding(0, 2, 6, 0);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(96, 30);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancelar";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            //
            // SchedulePickerForm
            //
            AcceptButton = btnAccept;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(364, 310);
            Controls.Add(mainLayout);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SchedulePickerForm";
            Padding = new Padding(12);
            StartPosition = FormStartPosition.CenterParent;
            Text = "Seleccionar fecha y hora";
            mainLayout.ResumeLayout(false);
            timeLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numScheduleHour).EndInit();
            ((System.ComponentModel.ISupportInitialize)numScheduleMinute).EndInit();
            actionsLayout.ResumeLayout(false);
            ResumeLayout(false);
        }

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.MonthCalendar monthCalendarSchedule;
        private System.Windows.Forms.TableLayoutPanel timeLayout;
        private System.Windows.Forms.Label lblHour;
        private System.Windows.Forms.NumericUpDown numScheduleHour;
        private System.Windows.Forms.Label lblMinute;
        private System.Windows.Forms.NumericUpDown numScheduleMinute;
        private System.Windows.Forms.Label lblSummary;
        private System.Windows.Forms.FlowLayoutPanel actionsLayout;
        private System.Windows.Forms.Button btnAccept;
        private System.Windows.Forms.Button btnCancel;
    }
}

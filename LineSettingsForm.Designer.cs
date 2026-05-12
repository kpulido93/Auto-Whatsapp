namespace Automate_Whatsapp
{
    partial class LineSettingsForm
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
            components = new System.ComponentModel.Container();
            mainLayout = new TableLayoutPanel();
            lblHeader = new Label();
            lblConfigPath = new Label();
            dgvLines = new DataGridView();
            colLineId = new DataGridViewTextBoxColumn();
            colLineName = new DataGridViewTextBoxColumn();
            colLineEnabled = new DataGridViewCheckBoxColumn();
            colLinePriority = new DataGridViewTextBoxColumn();
            colLineSessionPath = new DataGridViewTextBoxColumn();
            topActionsLayout = new FlowLayoutPanel();
            btnAddLine = new Button();
            btnEditLine = new Button();
            btnDeleteLine = new Button();
            btnMoveUp = new Button();
            btnMoveDown = new Button();
            footerLayout = new TableLayoutPanel();
            lblSummary = new Label();
            saveActionsLayout = new FlowLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            uiToolTip = new ToolTip(components);
            mainLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvLines).BeginInit();
            topActionsLayout.SuspendLayout();
            footerLayout.SuspendLayout();
            saveActionsLayout.SuspendLayout();
            SuspendLayout();
            //
            // mainLayout
            //
            mainLayout.ColumnCount = 1;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.Controls.Add(lblHeader, 0, 0);
            mainLayout.Controls.Add(lblConfigPath, 0, 1);
            mainLayout.Controls.Add(topActionsLayout, 0, 2);
            mainLayout.Controls.Add(dgvLines, 0, 3);
            mainLayout.Controls.Add(footerLayout, 0, 4);
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Location = new Point(14, 14);
            mainLayout.Name = "mainLayout";
            mainLayout.RowCount = 5;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            mainLayout.Size = new Size(792, 492);
            mainLayout.TabIndex = 0;
            //
            // lblHeader
            //
            lblHeader.Dock = DockStyle.Fill;
            lblHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblHeader.Location = new Point(3, 0);
            lblHeader.Name = "lblHeader";
            lblHeader.Size = new Size(786, 28);
            lblHeader.TabIndex = 0;
            lblHeader.Text = "Líneas autorizadas de WhatsApp";
            lblHeader.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblConfigPath
            //
            lblConfigPath.AutoEllipsis = true;
            lblConfigPath.Dock = DockStyle.Fill;
            lblConfigPath.ForeColor = Color.FromArgb(75, 85, 99);
            lblConfigPath.Location = new Point(3, 28);
            lblConfigPath.Name = "lblConfigPath";
            lblConfigPath.Size = new Size(786, 26);
            lblConfigPath.TabIndex = 1;
            lblConfigPath.Text = "Configuración local:";
            lblConfigPath.TextAlign = ContentAlignment.MiddleLeft;
            //
            // topActionsLayout
            //
            topActionsLayout.Controls.Add(btnAddLine);
            topActionsLayout.Controls.Add(btnEditLine);
            topActionsLayout.Controls.Add(btnDeleteLine);
            topActionsLayout.Controls.Add(btnMoveUp);
            topActionsLayout.Controls.Add(btnMoveDown);
            topActionsLayout.Dock = DockStyle.Fill;
            topActionsLayout.Location = new Point(0, 54);
            topActionsLayout.Margin = new Padding(0);
            topActionsLayout.Name = "topActionsLayout";
            topActionsLayout.Size = new Size(792, 42);
            topActionsLayout.TabIndex = 2;
            topActionsLayout.WrapContents = false;
            //
            // btnAddLine
            //
            btnAddLine.Location = new Point(3, 6);
            btnAddLine.Margin = new Padding(3, 6, 6, 3);
            btnAddLine.Name = "btnAddLine";
            btnAddLine.Size = new Size(112, 30);
            btnAddLine.TabIndex = 0;
            btnAddLine.Text = "Agregar";
            btnAddLine.UseVisualStyleBackColor = true;
            btnAddLine.Click += btnAddLine_Click;
            //
            // btnEditLine
            //
            btnEditLine.Location = new Point(124, 6);
            btnEditLine.Margin = new Padding(3, 6, 6, 3);
            btnEditLine.Name = "btnEditLine";
            btnEditLine.Size = new Size(92, 30);
            btnEditLine.TabIndex = 1;
            btnEditLine.Text = "Editar";
            btnEditLine.UseVisualStyleBackColor = true;
            btnEditLine.Click += btnEditLine_Click;
            //
            // btnDeleteLine
            //
            btnDeleteLine.Location = new Point(225, 6);
            btnDeleteLine.Margin = new Padding(3, 6, 6, 3);
            btnDeleteLine.Name = "btnDeleteLine";
            btnDeleteLine.Size = new Size(112, 30);
            btnDeleteLine.TabIndex = 2;
            btnDeleteLine.Text = "Eliminar";
            btnDeleteLine.UseVisualStyleBackColor = true;
            btnDeleteLine.Click += btnDeleteLine_Click;
            //
            // btnMoveUp
            //
            btnMoveUp.Location = new Point(346, 6);
            btnMoveUp.Margin = new Padding(3, 6, 6, 3);
            btnMoveUp.Name = "btnMoveUp";
            btnMoveUp.Size = new Size(92, 30);
            btnMoveUp.TabIndex = 3;
            btnMoveUp.Text = "Subir";
            btnMoveUp.UseVisualStyleBackColor = true;
            btnMoveUp.Click += btnMoveUp_Click;
            //
            // btnMoveDown
            //
            btnMoveDown.Location = new Point(447, 6);
            btnMoveDown.Margin = new Padding(3, 6, 6, 3);
            btnMoveDown.Name = "btnMoveDown";
            btnMoveDown.Size = new Size(92, 30);
            btnMoveDown.TabIndex = 4;
            btnMoveDown.Text = "Bajar";
            btnMoveDown.UseVisualStyleBackColor = true;
            btnMoveDown.Click += btnMoveDown_Click;
            //
            // dgvLines
            //
            dgvLines.AllowUserToAddRows = false;
            dgvLines.AllowUserToDeleteRows = false;
            dgvLines.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
            dgvLines.BackgroundColor = SystemColors.Window;
            dgvLines.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvLines.Columns.AddRange(new DataGridViewColumn[] { colLineId, colLineName, colLineEnabled, colLinePriority, colLineSessionPath });
            dgvLines.Dock = DockStyle.Fill;
            dgvLines.Location = new Point(3, 99);
            dgvLines.MultiSelect = false;
            dgvLines.Name = "dgvLines";
            dgvLines.RowHeadersVisible = false;
            dgvLines.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvLines.Size = new Size(786, 344);
            dgvLines.TabIndex = 3;
            dgvLines.CellValidating += dgvLines_CellValidating;
            dgvLines.CellValueChanged += dgvLines_CellValueChanged;
            dgvLines.CurrentCellDirtyStateChanged += dgvLines_CurrentCellDirtyStateChanged;
            dgvLines.DataError += dgvLines_DataError;
            dgvLines.SelectionChanged += dgvLines_SelectionChanged;
            //
            // colLineId
            //
            colLineId.DataPropertyName = "Id";
            colLineId.HeaderText = "Id";
            colLineId.MinimumWidth = 90;
            colLineId.Name = "colLineId";
            colLineId.ReadOnly = true;
            colLineId.Width = 120;
            //
            // colLineName
            //
            colLineName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colLineName.DataPropertyName = "DisplayName";
            colLineName.HeaderText = "Nombre visible";
            colLineName.MinimumWidth = 180;
            colLineName.Name = "colLineName";
            //
            // colLineEnabled
            //
            colLineEnabled.DataPropertyName = "Enabled";
            colLineEnabled.HeaderText = "Activa";
            colLineEnabled.MinimumWidth = 70;
            colLineEnabled.Name = "colLineEnabled";
            colLineEnabled.Width = 70;
            //
            // colLinePriority
            //
            colLinePriority.DataPropertyName = "Priority";
            colLinePriority.HeaderText = "Prioridad";
            colLinePriority.MinimumWidth = 80;
            colLinePriority.Name = "colLinePriority";
            colLinePriority.Width = 80;
            //
            // colLineSessionPath
            //
            colLineSessionPath.DataPropertyName = "SessionPath";
            colLineSessionPath.HeaderText = "Perfil local";
            colLineSessionPath.MinimumWidth = 160;
            colLineSessionPath.Name = "colLineSessionPath";
            colLineSessionPath.ReadOnly = true;
            colLineSessionPath.Width = 190;
            //
            // footerLayout
            //
            footerLayout.ColumnCount = 2;
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            footerLayout.Controls.Add(lblSummary, 0, 0);
            footerLayout.Controls.Add(saveActionsLayout, 1, 0);
            footerLayout.Dock = DockStyle.Fill;
            footerLayout.Location = new Point(0, 446);
            footerLayout.Margin = new Padding(0);
            footerLayout.Name = "footerLayout";
            footerLayout.RowCount = 1;
            footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            footerLayout.Size = new Size(792, 46);
            footerLayout.TabIndex = 4;
            //
            // lblSummary
            //
            lblSummary.AutoEllipsis = true;
            lblSummary.Dock = DockStyle.Fill;
            lblSummary.Location = new Point(3, 0);
            lblSummary.Name = "lblSummary";
            lblSummary.Size = new Size(566, 46);
            lblSummary.TabIndex = 0;
            lblSummary.Text = "Líneas configuradas: 0 | Habilitadas: 0";
            lblSummary.TextAlign = ContentAlignment.MiddleLeft;
            //
            // saveActionsLayout
            //
            saveActionsLayout.Controls.Add(btnSave);
            saveActionsLayout.Controls.Add(btnCancel);
            saveActionsLayout.Dock = DockStyle.Fill;
            saveActionsLayout.FlowDirection = FlowDirection.RightToLeft;
            saveActionsLayout.Location = new Point(572, 0);
            saveActionsLayout.Margin = new Padding(0);
            saveActionsLayout.Name = "saveActionsLayout";
            saveActionsLayout.Size = new Size(220, 46);
            saveActionsLayout.TabIndex = 1;
            saveActionsLayout.WrapContents = false;
            //
            // btnSave
            //
            btnSave.BackColor = Color.FromArgb(22, 163, 74);
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(112, 8);
            btnSave.Margin = new Padding(6, 8, 0, 3);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(108, 30);
            btnSave.TabIndex = 0;
            btnSave.Text = "Guardar";
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += btnSave_Click;
            //
            // btnCancel
            //
            btnCancel.Location = new Point(0, 8);
            btnCancel.Margin = new Padding(0, 8, 6, 3);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(100, 30);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancelar";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            //
            // LineSettingsForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(820, 520);
            Controls.Add(mainLayout);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(720, 460);
            Name = "LineSettingsForm";
            Padding = new Padding(14);
            StartPosition = FormStartPosition.CenterParent;
            Text = "Configurar líneas";
            mainLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvLines).EndInit();
            topActionsLayout.ResumeLayout(false);
            footerLayout.ResumeLayout(false);
            saveActionsLayout.ResumeLayout(false);
            ResumeLayout(false);
        }

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Label lblConfigPath;
        private System.Windows.Forms.FlowLayoutPanel topActionsLayout;
        private System.Windows.Forms.Button btnAddLine;
        private System.Windows.Forms.Button btnEditLine;
        private System.Windows.Forms.Button btnDeleteLine;
        private System.Windows.Forms.Button btnMoveUp;
        private System.Windows.Forms.Button btnMoveDown;
        private System.Windows.Forms.DataGridView dgvLines;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLineId;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLineName;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colLineEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLinePriority;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLineSessionPath;
        private System.Windows.Forms.TableLayoutPanel footerLayout;
        private System.Windows.Forms.Label lblSummary;
        private System.Windows.Forms.FlowLayoutPanel saveActionsLayout;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ToolTip uiToolTip;
    }
}

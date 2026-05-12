using Automate_Whatsapp.Logic;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace Automate_Whatsapp
{
    public partial class LineSettingsForm : Form
    {
        private readonly BindingList<LineSettingsRow> lineRows = new();
        private bool isInitializing = true;

        public LineSettingsForm()
        {
            InitializeComponent();

            try
            {
                dgvLines.AutoGenerateColumns = false;

                foreach (var line in WhatsAppLineManager.LoadLines())
                {
                    lineRows.Add(LineSettingsRow.FromLine(line));
                }

                dgvLines.DataSource = lineRows;
                lblConfigPath.Text = $"Configuración local: {WhatsAppLineManager.GetConfigPath()}";
                uiToolTip.SetToolTip(lblConfigPath, WhatsAppLineManager.GetConfigPath());
            }
            finally
            {
                isInitializing = false;
            }

            UpdateSummaryAndButtons();
        }

        private void btnAddLine_Click(object sender, EventArgs e)
        {
            string? displayName = PromptForLineName(this, "Agregar línea", "Nombre visible de la línea:");
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return;
            }

            string id = GenerateUniqueId(displayName, lineRows.Select(row => row.Id));
            int nextPriority = lineRows.Count == 0 ? 1 : lineRows.Max(row => row.Priority) + 1;

            var newRow = new LineSettingsRow
            {
                Id = id,
                DisplayName = displayName.Trim(),
                SessionPath = WhatsAppLineManager.GetDefaultSessionPath(id),
                ProfileDirectory = "Default",
                Enabled = true,
                Priority = nextPriority
            };

            lineRows.Add(newRow);
            SelectRow(lineRows.Count - 1);
            UpdateSummaryAndButtons();
        }

        private void btnDeleteLine_Click(object sender, EventArgs e)
        {
            var selectedRow = SelectedLineRow;
            if (selectedRow == null)
            {
                return;
            }

            if (selectedRow.Enabled && lineRows.Count(row => row.Enabled) == 1)
            {
                MessageBox.Show(
                    "No se puede eliminar la última línea habilitada. Desactiva o agrega otra línea antes de eliminarla.",
                    "Línea requerida",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Se quitará la línea \"{selectedRow.DisplayName}\" de la configuración.\n\nNo se borrará la carpeta local de ChromeUserData.",
                "Eliminar línea",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            int selectedIndex = dgvLines.CurrentRow?.Index ?? -1;
            lineRows.Remove(selectedRow);
            NormalizePrioritiesByCurrentOrder();
            SelectRow(Math.Min(selectedIndex, lineRows.Count - 1));
            UpdateSummaryAndButtons();
        }

        private void btnEditLine_Click(object sender, EventArgs e)
        {
            if (SelectedLineRow == null || dgvLines.CurrentRow == null)
            {
                return;
            }

            dgvLines.CurrentCell = dgvLines.CurrentRow.Cells[colLineName.Index];
            dgvLines.BeginEdit(true);
        }

        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            MoveSelectedRow(-1);
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            MoveSelectedRow(1);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            dgvLines.EndEdit();

            if (!ValidateRows())
            {
                return;
            }

            var lines = lineRows
                .Select(row => row.ToLine())
                .ToList();

            WhatsAppLineManager.SaveLines(lines);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void dgvLines_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            if (dgvLines.Columns[e.ColumnIndex] == colLineName
                && string.IsNullOrWhiteSpace(Convert.ToString(e.FormattedValue)))
            {
                dgvLines.Rows[e.RowIndex].ErrorText = "El nombre visible es obligatorio.";
                e.Cancel = true;
                return;
            }

            if (dgvLines.Columns[e.ColumnIndex] == colLinePriority)
            {
                string value = Convert.ToString(e.FormattedValue) ?? "";
                if (!int.TryParse(value, out int priority) || priority < 1)
                {
                    dgvLines.Rows[e.RowIndex].ErrorText = "La prioridad debe ser un número mayor o igual a 1.";
                    e.Cancel = true;
                    return;
                }
            }

            dgvLines.Rows[e.RowIndex].ErrorText = "";
        }

        private void dgvLines_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (isInitializing)
            {
                return;
            }

            UpdateSummaryAndButtons();
        }

        private void dgvLines_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvLines.IsCurrentCellDirty)
            {
                dgvLines.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void dgvLines_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            if (e.RowIndex >= 0 && e.RowIndex < dgvLines.Rows.Count)
            {
                dgvLines.Rows[e.RowIndex].ErrorText = "Revisa el valor ingresado.";
            }
        }

        private void dgvLines_SelectionChanged(object sender, EventArgs e)
        {
            if (isInitializing)
            {
                return;
            }

            UpdateSummaryAndButtons();
        }

        private LineSettingsRow? SelectedLineRow =>
            dgvLines.CurrentRow?.DataBoundItem as LineSettingsRow;

        private bool ValidateRows()
        {
            if (lineRows.Count == 0)
            {
                MessageBox.Show(
                    "Debe existir al menos una línea configurada.",
                    "Configuración incompleta",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            var emptyName = lineRows.FirstOrDefault(row => string.IsNullOrWhiteSpace(row.DisplayName));
            if (emptyName != null)
            {
                MessageBox.Show(
                    "No se puede guardar una línea sin nombre visible.",
                    "Nombre requerido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            var duplicateId = lineRows
                .GroupBy(row => row.Id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicateId != null)
            {
                MessageBox.Show(
                    $"El Id \"{duplicateId.Key}\" está duplicado. Cada línea debe tener un Id único.",
                    "Id duplicado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            var invalidPriority = lineRows.FirstOrDefault(row => row.Priority < 1);
            if (invalidPriority != null)
            {
                MessageBox.Show(
                    "La prioridad debe ser un número mayor o igual a 1.",
                    "Prioridad inválida",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            if (!lineRows.Any(row => row.Enabled))
            {
                MessageBox.Show(
                    "Debe quedar al menos una línea habilitada para poder enviar mensajes.",
                    "Línea habilitada requerida",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void MoveSelectedRow(int direction)
        {
            int currentIndex = dgvLines.CurrentRow?.Index ?? -1;
            int targetIndex = currentIndex + direction;

            if (currentIndex < 0 || targetIndex < 0 || targetIndex >= lineRows.Count)
            {
                return;
            }

            var row = lineRows[currentIndex];
            lineRows.RemoveAt(currentIndex);
            lineRows.Insert(targetIndex, row);
            NormalizePrioritiesByCurrentOrder();
            SelectRow(targetIndex);
            UpdateSummaryAndButtons();
        }

        private void NormalizePrioritiesByCurrentOrder()
        {
            for (int i = 0; i < lineRows.Count; i++)
            {
                lineRows[i].Priority = i + 1;
            }

            dgvLines.Refresh();
        }

        private void SelectRow(int index)
        {
            if (index < 0 || index >= dgvLines.Rows.Count)
            {
                return;
            }

            dgvLines.ClearSelection();
            dgvLines.Rows[index].Selected = true;
            dgvLines.CurrentCell = dgvLines.Rows[index].Cells[colLineName.Index];
        }

        private void UpdateSummaryAndButtons()
        {
            if (isInitializing || lblSummary == null || dgvLines == null)
            {
                return;
            }

            int total = lineRows.Count;
            int enabled = lineRows.Count(row => row.Enabled);

            lblSummary.Text = $"Líneas configuradas: {total} | Habilitadas: {enabled}";

            int selectedIndex = dgvLines.CurrentRow?.Index ?? -1;
            bool hasSelectedRow = selectedIndex >= 0
                && selectedIndex < total
                && dgvLines.CurrentRow?.DataBoundItem is LineSettingsRow;

            btnEditLine.Enabled = hasSelectedRow;
            btnDeleteLine.Enabled = hasSelectedRow;
            btnMoveUp.Enabled = hasSelectedRow && selectedIndex > 0;
            btnMoveDown.Enabled = hasSelectedRow && selectedIndex < total - 1;
        }

        private static string? PromptForLineName(IWin32Window owner, string title, string label)
        {
            using var dialog = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(420, 128)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var promptLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = label,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 0, 6)
            };

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            var okButton = new Button
            {
                Text = "Aceptar",
                DialogResult = DialogResult.OK,
                Size = new Size(92, 30)
            };

            var cancelButton = new Button
            {
                Text = "Cancelar",
                DialogResult = DialogResult.Cancel,
                Size = new Size(92, 30)
            };

            actions.Controls.Add(okButton);
            actions.Controls.Add(cancelButton);
            layout.Controls.Add(promptLabel, 0, 0);
            layout.Controls.Add(textBox, 0, 1);
            layout.Controls.Add(actions, 0, 2);
            dialog.Controls.Add(layout);
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;

            return dialog.ShowDialog(owner) == DialogResult.OK
                ? textBox.Text.Trim()
                : null;
        }

        private static string GenerateUniqueId(string displayName, IEnumerable<string> existingIds)
        {
            string baseId = GenerateSafeId(displayName);
            var existing = new HashSet<string>(existingIds, StringComparer.OrdinalIgnoreCase);

            string id = baseId;
            int suffix = 2;
            while (existing.Contains(id))
            {
                id = $"{baseId}-{suffix}";
                suffix++;
            }

            return id;
        }

        private static string GenerateSafeId(string displayName)
        {
            string normalized = displayName.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            bool previousDash = false;

            foreach (char character in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                    previousDash = false;
                    continue;
                }

                if (!previousDash && builder.Length > 0)
                {
                    builder.Append('-');
                    previousDash = true;
                }
            }

            string safeId = builder.ToString().Trim('-');
            return string.IsNullOrWhiteSpace(safeId) ? $"linea-{Guid.NewGuid():N}"[..14] : safeId;
        }

        private sealed class LineSettingsRow
        {
            public string Id { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string SessionPath { get; set; } = "";
            public string ProfileDirectory { get; set; } = "Default";
            public bool Enabled { get; set; }
            public int Priority { get; set; }

            public static LineSettingsRow FromLine(WhatsAppLine line)
            {
                return new LineSettingsRow
                {
                    Id = line.Id,
                    DisplayName = line.DisplayName,
                    SessionPath = line.SessionPath,
                    ProfileDirectory = line.ProfileDirectory,
                    Enabled = line.Enabled,
                    Priority = line.Priority
                };
            }

            public WhatsAppLine ToLine()
            {
                return new WhatsAppLine
                {
                    Id = Id.Trim(),
                    DisplayName = DisplayName.Trim(),
                    SessionPath = SessionPath.Trim(),
                    ProfileDirectory = string.IsNullOrWhiteSpace(ProfileDirectory)
                        ? "Default"
                        : ProfileDirectory.Trim(),
                    Enabled = Enabled,
                    Priority = Priority
                };
            }
        }
    }
}

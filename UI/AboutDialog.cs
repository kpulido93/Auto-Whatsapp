using System.Diagnostics;
using System.Reflection;

namespace Automate_Whatsapp
{
    internal sealed class AboutDialog : Form
    {
        public AboutDialog(Icon? applicationIcon)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string assemblyPath = FirstNonEmpty(assembly.Location, Application.ExecutablePath);
            var versionInfo = FileVersionInfo.GetVersionInfo(assemblyPath);
            string version = FirstNonEmpty(
                assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                versionInfo.ProductVersion,
                Application.ProductVersion,
                assembly.GetName().Version?.ToString(),
                "desconocida");
            string authors = GetAssemblyMetadata(assembly, "Authors");
            string company = FirstNonEmpty(
                versionInfo.CompanyName,
                Application.CompanyName,
                assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company);
            string attribution = BuildAttribution(authors, company);
            string description = FirstNonEmpty(
                assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description,
                versionInfo.Comments,
                versionInfo.FileDescription,
                "Automatización de envíos de WhatsApp desde archivos Excel.");

            Text = "Acerca de AutoWhatsApp";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(460, 240);

            if (applicationIcon != null)
            {
                Icon = (Icon)applicationIcon.Clone();
            }

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(18)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

            var iconBox = new PictureBox
            {
                Dock = DockStyle.Top,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Image = applicationIcon?.ToBitmap()
            };
            layout.Controls.Add(iconBox, 0, 0);
            layout.SetRowSpan(iconBox, 4);

            layout.Controls.Add(new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Text = "AutoWhatsApp",
                TextAlign = ContentAlignment.MiddleLeft
            }, 1, 0);

            layout.Controls.Add(new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = $"Versión instalada: {version}",
                TextAlign = ContentAlignment.MiddleLeft
            }, 1, 1);

            layout.Controls.Add(new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = $"Autor/compañía: {attribution}",
                TextAlign = ContentAlignment.MiddleLeft
            }, 1, 2);

            layout.Controls.Add(new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = description,
                TextAlign = ContentAlignment.TopLeft
            }, 1, 3);

            var okButton = new Button
            {
                DialogResult = DialogResult.OK,
                Text = "Aceptar",
                Size = new Size(96, 32)
            };

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            actions.Controls.Add(okButton);
            layout.Controls.Add(actions, 0, 4);
            layout.SetColumnSpan(actions, 2);

            AcceptButton = okButton;
            Controls.Add(layout);
        }

        private static string GetAssemblyMetadata(Assembly assembly, string key)
        {
            foreach (var attribute in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
            {
                if (string.Equals(attribute.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return attribute.Value ?? "";
                }
            }

            return "";
        }

        private static string BuildAttribution(string? authors, string? company)
        {
            string authorValue = FirstNonEmpty(authors);
            string companyValue = FirstNonEmpty(company);

            if (string.IsNullOrWhiteSpace(authorValue))
            {
                return FirstNonEmpty(companyValue, "No disponible");
            }

            if (string.IsNullOrWhiteSpace(companyValue)
                || string.Equals(authorValue, companyValue, StringComparison.OrdinalIgnoreCase))
            {
                return authorValue;
            }

            return $"{authorValue} / {companyValue}";
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (string? value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return "";
        }
    }
}

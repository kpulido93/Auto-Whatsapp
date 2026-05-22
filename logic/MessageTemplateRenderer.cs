using System.Text.RegularExpressions;

namespace Automate_Whatsapp.Logic;

public static partial class MessageTemplateRenderer
{
    public static string Render(string? templateBody, string? banco, string? nombreDeudor)
    {
        if (string.IsNullOrEmpty(templateBody))
        {
            return string.Empty;
        }

        string resolvedBanco = NormalizeReplacement(banco);
        string resolvedNombreDeudor = NormalizeReplacement(nombreDeudor);

        return SupportedPlaceholdersRegex().Replace(
            templateBody,
            match => match.Groups[1].Value.Equals("Banco", StringComparison.OrdinalIgnoreCase)
                ? resolvedBanco
                : resolvedNombreDeudor);
    }

    private static string NormalizeReplacement(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
    }

    [GeneratedRegex(@"\{(Banco|NombreDeudor)\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SupportedPlaceholdersRegex();
}

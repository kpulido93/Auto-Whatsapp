namespace Automate_Whatsapp.Logic;

public sealed record MessageTemplateValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors);

public static class MessageTemplateValidator
{
    public const int MinimumTemplateCount = 3;

    public static MessageTemplateValidationResult Validate(IEnumerable<MessageTemplate> templates)
    {
        ArgumentNullException.ThrowIfNull(templates);

        MessageTemplate[] templateList = templates.ToArray();
        var errors = new List<string>();

        if (templateList.Length < MinimumTemplateCount)
        {
            errors.Add($"Debe haber al menos {MinimumTemplateCount} plantillas.");
        }

        for (int index = 0; index < templateList.Length; index++)
        {
            MessageTemplate template = templateList[index];

            if (template.Number <= 0)
            {
                errors.Add($"La plantilla en la fila {index + 1} debe tener un número mayor a 0.");
            }

            if (template.Enabled && string.IsNullOrWhiteSpace(template.Body))
            {
                errors.Add($"La plantilla {DescribeTemplate(template, index)} está habilitada y debe tener cuerpo.");
            }
        }

        int[] duplicateNumbers = templateList
            .Where(template => template.Number > 0)
            .GroupBy(template => template.Number)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(number => number)
            .ToArray();

        if (duplicateNumbers.Length > 0)
        {
            errors.Add($"No se permiten números de plantilla duplicados: {string.Join(", ", duplicateNumbers)}.");
        }

        return new MessageTemplateValidationResult(errors.Count == 0, errors);
    }

    private static string DescribeTemplate(MessageTemplate template, int index)
    {
        return template.Number > 0
            ? template.Number.ToString()
            : $"en la fila {index + 1}";
    }
}

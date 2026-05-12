namespace Automate_Whatsapp.Logic;

public sealed record WhatsAppSendRunSummary(
    int Processed,
    int Total,
    int Sent,
    int Invalid,
    int Failed,
    int Skipped,
    int LineChanges,
    string FinalLineName,
    bool Cancelled,
    bool StoppedByGlobalFailure,
    string GlobalFailureReason)
{
    public int Pending => Math.Max(Total - Processed, 0);

    public bool CompletedSuccessfully =>
        !Cancelled && !StoppedByGlobalFailure && Processed == Total;

    public string DisplaySummary =>
        $"Procesados: {Processed}. Enviados: {Sent}. Inválidos: {Invalid}. Fallidos: {Failed}. Línea final: {FinalLineName}.";
}

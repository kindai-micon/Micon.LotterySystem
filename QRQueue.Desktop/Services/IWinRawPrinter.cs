namespace QRQueue.Desktop.Services;

public interface IWinRawPrinter
{
    bool CanOpen(string printerName, out string? errorMessage);

    void Print(string printerName, string documentName, byte[] data);
}
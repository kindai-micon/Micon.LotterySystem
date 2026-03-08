namespace Micon.LotterySystem.Desktop.Models;

public enum PrintStage
{
    None = 0,
    QrFetch = 1,
    Render = 2,
    SendToPrinter = 3,
    Complete = 4
}

public class PrintResult
{
    public bool IsSuccess { get; init; }

    public PrintStage Stage { get; init; } = PrintStage.None;

    public string Message { get; init; } = string.Empty;

    public bool IsPrinted { get; init; }

    public bool CanRetry { get; init; }

    public static PrintResult Success(string message = "印刷に成功しました")
    {
        return new PrintResult
        {
            IsSuccess = true,
            Stage = PrintStage.None,
            Message = message,
            IsPrinted = true,
            CanRetry = false
        };
    }

    public static PrintResult Fail(
        PrintStage stage,
        string message,
        bool isPrinted = false,
        bool canRetry = true)
    {
        return new PrintResult
        {
            IsSuccess = false,
            Stage = stage,
            Message = message,
            IsPrinted = isPrinted,
            CanRetry = canRetry
        };
    }
}
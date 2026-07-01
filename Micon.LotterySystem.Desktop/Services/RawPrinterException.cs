using System;

namespace Micon.LotterySystem.Desktop.Services;

public class RawPrinterException : Exception
{
    public int? Win32ErrorCode { get; }

    public RawPrinterException(string message)
        : base(message)
    {
    }

    public RawPrinterException(string message, int? win32ErrorCode)
        : base(message)
    {
        Win32ErrorCode = win32ErrorCode;
    }

    public RawPrinterException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
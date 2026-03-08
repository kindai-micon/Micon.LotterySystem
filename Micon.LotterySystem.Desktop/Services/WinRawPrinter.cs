using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace Micon.LotterySystem.Desktop.Services;

[SupportedOSPlatform("windows")]
public class WinRawPrinter : IWinRawPrinter
{
    private readonly ILogger<WinRawPrinter> _logger;

    public WinRawPrinter(ILogger<WinRawPrinter> logger)
    {
        _logger = logger;
    }

    public bool CanOpen(string printerName, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(printerName))
        {
            errorMessage = "プリンタ名が設定されていません。";
            return false;
        }

        if (!OpenPrinter(printerName, out var printerHandle, IntPtr.Zero))
        {
            var win32Error = Marshal.GetLastWin32Error();
            errorMessage = $"プリンタ '{printerName}' を開けませんでした。Win32Error={win32Error}";
            _logger.LogWarning("Failed to open printer {PrinterName}. Win32Error={Win32Error}", printerName, win32Error);
            return false;
        }

        try
        {
            return true;
        }
        finally
        {
            ClosePrinter(printerHandle);
        }
    }

    public void Print(string printerName, string documentName, byte[] data)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            throw new RawPrinterException("プリンタ名が設定されていません。");
        }

        if (string.IsNullOrWhiteSpace(documentName))
        {
            throw new RawPrinterException("ドキュメント名が設定されていません。");
        }

        if (data is null || data.Length == 0)
        {
            throw new RawPrinterException("印刷データが空です。");
        }

        IntPtr printerHandle = IntPtr.Zero;
        var docStarted = false;
        var pageStarted = false;

        try
        {
            if (!OpenPrinter(printerName, out printerHandle, IntPtr.Zero))
            {
                ThrowLastWin32Error($"プリンタ '{printerName}' を開けませんでした。");
            }

            var docInfo = new DOC_INFO_1
            {
                pDocName = documentName,
                pDataType = "RAW"
            };

            var jobId = StartDocPrinter(printerHandle, 1, docInfo);
            if (jobId == 0)
            {
                ThrowLastWin32Error("印刷ドキュメントの開始に失敗しました。");
            }

            docStarted = true;

            if (!StartPagePrinter(printerHandle))
            {
                ThrowLastWin32Error("印刷ページの開始に失敗しました。");
            }

            pageStarted = true;

            if (!WritePrinter(printerHandle, data, data.Length, out var writtenBytes))
            {
                ThrowLastWin32Error("プリンタへのデータ送信に失敗しました。");
            }

            if (writtenBytes != data.Length)
            {
                throw new RawPrinterException(
                    $"プリンタへの送信バイト数が一致しません。expected={data.Length}, actual={writtenBytes}");
            }

            _logger.LogInformation(
                "Raw print succeeded. Printer={PrinterName}, DocumentName={DocumentName}, Bytes={Bytes}",
                printerName,
                documentName,
                data.Length);
        }
        catch (RawPrinterException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected raw print error. Printer={PrinterName}, DocumentName={DocumentName}", printerName, documentName);
            throw new RawPrinterException("RAW 印刷処理で予期しないエラーが発生しました。", ex);
        }
        finally
        {
            if (printerHandle != IntPtr.Zero)
            {
                if (pageStarted)
                {
                    EndPagePrinter(printerHandle);
                }

                if (docStarted)
                {
                    EndDocPrinter(printerHandle);
                }

                ClosePrinter(printerHandle);
            }
        }
    }

    private static void ThrowLastWin32Error(string message)
    {
        var win32Error = Marshal.GetLastWin32Error();
        var detail = new Win32Exception(win32Error).Message;
        throw new RawPrinterException($"{message} Win32Error={win32Error}: {detail}", win32Error);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DOC_INFO_1
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pDocName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pOutputFile;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pDataType;
    }

    [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenPrinter(
        string pPrinterName,
        out IntPtr phPrinter,
        IntPtr pDefault);

    [DllImport("winspool.drv", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int StartDocPrinter(
        IntPtr hPrinter,
        int level,
        [In] DOC_INFO_1 di);

    [DllImport("winspool.drv", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WritePrinter(
        IntPtr hPrinter,
        byte[] pBytes,
        int dwCount,
        out int dwWritten);
}
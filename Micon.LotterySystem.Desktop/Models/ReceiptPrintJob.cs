using System;
using System.Collections.Generic;

namespace Micon.LotterySystem.Desktop.Models;

public class ReceiptPrintJob
{
    public string LotteryGroupName { get; init; } = string.Empty;

    public long TicketNumber { get; init; }

    public Guid TicketDisplayId { get; init; }

    public DateTimeOffset IssuedAt { get; init; }

    public byte[] QrCodePngBytes { get; init; } = Array.Empty<byte>();

    public bool ActivateOnIssue { get; init; }

    public string PrinterName { get; init; } = string.Empty;

    public IReadOnlyList<string> WarningLines { get; init; } = Array.Empty<string>();

    public string FooterText { get; init; } = string.Empty;
}
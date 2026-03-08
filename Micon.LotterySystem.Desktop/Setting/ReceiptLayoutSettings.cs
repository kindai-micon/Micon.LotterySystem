using System.Collections.Generic;

namespace Micon.LotterySystem.Desktop.Settings;

public class ReceiptLayoutSettings
{
    public int PaperWidthPx { get; set; } = 576;

    public int QrSizePx { get; set; } = 220;

    public int MarginLeft { get; set; } = 24;

    public int MarginRight { get; set; } = 24;

    public int MarginTop { get; set; } = 24;

    public int MarginBottom { get; set; } = 24;

    public float TitleFontSize { get; set; } = 28;

    public float NumberFontSize { get; set; } = 42;

    public float BodyFontSize { get; set; } = 22;

    public float FooterFontSize { get; set; } = 18;

    public int Threshold { get; set; } = 160;

    public List<string> WarningLines { get; set; } = new()
    {
        "本券は大切に保管してください",
        "抽選時までお持ちください"
    };

    public string FooterText { get; set; } = "Micon Lottery System";
}
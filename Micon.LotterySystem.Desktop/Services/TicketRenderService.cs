using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Micon.LotterySystem.Desktop.Models;
using Micon.LotterySystem.Desktop.Settings;
using SkiaSharp;

namespace Micon.LotterySystem.Desktop.Services;

public class TicketRenderService : ITicketRenderService
{
    private readonly ReceiptLayoutSettings _layoutSettings;
    private readonly PrinterSettings _printerSettings;
    private readonly ILogger<TicketRenderService> _logger;

    public TicketRenderService(
        ReceiptLayoutSettings layoutSettings,
        PrinterSettings printerSettings,
        ILogger<TicketRenderService> logger)
    {
        _layoutSettings = layoutSettings;
        _printerSettings = printerSettings;
        _logger = logger;
    }

    public byte[] RenderEscPos(ReceiptPrintJob printJob)
    {
        try
        {
            Validate(printJob);

            using var qrBitmap = SKBitmap.Decode(printJob.QrCodePngBytes);
            if (qrBitmap is null)
            {
                throw new TicketRenderException("QRコードPNGの読み込みに失敗しました。");
            }

            var canvasWidth = _layoutSettings.PaperWidthPx;
            var contentWidth = canvasWidth - _layoutSettings.MarginLeft - _layoutSettings.MarginRight;

            var titlePaint = CreateTextPaint(_layoutSettings.TitleFontSize, true);
            var numberPaint = CreateTextPaint(_layoutSettings.NumberFontSize, true);
            var bodyPaint = CreateTextPaint(_layoutSettings.BodyFontSize, false);
            var footerPaint = CreateTextPaint(_layoutSettings.FooterFontSize, false);
            var linePaint = new SKPaint
            {
                Color = SKColors.Black,
                StrokeWidth = 2,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            };

            var warningLines = (printJob.WarningLines?.Count > 0
                    ? printJob.WarningLines
                    : Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            var footerText = string.IsNullOrWhiteSpace(printJob.FooterText)
                ? string.Empty
                : printJob.FooterText.Trim();

            var titleLine1 = string.IsNullOrWhiteSpace(printJob.LotteryGroupName)
                ? "抽選会"
                : printJob.LotteryGroupName.Trim();

            const string titleLine2 = "抽選券";
            var numberText = $"No.{printJob.TicketNumber}";

            var topMargin = _layoutSettings.MarginTop;
            var bottomMargin = _layoutSettings.MarginBottom;
            var spacingSmall = 10;
            var spacingMedium = 18;
            var spacingLarge = 28;

            var titleLine1Height = MeasureTextHeight(titlePaint);
            var titleLine2Height = MeasureTextHeight(titlePaint);
            var numberHeight = MeasureTextHeight(numberPaint);
            var bodyHeight = MeasureTextHeight(bodyPaint);
            var footerHeight = MeasureTextHeight(footerPaint);
            var qrSize = _layoutSettings.QrSizePx;

            var totalHeight =
                topMargin +
                titleLine1Height +
                spacingSmall +
                titleLine2Height +
                spacingMedium +
                10 + // 区切り線領域
                spacingMedium +
                numberHeight +
                spacingLarge +
                qrSize +
                spacingLarge +
                (warningLines.Count * (bodyHeight + spacingSmall)) +
                (string.IsNullOrWhiteSpace(footerText) ? 0 : footerHeight + spacingMedium) +
                bottomMargin;

            using var surface = SKSurface.Create(new SKImageInfo(canvasWidth, totalHeight, SKColorType.Bgra8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            var y = topMargin;

            y = DrawCenteredText(canvas, titleLine1, titlePaint, y);
            y += spacingSmall;
            y = DrawCenteredText(canvas, titleLine2, titlePaint, y);

            y += spacingMedium;
            canvas.DrawLine(
                _layoutSettings.MarginLeft,
                y,
                canvasWidth - _layoutSettings.MarginRight,
                y,
                linePaint);

            y += spacingMedium;
            y = DrawCenteredText(canvas, numberText, numberPaint, y);

            y += spacingLarge;

            var qrRect = CalculateCenteredRect(canvasWidth, y, qrSize, qrSize);
            using (var qrPaint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High })
            {
                canvas.DrawBitmap(qrBitmap, qrRect, qrPaint);
            }

            y += qrSize;
            y += spacingLarge;

            foreach (var line in warningLines)
            {
                y = DrawCenteredText(canvas, line, bodyPaint, y);
                y += spacingSmall;
            }

            if (!string.IsNullOrWhiteSpace(footerText))
            {
                y += spacingSmall;
                y = DrawCenteredText(canvas, footerText, footerPaint, y);
            }

            using var image = surface.Snapshot();
            using var bitmap = SKBitmap.FromImage(image);

            var escPosBytes = ConvertBitmapToEscPos(bitmap, _layoutSettings.Threshold, _printerSettings.CutEnabled);

            _logger.LogInformation(
                "ESC/POS render succeeded. TicketNumber={TicketNumber}, DisplayId={DisplayId}, Bytes={Bytes}",
                printJob.TicketNumber,
                printJob.TicketDisplayId,
                escPosBytes.Length);

            return escPosBytes;
        }
        catch (TicketRenderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ticket render failed. TicketNumber={TicketNumber}, DisplayId={DisplayId}",
                printJob.TicketNumber,
                printJob.TicketDisplayId);

            throw new TicketRenderException("抽選券の券面生成に失敗しました。", ex);
        }
    }

    private static void Validate(ReceiptPrintJob printJob)
    {
        if (printJob is null)
        {
            throw new TicketRenderException("印刷ジョブが null です。");
        }

        if (printJob.QrCodePngBytes is null || printJob.QrCodePngBytes.Length == 0)
        {
            throw new TicketRenderException("QRコードPNGデータが空です。");
        }

        if (printJob.TicketNumber <= 0)
        {
            throw new TicketRenderException("抽選番号が不正です。");
        }
    }

    private static SKPaint CreateTextPaint(float fontSize, bool bold)
    {
        var typeface =
            SKTypeface.FromFamilyName("Meiryo", bold ? SKFontStyle.Bold : SKFontStyle.Normal) ??
            SKTypeface.FromFamilyName("Yu Gothic UI", bold ? SKFontStyle.Bold : SKFontStyle.Normal) ??
            SKTypeface.Default;

        return new SKPaint
        {
            Color = SKColors.Black,
            TextSize = fontSize,
            IsAntialias = true,
            Typeface = typeface,
            TextAlign = SKTextAlign.Center
        };
    }

    private static int MeasureTextHeight(SKPaint paint)
    {
        var metrics = paint.FontMetrics;
        return (int)Math.Ceiling(metrics.Descent - metrics.Ascent);
    }

    private static int DrawCenteredText(SKCanvas canvas, string text, SKPaint paint, int topY)
    {
        var metrics = paint.FontMetrics;
        var baseline = topY - metrics.Ascent;
        canvas.DrawText(text, canvas.LocalClipBounds.MidX, baseline, paint);
        return (int)Math.Ceiling(baseline + metrics.Descent);
    }

    private static SKRect CalculateCenteredRect(int canvasWidth, int topY, int width, int height)
    {
        var left = (canvasWidth - width) / 2f;
        return new SKRect(left, topY, left + width, topY + height);
    }

    private static byte[] ConvertBitmapToEscPos(SKBitmap bitmap, int threshold, bool cutEnabled)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        var widthBytes = (width + 7) / 8;
        var rasterData = new byte[widthBytes * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                var luminance = (0.299 * color.Red) + (0.587 * color.Green) + (0.114 * color.Blue);
                var isBlack = luminance < threshold;

                if (!isBlack)
                {
                    continue;
                }

                var index = (y * widthBytes) + (x / 8);
                rasterData[index] |= (byte)(0x80 >> (x % 8));
            }
        }

        var result = new List<byte>(8 + rasterData.Length + 16)
        {
            0x1B, 0x40,             // ESC @ 初期化
            0x1B, 0x61, 0x01,       // ESC a 1 中央寄せ
            0x1D, 0x76, 0x30, 0x00, // GS v 0
            (byte)(widthBytes & 0xFF),
            (byte)((widthBytes >> 8) & 0xFF),
            (byte)(height & 0xFF),
            (byte)((height >> 8) & 0xFF)
        };

        result.AddRange(rasterData);

        // 給紙
        result.Add(0x0A);
        result.Add(0x0A);
        result.Add(0x0A);
        result.Add(0x0A);

        if (cutEnabled)
        {
            // GS V B 0
            result.Add(0x1D);
            result.Add(0x56);
            result.Add(0x42);
            result.Add(0x00);
        }

        return result.ToArray();
    }
}
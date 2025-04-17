using System.Drawing;
using System.Drawing.Imaging;
using ZXing;
using ZXing.Common;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Micon.LotterySystem.Models;

public class TicketPdfGenerator
{
    public byte[] GenerateTicketsPdf(List<TicketInfo> tickets)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        using var stream = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Grid(grid =>
                {
                    grid.Columns(2);
                    grid.Spacing(10);

                    foreach (var ticket in tickets)
                    {
                        grid.Item().Border(1).Padding(8).Height(150).Width(250).Column(ticketCol =>
                        {
                            // 1. タイトル：中央寄せ
                            ticketCol.Item().AlignCenter().Text(ticket.Name).FontSize(16).SemiBold();

                            // 2. 中段：QRコード + 番号
                            ticketCol.Item().Row(row =>
                            {
                                row.Spacing(5);

                                // 左側：抽選番号（下寄せ）
                                row.RelativeItem().AlignBottom().Text($"抽選番号：No.{ticket.TicketNumber}")
                                    .FontSize(16).SemiBold();

                                // 右側：QRコード（右寄せ・サイズ指定）
                                row.ConstantItem(100).AlignRight().Height(80).Image(GenerateQrCode(ticket.Url), ImageScaling.FitHeight);
                            });

                            // 3. 下段：説明と注意
                            ticketCol.Item().PaddingTop(5).Column(bottom =>
                            {
                                bottom.Item().Text(ticket.Description).FontSize(9);
                                bottom.Item().Text(ticket.Warning).FontSize(8);
                            });
                        });
                    }
                });
            });
        }).GeneratePdf(stream);

        return stream.ToArray();
    }

    private byte[] GenerateQrCode(string text)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Height = 150,
                Width = 150,
                Margin = 1
            }
        };

        var pixelData = writer.Write(text);

        using var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb);
        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                                         ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
        try
        {
            System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        using var ms = new MemoryStream();
        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return ms.ToArray();
    }
}
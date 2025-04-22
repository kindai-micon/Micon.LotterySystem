using ZXing;
using ZXing.Common;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Micon.LotterySystem.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using QuestPDF.Helpers;
using QuestPDF.Drawing;

public class TicketPdfGenerator
{
    public byte[] GenerateTicketsPdf(List<TicketInfo> tickets)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.FontDiscoveryPaths.Add(Path.Combine(Directory.GetCurrentDirectory(),"fonts"));
        FontManager.RegisterFont(File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), "fonts", "NotoSansJP.ttf")));
        using var stream = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Noto Sans JP"));

                page.Content().Grid(grid =>
                {
                    grid.Columns(2);
                    grid.Spacing(10);

                    foreach (var ticket in tickets)
                    {
                        grid.Item().Border(1).Padding(8).Height(150).Width(250).Column(ticketCol =>
                        {
                            // タイトル
                            ticketCol.Item().AlignCenter().Text(ticket.Name).FontSize(16).FontFamily("Noto Sans JP SemiBold");

                            // 中段：QR + 番号
                            ticketCol.Item().Row(row =>
                            {
                                row.Spacing(5);
                                row.RelativeItem().AlignBottom().Text($"抽選番号：No.{ticket.TicketNumber}")
                                    .FontFamily("Noto Sans JP SemiBold").FontSize(16);

                                row.ConstantItem(100).AlignRight().Height(80)
                                    .Image(GenerateQrCode(ticket.Url), ImageScaling.FitHeight);
                            });

                            // 説明と注意
                            ticketCol.Item().PaddingTop(5).Column(bottom =>
                            {
                                bottom.Item().Text(ticket.Description).FontSize(9);
                                bottom.Item().Text(ticket.Warning).FontSize(8);

                                // Powered by 表記
                                bottom.Item().AlignRight().Text("Powered by Micon club").FontSize(6).Italic().FontColor(Colors.Grey.Medium);
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

        using var image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(
            pixelData.Pixels, pixelData.Width, pixelData.Height
        );

        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }
}

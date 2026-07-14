using ZXing;
using ZXing.Common;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Micon.LotterySystem.Models.API;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using QuestPDF.Helpers;
using Micon.LotterySystem.Services;

namespace Micon.LotterySystem.Services
{
    public class TicketPdfGenerator : ITicketPdfGenerator
    {
    public byte[] GenerateTicketsPdf(List<TicketPrintData> tickets)
    {
        return Document.Create(container =>
        {
            // 1ページあたりのチケット数（2列 x 4行 = 8枚）
            const int ticketsPerPage = 8;
            const int columns = 2;
            const int rows = 4;

            // チケットをページごとに分割
            for (int pageStart = 0; pageStart < tickets.Count; pageStart += ticketsPerPage)
            {
                var pageTickets = tickets.Skip(pageStart).Take(ticketsPerPage).ToList();

                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Noto Sans JP").Medium());

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        // チケットを2列×3行で配置
                        for (int i = 0; i < pageTickets.Count; i += columns)
                        {
                            // 左列のチケット
                            table.Cell().Border(1).Padding(8).Column(ticketCol =>
                            {
                                RenderTicket(ticketCol, pageTickets[i]);
                            });

                            // 右列のチケット（存在する場合）
                            if (i + 1 < pageTickets.Count)
                            {
                                table.Cell().Border(1).Padding(8).Column(ticketCol =>
                                {
                                    RenderTicket(ticketCol, pageTickets[i + 1]);
                                });
                            }
                            else
                            {
                                table.Cell(); // 空のセル
                            }
                        }
                    });
                });
            }
        }).GeneratePdf();
    }

    private void RenderTicket(ColumnDescriptor ticketCol, TicketPrintData ticket)
    {
        // タイトル
        ticketCol.Item().AlignCenter().Text(ticket.TicketLabel).FontSize(16).FontFamily("Noto Sans JP").Bold();

        // 中段：QR + 番号
        ticketCol.Item().Row(row =>
        {
            row.Spacing(5);
            row.RelativeItem().AlignBottom().Text($"抽選番号：No.{ticket.TicketNumber}")
                .FontFamily("Noto Sans JP").FontSize(16).Bold();

            row.ConstantItem(100).AlignRight().Height(80)
                .Image(GenerateQrCode(ticket.Url), ImageScaling.FitHeight);
        });

        // 説明と注意
        ticketCol.Item().PaddingTop(5).Column(bottom =>
        {
            // 説明文（改行対応）
            if (!string.IsNullOrEmpty(ticket.Description))
            {
                var descLines = ticket.Description.Split('\n');
                foreach (var descLine in descLines)
                {
                    bottom.Item().Text(descLine).FontSize(9);
                }
            }

            // 説明と注意書きの間にスペース
            if (!string.IsNullOrEmpty(ticket.Description) && !string.IsNullOrEmpty(ticket.WarningText))
            {
                bottom.Item().PaddingTop(3);
            }

            // 注意書き（改行対応）
            if (!string.IsNullOrEmpty(ticket.WarningText))
            {
                var warningLines = ticket.WarningText.Split('\n');
                foreach (var line in warningLines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        bottom.Item().Text(line).FontSize(7);
                    }
                }
            }

            // フッターテキスト（カスタムまたはデフォルト）
            var footerText = !string.IsNullOrEmpty(ticket.FooterText)
                ? ticket.FooterText
                : "Powered by Micon club";
            bottom.Item().PaddingTop(10).AlignRight().Text(footerText).FontSize(6).Italic().FontColor(Colors.Grey.Medium);
        });
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
}

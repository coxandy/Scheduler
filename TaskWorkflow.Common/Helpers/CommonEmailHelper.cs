using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Utils;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using TaskWorkflow.Common.Models.BlockDefinition;
using TaskWorkflow.Common.Tasks;

namespace TaskWorkflow.Common.Helpers;

public static class CommonEmailHelper
{
    const string _sourceEmailName = "Andy Cox";
    const string _sourceEmail = "coxandy@yahoo.com";

    // Yahoo - App: C#Email
    // Use your 16-character Yahoo App Password (no spaces)
    const string _yahookey = "qrqunmydnvrmeuuv";

    public static async Task SendEmailAsync (   Message emailMessage,
                                                TaskContext taskContext,
                                                List<object>? chartConfigList = null,
                                                string? bannerFullFilePath = null,
                                                string? bannerOverlayText = null,
                                                int chartWidthPercent = 100,
                                                int chartHeightPercent = 100,
                                                bool showChartDataTables = true)
    {
        byte[] imageBytes;
        var body = emailMessage.Body ?? string.Empty;


        //Create message
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_sourceEmailName, _sourceEmail));

        foreach (var to in emailMessage.To ?? [])
            message.To.Add(new MailboxAddress("", to));
        foreach (var cc in emailMessage.CC ?? [])
            message.Cc.Add(new MailboxAddress("", cc));
        foreach (var bcc in emailMessage.BCC ?? [])
            message.Bcc.Add(new MailboxAddress("", bcc));

        message.Subject = emailMessage.Subject;
        message.Priority = emailMessage.Priority?.Equals("High", StringComparison.OrdinalIgnoreCase) == true
            ? MessagePriority.Urgent
            : MessagePriority.Normal;

        // Use BodyBuilder to handle both the HTML body and attachments
        var builder = new BodyBuilder();

        //handle header image
        if (bannerFullFilePath != null)
        {
            if (File.Exists(bannerFullFilePath))
            {
                var headerBytes = bannerOverlayText != null
                    ? OverlayTextOnImage(bannerFullFilePath, bannerOverlayText)
                    : await File.ReadAllBytesAsync(bannerFullFilePath);

                var headerImage = builder.LinkedResources.Add("header.png", headerBytes);
                headerImage.ContentId = MimeUtils.GenerateMessageId();
                body = $@"<img src=""cid:{headerImage.ContentId}"" style=""width:100%; display:block;"" />" + body;
            }
        }

        // handle chart data
        if (chartConfigList is { Count: > 0 })
        {
            int chartWidth = 500 * chartWidthPercent / 100;
            int chartHeight = 300 * chartHeightPercent / 100;

            foreach(var chartConfig in chartConfigList)
            {
                imageBytes = await CommonGraphDataHelper.CreateGraphImage(chartConfig, chartWidth, chartHeight);
                var image = builder.LinkedResources.Add($"chart_{Guid.CreateVersion7()}.png", imageBytes);
                image.ContentId = MimeUtils.GenerateMessageId();
                body += $@"<img src=""cid:{image.ContentId}"" style=""width:100%; display:block;"" />";

                if (showChartDataTables)
                {
                    body += CommonHtmlHelper.ChartConfigToHtml(chartConfig);
                }
            }
        }

        builder.HtmlBody = body;
        await AddAttachments(builder, emailMessage.Attachments);

        // Assign the combined content to the message body
        message.Body = builder.ToMessageBody();

        await SendMessageAsync(message);
    }

    private static async Task AddAttachments(BodyBuilder builder, List<string>? filePaths)
    {
        if (filePaths != null)
        {
            foreach (var path in filePaths)
            {
                // Check if the file exists before attempting to attach
                if (File.Exists(path))
                {
                    builder.Attachments.Add(path);
                }
            }
        }
    }

    public static async Task SendMessageAsync(MimeMessage message)
    {
        using (var client = new SmtpClient())
        {
            try
            {
                await client.ConnectAsync("smtp.mail.yahoo.com", 587, SecureSocketOptions.StartTls);
                // Yahoo - App: C#Email
                // Use your 16-character Yahoo App Password (no spaces)
                await client.AuthenticateAsync(_sourceEmail, _yahookey);
                await client.SendAsync(message);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }

    private static byte[] OverlayTextOnImage(string imagePath, string text)
    {
        using var image = Image.Load(imagePath);

        // Scale up 3x for crisp text rendering at any display width
        const int scale = 3;
        image.Mutate(ctx => ctx.Resize(image.Width * scale, image.Height * scale, KnownResamplers.Lanczos3));

        var font = SystemFonts.CreateFont("Segoe UI", image.Height * 0.27f, FontStyle.Bold);
        var options = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Origin = new System.Numerics.Vector2(image.Width - (10 * scale), (image.Height / 2f) - (2 * scale))
        };

        image.Mutate(ctx => ctx.DrawText(options, text, Color.White));

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}

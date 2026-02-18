using System.Text.RegularExpressions;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit.Utils;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using TaskWorkflow.Common.Models.BlockDefinition;
using TaskWorkflow.Common.Models.Configuration;
using TaskWorkflow.Common.Tasks;

namespace TaskWorkflow.Common.Helpers;

public static class CommonEmailHelper
{
    public static async Task SendEmailAsync (   Message emailMessage,
                                                TaskContext taskContext,
                                                EmailSettings emailSettings,
                                                List<object>? chartConfigList = null,                                       
                                                int chartWidthPercent = 100,
                                                int chartHeightPercent = 100,
                                                bool showChartDataTables = true)
    {
        byte[] imageBytes;
        var body = emailMessage.Body ?? string.Empty;
        string bannerFullFilePath = String.Empty;
        if (!String.IsNullOrEmpty(emailMessage.BannerFilePath) && emailMessage.IncludeBanner)
        {
            bannerFullFilePath = Path.Combine(emailMessage.BannerFilePath, emailMessage.BannerFileName);
        }

        //Create message
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(emailSettings.SenderName, emailSettings.SenderEmail));

        //Create recipient list (To, CC, BCC)
        if ((!emailMessage.To.Any()) && (!emailMessage.CC.Any()) && (!emailMessage.BCC.Any()))
        {
            throw new InvalidOperationException($"Email message has no recipients");
        }

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
        if (!String.IsNullOrEmpty(bannerFullFilePath))
        {
            if (File.Exists(bannerFullFilePath))
            {
                var headerBytes = emailMessage.BannerOverlayText != null
                    ? OverlayTextOnImage(bannerFullFilePath, emailMessage.BannerOverlayText)
                    : await File.ReadAllBytesAsync(bannerFullFilePath);

                var headerImage = builder.LinkedResources.Add("header.png", headerBytes);
                headerImage.ContentId = MimeUtils.GenerateMessageId();
                body = $@"<img src=""cid:{headerImage.ContentId}"" style=""width:100%; display:block;"" />" + body;
            }
            else
            {
                throw new FileNotFoundException($"{bannerFullFilePath} - banner file not found");
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

        body = ProcessEmailBody(body, taskContext);
        builder.HtmlBody = body;
        await AddAttachments(builder, emailMessage.Attachments);

        // Assign the combined content to the message body
        message.Body = builder.ToMessageBody();

        await SendMessageAsync(message, emailSettings);
    }

    internal static string ProcessEmailBody(string body, TaskContext taskContext)
    {
        return Regex.Replace(body, @"<<DATATABLE:\s*(.+?)>>", match =>
        {
            var tableName = match.Groups[1].Value.Trim();
            var dataTable = taskContext.GetDataTable(tableName);
            if (dataTable == null)
                return match.Value;

            return CommonHtmlHelper.DataTableToHtml(dataTable, showColumnHeaders: true);
        });
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

    private static async Task SendMessageAsync(MimeMessage message, EmailSettings emailSettings)
    {
        using (var client = new SmtpClient())
        {
            try
            {
                await client.ConnectAsync(emailSettings.SmtpServer, emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(emailSettings.SenderEmail, emailSettings.AppPassword);
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

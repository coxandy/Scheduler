namespace TaskWorkflow.Common.Models.BlockDefinition;

public class Message
{
    public bool SendEmail { get; set; } = true;
    public bool IncludeBanner { get; set; } = false;
    public string BannerFilePath { get; set; } = String.Empty;
    public string BannerFileName { get; set; } = String.Empty;
    public string BannerOverlayText { get; set; } = String.Empty;
    public List<string> To { get; set; }
    public List<string> CC { get; set; }
    public List<string> BCC { get; set; }
    public string Subject { get; set; } = String.Empty;
    public string Body { get; set; } = String.Empty;
    public string Priority { get; set; } = String.Empty;
    public List<string> Attachments { get; set; }
}

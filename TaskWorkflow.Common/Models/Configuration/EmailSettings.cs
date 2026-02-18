namespace TaskWorkflow.Common.Models.Configuration;

public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string AppPassword { get; set; } = string.Empty;
    public string DefaultErrorRecipient { get; set; } = string.Empty;
}

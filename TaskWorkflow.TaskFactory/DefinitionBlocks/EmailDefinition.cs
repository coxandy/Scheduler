using Microsoft.Extensions.Configuration;
using Serilog;
using TaskWorkflow.Common.Models.BlockDefinition;
using TaskWorkflow.Common.Models.Configuration;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;
using TaskWorkflow.Common.Helpers;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;

public class EmailDefinition : IDefinition
{
    public bool IsActive { get; set; } = true;
    public string BlockName { get; set; } = String.Empty;
    public eOnError OnError { get; set; } = eOnError.AbortTaskAndReportError;
    public eTaskStatus Status { get; set; }

    public List<Message> Messages { get; set; } = new();

    public async Task RunDefinitionBlockAsync(TaskInstance taskInstance, IServiceProvider serviceProvider, TaskContext taskContext)
    {
        Log.Debug($"RunDefinitionBlockAsync() - RunId: {taskInstance.RunId}  Running {GetType().Name}..");

        var emailSettings = ResolveEmailSettings(serviceProvider);

        foreach (var msg in Messages)
        {
            if (msg.SendEmail) await ProcessEmailMessage(msg, taskContext, emailSettings);
        }
    }

    internal static EmailSettings ResolveEmailSettings(IServiceProvider serviceProvider)
    {
        var config = serviceProvider?.GetService(typeof(IConfiguration)) as IConfiguration;
        var settings = new EmailSettings();
        config?.GetSection("Email").Bind(settings);
        return settings;
    }

    public async Task ProcessEmailMessage(Message emailMessage, TaskContext taskContext, EmailSettings emailSettings)
    {
        string bannerFullFilePath = String.Empty;
        if (emailMessage.IncludeBanner)
        {
            if (String.IsNullOrEmpty(emailMessage.BannerFilePath) || String.IsNullOrEmpty(emailMessage.BannerFileName))
            {
                throw new InvalidOperationException("Email config has missing banner file details");
            }
            bannerFullFilePath = Path.Combine(emailMessage.BannerFilePath, emailMessage.BannerFileName);

            if (!File.Exists(bannerFullFilePath))
            {
                throw new FileNotFoundException($"{bannerFullFilePath} - banner file not found");
            }
        }

        await CommonEmailHelper.SendEmailAsync(emailMessage, taskContext, emailSettings);
    }
}

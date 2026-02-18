using Serilog;
using TaskWorkflow.Common.Models.BlockDefinition;
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
    public eOnError OnError { get; set; } = eOnError.AbortTask;
    public eTaskStatus Status { get; set; }

    public List<Message> Messages { get; set; } = new();

    public async Task RunDefinitionBlockAsync(TaskInstance taskInstance, IServiceProvider serviceProvider, TaskContext taskContext)
    {
        Log.Debug($"RunDefinitionBlockAsync() - RunId: {taskInstance.RunId}  Running {GetType().Name}..");
        foreach (var msg in Messages)
        {
            if (msg.SendEmail) await ProcessEmailMessage(msg, taskContext);
        }
    }

    public async Task ProcessEmailMessage(Message emailMessage, TaskContext taskContext)
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

            //send email with banner
            if (!String.IsNullOrEmpty(emailMessage.BannerOverlayText))
            {
                //banner email with overlay text
                await CommonEmailHelper.SendEmailAsync(emailMessage, taskContext, bannerFullFilePath: bannerFullFilePath, bannerOverlayText: emailMessage.BannerOverlayText);
            }

            //default banner email
            await CommonEmailHelper.SendEmailAsync(emailMessage, taskContext, bannerFullFilePath: bannerFullFilePath);
        }
        else
        {
            await CommonEmailHelper.SendEmailAsync(emailMessage, taskContext);    
        }
    }
}

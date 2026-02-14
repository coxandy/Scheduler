using System.Text;
using System.Text.Json;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Scheduler.Interfaces;
using Serilog;

namespace TaskWorkflow.Scheduler.Services;

public class TaskExecutionService : ITaskExecutionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _client;

    public TaskExecutionService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _client = _httpClientFactory.CreateClient();
    }

    public async Task ExecuteTask(ScheduledTask scheduledTask)
    {
        TaskInstance taskInstance = new TaskInstance();
        taskInstance.RunId = Guid.CreateVersion7().ToString();
        taskInstance.Instance = scheduledTask;
        taskInstance.dtEffective = DateTime.Today.AddDays(scheduledTask.DayOffset);
        taskInstance.IsManual = false;
        taskInstance.Status= Common.Models.Enums.eTaskStatus.ReadyToRun;
        string json = JsonSerializer.Serialize(taskInstance);
        await PostScheduledTask(json, scheduledTask.WebService);
    }

    private async Task<HttpResponseMessage?> PostScheduledTask(string json, string webService)
    {
        var serverCount = UriHelper.AvailableWebServers.Count;
        var primaryIndex = UriHelper.GetServerIndex(webService);

        // If the HttpPost call fails retry against the other available servers
        for (int attempt = 0; attempt < serverCount; attempt++)
        {
            int serverIndex = (primaryIndex + attempt) % serverCount;
            string url = $"{UriHelper.GetUriForServer(serverIndex)}/TaskExecution/ExecuteTask";

            Log.Information($"Posting task '{webService}' to {url} (attempt {attempt + 1} of {serverCount})");
            var response = await Post(json, url, webService, attempt + 1, serverCount);
            if (response != null)
                return response;
        }

        Log.Error($"Task '{webService}' failed on all {serverCount} servers");
        return null;
    }

    private async Task<HttpResponseMessage?> Post(string json, string url, string webService, int attempt, int serverCount)
    {
        Log.Debug("Request payload: {Json}", json);

        try
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Task '{WebService}' posted successfully to {Url} with status {StatusCode}", webService, url, (int)response.StatusCode);
                return response;
            }

            Log.Warning("Task '{WebService}' post to {Url} failed with status {StatusCode}: {ReasonPhrase}. Response: {ResponseBody}", webService, url, (int)response.StatusCode, response.ReasonPhrase, responseBody);
        }
        catch (HttpRequestException ex)
        {
            Log.Warning(ex, "HTTP request failed for {Url} (attempt {Attempt} of {ServerCount})", url, attempt, serverCount);
        }
        catch (TaskCanceledException ex)
        {
            Log.Warning(ex, "Request to {Url} timed out (attempt {Attempt} of {ServerCount})", url, attempt, serverCount);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error posting task to {Url}", url);
        }

        return null;
    }
}
